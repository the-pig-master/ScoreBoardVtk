namespace ScoreBoardVtk.Core.Services;

public sealed class MockSerialTransport : ISerialTransport
{
    public const string MockPortName = "MOCK";

    private readonly List<byte[]> _writes = [];
    private bool _isOpen;

    public bool IsOpen => _isOpen;

    public string PortName { get; private set; } = string.Empty;

    public IReadOnlyList<byte[]> Writes => _writes;

    public string[] GetAvailablePorts()
    {
        return [MockPortName];
    }

    public void Open(string portName)
    {
        if (!string.Equals(portName?.Trim(), MockPortName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unsupported mock port: {portName}");
        }

        PortName = MockPortName;
        _isOpen = true;
    }

    public void Write(byte[] data)
    {
        if (!_isOpen)
        {
            return;
        }

        _writes.Add(data.ToArray());
    }

    public void Close()
    {
        _isOpen = false;
        PortName = string.Empty;
    }

    public void Dispose()
    {
        Close();
    }
}
