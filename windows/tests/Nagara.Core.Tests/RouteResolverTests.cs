using Nagara.Core.Media;
using Nagara.Core.Routing;

namespace Nagara.Core.Tests;

public sealed class RouteResolverTests
{
    private readonly RouteResolver _resolver = new();

    [Fact]
    public void SpotifyModeChoosesSpotifyEvenWhenBrowserIsPlayingAndCurrent()
    {
        var sessions = new[]
        {
            Session("msedge.exe", isPlaying: true),
            Session("Spotify.exe", isPlaying: false),
        };

        var decision = _resolver.Resolve(
            RoutingMode.Spotify,
            fixedSessionId: null,
            sessions,
            currentSessionId: "msedge.exe",
            lastSentSessionId: "msedge.exe");

        Assert.Equal("Spotify.exe", decision.Session?.AppUserModelId);
        Assert.Equal(RouteReason.SpotifyFixed, decision.Reason);
    }

    [Fact]
    public void SpotifyModeChoosesAPlayingSpotifySessionWhenThereAreTwo()
    {
        var sessions = new[]
        {
            Session("SpotifyAB.SpotifyMusic_zpdnekdrzrea0!Spotify", isPlaying: false),
            Session("Spotify.exe", isPlaying: true),
        };

        var decision = _resolver.Resolve(
            RoutingMode.Spotify,
            fixedSessionId: null,
            sessions,
            currentSessionId: "SpotifyAB.SpotifyMusic_zpdnekdrzrea0!Spotify",
            lastSentSessionId: null);

        Assert.Equal("Spotify.exe", decision.Session?.AppUserModelId);
        Assert.Equal(RouteReason.SpotifyFixed, decision.Reason);
    }

    [Fact]
    public void SpotifyModeRecognisesDisplayNameWhenIdIsUnusual()
    {
        var sessions = new[] { Session("UnknownPlayer.exe", isPlaying: true, displayName: "Spotify") };

        var decision = _resolver.Resolve(
            RoutingMode.Spotify,
            fixedSessionId: null,
            sessions,
            currentSessionId: null,
            lastSentSessionId: null);

        Assert.Equal("UnknownPlayer.exe", decision.Session?.AppUserModelId);
        Assert.Equal(RouteReason.SpotifyFixed, decision.Reason);
    }

    [Fact]
    public void SpotifyModeDoesNothingWhenSpotifyIsAbsent()
    {
        var sessions = new[]
        {
            Session("msedge.exe", isPlaying: true),
            Session("chrome.exe", isPlaying: false),
        };

        var decision = _resolver.Resolve(
            RoutingMode.Spotify,
            fixedSessionId: null,
            sessions,
            currentSessionId: "msedge.exe",
            lastSentSessionId: null);

        Assert.Null(decision.Session);
        Assert.Equal(RouteReason.None, decision.Reason);
    }

    [Fact]
    public void FixedModeChoosesTheSavedSessionEvenWhenSpotifyIsPlaying()
    {
        var sessions = new[]
        {
            Session("msedge.exe", isPlaying: false),
            Session("Spotify.exe", isPlaying: true),
        };

        var decision = _resolver.Resolve(
            RoutingMode.FixedSession,
            fixedSessionId: "msedge.exe",
            sessions,
            currentSessionId: "Spotify.exe",
            lastSentSessionId: "Spotify.exe");

        Assert.Equal("msedge.exe", decision.Session?.AppUserModelId);
        Assert.Equal(RouteReason.FixedSession, decision.Reason);
    }

    [Fact]
    public void FixedModeMatchesIdsCaseInsensitively()
    {
        var sessions = new[] { Session("MSEDGE.EXE", isPlaying: false) };

        var decision = _resolver.Resolve(
            RoutingMode.FixedSession,
            fixedSessionId: "msedge.exe",
            sessions,
            currentSessionId: null,
            lastSentSessionId: null);

        Assert.Equal("MSEDGE.EXE", decision.Session?.AppUserModelId);
    }

    [Fact]
    public void FixedModeDoesNothingWhenTheSavedSessionIsAbsent()
    {
        var sessions = new[] { Session("Spotify.exe", isPlaying: true) };

        var decision = _resolver.Resolve(
            RoutingMode.FixedSession,
            fixedSessionId: "msedge.exe",
            sessions,
            currentSessionId: "Spotify.exe",
            lastSentSessionId: null);

        Assert.Null(decision.Session);
        Assert.Equal(RouteReason.None, decision.Reason);
    }

