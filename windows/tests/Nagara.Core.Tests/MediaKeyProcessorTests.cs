using Nagara.Core.Media;

namespace Nagara.Core.Tests;

public sealed class MediaKeyProcessorTests
{
    private readonly MediaKeyProcessor _processor = new();

    [Theory]
    [InlineData(0xB3, MediaKeyCommand.PlayPause)]
    [InlineData(0xB0, MediaKeyCommand.NextTrack)]
    [InlineData(0xB1, MediaKeyCommand.PreviousTrack)]
    public void MapsTheThreeSupportedMediaKeys(int virtualKey, MediaKeyCommand expected)
    {
        var decision = _processor.Process(virtualKey, MediaKeyEventType.KeyDown, interceptionEnabled: true);

        Assert.True(decision.Swallow);
        Assert.Equal(expected, decision.Command);
    }

    [Theory]
    [InlineData(0x41)]
    [InlineData(0x20)]
    [InlineData(0x7B)]
    [InlineData(0xAF)]
    public void PassesOtherKeysThroughWithoutInspectingThem(int virtualKey)
    {
        var down = _processor.Process(virtualKey, MediaKeyEventType.KeyDown, interceptionEnabled: true);
        var up = _processor.Process(virtualKey, MediaKeyEventType.KeyUp, interceptionEnabled: true);

        Assert.False(down.Swallow);
        Assert.Null(down.Command);
        Assert.False(up.Swallow);
        Assert.Null(up.Command);
    }

    [Fact]
    public void PassesEverythingThroughWhenInterceptionIsOff()
    {
        var down = _processor.Process(0xB3, MediaKeyEventType.KeyDown, interceptionEnabled: false);
        var repeat = _processor.Process(0xB3, MediaKeyEventType.KeyDown, interceptionEnabled: false);
        var up = _processor.Process(0xB3, MediaKeyEventType.KeyUp, interceptionEnabled: false);

        Assert.False(down.Swallow);
        Assert.Null(down.Command);
        Assert.False(repeat.Swallow);
        Assert.Null(repeat.Command);
        Assert.False(up.Swallow);
        Assert.Null(up.Command);
    }

    [Fact]
    public void ExecutesExactlyOnceOnKeyDown()
    {
        var decision = _processor.Process(0xB3, MediaKeyEventType.KeyDown, interceptionEnabled: true);

        Assert.True(decision.Swallow);
        Assert.Equal(MediaKeyCommand.PlayPause, decision.Command);
    }

    [Fact]
    public void DoesNotExecuteOnKeyUp()
    {
        _processor.Process(0xB3, MediaKeyEventType.KeyDown, interceptionEnabled: true);

        var up = _processor.Process(0xB3, MediaKeyEventType.KeyUp, interceptionEnabled: true);

        Assert.True(up.Swallow);
        Assert.Null(up.Command);
    }

    [Fact]
    public void DoesNotExecuteAgainOnAutoRepeat()
    {
        var first = _processor.Process(0xB0, MediaKeyEventType.KeyDown, interceptionEnabled: true);
        var repeat = _processor.Process(0xB0, MediaKeyEventType.KeyDown, interceptionEnabled: true);
        var anotherRepeat = _processor.Process(0xB0, MediaKeyEventType.KeyDown, interceptionEnabled: true);

        Assert.Equal(MediaKeyCommand.NextTrack, first.Command);
        Assert.True(repeat.Swallow);
        Assert.Null(repeat.Command);
        Assert.True(anotherRepeat.Swallow);
        Assert.Null(anotherRepeat.Command);
    }

    [Fact]
    public void ExecutesAgainAfterACompletePress()
    {
        _processor.Process(0xB1, MediaKeyEventType.KeyDown, interceptionEnabled: true);
        _processor.Process(0xB1, MediaKeyEventType.KeyUp, interceptionEnabled: true);

        var second = _processor.Process(0xB1, MediaKeyEventType.KeyDown, interceptionEnabled: true);

        Assert.Equal(MediaKeyCommand.PreviousTrack, second.Command);
    }

    [Fact]
    public void SwallowsKeyUpEvenWithoutAMatchingKeyDown()
    {
        var up = _processor.Process(0xB3, MediaKeyEventType.KeyUp, interceptionEnabled: true);

        Assert.True(up.Swallow);
        Assert.Null(up.Command);
    }

    [Fact]
    public void ResetForgetsHeldKeys()
    {
        _processor.Process(0xB3, MediaKeyEventType.KeyDown, interceptionEnabled: true);

        _processor.Reset();
        var next = _processor.Process(0xB3, MediaKeyEventType.KeyDown, interceptionEnabled: true);

        Assert.Equal(MediaKeyCommand.PlayPause, next.Command);
    }

    [Fact]
    public void DisablingAndReenablingDoesNotLeaveAStalePress()
    {
        _processor.Process(0xB3, MediaKeyEventType.KeyDown, interceptionEnabled: true);
        _processor.Process(0xB3, MediaKeyEventType.KeyUp, interceptionEnabled: false);

        var next = _processor.Process(0xB3, MediaKeyEventType.KeyDown, interceptionEnabled: true);

        Assert.Equal(MediaKeyCommand.PlayPause, next.Command);
    }
}
