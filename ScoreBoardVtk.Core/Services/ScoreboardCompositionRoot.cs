using ScoreBoardVtk.Core.Models;

namespace ScoreBoardVtk.Core.Services;

public static class ScoreboardCompositionRoot
{
    public static ScoreboardApplicationServices CreateDesktopServices(
        string? hostSettingsPath = null,
        ApplicationProfile? profileOverride = null)
    {
        var hostSettingsStore = new HostSettingsStore(hostSettingsPath);
        var hostSettings = hostSettingsStore.Load();

        if (!File.Exists(hostSettingsStore.FilePath))
        {
            hostSettingsStore.Save(hostSettings);
        }

        var profile = profileOverride ?? hostSettings.Profile;
        var settingsStore = new SettingsStore(ResolveGameSettingsPath(hostSettingsStore, hostSettings));
        var transport = CreateTransport(profile);
        var scoreboard = new ScoreboardApi(settingsStore.Load(), transport);
        var runtime = new ScoreboardRuntime(scoreboard, hostSettings.Runtime.ToRuntimeOptions());

        return new ScoreboardApplicationServices(hostSettings, settingsStore, scoreboard, runtime);
    }

    public static IScoreboardApi CreateConsoleApi(
        string? hostSettingsPath = null,
        ApplicationProfile? profileOverride = null)
    {
        var hostSettingsStore = new HostSettingsStore(hostSettingsPath);
        var hostSettings = hostSettingsStore.Load();

        if (!File.Exists(hostSettingsStore.FilePath))
        {
            hostSettingsStore.Save(hostSettings);
        }

        var profile = profileOverride ?? hostSettings.Profile;
        return new ScoreboardApi(new AppSettings(), CreateTransport(profile));
    }

    public static IScoreboardApi CreateProbeApi(
        string? hostSettingsPath = null,
        ApplicationProfile? profileOverride = null)
    {
        var hostSettingsStore = new HostSettingsStore(hostSettingsPath);
        var hostSettings = hostSettingsStore.Load();

        if (!File.Exists(hostSettingsStore.FilePath))
        {
            hostSettingsStore.Save(hostSettings);
        }

        var profile = profileOverride ?? hostSettings.Profile;

        return new ScoreboardApi(new AppSettings
        {
            GameMode = GameMode.Basketball,
            TimerDirection = TimerDirection.Down,
            GameTimePreset = "10:00",
            MainSignalDurationSeconds = 0,
            FontMode = FontMode.Font6x8,
            CountFoulsToFive = true,
            RunningTextEnabled = false,
            RunningText = string.Empty,
        }, CreateTransport(profile));
    }

    public static ISerialTransport CreateTransport(ApplicationProfile profile)
    {
        return profile switch
        {
            ApplicationProfile.Hardware => new SerialTransport(),
            ApplicationProfile.Mock => new MockSerialTransport(),
            ApplicationProfile.Development => new CompositeSerialTransport(
                new SerialTransport(),
                new MockSerialTransport()),
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unsupported application profile."),
        };
    }

    private static string ResolveGameSettingsPath(HostSettingsStore hostSettingsStore, HostSettings hostSettings)
    {
        var fileName = string.IsNullOrWhiteSpace(hostSettings.GameSettingsFileName)
            ? "settings.json"
            : hostSettings.GameSettingsFileName.Trim();
        var directory = Path.GetDirectoryName(hostSettingsStore.FilePath) ?? AppContext.BaseDirectory;

        return Path.Combine(directory, fileName);
    }
}
