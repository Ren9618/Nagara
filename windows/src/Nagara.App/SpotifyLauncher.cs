using System.Diagnostics;
using System.IO;
using System.Security;
using Microsoft.Win32;
using Nagara.App.Diagnostics;

namespace Nagara.App;

/// <summary>
/// Launches Spotify through its registered <c>spotify:</c> protocol. Nagara
/// checks the registration first so a missing Spotify does not open a Windows
/// "how do you want to open this?" dialog.
/// </summary>
internal sealed class SpotifyLauncher
{
    private readonly DiagnosticsLog _log;

    public SpotifyLauncher(DiagnosticsLog log)
    {
        _log = log;
    }

    public bool TryLaunch()
    {
        if (!IsProtocolRegistered())
        {
            _log.Write("INFO", "Spotify protocol is not registered; launch skipped.");
            return false;
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo("spotify:")
            {
                UseShellExecute = true,
            });
            _log.Write("INFO", "Requested Spotify launch through the spotify: protocol.");
            return true;
        }
        catch (Exception ex)
        {
            _log.Write("WARN", $"Spotify launch failed: {ex.Message}");
            return false;
        }
    }

    internal static bool IsProtocolRegistered()
    {
        try
        {
            using var key = Registry.ClassesRoot.OpenSubKey(@"spotify\shell\open\command");
            return key?.GetValue(null) is string command && !string.IsNullOrWhiteSpace(command);
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
        {
            return false;
        }
    }
}
