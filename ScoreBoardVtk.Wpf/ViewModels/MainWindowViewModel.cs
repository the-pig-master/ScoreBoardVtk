using System.Windows;
using ScoreBoardVtk.Core.Services;

namespace ScoreBoardVtk.Wpf.ViewModels;

public sealed class MainWindowViewModel : IDisposable
{
    public MainWindowViewModel(SettingsStore settingsStore, IScoreboardApi scoreboard, IScoreboardRuntime runtime, bool showDebugTab)
    {
        ShowDebugTab = showDebugTab;
        Workspace = new ScoreboardWorkspaceViewModel(settingsStore, scoreboard, runtime);
        GameTab = new GameTabViewModel(Workspace);
        SettingsTab = new SettingsTabViewModel(Workspace);
        DebugTab = new DebugTabViewModel(Workspace);
    }

    public ScoreboardWorkspaceViewModel Workspace { get; }

    public GameTabViewModel GameTab { get; }

    public SettingsTabViewModel SettingsTab { get; }

    public DebugTabViewModel DebugTab { get; }

    public bool ShowDebugTab { get; }

    public Visibility DebugTabVisibility => ShowDebugTab ? Visibility.Visible : Visibility.Collapsed;

    public void Dispose()
    {
        Workspace.Dispose();
    }
}
