using Nagara.Core.Media;

namespace Nagara.Core.Tests;

public sealed class SpotifyIdentityTests
{
    [Theory]
    [InlineData("Spotify.exe", null)]
    [InlineData("spotify.exe", "Spotify")]
    [InlineData("Spotify", null)]
    [InlineData("SpotifyAB.SpotifyMusic_zpdnekdrzrea0!Spotify", "Spotify")]
    [InlineData("spotifyab.spotifymusic_zpdnekdrzrea0!spotify", null)]
    [InlineData("com.spotify.client", null)]
    [InlineData("C:\\Program Files\\WindowsApps\\SpotifyAB.SpotifyMusic_zpdnekdrzrea0\\Spotify.exe", null)]
    [InlineData(null, "Spotify")]
    [InlineData("", "Spotify Music")]
    [InlineData("UnknownPlayer.exe", "Spotify Web Player")]
    public void RecognisesSpotifyAcrossInstallationChannels(string? appUserModelId, string? displayName)
    {
        Assert.True(SpotifyIdentity.Matches(appUserModelId, displayName));
    }

    [Theory]
    [InlineData("msedge.exe", "Microsoft Edge")]
    [InlineData("chrome.exe", "Google Chrome")]
    [InlineData("firefox.exe", "Mozilla Firefox")]
    [InlineData("notspotify.exe", "Not Spotify")]
    [InlineData("wmplayer.exe", "Windows Media Player")]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("com.example.music", "Music")]
    public void DoesNotMistakeOtherPlayersForSpotify(string? appUserModelId, string? displayName)
    {
        Assert.False(SpotifyIdentity.Matches(appUserModelId, displayName));
    }
}
