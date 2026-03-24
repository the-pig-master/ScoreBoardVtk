namespace ScoreBoardVtk.Core.Models;

public sealed class AppSettings
{
    public string SelectedPort { get; set; } = string.Empty;

    public string GameTimePreset { get; set; } = "10:00";

    public string OvertimeTimePreset { get; set; } = "05:00";

    public bool CountFoulsToFive { get; set; } = true;

    public TimerDirection TimerDirection { get; set; } = TimerDirection.Down;

    public int MainSignalDurationSeconds { get; set; } = 3;

    public FontMode FontMode { get; set; } = FontMode.Font6x8;

    public GameMode GameMode { get; set; } = GameMode.Basketball;

    public int ShotClockSignalDurationTenths { get; set; } = 15;

    public bool AutoStartShotClock { get; set; }

    public bool RunningTextEnabled { get; set; }

    public string RunningText { get; set; } = string.Empty;
}
