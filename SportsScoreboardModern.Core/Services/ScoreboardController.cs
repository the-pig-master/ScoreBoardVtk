using System.Globalization;
using SportsScoreboardModern.Core.Models;

namespace SportsScoreboardModern.Core.Services;

public sealed class ScoreboardController
{
    private const int MaxScore = 999;
    private const int MaxPenaltyVolleyball = 9;
    private const int MaxPenaltyBasketball = 9;

    private readonly AppSettings _settings;

    private int _presetTenths;
    private int _mainClockTenths;
    private int _scoreA;
    private int _scoreB;
    private int _period;
    private int _penaltyA;
    private int _penaltyB;
    private int _mainSignalRemainingSeconds;
    private int _shotClockRemainingTenths;
    private int _shotClockSignalRemainingTenths;
    private bool _isGameClockRunning;
    private bool _isShotClockRunning;
    private bool _isManualSignalActive;

    public ScoreboardController(AppSettings settings)
    {
        _settings = settings;
        ApplySettingsDefaults();
        ResetForCurrentMode();
        RefreshDisplay();
    }

    public event EventHandler<ScoreboardSnapshot>? StateChanged;

    public AppSettings Settings => _settings;

    public ScoreboardSnapshot Snapshot { get; private set; } = default!;

    public void RefreshDisplay()
    {
        Snapshot = BuildSnapshot();
        StateChanged?.Invoke(this, Snapshot);
    }

    public void SetGameMode(GameMode gameMode)
    {
        _settings.GameMode = gameMode;
        ResetForCurrentMode();
        RefreshDisplay();
    }

    public void SetFontMode(FontMode fontMode)
    {
        _settings.FontMode = fontMode;
        RefreshDisplay();
    }

    public void SetTimerDirection(TimerDirection timerDirection)
    {
        if (_isGameClockRunning)
        {
            return;
        }

        _settings.TimerDirection = timerDirection;
        ResetMainClockOnly();
        RefreshDisplay();
    }

    public void SetCountFoulsToFive(bool enabled)
    {
        _settings.CountFoulsToFive = enabled;

        if (_settings.GameMode == GameMode.Basketball)
        {
            _penaltyA = NormalizeBasketballPenalty(_penaltyA);
            _penaltyB = NormalizeBasketballPenalty(_penaltyB);
        }

        RefreshDisplay();
    }

    public void SetAutoStartShotClock(bool enabled)
    {
        _settings.AutoStartShotClock = enabled;
        RefreshDisplay();
    }

    public void SetMainSignalDurationSeconds(int seconds)
    {
        _settings.MainSignalDurationSeconds = Math.Clamp(seconds, 0, 9);
        RefreshDisplay();
    }

    public void SetShotClockSignalDurationTenths(int tenths)
    {
        _settings.ShotClockSignalDurationTenths = Math.Clamp(tenths, 5, 30);
        RefreshDisplay();
    }

    public void SetRunningTextEnabled(bool enabled)
    {
        _settings.RunningTextEnabled = enabled;
        RefreshDisplay();
    }

    public void SetRunningText(string text)
    {
        _settings.RunningText = (text ?? string.Empty).TrimEnd();
        RefreshDisplay();
    }

    public void SetTimerPreset(int minutes, int seconds, int tenths)
    {
        if (_isGameClockRunning)
        {
            return;
        }

        minutes = Math.Clamp(minutes, 0, 59);
        seconds = Math.Clamp(seconds, 0, 59);
        tenths = Math.Clamp(tenths, 0, 9);

        _presetTenths = (minutes * 60 * 10) + (seconds * 10) + tenths;
        _settings.GameTimePreset = FormatPreset(_presetTenths);

        ResetMainClockOnly();
        RefreshDisplay();
    }

    public void IncreaseScoreA()
    {
        _scoreA = (_scoreA + 1) % (MaxScore + 1);
        RefreshDisplay();
    }

