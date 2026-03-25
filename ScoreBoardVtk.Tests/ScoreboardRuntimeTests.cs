using System.Collections.Concurrent;
using ScoreBoardVtk.Core.Models;
using ScoreBoardVtk.Core.Services;

namespace ScoreBoardVtk.Tests;

public sealed class ScoreboardRuntimeTests
{
    [Fact]
    public async Task Start_SchedulesTicksRefreshAndPublish()
    {
        var api = new FakeScoreboardApi { IsConnected = true };
        using var runtime = new ScoreboardRuntime(api, new ScoreboardRuntimeOptions
        {
            MainClockInterval = TimeSpan.FromMilliseconds(20),
            MainSignalInterval = TimeSpan.FromMilliseconds(25),
            ShotClockSignalInterval = TimeSpan.FromMilliseconds(20),
            DisplayRefreshInterval = TimeSpan.FromMilliseconds(30),
            PublishInterval = TimeSpan.FromMilliseconds(15),
        });

        runtime.Start();
        await Task.Delay(120);
        runtime.Stop();

        Assert.Contains(api.ExecutedCommands, command => command is TickMainClockCommand);
        Assert.Contains(api.ExecutedCommands, command => command is RefreshDisplayCommand);
        Assert.True(api.PublishCount > 0);
    }

    private sealed class FakeScoreboardApi : IScoreboardApi
    {
        public event EventHandler<ScoreboardState>? StateChanged
        {
            add { }
            remove { }
        }

        public AppSettings Settings { get; } = new();

        public ScoreboardState State { get; private set; } = new(
            GameMode.Basketball,
            TimerDirection.Down,
            FontMode.Font6x8,
            0,
            0,
            1,
            SecondaryCounterKind.Fouls,
            0,
            0,
            6000,
            6000,
            0,
            240,
            0,
            string.Empty,
            false,
            true,
            false,
            false,
            false,
            false,
            false);

        public ScoreboardSnapshot Snapshot { get; private set; } = ScoreboardSnapshotFactory.Create(
            new ScoreboardState(
                GameMode.Basketball,
                TimerDirection.Down,
                FontMode.Font6x8,
                0,
                0,
                1,
                SecondaryCounterKind.Fouls,
                0,
                0,
                6000,
                6000,
                0,
                240,
                0,
                string.Empty,
                false,
                true,
                false,
                false,
                false,
                false,
                false),
            DateTime.Now,
            string.Empty);

        public bool IsConnected { get; set; }

        public string ConnectedPortName { get; private set; } = string.Empty;

        public ConcurrentQueue<ScoreboardCommand> ExecutedCommands { get; } = new();

        public int PublishCount { get; private set; }

        public IReadOnlyList<string> GetAvailablePorts()
        {
            return ["COM1"];
        }

        public void Execute(ScoreboardCommand command)
        {
            ExecutedCommands.Enqueue(command);
        }

        public void Connect(string portName)
        {
            IsConnected = true;
            ConnectedPortName = portName;
        }

        public void Disconnect()
        {
            IsConnected = false;
            ConnectedPortName = string.Empty;
        }

        public void Publish()
        {
            PublishCount++;
        }

        public void Dispose()
        {
        }
    }
}
