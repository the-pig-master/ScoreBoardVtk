namespace ScoreBoardVtk.Wpf.ViewModels;

public sealed class GameTabViewModel
{
    public GameTabViewModel(ScoreboardWorkspaceViewModel workspace)
    {
        Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
    }

    public ScoreboardWorkspaceViewModel Workspace { get; }

    public void StartManualSignal()
    {
        Workspace.StartManualSignal();
    }

    public void StopManualSignal()
    {
        Workspace.StopManualSignal();
    }
}