    public void DecreaseScoreA()
    {
        _scoreA = _scoreA == 0 ? MaxScore : _scoreA - 1;
        RefreshDisplay();
    }

    public void IncreaseScoreB()
    {
        _scoreB = (_scoreB + 1) % (MaxScore + 1);
        RefreshDisplay();
    }

    public void DecreaseScoreB()
    {
        _scoreB = _scoreB == 0 ? MaxScore : _scoreB - 1;
        RefreshDisplay();
    }

    public void IncreasePenaltyA()
    {
        if (_settings.GameMode != GameMode.Basketball)
        {
            return;
        }

        _penaltyA = NormalizeBasketballPenalty(_penaltyA + 1);
        RefreshDisplay();
    }

    public void DecreasePenaltyA()
    {
        if (_settings.GameMode != GameMode.Basketball)
        {
            return;
        }

        var maxPenalty = _settings.CountFoulsToFive ? 5 : MaxPenaltyBasketball;
        _penaltyA = _penaltyA == 0 ? maxPenalty : _penaltyA - 1;
        RefreshDisplay();
    }

    public void IncreasePenaltyB()
    {
        if (_settings.GameMode != GameMode.Basketball)
        {
            return;
        }

        _penaltyB = NormalizeBasketballPenalty(_penaltyB + 1);
        RefreshDisplay();
    }

    public void DecreasePenaltyB()
    {
        if (_settings.GameMode != GameMode.Basketball)
        {
            return;
        }

        var maxPenalty = _settings.CountFoulsToFive ? 5 : MaxPenaltyBasketball;
        _penaltyB = _penaltyB == 0 ? maxPenalty : _penaltyB - 1;
        RefreshDisplay();
    }

    public void ToggleGameClock()
    {
        if (_settings.GameMode != GameMode.Basketball)
        {
            return;
        }

        if (_isGameClockRunning)
        {
            StopGameClockInternal();
        }
        else
        {
            _isGameClockRunning = true;
        }

        RefreshDisplay();
    }

    public void StopGameClock()
    {
        if (_settings.GameMode != GameMode.Basketball)
        {
            return;
        }

        StopGameClockInternal();
        RefreshDisplay();
    }

    public void Reset()
    {
        if (_settings.GameMode == GameMode.Basketball && _isGameClockRunning)
        {
            return;
        }

        ResetForCurrentMode();
        RefreshDisplay();
    }

    public void AdvancePeriodOrSet()
    {
        if (_settings.GameMode == GameMode.Basketball)
        {
            _period = _period switch
            {
                < 4 => _period + 1,
                4 => 6,
                _ => 1,
            };
        }
        else if (_period < 5)
        {
            _period++;

            if (_scoreA > _scoreB)
            {
                _penaltyA = (_penaltyA + 1) % (MaxPenaltyVolleyball + 1);
                _scoreA = 0;
                _scoreB = 0;
            }
            else if (_scoreB > _scoreA)
            {
                _penaltyB = (_penaltyB + 1) % (MaxPenaltyVolleyball + 1);
                _scoreA = 0;
                _scoreB = 0;
            }
        }

        RefreshDisplay();
    }

    public void SetShotClock24()
    {
        SetShotClock(24);
    }

    public void SetShotClock14()
    {
        SetShotClock(14);
    }

    public void ToggleShotClock()
    {
        if (_settings.GameMode != GameMode.Basketball)
        {
            return;
        }

        if (_isShotClockRunning)
        {
            _isShotClockRunning = false;
        }
        else
        {
            if (_shotClockRemainingTenths <= 0)
            {
                InitializeShotClock(24, respectAutoStart: false);
            }

            _shotClockRemainingTenths = ClampShotClockTenths(_shotClockRemainingTenths);
            _isShotClockRunning = _shotClockRemainingTenths > 0;
        }

        RefreshDisplay();
    }

    public void StartManualSignal()
    {
        _isManualSignalActive = true;
        RefreshDisplay();
    }

