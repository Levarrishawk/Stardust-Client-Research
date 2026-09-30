using System.Buffers.Binary;
using Stardust.Core.Configuration;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

/// <summary>In-memory peer: bitwise CRC and hand-packed frames, no production wire codecs.</summary>
internal static class IndependentPeerScenarios
{
    private const uint Seed = 0xA1B2C3D4;

    public static void Run(byte[] response, byte[] token, byte[] list, byte[] status, byte[] characters, byte[] error)
    {
        SoeClientTransport bounds = new();
        try { bounds.BeginSession(1, 0); throw new Exception("Invalid session size accepted."); }
        catch (ArgumentOutOfRangeException) { }
        Check(bounds.State == SoeClientTransportState.Offline, "invalid start is transactional");
        bounds.BeginSession(0x12345678, 496);
        byte[] excessive = (byte[])response.Clone();
        BinaryPrimitives.WriteUInt32BigEndian(excessive.AsSpan(13), 600);
        ExpectInvalid(() => bounds.AcceptSessionResponse(excessive));
        Check(bounds.State == SoeClientTransportState.SessionRequested, "rejected negotiation preserves pending session");
        bounds.AcceptSessionResponse(response);
        bounds.Close();
        using (LoginClientCoordinator client = NewClient(response))
        {
            byte[] login = client.Authenticate("u", "p", 0).Bytes;
            Check(Open(login).AsSpan(4).SequenceEqual(Convert.FromHexString(
                "0400961F13410100750100700E0032303236303831372D53443A3031")), "independent login decode");
            // Drop request/ACK, exercise exact retransmission, then ACK it independently.
            Check(client.Poll(199).Count == 0, "no early retransmission");
            Check(client.Poll(200).Single().Bytes.AsSpan().SequenceEqual(login), "lost-request retransmission");
            client.AcceptIncoming(Frame(0x15, 0, Array.Empty<byte>()));
            Check(client.Poll(400).Count == 0, "ACK released login request");

            byte[] damaged = Frame(0x09, 0, token);
            damaged[^1] ^= 1;
            Check(client.AcceptIncoming(damaged).Count == 0, "corruption produced no ACK");
            Check(client.Session.State == LoginClientState.AwaitingResponses && client.Session.CredentialLength == 0,
                "corruption preserved session and receive sequence");
            byte[][] replies = { token, list, status, characters };
            foreach (int index in new[] { 2, 3, 2, 0, 1 })
            {
                byte[] control = Open(client.AcceptIncoming(Frame(0x09, (ushort)index, replies[index])).Single().Bytes);
                Check(control[1] == (index >= 2 ? 0x11 : 0x15), "future/normal ACK opcode");
            }
            Check(client.Session.State == LoginClientState.Complete && client.Session.CredentialLength == 5,
                "reordered duplicate login transcript completed");
            byte[] duplicateAck = Open(client.AcceptIncoming(Frame(0x09, 1, list)).Single().Bytes);
            Check(duplicateAck[1] == 0x15, "duplicate acknowledged without redelivery");
            client.Poll(30_000); // Completion disables the response deadline.
        }
        using (LoginClientCoordinator client = NewClient(response))
        {
            client.Authenticate("u", "p", 0);
            client.AcceptIncoming(Frame(0x09, 0, token));
            client.AcceptIncoming(Frame(0x09, 1, error));
            Check(client.Session.State == LoginClientState.Failed && client.Session.CredentialLength == 0 &&
                client.Session.LastError?.Text == "bad", "fatal cleanup preserves reason");
            Check(client.Poll(100_000).Count == 0, "fatal failure stops retransmissions");
        }
        using (LoginClientCoordinator client = NewClient(response))
        {
            client.Authenticate("u", "p", 10);
            client.AcceptIncoming(Frame(0x09, 0, token));
            client.Poll(30_009);
            try { client.Poll(30_010); throw new Exception("Response deadline did not fire."); }
            catch (TimeoutException) { }
            Check(client.Session.State == LoginClientState.Failed && client.Session.CredentialLength == 0,
                "partial-login deadline clears credential");
        }
        using (LoginClientCoordinator client = NewClient(response))
        {
            client.Authenticate("u", "p", 0);
            client.AcceptIncoming(Frame(0x09, 0, token));
            client.Dispose();
            Check(client.Session.State == LoginClientState.Closed && client.Session.CredentialLength == 0 &&
                client.Poll(100_000).Count == 0, "cancellation cleanup");
        }
        TestLargeQueuedMessage(response);
        TestOuterPackets(response, token, list, status, characters);
        TestFragmentedMultipayload(response, error, token);
        TestLossyFragmentTransfers(response);
        Console.WriteLine("PASS: independent peer corruption/loss/reorder/duplicate/deadline/cancellation and 1.5 MB queued fragmentation");
    }

