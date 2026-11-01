using System.Globalization;
using Nagara.Core.Localization;
using Nagara.Core.Settings;

namespace Nagara.Core.Tests;

public sealed class LanguageResolverTests
{
    [Theory]
    [InlineData("ja-JP", UiLanguage.Japanese)]
    [InlineData("ja", UiLanguage.Japanese)]
    [InlineData("en-US", UiLanguage.English)]
    [InlineData("en-GB", UiLanguage.English)]
    [InlineData("fr-FR", UiLanguage.English)]
    public void AutoFollowsTheOsDisplayLanguage(string cultureName, UiLanguage expected)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);

        Assert.Equal(expected, LanguageResolver.Resolve(AppLanguage.Auto, culture));
    }

    [Fact]
    public void AutoFallsBackToEnglishForInvariantCulture()
    {
        Assert.Equal(UiLanguage.English, LanguageResolver.Resolve(AppLanguage.Auto, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ManualJapaneseWinsOverTheOsLanguage()
    {
        Assert.Equal(
            UiLanguage.Japanese,
            LanguageResolver.Resolve(AppLanguage.Japanese, CultureInfo.GetCultureInfo("en-US")));
    }

    [Fact]
    public void ManualEnglishWinsOverTheOsLanguage()
    {
        Assert.Equal(
            UiLanguage.English,
            LanguageResolver.Resolve(AppLanguage.English, CultureInfo.GetCultureInfo("ja-JP")));
    }

    [Fact]
    public void NullCultureIsTreatedAsNonJapanese()
    {
        Assert.Equal(UiLanguage.English, LanguageResolver.Resolve(AppLanguage.Auto, null));
    }
}
