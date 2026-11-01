using Nagara.App.Diagnostics;
using Nagara.App.Localization;
using Nagara.Core.Media;
using Nagara.Core.Routing;
using Nagara.Core.Settings;

namespace Nagara.App;

/// <summary>
/// Resolves the destination for one media key press, launches Spotify when the
/// Spotify-fixed mode has no session, sends the command, and records the
/// outcome. Commands are serialized by the caller's single reader loop.
/// </summary>
internal sealed class RouteCommandHandler
{
    private static readonly TimeSpan SpotifyLaunchWait = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SpotifyLaunchCooldown = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan SpotifyPollInterval = TimeSpan.FromMilliseconds(350);

    private readonly SettingsStore _settings;
    private readonly GsmtcSessionService _sessions;
    private readonly RouteResolver _resolver;
    private readonly DiagnosticsState _diagnostics;
    private readonly DiagnosticsLog _log;
    private readonly SpotifyLauncher _spotifyLauncher;
    private readonly SemaphoreSlim _spotifyLaunchGate = new(1, 1);

    private DateTimeOffset _lastSpotifyLaunchAttempt = DateTimeOffset.MinValue;

    public RouteCommandHandler(
        SettingsStore settings,
        GsmtcSessionService sessions,
        RouteResolver resolver,
        DiagnosticsState diagnostics,
        DiagnosticsLog log,
        SpotifyLauncher spotifyLauncher)
    {
        _settings = settings;
        _sessions = sessions;
        _resolver = resolver;
        _diagnostics = diagnostics;
        _log = log;
        _spotifyLauncher = spotifyLauncher;
    }

    public async Task HandleAsync(MediaKeyCommand command, CancellationToken cancellationToken)
    {
        var settings = _settings.Current;
        _diagnostics.RecordKeyPressed(command);

        try
        {
            var catalog = await _sessions.GetCatalogAsync(cancellationToken).ConfigureAwait(false);
            var decision = _resolver.Resolve(
                settings.Mode,
                settings.FixedSessionId,
                catalog.Sessions,
                catalog.CurrentSessionId,
                settings.LastSentSessionId);

            var target = decision.Session;

            if (target is null && settings.Mode == RoutingMode.Spotify)
            {
                target = await LaunchSpotifyAndWaitAsync(cancellationToken).ConfigureAwait(false);
            }

            if (target is null)
            {
                _diagnostics.RecordNoTarget();
                _log.Write("INFO", $"No destination for {command} (mode={settings.Mode}).");
                return;
            }

            var sent = await _sessions
                .SendAsync(target.AppUserModelId, command, cancellationToken)
                .ConfigureAwait(false);

            if (!sent)
            {
                _diagnostics.RecordSendFailed(target, _sessions.LastError);
                _log.Write(
                    "WARN",
                    $"GSMTC did not accept {command} for {target.AppUserModelId}.");
                return;
            }

            try
            {
                _settings.Update(current => current with
                {
                    LastSentSessionId = target.AppUserModelId,
                });
            }
            catch (Exception ex)
            {
                // Sending worked; failing to remember the target is not fatal.
                _diagnostics.RecordError(
                    Strings.Format("Diagnostics.PersistLastTargetFailed", ex.Message));
                _log.Write("WARN", $"Could not persist the last destination: {ex.Message}");
            }

            _diagnostics.RecordSent(target);
            _log.Write("INFO", $"Sent {command} to {target.AppUserModelId}.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _diagnostics.RecordError(ex.Message);
            _log.Write("ERROR", $"Media key handling failed: {ex}");
        }
    }

    private async Task<MediaSessionSnapshot?> LaunchSpotifyAndWaitAsync(
        CancellationToken cancellationToken)
    {
        await _spotifyLaunchGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = DateTimeOffset.UtcNow;
            if (now - _lastSpotifyLaunchAttempt < SpotifyLaunchCooldown)
            {
                _log.Write("INFO", "Spotify launch skipped because of the retry cooldown.");
                return null;
            }

            _lastSpotifyLaunchAttempt = now;

            if (!_spotifyLauncher.TryLaunch())
            {
                _diagnostics.RecordError(Strings.Get("Diagnostics.SpotifyLaunchFailed"));
                return null;
            }

            var deadline = now + SpotifyLaunchWait;
            while (DateTimeOffset.UtcNow < deadline)
            {
                await Task.Delay(SpotifyPollInterval, cancellationToken).ConfigureAwait(false);

                var catalog = await _sessions.GetCatalogAsync(cancellationToken).ConfigureAwait(false);
                var spotify = catalog.Sessions
                    .Where(session => SpotifyIdentity.Matches(session.AppUserModelId, session.DisplayName))
                    .OrderByDescending(session => session.IsPlaying)
                    .FirstOrDefault();

                if (spotify is not null)
                {
                    _log.Write("INFO", "Spotify session appeared after launch.");
                    return spotify;
                }
            }

            _diagnostics.RecordError(Strings.Get("Diagnostics.SpotifySessionTimeout"));
            _log.Write("WARN", "Spotify did not create a media session within 10 seconds.");
            return null;
        }
        finally
        {
            _spotifyLaunchGate.Release();
        }
    }
}
