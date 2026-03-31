using ScoreBoardVtk.Core.Models;
using ScoreBoardVtk.Core.Services;

namespace ScoreBoardVtk.Tests;

public sealed class HostSettingsStoreTests
{
    [Fact]
    public void Load_WhenFileIsMissing_ReturnsDefaultSettings()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        var store = new HostSettingsStore(path);

        var settings = store.Load();

        Assert.Equal(ApplicationProfile.Development, settings.Profile);
        Assert.Equal("settings.json", settings.GameSettingsFileName);
        Assert.True(settings.ShowDebugTab);
        Assert.Equal(50, settings.Runtime.PublishIntervalMilliseconds);
    }

    [Fact]
    public void Load_SupportsJsonComments()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var path = Path.Combine(directory, "hostsettings.json");
            File.WriteAllText(path, """
            {
              // Boot profile.
              "profile": "Mock",
              "gameSettingsFileName": "match.json",
              "showDebugTab": false,
              "runtime": {
                // Publish interval.
                "publishIntervalMilliseconds": 77
              }
            }
            """);

            var store = new HostSettingsStore(path);
            var settings = store.Load();

            Assert.Equal(ApplicationProfile.Mock, settings.Profile);
            Assert.Equal("match.json", settings.GameSettingsFileName);
            Assert.False(settings.ShowDebugTab);
            Assert.Equal(77, settings.Runtime.PublishIntervalMilliseconds);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void SaveAndLoad_RoundTripsHostSettings()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var path = Path.Combine(directory, "hostsettings.json");
            var store = new HostSettingsStore(path);
            var expected = new HostSettings
            {
                Profile = ApplicationProfile.Mock,
                GameSettingsFileName = "game-state.json",
                ShowDebugTab = false,
                Runtime = new HostRuntimeSettings
                {
                    TimingIntervalMilliseconds = 101,
                    PublishIntervalMilliseconds = 505,
                },
            };

            store.Save(expected);
            var actual = store.Load();
            var json = File.ReadAllText(path);

            Assert.Equal(ApplicationProfile.Mock, actual.Profile);
            Assert.Equal("game-state.json", actual.GameSettingsFileName);
            Assert.False(actual.ShowDebugTab);
            Assert.Equal(101, actual.Runtime.TimingIntervalMilliseconds);
            Assert.Equal(505, actual.Runtime.PublishIntervalMilliseconds);
            Assert.Contains("// Boot profile used to select the transport implementation.", json);
            Assert.Contains("\"showDebugTab\": false", json);
            Assert.Contains("\"timingIntervalMilliseconds\": 101", json);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
