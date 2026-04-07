using System.Text;
using ScoreBoardVtk.Core.Models;
using ScoreBoardVtk.Core.Services;

namespace ScoreBoardVtk.Tests;

public sealed class LegacyVtkProtocolTests
{
    [Fact]
    public void CreateGamePayload_BuildsExpectedLegacyPayload()
    {
        var protocol = new LegacyVtkProtocol();
        var state = new ScoreboardState(
            GameMode.Basketball,
            TimerDirection.Down,
            FontMode.Font8x8,
            12,
            7,
            2,
            SecondaryCounterKind.Fouls,
            3,
            4,
            754,
            6000,
            0,
            93,
            0,
            "HELLO",
            true,
            true,
            true,
            true,
            false,
            true,
            false);

        var payload = protocol.CreateGamePayload(state, new DateTime(2026, 3, 23, 12, 34, 56));

        Assert.Equal(44, payload.Length);
        Assert.Equal(" 12", payload.Substring(0, 3));
        Assert.Equal("2", payload.Substring(3, 1));
        Assert.Equal("  7", payload.Substring(4, 3));
        Assert.Equal("3", payload.Substring(7, 1));
        Assert.Equal(" 1:16", payload.Substring(8, 5));
        Assert.Equal("4", payload.Substring(13, 1));
        Assert.Equal("S", payload.Substring(14, 1));
        Assert.Equal("1", payload.Substring(15, 1));
        Assert.Equal("1", payload.Substring(16, 1));
        Assert.Equal("10", payload.Substring(17, 2));
        Assert.Equal(" ", payload.Substring(19, 1));
        Assert.Equal("HELLO", payload.Substring(20, 5));
        Assert.True(payload.Substring(25).All(ch => ch == ' '));
    }

    [Fact]
    public void CreateGamePayload_FormatsUnderMinuteClockAsFixedFiveCharacters()
    {
        var protocol = new LegacyVtkProtocol();
        var state = new ScoreboardState(
            GameMode.Basketball,
            TimerDirection.Down,
            FontMode.Font6x8,
            1,
            2,
            1,
            SecondaryCounterKind.Fouls,
            5,
            6,
            95,
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

        var payload = protocol.CreateGamePayload(state, new DateTime(2026, 3, 23, 12, 34, 56));

        Assert.Equal(" 9:5 ", payload.Substring(8, 5));
        Assert.Equal(44, payload.Length);
    }

    [Fact]
    public void CreateGamePayload_ByDefault_RoutesShotClockSignalThroughMainSignalField()
    {
        var protocol = new LegacyVtkProtocol(new AppSettings
        {
            UseMainSignalFieldForShotClockSignal = true,
        });
        var state = new ScoreboardState(
            GameMode.Basketball,
            TimerDirection.Down,
            FontMode.Font6x8,
            1,
            2,
            1,
            SecondaryCounterKind.Fouls,
            0,
            0,
            6000,
            6000,
            0,
            140,
            0,
            string.Empty,
            false,
            true,
            false,
            false,
            false,
            false,
            true);

        var payload = protocol.CreateGamePayload(state, new DateTime(2026, 3, 23, 12, 34, 56));

        Assert.Equal("S", payload.Substring(14, 1));
        Assert.Equal(" ", payload.Substring(19, 1));
    }

    [Fact]
    public void CreateGamePayload_WhenConfigured_RoutesShotClockSignalThroughDedicatedField()
    {
        var protocol = new LegacyVtkProtocol(new AppSettings
        {
            UseMainSignalFieldForShotClockSignal = false,
        });
        var state = new ScoreboardState(
            GameMode.Basketball,
            TimerDirection.Down,
            FontMode.Font6x8,
            1,
            2,
            1,
            SecondaryCounterKind.Fouls,
            0,
            0,
            6000,
            6000,
            0,
            140,
            0,
            string.Empty,
            false,
            true,
            false,
            false,
            false,
            false,
            true);

        var payload = protocol.CreateGamePayload(state, new DateTime(2026, 3, 23, 12, 34, 56));

        Assert.Equal(" ", payload.Substring(14, 1));
        Assert.Equal("S", payload.Substring(19, 1));
    }

    [Fact]
    public void CreateGamePacket_NormalizesManualPayloadToFixedLength()
    {
        var protocol = new LegacyVtkProtocol();

        var packet = protocol.CreateGamePacket("AT+GD123");

        Assert.Equal(51, packet.Length);
        Assert.Equal("AT+GD123", Encoding.GetEncoding(1251).GetString(packet[..8]));
    }

    [Fact]
    public void CreateGamePacket_UsesSpecifiedEncodingForManualPayload()
    {
        var protocol = new LegacyVtkProtocol();

        var packet = protocol.CreateGamePacket("А", Encoding.GetEncoding(866));

        Assert.Equal(51, packet.Length);
        Assert.Equal(0xC0, packet[5]);
    }
}
