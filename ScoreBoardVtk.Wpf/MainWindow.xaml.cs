using System.Windows;
using System.Windows.Input;
using ScoreBoardVtk.Core.Services;
using ScoreBoardVtk.Wpf.ViewModels;

namespace ScoreBoardVtk.Wpf;

public partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainViewModel(new SettingsStore(), new SerialTransport());
        DataContext = ViewModel;
    }

    protected override void OnClosed(EventArgs e)
    {
        ViewModel.Dispose();
        base.OnClosed(e);
    }

    private void SignalButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        ViewModel.StartManualSignal();
    }

    private void SignalButton_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ViewModel.StopManualSignal();
    }

    private void SignalButton_OnLostMouseCapture(object sender, MouseEventArgs e)
    {
        ViewModel.StopManualSignal();
    }
}
