using System.Globalization;
using Nagara.Core.Settings;

namespace Nagara.Core.Localization;

public enum UiLanguage
{
    Japanese,
    English,
}

/// <summary>
/// Resolves the two supported UI languages. "Auto" follows the OS display
/// language: Japanese for Japanese Windows, English for everything else.
/// </summary>
public static class LanguageResolver
{
    public static UiLanguage Resolve(AppLanguage setting, CultureInfo? osUiCulture)
    {
        return setting switch
        {
            AppLanguage.Japanese => UiLanguage.Japanese,
            AppLanguage.English => UiLanguage.English,
            _ => IsJapanese(osUiCulture) ? UiLanguage.Japanese : UiLanguage.English,
        };
    }

    private static bool IsJapanese(CultureInfo? culture)
    {
        for (var current = culture; current is not null && !string.IsNullOrEmpty(current.Name); current = current.Parent)
        {
            if (string.Equals(current.TwoLetterISOLanguageName, "ja", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
