using ScoreBoardVtk.Core.Services;

namespace ScoreBoardVtk.Core.Models;

public sealed class HostRuntimeSettings
{
    public int TimingIntervalMilliseconds { get; set; } = 100;

    public int PublishIntervalMilliseconds { get; set; } = 50;

    public ScoreboardRuntimeOptions ToRuntimeOptions()
    {
        return new ScoreboardRuntimeOptions
        {
            TimingInterval = TimeSpan.FromMilliseconds(Math.Max(TimingIntervalMilliseconds, 1)),
            PublishInterval = TimeSpan.FromMilliseconds(Math.Max(PublishIntervalMilliseconds, 1)),
        };
    }
}
