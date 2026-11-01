using Nagara.Core.Media;

namespace Nagara.App.Diagnostics;

internal enum HookState
{
    Inactive,
    Active,
    Failed,
}

internal enum SendOutcome
{
    None,
    Sent,
    NoTarget,
    Failed,
}

internal sealed record DiagnosticsSnapshot(
    HookState HookState,
    string? HookError,
    MediaKeyCommand? LastCommand,
    DateTimeOffset? LastCommandAt,
    string? LastTargetId,
    string? LastTargetDisplayName,
    SendOutcome LastOutcome,
    string? LastError,
    DateTimeOffset? LastErrorAt);

/// <summary>
/// Thread-safe in-memory state for the diagnostics pane. Only the three media
/// keys, target AUMIDs, and errors are stored.
/// </summary>
internal sealed class DiagnosticsState
{
    private readonly object _gate = new();

    private HookState _hookState = HookState.Inactive;
    private string? _hookError;
    private MediaKeyCommand? _lastCommand;
    private DateTimeOffset? _lastCommandAt;
    private string? _lastTargetId;
    private string? _lastTargetDisplayName;
    private SendOutcome _lastOutcome = SendOutcome.None;
    private string? _lastError;
    private DateTimeOffset? _lastErrorAt;

    public event EventHandler? Changed;

    public DiagnosticsSnapshot Snapshot()
    {
        lock (_gate)
        {
            return new DiagnosticsSnapshot(
                _hookState,
                _hookError,
                _lastCommand,
                _lastCommandAt,
                _lastTargetId,
                _lastTargetDisplayName,
                _lastOutcome,
                _lastError,
                _lastErrorAt);
        }
    }

    public void SetHookState(HookState state, string? error = null)
    {
        lock (_gate)
        {
            _hookState = state;
            _hookError = error;
        }

        RaiseChanged();
    }

    public void RecordKeyPressed(MediaKeyCommand command)
    {
        lock (_gate)
        {
            _lastCommand = command;
            _lastCommandAt = DateTimeOffset.Now;
            _lastTargetId = null;
            _lastTargetDisplayName = null;
            _lastOutcome = SendOutcome.None;
        }

        RaiseChanged();
    }

    public void RecordSent(MediaSessionSnapshot target)
    {
        lock (_gate)
        {
            _lastTargetId = target.AppUserModelId;
            _lastTargetDisplayName = target.DisplayName;
            _lastOutcome = SendOutcome.Sent;
        }

        RaiseChanged();
    }

    public void RecordNoTarget()
    {
        lock (_gate)
        {
            _lastTargetId = null;
            _lastTargetDisplayName = null;
            _lastOutcome = SendOutcome.NoTarget;
        }

        RaiseChanged();
    }

    public void RecordSendFailed(MediaSessionSnapshot target, string? error)
    {
        lock (_gate)
        {
            _lastTargetId = target.AppUserModelId;
            _lastTargetDisplayName = target.DisplayName;
            _lastOutcome = SendOutcome.Failed;
            if (!string.IsNullOrWhiteSpace(error))
            {
                _lastError = error;
                _lastErrorAt = DateTimeOffset.Now;
            }
        }

        RaiseChanged();
    }

    public void RecordError(string message)
    {
        lock (_gate)
        {
            _lastError = message;
            _lastErrorAt = DateTimeOffset.Now;
        }

        RaiseChanged();
    }

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
