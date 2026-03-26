using ScoreBoardVtk.Core.Models;
using ScoreBoardVtk.Core.Services;

namespace ScoreBoardVtk.Tests;

public sealed class ScoreboardControllerTests
{
    [Fact]
    public void SetTimerDirection_IsIgnoredWhileGameClockIsRunning()
    {
        var controller = new ScoreboardController(CreateBasketballSettings());

        controller.Apply(new ToggleGameClockCommand());
        controller.Apply(new SetTimerDirectionCommand(TimerDirection.Up));

        Assert.Equal(TimerDirection.Down, controller.State.TimerDirection);
        Assert.True(controller.State.IsGameClockRunning);
    }

    [Fact]
    public void SetTimerDirection_InBasketball_IsForcedToDown()
    {
        var controller = new ScoreboardController(CreateBasketballSettings());

        controller.Apply(new SetTimerDirectionCommand(TimerDirection.Up));

        Assert.Equal(TimerDirection.Down, controller.State.TimerDirection);
    }

    [Fact]
    public void AdvancePeriodOrSet_InBasketball_FirstOvertimeUsesFiveMinutePreset()
    {
        var controller = new ScoreboardController(CreateBasketballSettings());

        controller.Apply(new AdvancePeriodOrSetCommand());
        controller.Apply(new AdvancePeriodOrSetCommand());
        controller.Apply(new AdvancePeriodOrSetCommand());
        controller.Apply(new AdvancePeriodOrSetCommand());

        Assert.Equal(5, controller.State.PeriodNumber);
        Assert.Equal(3000, controller.State.TimerPresetTenths);
        Assert.Equal(3000, controller.State.MainClockTenths);
    }

    [Fact]
    public void AdvancePeriodOrSet_InBasketball_UsesConfiguredOvertimePreset()
    {
        var controller = new ScoreboardController(CreateBasketballSettings());

        controller.Apply(new SetOvertimeTimerPresetCommand(3, 30, 0));
        controller.Apply(new AdvancePeriodOrSetCommand());
        controller.Apply(new AdvancePeriodOrSetCommand());
        controller.Apply(new AdvancePeriodOrSetCommand());
        controller.Apply(new AdvancePeriodOrSetCommand());

        Assert.Equal(5, controller.State.PeriodNumber);
        Assert.Equal(2100, controller.State.TimerPresetTenths);
        Assert.Equal(2100, controller.State.MainClockTenths);
    }

    [Fact]
    public void AdvancePeriodOrSet_InBasketball_AfterOvertimeWrapsBackToFirstQuarter()
    {
        var controller = new ScoreboardController(CreateBasketballSettings());

        controller.Apply(new AdvancePeriodOrSetCommand());
        controller.Apply(new AdvancePeriodOrSetCommand());
        controller.Apply(new AdvancePeriodOrSetCommand());
        controller.Apply(new AdvancePeriodOrSetCommand());
        controller.Apply(new AdvancePeriodOrSetCommand());

        Assert.Equal(1, controller.State.PeriodNumber);
        Assert.Equal(6000, controller.State.TimerPresetTenths);
        Assert.Equal(6000, controller.State.MainClockTenths);
    }

    [Fact]
    public void AdvancePeriodOrSet_InBasketball_WhenClockStopped_RearmsClockForNewQuarter()
    {
        var controller = CreateBasketballController(out var timeProvider);

        controller.Apply(new ToggleGameClockCommand());

        for (var index = 0; index < 6000; index++)
        {
            timeProvider.Advance(TimeSpan.FromMilliseconds(100));
            controller.Apply(new TickMainClockCommand());
        }

        controller.Apply(new AdvancePeriodOrSetCommand());

        Assert.Equal(2, controller.State.PeriodNumber);
        Assert.Equal(6000, controller.State.MainClockTenths);
        Assert.Equal(240, controller.State.ShotClockTenths);
    }

    [Fact]
    public void AdvancePeriodOrSet_InBasketball_WhenClockRunning_IsIgnored()
    {
        var controller = new ScoreboardController(CreateBasketballSettings());

        controller.Apply(new ToggleGameClockCommand());
        controller.Apply(new AdvancePeriodOrSetCommand());

        Assert.Equal(1, controller.State.PeriodNumber);
        Assert.True(controller.State.IsGameClockRunning);
    }

