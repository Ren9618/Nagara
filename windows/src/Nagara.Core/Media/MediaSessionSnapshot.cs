namespace Nagara.Core.Media;

/// <summary>
/// A WinRT-free view of one GSMTC session. The AUMID is the identity; the
/// display name is only a convenience for menus and diagnostics.
/// </summary>
public sealed record MediaSessionSnapshot(
    string AppUserModelId,
    string DisplayName,
    PlaybackStatus Status)
{
    public bool IsPlaying => Status == PlaybackStatus.Playing;
}