    [Fact]
    public void AutomaticPrefersTheCurrentSessionWhenItIsPlaying()
    {
        var sessions = new[]
        {
            Session("msedge.exe", isPlaying: true),
            Session("Spotify.exe", isPlaying: true),
        };

        var decision = _resolver.Resolve(
            RoutingMode.Automatic,
            fixedSessionId: null,
            sessions,
            currentSessionId: "msedge.exe",
            lastSentSessionId: "Spotify.exe");

        Assert.Equal("msedge.exe", decision.Session?.AppUserModelId);
        Assert.Equal(RouteReason.CurrentPlaying, decision.Reason);
    }

    [Fact]
    public void AutomaticChoosesTheOnlyPlayingSessionWhenCurrentIsPaused()
    {
        var sessions = new[]
        {
            Session("msedge.exe", isPlaying: false),
            Session("Spotify.exe", isPlaying: true),
        };

        var decision = _resolver.Resolve(
            RoutingMode.Automatic,
            fixedSessionId: null,
            sessions,
            currentSessionId: "msedge.exe",
            lastSentSessionId: null);

        Assert.Equal("Spotify.exe", decision.Session?.AppUserModelId);
        Assert.Equal(RouteReason.SpotifyPlaying, decision.Reason);
    }

    [Fact]
    public void AutomaticChoosesANonSpotifyPlayingSession()
    {
        var sessions = new[]
        {
            Session("Spotify.exe", isPlaying: false),
            Session("msedge.exe", isPlaying: true),
        };

        var decision = _resolver.Resolve(
            RoutingMode.Automatic,
            fixedSessionId: null,
            sessions,
            currentSessionId: "Spotify.exe",
            lastSentSessionId: "Spotify.exe");

        Assert.Equal("msedge.exe", decision.Session?.AppUserModelId);
        Assert.Equal(RouteReason.FirstPlaying, decision.Reason);
    }

    [Fact]
    public void AutomaticPrefersLastSentWhenMultipleSessionsPlayAndCurrentIsPaused()
    {
        var sessions = new[]
        {
            Session("msedge.exe", isPlaying: true),
            Session("Spotify.exe", isPlaying: true),
            Session("chrome.exe", isPlaying: false),
        };

        var decision = _resolver.Resolve(
            RoutingMode.Automatic,
            fixedSessionId: null,
            sessions,
            currentSessionId: "chrome.exe",
            lastSentSessionId: "Spotify.exe");

        Assert.Equal("Spotify.exe", decision.Session?.AppUserModelId);
        Assert.Equal(RouteReason.LastSentPlaying, decision.Reason);
    }

    [Fact]
    public void AutomaticFallsBackToSpotifyWhenMultipleSessionsPlayAndHistoryIsEmpty()
    {
        var sessions = new[]
        {
            Session("msedge.exe", isPlaying: true),
            Session("Spotify.exe", isPlaying: true),
            Session("chrome.exe", isPlaying: false),
        };

        var decision = _resolver.Resolve(
            RoutingMode.Automatic,
            fixedSessionId: null,
            sessions,
            currentSessionId: "chrome.exe",
            lastSentSessionId: null);

        Assert.Equal("Spotify.exe", decision.Session?.AppUserModelId);
        Assert.Equal(RouteReason.SpotifyPlaying, decision.Reason);
    }

    [Fact]
    public void AutomaticFallsBackToTheFirstPlayingSessionWhenSpotifyIsNotPlaying()
    {
        var sessions = new[]
        {
            Session("msedge.exe", isPlaying: true),
            Session("chrome.exe", isPlaying: true),
            Session("firefox.exe", isPlaying: false),
        };

        var decision = _resolver.Resolve(
            RoutingMode.Automatic,
            fixedSessionId: null,
            sessions,
            currentSessionId: "firefox.exe",
            lastSentSessionId: null);

        Assert.Equal("msedge.exe", decision.Session?.AppUserModelId);
        Assert.Equal(RouteReason.FirstPlaying, decision.Reason);
    }

