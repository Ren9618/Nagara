namespace Nagara.Core.Media;

/// <summary>
/// The only virtual-key codes Nagara ever inspects. Keeping this list tiny is
/// part of the privacy design: no other key is recorded, logged, or displayed.
/// </summary>
public static class MediaVirtualKeys
{
    public const int MediaNextTrack = 0xB0;
    public const int MediaPreviousTrack = 0xB1;
    public const int MediaPlayPause = 0xB3;

    public static bool TryMap(int virtualKey, out MediaKeyCommand command)
    {
        switch (virtualKey)
        {
            case MediaPlayPause:
                command = MediaKeyCommand.PlayPause;
                return true;
            case MediaNextTrack:
                command = MediaKeyCommand.NextTrack;
                return true;
            case MediaPreviousTrack:
                command = MediaKeyCommand.PreviousTrack;
                return true;
            default:
                command = default;
                return false;
        }
    }
}
