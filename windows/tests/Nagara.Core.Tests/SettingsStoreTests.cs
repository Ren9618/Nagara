using System.Text.Json;
using Nagara.Core.Routing;
using Nagara.Core.Settings;

namespace Nagara.Core.Tests;

public sealed class SettingsStoreTests
{
    [Fact]
    public void MissingFileLoadsDefaults()
    {
        using var temp = new TempDirectory();
        var store = new SettingsStore(Path.Combine(temp.Path, "settings.json"));

        Assert.Equal(RoutingMode.Automatic, store.Current.Mode);
        Assert.True(store.Current.InterceptionEnabled);
        Assert.Null(store.LastLoadError);
    }

    [Fact]
    public void RoundTripsEveryPersistedValue()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "nested", "settings.json");
        var store = new SettingsStore(path);

        store.Save(new AppSettings
        {
            Mode = RoutingMode.FixedSession,
            FixedSessionId = "msedge.exe",
            FixedSessionDisplayName = "Microsoft Edge",
            InterceptionEnabled = false,
            LaunchAtLogin = true,
            Language = AppLanguage.English,
            LastSentSessionId = "Spotify.exe",
        });

        var reloaded = new SettingsStore(path);

        Assert.Equal(RoutingMode.FixedSession, reloaded.Current.Mode);
        Assert.Equal("msedge.exe", reloaded.Current.FixedSessionId);
        Assert.Equal("Microsoft Edge", reloaded.Current.FixedSessionDisplayName);
        Assert.False(reloaded.Current.InterceptionEnabled);
        Assert.True(reloaded.Current.LaunchAtLogin);
        Assert.Equal(AppLanguage.English, reloaded.Current.Language);
        Assert.Equal("Spotify.exe", reloaded.Current.LastSentSessionId);
    }

    [Fact]
    public void WritesReadableCamelCaseJsonWithStringEnums()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "settings.json");
        var store = new SettingsStore(path);

        store.Save(new AppSettings
        {
            Mode = RoutingMode.FixedSession,
            FixedSessionId = "msedge.exe",
            Language = AppLanguage.English,
        });

        var json = File.ReadAllText(path);

        Assert.Contains("\"mode\": \"fixedSession\"", json);
        Assert.Contains("\"language\": \"english\"", json);
        Assert.Contains("\"fixedSessionId\": \"msedge.exe\"", json);
        Assert.DoesNotContain("\"Mode\"", json);
    }

    [Fact]
    public void InvalidJsonFallsBackToDefaultsAndReportsTheError()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "settings.json");
        File.WriteAllText(path, "{ this is not json");

        var store = new SettingsStore(path);

        Assert.Equal(RoutingMode.Automatic, store.Current.Mode);
        Assert.NotNull(store.LastLoadError);
    }

    [Fact]
    public void UnknownEnumTextFallsBackToDefaults()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "settings.json");
        File.WriteAllText(path, """{"mode":"somethingNew","interceptionEnabled":false}""");

        var store = new SettingsStore(path);

        Assert.Equal(RoutingMode.Automatic, store.Current.Mode);
        Assert.True(store.Current.InterceptionEnabled);
        Assert.NotNull(store.LastLoadError);
    }

    [Fact]
    public void FixedSessionModeWithoutAnIdIsRepairedOnLoad()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "settings.json");
        File.WriteAllText(path, """{"mode":"fixedSession"}""");

        var store = new SettingsStore(path);

        Assert.Equal(RoutingMode.Automatic, store.Current.Mode);
    }

    [Fact]
    public void UpdateAppliesTheMutationAndRaisesChanged()
    {
        using var temp = new TempDirectory();
        var store = new SettingsStore(Path.Combine(temp.Path, "settings.json"));
        AppSettings? observed = null;
        store.Changed += (_, settings) => observed = settings;

        store.Update(current => current with { LastSentSessionId = "  Spotify.exe  " });

        Assert.Equal("Spotify.exe", store.Current.LastSentSessionId);
        Assert.NotNull(observed);
        Assert.Equal("Spotify.exe", observed!.LastSentSessionId);
    }

    [Fact]
    public void SaveLeavesNoTemporaryFileBehind()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "settings.json");
        var store = new SettingsStore(path);

        store.Save(new AppSettings());

        Assert.True(File.Exists(path));
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void ReloadPicksUpExternalChanges()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "settings.json");
        var store = new SettingsStore(path);
        File.WriteAllText(path, JsonSerializer.Serialize(new AppSettings
        {
            Mode = RoutingMode.Spotify,
            InterceptionEnabled = false,
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        }));

        var reloaded = store.Reload();

        Assert.Equal(RoutingMode.Spotify, reloaded.Mode);
        Assert.False(reloaded.InterceptionEnabled);
        Assert.False(store.Current.InterceptionEnabled);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "nagara-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup.
            }
        }
    }
}
