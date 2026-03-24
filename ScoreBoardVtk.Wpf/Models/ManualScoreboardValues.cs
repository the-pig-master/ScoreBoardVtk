namespace ScoreBoardVtk.Wpf.Models;

public sealed record ManualScoreboardValues(
    int HomeScore,
    int GuestScore,
    int HomeSecondaryCounter,
    int GuestSecondaryCounter,
    int PeriodNumber,
    int MainClockTenths,
    int ShotClockTenths);
