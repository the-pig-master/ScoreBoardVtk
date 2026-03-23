using SportsScoreboardModern.Core.Models;
using SportsScoreboardModern.Core.Services;
using FormsTimer = System.Windows.Forms.Timer;

namespace SportsScoreboardModern.App;

public sealed class MainForm : Form
{
    private static readonly Color CanvasColor = Color.FromArgb(236, 240, 245);
    private static readonly Color PanelColor = Color.FromArgb(247, 249, 252);
    private static readonly Color ScoreboardColor = Color.FromArgb(14, 18, 24);
    private static readonly Color AccentColor = Color.FromArgb(0, 152, 116);
    private static readonly Color WarningColor = Color.FromArgb(225, 92, 70);
    private static readonly Color ScoreColor = Color.FromArgb(255, 92, 92);
    private static readonly Color MetaColor = Color.FromArgb(176, 247, 105);
    private static readonly Color ClockColor = Color.FromArgb(255, 214, 77);
    private static readonly Color MutedTextColor = Color.FromArgb(88, 96, 105);

    private readonly SerialTransport _transport;
    private readonly SettingsStore _settingsStore = new();
    private readonly ScoreboardController _controller;

    private readonly FormsTimer _mainClockTimer = new() { Interval = 100 };
    private readonly FormsTimer _mainSignalTimer = new() { Interval = 1000 };
    private readonly FormsTimer _shotClockSignalTimer = new() { Interval = 100 };
    private readonly FormsTimer _displayRefreshTimer = new() { Interval = 1000 };
    private readonly FormsTimer _sendTimer = new() { Interval = 50 };

    private bool _suppressUiEvents;

    private Label _scoreALabel = null!;
    private Label _scoreBLabel = null!;
    private Label _periodLabel = null!;
    private Label _mainClockLabel = null!;
    private Label _penaltyALabel = null!;
    private Label _penaltyBLabel = null!;
    private Label _penaltyTitleALabel = null!;
    private Label _penaltyTitleBLabel = null!;
    private Label _shotClockLabel = null!;
    private Label _shotClockTenthsLabel = null!;
    private Label _currentTimeLabel = null!;

    private Panel _mainSignalIndicator = null!;
    private Panel _shotClockIndicator = null!;

    private Button _gameClockButton = null!;
    private Button _stopClockButton = null!;
    private Button _periodButton = null!;
    private Button _resetButton = null!;
    private Button _signalButton = null!;
    private Button _shotClockButton = null!;
    private Button _timerPresetButton = null!;
    private Button _togglePortButton = null!;

    private GroupBox _penaltyAGroup = null!;
    private GroupBox _penaltyBGroup = null!;
    private GroupBox _shotClockGroup = null!;

    private CheckBox _runningTextCheckBox = null!;
    private TextBox _runningTextTextBox = null!;
    private ComboBox _portComboBox = null!;
    private ComboBox _gameModeComboBox = null!;
    private ComboBox _fontModeComboBox = null!;
    private ComboBox _timerDirectionComboBox = null!;
    private NumericUpDown _signalDurationNumeric = null!;
    private NumericUpDown _shotClockSignalNumeric = null!;
    private CheckBox _foulsToFiveCheckBox = null!;
    private CheckBox _autoStartShotClockCheckBox = null!;
    private TextBox _payloadPreviewTextBox = null!;

    private ToolStripStatusLabel _portStatusLabel = null!;
    private ToolStripStatusLabel _presetStatusLabel = null!;
    private ToolStripStatusLabel _payloadStatusLabel = null!;

    public MainForm(SerialTransport transport)
    {
        _transport = transport;
        _controller = new ScoreboardController(_settingsStore.Load());

        InitializeComponent();
        WireEvents();
        BindSettingsFromModel();
        RefreshPortList(_controller.Settings.SelectedPort);
        UpdatePortStatus();

        _controller.StateChanged += ControllerOnStateChanged;
        _controller.RefreshDisplay();

        _mainClockTimer.Start();
        _mainSignalTimer.Start();
        _shotClockSignalTimer.Start();
        _displayRefreshTimer.Start();
        _sendTimer.Start();
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        Text = "Sports Scoreboard Modern";
        MinimumSize = new Size(1240, 860);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = CanvasColor;
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            ItemSize = new Size(140, 34),
            SizeMode = TabSizeMode.Fixed,
        };

