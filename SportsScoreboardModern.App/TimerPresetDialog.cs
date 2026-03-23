using System.Globalization;

namespace SportsScoreboardModern.App;

public sealed class TimerPresetDialog : Form
{
    private readonly NumericUpDown _minutesNumeric = new()
    {
        Minimum = 0,
        Maximum = 59,
        Width = 80,
        Font = new Font("Segoe UI", 11F, FontStyle.Regular),
    };

    private readonly NumericUpDown _secondsNumeric = new()
    {
        Minimum = 0,
        Maximum = 59,
        Width = 80,
        Font = new Font("Segoe UI", 11F, FontStyle.Regular),
    };

    private readonly NumericUpDown _tenthsNumeric = new()
    {
        Minimum = 0,
        Maximum = 9,
        Width = 80,
        Font = new Font("Segoe UI", 11F, FontStyle.Regular),
    };

    public TimerPresetDialog(string currentPreset)
    {
        Text = "Set Game Timer";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = Color.FromArgb(245, 247, 250);
        ClientSize = new Size(340, 180);

        var (minutes, seconds, tenths) = ParsePreset(currentPreset);
        _minutesNumeric.Value = minutes;
        _secondsNumeric.Value = seconds;
        _tenthsNumeric.Value = tenths;

        InitializeComponent();
    }

    public int Minutes => (int)_minutesNumeric.Value;

    public int Seconds => (int)_secondsNumeric.Value;

    public int Tenths => (int)_tenthsNumeric.Value;

    private void InitializeComponent()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 1,
            RowCount = 3,
        };

        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var titleLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Text = "Set the main game timer preset.",
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
            ForeColor = Color.FromArgb(36, 41, 46),
        };

        var valuesLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            Padding = new Padding(0, 12, 0, 12),
        };

        for (var index = 0; index < 3; index++)
        {
            valuesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        }

        valuesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        valuesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        valuesLayout.Controls.Add(CreateFieldLabel("Minutes"), 0, 0);
        valuesLayout.Controls.Add(CreateFieldLabel("Seconds"), 1, 0);
        valuesLayout.Controls.Add(CreateFieldLabel("Tenths"), 2, 0);
        valuesLayout.Controls.Add(_minutesNumeric, 0, 1);
        valuesLayout.Controls.Add(_secondsNumeric, 1, 1);
        valuesLayout.Controls.Add(_tenthsNumeric, 2, 1);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
        };

        var okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Width = 88,
            Height = 32,
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Width = 88,
            Height = 32,
        };

        buttons.Controls.Add(okButton);
        buttons.Controls.Add(cancelButton);

        AcceptButton = okButton;
        CancelButton = cancelButton;

        root.Controls.Add(titleLabel, 0, 0);
        root.Controls.Add(valuesLayout, 0, 1);
        root.Controls.Add(buttons, 0, 2);

        Controls.Add(root);
    }

    private static Label CreateFieldLabel(string text)
    {
        return new Label
        {
            AutoSize = true,
            Text = text,
            Margin = new Padding(0, 0, 0, 6),
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
            ForeColor = Color.FromArgb(90, 98, 108),
        };
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

        if (!TimeSpan.TryParseExact(trimmed, ["m\\:ss", "mm\\:ss"], CultureInfo.InvariantCulture, out var timeSpan))
        {
            return (10, 0, 0);
        }

        return ((int)timeSpan.TotalMinutes, timeSpan.Seconds, tenths);
    }
}
