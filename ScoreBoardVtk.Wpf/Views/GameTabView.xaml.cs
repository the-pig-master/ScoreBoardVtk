using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using ScoreBoardVtk.Core.Models;
using ScoreBoardVtk.Wpf.ViewModels;

namespace ScoreBoardVtk.Wpf.Views;

public partial class GameTabView : UserControl
{
    private GameTabViewModel? ViewModel => DataContext as GameTabViewModel;

    public GameTabView()
    {
        InitializeComponent();
    }

    private void SignalButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        ViewModel?.StartManualSignal();
    }

    private void SignalButton_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ViewModel?.StopManualSignal();
    }

    private void SignalButton_OnLostMouseCapture(object sender, MouseEventArgs e)
    {
        ViewModel?.StopManualSignal();
    }

    private void ResetButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.Workspace is null || !ViewModel.Workspace.CanResetTimers)
        {
            return;
        }

        var result = MessageBox.Show(
            "Reset the game and shot clocks?",
            "Confirm Reset",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            ViewModel.Workspace.ResetCommand.Execute(null);
        }
    }

    private void AdvancePeriodButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.Workspace is null || !ViewModel.Workspace.CanResetTimers)
        {
            return;
        }

        var state = ViewModel.Workspace.CurrentState;

        if (state.GameMode == GameMode.Basketball && state.MainClockTenths > 0)
        {
            var result = MessageBox.Show(
                "The main game clock has not expired. Switch period anyway?",
                "Confirm Period Change",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        ViewModel.Workspace.AdvancePeriodOrSetCommand.Execute(null);
    }

    private void SetButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.Workspace is null)
        {
            return;
        }

        var dialog = new SetValuesWindow(
            ViewModel.Workspace.CurrentState,
            ViewModel.Workspace.ConfiguredTimerPresetTenths)
        {
            Owner = Window.GetWindow(this),
        };

        if (dialog.ShowDialog() == true && dialog.Result is not null)
        {
            ViewModel.ApplyManualValues(dialog.Result);
        }
    }
}
