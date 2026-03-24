using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScoreBoardVtk.Core.Models;
using ScoreBoardVtk.Wpf.Models;

namespace ScoreBoardVtk.Wpf.Views;

public partial class SetValuesWindow : Window
{
    private readonly GameMode _gameMode;
    private readonly int _defaultMainClockTenths;

    public SetValuesWindow(ScoreboardState state, int defaultMainClockTenths)
    {
        _gameMode = state.GameMode;
        _defaultMainClockTenths = Math.Clamp(defaultMainClockTenths, 0, 59 * 60 * 10 + 59 * 10 + 9);
        PeriodOptions = state.GameMode == GameMode.Basketball
            ?
            [
                new OptionItem<int>(1, "1"),
                new OptionItem<int>(2, "2"),
                new OptionItem<int>(3, "3"),
                new OptionItem<int>(4, "4"),
                new OptionItem<int>(5, "E"),
            ]
            :
            [
                new OptionItem<int>(1, "1"),
                new OptionItem<int>(2, "2"),
                new OptionItem<int>(3, "3"),
                new OptionItem<int>(4, "4"),
                new OptionItem<int>(5, "5"),
            ];

        MinuteOptions = Enumerable.Range(0, 60).ToArray();
        SecondOptions = Enumerable.Range(0, 60).ToArray();
        TenthsOptions = Enumerable.Range(0, 10).ToArray();
        ShotSecondOptions = state.GameMode == GameMode.Basketball
            ? Enumerable.Range(0, 25).ToArray()
            : [0];
        SecondaryCounterLabel = state.SecondaryCounterKind == SecondaryCounterKind.Fouls ? "Fouls" : "Sets";

        ApplyState(state);

        InitializeComponent();
        DataContext = this;
    }

    public ManualScoreboardValues? Result { get; private set; }

    public IReadOnlyList<OptionItem<int>> PeriodOptions { get; }

    public IReadOnlyList<int> MinuteOptions { get; }

    public IReadOnlyList<int> SecondOptions { get; }

    public IReadOnlyList<int> TenthsOptions { get; }

    public IReadOnlyList<int> ShotSecondOptions { get; }

    public string SecondaryCounterLabel { get; }

    public string HomeScoreText { get; set; } = "0";

    public string GuestScoreText { get; set; } = "0";

    public string HomeSecondaryText { get; set; } = "0";

    public string GuestSecondaryText { get; set; } = "0";

    public int SelectedPeriod { get; set; } = 1;

    public int MainMinutes { get; set; }

    public int MainSeconds { get; set; }

    public int MainTenths { get; set; }

    public int ShotSeconds { get; set; }

    public int ShotTenths { get; set; }

    private void OkButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryParseInteger(HomeScoreText, "Home score", 0, 999, out var homeScore) ||
            !TryParseInteger(GuestScoreText, "Guest score", 0, 999, out var guestScore))
        {
            return;
        }

        if (!TryParseInteger(HomeSecondaryText, $"Home {SecondaryCounterLabel.ToLowerInvariant()}", 0, 9, out var homeSecondary) ||
            !TryParseInteger(GuestSecondaryText, $"Guest {SecondaryCounterLabel.ToLowerInvariant()}", 0, 9, out var guestSecondary))
        {
            return;
        }

        var periodMax = _gameMode == GameMode.Basketball ? 5 : 5;
        if (SelectedPeriod < 1 || SelectedPeriod > periodMax)
        {
            ShowValidationMessage($"Period must be between 1 and {periodMax}.");
            return;
        }

        if (MainMinutes is < 0 or > 59 || MainSeconds is < 0 or > 59 || MainTenths is < 0 or > 9)
        {
            ShowValidationMessage("Game clock must be within 00:00.0 and 59:59.9.");
            return;
        }

        if (ShotSeconds is < 0 or > 24 || ShotTenths is < 0 or > 9 || (ShotSeconds == 24 && ShotTenths > 0))
        {
            ShowValidationMessage("24-second clock must be within 00.0 and 24.0.");
            return;
        }

        if (_gameMode != GameMode.Basketball && (ShotSeconds != 0 || ShotTenths != 0))
        {
            ShowValidationMessage("24-second clock is available only in basketball mode.");
            return;
        }

        Result = new ManualScoreboardValues(
            homeScore,
            guestScore,
            homeSecondary,
            guestSecondary,
            SelectedPeriod,
            (MainMinutes * 60 * 10) + (MainSeconds * 10) + MainTenths,
            (ShotSeconds * 10) + ShotTenths);

        DialogResult = true;
    }

    private void ResetAllButton_OnClick(object sender, RoutedEventArgs e)
    {
        HomeScoreText = "0";
        GuestScoreText = "0";
        HomeSecondaryText = "0";
        GuestSecondaryText = "0";
        SelectedPeriod = 1;
        MainMinutes = _defaultMainClockTenths / 600;
        MainSeconds = (_defaultMainClockTenths / 10) % 60;
        MainTenths = _defaultMainClockTenths % 10;
        ShotSeconds = _gameMode == GameMode.Basketball ? 24 : 0;
        ShotTenths = 0;
        RefreshBindings();
    }

    private void ApplyState(ScoreboardState state)
    {
        HomeScoreText = state.HomeScore.ToString();
        GuestScoreText = state.GuestScore.ToString();
        HomeSecondaryText = state.HomeSecondaryCounter.ToString();
        GuestSecondaryText = state.GuestSecondaryCounter.ToString();
        SelectedPeriod = state.GameMode == GameMode.Basketball && state.PeriodNumber > 4 ? 5 : state.PeriodNumber;
        MainMinutes = state.MainClockTenths / 600;
        MainSeconds = (state.MainClockTenths / 10) % 60;
        MainTenths = state.MainClockTenths % 10;
        ShotSeconds = state.ShotClockTenths / 10;
        ShotTenths = state.ShotClockTenths % 10;
    }

    private void RefreshBindings()
    {
        DataContext = null;
        DataContext = this;
    }

    private static bool TryParseInteger(string text, string fieldName, int minValue, int maxValue, out int value)
    {
        if (int.TryParse(text?.Trim(), out value))
        {
            if (value >= minValue && value <= maxValue)
            {
                return true;
            }

            ShowValidationMessage($"{fieldName} must be between {minValue} and {maxValue}.");
            return false;
        }

        ShowValidationMessage($"{fieldName} must be an integer value.");
        return false;
    }

    private void NumericTextBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = e.Text.Any(character => !char.IsDigit(character));
    }

    private void NumericTextBox_OnPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)) &&
            e.DataObject.GetData(typeof(string)) is string text &&
            text.All(char.IsDigit))
        {
            return;
        }

        e.CancelCommand();
    }

    private static void ShowValidationMessage(string message)
    {
        MessageBox.Show(
            message,
            "Invalid Value",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }
}
