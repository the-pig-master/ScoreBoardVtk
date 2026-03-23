using ScoreBoardVtk.Core.Models;

namespace ScoreBoardVtk.Core.Services;

public sealed record ScoreboardApplicationServices(
    HostSettingsStore HostSettingsStore,
    HostSettings HostSettings,
    SettingsStore SettingsStore,
    IScoreboardApi ScoreboardApi,
    IScoreboardRuntime Runtime) : IDisposable
{
    public void Dispose()
    {
        Runtime.Dispose();
        ScoreboardApi.Dispose();
    }
}
