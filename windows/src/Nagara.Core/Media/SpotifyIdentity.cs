namespace Nagara.Core.Media;

/// <summary>
/// Recognises Spotify sessions across installation channels. The desktop app
/// reports IDs such as "Spotify.exe", while Store builds use package-style
/// AUMIDs. Matching therefore normalises the AUMID and also checks the display
/// name rather than relying on one fixed string.
/// </summary>
public static class SpotifyIdentity
{
    public static bool Matches(string? appUserModelId, string? displayName)
        => MatchesId(appUserModelId) || MatchesDisplayName(displayName);

    public static bool MatchesId(string? appUserModelId)
    {
        var normalized = Normalize(appUserModelId);
        if (normalized.Length == 0)
        {
            return false;
        }

        // GSMTC normally reports just the executable name, but tolerate a full
        // path as well (some desktop registrations do).
        var lastSeparator = Math.Max(normalized.LastIndexOf('\\'), normalized.LastIndexOf('/'));
        if (lastSeparator >= 0)
        {
            normalized = normalized[(lastSeparator + 1)..];
        }

        if (normalized.EndsWith(".exe", StringComparison.Ordinal))
        {
            normalized = normalized[..^4];
        }

        return normalized == "spotify"
            || normalized.StartsWith("spotify.", StringComparison.Ordinal)
            || normalized.StartsWith("spotifyab.", StringComparison.Ordinal)
            || normalized.Contains(".spotify", StringComparison.Ordinal)
            || normalized.Contains("spotifymusic", StringComparison.Ordinal)
            || normalized.Contains("spotify_", StringComparison.Ordinal);
    }

    public static bool MatchesDisplayName(string? displayName)
    {
        var normalized = Normalize(displayName);
        return normalized == "spotify"
            || normalized.StartsWith("spotify ", StringComparison.Ordinal)
            || normalized.StartsWith("spotify-", StringComparison.Ordinal)
            || normalized.StartsWith("spotify.", StringComparison.Ordinal);
    }

    private static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
}
