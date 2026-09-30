using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Channels;
using Stardust.Core.Configuration;
using Stardust.Core.Hosting;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.Proof;

/// <summary>Injected async I/O only. These fixed login bodies contain synthetic credentials/identities.</summary>
public static class OfflineHostedLoginScenario
{
    private static readonly DatagramPeer Peer = new(0xC0000201, 1);

    public static async Task<IReadOnlyList<string>> RunAsync(CancellationToken cancellationToken = default)
    {
        using MemoryStream journalBytes = new();
        using BoundedLoginJournal journal = new(journalBytes, leaveOpen: true);
        FixturePort port = new();
        await using LoginDatagramHost host = new(new LoginClientOptions("peer.invalid", 1, "20260817-SD:01"),
            Peer, port, diagnostics: journal.Write);
        using LoginConnectionResult result = await host.RunAsync(0x12345678, "u", "p", cancellationToken).ConfigureAwait(false);
        if (host.Outcome != LoginHostOutcome.Completed || host.State != LoginClientState.Closed ||
            result.Clusters.Count != 1 || result.ClusterStatuses.Count != 1 || result.Characters.Count != 1 ||
            result.Characters[0].ObjectId != 0x1122334455667788 || result.CredentialLength != 5 ||
            result.Counters.ForeignDatagramsDropped != 1 || result.Counters.PeerDatagramsReceived != 7 ||
            result.Counters.SendAttempts != 6 || journal.IoFailures != 0 || journal.WrittenEvents == 0 ||
            port.Borrowed.Any(bytes => bytes.Any(value => value != 0)))
            throw new InvalidDataException("Offline hosted login proof did not meet its fixture contract.");
        result.Dispose();
        if (result.CredentialLength != 0) throw new InvalidDataException("Offline result credential cleanup failed.");
        return new[]
        {
            "Async login host completed through injected datagram I/O",
            "Foreign sender filtered; malformed handshake retained its deadline",
            "CRC-damaged token recovered with reordered enumeration replies",
            "One owned result transferred; session and wire buffers cleared",
            $"I/O accounting: sends={result.Counters.SendAttempts} peer_receives={result.Counters.PeerDatagramsReceived} foreign_drops={result.Counters.ForeignDatagramsDropped}",
            $"Allowlisted journal: events={journal.WrittenEvents} failures={journal.IoFailures}"
        };
    }

    private sealed class FixturePort : IDatagramPort
    {
        private readonly Channel<InboundDatagram> incoming = Channel.CreateUnbounded<InboundDatagram>();
        private int sends;
        public List<byte[]> Borrowed { get; } = new();

        public ValueTask SendAsync(DatagramPeer destination, ReadOnlyMemory<byte> bytes, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (destination != Peer) throw new InvalidDataException("Offline host changed its pinned peer.");
            if (MemoryMarshal.TryGetArray(bytes, out ArraySegment<byte> segment)) Borrowed.Add(segment.Array!);
            sends++;
            if (sends == 1)
            {
                Enqueue(Convert.FromHexString("000212345678A1B2C3D4020104000001F0"), Peer with { Address = Peer.Address + 1 });
                Enqueue(new byte[] { 0, 2 });
                Enqueue(Convert.FromHexString("000212345678A1B2C3D4020104000001F0"));
            }
            if (sends == 2)
            {
                byte[][] applications =
                {
                    Convert.FromHexString("0400C696B2AA05000000544433221188776655010075"),
                    Convert.FromHexString("0200B9631CC1010000000700000008005374617264757374808FFFFF08000000"),
                    Convert.FromHexString("0300B6AE3634010000000700000009003132372E302E302E31A5ADAEAD64000000B20C000008000000808FFFFF0200000000"),
                    Convert.FromHexString("02007445EA65010000000100000041004433221188776655443322110700000001000000")
                };
                byte[] damaged = Frame(0, applications[0]); damaged[^1] ^= 1;
                Enqueue(damaged);
                foreach (ushort sequence in new ushort[] { 2, 3, 0, 1 }) Enqueue(Frame(sequence, applications[sequence]));
                foreach (byte[] application in applications) CryptographicOperations.ZeroMemory(application);
            }
            return ValueTask.CompletedTask;
        }

        public async ValueTask<InboundDatagram> ReceiveAsync(CancellationToken token)
        {
            await Task.Yield();
            return await incoming.Reader.ReadAsync(token).ConfigureAwait(false);
        }

        private void Enqueue(byte[] bytes, DatagramPeer? source = null) => incoming.Writer.TryWrite(new(source ?? Peer, bytes));

        private static byte[] Frame(ushort sequence, byte[] body)
        {
            byte[] plain = new byte[4 + body.Length];
            plain[1] = 9;
            BinaryPrimitives.WriteUInt16BigEndian(plain.AsSpan(2), sequence);
            body.CopyTo(plain, 4);
            try { return SoeDatagramCodec.EncodeUncompressed(plain, 0xA1B2C3D4, 2, 496); }
            finally { CryptographicOperations.ZeroMemory(plain); }
        }

        public ValueTask DisposeAsync()
        {
            incoming.Writer.TryComplete();
            while (incoming.Reader.TryRead(out InboundDatagram? pending)) CryptographicOperations.ZeroMemory(pending.Bytes);
            return ValueTask.CompletedTask;
        }
    }
}
