using System.Buffers.Binary;

namespace Stardust.Core.Protocol;

public sealed record DecodedLoginApplicationMessage(uint Opcode, object Value);

public static class LoginApplicationDecoder
{
    public static DecodedLoginApplicationMessage Decode(ReadOnlySpan<byte> message)
    {
        if (message.Length < 6) throw new InvalidDataException("Login application message is shorter than its header.");
        uint opcode = BinaryPrimitives.ReadUInt32LittleEndian(message[2..]);
        object value = opcode switch
        {
            LoginProtocolCodec.LoginClientTokenOpcode => LoginProtocolCodec.DecodeLoginClientToken(message),
            LoginProtocolCodec.ErrorMessageOpcode => LoginProtocolCodec.DecodeErrorMessage(message),
            LoginServerListCodec.LoginEnumClusterOpcode => LoginServerListCodec.DecodeClusterList(message),
            LoginServerListCodec.LoginClusterStatusOpcode => LoginServerListCodec.DecodeClusterStatus(message),
            LoginServerListCodec.EnumerateCharacterIdOpcode => LoginServerListCodec.DecodeCharacters(message),
            _ => throw new InvalidDataException($"Unsupported login application opcode 0x{opcode:X8}.")
        };
        return new(opcode, value);
    }
}
