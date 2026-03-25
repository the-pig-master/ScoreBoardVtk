using ScoreBoardVtk.Core.Models;

namespace ScoreBoardVtk.Core.Services;

public interface IScoreboardApi : IDisposable
{
    event EventHandler<ScoreboardState>? StateChanged;

    AppSettings Settings { get; }

    ScoreboardState State { get; }

    ScoreboardSnapshot Snapshot { get; }

    bool IsConnected { get; }

    string ConnectedPortName { get; }

    IReadOnlyList<string> GetAvailablePorts();

    void Execute(ScoreboardCommand command);

    void Connect(string portName);

    void Disconnect();

    void Publish();
}
