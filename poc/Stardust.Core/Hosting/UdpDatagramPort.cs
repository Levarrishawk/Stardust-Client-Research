using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using Stardust.Core.Protocol;

namespace Stardust.Core.Hosting;

/// <summary>Explicit IPv4 UDP adapter. Construction binds locally; it never resolves or selects a server.</summary>
public sealed class UdpDatagramPort : IDatagramPort
{
    private readonly Socket socket;
    private int disposed;
    private int receiving;

    public UdpDatagramPort()
    {
        socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        try { socket.Bind(new IPEndPoint(IPAddress.Any, 0)); }
        catch { socket.Dispose(); throw; }
    }

    public async ValueTask SendAsync(DatagramPeer destination, ReadOnlyMemory<byte> bytes, CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
        if (destination.Port == 0 || bytes.Length > SoeClientTransport.MaximumWireDatagramSize)
            throw new ArgumentOutOfRangeException(nameof(destination));
        int sent = await socket.SendToAsync(bytes, SocketFlags.None, destination.ToEndPoint(), token).ConfigureAwait(false);
        if (sent != bytes.Length) throw new IOException("UDP send did not consume the entire datagram.");
    }

    public async ValueTask<InboundDatagram> ReceiveAsync(CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
        if (Interlocked.CompareExchange(ref receiving, 1, 0) != 0)
            throw new InvalidOperationException("Only one UDP receive may be pending.");
        byte[] buffer = new byte[SoeClientTransport.MaximumWireDatagramSize];
        try
        {
            SocketReceiveMessageFromResult received = await socket.ReceiveMessageFromAsync(buffer,
                SocketFlags.None, new IPEndPoint(IPAddress.Any, 0), token).ConfigureAwait(false);
            if ((received.SocketFlags & SocketFlags.Truncated) != 0)
                throw new InvalidDataException("Truncated UDP datagram.");
            DatagramPeer source = DatagramPeer.FromEndPoint((IPEndPoint)received.RemoteEndPoint);
            return new(source, buffer.AsSpan(0, received.ReceivedBytes).ToArray());
        }
        finally
        {
            CryptographicOperations.ZeroMemory(buffer);
            Volatile.Write(ref receiving, 0);
        }
    }

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 0) socket.Dispose();
        return ValueTask.CompletedTask;
    }
}
