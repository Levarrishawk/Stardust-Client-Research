using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace Stardust.Core.Hosting;

/// <summary>Immutable IPv4 identity. Host filtering compares address and port exactly.</summary>
public readonly record struct DatagramPeer(uint Address, ushort Port)
{
    public static DatagramPeer FromEndPoint(IPEndPoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        if (endpoint.AddressFamily != AddressFamily.InterNetwork)
            throw new ArgumentException("Peer must be an IPv4 endpoint.", nameof(endpoint));
        return new(BinaryPrimitives.ReadUInt32BigEndian(endpoint.Address.GetAddressBytes()), (ushort)endpoint.Port);
    }

    public IPEndPoint ToEndPoint()
    {
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, Address);
        return new(new IPAddress(bytes), Port);
    }

    public override string ToString() => "peer_configured=true";
}

public sealed record InboundDatagram(DatagramPeer Source, byte[] Bytes);

/// <summary>Send borrows bytes until completion; receive transfers an owned buffer to the host.</summary>
public interface IDatagramPort : IAsyncDisposable
{
    ValueTask SendAsync(DatagramPeer destination, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken);
    ValueTask<InboundDatagram> ReceiveAsync(CancellationToken cancellationToken);
}

public interface IHostTickSource
{
    ValueTask WaitAsync(CancellationToken cancellationToken);
}

public sealed class PeriodicHostTickSource : IHostTickSource
{
    private readonly TimeSpan interval;
    public PeriodicHostTickSource(TimeSpan? interval = null)
    {
        this.interval = interval ?? TimeSpan.FromMilliseconds(25);
        if (this.interval < TimeSpan.FromMilliseconds(1) || this.interval > TimeSpan.FromSeconds(1))
            throw new ArgumentOutOfRangeException(nameof(interval));
    }
    public ValueTask WaitAsync(CancellationToken cancellationToken) => new(Task.Delay(interval, cancellationToken));
}
