namespace Nagara.Core.Media;

/// <summary>
/// The result of examining one key event. <see cref="Swallow"/> means the
/// event must not reach other applications. <see cref="Command"/> is set only
/// for the first KeyDown of a press.
/// </summary>
public readonly record struct MediaKeyDecision(bool Swallow, MediaKeyCommand? Command)
{
    public static MediaKeyDecision PassThrough => default;

    public static MediaKeyDecision SwallowOnly => new(true, null);

    public static MediaKeyDecision Execute(MediaKeyCommand command) => new(true, command);
}
