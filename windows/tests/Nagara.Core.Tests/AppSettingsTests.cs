using Nagara.Core.Routing;
using Nagara.Core.Settings;

namespace Nagara.Core.Tests;

public sealed class AppSettingsTests
{
    [Fact]
    public void DefaultsAreSafeAndUnobtrusive()
    {
        var settings = new AppSettings().Normalize();

        Assert.Equal(RoutingMode.Automatic, settings.Mode);
        Assert.True(settings.InterceptionEnabled);
        Assert.False(settings.LaunchAtLogin);
        Assert.Equal(AppLanguage.Auto, settings.Language);
        Assert.Null(settings.FixedSessionId);
        Assert.Null(settings.LastSentSessionId);
    }

    [Fact]
    public void NormalizeTrimsPersistedIds()
    {
        var settings = new AppSettings
        {
            Mode = RoutingMode.FixedSession,
            FixedSessionId = "  msedge.exe  ",
            FixedSessionDisplayName = "  Microsoft Edge  ",
            LastSentSessionId = "  Spotify.exe ",
        }.Normalize();

        Assert.Equal("msedge.exe", settings.FixedSessionId);
        Assert.Equal("Microsoft Edge", settings.FixedSessionDisplayName);
        Assert.Equal("Spotify.exe", settings.LastSentSessionId);
    }

    [Fact]
    public void FixedModeWithoutAnIdFallsBackToAutomatic()
    {
        var settings = new AppSettings
        {
            Mode = RoutingMode.FixedSession,
            FixedSessionId = "   ",
        }.Normalize();

        Assert.Equal(RoutingMode.Automatic, settings.Mode);
    }

    [Fact]
    public void FixedModeWithAnIdButNoNameUsesTheIdAsTheName()
    {
        var settings = new AppSettings
        {
            Mode = RoutingMode.FixedSession,
            FixedSessionId = "Spotify.exe",
        }.Normalize();

        Assert.Equal("Spotify.exe", settings.FixedSessionDisplayName);
    }

    [Fact]
    public void AutomaticModeKeepsTheRememberedFixedSession()
    {
        var settings = new AppSettings
        {
            Mode = RoutingMode.Automatic,
            FixedSessionId = "msedge.exe",
            FixedSessionDisplayName = "Microsoft Edge",
        }.Normalize();

        Assert.Equal("msedge.exe", settings.FixedSessionId);
        Assert.Equal("Microsoft Edge", settings.FixedSessionDisplayName);
    }

    [Fact]
    public void InvalidEnumValuesAreRepaired()
    {
        var settings = new AppSettings
        {
            Mode = (RoutingMode)99,
            Language = (AppLanguage)99,
        }.Normalize();

        Assert.Equal(RoutingMode.Automatic, settings.Mode);
        Assert.Equal(AppLanguage.Auto, settings.Language);
    }
}
