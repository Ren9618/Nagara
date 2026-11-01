using Nagara.App.Diagnostics;
using Nagara.App.Localization;
using Nagara.Core.Media;
using Windows.Foundation;
using Windows.Media.Control;

namespace Nagara.App;

/// <summary>
/// Thin wrapper around GSMTC. It reads only playback status and AUMIDs — never
/// media titles, artists, thumbnails, or other metadata.
/// </summary>
internal sealed class GsmtcSessionService : IDisposable
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RetryCooldown = TimeSpan.FromSeconds(10);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly DiagnosticsLog _log;
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private bool _requestFailed;
    private DateTimeOffset _lastRequestAttempt;
    private bool _disposed;

    public GsmtcSessionService(DiagnosticsLog log)
    {
        _log = log;
    }

    public string? LastError { get; private set; }

    public async Task<bool> EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_manager is not null)
        {
            return true;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_manager is not null)
            {
                return true;
            }

            if (_requestFailed && DateTimeOffset.UtcNow - _lastRequestAttempt < RetryCooldown)
            {
                return false;
            }

            _lastRequestAttempt = DateTimeOffset.UtcNow;
            try
            {
                var operation = GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                _manager = await operation
                    .AsTask()
                    .WaitAsync(RequestTimeout, cancellationToken)
                    .ConfigureAwait(false);
                _requestFailed = false;
                LastError = null;
                return _manager is not null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _requestFailed = true;
                LastError = ex.Message;
                _log.Write("ERROR", $"GSMTC initialization failed: {ex.Message}");
                return false;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<SessionCatalog> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        if (!await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false))
        {
            return SessionCatalog.Empty;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_manager is null)
            {
                return SessionCatalog.Empty;
            }

            var sessions = new List<MediaSessionSnapshot>();
            foreach (var session in _manager.GetSessions())
            {
                var snapshot = CreateSnapshot(session);
                if (snapshot is not null)
                {
                    sessions.Add(snapshot);
                }
            }

            string? currentSessionId = null;
            try
            {
                currentSessionId = _manager.GetCurrentSession()?.SourceAppUserModelId;
            }
            catch (Exception ex)
            {
                _log.Write("WARN", $"Could not read the current GSMTC session: {ex.Message}");
            }

            return new SessionCatalog(sessions, currentSessionId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LastError = ex.Message;
            _log.Write("ERROR", $"Could not enumerate GSMTC sessions: {ex.Message}");
            return SessionCatalog.Empty;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> SendAsync(
        string appUserModelId,
        MediaKeyCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(appUserModelId))
        {
            return false;
        }

        if (!await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_manager is null)
            {
                return false;
            }

            var session = _manager.GetSessions().FirstOrDefault(candidate =>
                string.Equals(
                    candidate.SourceAppUserModelId,
                    appUserModelId,
                    StringComparison.OrdinalIgnoreCase));

            if (session is null)
            {
                LastError = Strings.Get("Diagnostics.TargetDisappeared");
                return false;
            }

            var sent = command switch
            {
                MediaKeyCommand.PlayPause => await TogglePlayPauseAsync(session).ConfigureAwait(false),
                MediaKeyCommand.NextTrack => await AwaitBoolAsync(session.TrySkipNextAsync()).ConfigureAwait(false),
                MediaKeyCommand.PreviousTrack => await AwaitBoolAsync(session.TrySkipPreviousAsync()).ConfigureAwait(false),
                _ => false,
            };

            if (!sent)
            {
                LastError = Strings.Format(
                    "Diagnostics.CommandRejected",
                    command,
                    appUserModelId);
            }

            return sent;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LastError = ex.Message;
            _log.Write("ERROR", $"Sending {command} to {appUserModelId} failed: {ex.Message}");
            return false;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static MediaSessionSnapshot? CreateSnapshot(GlobalSystemMediaTransportControlsSession session)
    {
        var id = session.SourceAppUserModelId?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        var status = PlaybackStatus.Unknown;
        try
        {
            status = MapStatus(session.GetPlaybackInfo().PlaybackStatus);
        }
        catch (Exception)
        {
            // A session can disappear between enumeration and inspection.
        }

        return new MediaSessionSnapshot(id, FriendlySessionNames.Resolve(id), status);
    }

    private static PlaybackStatus MapStatus(
        GlobalSystemMediaTransportControlsSessionPlaybackStatus status)
    {
        return status switch
        {
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => PlaybackStatus.Playing,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => PlaybackStatus.Paused,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped => PlaybackStatus.Stopped,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed => PlaybackStatus.Closed,
            _ => PlaybackStatus.Unknown,
        };
    }

    private static async Task<bool> TogglePlayPauseAsync(
        GlobalSystemMediaTransportControlsSession session)
    {
        // Toggle is the normal path. Some players advertise Play but not
        // TogglePlayPause, so fall back once instead of dropping the key.
        if (await AwaitBoolAsync(session.TryTogglePlayPauseAsync()).ConfigureAwait(false))
        {
            return true;
        }

        // Some players expose play/pause but not toggle. Match the current
        // state so the key still does the expected thing.
        try
        {
            var status = session.GetPlaybackInfo().PlaybackStatus;
            return status == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing
                ? await AwaitBoolAsync(session.TryPauseAsync()).ConfigureAwait(false)
                : await AwaitBoolAsync(session.TryPlayAsync()).ConfigureAwait(false);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static async Task<bool> AwaitBoolAsync(IAsyncOperation<bool> operation)
        => await operation.AsTask().WaitAsync(SendTimeout).ConfigureAwait(false);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _gate.Dispose();
    }
}
