using System.Buffers.Binary;
namespace Stardust.Core.Protocol;

public static class SceneReadyProtocolCodec
{
    public const uint Opcode = 0x43FD1C22;
    public static byte[] Encode()
    {
        byte[] result = new byte[6];
        BinaryPrimitives.WriteUInt16LittleEndian(result, 1);
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(2), Opcode);
        return result;
    }
    public static void Decode(ReadOnlySpan<byte> message)
    {
        if (message.Length != 6 || BinaryPrimitives.ReadUInt16LittleEndian(message) != 1 ||
            BinaryPrimitives.ReadUInt32LittleEndian(message[2..]) != Opcode)
            throw new InvalidDataException("Expected exact header-only CmdSceneReady.");
    }
}
