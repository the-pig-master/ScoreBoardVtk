using System.Text.Json;
using System.Text.Json.Serialization;
using ScoreBoardVtk.Core.Models;

namespace ScoreBoardVtk.Core.Services;

public sealed class HostSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public HostSettingsStore(string? settingsPath = null)
    {
        FilePath = settingsPath ?? Path.Combine(AppContext.BaseDirectory, "hostsettings.json");
    }

    public string FilePath { get; }

    public HostSettings Load()
    {
        if (!File.Exists(FilePath))
        {
            return new HostSettings();
        }

        try
        {
            var json = JsonConfigurationText.Normalize(File.ReadAllText(FilePath));
            return JsonSerializer.Deserialize<HostSettings>(json, SerializerOptions) ?? new HostSettings();
        }
        catch
        {
            return new HostSettings();
        }
    }

    public void Save(HostSettings settings)
    {
        var directory = Path.GetDirectoryName(FilePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = BuildHostSettingsJson(settings);
        File.WriteAllText(FilePath, json);
    }

    private static string BuildHostSettingsJson(HostSettings settings)
    {
        return $$"""
        {
          // Boot profile used to select the transport implementation.
          // "Hardware"    = only real serial ports
          // "Mock"        = only the built-in MOCK port
          // "Development" = real serial ports + MOCK port
          "profile": {{ToJson(settings.Profile)}},

          // File name for persisted game settings.
          // This file is resolved relative to hostsettings.json.
          "gameSettingsFileName": {{ToJson(settings.GameSettingsFileName)}},

          "runtime": {
            // Main game clock tick interval in milliseconds.
            "mainClockIntervalMilliseconds": {{ToJson(settings.Runtime.MainClockIntervalMilliseconds)}},

            // Main buzzer countdown tick interval in milliseconds.
            "mainSignalIntervalMilliseconds": {{ToJson(settings.Runtime.MainSignalIntervalMilliseconds)}},

            // Shot-clock buzzer countdown tick interval in milliseconds.
            "shotClockSignalIntervalMilliseconds": {{ToJson(settings.Runtime.ShotClockSignalIntervalMilliseconds)}},

            // UI/display refresh interval in milliseconds.
            "displayRefreshIntervalMilliseconds": {{ToJson(settings.Runtime.DisplayRefreshIntervalMilliseconds)}},

            // Packet publish interval in milliseconds.
            "publishIntervalMilliseconds": {{ToJson(settings.Runtime.PublishIntervalMilliseconds)}}
          }
        }
        """;
    }

    private static string ToJson<T>(T value)
    {
        return JsonSerializer.Serialize(value, SerializerOptions);
    }
}
