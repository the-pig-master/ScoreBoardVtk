using System.Globalization;
using ScoreBoardVtk.Core.Models;

namespace ScoreBoardVtk.Core.Services;

public sealed class ScoreboardController
{
    private const int MaxScore = 999;
    private const int MaxPenaltyVolleyball = 9;
    private const int MaxPenaltyBasketball = 9;
    private static readonly TimeSpan TenthInterval = TimeSpan.FromMilliseconds(100);

    private readonly AppSettings _settings;
    private readonly TimeProvider _timeProvider;

    private int _presetTenths;
    private int _mainClockTenths;
    private int _scoreA;
    private int _scoreB;
    private int _period;
    private int _penaltyA;
    private int _penaltyB;
    private int _mainSignalRemainingTenths;
    private int _shotClockRemainingTenths;
    private int _shotClockSignalRemainingTenths;
    private bool _isGameClockRunning;
    private bool _isShotClockRunning;
    private bool _isManualSignalActive;
    private DateTimeOffset? _lastTimingTimestamp;
    private TimeSpan _timingRemainder = TimeSpan.Zero;

    public ScoreboardController(AppSettings settings, TimeProvider? timeProvider = null)
    {
        _settings = settings;
        _timeProvider = timeProvider ?? TimeProvider.System;
        ApplySettingsDefaults();
        ResetForCurrentMode();
        RefreshDisplay();
    }

    public event EventHandler<ScoreboardState>? StateChanged;

    public AppSettings Settings => _settings;

    public ScoreboardState State { get; private set; } = default!;

    public void Apply(ScoreboardCommand command)
    {
        if (command is not (TickMainClockCommand or TickMainSignalCommand or TickShotClockSignalCommand or RefreshDisplayCommand))
        {
            SynchronizeElapsedTime();
        }

        switch (command)
        {
            case SetGameModeCommand typed:
                SetGameMode(typed.GameMode);
                break;

            case SetFontModeCommand typed:
                SetFontMode(typed.FontMode);
                break;

            case SetTimerDirectionCommand typed:
                SetTimerDirection(typed.TimerDirection);
                break;

            case SetCountFoulsToFiveCommand typed:
                SetCountFoulsToFive(typed.Enabled);
                break;

            case SetMainSignalDurationSecondsCommand typed:
                SetMainSignalDurationSeconds(typed.Seconds);
                break;

            case SetShotClockSignalDurationTenthsCommand typed:
                SetShotClockSignalDurationTenths(typed.Tenths);
                break;

            case SetRunningTextEnabledCommand typed:
                SetRunningTextEnabled(typed.Enabled);
                break;

            case SetRunningTextCommand typed:
                SetRunningText(typed.Text);
                break;

            case SetTimerPresetCommand typed:
                SetTimerPreset(typed.Minutes, typed.Seconds, typed.Tenths);
                break;

            case SetOvertimeTimerPresetCommand typed:
                SetOvertimeTimerPreset(typed.Minutes, typed.Seconds, typed.Tenths);
                break;

            case SetScoreboardValuesCommand typed:
                SetScoreboardValues(
                    typed.HomeScore,
                    typed.GuestScore,
                    typed.HomeSecondaryCounter,
                    typed.GuestSecondaryCounter,
                    typed.PeriodNumber,
                    typed.MainClockTenths,
                    typed.ShotClockTenths);
                break;

            case ChangeScoreCommand typed:
                ApplyScoreChange(typed.Side, typed.Delta);
                break;

            case ChangeSecondaryCounterCommand typed:
                ApplySecondaryCounterChange(typed.Side, typed.Delta);
                break;

            case SetShotClockCommand typed:
                SetShotClock(typed.Seconds);
                break;

            case RunShotClockCommand typed:
                RunShotClock(typed.Seconds);
                break;

            case SetManualSignalCommand typed:
                if (typed.IsActive)
                {
                    StartManualSignal();
                }
                else
                {
                    StopManualSignal();
                }

                break;

            case ToggleGameClockCommand:
                ToggleGameClock();
                break;

            case StopGameClockCommand:
                StopGameClock();
                break;

            case AdvancePeriodOrSetCommand:
                AdvancePeriodOrSet();
                break;

            case ResetScoreboardCommand:
                Reset();
                break;

            case ToggleShotClockCommand:
                ToggleShotClock();
                break;

            case TickMainClockCommand:
                TickMainClock();
                break;

            case TickMainSignalCommand:
                TickMainSignal();
                break;

            case TickShotClockSignalCommand:
                TickShotClockSignal();
                break;

            case RefreshDisplayCommand:
                RefreshDisplay();
                break;

            default:
                throw new NotSupportedException($"Unsupported scoreboard command: {command.GetType().Name}");
        }
    }

