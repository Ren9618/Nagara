using System.IO;

namespace Nagara.Core.Media;

/// <summary>
/// GSMTC exposes only an AUMID, so common desktop players and packaged apps
/// are mapped to friendly names. Unknown sessions fall back to the executable
/// name (or the class name after "!") and always keep the AUMID visible in the
/// settings diagnostics.
/// </summary>
public static class FriendlySessionNames
{
    private static readonly Dictionary<string, string> KnownNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Spotify.exe"] = "Spotify",
            ["msedge.exe"] = "Microsoft Edge",
            ["chrome.exe"] = "Google Chrome",
            ["firefox.exe"] = "Mozilla Firefox",
            ["brave.exe"] = "Brave",
            ["opera.exe"] = "Opera",
            ["opera_gx.exe"] = "Opera GX",
            ["vivaldi.exe"] = "Vivaldi",
            ["wmplayer.exe"] = "Windows Media Player",
            ["vlc.exe"] = "VLC media player",
            ["musicbee.exe"] = "MusicBee",
            ["foobar2000.exe"] = "foobar2000",
            ["microsoft.zunemusic_8wekyb3d8bbwe!microsoft.zunemusic"] = "Media Player",
            ["microsoft.microsoftedge_8wekyb3d8bbwe!microsoftedge"] = "Microsoft Edge",
        };

    public static string Resolve(string? appUserModelId)
    {
        var id = appUserModelId?.Trim() ?? string.Empty;
        if (id.Length == 0)
        {
            return string.Empty;
        }

        if (KnownNames.TryGetValue(id, out var known))
        {
            return known;
        }

        if (SpotifyIdentity.MatchesId(id))
        {
            return "Spotify";
        }

        if (id.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetFileNameWithoutExtension(id);
        }

        var bang = id.LastIndexOf('!');
        if (bang >= 0 && bang < id.Length - 1)
        {
            return id[(bang + 1)..];
        }

        return id;
    }
}
