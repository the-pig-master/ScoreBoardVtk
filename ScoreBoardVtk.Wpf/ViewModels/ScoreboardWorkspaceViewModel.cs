using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using ScoreBoardVtk.Core.Models;
using ScoreBoardVtk.Core.Services;
using ScoreBoardVtk.Wpf.Infrastructure;
using ScoreBoardVtk.Wpf.Models;

namespace ScoreBoardVtk.Wpf.ViewModels;

public sealed class ScoreboardWorkspaceViewModel : ObservableObject, IDisposable
{
    private readonly SettingsStore _settingsStore;
    private readonly IScoreboardApi _scoreboard;
    private readonly IScoreboardRuntime _runtime;
    private readonly Dispatcher _dispatcher;

    private bool _suppressControllerSync;
    private ScoreboardState _currentState = null!;
    private ScoreboardSnapshot _currentSnapshot = null!;
    private string _selectedPort = string.Empty;
    private bool _isConnected;
    private string _connectedPortName = string.Empty;
    private DateTime _hostClock = DateTime.Now;
    private string _systemMessageText = "Ready.";
    private string _runningText = string.Empty;
    private bool _runningTextEnabled;
    private bool _countFoulsToFive = true;
    private bool _autoStartShotClock;
    private int _mainSignalDurationSeconds = 3;
    private int _shotClockSignalDurationTenths = 15;
    private int _presetMinutes = 10;
    private int _presetSeconds;
    private int _presetTenths;
    private int _overtimePresetMinutes = 5;
    private int _overtimePresetSeconds;
    private int _overtimePresetTenths;
    private OptionItem<GameMode>? _selectedGameMode;
    private OptionItem<FontMode>? _selectedFontMode;
    private OptionItem<TimerDirection>? _selectedTimerDirection;

    public ScoreboardWorkspaceViewModel(SettingsStore settingsStore, IScoreboardApi scoreboard, IScoreboardRuntime runtime)
    {
        _settingsStore = settingsStore;
        _scoreboard = scoreboard;
        _runtime = runtime;
        _dispatcher = Dispatcher.CurrentDispatcher;

        AvailablePorts = new ObservableCollection<string>();
        GameModes =
        [
            new OptionItem<GameMode>(GameMode.Basketball, "Basketball"),
            new OptionItem<GameMode>(GameMode.Volleyball, "Volleyball"),
        ];
        FontModes =
        [
            new OptionItem<FontMode>(FontMode.Font6x8, "Font 6x8"),
            new OptionItem<FontMode>(FontMode.Font8x8, "Font 8x8"),
        ];
        TimerDirections =
        [
            new OptionItem<TimerDirection>(TimerDirection.Up, "Count Up"),
            new OptionItem<TimerDirection>(TimerDirection.Down, "Count Down"),
        ];
        PresetMinuteOptions = Enumerable.Range(0, 60).ToArray();
        PresetSecondOptions = Enumerable.Range(0, 60).ToArray();
        PresetTenthsOptions = Enumerable.Range(0, 10).ToArray();

        IncreaseScoreACommand = new RelayCommand(() => _scoreboard.Execute(new ChangeScoreCommand(TeamSide.Home, 1)));
        DecreaseScoreACommand = new RelayCommand(() => _scoreboard.Execute(new ChangeScoreCommand(TeamSide.Home, -1)));
        IncreaseScoreBCommand = new RelayCommand(() => _scoreboard.Execute(new ChangeScoreCommand(TeamSide.Guest, 1)));
        DecreaseScoreBCommand = new RelayCommand(() => _scoreboard.Execute(new ChangeScoreCommand(TeamSide.Guest, -1)));
        IncreasePenaltyACommand = new RelayCommand(() => _scoreboard.Execute(new ChangeSecondaryCounterCommand(TeamSide.Home, 1)));
        DecreasePenaltyACommand = new RelayCommand(() => _scoreboard.Execute(new ChangeSecondaryCounterCommand(TeamSide.Home, -1)));
        IncreasePenaltyBCommand = new RelayCommand(() => _scoreboard.Execute(new ChangeSecondaryCounterCommand(TeamSide.Guest, 1)));
        DecreasePenaltyBCommand = new RelayCommand(() => _scoreboard.Execute(new ChangeSecondaryCounterCommand(TeamSide.Guest, -1)));
        ToggleGameClockCommand = new RelayCommand(() => _scoreboard.Execute(new ToggleGameClockCommand()));
        StopGameClockCommand = new RelayCommand(() => _scoreboard.Execute(new StopGameClockCommand()));
        AdvancePeriodOrSetCommand = new RelayCommand(() => _scoreboard.Execute(new AdvancePeriodOrSetCommand()));
        ResetCommand = new RelayCommand(() => _scoreboard.Execute(new ResetScoreboardCommand()));
        SetShotClock24Command = new RelayCommand(() => _scoreboard.Execute(new SetShotClockCommand(24)));
        SetShotClock14Command = new RelayCommand(() => _scoreboard.Execute(new SetShotClockCommand(14)));
        ToggleShotClockCommand = new RelayCommand(() => _scoreboard.Execute(new ToggleShotClockCommand()));
        RefreshPortsCommand = new RelayCommand(() => RefreshPorts(SelectedPort));
        TogglePortCommand = new RelayCommand(TogglePort);
        SyncTimeCommand = new RelayCommand(SyncTime);
        ApplySettingsCommand = new RelayCommand(ApplySettings);

        _scoreboard.StateChanged += ScoreboardOnStateChanged;
        _runtime.Faulted += RuntimeOnFaulted;

        ApplySettingsFromController();
        RefreshPorts(_scoreboard.Settings.SelectedPort);
        UpdatePortState();
        RefreshScoreboardState(_scoreboard.State);
        _runtime.Start();
    }

