using System.Globalization;
using System.Text;
using ScoreBoardVtk.Core.Models;

namespace ScoreBoardVtk.Core.Services;

public sealed class LegacyVtkProtocol : IScoreboardProtocol
{
    private static readonly Encoding Win1251 = Encoding.GetEncoding(1251);

    public string CreateGamePayload(ScoreboardState state, DateTime currentTime)
    {
        var scoreAText = state.HomeScore.ToString(CultureInfo.InvariantCulture).PadLeft(3, ' ');
        var scoreBText = state.GuestScore.ToString(CultureInfo.InvariantCulture).PadLeft(3, ' ');
        var periodText = state.IsExtraPeriod
            ? "E"
            : state.PeriodNumber.ToString(CultureInfo.InvariantCulture);
        var mainClockText = BuildLegacyGameTimeText(state, currentTime);
        var penaltyAText = state.HomeSecondaryCounter.ToString(CultureInfo.InvariantCulture);
        var penaltyBText = state.GuestSecondaryCounter.ToString(CultureInfo.InvariantCulture);
        var signal = state.IsMainSignalActive ? "S" : " ";
        var font = state.FontMode == FontMode.Font8x8 ? "1" : "0";
        var runningTextState = state.RunningTextEnabled ? "1" : "0";
        var shotClockText = BuildLegacyShotClockText(state);
        var shotClockSignal = state.IsShotClockSignalActive ? "S" : " ";

        return scoreAText +
            periodText +
            scoreBText +
            penaltyAText +
            mainClockText +
            penaltyBText +
            signal +
            font +
            runningTextState +
            shotClockText +
            shotClockSignal +
            state.RunningText;
    }

    public byte[] CreateGamePacket(ScoreboardState state, DateTime currentTime)
    {
        const int bodyLength = 49;
        const int totalLength = 51;

        var payload = CreateGamePayload(state, currentTime);
        var result = Enumerable.Repeat((byte)' ', totalLength).ToArray();
        var command = "AT+GD" + payload;
        var encoded = Win1251.GetBytes(command);
        var bytesToCopy = Math.Min(bodyLength, encoded.Length);

        Array.Copy(encoded, result, bytesToCopy);

        for (var index = 0; index < bodyLength; index++)
        {
            result[index] = LegacyCharMap.Translate(result[index]);
        }

        var crc = Crc16.Compute(result.AsSpan(0, bodyLength));
        result[49] = (byte)((crc & 0xFF00) >> 8);
        result[50] = (byte)(crc & 0x00FF);

        return result;
    }

    private static string BuildLegacyShotClockText(ScoreboardState state)
    {
        if (state.GameMode != GameMode.Basketball)
        {
            return "  ";
        }

        var shotClockSeconds = state.ShotClockTenths <= 0 ? 0 : (state.ShotClockTenths + 9) / 10;

        if (shotClockSeconds == 0)
        {
            return "00";
        }

        return shotClockSeconds < 10
            ? $" {shotClockSeconds.ToString(CultureInfo.InvariantCulture)}"
            : shotClockSeconds.ToString(CultureInfo.InvariantCulture);
    }

    private static string BuildLegacyGameTimeText(ScoreboardState state, DateTime currentTime)
    {
        if (state.GameMode != GameMode.Basketball)
        {
            return currentTime.ToString("HH:mm", CultureInfo.InvariantCulture);
        }

        if (state.TimerDirection == TimerDirection.Down)
        {
            if (state.MainClockTenths >= 600)
            {
                var totalDisplaySeconds = (state.MainClockTenths + 9) / 10;
                var minutes = totalDisplaySeconds / 60;
                var displaySeconds = totalDisplaySeconds % 60;
                return $"{minutes.ToString(CultureInfo.InvariantCulture)}:{displaySeconds:00}";
            }

            var secondsUnderMinute = state.MainClockTenths / 10;
            var tenths = state.MainClockTenths % 10;
            return $"{secondsUnderMinute.ToString(CultureInfo.InvariantCulture)}:{tenths.ToString(CultureInfo.InvariantCulture)} ";
        }

        if (state.MainClockTenths >= 600)
        {
            var totalDisplaySeconds = state.MainClockTenths / 10;
            var minutes = totalDisplaySeconds / 60;
            var displaySeconds = totalDisplaySeconds % 60;
            return $"{minutes.ToString(CultureInfo.InvariantCulture)}:{displaySeconds:00}";
        }

        var visibleSeconds = state.MainClockTenths / 10;
        var subSecond = state.MainClockTenths % 10;
        return $"{visibleSeconds.ToString(CultureInfo.InvariantCulture)}:{subSecond.ToString(CultureInfo.InvariantCulture)} ";
    }
}