    [Fact]
    public void Reset_InBasketball_WhenStopped_ResetsOnlyTimers()
    {
        var controller = new ScoreboardController(CreateBasketballSettings());

        controller.Apply(new ChangeScoreCommand(TeamSide.Home, 12));
        controller.Apply(new ChangeScoreCommand(TeamSide.Guest, 7));
        controller.Apply(new ChangeSecondaryCounterCommand(TeamSide.Home, 3));
        controller.Apply(new ChangeSecondaryCounterCommand(TeamSide.Guest, 2));
        controller.Apply(new AdvancePeriodOrSetCommand());
        controller.Apply(new SetShotClockCommand(14));
        controller.Apply(new ResetScoreboardCommand());

        Assert.Equal(12, controller.State.HomeScore);
        Assert.Equal(7, controller.State.GuestScore);
        Assert.Equal(3, controller.State.HomeSecondaryCounter);
        Assert.Equal(2, controller.State.GuestSecondaryCounter);
        Assert.Equal(2, controller.State.PeriodNumber);
        Assert.Equal(6000, controller.State.MainClockTenths);
        Assert.Equal(240, controller.State.ShotClockTenths);
        Assert.False(controller.State.IsGameClockRunning);
        Assert.False(controller.State.IsShotClockRunning);
        Assert.False(controller.State.IsMainSignalActive);
        Assert.False(controller.State.IsShotClockSignalActive);
    }

    [Fact]
    public void Reset_InBasketball_WhenGameClockRunning_IsIgnored()
    {
        var controller = new ScoreboardController(CreateBasketballSettings());

        controller.Apply(new ToggleGameClockCommand());
        controller.Apply(new ResetScoreboardCommand());

        Assert.True(controller.State.IsGameClockRunning);
        Assert.Equal(6000, controller.State.MainClockTenths);
    }

    [Fact]
    public void SetScoreboardValues_InBasketball_SetsRequestedValuesAndStopsTimers()
    {
        var controller = new ScoreboardController(CreateBasketballSettings());

        controller.Apply(new ToggleGameClockCommand());
        controller.Apply(new SetScoreboardValuesCommand(87, 79, 4, 3, 5, 125, 143));

        Assert.Equal(87, controller.State.HomeScore);
        Assert.Equal(79, controller.State.GuestScore);
        Assert.Equal(4, controller.State.HomeSecondaryCounter);
        Assert.Equal(3, controller.State.GuestSecondaryCounter);
        Assert.Equal(5, controller.State.PeriodNumber);
        Assert.Equal(125, controller.State.MainClockTenths);
        Assert.Equal(125, controller.State.ShotClockTenths);
        Assert.False(controller.State.IsGameClockRunning);
        Assert.False(controller.State.IsShotClockRunning);
        Assert.False(controller.State.IsMainSignalActive);
        Assert.False(controller.State.IsShotClockSignalActive);
    }

    [Fact]
    public void SetScoreboardValues_InBasketball_ClampsShotClockToTwentyFourSeconds()
    {
        var controller = new ScoreboardController(CreateBasketballSettings());

        controller.Apply(new SetScoreboardValuesCommand(10, 8, 1, 2, 1, 6000, 300));

        Assert.Equal(240, controller.State.ShotClockTenths);
    }

    [Fact]
    public void RunShotClockCommand_SetsRequestedValueAndStartsShotClock()
    {
        var controller = new ScoreboardController(CreateBasketballSettings());

        controller.Apply(new RunShotClockCommand(14));

        Assert.Equal(140, controller.State.ShotClockTenths);
        Assert.True(controller.State.IsShotClockRunning);
    }

    [Fact]
    public void TickMainClock_WhenPeriodEnds_StopsAtZeroAndActivatesMainSignal()
    {
        var controller = CreateBasketballController(out var timeProvider);

        controller.Apply(new SetTimerPresetCommand(0, 0, 1));
        controller.Apply(new ToggleGameClockCommand());
        timeProvider.Advance(TimeSpan.FromMilliseconds(100));
        controller.Apply(new TickMainClockCommand());

        Assert.False(controller.State.IsGameClockRunning);
        Assert.True(controller.State.IsMainSignalActive);
        Assert.Equal(0, controller.State.MainClockTenths);
    }

    [Fact]
    public void TickMainClock_WhenShotClockExpires_StopsShotClockAtZeroAndActivatesSignal()
    {
        var controller = CreateBasketballController(out var timeProvider);

        controller.Apply(new RunShotClockCommand(14));
        controller.Apply(new ToggleGameClockCommand());

        for (var index = 0; index < 140; index++)
        {
            timeProvider.Advance(TimeSpan.FromMilliseconds(100));
            controller.Apply(new TickMainClockCommand());
        }

        Assert.True(controller.State.IsShotClockSignalActive);
        Assert.False(controller.State.IsShotClockRunning);
        Assert.Equal(0, controller.State.ShotClockTenths);
    }

    [Fact]
    public void TickMainClock_UsesElapsedTimeWhenTickIsDelayed()
    {
        var controller = CreateBasketballController(out var timeProvider);

        controller.Apply(new SetTimerPresetCommand(0, 0, 5));
        controller.Apply(new ToggleGameClockCommand());

        timeProvider.Advance(TimeSpan.FromMilliseconds(350));
        controller.Apply(new TickMainClockCommand());

        Assert.Equal(2, controller.State.MainClockTenths);
    }

