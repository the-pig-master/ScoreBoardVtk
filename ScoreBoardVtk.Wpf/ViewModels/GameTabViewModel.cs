namespace ScoreBoardVtk.Wpf.ViewModels;

using ScoreBoardVtk.Wpf.Models;

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

    public void ApplyManualValues(ManualScoreboardValues values)
    {
        Workspace.ApplyManualValues(values);
    }
}
