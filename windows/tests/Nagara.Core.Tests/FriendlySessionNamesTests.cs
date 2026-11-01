using Nagara.Core.Media;

namespace Nagara.Core.Tests;

public sealed class FriendlySessionNamesTests
{
    [Theory]
    [InlineData("msedge.exe", "Microsoft Edge")]
    [InlineData("chrome.exe", "Google Chrome")]
    [InlineData("firefox.exe", "Mozilla Firefox")]
    [InlineData("vlc.exe", "VLC media player")]
    [InlineData("Spotify.exe", "Spotify")]
    [InlineData("SpotifyAB.SpotifyMusic_zpdnekdrzrea0!Spotify", "Spotify")]
    [InlineData("Microsoft.ZuneMusic_8wekyb3d8bbwe!Microsoft.ZuneMusic", "Media Player")]
    [InlineData("UnknownPlayer.exe", "UnknownPlayer")]
    [InlineData("Contoso.Music_123!App", "App")]
    [InlineData("notpackaged", "notpackaged")]
    [InlineData("  msedge.exe  ", "Microsoft Edge")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void ResolvesFriendlyNames(string? appUserModelId, string expected)
    {
        Assert.Equal(expected, FriendlySessionNames.Resolve(appUserModelId));
    }
}