    [Fact]
    public void TickMainClock_AccumulatesSubTenthJitterAcrossCalls()
    {
        var controller = CreateBasketballController(out var timeProvider);

        controller.Apply(new SetTimerPresetCommand(0, 1, 0));
        controller.Apply(new ToggleGameClockCommand());

        timeProvider.Advance(TimeSpan.FromMilliseconds(55));
        controller.Apply(new TickMainClockCommand());
        Assert.Equal(10, controller.State.MainClockTenths);

        timeProvider.Advance(TimeSpan.FromMilliseconds(55));
        controller.Apply(new TickMainClockCommand());
        Assert.Equal(9, controller.State.MainClockTenths);
    }

    [Fact]
    public void StopGameClock_SynchronizesElapsedTimeBeforeStopping()
    {
        var controller = CreateBasketballController(out var timeProvider);

        controller.Apply(new SetTimerPresetCommand(0, 1, 0));
        controller.Apply(new ToggleGameClockCommand());

        timeProvider.Advance(TimeSpan.FromMilliseconds(250));
        controller.Apply(new ToggleGameClockCommand());

        Assert.False(controller.State.IsGameClockRunning);
        Assert.Equal(8, controller.State.MainClockTenths);
    }

    [Fact]
    public void TickMainClock_UsesOvershootFromPeriodCompletionSignal()
    {
        var settings = CreateBasketballSettings();
        settings.MainSignalDurationSeconds = 3;
        var timeProvider = new ManualTimeProvider();
        var controller = new ScoreboardController(settings, timeProvider);

        controller.Apply(new SetTimerPresetCommand(0, 0, 1));
        controller.Apply(new ToggleGameClockCommand());

        timeProvider.Advance(TimeSpan.FromMilliseconds(300));
        controller.Apply(new TickMainClockCommand());
        Assert.True(controller.State.IsMainSignalActive);

        timeProvider.Advance(TimeSpan.FromMilliseconds(2800));
        controller.Apply(new TickMainClockCommand());

        Assert.False(controller.State.IsMainSignalActive);
    }

    [Fact]
    public void TickMainClock_UsesOvershootFromShotClockExpirySignal()
    {
        var settings = CreateBasketballSettings();
        settings.ShotClockSignalDurationTenths = 5;
        var timeProvider = new ManualTimeProvider();
        var controller = new ScoreboardController(settings, timeProvider);

        controller.Apply(new SetScoreboardValuesCommand(0, 0, 0, 0, 1, 100, 1));
        controller.Apply(new ToggleShotClockCommand());
        controller.Apply(new ToggleGameClockCommand());

        timeProvider.Advance(TimeSpan.FromMilliseconds(300));
        controller.Apply(new TickMainClockCommand());
        Assert.True(controller.State.IsShotClockSignalActive);

        timeProvider.Advance(TimeSpan.FromMilliseconds(300));
        controller.Apply(new TickMainClockCommand());

        Assert.False(controller.State.IsShotClockSignalActive);
    }

    [Fact]
    public void AdvancePeriodOrSet_InVolleyballAwardsSetToWinnerAndResetsScores()
    {
        var controller = new ScoreboardController(new AppSettings
        {
            GameMode = GameMode.Volleyball,
            TimerDirection = TimerDirection.Down,
            GameTimePreset = "10:00",
        });

        controller.Apply(new ChangeScoreCommand(TeamSide.Home, 5));
        controller.Apply(new ChangeScoreCommand(TeamSide.Guest, 3));
        controller.Apply(new AdvancePeriodOrSetCommand());

        Assert.Equal(GameMode.Volleyball, controller.State.GameMode);
        Assert.Equal(SecondaryCounterKind.Sets, controller.State.SecondaryCounterKind);
        Assert.Equal(2, controller.State.PeriodNumber);
        Assert.Equal(1, controller.State.HomeSecondaryCounter);
        Assert.Equal(0, controller.State.GuestSecondaryCounter);
        Assert.Equal(0, controller.State.HomeScore);
        Assert.Equal(0, controller.State.GuestScore);
    }

    private static AppSettings CreateBasketballSettings()
    {
        return new AppSettings
        {
            GameMode = GameMode.Basketball,
            TimerDirection = TimerDirection.Down,
            GameTimePreset = "10:00",
            OvertimeTimePreset = "05:00",
            MainSignalDurationSeconds = 3,
            FontMode = FontMode.Font6x8,
            CountFoulsToFive = true,
            RunningTextEnabled = false,
            RunningText = string.Empty,
        };
    }

    private static ScoreboardController CreateBasketballController(out ManualTimeProvider timeProvider)
    {
        timeProvider = new ManualTimeProvider();
        return new ScoreboardController(CreateBasketballSettings(), timeProvider);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow = DateTimeOffset.UnixEpoch;
        private long _timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        public override long GetTimestamp()
        {
            return _timestamp;
        }

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public void Advance(TimeSpan elapsed)
        {
            _utcNow = _utcNow.Add(elapsed);
            _timestamp += elapsed.Ticks;
        }
    }
}
