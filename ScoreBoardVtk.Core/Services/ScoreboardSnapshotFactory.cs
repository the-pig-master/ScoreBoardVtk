using System.Globalization;
using ScoreBoardVtk.Core.Models;

namespace ScoreBoardVtk.Core.Services;

public static class ScoreboardSnapshotFactory
{
    public static ScoreboardSnapshot Create(ScoreboardState state, DateTime currentTime, string payloadText)
    {
        var shotClockSeconds = state.ShotClockTenths <= 0 ? 0 : (state.ShotClockTenths + 9) / 10;
        var shotClockTenths = state.ShotClockTenths % 10;

        return new ScoreboardSnapshot(
            state.GameMode,
            state.TimerDirection,
            state.FontMode,
            state.HomeScore.ToString("000", CultureInfo.InvariantCulture),
            state.GuestScore.ToString("000", CultureInfo.InvariantCulture),
            state.IsExtraPeriod ? "E" : state.PeriodNumber.ToString(CultureInfo.InvariantCulture),
            FormatMainClock(state, currentTime),
            state.HomeSecondaryCounter.ToString(CultureInfo.InvariantCulture),
            state.GuestSecondaryCounter.ToString(CultureInfo.InvariantCulture),
            state.SecondaryCounterKind == SecondaryCounterKind.Fouls ? "FOULS" : "SETS",
            shotClockSeconds.ToString("00", CultureInfo.InvariantCulture),
            shotClockTenths.ToString(CultureInfo.InvariantCulture),
            FormatPreset(state.TimerPresetTenths),
            state.RunningText,
            state.RunningTextEnabled,
            state.CountFoulsToFive,
            state.AutoStartShotClock,
            state.IsGameClockRunning,
            state.IsShotClockRunning,
            state.IsMainSignalActive,
            state.IsShotClockSignalActive,
            state.IsGameClockRunning ? "Stop" : "Start",
            state.IsShotClockRunning ? "Stop" : "Start",
            payloadText);
    }

    private static string FormatMainClock(ScoreboardState state, DateTime currentTime)
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
                return TimeSpan.FromSeconds(totalDisplaySeconds).ToString(@"mm\:ss", CultureInfo.InvariantCulture);
            }

            var seconds = state.MainClockTenths / 10;
            var tenths = state.MainClockTenths % 10;
            return $"{seconds:00}.{tenths}";
        }

        if (state.MainClockTenths >= 600)
        {
            var totalDisplaySeconds = state.MainClockTenths / 10;
            return TimeSpan.FromSeconds(totalDisplaySeconds).ToString(@"mm\:ss", CultureInfo.InvariantCulture);
        }

        var secondsUnderMinute = state.MainClockTenths / 10;
        var subSecond = state.MainClockTenths % 10;
        return $"{secondsUnderMinute:00}.{subSecond}";
    }

    private static string FormatPreset(int tenths)
    {
        tenths = Math.Max(tenths, 0);
        var totalSeconds = tenths / 10;
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        var remainder = tenths % 10;

        return remainder == 0
            ? $"{minutes:00}:{seconds:00}"
            : $"{minutes:00}:{seconds:00}.{remainder}";
    }
}
