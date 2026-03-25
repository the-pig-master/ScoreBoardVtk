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
    private readonly KeyboardBindingsSettings _draftKeyboardBindings = new();

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
    private int _mainSignalDurationSeconds = 3;
    private int _shotClockSignalDurationTenths = 15;
    private int _presetMinutes = 10;
    private int _presetSeconds;
    private int _presetTenths;
    private int _overtimePresetMinutes = 5;
    private int _overtimePresetSeconds;
    private int _overtimePresetTenths;
    private KeyboardShortcutAction? _pendingKeyboardShortcutAction;
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
        KeyboardShortcutBindings =
        [
            new KeyboardShortcutBindingViewModel(KeyboardShortcutAction.ToggleGameClock, "Game clock start / stop", BeginKeyboardShortcutCapture, ClearKeyboardShortcut),
            new KeyboardShortcutBindingViewModel(KeyboardShortcutAction.ToggleShotClock, "Shot clock start / stop", BeginKeyboardShortcutCapture, ClearKeyboardShortcut),
            new KeyboardShortcutBindingViewModel(KeyboardShortcutAction.SetShotClock24, "Set shot clock to 24", BeginKeyboardShortcutCapture, ClearKeyboardShortcut),
            new KeyboardShortcutBindingViewModel(KeyboardShortcutAction.SetShotClock14, "Set shot clock to 14", BeginKeyboardShortcutCapture, ClearKeyboardShortcut),
            new KeyboardShortcutBindingViewModel(KeyboardShortcutAction.RunShotClock24, "Run shot clock from 24", BeginKeyboardShortcutCapture, ClearKeyboardShortcut),
            new KeyboardShortcutBindingViewModel(KeyboardShortcutAction.RunShotClock14, "Run shot clock from 14", BeginKeyboardShortcutCapture, ClearKeyboardShortcut),
            new KeyboardShortcutBindingViewModel(KeyboardShortcutAction.IncreaseHomeScore, "Home score +1", BeginKeyboardShortcutCapture, ClearKeyboardShortcut),
            new KeyboardShortcutBindingViewModel(KeyboardShortcutAction.DecreaseHomeScore, "Home score -1", BeginKeyboardShortcutCapture, ClearKeyboardShortcut),
            new KeyboardShortcutBindingViewModel(KeyboardShortcutAction.IncreaseGuestScore, "Guest score +1", BeginKeyboardShortcutCapture, ClearKeyboardShortcut),
            new KeyboardShortcutBindingViewModel(KeyboardShortcutAction.DecreaseGuestScore, "Guest score -1", BeginKeyboardShortcutCapture, ClearKeyboardShortcut),
            new KeyboardShortcutBindingViewModel(KeyboardShortcutAction.IncreaseHomeFouls, "Home fouls +1", BeginKeyboardShortcutCapture, ClearKeyboardShortcut),
            new KeyboardShortcutBindingViewModel(KeyboardShortcutAction.DecreaseHomeFouls, "Home fouls -1", BeginKeyboardShortcutCapture, ClearKeyboardShortcut),
            new KeyboardShortcutBindingViewModel(KeyboardShortcutAction.IncreaseGuestFouls, "Guest fouls +1", BeginKeyboardShortcutCapture, ClearKeyboardShortcut),
            new KeyboardShortcutBindingViewModel(KeyboardShortcutAction.DecreaseGuestFouls, "Guest fouls -1", BeginKeyboardShortcutCapture, ClearKeyboardShortcut),
        ];

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
        RunShotClock24Command = new RelayCommand(() => _scoreboard.Execute(new RunShotClockCommand(24)));
        RunShotClock14Command = new RelayCommand(() => _scoreboard.Execute(new RunShotClockCommand(14)));
        ToggleShotClockCommand = new RelayCommand(() => _scoreboard.Execute(new ToggleShotClockCommand()));
        RefreshPortsCommand = new RelayCommand(() => RefreshPorts(SelectedPort));
        TogglePortCommand = new RelayCommand(TogglePort);
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

    public ObservableCollection<KeyboardShortcutBindingViewModel> KeyboardShortcutBindings { get; }

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

    public ICommand RunShotClock24Command { get; }

    public ICommand RunShotClock14Command { get; }

    public ICommand ToggleShotClockCommand { get; }

    public ICommand RefreshPortsCommand { get; }

    public ICommand TogglePortCommand { get; }

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

    public string KeyboardShortcutCaptureText => _pendingKeyboardShortcutAction is null
        ? "Click Assign and press a key. Use Clear to disable that shortcut."
        : $"Press a key for {GetKeyboardShortcutLabel(_pendingKeyboardShortcutAction.Value)}. Press Esc to clear.";

    public bool CanResetTimers => _currentState is not null &&
                                  !_currentState.IsGameClockRunning &&
                                  !_currentState.IsShotClockRunning;

    public string GameClockToggleButtonText => BuildButtonText(
        _currentState is not null && _currentState.IsGameClockRunning ? "Stop" : "Start",
        GetAppliedKeyboardShortcutDisplay(KeyboardShortcutAction.ToggleGameClock));

    public string ShotClockToggleButtonText => BuildButtonText(
        _currentState is not null && _currentState.IsShotClockRunning ? "Stop" : "Start",
        GetAppliedKeyboardShortcutDisplay(KeyboardShortcutAction.ToggleShotClock));

    public string SetShotClock24ButtonText => BuildButtonText("Set 24", GetAppliedKeyboardShortcutDisplay(KeyboardShortcutAction.SetShotClock24));

    public string SetShotClock14ButtonText => BuildButtonText("Set 14", GetAppliedKeyboardShortcutDisplay(KeyboardShortcutAction.SetShotClock14));

    public string RunShotClock24ButtonText => BuildButtonText("Run 24", GetAppliedKeyboardShortcutDisplay(KeyboardShortcutAction.RunShotClock24));

    public string RunShotClock14ButtonText => BuildButtonText("Run 14", GetAppliedKeyboardShortcutDisplay(KeyboardShortcutAction.RunShotClock14));

    public string IncreaseScoreAButtonText => BuildButtonText("+1", GetAppliedKeyboardShortcutDisplay(KeyboardShortcutAction.IncreaseHomeScore));

    public string DecreaseScoreAButtonText => BuildButtonText("-1", GetAppliedKeyboardShortcutDisplay(KeyboardShortcutAction.DecreaseHomeScore));

    public string IncreaseScoreBButtonText => BuildButtonText("+1", GetAppliedKeyboardShortcutDisplay(KeyboardShortcutAction.IncreaseGuestScore));

    public string DecreaseScoreBButtonText => BuildButtonText("-1", GetAppliedKeyboardShortcutDisplay(KeyboardShortcutAction.DecreaseGuestScore));

    public string IncreasePenaltyAButtonText => BuildButtonText("+1", GetAppliedKeyboardShortcutDisplay(KeyboardShortcutAction.IncreaseHomeFouls));

    public string DecreasePenaltyAButtonText => BuildButtonText("-1", GetAppliedKeyboardShortcutDisplay(KeyboardShortcutAction.DecreaseHomeFouls));

    public string IncreasePenaltyBButtonText => BuildButtonText("+1", GetAppliedKeyboardShortcutDisplay(KeyboardShortcutAction.IncreaseGuestFouls));

    public string DecreasePenaltyBButtonText => BuildButtonText("-1", GetAppliedKeyboardShortcutDisplay(KeyboardShortcutAction.DecreaseGuestFouls));

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

    public bool TryHandleKeyboardShortcutCapture(Key key)
    {
        if (_pendingKeyboardShortcutAction is null)
        {
            return false;
        }

        if (key == Key.Escape)
        {
            SetDraftKeyboardShortcut(_pendingKeyboardShortcutAction.Value, string.Empty);
            SystemMessageText = $"{GetKeyboardShortcutLabel(_pendingKeyboardShortcutAction.Value)} shortcut cleared.";
        }
        else if (!IsAssignableKeyboardShortcut(key))
        {
            SystemMessageText = "This key cannot be assigned as a shortcut.";
            return true;
        }
        else
        {
            RemoveDraftKeyboardShortcut(key, _pendingKeyboardShortcutAction.Value);
            SetDraftKeyboardShortcut(_pendingKeyboardShortcutAction.Value, key.ToString());
            SystemMessageText = $"{GetKeyboardShortcutLabel(_pendingKeyboardShortcutAction.Value)} shortcut set to {FormatKeyDisplay(key)}.";
        }

        _pendingKeyboardShortcutAction = null;
        RefreshKeyboardShortcutBindings();
        return true;
    }

    public bool TryExecuteKeyboardShortcut(Key key)
    {
        if (key == Key.None || Keyboard.Modifiers != ModifierKeys.None)
        {
            return false;
        }

        foreach (KeyboardShortcutAction action in Enum.GetValues(typeof(KeyboardShortcutAction)))
        {
            if (TryGetAssignedKey(GetAppliedKeyboardShortcutValue(action), out var assignedKey) && assignedKey == key)
            {
                ExecuteKeyboardShortcut(action);
                return true;
            }
        }

        return false;
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
        MainSignalDurationSeconds = settings.MainSignalDurationSeconds;
        ShotClockSignalDurationTenths = settings.ShotClockSignalDurationTenths;
        SelectedPort = settings.SelectedPort;
        SelectedGameMode = GameModes.First(option => option.Value == settings.GameMode);
        SelectedFontMode = FontModes.First(option => option.Value == settings.FontMode);
        SelectedTimerDirection = TimerDirections.First(option => option.Value == settings.TimerDirection);
        ApplyMainPresetFields(settings.GameTimePreset);
        ApplyOvertimePresetFields(settings.OvertimeTimePreset);
        ApplyKeyboardShortcutDraft(settings.KeyboardBindings);

        _suppressControllerSync = false;
    }

    private void RefreshScoreboardState(ScoreboardState state)
    {
        CurrentState = state;
        OnPropertyChanged(nameof(CanResetTimers));
        CurrentSnapshot = _scoreboard.Snapshot;
        HostClock = DateTime.Now;
        NotifyKeyboardShortcutButtonTextChanged();
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
        _scoreboard.Execute(new SetMainSignalDurationSecondsCommand(MainSignalDurationSeconds));
        _scoreboard.Execute(new SetShotClockSignalDurationTenthsCommand(ShotClockSignalDurationTenths));
        _scoreboard.Execute(new SetTimerPresetCommand(PresetMinutes, PresetSeconds, PresetTenths));
        _scoreboard.Execute(new SetOvertimeTimerPresetCommand(OvertimePresetMinutes, OvertimePresetSeconds, OvertimePresetTenths));
        _scoreboard.Settings.KeyboardBindings = CloneKeyboardBindings(_draftKeyboardBindings);

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

    private void BeginKeyboardShortcutCapture(KeyboardShortcutAction action)
    {
        _pendingKeyboardShortcutAction = action;
        RefreshKeyboardShortcutBindings();
        SystemMessageText = $"Press a key for {GetKeyboardShortcutLabel(action)}. Press Esc to clear.";
    }

    private void ClearKeyboardShortcut(KeyboardShortcutAction action)
    {
        SetDraftKeyboardShortcut(action, string.Empty);

        if (_pendingKeyboardShortcutAction == action)
        {
            _pendingKeyboardShortcutAction = null;
        }

        RefreshKeyboardShortcutBindings();
        SystemMessageText = $"{GetKeyboardShortcutLabel(action)} shortcut cleared.";
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

    private void UpdatePortState()
    {
        IsConnected = _scoreboard.IsConnected;
        ConnectedPortName = _scoreboard.IsConnected ? _scoreboard.ConnectedPortName : string.Empty;
    }

    private void ApplyKeyboardShortcutDraft(KeyboardBindingsSettings bindings)
    {
        _draftKeyboardBindings.ToggleGameClockKey = bindings.ToggleGameClockKey;
        _draftKeyboardBindings.ToggleShotClockKey = bindings.ToggleShotClockKey;
        _draftKeyboardBindings.SetShotClock24Key = bindings.SetShotClock24Key;
        _draftKeyboardBindings.SetShotClock14Key = bindings.SetShotClock14Key;
        _draftKeyboardBindings.RunShotClock24Key = bindings.RunShotClock24Key;
        _draftKeyboardBindings.RunShotClock14Key = bindings.RunShotClock14Key;
        _draftKeyboardBindings.IncreaseHomeScoreKey = bindings.IncreaseHomeScoreKey;
        _draftKeyboardBindings.DecreaseHomeScoreKey = bindings.DecreaseHomeScoreKey;
        _draftKeyboardBindings.IncreaseGuestScoreKey = bindings.IncreaseGuestScoreKey;
        _draftKeyboardBindings.DecreaseGuestScoreKey = bindings.DecreaseGuestScoreKey;
        _draftKeyboardBindings.IncreaseHomeFoulsKey = bindings.IncreaseHomeFoulsKey;
        _draftKeyboardBindings.DecreaseHomeFoulsKey = bindings.DecreaseHomeFoulsKey;
        _draftKeyboardBindings.IncreaseGuestFoulsKey = bindings.IncreaseGuestFoulsKey;
        _draftKeyboardBindings.DecreaseGuestFoulsKey = bindings.DecreaseGuestFoulsKey;
        _pendingKeyboardShortcutAction = null;
        RefreshKeyboardShortcutBindings();
        NotifyKeyboardShortcutButtonTextChanged();
    }

    private void RefreshKeyboardShortcutBindings()
    {
        foreach (var binding in KeyboardShortcutBindings)
        {
            binding.AssignedKeyDisplay = GetDraftKeyboardShortcutDisplay(binding.Action);
            binding.IsCapturing = _pendingKeyboardShortcutAction == binding.Action;
        }

        OnPropertyChanged(nameof(KeyboardShortcutCaptureText));
    }

    private void RemoveDraftKeyboardShortcut(Key key, KeyboardShortcutAction exceptAction)
    {
        var keyName = key.ToString();

        foreach (KeyboardShortcutAction action in Enum.GetValues(typeof(KeyboardShortcutAction)))
        {
            if (action != exceptAction &&
                string.Equals(GetDraftKeyboardShortcutValue(action), keyName, StringComparison.OrdinalIgnoreCase))
            {
                SetDraftKeyboardShortcut(action, string.Empty);
            }
        }
    }

    private void SetDraftKeyboardShortcut(KeyboardShortcutAction action, string keyName)
    {
        switch (action)
        {
            case KeyboardShortcutAction.ToggleGameClock:
                _draftKeyboardBindings.ToggleGameClockKey = keyName;
                break;
            case KeyboardShortcutAction.ToggleShotClock:
                _draftKeyboardBindings.ToggleShotClockKey = keyName;
                break;
            case KeyboardShortcutAction.SetShotClock24:
                _draftKeyboardBindings.SetShotClock24Key = keyName;
                break;
            case KeyboardShortcutAction.SetShotClock14:
                _draftKeyboardBindings.SetShotClock14Key = keyName;
                break;
            case KeyboardShortcutAction.RunShotClock24:
                _draftKeyboardBindings.RunShotClock24Key = keyName;
                break;
            case KeyboardShortcutAction.RunShotClock14:
                _draftKeyboardBindings.RunShotClock14Key = keyName;
                break;
            case KeyboardShortcutAction.IncreaseHomeScore:
                _draftKeyboardBindings.IncreaseHomeScoreKey = keyName;
                break;
            case KeyboardShortcutAction.DecreaseHomeScore:
                _draftKeyboardBindings.DecreaseHomeScoreKey = keyName;
                break;
            case KeyboardShortcutAction.IncreaseGuestScore:
                _draftKeyboardBindings.IncreaseGuestScoreKey = keyName;
                break;
            case KeyboardShortcutAction.DecreaseGuestScore:
                _draftKeyboardBindings.DecreaseGuestScoreKey = keyName;
                break;
            case KeyboardShortcutAction.IncreaseHomeFouls:
                _draftKeyboardBindings.IncreaseHomeFoulsKey = keyName;
                break;
            case KeyboardShortcutAction.DecreaseHomeFouls:
                _draftKeyboardBindings.DecreaseHomeFoulsKey = keyName;
                break;
            case KeyboardShortcutAction.IncreaseGuestFouls:
                _draftKeyboardBindings.IncreaseGuestFoulsKey = keyName;
                break;
            case KeyboardShortcutAction.DecreaseGuestFouls:
                _draftKeyboardBindings.DecreaseGuestFoulsKey = keyName;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }

    private string GetDraftKeyboardShortcutValue(KeyboardShortcutAction action)
    {
        return action switch
        {
            KeyboardShortcutAction.ToggleGameClock => _draftKeyboardBindings.ToggleGameClockKey,
            KeyboardShortcutAction.ToggleShotClock => _draftKeyboardBindings.ToggleShotClockKey,
            KeyboardShortcutAction.SetShotClock24 => _draftKeyboardBindings.SetShotClock24Key,
            KeyboardShortcutAction.SetShotClock14 => _draftKeyboardBindings.SetShotClock14Key,
            KeyboardShortcutAction.RunShotClock24 => _draftKeyboardBindings.RunShotClock24Key,
            KeyboardShortcutAction.RunShotClock14 => _draftKeyboardBindings.RunShotClock14Key,
            KeyboardShortcutAction.IncreaseHomeScore => _draftKeyboardBindings.IncreaseHomeScoreKey,
            KeyboardShortcutAction.DecreaseHomeScore => _draftKeyboardBindings.DecreaseHomeScoreKey,
            KeyboardShortcutAction.IncreaseGuestScore => _draftKeyboardBindings.IncreaseGuestScoreKey,
            KeyboardShortcutAction.DecreaseGuestScore => _draftKeyboardBindings.DecreaseGuestScoreKey,
            KeyboardShortcutAction.IncreaseHomeFouls => _draftKeyboardBindings.IncreaseHomeFoulsKey,
            KeyboardShortcutAction.DecreaseHomeFouls => _draftKeyboardBindings.DecreaseHomeFoulsKey,
            KeyboardShortcutAction.IncreaseGuestFouls => _draftKeyboardBindings.IncreaseGuestFoulsKey,
            KeyboardShortcutAction.DecreaseGuestFouls => _draftKeyboardBindings.DecreaseGuestFoulsKey,
            _ => string.Empty,
        };
    }

    private string GetDraftKeyboardShortcutDisplay(KeyboardShortcutAction action)
    {
        return FormatAssignedKeyDisplay(GetDraftKeyboardShortcutValue(action));
    }

    private string GetAppliedKeyboardShortcutValue(KeyboardShortcutAction action)
    {
        var bindings = _scoreboard.Settings.KeyboardBindings;

        return action switch
        {
            KeyboardShortcutAction.ToggleGameClock => bindings.ToggleGameClockKey,
            KeyboardShortcutAction.ToggleShotClock => bindings.ToggleShotClockKey,
            KeyboardShortcutAction.SetShotClock24 => bindings.SetShotClock24Key,
            KeyboardShortcutAction.SetShotClock14 => bindings.SetShotClock14Key,
            KeyboardShortcutAction.RunShotClock24 => bindings.RunShotClock24Key,
            KeyboardShortcutAction.RunShotClock14 => bindings.RunShotClock14Key,
            KeyboardShortcutAction.IncreaseHomeScore => bindings.IncreaseHomeScoreKey,
            KeyboardShortcutAction.DecreaseHomeScore => bindings.DecreaseHomeScoreKey,
            KeyboardShortcutAction.IncreaseGuestScore => bindings.IncreaseGuestScoreKey,
            KeyboardShortcutAction.DecreaseGuestScore => bindings.DecreaseGuestScoreKey,
            KeyboardShortcutAction.IncreaseHomeFouls => bindings.IncreaseHomeFoulsKey,
            KeyboardShortcutAction.DecreaseHomeFouls => bindings.DecreaseHomeFoulsKey,
            KeyboardShortcutAction.IncreaseGuestFouls => bindings.IncreaseGuestFoulsKey,
            KeyboardShortcutAction.DecreaseGuestFouls => bindings.DecreaseGuestFoulsKey,
            _ => string.Empty,
        };
    }

    private string GetAppliedKeyboardShortcutDisplay(KeyboardShortcutAction action)
    {
        if (TryGetAssignedKey(GetAppliedKeyboardShortcutValue(action), out var key))
        {
            return FormatKeyDisplay(key);
        }

        return string.Empty;
    }

    private void ExecuteKeyboardShortcut(KeyboardShortcutAction action)
    {
        switch (action)
        {
            case KeyboardShortcutAction.ToggleGameClock:
                _scoreboard.Execute(new ToggleGameClockCommand());
                break;
            case KeyboardShortcutAction.ToggleShotClock:
                _scoreboard.Execute(new ToggleShotClockCommand());
                break;
            case KeyboardShortcutAction.SetShotClock24:
                _scoreboard.Execute(new SetShotClockCommand(24));
                break;
            case KeyboardShortcutAction.SetShotClock14:
                _scoreboard.Execute(new SetShotClockCommand(14));
                break;
            case KeyboardShortcutAction.RunShotClock24:
                _scoreboard.Execute(new RunShotClockCommand(24));
                break;
            case KeyboardShortcutAction.RunShotClock14:
                _scoreboard.Execute(new RunShotClockCommand(14));
                break;
            case KeyboardShortcutAction.IncreaseHomeScore:
                _scoreboard.Execute(new ChangeScoreCommand(TeamSide.Home, 1));
                break;
            case KeyboardShortcutAction.DecreaseHomeScore:
                _scoreboard.Execute(new ChangeScoreCommand(TeamSide.Home, -1));
                break;
            case KeyboardShortcutAction.IncreaseGuestScore:
                _scoreboard.Execute(new ChangeScoreCommand(TeamSide.Guest, 1));
                break;
            case KeyboardShortcutAction.DecreaseGuestScore:
                _scoreboard.Execute(new ChangeScoreCommand(TeamSide.Guest, -1));
                break;
            case KeyboardShortcutAction.IncreaseHomeFouls:
                _scoreboard.Execute(new ChangeSecondaryCounterCommand(TeamSide.Home, 1));
                break;
            case KeyboardShortcutAction.DecreaseHomeFouls:
                _scoreboard.Execute(new ChangeSecondaryCounterCommand(TeamSide.Home, -1));
                break;
            case KeyboardShortcutAction.IncreaseGuestFouls:
                _scoreboard.Execute(new ChangeSecondaryCounterCommand(TeamSide.Guest, 1));
                break;
            case KeyboardShortcutAction.DecreaseGuestFouls:
                _scoreboard.Execute(new ChangeSecondaryCounterCommand(TeamSide.Guest, -1));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }

    private void NotifyKeyboardShortcutButtonTextChanged()
    {
        OnPropertyChanged(nameof(GameClockToggleButtonText));
        OnPropertyChanged(nameof(ShotClockToggleButtonText));
        OnPropertyChanged(nameof(SetShotClock24ButtonText));
        OnPropertyChanged(nameof(SetShotClock14ButtonText));
        OnPropertyChanged(nameof(RunShotClock24ButtonText));
        OnPropertyChanged(nameof(RunShotClock14ButtonText));
        OnPropertyChanged(nameof(IncreaseScoreAButtonText));
        OnPropertyChanged(nameof(DecreaseScoreAButtonText));
        OnPropertyChanged(nameof(IncreaseScoreBButtonText));
        OnPropertyChanged(nameof(DecreaseScoreBButtonText));
        OnPropertyChanged(nameof(IncreasePenaltyAButtonText));
        OnPropertyChanged(nameof(DecreasePenaltyAButtonText));
        OnPropertyChanged(nameof(IncreasePenaltyBButtonText));
        OnPropertyChanged(nameof(DecreasePenaltyBButtonText));
    }

    private static KeyboardBindingsSettings CloneKeyboardBindings(KeyboardBindingsSettings source)
    {
        return new KeyboardBindingsSettings
        {
            ToggleGameClockKey = source.ToggleGameClockKey,
            ToggleShotClockKey = source.ToggleShotClockKey,
            SetShotClock24Key = source.SetShotClock24Key,
            SetShotClock14Key = source.SetShotClock14Key,
            RunShotClock24Key = source.RunShotClock24Key,
            RunShotClock14Key = source.RunShotClock14Key,
            IncreaseHomeScoreKey = source.IncreaseHomeScoreKey,
            DecreaseHomeScoreKey = source.DecreaseHomeScoreKey,
            IncreaseGuestScoreKey = source.IncreaseGuestScoreKey,
            DecreaseGuestScoreKey = source.DecreaseGuestScoreKey,
            IncreaseHomeFoulsKey = source.IncreaseHomeFoulsKey,
            DecreaseHomeFoulsKey = source.DecreaseHomeFoulsKey,
            IncreaseGuestFoulsKey = source.IncreaseGuestFoulsKey,
            DecreaseGuestFoulsKey = source.DecreaseGuestFoulsKey,
        };
    }

    private static string GetKeyboardShortcutLabel(KeyboardShortcutAction action)
    {
        return action switch
        {
            KeyboardShortcutAction.ToggleGameClock => "Game clock start / stop",
            KeyboardShortcutAction.ToggleShotClock => "Shot clock start / stop",
            KeyboardShortcutAction.SetShotClock24 => "Set shot clock to 24",
            KeyboardShortcutAction.SetShotClock14 => "Set shot clock to 14",
            KeyboardShortcutAction.RunShotClock24 => "Run shot clock from 24",
            KeyboardShortcutAction.RunShotClock14 => "Run shot clock from 14",
            KeyboardShortcutAction.IncreaseHomeScore => "Home score +1",
            KeyboardShortcutAction.DecreaseHomeScore => "Home score -1",
            KeyboardShortcutAction.IncreaseGuestScore => "Guest score +1",
            KeyboardShortcutAction.DecreaseGuestScore => "Guest score -1",
            KeyboardShortcutAction.IncreaseHomeFouls => "Home fouls +1",
            KeyboardShortcutAction.DecreaseHomeFouls => "Home fouls -1",
            KeyboardShortcutAction.IncreaseGuestFouls => "Guest fouls +1",
            KeyboardShortcutAction.DecreaseGuestFouls => "Guest fouls -1",
            _ => "Shortcut",
        };
    }

    private static bool IsAssignableKeyboardShortcut(Key key)
    {
        return key is not (Key.None or
            Key.LeftCtrl or Key.RightCtrl or
            Key.LeftAlt or Key.RightAlt or
            Key.LeftShift or Key.RightShift or
            Key.LWin or Key.RWin or
            Key.Apps or
            Key.Clear or
            Key.DeadCharProcessed);
    }

    private static bool TryGetAssignedKey(string? keyName, out Key key)
    {
        if (!string.IsNullOrWhiteSpace(keyName) && Enum.TryParse(keyName.Trim(), true, out key) && key != Key.None)
        {
            return true;
        }

        key = Key.None;
        return false;
    }

    private static string FormatAssignedKeyDisplay(string? keyName)
    {
        return TryGetAssignedKey(keyName, out var key)
            ? FormatKeyDisplay(key)
            : "Not assigned";
    }

    private static string FormatKeyDisplay(Key key)
    {
        if (key is >= Key.D0 and <= Key.D9)
        {
            return ((char)('0' + (key - Key.D0))).ToString();
        }

        if (key is >= Key.NumPad0 and <= Key.NumPad9)
        {
            return $"Num{key - Key.NumPad0}";
        }

        return key switch
        {
            Key.Return => "Enter",
            Key.Space => "Space",
            Key.Prior => "PageUp",
            Key.Next => "PageDown",
            Key.OemPlus => "+",
            Key.OemMinus => "-",
            Key.Multiply => "*",
            Key.Add => "Num+",
            Key.Subtract => "Num-",
            Key.Divide => "Num/",
            Key.Decimal => "Num.",
            _ => key.ToString(),
        };
    }

    private static string BuildButtonText(string baseText, string keyDisplay)
    {
        return string.IsNullOrWhiteSpace(keyDisplay)
            ? baseText
            : $"{baseText} ({keyDisplay})";
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
