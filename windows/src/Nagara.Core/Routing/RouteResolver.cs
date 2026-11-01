using Nagara.Core.Media;

namespace Nagara.Core.Routing;

/// <summary>
/// Pure routing rules, deliberately independent of WinRT so every branch can
/// be unit tested. Sessions come from GSMTC; the current session and the last
/// session Nagara sent to are supplied by the caller.
/// </summary>
public sealed class RouteResolver
{
    public RouteDecision Resolve(
        RoutingMode mode,
        string? fixedSessionId,
        IReadOnlyList<MediaSessionSnapshot> sessions,
        string? currentSessionId,
        string? lastSentSessionId)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        return mode switch
        {
            RoutingMode.Spotify => ResolveSpotify(sessions, currentSessionId),
            RoutingMode.FixedSession => ResolveFixed(sessions, fixedSessionId),
            _ => ResolveAutomatic(sessions, currentSessionId, lastSentSessionId),
        };
    }

    private static RouteDecision ResolveSpotify(
        IReadOnlyList<MediaSessionSnapshot> sessions,
        string? currentSessionId)
    {
        var spotify = FindSpotify(sessions, currentSessionId);
        return spotify is null
            ? RouteDecision.None
            : new RouteDecision(spotify, RouteReason.SpotifyFixed);
    }

    private static RouteDecision ResolveFixed(
        IReadOnlyList<MediaSessionSnapshot> sessions,
        string? fixedSessionId)
    {
        var target = FindPreferred(sessions, fixedSessionId);
        return target is null
            ? RouteDecision.None
            : new RouteDecision(target, RouteReason.FixedSession);
    }

    private static RouteDecision ResolveAutomatic(
        IReadOnlyList<MediaSessionSnapshot> sessions,
        string? currentSessionId,
        string? lastSentSessionId)
    {
        var playing = sessions.Where(session => session.IsPlaying).ToList();

        if (playing.Count > 0)
        {
            // The current session is the base choice, but only when it is
            // actually playing. Otherwise prefer playing sessions.
            var current = FindPreferred(sessions, currentSessionId);
            if (current is { IsPlaying: true })
            {
                return new RouteDecision(current, RouteReason.CurrentPlaying);
            }

            var lastPlaying = FindPreferred(playing, lastSentSessionId);
            if (lastPlaying is not null)
            {
                return new RouteDecision(lastPlaying, RouteReason.LastSentPlaying);
            }

            var spotifyPlaying = FindSpotify(playing, currentSessionId);
            if (spotifyPlaying is not null)
            {
                return new RouteDecision(spotifyPlaying, RouteReason.SpotifyPlaying);
            }

            return new RouteDecision(playing[0], RouteReason.FirstPlaying);
        }

        // Nothing is playing: stay with the current session if Windows has
        // one, then the last target, then Spotify.
        var currentSession = FindPreferred(sessions, currentSessionId);
        if (currentSession is not null)
        {
            return new RouteDecision(currentSession, RouteReason.CurrentSession);
        }

        var lastSession = FindPreferred(sessions, lastSentSessionId);
        if (lastSession is not null)
        {
            return new RouteDecision(lastSession, RouteReason.LastSentSession);
        }

        var spotify = FindSpotify(sessions, currentSessionId);
        return spotify is null
            ? RouteDecision.None
            : new RouteDecision(spotify, RouteReason.SpotifySession);
    }

    private static MediaSessionSnapshot? FindSpotify(
        IEnumerable<MediaSessionSnapshot> sessions,
        string? currentSessionId)
    {
        var matches = sessions
            .Where(session => SpotifyIdentity.Matches(session.AppUserModelId, session.DisplayName))
            .ToList();

        if (matches.Count == 0)
        {
            return null;
        }

        var playing = matches.Where(session => session.IsPlaying).ToList();
        if (playing.Count > 0)
        {
            return FindPreferred(playing, currentSessionId) ?? playing[0];
        }

        return FindPreferred(matches, currentSessionId) ?? matches[0];
    }

    private static MediaSessionSnapshot? FindPreferred(
        IEnumerable<MediaSessionSnapshot> sessions,
        string? appUserModelId)
    {
        if (string.IsNullOrWhiteSpace(appUserModelId))
        {
            return null;
        }

        var id = appUserModelId.Trim();
        var matches = sessions
            .Where(session => string.Equals(
                session.AppUserModelId,
                id,
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        return matches.FirstOrDefault(session => session.IsPlaying) ?? matches.FirstOrDefault();
    }
}