    public void StopManualSignal()
    {
        _isManualSignalActive = false;
        RefreshDisplay();
    }

    public void TickMainClock()
    {
        if (_settings.GameMode != GameMode.Basketball || !_isGameClockRunning)
        {
            return;
        }

        if (_settings.TimerDirection == TimerDirection.Down)
        {
            if (_mainClockTenths > 0)
            {
                _mainClockTenths--;
            }

            if (_isShotClockRunning)
            {
                TickShotClock();
            }

            if (_mainClockTenths <= 0)
            {
                HandlePeriodCompleted();
            }
        }
        else
        {
            _mainClockTenths++;

            if (_isShotClockRunning)
            {
                TickShotClock();
            }

            if (_mainClockTenths >= _presetTenths)
            {
                HandlePeriodCompleted();
            }
        }

        RefreshDisplay();
    }

    public void TickMainSignal()
    {
        if (_mainSignalRemainingSeconds <= 0)
        {
            return;
        }

        _mainSignalRemainingSeconds--;
        RefreshDisplay();
    }

    public void TickShotClockSignal()
    {
        if (_shotClockSignalRemainingTenths <= 0)
        {
            return;
        }

        _shotClockSignalRemainingTenths--;
        RefreshDisplay();
    }

    private void ApplySettingsDefaults()
    {
        _settings.MainSignalDurationSeconds = Math.Clamp(_settings.MainSignalDurationSeconds, 0, 9);
        _settings.ShotClockSignalDurationTenths = Math.Clamp(_settings.ShotClockSignalDurationTenths, 5, 30);
        _settings.GameTimePreset = string.IsNullOrWhiteSpace(_settings.GameTimePreset) ? "10:00" : _settings.GameTimePreset;
        _presetTenths = ParsePresetTenths(_settings.GameTimePreset);
    }

    private void ResetForCurrentMode()
    {
        _scoreA = 0;
        _scoreB = 0;
        _penaltyA = 0;
        _penaltyB = 0;
        _period = 1;
        _mainSignalRemainingSeconds = 0;
        _shotClockSignalRemainingTenths = 0;
        _isManualSignalActive = false;
        _isGameClockRunning = false;
        _isShotClockRunning = false;

        ResetMainClockOnly();
    }

    private void ResetMainClockOnly()
    {
        _presetTenths = ParsePresetTenths(_settings.GameTimePreset);
        _mainClockTenths = _settings.TimerDirection == TimerDirection.Down ? _presetTenths : 0;
        InitializeShotClock(24, respectAutoStart: false);
    }

    private void StopGameClockInternal()
    {
        _isGameClockRunning = false;
        _isShotClockRunning = false;
        _shotClockSignalRemainingTenths = 0;
    }

    private void HandlePeriodCompleted()
    {
        _isGameClockRunning = false;
        _isShotClockRunning = false;
        _mainSignalRemainingSeconds = _settings.MainSignalDurationSeconds;

        _mainClockTenths = _settings.TimerDirection == TimerDirection.Down ? _presetTenths : 0;
    }

    private void SetShotClock(int seconds)
    {
        if (_settings.GameMode != GameMode.Basketball)
        {
            return;
        }

        InitializeShotClock(seconds, respectAutoStart: true);
        _shotClockSignalRemainingTenths = 0;
        RefreshDisplay();
    }

    private void InitializeShotClock(int seconds, bool respectAutoStart)
    {
        var tenths = Math.Max(0, seconds * 10);
        _shotClockRemainingTenths = ClampShotClockTenths(tenths);
        _isShotClockRunning = respectAutoStart && _settings.AutoStartShotClock && _shotClockRemainingTenths > 0;
    }

    private int ClampShotClockTenths(int requestedTenths)
    {
        if (_settings.GameMode != GameMode.Basketball)
        {
            return 0;
        }

        if (_settings.TimerDirection != TimerDirection.Down || requestedTenths <= 0)
        {
            return requestedTenths;
        }

        if (_mainClockTenths <= 0)
        {
            return requestedTenths;
        }

        return Math.Min(requestedTenths, _mainClockTenths);
    }

