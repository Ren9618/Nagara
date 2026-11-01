using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Nagara.App;

internal static class AppPaths
{
    public static string RootDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Nagara");

    public static string SettingsFile { get; } = Path.Combine(RootDirectory, "settings.json");

    public static string LogFile { get; } = Path.Combine(RootDirectory, "diagnostics.log");

    public static string ExecutablePath { get; } =
        Environment.ProcessPath
        ?? Process.GetCurrentProcess().MainModule?.FileName
        ?? AppContext.BaseDirectory;

    public static string Version
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version is null
                ? "0.1.0"
                : $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }
}
