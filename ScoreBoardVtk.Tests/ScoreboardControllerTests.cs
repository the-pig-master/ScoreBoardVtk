using ScoreBoardVtk.Core.Models;
using ScoreBoardVtk.Core.Services;

namespace ScoreBoardVtk.Tests;

public sealed class ScoreboardControllerTests
{
    [Fact]
    public void SetTimerDirection_IsIgnoredWhileGameClockIsRunning()
    {
        var controller = new ScoreboardController(CreateBasketballSettings());

        controller.ToggleGameClock();
        controller.SetTimerDirection(TimerDirection.Up);

        Assert.Equal(TimerDirection.Down, controller.State.TimerDirection);
        Assert.True(controller.State.IsGameClockRunning);
    }

    [Fact]
    public void SetTimerDirection_InBasketball_IsForcedToDown()
    {
        var controller = new ScoreboardController(CreateBasketballSettings());

        controller.SetTimerDirection(TimerDirection.Up);

        Assert.Equal(TimerDirection.Down, controller.State.TimerDirection);
    }

    [Fact]
    public void AdvancePeriodOrSet_InBasketball_FirstOvertimeUsesFiveMinutePreset()
    {
        var controller = new ScoreboardController(CreateBasketballSettings());

        controller.AdvancePeriodOrSet();
        controller.AdvancePeriodOrSet();
        controller.AdvancePeriodOrSet();
        controller.AdvancePeriodOrSet();

        Assert.Equal(5, controller.State.PeriodNumber);
        Assert.Equal(3000, controller.State.TimerPresetTenths);
        Assert.Equal(3000, controller.State.MainClockTenths);
    }

    [Fact]
    public void AdvancePeriodOrSet_InBasketball_AfterOvertimeWrapsBackToFirstQuarter()
    {
        var controller = new ScoreboardController(CreateBasketballSettings());

        controller.AdvancePeriodOrSet();
        controller.AdvancePeriodOrSet();
        controller.AdvancePeriodOrSet();
        controller.AdvancePeriodOrSet();
        controller.AdvancePeriodOrSet();

        Assert.Equal(1, controller.State.PeriodNumber);
        Assert.Equal(6000, controller.State.TimerPresetTenths);
        Assert.Equal(6000, controller.State.MainClockTenths);
    }

    [Fact]
    public void AdvancePeriodOrSet_InBasketball_WhenClockStopped_RearmsClockForNewQuarter()
    {
        var controller = new ScoreboardController(CreateBasketballSettings());

        controller.ToggleGameClock();

        for (var index = 0; index < 6000; index++)
        {
            controller.TickMainClock();
        }

        controller.AdvancePeriodOrSet();

        Assert.Equal(2, controller.State.PeriodNumber);
        Assert.Equal(6000, controller.State.MainClockTenths);
        Assert.Equal(240, controller.State.ShotClockTenths);
    }

    [Fact]
    public void AdvancePeriodOrSet_InBasketball_WhenClockRunning_IsIgnored()
    {
        var controller = new ScoreboardController(CreateBasketballSettings());

        controller.ToggleGameClock();
        controller.AdvancePeriodOrSet();

        Assert.Equal(1, controller.State.PeriodNumber);
        Assert.True(controller.State.IsGameClockRunning);
    }

    [Fact]
    public void TickMainClock_WhenPeriodEnds_StopsAtZeroAndActivatesMainSignal()
    {
        var controller = new ScoreboardController(CreateBasketballSettings());

        controller.Apply(new SetTimerPresetCommand(0, 0, 1));
        controller.ToggleGameClock();
        controller.TickMainClock();

        Assert.False(controller.State.IsGameClockRunning);
        Assert.True(controller.State.IsMainSignalActive);
        Assert.Equal(0, controller.State.MainClockTenths);
    }

    [Fact]
    public void TickMainClock_WhenShotClockExpires_StopsShotClockAtZeroAndActivatesSignal()
    {
        var controller = new ScoreboardController(CreateBasketballSettings(autoStartShotClock: true));

        controller.SetShotClock14();
        controller.ToggleGameClock();

        for (var index = 0; index < 140; index++)
        {
            controller.TickMainClock();
        }

        Assert.True(controller.State.IsShotClockSignalActive);
        Assert.False(controller.State.IsShotClockRunning);
        Assert.Equal(0, controller.State.ShotClockTenths);
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
        controller.AdvancePeriodOrSet();

        Assert.Equal(GameMode.Volleyball, controller.State.GameMode);
        Assert.Equal(SecondaryCounterKind.Sets, controller.State.SecondaryCounterKind);
        Assert.Equal(2, controller.State.PeriodNumber);
        Assert.Equal(1, controller.State.HomeSecondaryCounter);
        Assert.Equal(0, controller.State.GuestSecondaryCounter);
        Assert.Equal(0, controller.State.HomeScore);
        Assert.Equal(0, controller.State.GuestScore);
    }

    private static AppSettings CreateBasketballSettings(bool autoStartShotClock = false)
    {
        return new AppSettings
        {
            GameMode = GameMode.Basketball,
            TimerDirection = TimerDirection.Down,
            GameTimePreset = "10:00",
            MainSignalDurationSeconds = 3,
            FontMode = FontMode.Font6x8,
            CountFoulsToFive = true,
            AutoStartShotClock = autoStartShotClock,
            RunningTextEnabled = false,
            RunningText = string.Empty,
        };
    }
}