    private static void TestFragmentedMultipayload(byte[] response, byte[] error, byte[] token)
    {
        byte[] one = Enumerable.Repeat((byte)0xA5, 300).ToArray();
        byte[] two = Enumerable.Repeat((byte)0x5A, 300).ToArray();
        byte[] multi = Multipayload(one, two);
        SoeClientTransport receiver = new();
        receiver.BeginSession(0x12345678, 496);
        receiver.AcceptSessionResponse(response);
        byte[] first = new byte[4 + 485];
        BinaryPrimitives.WriteUInt32BigEndian(first, (uint)multi.Length);
        multi.AsSpan(0, 485).CopyTo(first.AsSpan(4));
        receiver.AcceptDatagram(Frame(0x0D, 0, first));
        IReadOnlyList<byte[]> applications = receiver.AcceptDatagram(Frame(0x0D, 1, multi[485..])).ApplicationPayloads;
        Check(applications.Count == 2 && applications[0].AsSpan().SequenceEqual(one) && applications[1].AsSpan().SequenceEqual(two),
            "reassembled multipayload may exceed wire datagram size");
        receiver.Close();
        using LoginClientCoordinator client = NewClient(response);
        client.Authenticate("u", "p", 0);
        client.AcceptIncoming(Frame(0x09, 0, Multipayload(error, token)));
        Check(client.Session.State == LoginClientState.Failed && client.Session.CredentialLength == 0 && client.Session.AccountId == 0,
            "fatal multipayload stops later token application");
        Console.WriteLine("PASS: independent fragmented multipayload and fatal-response batch ordering");
    }

    private static byte[] Multipayload(params byte[][] bodies)
    {
        using MemoryStream output = new();
        output.Write(new byte[] { 0, 0x19 });
        foreach (byte[] body in bodies)
        {
            if (body.Length < 0xFE) output.WriteByte((byte)body.Length);
            else
            {
                output.WriteByte(0xFF);
                output.WriteByte((byte)(body.Length >> 8));
                output.WriteByte((byte)body.Length);
            }
            output.Write(body);
        }
        return output.ToArray();
    }

