using System.Windows.Markup;

namespace Nagara.App.Localization;

/// <summary>Looks up a .resx string for XAML. Language changes apply on restart.</summary>
[MarkupExtensionReturnType(typeof(string))]
public sealed class LocExtension : MarkupExtension
{
    public LocExtension()
    {
    }

    public LocExtension(string key)
    {
        Key = key;
    }

    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
        => Strings.Get(Key);
}
