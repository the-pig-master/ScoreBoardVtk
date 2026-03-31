namespace ScoreBoardVtk.Core.Services;

public interface IScoreboardRuntime : IDisposable
{
    event EventHandler<ScoreboardRuntimeFaultedEventArgs>? Faulted;

    bool IsRunning { get; }

    bool IsPublishEnabled { get; }

    void Start();

    void Stop();

    void SetPublishEnabled(bool enabled);
}