    private void TickShotClock()
    {
        if (_shotClockRemainingTenths <= 0)
        {
            _isShotClockRunning = false;
            return;
        }

        _shotClockRemainingTenths--;

        if (_shotClockRemainingTenths <= 0)
        {
            _isShotClockRunning = false;
            _shotClockSignalRemainingTenths = _settings.ShotClockSignalDurationTenths;
            InitializeShotClock(24, respectAutoStart: true);
        }
    }

    private int NormalizeBasketballPenalty(int value)
    {
        var maxPenalty = _settings.CountFoulsToFive ? 5 : MaxPenaltyBasketball;

        if (value < 0)
        {
            return maxPenalty;
        }

        return value > maxPenalty ? 0 : value;
    }

    private ScoreboardSnapshot BuildSnapshot()
    {
        var payload = BuildLegacyPayload();
        var shotClockRemaining = _settings.GameMode == GameMode.Basketball ? _shotClockRemainingTenths : 0;
        var shotClockSeconds = shotClockRemaining <= 0 ? 0 : (shotClockRemaining + 9) / 10;
        var shotClockTenths = shotClockRemaining % 10;

        return new ScoreboardSnapshot(
            _settings.GameMode,
            _settings.TimerDirection,
            _settings.FontMode,
            _scoreA.ToString("000", CultureInfo.InvariantCulture),
            _scoreB.ToString("000", CultureInfo.InvariantCulture),
            _settings.GameMode == GameMode.Basketball && _period == 6 ? "E" : _period.ToString(CultureInfo.InvariantCulture),
            FormatMainClock(),
            _penaltyA.ToString(CultureInfo.InvariantCulture),
            _penaltyB.ToString(CultureInfo.InvariantCulture),
            _settings.GameMode == GameMode.Basketball ? "FOULS" : "SETS",
            shotClockSeconds.ToString("00", CultureInfo.InvariantCulture),
            shotClockTenths.ToString(CultureInfo.InvariantCulture),
            FormatPreset(_presetTenths),
            _settings.RunningText,
            _settings.RunningTextEnabled,
            _settings.CountFoulsToFive,
            _settings.AutoStartShotClock,
            _isGameClockRunning,
            _isShotClockRunning,
            _isManualSignalActive || _mainSignalRemainingSeconds > 0,
            _shotClockSignalRemainingTenths > 0,
            _isGameClockRunning ? "Stop" : "Start",
            _isShotClockRunning ? "Stop" : "Start",
            payload);
    }

    private string BuildLegacyPayload()
    {
        var scoreAText = _scoreA.ToString(CultureInfo.InvariantCulture).PadLeft(3, ' ');
        var scoreBText = _scoreB.ToString(CultureInfo.InvariantCulture).PadLeft(3, ' ');
        var periodText = _settings.GameMode == GameMode.Basketball && _period == 6
            ? "E"
            : _period.ToString(CultureInfo.InvariantCulture);
        var mainClockText = BuildLegacyGameTimeText();
        var penaltyAText = _penaltyA.ToString(CultureInfo.InvariantCulture);
        var penaltyBText = _penaltyB.ToString(CultureInfo.InvariantCulture);
        var signal = _isManualSignalActive || _mainSignalRemainingSeconds > 0 ? "S" : " ";
        var font = _settings.FontMode == FontMode.Font8x8 ? "1" : "0";
        var runningTextState = _settings.RunningTextEnabled ? "1" : "0";
        var shotClockText = BuildLegacyShotClockText();
        var shotClockSignal = _shotClockSignalRemainingTenths > 0 ? "S" : " ";
        var runningText = _settings.RunningText;

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
            runningText;
    }