    public ObservableCollection<string> AvailablePorts { get; }

    public IReadOnlyList<OptionItem<GameMode>> GameModes { get; }

    public IReadOnlyList<OptionItem<FontMode>> FontModes { get; }

    public IReadOnlyList<OptionItem<TimerDirection>> TimerDirections { get; }

    public IReadOnlyList<int> PresetMinuteOptions { get; }

    public IReadOnlyList<int> PresetSecondOptions { get; }

    public IReadOnlyList<int> PresetTenthsOptions { get; }

    public IReadOnlyList<int> SignalDurationOptions { get; } = Enumerable.Range(0, 10).ToArray();

    public IReadOnlyList<int> ShotClockSignalOptions { get; } = [5, 10, 15, 20, 25, 30];

    public ICommand IncreaseScoreACommand { get; }

    public ICommand DecreaseScoreACommand { get; }

    public ICommand IncreaseScoreBCommand { get; }

    public ICommand DecreaseScoreBCommand { get; }

    public ICommand IncreasePenaltyACommand { get; }

    public ICommand DecreasePenaltyACommand { get; }

    public ICommand IncreasePenaltyBCommand { get; }

    public ICommand DecreasePenaltyBCommand { get; }

    public ICommand ToggleGameClockCommand { get; }

    public ICommand StopGameClockCommand { get; }

    public ICommand AdvancePeriodOrSetCommand { get; }

    public ICommand ResetCommand { get; }

    public ICommand SetShotClock24Command { get; }

    public ICommand SetShotClock14Command { get; }

    public ICommand ToggleShotClockCommand { get; }

    public ICommand RefreshPortsCommand { get; }

    public ICommand TogglePortCommand { get; }

    public ICommand SyncTimeCommand { get; }

    public ICommand ApplySettingsCommand { get; }

    public ScoreboardState CurrentState
    {
        get => _currentState;
        private set => SetProperty(ref _currentState, value);
    }

    public ScoreboardSnapshot CurrentSnapshot
    {
        get => _currentSnapshot;
        private set => SetProperty(ref _currentSnapshot, value);
    }