    private static void TestLossyFragmentTransfers(byte[] response)
    {
        // Repeatable impairment traces, independent receiver and ACK encoder.
        for (int scenario = 0; scenario < 32; scenario++)
        {
            Random random = new(0x534F45 + scenario);
            byte[] input = new byte[25_000 + scenario * 4093];
            random.NextBytes(input);
            SoeClientTransport sender = new();
            sender.BeginSession(0x12345678, 496);
            sender.AcceptSessionResponse(response);
            sender.QueueReliableMessage(input);
            ReferenceFragmentPeer peer = new();
            int ticks = 0;
            while (sender.PendingMessageCount != 0 || sender.RetainedOutboundPackets != 0)
            {
                Check(ticks < 10_000, "loss trace made bounded progress");
                long now = ticks++ * 50L;
                List<byte[]> outbound = sender.PumpQueuedDatagrams(now)
                    .Concat(sender.GetRetransmissionDatagrams(now, 200)).ToList();
                // Shuffle arrivals to create both missing heads and buffered successors.
                for (int i = outbound.Count - 1; i > 0; i--)
                {
                    int other = random.Next(i + 1);
                    (outbound[i], outbound[other]) = (outbound[other], outbound[i]);
                }
                foreach (byte[] packet in outbound)
                {
                    if (random.Next(100) < 20) continue;
                    byte[] arriving = (byte[])packet.Clone();
                    if (random.Next(100) < 8) arriving[^1] ^= 1;
                    byte[]? ack = peer.Accept(arriving);
                    if (ack is not null && random.Next(100) >= 20)
                    {
                        if (random.Next(100) < 8) ack[^1] ^= 1;
                        sender.AcceptDatagram(ack);
                    }
                    if (random.Next(100) < 15)
                    {
                        byte[]? duplicateAck = peer.Accept(packet);
                        if (duplicateAck is not null) sender.AcceptDatagram(duplicateAck);
                    }
                }
            }
            Check(peer.Content.AsSpan().SequenceEqual(input), "loss trace independent byte reconstruction");
            sender.Close();
        }
        Console.WriteLine("PASS: 32 seeded independent fragment transfers with packet/ACK loss, corruption, duplication, and reordering");
    }

    private sealed class ReferenceFragmentPeer
    {
        private readonly SortedDictionary<int, byte[]> future = new();
        private readonly MemoryStream content = new();
        private int expected;
        private uint declared;
        public byte[] Content => content.ToArray();

        public byte[]? Accept(byte[] wire)
        {
            uint crc = Crc(wire.AsSpan(0, wire.Length - 2));
            if (wire[^2] != (byte)(crc >> 8) || wire[^1] != (byte)crc) return null;
            byte[] packet = Open(wire);
            Check(packet[1] == 0x0D, "reference peer fragment opcode");
            int sequence = BinaryPrimitives.ReadUInt16BigEndian(packet.AsSpan(2));
            if (sequence < expected) return Frame(0x15, (ushort)sequence, Array.Empty<byte>());
            if (sequence > expected)
            {
                Check(sequence - expected <= 50, "sender respected reference reorder distance");
                future.TryAdd(sequence, packet);
                return Frame(0x11, (ushort)sequence, Array.Empty<byte>());
            }
            Append(packet);
            while (future.Remove(expected, out byte[]? next)) Append(next);
            return Frame(0x15, (ushort)(expected - 1), Array.Empty<byte>());
        }

        private void Append(byte[] packet)
        {
            int cursor = 4;
            if (expected == 0)
            {
                declared = BinaryPrimitives.ReadUInt32BigEndian(packet.AsSpan(cursor));
                cursor += 4;
            }
            content.Write(packet.AsSpan(cursor));
            Check(content.Length <= declared, "reference peer total length bound");
            expected++;
        }
    }

