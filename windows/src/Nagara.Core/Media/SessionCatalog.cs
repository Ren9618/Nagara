namespace Nagara.Core.Media;

/// <summary>A point-in-time view of GSMTC sessions and the current session.</summary>
public sealed record SessionCatalog(
    IReadOnlyList<MediaSessionSnapshot> Sessions,
    string? CurrentSessionId)
{
    public static SessionCatalog Empty { get; } =
        new(Array.Empty<MediaSessionSnapshot>(), null);
}
