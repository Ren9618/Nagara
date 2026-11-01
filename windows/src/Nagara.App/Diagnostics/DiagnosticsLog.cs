using System.IO;
using System.Text;

namespace Nagara.App.Diagnostics;

/// <summary>
/// A deliberately narrow diagnostics log. Callers must only pass media key
/// names, session AUMIDs, and errors; ordinary keystrokes and media metadata
/// are never recorded.
/// </summary>
internal sealed class DiagnosticsLog
{
    private const long MaxBytes = 256 * 1024;
    private readonly object _gate = new();

    public DiagnosticsLog(string filePath)
    {
        FilePath = filePath;
    }

    public string FilePath { get; }

    public void Write(string level, string message)
    {
        lock (_gate)
        {
            try
            {
                var directory = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (File.Exists(FilePath) && new FileInfo(FilePath).Length > MaxBytes)
                {
                    File.Move(FilePath, FilePath + ".1", overwrite: true);
                }

                var line = string.Format(
                    "{0:O} [{1}] {2}{3}",
                    DateTimeOffset.Now,
                    level,
                    Sanitize(message),
                    Environment.NewLine);
                File.AppendAllText(FilePath, line, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }
            catch (Exception)
            {
                // Diagnostics must never take the app down.
            }
        }
    }

    private static string Sanitize(string message)
        => message.Replace('\r', ' ').Replace('\n', ' ');
}
