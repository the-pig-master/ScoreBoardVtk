using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
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

    private void Window_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.None)
        {
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (ViewModel.Workspace.TryHandleKeyboardShortcutCapture(key))
        {
            e.Handled = true;
            return;
        }

        if (MainTabs.SelectedIndex != 0 || ShouldIgnoreKeyboardShortcut(e.OriginalSource as DependencyObject))
        {
            return;
        }

        if (ViewModel.Workspace.TryExecuteKeyboardShortcut(key))
        {
            e.Handled = true;
        }
    }

    private static bool ShouldIgnoreKeyboardShortcut(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is TextBoxBase or ComboBox or ComboBoxItem)
            {
                return true;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }
}
