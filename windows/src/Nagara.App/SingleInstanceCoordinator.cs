using System.Threading;

namespace Nagara.App;

/// <summary>
/// Keeps Nagara to one tray instance per Windows session. A second launch
/// signals the first instance to open its settings window instead of
/// installing a second keyboard hook.
/// </summary>
internal sealed class SingleInstanceCoordinator : IDisposable
{
    private readonly Mutex? _mutex;
    private readonly string _eventName;
    private readonly EventWaitHandle? _showSettingsEvent;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Action _onShowSettings;
    private Thread? _listener;
    private bool _disposed;

    private SingleInstanceCoordinator(
        Mutex? mutex,
        string eventName,
        bool isPrimary,
        Action onShowSettings)
    {
        _mutex = mutex;
        _eventName = eventName;
        _onShowSettings = onShowSettings;
        IsPrimary = isPrimary;

        if (isPrimary)
        {
            _showSettingsEvent = new EventWaitHandle(
                initialState: false,
                EventResetMode.AutoReset,
                name: eventName);
            _listener = new Thread(ListenLoop)
            {
                IsBackground = true,
                Name = "Nagara single-instance listener",
            };
            _listener.Start();
        }
    }

    public bool IsPrimary { get; }

    public static SingleInstanceCoordinator Create(
        string mutexName,
        string eventName,
        Action onShowSettings)
    {
        var mutex = new Mutex(initiallyOwned: false, mutexName);
        bool ownsMutex;
        try
        {
            ownsMutex = mutex.WaitOne(0);
        }
        catch (AbandonedMutexException)
        {
            ownsMutex = true;
        }

        if (!ownsMutex)
        {
            mutex.Dispose();
            return new SingleInstanceCoordinator(null, eventName, isPrimary: false, onShowSettings);
        }

        return new SingleInstanceCoordinator(mutex, eventName, isPrimary: true, onShowSettings);
    }

    public void SignalPrimary()
    {
        if (IsPrimary)
        {
            return;
        }

        try
        {
            using var handle = EventWaitHandle.OpenExisting(_eventName);
            handle.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            // The first instance is still starting or already gone.
        }
        catch (UnauthorizedAccessException)
        {
            // Another user's instance owns the name.
        }
    }

    private void ListenLoop()
    {
        var handles = new WaitHandle[] { _showSettingsEvent!, _cancellation.Token.WaitHandle };

        while (!_cancellation.IsCancellationRequested)
        {
            int index;
            try
            {
                index = WaitHandle.WaitAny(handles);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            if (index == 0)
            {
                try
                {
                    _onShowSettings();
                }
                catch (Exception)
                {
                    // A UI callback failure must not kill the listener.
                }
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cancellation.Cancel();

        if (_listener is { IsAlive: true })
        {
            _listener.Join(TimeSpan.FromSeconds(1));
        }

        _showSettingsEvent?.Dispose();
        _cancellation.Dispose();

        if (_mutex is not null)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // The mutex was abandoned or is no longer owned.
            }

            _mutex.Dispose();
        }
    }
}