        var gameTab = new TabPage("Game") { BackColor = CanvasColor };
        var settingsTab = new TabPage("Settings") { BackColor = CanvasColor };
        var aboutTab = new TabPage("About") { BackColor = CanvasColor };

        gameTab.Controls.Add(BuildGameTab());
        settingsTab.Controls.Add(BuildSettingsTab());
        aboutTab.Controls.Add(BuildAboutTab());

        tabControl.TabPages.Add(gameTab);
        tabControl.TabPages.Add(settingsTab);
        tabControl.TabPages.Add(aboutTab);

        var statusStrip = new StatusStrip
        {
            SizingGrip = false,
            BackColor = Color.White,
        };

        _portStatusLabel = new ToolStripStatusLabel("Port: closed") { Width = 220 };
        _presetStatusLabel = new ToolStripStatusLabel("Preset: 10:00") { Width = 180 };
        _payloadStatusLabel = new ToolStripStatusLabel("Payload: ") { Spring = true };

        statusStrip.Items.AddRange(new ToolStripItem[] { _portStatusLabel, _presetStatusLabel, _payloadStatusLabel });

        Controls.Add(tabControl);
        Controls.Add(statusStrip);

        ResumeLayout();
    }

    private Control BuildGameTab()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 3,
        };

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 280F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 140F));

        root.Controls.Add(BuildScoreboardPreview(), 0, 0);
        root.Controls.Add(BuildControlGrid(), 0, 1);
        root.Controls.Add(BuildRunningTextPanel(), 0, 2);

        return root;
    }

    private Control BuildScoreboardPreview()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ScoreboardColor,
            Padding = new Padding(24),
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 3,
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 26F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 48F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 26F));

        layout.Controls.Add(CreatePreviewHeader("TEAM A", ContentAlignment.MiddleLeft), 0, 0);
        layout.Controls.Add(CreatePreviewHeader("PERIOD", ContentAlignment.MiddleCenter), 1, 0);
        layout.Controls.Add(CreatePreviewHeader("TEAM B", ContentAlignment.MiddleRight), 2, 0);

        _scoreALabel = CreatePreviewValue("000", 68F, ScoreColor, ContentAlignment.MiddleLeft);
        _scoreBLabel = CreatePreviewValue("000", 68F, ScoreColor, ContentAlignment.MiddleRight);
        _periodLabel = CreatePreviewValue("1", 56F, MetaColor, ContentAlignment.MiddleCenter);

        layout.Controls.Add(_scoreALabel, 0, 1);
        layout.Controls.Add(_periodLabel, 1, 1);
        layout.Controls.Add(_scoreBLabel, 2, 1);

        var bottomLeft = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
        };

        bottomLeft.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottomLeft.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _penaltyTitleALabel = CreatePreviewHeader("FOULS", ContentAlignment.MiddleLeft);
        _penaltyALabel = CreatePreviewValue("0", 34F, MetaColor, ContentAlignment.MiddleLeft);
        bottomLeft.Controls.Add(_penaltyTitleALabel, 0, 0);
        bottomLeft.Controls.Add(_penaltyALabel, 1, 0);

        var bottomCenter = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            Margin = new Padding(0),
        };

        bottomCenter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        bottomCenter.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottomCenter.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottomCenter.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
        bottomCenter.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));

        _mainClockLabel = CreatePreviewValue("10:00", 58F, ClockColor, ContentAlignment.MiddleCenter);
        _shotClockLabel = CreatePreviewValue("24", 30F, ClockColor, ContentAlignment.MiddleRight);
        _shotClockTenthsLabel = CreatePreviewValue("0", 16F, ClockColor, ContentAlignment.BottomLeft);

        var shotClockHeader = CreatePreviewHeader("SHOT CLOCK", ContentAlignment.MiddleCenter);
        shotClockHeader.Margin = new Padding(0, 8, 0, 0);

        bottomCenter.Controls.Add(_mainClockLabel, 0, 0);
        bottomCenter.SetColumnSpan(_mainClockLabel, 3);
        bottomCenter.Controls.Add(shotClockHeader, 0, 1);
        bottomCenter.Controls.Add(_shotClockLabel, 1, 1);
        bottomCenter.Controls.Add(_shotClockTenthsLabel, 2, 1);

        var bottomRight = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
        };

        bottomRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        bottomRight.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _penaltyTitleBLabel = CreatePreviewHeader("FOULS", ContentAlignment.MiddleRight);
        _penaltyBLabel = CreatePreviewValue("0", 34F, MetaColor, ContentAlignment.MiddleRight);
        bottomRight.Controls.Add(_penaltyTitleBLabel, 0, 0);
        bottomRight.Controls.Add(_penaltyBLabel, 1, 0);

        layout.Controls.Add(bottomLeft, 0, 2);
        layout.Controls.Add(bottomCenter, 1, 2);
        layout.Controls.Add(bottomRight, 2, 2);

        panel.Controls.Add(layout);

        return panel;
    }
    private Control BuildControlGrid()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            Padding = new Padding(0, 18, 0, 12),
        };

        for (var index = 0; index < 3; index++)
        {
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        }

        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        layout.Controls.Add(BuildScoreGroup("Score A", () => _controller.IncreaseScoreA(), () => _controller.DecreaseScoreA()), 0, 0);
        layout.Controls.Add(BuildClockGroup(), 1, 0);
        layout.Controls.Add(BuildScoreGroup("Score B", () => _controller.IncreaseScoreB(), () => _controller.DecreaseScoreB()), 2, 0);
        layout.Controls.Add(BuildPenaltyGroup("Fouls A", () => _controller.IncreasePenaltyA(), () => _controller.DecreasePenaltyA(), out _penaltyAGroup), 0, 1);
        layout.Controls.Add(BuildShotClockGroup(), 1, 1);
        layout.Controls.Add(BuildPenaltyGroup("Fouls B", () => _controller.IncreasePenaltyB(), () => _controller.DecreasePenaltyB(), out _penaltyBGroup), 2, 1);

        return layout;
    }

    private Control BuildRunningTextPanel()
    {
        var group = CreateSurfaceGroup("Running Text");
        group.Dock = DockStyle.Fill;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var description = new Label
        {
            AutoSize = true,
            Text = "The text field is appended to the legacy AT+GD payload.",
            ForeColor = MutedTextColor,
            Margin = new Padding(0, 0, 0, 8),
        };

        _runningTextCheckBox = new CheckBox
        {
            Text = "Enable running text",
            AutoSize = true,
            Margin = new Padding(0, 3, 0, 0),
        };

        _runningTextTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            MaxLength = 24,
            Font = new Font("Segoe UI", 11F, FontStyle.Regular),
        };

        var hint = new Label
        {
            AutoSize = true,
            Text = "Legacy device limit: 24 characters.",
            ForeColor = MutedTextColor,
            TextAlign = ContentAlignment.MiddleRight,
            Dock = DockStyle.Right,
            Margin = new Padding(0, 6, 0, 0),
        };

        layout.Controls.Add(description, 0, 0);
        layout.SetColumnSpan(description, 3);
        layout.Controls.Add(_runningTextCheckBox, 0, 1);
        layout.Controls.Add(_runningTextTextBox, 1, 1);
        layout.Controls.Add(hint, 2, 1);

        group.Controls.Add(layout);

        return group;
    }

    private Control BuildSettingsTab()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 3,
        };

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 145F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 260F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        root.Controls.Add(BuildPortSettingsGroup(), 0, 0);
        root.Controls.Add(BuildGameSettingsGroup(), 0, 1);
        root.Controls.Add(BuildPayloadGroup(), 0, 2);

        return root;
    }

    private Control BuildPortSettingsGroup()
    {
        var group = CreateSurfaceGroup("Connection");

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 3,
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        for (var row = 0; row < 3; row++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        _portComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 140,
        };

        var refreshPortsButton = CreateActionButton("Refresh Ports", null);
        refreshPortsButton.Click += (_, _) => RefreshPortList(_portComboBox.Text);

        _togglePortButton = CreateActionButton("Open Port", null);
        var syncTimeButton = CreateActionButton("Sync Device Time", null);

        _currentTimeLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(44, 62, 80),
            TextAlign = ContentAlignment.MiddleRight,
        };

        layout.Controls.Add(CreateSettingsLabel("COM port"), 0, 0);
        layout.Controls.Add(_portComboBox, 1, 0);
        layout.Controls.Add(refreshPortsButton, 2, 0);
        layout.Controls.Add(_togglePortButton, 3, 0);
        layout.Controls.Add(_currentTimeLabel, 4, 0);

        layout.Controls.Add(CreateSettingsLabel("Protocol"), 0, 1);
        var protocolValue = CreateReadOnlyValue("AT+GD / AT+ST + CRC16, 9600 8N1");
        layout.Controls.Add(protocolValue, 1, 1);
        layout.SetColumnSpan(protocolValue, 4);

        layout.Controls.Add(CreateSettingsLabel("Device clock"), 0, 2);
        layout.Controls.Add(syncTimeButton, 1, 2);

        group.Controls.Add(layout);

        _togglePortButton.Click += TogglePortButtonOnClick;
        syncTimeButton.Click += SyncTimeButtonOnClick;

        return group;
    }

    private Control BuildGameSettingsGroup()
    {
        var group = CreateSurfaceGroup("Game Settings");

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 4,
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        for (var row = 0; row < 4; row++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        _timerPresetButton = CreateActionButton("Set Timer Preset", null);
        _gameModeComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        _gameModeComboBox.Items.AddRange(["Basketball", "Volleyball"]);

        _fontModeComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        _fontModeComboBox.Items.AddRange(["Font 6x8", "Font 8x8"]);

        _timerDirectionComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        _timerDirectionComboBox.Items.AddRange(["Count Up", "Count Down"]);

        _signalDurationNumeric = new NumericUpDown { Minimum = 0, Maximum = 9, Value = 3 };
        _shotClockSignalNumeric = new NumericUpDown { Minimum = 5, Maximum = 30, Value = 15 };

        _foulsToFiveCheckBox = new CheckBox
        {
            Text = "Basketball fouls cap at 5",
            AutoSize = true,
        };

        _autoStartShotClockCheckBox = new CheckBox
        {
            Text = "Auto-start shot clock after 24/14 reset",
            AutoSize = true,
        };

        layout.Controls.Add(CreateSettingsLabel("Timer preset"), 0, 0);
        layout.Controls.Add(_timerPresetButton, 1, 0);
        layout.Controls.Add(CreateSettingsLabel("Game mode"), 2, 0);
        layout.Controls.Add(_gameModeComboBox, 3, 0);

        layout.Controls.Add(CreateSettingsLabel("Main buzzer, sec"), 0, 1);
        layout.Controls.Add(_signalDurationNumeric, 1, 1);
        layout.Controls.Add(CreateSettingsLabel("Font mode"), 2, 1);
        layout.Controls.Add(_fontModeComboBox, 3, 1);

        layout.Controls.Add(CreateSettingsLabel("24 sec buzzer, tenths"), 0, 2);
        layout.Controls.Add(_shotClockSignalNumeric, 1, 2);
        layout.Controls.Add(CreateSettingsLabel("Timer direction"), 2, 2);
        layout.Controls.Add(_timerDirectionComboBox, 3, 2);

        layout.Controls.Add(_foulsToFiveCheckBox, 0, 3);
        layout.SetColumnSpan(_foulsToFiveCheckBox, 2);
        layout.Controls.Add(_autoStartShotClockCheckBox, 2, 3);
        layout.SetColumnSpan(_autoStartShotClockCheckBox, 2);

        group.Controls.Add(layout);

        _timerPresetButton.Click += TimerPresetButtonOnClick;
        _gameModeComboBox.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressUiEvents)
            {
                return;
            }

            _controller.SetGameMode(_gameModeComboBox.SelectedIndex == 1 ? GameMode.Volleyball : GameMode.Basketball);
        };
        _fontModeComboBox.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressUiEvents)
            {
                return;
            }

            _controller.SetFontMode(_fontModeComboBox.SelectedIndex == 1 ? FontMode.Font8x8 : FontMode.Font6x8);
        };
        _timerDirectionComboBox.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressUiEvents)
            {
                return;
            }

            _controller.SetTimerDirection(_timerDirectionComboBox.SelectedIndex == 0 ? TimerDirection.Up : TimerDirection.Down);
        };
        _signalDurationNumeric.ValueChanged += (_, _) =>
        {
            if (_suppressUiEvents)
            {
                return;
            }

            _controller.SetMainSignalDurationSeconds((int)_signalDurationNumeric.Value);
        };
        _shotClockSignalNumeric.ValueChanged += (_, _) =>
        {
            if (_suppressUiEvents)
            {
                return;
            }

            _controller.SetShotClockSignalDurationTenths((int)_shotClockSignalNumeric.Value);
        };
        _foulsToFiveCheckBox.CheckedChanged += (_, _) =>
        {
            if (_suppressUiEvents)
            {
                return;
            }

            _controller.SetCountFoulsToFive(_foulsToFiveCheckBox.Checked);
        };
        _autoStartShotClockCheckBox.CheckedChanged += (_, _) =>
        {
            if (_suppressUiEvents)
            {
                return;
            }

            _controller.SetAutoStartShotClock(_autoStartShotClockCheckBox.Checked);
        };

        return group;
    }

    private Control BuildPayloadGroup()
    {
        var group = CreateSurfaceGroup("Payload Preview");

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
        };

        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var description = new Label
        {
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10),
            Text = "The field below shows the exact legacy body that is appended to AT+GD before CRC16 is calculated.",
            ForeColor = MutedTextColor,
        };

        _payloadPreviewTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 10F, FontStyle.Regular),
            BackColor = Color.White,
        };

        layout.Controls.Add(description, 0, 0);
        layout.Controls.Add(_payloadPreviewTextBox, 0, 1);

        group.Controls.Add(layout);

        return group;
    }

    private Control BuildAboutTab()
    {
        var box = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            BackColor = CanvasColor,
            Font = new Font("Segoe UI", 11F, FontStyle.Regular),
            Text =
                "Sports Scoreboard Modern\r\n\r\n" +
                "This project is a C# / WinForms migration of the legacy Borland C++Builder scoreboard controller found in C:\\WORK\\final.\r\n\r\n" +
                "Implemented legacy behaviors:\r\n" +
                "- Basketball scoreboard flow with score, period, fouls, buzzer and 24/14 second shot clock\r\n" +
                "- Volleyball scoreboard mode with set counting\r\n" +
                "- Serial payload generation via AT+GD and time sync via AT+ST\r\n" +
                "- CRC16 calculation and legacy byte mapping used by the original protocol\r\n" +
                "- Persisted application settings in JSON for Visual Studio/.NET workflow\r\n\r\n" +
                "Hardware note:\r\n" +
                "The original application used low-level Win32 COM handling and toggled RS-485 control lines directly. This modern version uses System.IO.Ports and preserves the payload/protocol, but the final hardware validation should still be done against the real display controller.",
        };

        return box;
    }

    private Control BuildScoreGroup(string title, Action incrementAction, Action decrementAction)
    {
        var group = CreateSurfaceGroup(title);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        layout.Controls.Add(CreatePrimaryButton("+1", (_, _) => incrementAction()), 0, 0);
        layout.Controls.Add(CreatePrimaryButton("-1", (_, _) => decrementAction()), 0, 1);

        group.Controls.Add(layout);

        return group;
    }

    private Control BuildPenaltyGroup(string title, Action incrementAction, Action decrementAction, out GroupBox group)
    {
        group = CreateSurfaceGroup(title);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        layout.Controls.Add(CreatePrimaryButton("+1", (_, _) => incrementAction()), 0, 0);
        layout.Controls.Add(CreatePrimaryButton("-1", (_, _) => decrementAction()), 0, 1);

        group.Controls.Add(layout);

        return group;
    }

    private Control BuildClockGroup()
    {
        var group = CreateSurfaceGroup("Main Controls");

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));

        _gameClockButton = CreatePrimaryButton("Start", (_, _) => _controller.ToggleGameClock());
        _stopClockButton = CreateSecondaryButton("Stop", (_, _) => _controller.StopGameClock());
        _periodButton = CreateSecondaryButton("Period", (_, _) => _controller.AdvancePeriodOrSet());
        _resetButton = CreateSecondaryButton("Reset", (_, _) => _controller.Reset());
        _signalButton = CreateSecondaryButton("Signal", null);

        _mainSignalIndicator = CreateIndicatorPanel();

        var signalRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
        };

        signalRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        signalRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34F));
        signalRow.Controls.Add(_signalButton, 0, 0);
        signalRow.Controls.Add(_mainSignalIndicator, 1, 0);

        layout.Controls.Add(_gameClockButton, 0, 0);
        layout.Controls.Add(_stopClockButton, 1, 0);
        layout.Controls.Add(_periodButton, 0, 1);
        layout.Controls.Add(_resetButton, 1, 1);
        layout.Controls.Add(signalRow, 0, 2);
        layout.SetColumnSpan(signalRow, 2);

        group.Controls.Add(layout);

        _signalButton.MouseDown += (_, _) => _controller.StartManualSignal();
        _signalButton.MouseUp += (_, _) => _controller.StopManualSignal();
        _signalButton.MouseCaptureChanged += (_, _) => _controller.StopManualSignal();

        return group;
    }

    private Control BuildShotClockGroup()
    {
        _shotClockGroup = CreateSurfaceGroup("24 / 14 Seconds");

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 3,
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 16F));

        var shotClock24Button = CreatePrimaryButton("24", (_, _) => _controller.SetShotClock24());
        var shotClock14Button = CreatePrimaryButton("14", (_, _) => _controller.SetShotClock14());
        _shotClockButton = CreateSecondaryButton("Start", (_, _) => _controller.ToggleShotClock());
        _shotClockIndicator = CreateIndicatorPanel();

        layout.Controls.Add(shotClock24Button, 0, 0);
        layout.Controls.Add(shotClock14Button, 1, 0);
        layout.Controls.Add(_shotClockButton, 2, 0);
        layout.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Status",
            ForeColor = MutedTextColor,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0, 7, 8, 0),
        }, 1, 1);
        layout.Controls.Add(_shotClockIndicator, 2, 1);
        var hint = new Label
        {
            AutoSize = true,
            Text = "Shot clock follows the legacy 24/14 reset behavior.",
            ForeColor = MutedTextColor,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 10, 0, 0),
        };
        layout.Controls.Add(hint, 0, 2);
        layout.SetColumnSpan(hint, 3);

        _shotClockGroup.Controls.Add(layout);

        return _shotClockGroup;
    }
    private void WireEvents()
    {
        _mainClockTimer.Tick += (_, _) => _controller.TickMainClock();
        _mainSignalTimer.Tick += (_, _) => _controller.TickMainSignal();
        _shotClockSignalTimer.Tick += (_, _) => _controller.TickShotClockSignal();
        _displayRefreshTimer.Tick += (_, _) => _controller.RefreshDisplay();
        _sendTimer.Tick += SendTimerOnTick;

        _runningTextCheckBox.CheckedChanged += (_, _) =>
        {
            if (_suppressUiEvents)
            {
                return;
            }

            _controller.SetRunningTextEnabled(_runningTextCheckBox.Checked);
        };

        _runningTextTextBox.TextChanged += (_, _) =>
        {
            if (_suppressUiEvents)
            {
                return;
            }

            _controller.SetRunningText(_runningTextTextBox.Text);
        };

        FormClosing += OnFormClosing;
    }

    private void BindSettingsFromModel()
    {
        _suppressUiEvents = true;

        var settings = _controller.Settings;

        _runningTextCheckBox.Checked = settings.RunningTextEnabled;
        _runningTextTextBox.Text = settings.RunningText;
        _gameModeComboBox.SelectedIndex = settings.GameMode == GameMode.Volleyball ? 1 : 0;
        _fontModeComboBox.SelectedIndex = settings.FontMode == FontMode.Font8x8 ? 1 : 0;
        _timerDirectionComboBox.SelectedIndex = settings.TimerDirection == TimerDirection.Up ? 0 : 1;
        _signalDurationNumeric.Value = settings.MainSignalDurationSeconds;
        _shotClockSignalNumeric.Value = settings.ShotClockSignalDurationTenths;
        _foulsToFiveCheckBox.Checked = settings.CountFoulsToFive;
        _autoStartShotClockCheckBox.Checked = settings.AutoStartShotClock;

        _suppressUiEvents = false;
    }

    private void ControllerOnStateChanged(object? sender, ScoreboardSnapshot snapshot)
    {
        _scoreALabel.Text = snapshot.ScoreAText;
        _scoreBLabel.Text = snapshot.ScoreBText;
        _periodLabel.Text = snapshot.PeriodText;
        _mainClockLabel.Text = snapshot.MainClockText;
        _penaltyALabel.Text = snapshot.PenaltyAText;
        _penaltyBLabel.Text = snapshot.PenaltyBText;
        _penaltyTitleALabel.Text = snapshot.PenaltyLabelText;
        _penaltyTitleBLabel.Text = snapshot.PenaltyLabelText;
        _shotClockLabel.Text = snapshot.ShotClockSecondsText;
        _shotClockTenthsLabel.Text = snapshot.ShotClockTenthsText;
        _gameClockButton.Text = snapshot.GameClockActionText;
        _shotClockButton.Text = snapshot.ShotClockActionText;

        _mainSignalIndicator.BackColor = snapshot.IsMainSignalActive ? WarningColor : Color.FromArgb(209, 215, 224);
        _shotClockIndicator.BackColor = snapshot.IsShotClockRunning ? AccentColor : Color.FromArgb(209, 215, 224);

        _payloadPreviewTextBox.Text = $"AT+GD{snapshot.PayloadText}";
        _presetStatusLabel.Text = $"Preset: {snapshot.TimerPresetText}";
        _payloadStatusLabel.Text = $"Payload: {snapshot.PayloadText}";
        _currentTimeLabel.Text = $"Host clock: {DateTime.Now:HH:mm:ss}";

        var isBasketball = snapshot.GameMode == GameMode.Basketball;

        _shotClockGroup.Enabled = isBasketball;
        _stopClockButton.Enabled = isBasketball;
        _timerPresetButton.Enabled = isBasketball;
        _autoStartShotClockCheckBox.Enabled = isBasketball;
        _foulsToFiveCheckBox.Enabled = isBasketball;
        _timerDirectionComboBox.Enabled = isBasketball && !snapshot.IsGameClockRunning;

        _penaltyAGroup.Text = isBasketball ? "Fouls A" : "Sets A";
        _penaltyBGroup.Text = isBasketball ? "Fouls B" : "Sets B";

        if (_gameModeComboBox.SelectedIndex != (isBasketball ? 0 : 1))
        {
            _suppressUiEvents = true;
            _gameModeComboBox.SelectedIndex = isBasketball ? 0 : 1;
            _suppressUiEvents = false;
        }
    }

    private void SendTimerOnTick(object? sender, EventArgs e)
    {
        if (!_transport.IsOpen)
        {
            return;
        }

        try
        {
            var packet = SerialProtocol.CreateGamePacket(_controller.Snapshot);
            _transport.Write(packet);
        }
        catch (Exception exception)
        {
            _transport.Close();
            UpdatePortStatus();
            MessageBox.Show(this, exception.Message, "Serial Transmission Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void TimerPresetButtonOnClick(object? sender, EventArgs e)
    {
        using var dialog = new TimerPresetDialog(_controller.Settings.GameTimePreset);

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _controller.SetTimerPreset(dialog.Minutes, dialog.Seconds, dialog.Tenths);
        }
    }

    private void TogglePortButtonOnClick(object? sender, EventArgs e)
    {
        try
        {
            if (_transport.IsOpen)
            {
                _transport.Close();
            }
            else
            {
                var portName = _portComboBox.Text;
                _transport.Open(portName);
                _controller.Settings.SelectedPort = portName;
            }

            UpdatePortStatus();
        }
        catch (Exception exception)
        {
            _transport.Close();
            UpdatePortStatus();
            MessageBox.Show(this, exception.Message, "COM Port Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void SyncTimeButtonOnClick(object? sender, EventArgs e)
    {
        if (!_transport.IsOpen)
        {
            MessageBox.Show(this, "Open the COM port before syncing time.", "Port Closed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            var packet = SerialProtocol.CreateTimeSyncPacket(DateTime.Now);
            _transport.Write(packet);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Time Sync Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void RefreshPortList(string? preferredPort = null)
    {
        var ports = _transport.GetAvailablePorts();
        var selectedPort = !string.IsNullOrWhiteSpace(preferredPort) ? preferredPort : _portComboBox.Text;

        _portComboBox.BeginUpdate();
        _portComboBox.Items.Clear();
        _portComboBox.Items.AddRange(ports);
        _portComboBox.EndUpdate();

        if (!string.IsNullOrWhiteSpace(selectedPort) && ports.Contains(selectedPort, StringComparer.OrdinalIgnoreCase))
        {
            _portComboBox.SelectedItem = ports.First(port => string.Equals(port, selectedPort, StringComparison.OrdinalIgnoreCase));
        }
        else if (ports.Length > 0)
        {
            _portComboBox.SelectedIndex = 0;
        }

        if (_portComboBox.SelectedItem is string currentPort)
        {
            _controller.Settings.SelectedPort = currentPort;
        }
    }

    private void UpdatePortStatus()
    {
        if (_transport.IsOpen)
        {
            _portStatusLabel.Text = $"Port: open ({_transport.PortName})";
            _togglePortButton.Text = "Close Port";
        }
        else
        {
            _portStatusLabel.Text = "Port: closed";
            _togglePortButton.Text = "Open Port";
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        _mainClockTimer.Stop();
        _mainSignalTimer.Stop();
        _shotClockSignalTimer.Stop();
        _displayRefreshTimer.Stop();
        _sendTimer.Stop();

        _settingsStore.Save(_controller.Settings);
        _transport.Close();
    }

    private static GroupBox CreateSurfaceGroup(string title)
    {
        return new GroupBox
        {
            Text = title,
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            Margin = new Padding(8),
            BackColor = PanelColor,
            ForeColor = Color.FromArgb(44, 62, 80),
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
        };
    }

    private static Label CreatePreviewHeader(string text, ContentAlignment alignment)
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            Text = text,
            TextAlign = alignment,
            Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
            ForeColor = Color.FromArgb(211, 218, 227),
            AutoSize = false,
        };
    }

    private static Label CreatePreviewValue(string text, float size, Color color, ContentAlignment alignment)
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            Text = text,
            TextAlign = alignment,
            Font = new Font("Bahnschrift", size, FontStyle.Bold),
            ForeColor = color,
            AutoSize = false,
        };
    }

    private static Button CreatePrimaryButton(string text, EventHandler? handler)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            Height = 44,
            Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
        };

        button.FlatAppearance.BorderColor = Color.FromArgb(218, 224, 231);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 248);

        if (handler is not null)
        {
            button.Click += handler;
        }

        return button;
    }

    private static Button CreateSecondaryButton(string text, EventHandler? handler)
    {
        var button = CreatePrimaryButton(text, handler);
        button.BackColor = Color.FromArgb(248, 250, 252);
        return button;
    }

    private static Button CreateActionButton(string text, EventHandler? handler)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 34,
            Padding = new Padding(12, 4, 12, 4),
        };

        if (handler is not null)
        {
            button.Click += handler;
        }

        return button;
    }

    private static Control CreateReadOnlyValue(string text)
    {
        return new Label
        {
            AutoSize = true,
            Text = text,
            ForeColor = Color.FromArgb(44, 62, 80),
            Padding = new Padding(0, 6, 0, 0),
        };
    }

    private static Label CreateSettingsLabel(string text)
    {
        return new Label
        {
            AutoSize = true,
            Text = text,
            ForeColor = MutedTextColor,
            Padding = new Padding(0, 6, 12, 0),
        };
    }

    private static Panel CreateIndicatorPanel()
    {
        return new Panel
        {
            Width = 22,
            Height = 22,
            Margin = new Padding(6, 10, 0, 0),
            BackColor = Color.FromArgb(209, 215, 224),
        };
    }
}
