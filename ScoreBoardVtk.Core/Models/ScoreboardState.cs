namespace ScoreBoardVtk.Core.Models;

public sealed record ScoreboardState(
    GameMode GameMode,
    TimerDirection TimerDirection,
    FontMode FontMode,
    int HomeScore,
    int GuestScore,
    int PeriodNumber,
    SecondaryCounterKind SecondaryCounterKind,
    int HomeSecondaryCounter,
    int GuestSecondaryCounter,
    int MainClockTenths,
    int TimerPresetTenths,
    int MainSignalRemainingSeconds,
    int ShotClockTenths,
    int ShotClockSignalRemainingTenths,
    string RunningText,
    bool RunningTextEnabled,
    bool CountFoulsToFive,
    bool IsGameClockRunning,
    bool IsShotClockRunning,
    bool IsManualSignalActive,
    bool IsMainSignalActive,
    bool IsShotClockSignalActive)
{
    public bool IsExtraPeriod => GameMode == GameMode.Basketball && PeriodNumber > 4;
}
