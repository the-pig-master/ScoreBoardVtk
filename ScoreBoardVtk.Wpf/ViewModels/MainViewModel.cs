using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ScoreBoardVtk.Core.Models;
using ScoreBoardVtk.Core.Services;
using ScoreBoardVtk.Wpf.Infrastructure;
using ScoreBoardVtk.Wpf.Models;

namespace ScoreBoardVtk.Wpf.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private static readonly Brush IdleBrush = CreateBrush(209, 215, 224);
    private static readonly Brush AccentBrush = CreateBrush(0, 152, 116);
    private static readonly Brush WarningBrush = CreateBrush(225, 92, 70);

    private readonly SettingsStore _settingsStore;
    private readonly SerialTransport _transport;
    private readonly ScoreboardController _controller;

    private readonly DispatcherTimer _mainClockTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private readonly DispatcherTimer _mainSignalTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _shotClockSignalTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private readonly DispatcherTimer _displayRefreshTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _sendTimer = new() { Interval = TimeSpan.FromMilliseconds(50) };

    private bool _suppressControllerSync;

    private string _scoreAText = "000";
    private string _scoreBText = "000";
    private string _periodText = "1";
    private string _mainClockText = "10:00";
    private string _penaltyAText = "0";
    private string _penaltyBText = "0";
    private string _penaltyLabelText = "FOULS";
    private string _shotClockSecondsText = "24";
    private string _shotClockTenthsText = "0";
    private string _gameClockButtonText = "Start";
    private string _shotClockButtonText = "Start";
    private Brush _mainSignalBrush = IdleBrush;
    private Brush _shotClockBrush = IdleBrush;
    private string _selectedPort = string.Empty;
    private string _portButtonText = "Open Port";
    private string _portStatusText = "Port: closed";
    private string _presetStatusText = "Preset: 10:00";
    private string _payloadStatusText = "Payload: ";
    private string _payloadPreviewText = string.Empty;
    private string _hostClockText = $"Host clock: {DateTime.Now:HH:mm:ss}";
    private string _systemMessageText = "Ready.";
    private string _runningText = string.Empty;
    private bool _runningTextEnabled;
    private bool _countFoulsToFive = true;
    private bool _autoStartShotClock;
    private bool _isBasketballMode = true;
    private int _mainSignalDurationSeconds = 3;
    private int _shotClockSignalDurationTenths = 15;
    private int _presetMinutes = 10;
    private int _presetSeconds;
    private int _presetTenths;
    private OptionItem<GameMode>? _selectedGameMode;
    private OptionItem<FontMode>? _selectedFontMode;
    private OptionItem<TimerDirection>? _selectedTimerDirection;

    public MainViewModel(SettingsStore settingsStore, SerialTransport transport)
    {
        _settingsStore = settingsStore;
        _transport = transport;
        _controller = new ScoreboardController(_settingsStore.Load());

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

        IncreaseScoreACommand = new RelayCommand(_controller.IncreaseScoreA);
        DecreaseScoreACommand = new RelayCommand(_controller.DecreaseScoreA);
        IncreaseScoreBCommand = new RelayCommand(_controller.IncreaseScoreB);
        DecreaseScoreBCommand = new RelayCommand(_controller.DecreaseScoreB);
        IncreasePenaltyACommand = new RelayCommand(_controller.IncreasePenaltyA);
        DecreasePenaltyACommand = new RelayCommand(_controller.DecreasePenaltyA);
        IncreasePenaltyBCommand = new RelayCommand(_controller.IncreasePenaltyB);
        DecreasePenaltyBCommand = new RelayCommand(_controller.DecreasePenaltyB);
        ToggleGameClockCommand = new RelayCommand(_controller.ToggleGameClock);
        StopGameClockCommand = new RelayCommand(_controller.StopGameClock);
        AdvancePeriodOrSetCommand = new RelayCommand(_controller.AdvancePeriodOrSet);
        ResetCommand = new RelayCommand(_controller.Reset);
        SetShotClock24Command = new RelayCommand(_controller.SetShotClock24);
        SetShotClock14Command = new RelayCommand(_controller.SetShotClock14);
        ToggleShotClockCommand = new RelayCommand(_controller.ToggleShotClock);
        RefreshPortsCommand = new RelayCommand(() => RefreshPorts(SelectedPort));
        TogglePortCommand = new RelayCommand(TogglePort);
        SyncTimeCommand = new RelayCommand(SyncTime);
        ApplyTimerPresetCommand = new RelayCommand(ApplyTimerPreset);

        _controller.StateChanged += ControllerOnStateChanged;

        _mainClockTimer.Tick += (_, _) => _controller.TickMainClock();
        _mainSignalTimer.Tick += (_, _) => _controller.TickMainSignal();
        _shotClockSignalTimer.Tick += (_, _) => _controller.TickShotClockSignal();
        _displayRefreshTimer.Tick += (_, _) => _controller.RefreshDisplay();
        _sendTimer.Tick += (_, _) => SendCurrentPacket();

        ApplySettingsFromController();
        RefreshPorts(_controller.Settings.SelectedPort);
        UpdatePortState();
        _controller.RefreshDisplay();

        _mainClockTimer.Start();
        _mainSignalTimer.Start();
        _shotClockSignalTimer.Start();
        _displayRefreshTimer.Start();
        _sendTimer.Start();
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

    public ICommand ApplyTimerPresetCommand { get; }

    public string AboutText =>
        "Sports Scoreboard Modern\r\n\r\n" +
        "WPF / MVVM variant of the legacy scoreboard controller from C:\\WORK\\final.\r\n\r\n" +
        "Implemented behaviors:\r\n" +
        "- basketball mode with score, period, fouls, timer, manual buzzer and 24/14 shot clock\r\n" +
        "- volleyball mode with score and set progression\r\n" +
        "- AT+GD game-state transmission\r\n" +
        "- AT+ST device time synchronization\r\n" +
        "- CRC16 and legacy byte remapping\r\n" +
        "- persisted settings in settings.json\r\n\r\n" +
        "Hardware note:\r\n" +
        "The original Borland application controlled an RS-485 scoreboard controller directly. The protocol and state logic are preserved here, but final validation still needs to be done on the real hardware.";

    public string ScoreAText
    {
        get => _scoreAText;
        private set => SetProperty(ref _scoreAText, value);
    }

    public string ScoreBText
    {
        get => _scoreBText;
        private set => SetProperty(ref _scoreBText, value);
    }

    public string PeriodText
    {
        get => _periodText;
        private set => SetProperty(ref _periodText, value);
    }

    public string MainClockText
    {
        get => _mainClockText;
        private set => SetProperty(ref _mainClockText, value);
    }

    public string PenaltyAText
    {
        get => _penaltyAText;
        private set => SetProperty(ref _penaltyAText, value);
    }

    public string PenaltyBText
    {
        get => _penaltyBText;
        private set => SetProperty(ref _penaltyBText, value);
    }

    public string PenaltyLabelText
    {
        get => _penaltyLabelText;
        private set => SetProperty(ref _penaltyLabelText, value);
    }

    public string ShotClockSecondsText
    {
        get => _shotClockSecondsText;
        private set => SetProperty(ref _shotClockSecondsText, value);
    }

    public string ShotClockTenthsText
    {
        get => _shotClockTenthsText;
        private set => SetProperty(ref _shotClockTenthsText, value);
    }

    public string GameClockButtonText
    {
        get => _gameClockButtonText;
        private set => SetProperty(ref _gameClockButtonText, value);
    }

    public string ShotClockButtonText
    {
        get => _shotClockButtonText;
        private set => SetProperty(ref _shotClockButtonText, value);
    }

    public Brush MainSignalBrush
    {
        get => _mainSignalBrush;
        private set => SetProperty(ref _mainSignalBrush, value);
    }

    public Brush ShotClockBrush
    {
        get => _shotClockBrush;
        private set => SetProperty(ref _shotClockBrush, value);
    }

    public string SelectedPort
    {
        get => _selectedPort;
        set
        {
            if (SetProperty(ref _selectedPort, value))
            {
                _controller.Settings.SelectedPort = value ?? string.Empty;
            }
        }
    }

    public string PortButtonText
    {
        get => _portButtonText;
        private set => SetProperty(ref _portButtonText, value);
    }

    public string PortStatusText
    {
        get => _portStatusText;
        private set => SetProperty(ref _portStatusText, value);
    }

    public string PresetStatusText
    {
        get => _presetStatusText;
        private set => SetProperty(ref _presetStatusText, value);
    }

    public string PayloadStatusText
    {
        get => _payloadStatusText;
        private set => SetProperty(ref _payloadStatusText, value);
    }

    public string PayloadPreviewText
    {
        get => _payloadPreviewText;
        private set => SetProperty(ref _payloadPreviewText, value);
    }

    public string HostClockText
    {
        get => _hostClockText;
        private set => SetProperty(ref _hostClockText, value);
    }

    public string SystemMessageText
    {
        get => _systemMessageText;
        private set => SetProperty(ref _systemMessageText, value);
    }

    public string RunningText
    {
        get => _runningText;
        set
        {
            var trimmed = (value ?? string.Empty);
            if (trimmed.Length > 24)
            {
                trimmed = trimmed[..24];
            }

            if (SetProperty(ref _runningText, trimmed) && !_suppressControllerSync)
            {
                _controller.SetRunningText(trimmed);
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
                _controller.SetRunningTextEnabled(value);
            }
        }
    }

    public bool CountFoulsToFive
    {
        get => _countFoulsToFive;
        set
        {
            if (SetProperty(ref _countFoulsToFive, value) && !_suppressControllerSync)
            {
                _controller.SetCountFoulsToFive(value);
            }
        }
    }

    public bool AutoStartShotClock
    {
        get => _autoStartShotClock;
        set
        {
            if (SetProperty(ref _autoStartShotClock, value) && !_suppressControllerSync)
            {
                _controller.SetAutoStartShotClock(value);
            }
        }
    }

    public bool IsBasketballMode
    {
        get => _isBasketballMode;
        private set => SetProperty(ref _isBasketballMode, value);
    }

    public int MainSignalDurationSeconds
    {
        get => _mainSignalDurationSeconds;
        set
        {
            if (SetProperty(ref _mainSignalDurationSeconds, value) && !_suppressControllerSync)
            {
                _controller.SetMainSignalDurationSeconds(value);
            }
        }
    }

    public int ShotClockSignalDurationTenths
    {
        get => _shotClockSignalDurationTenths;
        set
        {
            if (SetProperty(ref _shotClockSignalDurationTenths, value) && !_suppressControllerSync)
            {
                _controller.SetShotClockSignalDurationTenths(value);
            }
        }
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

    public OptionItem<GameMode>? SelectedGameMode
    {
        get => _selectedGameMode;
        set
        {
            if (SetProperty(ref _selectedGameMode, value) && !_suppressControllerSync && value is not null)
            {
                _controller.SetGameMode(value.Value);
            }
        }
    }

    public OptionItem<FontMode>? SelectedFontMode
    {
        get => _selectedFontMode;
        set
        {
            if (SetProperty(ref _selectedFontMode, value) && !_suppressControllerSync && value is not null)
            {
                _controller.SetFontMode(value.Value);
            }
        }
    }

    public OptionItem<TimerDirection>? SelectedTimerDirection
    {
        get => _selectedTimerDirection;
        set
        {
            if (SetProperty(ref _selectedTimerDirection, value) && !_suppressControllerSync && value is not null)
            {
                _controller.SetTimerDirection(value.Value);
            }
        }
    }

    public void StartManualSignal()
    {
        _controller.StartManualSignal();
    }

    public void StopManualSignal()
    {
        _controller.StopManualSignal();
    }

    public void Dispose()
    {
        _mainClockTimer.Stop();
        _mainSignalTimer.Stop();
        _shotClockSignalTimer.Stop();
        _displayRefreshTimer.Stop();
        _sendTimer.Stop();

        _controller.StateChanged -= ControllerOnStateChanged;
        _settingsStore.Save(_controller.Settings);
        _transport.Close();
    }

    private void ControllerOnStateChanged(object? sender, ScoreboardSnapshot snapshot)
    {
        ScoreAText = snapshot.ScoreAText;
        ScoreBText = snapshot.ScoreBText;
        PeriodText = snapshot.PeriodText;
        MainClockText = snapshot.MainClockText;
        PenaltyAText = snapshot.PenaltyAText;
        PenaltyBText = snapshot.PenaltyBText;
        PenaltyLabelText = snapshot.PenaltyLabelText;
        ShotClockSecondsText = snapshot.ShotClockSecondsText;
        ShotClockTenthsText = snapshot.ShotClockTenthsText;
        GameClockButtonText = snapshot.GameClockActionText;
        ShotClockButtonText = snapshot.ShotClockActionText;
        MainSignalBrush = snapshot.IsMainSignalActive ? WarningBrush : IdleBrush;
        ShotClockBrush = snapshot.IsShotClockRunning ? AccentBrush : IdleBrush;
        IsBasketballMode = snapshot.GameMode == GameMode.Basketball;

        PresetStatusText = $"Preset: {snapshot.TimerPresetText}";
        PayloadStatusText = $"Payload: {snapshot.PayloadText}";
        PayloadPreviewText = $"AT+GD{snapshot.PayloadText}";
        HostClockText = $"Host clock: {DateTime.Now:HH:mm:ss}";

        ApplyPresetFields(snapshot.TimerPresetText);
    }

    private void ApplySettingsFromController()
    {
        _suppressControllerSync = true;

        var settings = _controller.Settings;

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
        ApplyPresetFields(settings.GameTimePreset);

        _suppressControllerSync = false;
    }

    private void ApplyPresetFields(string preset)
    {
        var parsed = ParsePreset(preset);
        PresetMinutes = parsed.Minutes;
        PresetSeconds = parsed.Seconds;
        PresetTenths = parsed.Tenths;
    }

    private void ApplyTimerPreset()
    {
        _controller.SetTimerPreset(PresetMinutes, PresetSeconds, PresetTenths);
        SystemMessageText = $"Timer preset set to {PresetMinutes:00}:{PresetSeconds:00}.{PresetTenths}.";
    }

    private void RefreshPorts(string? preferredPort)
    {
        var ports = _transport.GetAvailablePorts();
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
            if (_transport.IsOpen)
            {
                _transport.Close();
                SystemMessageText = "COM port closed.";
            }
            else
            {
                _transport.Open(SelectedPort);
                _controller.Settings.SelectedPort = SelectedPort;
                SystemMessageText = $"COM port {SelectedPort} opened.";
            }

            UpdatePortState();
        }
        catch (Exception exception)
        {
            _transport.Close();
            UpdatePortState();
            SystemMessageText = exception.Message;
        }
    }

    private void SyncTime()
    {
        if (!_transport.IsOpen)
        {
            SystemMessageText = "Open the COM port before syncing time.";
            return;
        }

        try
        {
            _transport.Write(SerialProtocol.CreateTimeSyncPacket(DateTime.Now));
            SystemMessageText = "Device time sync packet sent.";
        }
        catch (Exception exception)
        {
            SystemMessageText = exception.Message;
        }
    }

    private void SendCurrentPacket()
    {
        if (!_transport.IsOpen)
        {
            return;
        }

        try
        {
            _transport.Write(SerialProtocol.CreateGamePacket(_controller.Snapshot));
        }
        catch (Exception exception)
        {
            _transport.Close();
            UpdatePortState();
            SystemMessageText = exception.Message;
        }
    }

    private void UpdatePortState()
    {
        if (_transport.IsOpen)
        {
            PortStatusText = $"Port: open ({_transport.PortName})";
            PortButtonText = "Close Port";
        }
        else
        {
            PortStatusText = "Port: closed";
            PortButtonText = "Open Port";
        }
    }

    private static (int Minutes, int Seconds, int Tenths) ParsePreset(string preset)
    {
        if (string.IsNullOrWhiteSpace(preset))
        {
            return (10, 0, 0);
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
            return (10, 0, 0);
        }

        return (Math.Clamp(minutes, 0, 59), Math.Clamp(seconds, 0, 59), Math.Clamp(tenths, 0, 9));
    }

    private static Brush CreateBrush(byte red, byte green, byte blue)
    {
        var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }
}
