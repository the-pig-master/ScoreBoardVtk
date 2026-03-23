namespace ScoreBoardVtk.Core.Services;

public sealed class CompositeSerialTransport : ISerialTransport
{
    private readonly IReadOnlyList<ISerialTransport> _transports;
    private ISerialTransport? _activeTransport;

    public CompositeSerialTransport(params ISerialTransport[] transports)
    {
        ArgumentNullException.ThrowIfNull(transports);

        if (transports.Length == 0)
        {
            throw new ArgumentException("At least one transport is required.", nameof(transports));
        }

        _transports = transports;
    }

    public bool IsOpen => _activeTransport?.IsOpen == true;

    public string PortName => _activeTransport?.PortName ?? string.Empty;

    public string[] GetAvailablePorts()
    {
        return _transports
            .SelectMany(static transport => transport.GetAvailablePorts())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static port => port, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public void Open(string portName)
    {
        if (string.IsNullOrWhiteSpace(portName))
        {
            throw new InvalidOperationException("COM port is not selected.");
        }

        Close();

        foreach (var transport in _transports)
        {
            var supportedPorts = transport.GetAvailablePorts();
            if (!supportedPorts.Contains(portName, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            transport.Open(portName);
            _activeTransport = transport;
            return;
        }

        throw new InvalidOperationException($"Port '{portName}' is not available.");
    }

    public void Write(byte[] data)
    {
        _activeTransport?.Write(data);
    }

    public void Close()
    {
        if (_activeTransport is null)
        {
            return;
        }

        try
        {
            _activeTransport.Close();
        }
        finally
        {
            _activeTransport = null;
        }
    }

    public void Dispose()
    {
        Close();

        foreach (var transport in _transports)
        {
            transport.Dispose();
        }
    }
}
