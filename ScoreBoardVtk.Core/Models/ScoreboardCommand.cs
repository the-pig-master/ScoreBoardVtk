namespace ScoreBoardVtk.Core.Models;

public abstract record ScoreboardCommand;

public sealed record SetGameModeCommand(GameMode GameMode) : ScoreboardCommand;

public sealed record SetFontModeCommand(FontMode FontMode) : ScoreboardCommand;

public sealed record SetTimerDirectionCommand(TimerDirection TimerDirection) : ScoreboardCommand;

public sealed record SetCountFoulsToFiveCommand(bool Enabled) : ScoreboardCommand;

public sealed record SetMainSignalDurationSecondsCommand(int Seconds) : ScoreboardCommand;

public sealed record SetShotClockSignalDurationTenthsCommand(int Tenths) : ScoreboardCommand;

public sealed record SetRunningTextEnabledCommand(bool Enabled) : ScoreboardCommand;

public sealed record SetRunningTextCommand(string Text) : ScoreboardCommand;

public sealed record SetTimerPresetCommand(int Minutes, int Seconds, int Tenths) : ScoreboardCommand;

public sealed record SetOvertimeTimerPresetCommand(int Minutes, int Seconds, int Tenths) : ScoreboardCommand;

public sealed record SetScoreboardValuesCommand(
    int HomeScore,
    int GuestScore,
    int HomeSecondaryCounter,
    int GuestSecondaryCounter,
    int PeriodNumber,
    int MainClockTenths,
    int ShotClockTenths) : ScoreboardCommand;

public sealed record ChangeScoreCommand(TeamSide Side, int Delta) : ScoreboardCommand;

public sealed record ChangeSecondaryCounterCommand(TeamSide Side, int Delta) : ScoreboardCommand;

public sealed record SetShotClockCommand(int Seconds) : ScoreboardCommand;

public sealed record RunShotClockCommand(int Seconds) : ScoreboardCommand;

public sealed record SetManualSignalCommand(bool IsActive) : ScoreboardCommand;

public sealed record ToggleGameClockCommand : ScoreboardCommand;

public sealed record StopGameClockCommand : ScoreboardCommand;

public sealed record AdvancePeriodOrSetCommand : ScoreboardCommand;

public sealed record ResetScoreboardCommand : ScoreboardCommand;

public sealed record ToggleShotClockCommand : ScoreboardCommand;

public sealed record TickMainClockCommand : ScoreboardCommand;

public sealed record TickMainSignalCommand : ScoreboardCommand;

public sealed record TickShotClockSignalCommand : ScoreboardCommand;

public sealed record RefreshDisplayCommand : ScoreboardCommand;
