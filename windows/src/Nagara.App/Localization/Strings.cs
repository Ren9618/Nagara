using System.Globalization;
using System.Resources;

namespace Nagara.App.Localization;

/// <summary>
/// Thin wrapper around the neutral (Japanese) .resx and its English satellite.
/// UI code never hard-codes user-facing text.
/// </summary>
internal static class Strings
{
    private static readonly ResourceManager ResourceManager =
        new("Nagara.App.Resources.Strings", typeof(Strings).Assembly);

    public static string Get(string key)
        => ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    public static string Format(string key, params object?[] arguments)
        => string.Format(CultureInfo.CurrentUICulture, Get(key), arguments);
}
