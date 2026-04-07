using ScoreBoardVtk.Core.Models;

namespace ScoreBoardVtk.Core.Services;

public sealed class ScoreboardApi : IScoreboardApi
{
    private readonly ScoreboardController _controller;
    private readonly IScoreboardProtocol _protocol;
    private readonly ISerialTransport _transport;
    private readonly TimeProvider _timeProvider;
    private readonly object _sync = new();

    public ScoreboardApi(
        AppSettings settings,
        ISerialTransport transport,
        IScoreboardProtocol? protocol = null,
        TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _controller = new ScoreboardController(settings ?? new AppSettings(), _timeProvider);
        _protocol = protocol ?? new LegacyVtkProtocol(_controller.Settings);
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));

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
        lock (_sync)
        {
            return _transport.GetAvailablePorts();
        }
    }

    public void Execute(ScoreboardCommand command)
    {
        lock (_sync)
        {
            _controller.Apply(command);
        }
    }

    public void Connect(string portName)
    {
        lock (_sync)
        {
            _transport.Open(portName);
            Settings.SelectedPort = portName?.Trim() ?? string.Empty;
        }
    }

    public void Disconnect()
    {
        lock (_sync)
        {
            _transport.Close();
        }
    }

    public void Publish()
    {
        lock (_sync)
        {
            if (!_transport.IsOpen)
            {
                return;
            }

            _transport.Write(_protocol.CreateGamePacket(State, GetCurrentTime()));
        }
    }

    public void SendPayload(string payload)
    {
        lock (_sync)
        {
            if (!_transport.IsOpen)
            {
                return;
            }

            _transport.Write(_protocol.CreateGamePacket(payload));
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _controller.StateChanged -= ControllerOnStateChanged;
            _transport.Dispose();
        }
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
