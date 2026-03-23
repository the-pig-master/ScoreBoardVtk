namespace ScoreBoardVtk.Core.Services;

public interface ISerialTransport : IDisposable
{
    bool IsOpen { get; }

    string PortName { get; }

    string[] GetAvailablePorts();

    void Open(string portName);

    void Write(byte[] data);

    void Close();
}
