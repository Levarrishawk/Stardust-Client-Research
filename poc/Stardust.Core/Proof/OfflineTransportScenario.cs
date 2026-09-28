using Stardust.Core.Protocol;
using Stardust.Core.Session;
using System.Buffers.Binary;

namespace Stardust.Core.Proof;

/// <summary>A network-free transcript using recovered SOE and login wire formats.</summary>
public static class OfflineTransportScenario
{
    public static IReadOnlyList<string> Run()
    {
        const uint connectionId = 0x12345678;
        const uint seed = 0xA1B2C3D4;
        const int maximumDatagram = 496;
        const int crcWidth = 2;
        List<string> events = [];

        using LoginClientSession client = new();
        byte[] request = client.BeginTransport(connectionId, maximumDatagram);
        SoeSessionCodec.DecodeRequest(request);
        events.Add("SessionRequest encoded and validated");

        byte[] response = Convert.FromHexString("000212345678A1B2C3D4020104000001F0");
        SoeSessionResponse negotiated = SoeSessionCodec.DecodeResponse(response, connectionId);
        if (negotiated.Seed != seed || negotiated.CrcWidth != crcWidth ||
            negotiated.SecondTransformSelector != 4 || negotiated.MaximumDatagramSize != maximumDatagram)
            throw new InvalidDataException("Offline session negotiation parameters changed.");
        client.AcceptSessionResponse(response);
        events.Add("SessionResponse negotiated CRC and selector-4");

        byte[] loginDatagram = client.CreateLoginDatagram("u", "p", "20260817-SD:01", 0);
        SoeDatagramCodec.Decode(loginDatagram, negotiated.Seed, negotiated.CrcWidth, maximumDatagram);
        events.Add("LoginClientId framed on reliable channel 0");

        byte[] tokenApplication = Convert.FromHexString("0400C696B2AA05000000544433221188776655010075");
        byte[] tokenDatagram = CreateServerDatagram(tokenApplication, 0, seed, crcWidth, maximumDatagram);
        LoginSessionReceiveResult received = client.AcceptDatagram(tokenDatagram);
        if (client.Username != "u") throw new InvalidDataException("Offline token identity mismatch.");
        if (received.ControlDatagram is null) throw new InvalidDataException("Reliable token was not acknowledged.");
        events.Add("LoginClientToken parsed with opaque credential preservation");

        byte[][] remainingResponses =
        {
            Convert.FromHexString("0200B9631CC1010000000700000008005374617264757374808FFFFF08000000"),
            Convert.FromHexString("0300B6AE3634010000000700000009003132372E302E302E31A5ADAEAD64000000B20C000008000000808FFFFF0200000000"),
            Convert.FromHexString("02007445EA65010000000100000041004433221188776655443322110700000001000000")
        };
        for (ushort sequence = 1; sequence <= remainingResponses.Length; sequence++)
        {
            byte[] datagram = CreateServerDatagram(remainingResponses[sequence - 1], sequence, seed, crcWidth, maximumDatagram);
            client.AcceptDatagram(datagram);
        }
        if (client.State != LoginClientState.Complete || client.ClusterList?.Clusters.Count != 1 ||
            client.ClusterStatuses.Count != 1 || client.Characters.Count != 1 ||
            client.Characters[0].ObjectId != 0x1122334455667788)
            throw new InvalidDataException("Offline login enumeration changed.");
        events.Add("Cluster endpoints and 64-bit character IDs parsed");
        events.Add("Offline transport transcript completed cleanly");
        return events;
    }

    private static byte[] CreateServerDatagram(
        ReadOnlySpan<byte> application, ushort sequence, uint seed, int crcWidth, int maximumDatagram)
    {
        byte[] packet = new byte[4 + application.Length];
        packet[1] = 0x09;
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(2), sequence);
        application.CopyTo(packet.AsSpan(4));
        return SoeDatagramCodec.EncodeUncompressed(packet, seed, crcWidth, maximumDatagram);
    }
}
