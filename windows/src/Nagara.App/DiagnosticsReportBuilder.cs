using System.Text;
using Nagara.App.Diagnostics;
using Nagara.App.Localization;
using Nagara.Core.Media;

namespace Nagara.App;

internal static class DiagnosticsReportBuilder
{
    public static string Build(AppServices services, SessionCatalog? catalog)
    {
        var settings = services.Settings.Current;
        var diagnostics = services.Diagnostics.Snapshot();
        var text = new StringBuilder();

        text.AppendLine(Strings.Get("Diagnostics.ReportTitle"));
        text.AppendLine($"{Strings.Get("Diagnostics.ReportGenerated")}: {DateTimeOffset.Now:O}");
        text.AppendLine($"{Strings.Get("Settings.Version")}: {services.Version}");
        text.AppendLine($"{Strings.Get("Diagnostics.ReportOS")}: {Environment.OSVersion.VersionString}");
        text.AppendLine($"{Strings.Get("Settings.ExecutablePath")}: {services.ExecutablePath}");
        text.AppendLine($"{Strings.Get("Settings.SettingsPath")}: {services.SettingsPath}");
        text.AppendLine($"{Strings.Get("Settings.HookStatus")}: {UiText.Hook(diagnostics)}");
        text.AppendLine($"{Strings.Get("Diagnostics.ReportInterception")}: {UiText.OnOff(settings.InterceptionEnabled)}");
        text.AppendLine($"{Strings.Get("Diagnostics.ReportMode")}: {UiText.Mode(settings.Mode)}");
        text.AppendLine($"{Strings.Get("Diagnostics.ReportFixedSession")}: {FormatSavedFixedSession(settings)}");
        text.AppendLine($"{Strings.Get("Diagnostics.ReportLanguage")}: {UiText.Language(settings.Language)}");
        text.AppendLine($"{Strings.Get("Settings.LastKey")}: {UiText.CommandWithTime(diagnostics)}");
        text.AppendLine($"{Strings.Get("Settings.LastTarget")}: {UiText.LastTarget(diagnostics)}");
        text.AppendLine($"{Strings.Get("Diagnostics.ReportResult")}: {UiText.Result(diagnostics)}");

        if (!string.IsNullOrWhiteSpace(diagnostics.LastError))
        {
            text.AppendLine($"{Strings.Get("Diagnostics.ReportLastError")}: {diagnostics.LastError}");
        }

        text.AppendLine($"{Strings.Get("Settings.Sessions")}:");
        if (catalog is null || catalog.Sessions.Count == 0)
        {
            text.AppendLine($"- {Strings.Get("Diagnostics.ReportNoSessions")}");
        }
        else
        {
            foreach (var session in catalog.Sessions)
            {
                var currentMarker = string.Equals(
                    session.AppUserModelId,
                    catalog.CurrentSessionId,
                    StringComparison.OrdinalIgnoreCase)
                    ? " " + Strings.Get("Diagnostics.ReportCurrent")
                    : string.Empty;
                text.AppendLine(
                    $"- {session.DisplayName} | {session.AppUserModelId} | {UiText.SessionStatus(session.Status)}{currentMarker}");
            }
        }

        return text.ToString();
    }

    private static string FormatSavedFixedSession(Nagara.Core.Settings.AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.FixedSessionId))
        {
            return Strings.Get("Diagnostics.None");
        }

        return $"{settings.FixedSessionDisplayName ?? settings.FixedSessionId} ({settings.FixedSessionId})";
    }
}