    public string SelectedPort
    {
        get => _selectedPort;
        set => SetProperty(ref _selectedPort, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        private set => SetProperty(ref _isConnected, value);
    }

    public string ConnectedPortName
    {
        get => _connectedPortName;
        private set => SetProperty(ref _connectedPortName, value);
    }

    public DateTime HostClock
    {
        get => _hostClock;
        private set => SetProperty(ref _hostClock, value);
    }

    public string SystemMessageText
    {
        get => _systemMessageText;
        private set => SetProperty(ref _systemMessageText, value);
    }

    public int ConfiguredTimerPresetTenths
    {
        get
        {
            var parsed = ParsePreset(_scoreboard.Settings.GameTimePreset, 10, 0, 0);
            return (parsed.Minutes * 60 * 10) + (parsed.Seconds * 10) + parsed.Tenths;
        }
    }

    public bool CanResetTimers => _currentState is not null &&
                                  !_currentState.IsGameClockRunning &&
                                  !_currentState.IsShotClockRunning;

    public string RunningText
    {
        get => _runningText;
        set
        {
            var trimmed = value ?? string.Empty;
            if (trimmed.Length > 24)
            {
                trimmed = trimmed[..24];
            }

            if (SetProperty(ref _runningText, trimmed) && !_suppressControllerSync)
            {
                _scoreboard.Execute(new SetRunningTextCommand(trimmed));
            }
        }
    }

    public bool RunningTextEnabled
    {
        get => _runningTextEnabled;
        set
        {
            if (SetProperty(ref _runningTextEnabled, value) && !_suppressControllerSync)
            {
                _scoreboard.Execute(new SetRunningTextEnabledCommand(value));
            }
        }
    }

    public bool CountFoulsToFive
    {
        get => _countFoulsToFive;
        set => SetProperty(ref _countFoulsToFive, value);
    }

    public bool AutoStartShotClock
    {
        get => _autoStartShotClock;
        set => SetProperty(ref _autoStartShotClock, value);
    }

    public int MainSignalDurationSeconds
    {
        get => _mainSignalDurationSeconds;
        set => SetProperty(ref _mainSignalDurationSeconds, value);
    }

    public int ShotClockSignalDurationTenths
    {
        get => _shotClockSignalDurationTenths;
        set => SetProperty(ref _shotClockSignalDurationTenths, value);
    }

    public int PresetMinutes
    {
        get => _presetMinutes;
        set => SetProperty(ref _presetMinutes, value);
    }

    public int PresetSeconds
    {
        get => _presetSeconds;
        set => SetProperty(ref _presetSeconds, value);
    }

    public int PresetTenths
    {
        get => _presetTenths;
        set => SetProperty(ref _presetTenths, value);
    }

    public int OvertimePresetMinutes
    {
        get => _overtimePresetMinutes;
        set => SetProperty(ref _overtimePresetMinutes, value);
    }

    public int OvertimePresetSeconds
    {
        get => _overtimePresetSeconds;
        set => SetProperty(ref _overtimePresetSeconds, value);
    }

    public int OvertimePresetTenths
    {
        get => _overtimePresetTenths;
        set => SetProperty(ref _overtimePresetTenths, value);
    }

    public OptionItem<GameMode>? SelectedGameMode
    {
        get => _selectedGameMode;
        set
        {
            if (SetProperty(ref _selectedGameMode, value) &&
                !_suppressControllerSync &&
                value?.Value == GameMode.Basketball &&
                SelectedTimerDirection?.Value != TimerDirection.Down)
            {
                SelectedTimerDirection = TimerDirections.First(option => option.Value == TimerDirection.Down);
            }
        }
    }

    public OptionItem<FontMode>? SelectedFontMode
    {
        get => _selectedFontMode;
        set => SetProperty(ref _selectedFontMode, value);
    }

    public OptionItem<TimerDirection>? SelectedTimerDirection
    {
        get => _selectedTimerDirection;
        set
        {
            if (_selectedGameMode?.Value == GameMode.Basketball && value?.Value != TimerDirection.Down)
            {
                value = TimerDirections.First(option => option.Value == TimerDirection.Down);
            }

            SetProperty(ref _selectedTimerDirection, value);
        }
    }

    public void StartManualSignal()
    {
        _scoreboard.Execute(new SetManualSignalCommand(true));
    }

    public void StopManualSignal()
    {
        _scoreboard.Execute(new SetManualSignalCommand(false));
    }

    public void ApplyManualValues(ManualScoreboardValues values)
    {
        ArgumentNullException.ThrowIfNull(values);

        _scoreboard.Execute(new SetScoreboardValuesCommand(
            values.HomeScore,
            values.GuestScore,
            values.HomeSecondaryCounter,
            values.GuestSecondaryCounter,
            values.PeriodNumber,
            values.MainClockTenths,
            values.ShotClockTenths));

        SystemMessageText = "Values applied.";
    }

    public void Dispose()
    {
        _scoreboard.StateChanged -= ScoreboardOnStateChanged;
        _runtime.Faulted -= RuntimeOnFaulted;
        _runtime.Dispose();
        _settingsStore.Save(_scoreboard.Settings);
        _scoreboard.Dispose();
    }

    private void ScoreboardOnStateChanged(object? sender, ScoreboardState state)
    {
        if (!_dispatcher.CheckAccess())
        {
            _ = _dispatcher.InvokeAsync(() => ScoreboardOnStateChanged(sender, state));
            return;
        }

        RefreshScoreboardState(state);
    }

    private void RuntimeOnFaulted(object? sender, ScoreboardRuntimeFaultedEventArgs e)
    {
        if (!_dispatcher.CheckAccess())
        {
            _ = _dispatcher.InvokeAsync(() => RuntimeOnFaulted(sender, e));
            return;
        }

        if (_scoreboard.IsConnected)
        {
            _scoreboard.Disconnect();
            UpdatePortState();
        }

        SystemMessageText = e.Exception.Message;
    }

    private void ApplySettingsFromController()
    {
        _suppressControllerSync = true;

        var settings = _scoreboard.Settings;

        RunningText = settings.RunningText;
        RunningTextEnabled = settings.RunningTextEnabled;
        CountFoulsToFive = settings.CountFoulsToFive;
        AutoStartShotClock = settings.AutoStartShotClock;
        MainSignalDurationSeconds = settings.MainSignalDurationSeconds;
        ShotClockSignalDurationTenths = settings.ShotClockSignalDurationTenths;
        SelectedPort = settings.SelectedPort;
        SelectedGameMode = GameModes.First(option => option.Value == settings.GameMode);
        SelectedFontMode = FontModes.First(option => option.Value == settings.FontMode);
        SelectedTimerDirection = TimerDirections.First(option => option.Value == settings.TimerDirection);
        ApplyMainPresetFields(settings.GameTimePreset);
        ApplyOvertimePresetFields(settings.OvertimeTimePreset);

        _suppressControllerSync = false;
    }

    private void RefreshScoreboardState(ScoreboardState state)
    {
        CurrentState = state;
        OnPropertyChanged(nameof(CanResetTimers));
        CurrentSnapshot = _scoreboard.Snapshot;
        HostClock = DateTime.Now;
    }

    private void ApplyMainPresetFields(string preset)
    {
        var parsed = ParsePreset(preset, 10, 0, 0);
        PresetMinutes = parsed.Minutes;
        PresetSeconds = parsed.Seconds;
        PresetTenths = parsed.Tenths;
    }

    private void ApplyOvertimePresetFields(string preset)
    {
        var parsed = ParsePreset(preset, 5, 0, 0);
        OvertimePresetMinutes = parsed.Minutes;
        OvertimePresetSeconds = parsed.Seconds;
        OvertimePresetTenths = parsed.Tenths;
    }

    private void ApplySettings()
    {
        var requestedTimerDirection = SelectedTimerDirection?.Value ?? CurrentState.TimerDirection;
        var requestedTimerPreset = FormatPreset(PresetMinutes, PresetSeconds, PresetTenths);
        var requestedOvertimePreset = FormatPreset(OvertimePresetMinutes, OvertimePresetSeconds, OvertimePresetTenths);

        _scoreboard.Settings.SelectedPort = SelectedPort ?? string.Empty;

        if (SelectedGameMode is not null)
        {
            _scoreboard.Execute(new SetGameModeCommand(SelectedGameMode.Value));
        }

        if (SelectedFontMode is not null)
        {
            _scoreboard.Execute(new SetFontModeCommand(SelectedFontMode.Value));
        }

        if (SelectedTimerDirection is not null)
        {
            _scoreboard.Execute(new SetTimerDirectionCommand(SelectedTimerDirection.Value));
        }

        _scoreboard.Execute(new SetCountFoulsToFiveCommand(CountFoulsToFive));
        _scoreboard.Execute(new SetAutoStartShotClockCommand(AutoStartShotClock));
        _scoreboard.Execute(new SetMainSignalDurationSecondsCommand(MainSignalDurationSeconds));
        _scoreboard.Execute(new SetShotClockSignalDurationTenthsCommand(ShotClockSignalDurationTenths));
        _scoreboard.Execute(new SetTimerPresetCommand(PresetMinutes, PresetSeconds, PresetTenths));
        _scoreboard.Execute(new SetOvertimeTimerPresetCommand(OvertimePresetMinutes, OvertimePresetSeconds, OvertimePresetTenths));

        ApplySettingsFromController();

        var requestedCurrentPreset = CurrentState.IsExtraPeriod
            ? requestedOvertimePreset
            : requestedTimerPreset;
        var timerSettingsPending =
            CurrentState.TimerDirection != requestedTimerDirection ||
            CurrentSnapshot.TimerPresetText != requestedCurrentPreset;

        SystemMessageText = CurrentState.IsGameClockRunning && timerSettingsPending
            ? "Settings applied. Stop the game clock to apply timer direction and timer preset."
            : "Settings applied.";
    }

    private void RefreshPorts(string? preferredPort)
    {
        var ports = _scoreboard.GetAvailablePorts().ToArray();
        var selected = string.IsNullOrWhiteSpace(preferredPort) ? SelectedPort : preferredPort;

        AvailablePorts.Clear();

        foreach (var port in ports)
        {
            AvailablePorts.Add(port);
        }

        if (!string.IsNullOrWhiteSpace(selected) && ports.Contains(selected, StringComparer.OrdinalIgnoreCase))
        {
            SelectedPort = ports.First(port => string.Equals(port, selected, StringComparison.OrdinalIgnoreCase));
        }
        else if (ports.Length > 0)
        {
            SelectedPort = ports[0];
        }
    }

    private void TogglePort()
    {
        try
        {
            if (_scoreboard.IsConnected)
            {
                _scoreboard.Disconnect();
                SystemMessageText = "COM port closed.";
            }
            else
            {
                _scoreboard.Connect(SelectedPort);
                _scoreboard.Settings.SelectedPort = SelectedPort;
                SystemMessageText = $"COM port {SelectedPort} opened.";
            }

            UpdatePortState();
        }
        catch (Exception exception)
        {
            _scoreboard.Disconnect();
            UpdatePortState();
            SystemMessageText = exception.Message;
        }
    }

    private void SyncTime()
    {
        if (!_scoreboard.IsConnected)
        {
            SystemMessageText = "Open the COM port before syncing time.";
            return;
        }

        try
        {
            _scoreboard.SyncClock(DateTime.Now);
            SystemMessageText = "Device time sync packet sent.";
        }
        catch (Exception exception)
        {
            SystemMessageText = exception.Message;
        }
    }

    private void UpdatePortState()
    {
        IsConnected = _scoreboard.IsConnected;
        ConnectedPortName = _scoreboard.IsConnected ? _scoreboard.ConnectedPortName : string.Empty;
    }

    private static (int Minutes, int Seconds, int Tenths) ParsePreset(string preset, int defaultMinutes, int defaultSeconds, int defaultTenths)
    {
        if (string.IsNullOrWhiteSpace(preset))
        {
            return (defaultMinutes, defaultSeconds, defaultTenths);
        }

        var tenths = 0;
        var trimmed = preset.Trim();
        var dotIndex = trimmed.IndexOfAny(['.', ',']);

        if (dotIndex >= 0 && dotIndex < trimmed.Length - 1 && char.IsDigit(trimmed[dotIndex + 1]))
        {
            tenths = trimmed[dotIndex + 1] - '0';
            trimmed = trimmed[..dotIndex];
        }

        var segments = trimmed.Split(':', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length != 2 ||
            !int.TryParse(segments[0], out var minutes) ||
            !int.TryParse(segments[1], out var seconds))
        {
            return (defaultMinutes, defaultSeconds, defaultTenths);
        }

        return (Math.Clamp(minutes, 0, 59), Math.Clamp(seconds, 0, 59), Math.Clamp(tenths, 0, 9));
    }

    private static string FormatPreset(int minutes, int seconds, int tenths)
    {
        minutes = Math.Clamp(minutes, 0, 59);
        seconds = Math.Clamp(seconds, 0, 59);
        tenths = Math.Clamp(tenths, 0, 9);

        return tenths == 0
            ? $"{minutes:00}:{seconds:00}"
            : $"{minutes:00}:{seconds:00}.{tenths}";
    }
}
