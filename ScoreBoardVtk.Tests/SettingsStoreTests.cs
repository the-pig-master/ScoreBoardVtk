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
              "uiLanguage": "Russian",
              "gameTimePreset": "08:00",
              "overtimeTimePreset": "03:00",
              "useMainSignalFieldForShotClockSignal": false,
              "keyboardBindings": {
                "toggleGameClockKey": "Space",
                "setShotClock24Key": "D1",
                "runShotClock14Key": "D2",
                "increaseHomeScoreKey": "F1"
              }
            }
            """);

            var store = new SettingsStore(path);
            var settings = store.Load();

            Assert.Equal("COM7", settings.SelectedPort);
            Assert.Equal(GameMode.Volleyball, settings.GameMode);
            Assert.Equal(UiLanguage.Russian, settings.UiLanguage);
            Assert.Equal("08:00", settings.GameTimePreset);
            Assert.Equal("03:00", settings.OvertimeTimePreset);
            Assert.False(settings.UseMainSignalFieldForShotClockSignal);
            Assert.Equal("Space", settings.KeyboardBindings.ToggleGameClockKey);
            Assert.Equal("D1", settings.KeyboardBindings.SetShotClock24Key);
            Assert.Equal("D2", settings.KeyboardBindings.RunShotClock14Key);
            Assert.Equal("F1", settings.KeyboardBindings.IncreaseHomeScoreKey);
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
                UiLanguage = UiLanguage.Russian,
                GameMode = GameMode.Basketball,
                RunningText = "TEST"
            });

            var json = File.ReadAllText(path);

            Assert.Contains("// Serial port used by the scoreboard application.", json);
            Assert.Contains("\"selectedPort\": \"COM3\"", json);
            Assert.Contains("\"uiLanguage\": \"Russian\"", json);
            Assert.Contains("\"overtimeTimePreset\": \"05:00\"", json);
            Assert.Contains("\"useMainSignalFieldForShotClockSignal\": true", json);
            Assert.Contains("\"keyboardBindings\": {", json);
            Assert.Contains("\"runShotClock24Key\": \"\"", json);
            Assert.Contains("\"runningText\": \"TEST\"", json);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
