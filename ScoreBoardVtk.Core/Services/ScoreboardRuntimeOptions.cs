namespace ScoreBoardVtk.Core.Services;

public sealed class ScoreboardRuntimeOptions
{
    public TimeSpan TimingInterval { get; init; } = TimeSpan.FromMilliseconds(100);

    public TimeSpan PublishInterval { get; init; } = TimeSpan.FromMilliseconds(50);
}
