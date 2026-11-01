using Nagara.Core.Media;
using Nagara.Core.Settings;

namespace Nagara.App.UI;

internal sealed class SessionChoice
{
    public SessionChoice(MediaSessionSnapshot session, string label)
    {
        AppUserModelId = session.AppUserModelId;
        DisplayName = session.DisplayName;
        Label = label;
        StatusText = string.Empty;
        IsMissing = false;
    }

    public SessionChoice(string appUserModelId, string displayName, string label)
    {
        AppUserModelId = appUserModelId;
        DisplayName = displayName;
        Label = label;
        StatusText = string.Empty;
        IsMissing = true;
    }

    public string AppUserModelId { get; }

    public string DisplayName { get; }

    public string Label { get; }

    public string StatusText { get; }

    public bool IsMissing { get; }

    public override string ToString() => Label;
}

internal sealed class SessionRow
{
    public required string DisplayName { get; init; }

    public required string AppUserModelId { get; init; }

    public required string StatusText { get; init; }

    public override string ToString() => $"{DisplayName} | {AppUserModelId} | {StatusText}";
}

internal sealed class LanguageChoice
{
    public LanguageChoice(AppLanguage value, string label)
    {
        Value = value;
        Label = label;
    }

    public AppLanguage Value { get; }

    public string Label { get; }

    public override string ToString() => Label;
}
