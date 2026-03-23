namespace ScoreBoardVtk.Core.Services;

public interface IScoreboardRuntime : IDisposable
{
    event EventHandler<ScoreboardRuntimeFaultedEventArgs>? Faulted;

    bool IsRunning { get; }

    void Start();

    void Stop();
}
