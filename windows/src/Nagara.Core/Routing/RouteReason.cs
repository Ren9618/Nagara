namespace Nagara.Core.Routing;

/// <summary>Why a particular session was chosen. Useful for diagnostics and tests.</summary>
public enum RouteReason
{
    None,
    SpotifyFixed,
    FixedSession,
    CurrentPlaying,
    LastSentPlaying,
    SpotifyPlaying,
    FirstPlaying,
    CurrentSession,
    LastSentSession,
    SpotifySession,
}
