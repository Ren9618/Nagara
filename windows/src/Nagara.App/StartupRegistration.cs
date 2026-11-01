using System.IO;
using System.Security;
using Microsoft.Win32;

namespace Nagara.App;

/// <summary>
/// Registers the current executable under HKCU Run. No administrator rights,
/// scheduled tasks, or services are involved.
/// </summary>
internal sealed class StartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string DefaultValueName = "Nagara";

    private readonly string _executablePath;
    private readonly string _valueName;

    public StartupRegistration(string executablePath, string valueName = DefaultValueName)
    {
        _executablePath = executablePath;
        _valueName = valueName;
    }

    public string? CurrentCommand
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
                return key?.GetValue(_valueName) as string;
            }
            catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
            {
                return null;
            }
        }
    }

    /// <summary>Makes the registry match the stored preference; returns an error or null.</summary>
    public string? Sync(bool enabled)
    {
        var current = CurrentCommand;

        if (enabled)
        {
            var expected = BuildCommand(_executablePath);
            return string.Equals(current, expected, StringComparison.OrdinalIgnoreCase)
                ? null
                : SetEnabled(true);
        }

        return string.IsNullOrEmpty(current) ? null : SetEnabled(false);
    }

    public string? SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (key is null)
            {
                return "Could not open the current user's Run registry key.";
            }

            if (enabled)
            {
                key.SetValue(_valueName, BuildCommand(_executablePath), RegistryValueKind.String);
            }
            else
            {
                key.DeleteValue(_valueName, throwOnMissingValue: false);
            }

            return null;
        }
        catch (Exception ex) when (ex is SecurityException
            or UnauthorizedAccessException
            or IOException)
        {
            return ex.Message;
        }
    }

    internal static string BuildCommand(string executablePath) => $"\"{executablePath}\"";
}
