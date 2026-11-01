using Nagara.App.Diagnostics;
using Nagara.Core.Settings;

namespace Nagara.App;

/// <summary>Shared dependencies handed to the tray menu and settings window.</summary>
internal sealed class AppServices
{
    public required SettingsStore Settings { get; init; }

    public required GsmtcSessionService Sessions { get; init; }

    public required DiagnosticsState Diagnostics { get; init; }

    public required DiagnosticsLog Log { get; init; }

    public required StartupRegistration Startup { get; init; }

    public required string Version { get; init; }

    public required string ExecutablePath { get; init; }

    public required string SettingsPath { get; init; }

    public required string LogFolderPath { get; init; }

    /// <summary>Starts or stops the hook; returns an error message on failure.</summary>
    public required Func<bool, string?> SetInterception { get; init; }

    public required Action ShowSettings { get; init; }

    public required Action Exit { get; init; }

    /// <summary>Opens the diagnostics folder; returns an error message on failure.</summary>
    public required Func<string?> OpenLogFolder { get; init; }
}
