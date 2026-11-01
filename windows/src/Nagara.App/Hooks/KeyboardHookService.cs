using System.ComponentModel;
using System.Runtime.InteropServices;
using Nagara.Core.Media;

namespace Nagara.App.Hooks;

/// <summary>
/// Installs WH_KEYBOARD_LL only while interception is enabled. The callback
/// checks the three supported virtual keys and nothing else; all other input
/// is passed straight to Windows without inspection or logging.
/// </summary>
internal sealed class KeyboardHookService : IDisposable
{
    private readonly MediaKeyProcessor _processor;
    private readonly Action<MediaKeyCommand> _onCommand;
    private readonly NativeMethods.LowLevelKeyboardProc _hookProcedure;
    private IntPtr _hook;
    private bool _disposed;

    public KeyboardHookService(MediaKeyProcessor processor, Action<MediaKeyCommand> onCommand)
    {
        _processor = processor;
        _onCommand = onCommand;
        _hookProcedure = HookCallback;
    }

    public bool IsRunning => _hook != IntPtr.Zero;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_hook != IntPtr.Zero)
        {
            return;
        }

        _processor.Reset();
        var module = NativeMethods.GetModuleHandle(null);
        _hook = NativeMethods.SetWindowsHookEx(
            NativeMethods.WhKeyboardLl,
            _hookProcedure,
            module,
            dwThreadId: 0);

        if (_hook == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public void Stop()
    {
        if (_hook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }

        _processor.Reset();
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            try
            {
                var message = (int)wParam;
                MediaKeyEventType? eventType = message switch
                {
                    NativeMethods.WmKeyDown or NativeMethods.WmSysKeyDown => MediaKeyEventType.KeyDown,
                    NativeMethods.WmKeyUp or NativeMethods.WmSysKeyUp => MediaKeyEventType.KeyUp,
                    _ => null,
                };

                if (eventType is not null)
                {
                    var data = Marshal.PtrToStructure<NativeMethods.KbdLlHookStruct>(lParam);
                    var decision = _processor.Process(
                        (int)data.VirtualKeyCode,
                        eventType.Value,
                        interceptionEnabled: true);

                    if (decision.Command is { } command)
                    {
                        _onCommand(command);
                    }

                    if (decision.Swallow)
                    {
                        return new IntPtr(1);
                    }
                }
            }
            catch (Exception)
            {
                // Never let an exception escape an unmanaged callback.
            }
        }

        return NativeMethods.CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
    }
}
