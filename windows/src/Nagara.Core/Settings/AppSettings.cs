using Nagara.Core.Routing;

namespace Nagara.Core.Settings;

/// <summary>
/// Everything Nagara persists. This contains no personal information and no
/// media metadata, only routing preferences and a session AUMID.
/// </summary>
public sealed record AppSettings
{
    public RoutingMode Mode { get; init; } = RoutingMode.Automatic;

    public string? FixedSessionId { get; init; }

    public string? FixedSessionDisplayName { get; init; }

    public bool InterceptionEnabled { get; init; } = true;

    public bool LaunchAtLogin { get; init; }

    public AppLanguage Language { get; init; } = AppLanguage.Auto;

    public string? LastSentSessionId { get; init; }

    /// <summary>
    /// Repairs values that can arrive from an older or hand-edited settings
    /// file. A fixed-session mode without an ID falls back to automatic.
    /// </summary>
    public AppSettings Normalize()
    {
        var mode = Enum.IsDefined(Mode) ? Mode : RoutingMode.Automatic;
        var fixedSessionId = Clean(FixedSessionId);
        var fixedSessionDisplayName = Clean(FixedSessionDisplayName);
        var language = Enum.IsDefined(Language) ? Language : AppLanguage.Auto;

        if (mode == RoutingMode.FixedSession && fixedSessionId is null)
        {
            mode = RoutingMode.Automatic;
        }

        if (fixedSessionId is not null && fixedSessionDisplayName is null)
        {
            fixedSessionDisplayName = fixedSessionId;
        }

        return this with
        {
            Mode = mode,
            FixedSessionId = fixedSessionId,
            FixedSessionDisplayName = fixedSessionDisplayName,
            Language = language,
            LastSentSessionId = Clean(LastSentSessionId),
        };
    }

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
