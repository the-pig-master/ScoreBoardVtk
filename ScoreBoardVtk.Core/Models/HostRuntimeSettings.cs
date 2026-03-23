using ScoreBoardVtk.Core.Services;

namespace ScoreBoardVtk.Core.Models;

public sealed class HostRuntimeSettings
{
    public int MainClockIntervalMilliseconds { get; set; } = 100;

    public int MainSignalIntervalMilliseconds { get; set; } = 1000;

    public int ShotClockSignalIntervalMilliseconds { get; set; } = 100;

    public int DisplayRefreshIntervalMilliseconds { get; set; } = 1000;

    public int PublishIntervalMilliseconds { get; set; } = 50;

    public ScoreboardRuntimeOptions ToRuntimeOptions()
    {
        return new ScoreboardRuntimeOptions
        {
            MainClockInterval = TimeSpan.FromMilliseconds(Math.Max(MainClockIntervalMilliseconds, 1)),
            MainSignalInterval = TimeSpan.FromMilliseconds(Math.Max(MainSignalIntervalMilliseconds, 1)),
            ShotClockSignalInterval = TimeSpan.FromMilliseconds(Math.Max(ShotClockSignalIntervalMilliseconds, 1)),
            DisplayRefreshInterval = TimeSpan.FromMilliseconds(Math.Max(DisplayRefreshIntervalMilliseconds, 1)),
            PublishInterval = TimeSpan.FromMilliseconds(Math.Max(PublishIntervalMilliseconds, 1)),
        };
    }
}
