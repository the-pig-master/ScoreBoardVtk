namespace ScoreBoardVtk.Wpf.ViewModels;

public sealed class DebugTabViewModel
{
    public DebugTabViewModel(ScoreboardWorkspaceViewModel workspace)
    {
        Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
    }

    public ScoreboardWorkspaceViewModel Workspace { get; }
}
