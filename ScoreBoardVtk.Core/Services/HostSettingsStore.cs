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
            // Shared timing loop interval for game clock, shot clock, and buzzer timing.
            "timingIntervalMilliseconds": {{ToJson(settings.Runtime.TimingIntervalMilliseconds)}},

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
