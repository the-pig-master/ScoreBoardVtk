using ScoreBoardVtk.Core.Models;
using ScoreBoardVtk.Core.Services;

namespace ScoreBoardVtk.Tests;

public sealed class ScoreboardSnapshotFactoryTests
{
    [Fact]
    public void Create_InBasketball_AlwaysExposesShotClockTenthsForOperatorView()
    {
        var state = new ScoreboardState(
            GameMode.Basketball,
            TimerDirection.Down,
            FontMode.Font6x8,
            10,
            8,
            2,
            SecondaryCounterKind.Fouls,
            1,
            2,
            6000,
            6000,
            0,
            120,
            0,
            string.Empty,
            false,
            true,
            false,
            false,
            false,
            false,
            false);

        var snapshot = ScoreboardSnapshotFactory.Create(state, DateTime.Today, "payload");

        Assert.Equal("12", snapshot.ShotClockSecondsText);
        Assert.Equal("0", snapshot.ShotClockTenthsText);
    }

    [Fact]
    public void Create_InBasketball_ShowsShotClockTenthsDuringLastFiveSeconds()
    {
        var state = new ScoreboardState(
            GameMode.Basketball,
            TimerDirection.Down,
            FontMode.Font6x8,
            10,
            8,
            2,
            SecondaryCounterKind.Fouls,
            1,
            2,
            6000,
            6000,
            0,
            49,
            0,
            string.Empty,
            false,
            true,
            false,
            false,
            false,
            false,
            false);

        var snapshot = ScoreboardSnapshotFactory.Create(state, DateTime.Today, "payload");

        Assert.Equal("05", snapshot.ShotClockSecondsText);
        Assert.Equal("9", snapshot.ShotClockTenthsText);
    }
}
