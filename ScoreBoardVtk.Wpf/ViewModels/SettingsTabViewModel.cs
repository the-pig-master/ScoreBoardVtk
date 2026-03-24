namespace ScoreBoardVtk.Wpf.ViewModels;

public sealed class SettingsTabViewModel
{
    public SettingsTabViewModel(ScoreboardWorkspaceViewModel workspace)
    {
        Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
    }

    public ScoreboardWorkspaceViewModel Workspace { get; }
}
