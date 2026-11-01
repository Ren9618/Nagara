using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nagara.Core.Settings;

/// <summary>
/// Loads and atomically writes <c>settings.json</c>. The store is safe to use
/// from both the UI thread and the media-key worker; reads always return an
/// immutable snapshot.
/// </summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly object _gate = new();
    private AppSettings _current;
    private string? _lastLoadError;

    public SettingsStore(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("A settings file path is required.", nameof(filePath));
        }

        FilePath = Path.GetFullPath(filePath);
        lock (_gate)
        {
            _current = LoadFromDisk();
        }
    }

    public string FilePath { get; }

    public AppSettings Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    public string? LastLoadError
    {
        get
        {
            lock (_gate)
            {
                return _lastLoadError;
            }
        }
    }

    public event EventHandler<AppSettings>? Changed;

    /// <summary>Re-reads the file and replaces the in-memory snapshot.</summary>
    public AppSettings Reload()
    {
        AppSettings loaded;
        lock (_gate)
        {
            loaded = LoadFromDisk();
            _current = loaded;
        }

        Changed?.Invoke(this, loaded);
        return loaded;
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Update(_ => settings);
    }

    public void Update(Func<AppSettings, AppSettings> mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);

        AppSettings updated;
        lock (_gate)
        {
            updated = mutate(_current).Normalize();
            Persist(updated);
            _current = updated;
            _lastLoadError = null;
        }

        Changed?.Invoke(this, updated);
    }

    private AppSettings LoadFromDisk()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                _lastLoadError = null;
                return new AppSettings();
            }

            var json = File.ReadAllText(FilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                _lastLoadError = "The settings file is empty.";
                return new AppSettings();
            }

            var settings = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions);
            if (settings is null)
            {
                _lastLoadError = "The settings file did not contain a settings object.";
                return new AppSettings();
            }

            _lastLoadError = null;
            return settings.Normalize();
        }
        catch (Exception ex) when (ex is IOException
            or UnauthorizedAccessException
            or JsonException
            or NotSupportedException)
        {
            _lastLoadError = ex.Message;
            return new AppSettings();
        }
    }

    private void Persist(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, SerializerOptions);
        var temporaryPath = FilePath + ".tmp";

        try
        {
            File.WriteAllText(temporaryPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temporaryPath, FilePath, overwrite: true);
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch (IOException)
            {
                // A stale temp file is harmless; the next save overwrites it.
            }
        }
    }
}
