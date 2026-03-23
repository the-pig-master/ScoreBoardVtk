namespace ScoreBoardVtk.Core.Services;

public sealed class ScoreboardRuntimeOptions
{
    public TimeSpan MainClockInterval { get; init; } = TimeSpan.FromMilliseconds(100);

    public TimeSpan MainSignalInterval { get; init; } = TimeSpan.FromSeconds(1);

    public TimeSpan ShotClockSignalInterval { get; init; } = TimeSpan.FromMilliseconds(100);

    public TimeSpan DisplayRefreshInterval { get; init; } = TimeSpan.FromSeconds(1);

    public TimeSpan PublishInterval { get; init; } = TimeSpan.FromMilliseconds(50);
}
