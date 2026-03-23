using System.IO.Ports;

namespace SportsScoreboardModern.Core.Services;

public sealed class SerialTransport : IDisposable
{
    private SerialPort? _port;

    public bool IsOpen => _port?.IsOpen == true;

    public string PortName => _port?.PortName ?? string.Empty;

    public string[] GetAvailablePorts()
    {
        return SerialPort.GetPortNames()
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

        _port = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One)
        {
            Handshake = Handshake.None,
            DtrEnable = false,
            RtsEnable = false,
            ReadTimeout = 500,
            WriteTimeout = 500,
        };

        _port.Open();
    }

    public void Write(byte[] data)
    {
        if (!IsOpen || _port is null)
        {
            return;
        }

        _port.Write(data, 0, data.Length);
    }

    public void Close()
    {
        if (_port is null)
        {
            return;
        }

        try
        {
            if (_port.IsOpen)
            {
                _port.Close();
            }
        }
        finally
        {
            _port.Dispose();
            _port = null;
        }
    }

    public void Dispose()
    {
        Close();
    }
}
