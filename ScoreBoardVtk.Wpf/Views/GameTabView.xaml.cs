using System.Windows.Controls;
using System.Windows.Input;
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
}