    [Fact]
    public void AutomaticIgnoresAPausedLastSentSessionWhenOthersArePlaying()
    {
        var sessions = new[]
        {
            Session("msedge.exe", isPlaying: true),
            Session("Spotify.exe", isPlaying: true),
            Session("chrome.exe", isPlaying: false),
        };

        var decision = _resolver.Resolve(
            RoutingMode.Automatic,
            fixedSessionId: null,
            sessions,
            currentSessionId: null,
            lastSentSessionId: "chrome.exe");

        Assert.Equal("Spotify.exe", decision.Session?.AppUserModelId);
        Assert.Equal(RouteReason.SpotifyPlaying, decision.Reason);
    }

    [Fact]
    public void AutomaticUsesTheCurrentSessionWhenNothingIsPlaying()
    {
        var sessions = new[]
        {
            Session("msedge.exe", isPlaying: false),
            Session("Spotify.exe", isPlaying: false),
        };

        var decision = _resolver.Resolve(
            RoutingMode.Automatic,
            fixedSessionId: null,
            sessions,
            currentSessionId: "msedge.exe",
            lastSentSessionId: "Spotify.exe");

        Assert.Equal("msedge.exe", decision.Session?.AppUserModelId);
        Assert.Equal(RouteReason.CurrentSession, decision.Reason);
    }

    [Fact]
    public void AutomaticUsesLastSentWhenNothingIsPlayingAndCurrentIsGone()
    {
        var sessions = new[] { Session("Spotify.exe", isPlaying: false) };

        var decision = _resolver.Resolve(
            RoutingMode.Automatic,
            fixedSessionId: null,
            sessions,
            currentSessionId: null,
            lastSentSessionId: "Spotify.exe");

        Assert.Equal("Spotify.exe", decision.Session?.AppUserModelId);
        Assert.Equal(RouteReason.LastSentSession, decision.Reason);
    }

    [Fact]
    public void AutomaticFallsBackToSpotifyWhenNothingIsPlayingAndHistoryIsEmpty()
    {
        var sessions = new[]
        {
            Session("msedge.exe", isPlaying: false),
            Session("Spotify.exe", isPlaying: false),
        };

        var decision = _resolver.Resolve(
            RoutingMode.Automatic,
            fixedSessionId: null,
            sessions,
            currentSessionId: null,
            lastSentSessionId: null);

        Assert.Equal("Spotify.exe", decision.Session?.AppUserModelId);
        Assert.Equal(RouteReason.SpotifySession, decision.Reason);
    }

    [Fact]
    public void AutomaticDoesNothingWhenThereAreNoSessions()
    {
        var decision = _resolver.Resolve(
            RoutingMode.Automatic,
            fixedSessionId: null,
            Array.Empty<MediaSessionSnapshot>(),
            currentSessionId: "msedge.exe",
            lastSentSessionId: "Spotify.exe");

        Assert.Null(decision.Session);
        Assert.Equal(RouteReason.None, decision.Reason);
    }

    [Fact]
    public void AutomaticFallsBackToAPlayingSessionWhenTheCurrentIdIsUnknown()
    {
        var sessions = new[] { Session("msedge.exe", isPlaying: true) };

        var decision = _resolver.Resolve(
            RoutingMode.Automatic,
            fixedSessionId: null,
            sessions,
            currentSessionId: "gone.exe",
            lastSentSessionId: null);

        Assert.Equal("msedge.exe", decision.Session?.AppUserModelId);
        Assert.Equal(RouteReason.FirstPlaying, decision.Reason);
    }

    [Fact]
    public void FixedModePrefersAPlayingDuplicateOfTheSavedId()
    {
        var sessions = new[]
        {
            new MediaSessionSnapshot("msedge.exe", "Microsoft Edge", PlaybackStatus.Paused),
            new MediaSessionSnapshot("MSEDGE.EXE", "Microsoft Edge", PlaybackStatus.Playing),
        };

        var decision = _resolver.Resolve(
            RoutingMode.FixedSession,
            fixedSessionId: "msedge.exe",
            sessions,
            currentSessionId: null,
            lastSentSessionId: null);

        Assert.Equal(PlaybackStatus.Playing, decision.Session?.Status);
    }

    private static MediaSessionSnapshot Session(
        string id,
        bool isPlaying,
        string? displayName = null)
        => new(
            id,
            displayName ?? FriendlySessionNames.Resolve(id),
            isPlaying ? PlaybackStatus.Playing : PlaybackStatus.Paused);
}
