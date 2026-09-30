using Stardust.Core.Protocol;

internal static class StatusScenarios
{
    internal static void Run(byte[] responseVector)
    {
        byte[] extended = Convert.FromHexString("0007FFFE000000010000000200000003000000040000000501020304818283841112131491929394");
        byte[] expectedReply = Convert.FromHexString("0008FFFEAABBCCDD0102030481828384111213149192939421222324A1A2A3A431323334B1B2B3B4");
        SoeStatusSnapshot snapshot = new(0xAABBCCDD, 0x21222324A1A2A3A4, 0x31323334B1B2B3B4);
        if (!SoeStatusCodec.CreateResponse(extended, snapshot).AsSpan().SequenceEqual(expectedReply))
            throw new Exception("Extended status reply layout mismatch.");
        SoeStatusResponse typed = SoeStatusCodec.DecodeResponse(expectedReply);
        if (typed != new SoeStatusResponse(65534, snapshot.Timestamp, 0x0102030481828384,
            0x1112131491929394, snapshot.PacketsSent, snapshot.PacketsReceived))
            throw new Exception("Typed response counters mismatch.");
        for (int length = 0; length < 40; length++)
        {
            Reject(() => SoeStatusCodec.CreateResponse(extended.AsSpan(0, length), snapshot));
            Reject(() => SoeStatusCodec.DecodeResponse(expectedReply.AsSpan(0, length)));
        }
        Reject(() => SoeStatusCodec.CreateResponse(extended.Concat(new byte[1]).ToArray(), snapshot));
        Reject(() => SoeStatusCodec.CreateResponse(expectedReply, snapshot));
        Reject(() => SoeStatusCodec.DecodeResponse(extended));
        Reject(() => SoeStatusCodec.DecodeResponse(expectedReply.Concat(new byte[1]).ToArray()));
        byte[] maxReply = SoeStatusCodec.CreateResponse(extended, new(uint.MaxValue, ulong.MaxValue, 0));
        if (SoeStatusCodec.DecodeResponse(maxReply).LocalPacketsSent != ulong.MaxValue ||
            SoeStatusCodec.DecodeResponse(maxReply).LocalPacketsReceived != 0)
            throw new Exception("Status counter range narrowed.");
        byte[] minimal = Convert.FromHexString("0007FFFE");
        SoeStatusPacket request = SoeStatusCodec.Decode(minimal);
        if (request.Opcode != 7 || request.Tick != 65534 || request.RawWords.Count != 0)
            throw new Exception("Minimal server status request mismatch.");
        byte[] full = Convert.FromHexString("00080102010203041112131421222324313233344142434451525354616263647172737481828384");
        SoeStatusPacket status = SoeStatusCodec.Decode(full);
        if (status.Tick != 258 || status.RawWords.Count != 9 || status.RawWords[0] != 0x01020304 || status.RawWords[8] != 0x81828384)
            throw new Exception("Raw status positions mismatch.");
        full[4] = 0;
        if (status.RawWords[0] != 0x01020304) throw new Exception("Status retained caller buffer.");
        for (int opcode = 7; opcode <= 8; opcode++)
        {
            full[1] = (byte)opcode;
            for (int length = 0; length < 40; length++)
            {
                if (opcode == 7 && length == 4) continue;
                Reject(() => SoeStatusCodec.Decode(full.AsSpan(0, length)));
            }
            Reject(() => SoeStatusCodec.Decode(full.Concat(new byte[1]).ToArray()));
        }
        SoeClientTransport transport = new();
        transport.BeginSession(0x12345678, 496);
        transport.AcceptSessionResponse(responseVector);
        byte[] expectedProtected = Convert.FromHexString("00082B3D181AE7E01918E4E4989A676089887474181AE7E03938C4C4989A6760A9A85454181AE7E054A8DD");
        if (!transport.CreateStatusResponseDatagram(extended, snapshot).AsSpan().SequenceEqual(expectedProtected))
            throw new Exception("Independent protected client reply mismatch.");
        if (transport.GetRetransmissionDatagrams(100, 1).Count != 0)
            throw new Exception("Unsequenced status reply was retained for retransmission.");
        // Fixed bytes from the independent Python source-correlation model.
        byte[] wire = Convert.FromHexString("00072B2AD493D0");
        if (!wire.AsSpan().SequenceEqual(SoeDatagramCodec.EncodeUncompressed(minimal, 0xA1B2C3D4, 2, 496)))
            throw new Exception("Independent status framing mismatch.");
        SoeInboundDatagramResult accepted = transport.AcceptDatagram(wire);
        if (accepted.ControlDatagrams.Count != 0 || accepted.ApplicationPayloads.Count != 0 || transport.LastStatusPacket?.Tick != 65534)
            throw new Exception("Status observation affected application/control dispatch.");
        wire[^1] ^= 1;
        if (!transport.AcceptDatagram(wire).RejectedFraming || transport.LastStatusPacket?.Tick != 65534)
            throw new Exception("Corrupt status changed state.");
        byte[] malformedOuter = Convert.FromHexString("00030400070001050008000000");
        Reject(() => transport.AcceptDatagram(SoeDatagramCodec.EncodeUncompressed(malformedOuter, 0xA1B2C3D4, 2, 496)));
        if (transport.LastStatusPacket?.Tick != 65534)
            throw new Exception("Malformed outer leaf changed earlier status observation.");
        full[1] = 8;
        transport.AcceptDatagram(SoeDatagramCodec.EncodeUncompressed(full, 0xA1B2C3D4, 2, 496));
        if (transport.LastStatusPacket?.Opcode != 8 || transport.LastStatusPacket.RawWords.Count != 9)
            throw new Exception("Protected response not observed.");
        byte[] noCrc = Convert.FromHexString("00082B3DB2A12B3DB2A12B3DB2A12B3DB2A12B3DB2A12B3DB2A12B3DB2A12B3DB2A12B3DB2A12B2B2B0000");
        if (!transport.AcceptDatagram(noCrc).RejectedFraming)
            throw new Exception("CRC-disabled status unexpectedly accepted.");
        if (transport.LastStatusPacket?.Tick != 258)
            throw new Exception("Rejected status response changed observation.");
        // Repair only the trailer of the independent server-model response.
        SoeSeededCrc.WriteTrailer(noCrc.AsSpan(0, noCrc.Length - 2), 0xA1B2C3D4, noCrc.AsSpan(noCrc.Length - 2));
        if (transport.AcceptDatagram(noCrc).RejectedFraming || transport.LastStatusPacket?.Tick != 65534)
            throw new Exception("CRC-valid server-shaped response rejected.");
        for (int index = 2; index < noCrc.Length; index++)
        {
            byte[] corrupt = (byte[])noCrc.Clone();
            corrupt[index] ^= 1;
            if (!transport.AcceptDatagram(corrupt).RejectedFraming || transport.LastStatusPacket?.Tick != 65534)
                throw new Exception("Status corruption bypassed CRC gate.");
        }
        transport.Close();
        if (transport.LastStatusPacket is not null) throw new Exception("Status survived close.");
        Console.WriteLine("PASS: raw status observations, explicit extended replies, independent wire, CRC gate and cleanup");
    }

    private static void Reject(Action action)
    {
        try { action(); }
        catch (InvalidDataException) { return; }
        throw new Exception("Malformed status accepted.");
    }
}
