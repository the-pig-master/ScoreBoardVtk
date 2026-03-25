using ScoreBoardVtk.Core.Models;
using ScoreBoardVtk.Core.Services;

namespace ScoreBoardVtk.Tests;

public sealed class ScoreboardApiTests
{
    [Fact]
    public void Publish_UsesProtocolWithLatestState()
    {
        var transport = new FakeSerialTransport();
        var protocol = new FakeProtocol();
        var api = new ScoreboardApi(new AppSettings(), transport, protocol, new FixedTimeProvider(new DateTimeOffset(2026, 3, 23, 12, 34, 56, TimeSpan.Zero)));

        api.Connect("COM9");
        api.Execute(new ChangeScoreCommand(TeamSide.Home, 1));
        api.Publish();

        Assert.NotNull(protocol.LastPublishedState);
        Assert.Equal(1, protocol.LastPublishedState!.HomeScore);
        Assert.Equal(new byte[] { 1, 2, 3 }, transport.LastWrite);
        Assert.Equal("PAYLOAD", api.Snapshot.PayloadText);
    }

    private sealed class FakeSerialTransport : ISerialTransport
    {
        public byte[] LastWrite { get; private set; } = [];

        public bool IsOpen { get; private set; }

        public string PortName { get; private set; } = string.Empty;

        public string[] GetAvailablePorts()
        {
            return ["COM9"];
        }

        public void Open(string portName)
        {
            PortName = portName;
            IsOpen = true;
        }

        public void Write(byte[] data)
        {
            LastWrite = data.ToArray();
        }

        public void Close()
        {
            IsOpen = false;
            PortName = string.Empty;
        }

        public void Dispose()
        {
            Close();
        }
    }

    private sealed class FakeProtocol : IScoreboardProtocol
    {
        public ScoreboardState? LastPublishedState { get; private set; }

        public string CreateGamePayload(ScoreboardState state, DateTime currentTime)
        {
            return "PAYLOAD";
        }

        public byte[] CreateGamePacket(ScoreboardState state, DateTime currentTime)
        {
            LastPublishedState = state;
            return [1, 2, 3];
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset currentTime) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return currentTime;
        }

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
