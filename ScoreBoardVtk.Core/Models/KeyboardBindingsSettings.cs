namespace ScoreBoardVtk.Core.Models;

public sealed class KeyboardBindingsSettings
{
    public string ToggleGameClockKey { get; set; } = string.Empty;

    public string ToggleShotClockKey { get; set; } = string.Empty;

    public string SetShotClock24Key { get; set; } = string.Empty;

    public string SetShotClock14Key { get; set; } = string.Empty;

    public string RunShotClock24Key { get; set; } = string.Empty;

    public string RunShotClock14Key { get; set; } = string.Empty;

    public string IncreaseHomeScoreKey { get; set; } = string.Empty;

    public string DecreaseHomeScoreKey { get; set; } = string.Empty;

    public string IncreaseGuestScoreKey { get; set; } = string.Empty;

    public string DecreaseGuestScoreKey { get; set; } = string.Empty;

    public string IncreaseHomeFoulsKey { get; set; } = string.Empty;

    public string DecreaseHomeFoulsKey { get; set; } = string.Empty;

    public string IncreaseGuestFoulsKey { get; set; } = string.Empty;

    public string DecreaseGuestFoulsKey { get; set; } = string.Empty;
}