    private string BuildLegacyShotClockText()
    {
        if (_settings.GameMode != GameMode.Basketball)
        {
            return "  ";
        }

        var shotClockSeconds = _shotClockRemainingTenths <= 0 ? 0 : (_shotClockRemainingTenths + 9) / 10;

        if (shotClockSeconds == 0)
        {
            return "00";
        }

        return shotClockSeconds < 10
            ? $" {shotClockSeconds.ToString(CultureInfo.InvariantCulture)}"
            : shotClockSeconds.ToString(CultureInfo.InvariantCulture);
    }

    private string BuildLegacyGameTimeText()
    {
        if (_settings.GameMode != GameMode.Basketball)
        {
            return DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);
        }

        if (_settings.TimerDirection == TimerDirection.Down)
        {
            if (_mainClockTenths >= 600)
            {
                var totalDisplaySeconds = (_mainClockTenths + 9) / 10;
                var minutes = totalDisplaySeconds / 60;
                var displaySeconds = totalDisplaySeconds % 60;
                return $"{minutes.ToString(CultureInfo.InvariantCulture)}:{displaySeconds:00}";
            }

            var secondsUnderMinute = _mainClockTenths / 10;
            var tenths = _mainClockTenths % 10;
            return $"{secondsUnderMinute.ToString(CultureInfo.InvariantCulture)}:{tenths.ToString(CultureInfo.InvariantCulture)} ";
        }

        if (_mainClockTenths >= 600)
        {
            var totalDisplaySeconds = _mainClockTenths / 10;
            var minutes = totalDisplaySeconds / 60;
            var displaySeconds = totalDisplaySeconds % 60;
            return $"{minutes.ToString(CultureInfo.InvariantCulture)}:{displaySeconds:00}";
        }

        var visibleSeconds = _mainClockTenths / 10;
        var subSecond = _mainClockTenths % 10;
        return $"{visibleSeconds.ToString(CultureInfo.InvariantCulture)}:{subSecond.ToString(CultureInfo.InvariantCulture)} ";
    }

    private string FormatMainClock()
    {
        if (_settings.GameMode != GameMode.Basketball)
        {
            return DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);
        }

        if (_settings.TimerDirection == TimerDirection.Down)
        {
            if (_mainClockTenths >= 600)
            {
                var totalDisplaySeconds = (_mainClockTenths + 9) / 10;
                return TimeSpan.FromSeconds(totalDisplaySeconds).ToString(@"mm\:ss", CultureInfo.InvariantCulture);
            }

            var seconds = _mainClockTenths / 10;
            var tenths = _mainClockTenths % 10;
            return $"{seconds:00}.{tenths}";
        }

        if (_mainClockTenths >= 600)
        {
            var totalDisplaySeconds = _mainClockTenths / 10;
            return TimeSpan.FromSeconds(totalDisplaySeconds).ToString(@"mm\:ss", CultureInfo.InvariantCulture);
        }

        var secondsUnderMinute = _mainClockTenths / 10;
        var subSecond = _mainClockTenths % 10;
        return $"{secondsUnderMinute:00}.{subSecond}";
    }

    private static int ParsePresetTenths(string preset)
    {
        if (string.IsNullOrWhiteSpace(preset))
        {
            return 10 * 60 * 10;
        }

        var tenths = 0;
        var trimmed = preset.Trim();
        var dotIndex = trimmed.IndexOfAny(['.', ',']);

        if (dotIndex >= 0 && dotIndex < trimmed.Length - 1 && char.IsDigit(trimmed[dotIndex + 1]))
        {
            tenths = trimmed[dotIndex + 1] - '0';
            trimmed = trimmed[..dotIndex];
        }

        if (!TimeSpan.TryParseExact(trimmed, ["m\\:ss", "mm\\:ss"], CultureInfo.InvariantCulture, out var timeSpan))
        {
            return 10 * 60 * 10;
        }

        var totalTenths = ((int)timeSpan.TotalMinutes * 60 * 10) + (timeSpan.Seconds * 10) + tenths;
        return Math.Max(totalTenths, 0);
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