    private static void TestOuterPackets(byte[] response, params byte[][] replies)
    {
        using (LoginClientCoordinator client = NewClient(response))
        {
            client.Authenticate("u", "p", 0);
            List<byte[]> blocks = new() { new byte[] { 0, 6 }, new byte[] { 0, 0x15, 0, 0 } };
            for (int i = 0; i < replies.Length; i++) blocks.Add(Open(Frame(0x09, (ushort)i, replies[i])));
            byte[] outer = Outer(blocks.ToArray());
            IReadOnlyList<LoginOutboundDatagram> controls = client.AcceptIncoming(Protect(outer));
            Check(controls.Count == 4 && client.Session.State == LoginClientState.Complete,
                "outer multipacket preserves all ACKs and applications");
            for (int i = 0; i < controls.Count; i++)
            {
                byte[] ack = Open(controls[i].Bytes);
                Check(ack[1] == 0x15 && BinaryPrimitives.ReadUInt16BigEndian(ack.AsSpan(2)) == i,
                    "outer multipacket ACK sequence");
            }
            Check(client.Poll(1000).Count == 0, "outer ACK releases outbound request");
            client.AcceptIncoming(Protect(Outer(Outer(new byte[] { 0, 6 }))));
            byte[] disconnect = Convert.FromHexString("0005123456780006");
            client.AcceptIncoming(Protect(disconnect));
            Check(client.Session.State == LoginClientState.Failed && client.Session.CredentialLength == 0,
                "peer disconnect closes and clears credential");
        }
        SoeClientTransport transport = new();
        transport.BeginSession(0x12345678, 496);
        transport.AcceptSessionResponse(response);
        byte[] first = Open(Frame(0x09, 0, new byte[] { 0xAA }));
        // A malformed later block must not consume the first block's sequence.
        byte[] malformed = Outer(first).Concat(new byte[] { 4, 0, 0x15 }).ToArray();
        ExpectInvalid(() => transport.AcceptDatagram(Protect(malformed)));
        Check(transport.AcceptDatagram(Frame(0x09, 0, new byte[] { 0xAA })).ApplicationPayloads.Count == 1,
            "outer length validation precedes mutation");
        foreach (byte[] invalid in new[] { new byte[] { 0, 3 }, new byte[] { 0, 3, 0 },
            new byte[] { 0, 3, 1, 0 }, new byte[] { 0, 3, 255, 0, 6 } })
            ExpectInvalid(() => SoeOuterMultiCodec.Decode(invalid));
        ExpectInvalid(() => SoeOuterMultiCodec.Decode(Outer(new byte[] { 0, 6 }, new byte[] { 0, 6 }), maximumPackets: 1));
        ExpectInvalid(() => SoeOuterMultiCodec.Decode(Outer(Outer(new byte[] { 0, 6 })), maximumDepth: 1));
        byte[] maximumBlock = new byte[255];
        maximumBlock[1] = 9;
        Check(SoeOuterMultiCodec.Decode(Outer(maximumBlock)).Single().Length == 255,
            "FF is literal 255 in outer multipacket");
        Console.WriteLine("PASS: independent outer SOE nesting, lengths, mixed dispatch, all ACKs, ping, and disconnect");
    }

    private static byte[] Outer(params byte[][] blocks)
    {
        using MemoryStream output = new();
        output.Write(new byte[] { 0, 3 });
        foreach (byte[] block in blocks)
        {
            if (block.Length > 255) throw new Exception("Test outer block exceeds one-byte length.");
            output.WriteByte((byte)block.Length);
            output.Write(block);
        }
        return output.ToArray();
    }

    private static void ExpectInvalid(Action action)
    {
        try { action(); throw new Exception("Malformed outer SOE input accepted."); }
        catch (InvalidDataException) { }
    }

    private static void TestLargeQueuedMessage(byte[] response)
    {
        SoeClientTransport sender = new();
        sender.BeginSession(0x12345678, 496);
        sender.AcceptSessionResponse(response);
        byte[] input = Enumerable.Range(0, SoeClientTransport.MaximumMessageSize).Select(i => (byte)(i * 31)).ToArray();
        sender.QueueReliableMessage(input);
        byte[] first = (byte[])input.Clone(); // Caller mutation cannot change owned queued data.
        Array.Clear(input);
        using MemoryStream assembled = new();
        int sequence = 0;
        while (sender.PendingMessageCount > 0)
        {
            IReadOnlyList<byte[]> batch = sender.PumpQueuedDatagrams(sequence);
            Check(batch.Count is > 0 and <= 50, "bounded fragment window");
            foreach (byte[] wire in batch)
            {
                byte[] packet = Open(wire);
                Check(packet[1] == 0x0D && BinaryPrimitives.ReadUInt16BigEndian(packet.AsSpan(2)) == sequence,
                    "continuous fragment sequence");
                if (sequence == 0)
                    Check(BinaryPrimitives.ReadUInt32BigEndian(packet.AsSpan(4)) == first.Length, "first fragment total");
                assembled.Write(packet.AsSpan(sequence == 0 ? 8 : 4));
                sequence++;
            }
            if (sender.PendingMessageCount > 0)
                Check(sender.PumpQueuedDatagrams(sequence).Count == 0, "full window stays blocked");
            sender.AcceptDatagram(Frame(0x15, (ushort)(sequence - 1), Array.Empty<byte>()));
        }
        Check(assembled.ToArray().AsSpan().SequenceEqual(first), "independent large-message reconstruction");
        Check(sender.RetainedOutboundPackets == 0 && sequence > 50, "all fragments acknowledged");
        sender.QueueReliableMessage(new byte[100_000]);
        sender.PumpQueuedDatagrams(0);
        sender.Close();
        Check(sender.PendingMessageCount == 0 && sender.RetainedOutboundPackets == 0, "queued shutdown cleanup");

        SoeReliableChannel hole = new(maximumOutstanding: 3);
        for (int i = 0; i < 3; i++) hole.CreateDataPacket(new byte[] { 1 }, 0);
        hole.ApplySelectiveOutOfOrderAcknowledgement(1);
        hole.ApplySelectiveOutOfOrderAcknowledgement(2);
        Check(hole.AvailableOutboundSlots == 0, "selective ACK holes preserve span bound");
        hole.ApplyCumulativeAcknowledgement(0);
        Check(hole.AvailableOutboundSlots == 3, "oldest ACK opens window");
    }