    public void RefreshDisplay()
    {
        UpdateTimingTrackingState();
        State = BuildState();
        StateChanged?.Invoke(this, State);
    }

    public void SetGameMode(GameMode gameMode)
    {
        _settings.GameMode = gameMode;

        if (gameMode == GameMode.Basketball)
        {
            _settings.TimerDirection = TimerDirection.Down;
        }

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

        if (_settings.GameMode == GameMode.Basketball)
        {
            timerDirection = TimerDirection.Down;
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

    public void SetOvertimeTimerPreset(int minutes, int seconds, int tenths)
    {
        minutes = Math.Clamp(minutes, 0, 59);
        seconds = Math.Clamp(seconds, 0, 59);
        tenths = Math.Clamp(tenths, 0, 9);

        var overtimePresetTenths = (minutes * 60 * 10) + (seconds * 10) + tenths;
        _settings.OvertimeTimePreset = FormatPreset(overtimePresetTenths);

        if (_settings.GameMode == GameMode.Basketball && _period > 4 && !_isGameClockRunning)
        {
            ResetMainClockOnly();
        }

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

        SynchronizeElapsedTime();

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

        SynchronizeElapsedTime();
        StopGameClockInternal();
        RefreshDisplay();
    }

    public void Reset()
    {
        if (_settings.GameMode == GameMode.Basketball)
        {
            if (_isGameClockRunning || _isShotClockRunning)
            {
                return;
            }

            ResetBasketballTimersOnly();
            RefreshDisplay();
            return;
        }

        ResetForCurrentMode();
        RefreshDisplay();
    }

    public void AdvancePeriodOrSet()
    {
        if (_settings.GameMode == GameMode.Basketball)
        {
            if (_isGameClockRunning)
            {
                return;
            }

            _period = _period switch
            {
                < 4 => _period + 1,
                4 => 5,
                _ => 1,
            };

            ResetMainClockOnly();
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

        SynchronizeElapsedTime();

        if (_isShotClockRunning)
        {
            _isShotClockRunning = false;
        }
        else
        {
            if (_shotClockRemainingTenths <= 0)
            {
                InitializeShotClock(24, startRunning: false);
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

    public void SetScoreboardValues(
        int homeScore,
        int guestScore,
        int homeSecondaryCounter,
        int guestSecondaryCounter,
        int periodNumber,
        int mainClockTenths,
        int shotClockTenths)
    {
        _scoreA = Math.Clamp(homeScore, 0, MaxScore);
        _scoreB = Math.Clamp(guestScore, 0, MaxScore);

        if (_settings.GameMode == GameMode.Basketball)
        {
            _period = Math.Clamp(periodNumber, 1, 5);
            _penaltyA = NormalizeBasketballPenalty(homeSecondaryCounter);
            _penaltyB = NormalizeBasketballPenalty(guestSecondaryCounter);
            _mainClockTenths = Math.Clamp(mainClockTenths, 0, 59 * 60 * 10 + 59 * 10 + 9);
            _shotClockRemainingTenths = ClampShotClockTenths(Math.Clamp(shotClockTenths, 0, 24 * 10));
        }
        else
        {
            _period = Math.Clamp(periodNumber, 1, 5);
            _penaltyA = Math.Clamp(homeSecondaryCounter, 0, MaxPenaltyVolleyball);
            _penaltyB = Math.Clamp(guestSecondaryCounter, 0, MaxPenaltyVolleyball);
            _mainClockTenths = Math.Clamp(mainClockTenths, 0, 59 * 60 * 10 + 59 * 10 + 9);
            _shotClockRemainingTenths = 0;
        }

        _mainSignalRemainingTenths = 0;
        _shotClockSignalRemainingTenths = 0;
        _isManualSignalActive = false;
        _isGameClockRunning = false;
        _isShotClockRunning = false;
        _lastTimingTimestamp = null;
        _timingRemainder = TimeSpan.Zero;

        RefreshDisplay();
    }

    public void TickMainClock()
    {
        SynchronizeElapsedTime();
        RefreshDisplay();
    }

    public void TickMainSignal()
    {
        SynchronizeElapsedTime();
        RefreshDisplay();
    }

    public void TickShotClockSignal()
    {
        SynchronizeElapsedTime();
        RefreshDisplay();
    }

    private void ApplySettingsDefaults()
    {
        _settings.MainSignalDurationSeconds = Math.Clamp(_settings.MainSignalDurationSeconds, 0, 9);
        _settings.ShotClockSignalDurationTenths = Math.Clamp(_settings.ShotClockSignalDurationTenths, 5, 30);
        _settings.GameTimePreset = string.IsNullOrWhiteSpace(_settings.GameTimePreset) ? "10:00" : _settings.GameTimePreset;
        _settings.OvertimeTimePreset = string.IsNullOrWhiteSpace(_settings.OvertimeTimePreset) ? "05:00" : _settings.OvertimeTimePreset;

        if (_settings.GameMode == GameMode.Basketball)
        {
            _settings.TimerDirection = TimerDirection.Down;
        }

        _presetTenths = GetCurrentPeriodPresetTenths();
    }

    private void ResetForCurrentMode()
    {
        _scoreA = 0;
        _scoreB = 0;
        _penaltyA = 0;
        _penaltyB = 0;
        _period = 1;
        _mainSignalRemainingTenths = 0;
        _shotClockSignalRemainingTenths = 0;
        _isManualSignalActive = false;
        _isGameClockRunning = false;
        _isShotClockRunning = false;
        _lastTimingTimestamp = null;
        _timingRemainder = TimeSpan.Zero;

        ResetMainClockOnly();
    }

    private void ResetMainClockOnly()
    {
        _presetTenths = GetCurrentPeriodPresetTenths();
        _mainClockTenths = _settings.TimerDirection == TimerDirection.Down ? _presetTenths : 0;
        InitializeShotClock(24, startRunning: false);
    }

    private void ResetBasketballTimersOnly()
    {
        _mainSignalRemainingTenths = 0;
        _shotClockSignalRemainingTenths = 0;
        _isManualSignalActive = false;
        _isGameClockRunning = false;
        _isShotClockRunning = false;
        _lastTimingTimestamp = null;
        _timingRemainder = TimeSpan.Zero;
        ResetMainClockOnly();
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
        _mainSignalRemainingTenths = _settings.MainSignalDurationSeconds * 10;

        if (_settings.TimerDirection == TimerDirection.Down)
        {
            _mainClockTenths = 0;
        }
    }

    private void SetShotClock(int seconds)
    {
        if (_settings.GameMode != GameMode.Basketball)
        {
            return;
        }

        InitializeShotClock(seconds, startRunning: false);
        _shotClockSignalRemainingTenths = 0;
        RefreshDisplay();
    }

    private void RunShotClock(int seconds)
    {
        if (_settings.GameMode != GameMode.Basketball)
        {
            return;
        }

        InitializeShotClock(seconds, startRunning: true);
        _shotClockSignalRemainingTenths = 0;
        RefreshDisplay();
    }

    private void InitializeShotClock(int seconds, bool startRunning)
    {
        var tenths = Math.Max(0, seconds * 10);
        _shotClockRemainingTenths = ClampShotClockTenths(tenths);
        _isShotClockRunning = startRunning && _shotClockRemainingTenths > 0;
    }

    private void ApplyScoreChange(TeamSide side, int delta)
    {
        if (delta == 0)
        {
            RefreshDisplay();
            return;
        }

        if (side == TeamSide.Home)
        {
            ApplyCounterDelta(delta, IncreaseScoreA, DecreaseScoreA);
        }
        else
        {
            ApplyCounterDelta(delta, IncreaseScoreB, DecreaseScoreB);
        }
    }

    private void ApplySecondaryCounterChange(TeamSide side, int delta)
    {
        if (delta == 0)
        {
            RefreshDisplay();
            return;
        }

        if (side == TeamSide.Home)
        {
            ApplyCounterDelta(delta, IncreasePenaltyA, DecreasePenaltyA);
        }
        else
        {
            ApplyCounterDelta(delta, IncreasePenaltyB, DecreasePenaltyB);
        }
    }

    private static void ApplyCounterDelta(int delta, Action increment, Action decrement)
    {
        var action = delta > 0 ? increment : decrement;

        for (var index = 0; index < Math.Abs(delta); index++)
        {
            action();
        }
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

    private void SynchronizeElapsedTime()
    {
        if (!HasActiveTimedState())
        {
            _lastTimingTimestamp = null;
            _timingRemainder = TimeSpan.Zero;
            return;
        }

        var now = _timeProvider.GetUtcNow();

        if (_lastTimingTimestamp is null)
        {
            _lastTimingTimestamp = now;
            return;
        }

        var elapsed = now - _lastTimingTimestamp.Value;
        _lastTimingTimestamp = now;
        _timingRemainder += elapsed;

        var elapsedTenths = 0;

        while (_timingRemainder >= TenthInterval)
        {
            _timingRemainder -= TenthInterval;
            elapsedTenths++;
        }

        if (elapsedTenths <= 0)
        {
            return;
        }

        AdvanceTimedState(elapsedTenths);
    }

    private void AdvanceTimedState(int elapsedTenths)
    {
        for (var index = 0; index < elapsedTenths; index++)
        {
            if (_mainSignalRemainingTenths > 0)
            {
                _mainSignalRemainingTenths--;
            }

            if (_shotClockSignalRemainingTenths > 0)
            {
                _shotClockSignalRemainingTenths--;
            }

            if (_settings.GameMode != GameMode.Basketball || !_isGameClockRunning)
            {
                continue;
            }

            if (_settings.TimerDirection == TimerDirection.Down)
            {
                if (_mainClockTenths > 0)
                {
                    _mainClockTenths--;
                }

                AdvanceShotClockOneTenth();

                if (_mainClockTenths <= 0)
                {
                    HandlePeriodCompleted();
                }
            }
            else
            {
                _mainClockTenths++;
                AdvanceShotClockOneTenth();

                if (_mainClockTenths >= _presetTenths)
                {
                    HandlePeriodCompleted();
                }
            }
        }
    }

    private void AdvanceShotClockOneTenth()
    {
        if (!_isShotClockRunning)
        {
            return;
        }

        if (_shotClockRemainingTenths <= 0)
        {
            _shotClockRemainingTenths = 0;
            _isShotClockRunning = false;
            return;
        }

        _shotClockRemainingTenths--;

        if (_shotClockRemainingTenths <= 0)
        {
            _shotClockRemainingTenths = 0;
            _isShotClockRunning = false;
            _shotClockSignalRemainingTenths = _settings.ShotClockSignalDurationTenths;
        }
    }

    private void UpdateTimingTrackingState()
    {
        if (HasActiveTimedState())
        {
            _lastTimingTimestamp ??= _timeProvider.GetUtcNow();
            return;
        }

        _lastTimingTimestamp = null;
        _timingRemainder = TimeSpan.Zero;
    }

    private bool HasActiveTimedState()
    {
        return (_settings.GameMode == GameMode.Basketball && _isGameClockRunning) ||
               _mainSignalRemainingTenths > 0 ||
               _shotClockSignalRemainingTenths > 0;
    }

    private int GetCurrentPeriodPresetTenths()
    {
        if (_settings.GameMode == GameMode.Basketball && _period > 4)
        {
            return ParsePresetTenths(_settings.OvertimeTimePreset, 5 * 60 * 10);
        }

        return ParsePresetTenths(_settings.GameTimePreset, 10 * 60 * 10);
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

    private ScoreboardState BuildState()
    {
        return new ScoreboardState(
            _settings.GameMode,
            _settings.TimerDirection,
            _settings.FontMode,
            _scoreA,
            _scoreB,
            _period,
            _settings.GameMode == GameMode.Basketball ? SecondaryCounterKind.Fouls : SecondaryCounterKind.Sets,
            _penaltyA,
            _penaltyB,
            _mainClockTenths,
            _presetTenths,
            (_mainSignalRemainingTenths + 9) / 10,
            _settings.GameMode == GameMode.Basketball ? _shotClockRemainingTenths : 0,
            _shotClockSignalRemainingTenths,
            _settings.RunningText,
            _settings.RunningTextEnabled,
            _settings.CountFoulsToFive,
            _isGameClockRunning,
            _isShotClockRunning,
            _isManualSignalActive,
            _isManualSignalActive || _mainSignalRemainingTenths > 0,
            _shotClockSignalRemainingTenths > 0);
    }

    private static int ParsePresetTenths(string preset, int defaultTenths)
    {
        if (string.IsNullOrWhiteSpace(preset))
        {
            return defaultTenths;
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
            return defaultTenths;
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
