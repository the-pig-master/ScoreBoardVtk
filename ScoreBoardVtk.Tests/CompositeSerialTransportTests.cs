using ScoreBoardVtk.Core.Services;

namespace ScoreBoardVtk.Tests;

public sealed class CompositeSerialTransportTests
{
    [Fact]
    public void GetAvailablePorts_MergesUnderlyingTransports()
    {
        using var transport = new CompositeSerialTransport(
            new FakeTransport("COM9"),
            new FakeTransport("COM1", "COM2"),
            new MockSerialTransport());

        var ports = transport.GetAvailablePorts();

        Assert.Equal(new[] { "COM1", "COM2", "COM9", MockSerialTransport.MockPortName }, ports);
    }

    [Fact]
    public void OpenAndWrite_RoutesToMatchingUnderlyingTransport()
    {
        var comTransport = new FakeTransport("COM7");
        var mockTransport = new MockSerialTransport();
        using var transport = new CompositeSerialTransport(comTransport, mockTransport);

        transport.Open(MockSerialTransport.MockPortName);
        transport.Write([1, 2, 3]);

        Assert.True(transport.IsOpen);
        Assert.Equal(MockSerialTransport.MockPortName, transport.PortName);
        Assert.Single(mockTransport.Writes);
        Assert.Equal([1, 2, 3], mockTransport.Writes[0]);
        Assert.Empty(comTransport.Writes);
    }

    private sealed class FakeTransport(params string[] ports) : ISerialTransport
    {
        private readonly string[] _ports = ports;

        public List<byte[]> Writes { get; } = [];

        public bool IsOpen { get; private set; }

        public string PortName { get; private set; } = string.Empty;

        public string[] GetAvailablePorts()
        {
            return _ports;
        }

        public void Open(string portName)
        {
            PortName = portName;
            IsOpen = true;
        }

        public void Write(byte[] data)
        {
            if (!IsOpen)
            {
                return;
            }

            Writes.Add(data.ToArray());
        }

        public void Close()
        {
            IsOpen = false;
            PortName = string.Empty;
        }

        public void Dispose()
        {
            Close();
        }
    }
}
