using ScoreBoardVtk.Core.Models;

namespace ScoreBoardVtk.Core.Services;

public sealed class ScoreboardRuntime : IScoreboardRuntime
{
    private readonly IScoreboardApi _scoreboard;
    private readonly ScoreboardRuntimeOptions _options;

    private ScheduledLoop? _mainClockLoop;
    private ScheduledLoop? _mainSignalLoop;
    private ScheduledLoop? _shotClockSignalLoop;
    private ScheduledLoop? _displayRefreshLoop;
    private ScheduledLoop? _publishLoop;
    private bool _disposed;

    public ScoreboardRuntime(IScoreboardApi scoreboard, ScoreboardRuntimeOptions? options = null)
    {
        _scoreboard = scoreboard ?? throw new ArgumentNullException(nameof(scoreboard));
        _options = options ?? new ScoreboardRuntimeOptions();
    }

    public event EventHandler<ScoreboardRuntimeFaultedEventArgs>? Faulted;

    public bool IsRunning { get; private set; }

    public void Start()
    {
        ThrowIfDisposed();

        if (IsRunning)
        {
            return;
        }

        _mainClockLoop = new ScheduledLoop(_options.MainClockInterval, "TickMainClock", () => _scoreboard.Execute(new TickMainClockCommand()), OnFaulted);
        _mainSignalLoop = new ScheduledLoop(_options.MainSignalInterval, "TickMainSignal", () => _scoreboard.Execute(new TickMainSignalCommand()), OnFaulted);
        _shotClockSignalLoop = new ScheduledLoop(_options.ShotClockSignalInterval, "TickShotClockSignal", () => _scoreboard.Execute(new TickShotClockSignalCommand()), OnFaulted);
        _displayRefreshLoop = new ScheduledLoop(_options.DisplayRefreshInterval, "RefreshDisplay", () => _scoreboard.Execute(new RefreshDisplayCommand()), OnFaulted);
        _publishLoop = new ScheduledLoop(_options.PublishInterval, "Publish", () => _scoreboard.Publish(), OnFaulted);

        IsRunning = true;
        SafeInvoke("InitialRefresh", () => _scoreboard.Execute(new RefreshDisplayCommand()));
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        _mainClockLoop?.Dispose();
        _mainSignalLoop?.Dispose();
        _shotClockSignalLoop?.Dispose();
        _displayRefreshLoop?.Dispose();
        _publishLoop?.Dispose();

        _mainClockLoop = null;
        _mainSignalLoop = null;
        _shotClockSignalLoop = null;
        _displayRefreshLoop = null;
        _publishLoop = null;
        IsRunning = false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _disposed = true;
    }

    private void OnFaulted(string operationName, Exception exception)
    {
        Faulted?.Invoke(this, new ScoreboardRuntimeFaultedEventArgs(operationName, exception));
    }

    private void SafeInvoke(string operationName, Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            OnFaulted(operationName, exception);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private sealed class ScheduledLoop : IDisposable
    {
        private readonly Action _action;
        private readonly string _operationName;
        private readonly Action<string, Exception> _onFaulted;
        private readonly Timer _timer;
        private int _isExecuting;

        public ScheduledLoop(TimeSpan interval, string operationName, Action action, Action<string, Exception> onFaulted)
        {
            _action = action;
            _operationName = operationName;
            _onFaulted = onFaulted;
            _timer = new Timer(OnTick, null, interval, interval);
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        private void OnTick(object? state)
        {
            if (Interlocked.Exchange(ref _isExecuting, 1) == 1)
            {
                return;
            }

            try
            {
                _action();
            }
            catch (Exception exception)
            {
                _onFaulted(_operationName, exception);
            }
            finally
            {
                Volatile.Write(ref _isExecuting, 0);
            }
        }
    }
}
