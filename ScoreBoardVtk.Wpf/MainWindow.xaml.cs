using System.Windows;
using ScoreBoardVtk.Core.Services;
using ScoreBoardVtk.Wpf.ViewModels;

namespace ScoreBoardVtk.Wpf;

public partial class MainWindow : Window
{
    public MainWindowViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();
        var services = ScoreboardCompositionRoot.CreateDesktopServices();
        ViewModel = new MainWindowViewModel(services.SettingsStore, services.ScoreboardApi, services.Runtime);
        DataContext = ViewModel;
    }

    protected override void OnClosed(EventArgs e)
    {
        ViewModel.Dispose();
        base.OnClosed(e);
    }
}