    private static LoginClientCoordinator NewClient(byte[] response)
    {
        LoginClientCoordinator client = new(new LoginClientOptions("peer.invalid", 1, "20260817-SD:01",
            reliableRetransmitInterval: TimeSpan.FromMilliseconds(200)));
        client.Start(0x12345678, 0);
        client.AcceptIncoming(response);
        return client;
    }

    private static byte[] Frame(byte opcode, ushort sequence, byte[] body)
    {
        byte[] packet = new byte[4 + body.Length];
        packet[1] = opcode;
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(2), sequence);
        body.CopyTo(packet, 4);
        return Protect(packet);
    }

    internal static byte[] Protect(byte[] plain)
    {
        byte[] packet = new byte[plain.Length + 3];
        plain.CopyTo(packet, 0);
        Transform(packet.AsSpan(0, packet.Length - 2), decrypt: false);
        uint crc = Crc(packet.AsSpan(0, packet.Length - 2));
        packet[^2] = (byte)(crc >> 8);
        packet[^1] = (byte)crc;
        return packet;
    }

    private static byte[] Open(byte[] wire)
    {
        uint crc = Crc(wire.AsSpan(0, wire.Length - 2));
        Check(wire[^2] == (byte)(crc >> 8) && wire[^1] == (byte)crc, "independent CRC check");
        byte[] plain = wire[..^2];
        Transform(plain, decrypt: true);
        Check(plain[^1] == 0, "independent uncompressed marker");
        return plain[..^1];
    }

    private static uint Step(uint value)
    {
        for (int bit = 0; bit < 8; bit++)
            value = (value >> 1) ^ ((value & 1) != 0 ? 0xEDB88320u : 0u);
        return value;
    }

    private static uint Crc(ReadOnlySpan<byte> data)
    {
        uint value = Step(unchecked((byte)~Seed)) ^ 0x00FFFFFFu;
        for (int shift = 8; shift < 32; shift += 8)
            value = (value >> 8) ^ Step((byte)((Seed >> shift) ^ value));
        foreach (byte octet in data) value = (value >> 8) ^ Step((byte)(octet ^ value));
        return ~value;
    }

    private static void Transform(Span<byte> data, bool decrypt)
    {
        uint chain = Seed;
        int offset = 2;
        while (offset + 4 <= data.Length)
        {
            uint input = (uint)(data[offset] | data[offset + 1] << 8 | data[offset + 2] << 16 | data[offset + 3] << 24);
            uint output = input ^ chain;
            for (int n = 0; n < 4; n++) data[offset + n] = (byte)(output >> (8 * n));
            chain = decrypt ? input : output;
            offset += 4;
        }
        while (offset < data.Length) data[offset++] ^= (byte)chain;
    }

    private static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception($"Independent peer failed: {label}.");
    }
}
