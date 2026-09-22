using System.Buffers.Binary;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
namespace Stardust.Core.Proof;
public static class ProofScenario
{
    public static IReadOnlyList<string> Run()
    {
        List<string> events = [];
        SessionModel session = new(); events.Add($"Core created: {session.Phase}");
        session.Start(); events.Add(session.LastEvent);
        byte[] payload = "STARDUST"u8.ToArray();
        byte[] packet = new byte[MessageCodec.HeaderSize + payload.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(packet, SessionModel.ServerHelloOpcode);
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(sizeof(uint)), checked((ushort)payload.Length));
        payload.CopyTo(packet.AsSpan(MessageCodec.HeaderSize));
        session.Apply(MessageCodec.Decode(packet)); events.Add($"{session.LastEvent}: {session.Phase}");
        session.Stop(); events.Add(session.LastEvent); return events;
    }
}
