using ScoreBoardVtk.Core.Models;
using ScoreBoardVtk.Core.Services;

namespace ScoreBoardVtk.Tests;

public sealed class SettingsStoreTests
{
    [Fact]
    public void Load_SupportsJsonComments()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var path = Path.Combine(directory, "settings.json");
            File.WriteAllText(path, """
            {
              // Main transport port.
              "selectedPort": "COM7",
              // Active game mode.
              "gameMode": "Volleyball",
              "gameTimePreset": "08:00",
              "overtimeTimePreset": "03:00"
            }
            """);

            var store = new SettingsStore(path);
            var settings = store.Load();

            Assert.Equal("COM7", settings.SelectedPort);
            Assert.Equal(GameMode.Volleyball, settings.GameMode);
            Assert.Equal("08:00", settings.GameTimePreset);
            Assert.Equal("03:00", settings.OvertimeTimePreset);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Save_WritesDocumentedJson()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var path = Path.Combine(directory, "settings.json");
            var store = new SettingsStore(path);
            store.Save(new AppSettings
            {
                SelectedPort = "COM3",
                GameMode = GameMode.Basketball,
                RunningText = "TEST"
            });

            var json = File.ReadAllText(path);

            Assert.Contains("// Serial port used by the scoreboard application.", json);
            Assert.Contains("\"selectedPort\": \"COM3\"", json);
            Assert.Contains("\"overtimeTimePreset\": \"05:00\"", json);
            Assert.Contains("\"runningText\": \"TEST\"", json);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
