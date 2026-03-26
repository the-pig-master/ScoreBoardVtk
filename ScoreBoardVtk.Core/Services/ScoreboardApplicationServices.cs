namespace ScoreBoardVtk.Core.Services;

public sealed record ScoreboardApplicationServices(
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
