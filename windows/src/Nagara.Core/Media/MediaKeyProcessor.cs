namespace Nagara.Core.Media;

/// <summary>
/// Turns low-level key events into at most one command per physical press.
/// KeyUp and auto-repeat events are swallowed while interception is enabled,
/// but never produce another command. When interception is disabled every
/// event passes through untouched.
/// </summary>
public sealed class MediaKeyProcessor
{
    private readonly HashSet<int> _pressedKeys = new();

    public MediaKeyDecision Process(
        int virtualKey,
        MediaKeyEventType eventType,
        bool interceptionEnabled)
    {
        if (!MediaVirtualKeys.TryMap(virtualKey, out var command))
        {
            return MediaKeyDecision.PassThrough;
        }

        if (!interceptionEnabled)
        {
            _pressedKeys.Remove(virtualKey);
            return MediaKeyDecision.PassThrough;
        }

        if (eventType == MediaKeyEventType.KeyUp)
        {
            _pressedKeys.Remove(virtualKey);
            return MediaKeyDecision.SwallowOnly;
        }

        // Auto-repeat arrives as another KeyDown without a KeyUp in between.
        if (!_pressedKeys.Add(virtualKey))
        {
            return MediaKeyDecision.SwallowOnly;
        }

        return MediaKeyDecision.Execute(command);
    }

    /// <summary>Forgets held keys, for example when interception is toggled.</summary>
    public void Reset() => _pressedKeys.Clear();
}
