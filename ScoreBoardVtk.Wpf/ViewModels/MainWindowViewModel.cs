using ScoreBoardVtk.Core.Services;

namespace ScoreBoardVtk.Wpf.ViewModels;

public sealed class MainWindowViewModel : IDisposable
{
    public MainWindowViewModel(SettingsStore settingsStore, IScoreboardApi scoreboard, IScoreboardRuntime runtime)
    {
        Workspace = new ScoreboardWorkspaceViewModel(settingsStore, scoreboard, runtime);
        GameTab = new GameTabViewModel(Workspace);
        SettingsTab = new SettingsTabViewModel(Workspace);
    }

    public ScoreboardWorkspaceViewModel Workspace { get; }

    public GameTabViewModel GameTab { get; }

    public SettingsTabViewModel SettingsTab { get; }

    public void Dispose()
    {
        Workspace.Dispose();
    }
}
