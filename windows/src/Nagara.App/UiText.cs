using Nagara.App.Diagnostics;
using Nagara.App.Localization;
using Nagara.Core.Media;
using Nagara.Core.Routing;
using Nagara.Core.Settings;

namespace Nagara.App;

internal static class UiText
{
    public static string Mode(RoutingMode mode) => mode switch
    {
        RoutingMode.Spotify => Strings.Get("Mode.Spotify"),
        RoutingMode.FixedSession => Strings.Get("Mode.Fixed"),
        _ => Strings.Get("Mode.Automatic"),
    };

    public static string MenuMode(RoutingMode mode) => mode switch
    {
        RoutingMode.Spotify => Strings.Get("Menu.Spotify"),
        RoutingMode.FixedSession => Strings.Get("Menu.Fixed"),
        _ => Strings.Get("Menu.Automatic"),
    };

    public static string Command(MediaKeyCommand command) => command switch
    {
        MediaKeyCommand.PlayPause => Strings.Get("Command.PlayPause"),
        MediaKeyCommand.NextTrack => Strings.Get("Command.NextTrack"),
        _ => Strings.Get("Command.PreviousTrack"),
    };

    public static string CommandWithTime(DiagnosticsSnapshot snapshot)
        => snapshot.LastCommand is null
            ? Strings.Get("Diagnostics.None")
            : $"{Command(snapshot.LastCommand.Value)} ({snapshot.LastCommandAt:HH:mm:ss})";

    public static string Hook(DiagnosticsSnapshot snapshot) => snapshot.HookState switch
    {
        HookState.Active => Strings.Get("Hook.Active"),
        HookState.Failed => Strings.Format(
            "Hook.Failed",
            snapshot.HookError ?? Strings.Get("Common.None")),
        _ => Strings.Get("Hook.Inactive"),
    };

    public static string LastTarget(DiagnosticsSnapshot snapshot)
    {
        if (snapshot.LastOutcome == SendOutcome.NoTarget)
        {
            return Strings.Get("Diagnostics.NoTarget");
        }

        if (string.IsNullOrWhiteSpace(snapshot.LastTargetId))
        {
            return Strings.Get("Diagnostics.None");
        }

        var target = $"{snapshot.LastTargetDisplayName ?? snapshot.LastTargetId} ({snapshot.LastTargetId})";
        return snapshot.LastOutcome == SendOutcome.Failed
            ? Strings.Format("Diagnostics.SendFailed", target)
            : target;
    }

    public static string Result(DiagnosticsSnapshot snapshot) => snapshot.LastOutcome switch
    {
        SendOutcome.Sent => Strings.Get("Diagnostics.ResultSent"),
        SendOutcome.NoTarget => Strings.Get("Diagnostics.ResultNoTarget"),
        SendOutcome.Failed => Strings.Get("Diagnostics.ResultFailed"),
        _ => Strings.Get("Diagnostics.ResultNone"),
    };

    public static string Language(AppLanguage language) => language switch
    {
        AppLanguage.Japanese => Strings.Get("Settings.LanguageJapanese"),
        AppLanguage.English => Strings.Get("Settings.LanguageEnglish"),
        _ => Strings.Get("Settings.LanguageAuto"),
    };

    public static string OnOff(bool value)
        => Strings.Get(value ? "Common.On" : "Common.Off");

    public static string SessionStatus(PlaybackStatus status) => status switch
    {
        PlaybackStatus.Playing => Strings.Get("Playback.Playing"),
        PlaybackStatus.Paused => Strings.Get("Playback.Paused"),
        PlaybackStatus.Stopped => Strings.Get("Playback.Stopped"),
        PlaybackStatus.Closed => Strings.Get("Playback.Closed"),
        _ => Strings.Get("Playback.Unknown"),
    };
}
