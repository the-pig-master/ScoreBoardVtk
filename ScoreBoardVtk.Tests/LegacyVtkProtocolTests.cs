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
            false,
            true,
            true,
            false,
            true,
            false);

        var payload = protocol.CreateGamePayload(state, new DateTime(2026, 3, 23, 12, 34, 56));

        Assert.Equal(" 122  731:164S1110 HELLO", payload);
    }

    [Fact]
    public void CreateTimeSyncPacket_UsesExpectedPrefixAndLength()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var protocol = new LegacyVtkProtocol();

        var packet = protocol.CreateTimeSyncPacket(new DateTime(2026, 3, 23, 12, 34, 56));

        Assert.Equal(15, packet.Length);
        Assert.Equal("AT+ST12:34:56", Encoding.GetEncoding(1251).GetString(packet[..13]));
    }
}
