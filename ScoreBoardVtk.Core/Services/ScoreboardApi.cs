using ScoreBoardVtk.Core.Models;

namespace ScoreBoardVtk.Core.Services;

public sealed class ScoreboardApi : IScoreboardApi
{
    private readonly ScoreboardController _controller;
    private readonly IScoreboardProtocol _protocol;
    private readonly ISerialTransport _transport;
    private readonly TimeProvider _timeProvider;

    public ScoreboardApi(
        AppSettings settings,
        ISerialTransport transport,
        IScoreboardProtocol? protocol = null,
        TimeProvider? timeProvider = null)
    {
        _controller = new ScoreboardController(settings ?? new AppSettings());
        _protocol = protocol ?? new LegacyVtkProtocol();
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _timeProvider = timeProvider ?? TimeProvider.System;

        _controller.StateChanged += ControllerOnStateChanged;
        State = _controller.State;
        Snapshot = BuildSnapshot(State);
    }

    public event EventHandler<ScoreboardState>? StateChanged;

    public AppSettings Settings => _controller.Settings;

    public ScoreboardState State { get; private set; }

    public ScoreboardSnapshot Snapshot { get; private set; }

    public bool IsConnected => _transport.IsOpen;

    public string ConnectedPortName => _transport.PortName;

    public IReadOnlyList<string> GetAvailablePorts()
    {
        return _transport.GetAvailablePorts();
    }

    public void Execute(ScoreboardCommand command)
    {
        _controller.Apply(command);
    }

    public void Connect(string portName)
    {
        _transport.Open(portName);
        Settings.SelectedPort = portName?.Trim() ?? string.Empty;
    }

    public void Disconnect()
    {
        _transport.Close();
    }

    public void Publish()
    {
        if (!_transport.IsOpen)
        {
            return;
        }

        _transport.Write(_protocol.CreateGamePacket(State, GetCurrentTime()));
    }

    public void SyncClock(DateTime currentTime)
    {
        if (!_transport.IsOpen)
        {
            throw new InvalidOperationException("Open the COM port before syncing time.");
        }

        _transport.Write(_protocol.CreateTimeSyncPacket(currentTime));
    }

    public void Dispose()
    {
        _controller.StateChanged -= ControllerOnStateChanged;
        _transport.Dispose();
    }

    private void ControllerOnStateChanged(object? sender, ScoreboardState state)
    {
        State = state;
        Snapshot = BuildSnapshot(state);
        StateChanged?.Invoke(this, State);
    }

    private ScoreboardSnapshot BuildSnapshot(ScoreboardState state)
    {
        var currentTime = GetCurrentTime();
        var payload = _protocol.CreateGamePayload(state, currentTime);
        return ScoreboardSnapshotFactory.Create(state, currentTime, payload);
    }

    private DateTime GetCurrentTime()
    {
        return _timeProvider.GetLocalNow().DateTime;
    }
}
