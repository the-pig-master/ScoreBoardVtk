using System.Text.Json;
using System.Text.Json.Serialization;
using ScoreBoardVtk.Core.Models;

namespace ScoreBoardVtk.Core.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _settingsPath;

    public SettingsStore(string? settingsPath = null)
    {
        _settingsPath = settingsPath ?? Path.Combine(AppContext.BaseDirectory, "settings.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return new AppSettings();
        }

        try
        {
            var json = JsonConfigurationText.Normalize(File.ReadAllText(_settingsPath));
            var settings = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions) ?? new AppSettings();
            settings.KeyboardBindings ??= new KeyboardBindingsSettings();
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(_settingsPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = BuildSettingsJson(settings);
        File.WriteAllText(_settingsPath, json);
    }

    private static string BuildSettingsJson(AppSettings settings)
    {
        return $$"""
        {
          // Serial port used by the scoreboard application.
          // Leave empty to select the port manually in the UI.
          "selectedPort": {{ToJson(settings.SelectedPort)}},

          // Main game timer preset.
          // Supported formats: "mm:ss" or "mm:ss.t"
          "gameTimePreset": {{ToJson(settings.GameTimePreset)}},

          // Basketball overtime timer preset.
          // Supported formats: "mm:ss" or "mm:ss.t"
          "overtimeTimePreset": {{ToJson(settings.OvertimeTimePreset)}},

          // Keyboard shortcuts for the Game screen.
          // Leave a value empty to disable that shortcut.
          "keyboardBindings": {
            // Start or stop the main game clock.
            "toggleGameClockKey": {{ToJson(settings.KeyboardBindings.ToggleGameClockKey)}},

            // Start or stop the 24-second shot clock.
            "toggleShotClockKey": {{ToJson(settings.KeyboardBindings.ToggleShotClockKey)}},

            // Add or remove one point from the home team.
            "increaseHomeScoreKey": {{ToJson(settings.KeyboardBindings.IncreaseHomeScoreKey)}},
            "decreaseHomeScoreKey": {{ToJson(settings.KeyboardBindings.DecreaseHomeScoreKey)}},

            // Add or remove one point from the guest team.
            "increaseGuestScoreKey": {{ToJson(settings.KeyboardBindings.IncreaseGuestScoreKey)}},
            "decreaseGuestScoreKey": {{ToJson(settings.KeyboardBindings.DecreaseGuestScoreKey)}},

            // Add or remove one foul from the home team.
            "increaseHomeFoulsKey": {{ToJson(settings.KeyboardBindings.IncreaseHomeFoulsKey)}},
            "decreaseHomeFoulsKey": {{ToJson(settings.KeyboardBindings.DecreaseHomeFoulsKey)}},

            // Add or remove one foul from the guest team.
            "increaseGuestFoulsKey": {{ToJson(settings.KeyboardBindings.IncreaseGuestFoulsKey)}},
            "decreaseGuestFoulsKey": {{ToJson(settings.KeyboardBindings.DecreaseGuestFoulsKey)}}
          },

          // Basketball foul display mode.
          // true  = wrap counter at 5 fouls
          // false = wrap counter at 9 fouls
          "countFoulsToFive": {{ToJson(settings.CountFoulsToFive)}},

          // Main game clock direction.
          // Allowed values: "Down", "Up"
          "timerDirection": {{ToJson(settings.TimerDirection)}},

          // Duration of the main buzzer when a period ends.
          // Unit: whole seconds, allowed range: 0-9
          "mainSignalDurationSeconds": {{ToJson(settings.MainSignalDurationSeconds)}},

          // Font mode used by the legacy display controller.
          // Allowed values: "Font6x8", "Font8x8"
          "fontMode": {{ToJson(settings.FontMode)}},

          // Active sport mode.
          // Allowed values: "Basketball", "Volleyball"
          "gameMode": {{ToJson(settings.GameMode)}},

          // Duration of the shot-clock buzzer.
          // Unit: tenths of a second, typical values: 5-30
          "shotClockSignalDurationTenths": {{ToJson(settings.ShotClockSignalDurationTenths)}},

          // Automatically start the shot clock after setting 24/14.
          "autoStartShotClock": {{ToJson(settings.AutoStartShotClock)}},

          // Enable or disable the running text area on the display.
          "runningTextEnabled": {{ToJson(settings.RunningTextEnabled)}},

          // Text shown in the running text area.
          // The legacy display supports a short message.
          "runningText": {{ToJson(settings.RunningText)}}
        }
        """;
    }

    private static string ToJson<T>(T value)
    {
        return JsonSerializer.Serialize(value, SerializerOptions);
    }
}
