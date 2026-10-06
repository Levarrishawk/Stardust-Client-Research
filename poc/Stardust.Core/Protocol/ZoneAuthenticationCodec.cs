using System.Buffers.Binary;
using System.Text;

namespace Stardust.Core.Protocol;

public sealed record ZonePermissions(bool CanLogin, bool CanCreateRegular, bool CanCreateJedi, bool? CanSkipTutorial);

public static class ZoneAuthenticationCodec
{
    public const uint ClientIdOpcode = 0xD5899226;
    public const uint PermissionsOpcode = 0xE00730E5;

    public static byte[] EncodeClientId(ReadOnlySpan<byte> credential, string clientVersion)
    {
        if (credential.Length is < 4 or > 65536)
            throw new ArgumentOutOfRangeException(nameof(credential), "Credential length outside supported bounds.");
        ArgumentNullException.ThrowIfNull(clientVersion);
        if (clientVersion.Length is < 1 or > 256)
            throw new ArgumentOutOfRangeException(nameof(clientVersion), "Client version length outside supported bounds.");
        foreach (char character in clientVersion)
            if (character is < ' ' or > '~')
                throw new ArgumentException("Client version must contain printable ASCII characters.", nameof(clientVersion));

        byte[] message = new byte[checked(16 + credential.Length + clientVersion.Length)];
        BinaryPrimitives.WriteUInt16LittleEndian(message, 3);
        BinaryPrimitives.WriteUInt32LittleEndian(message.AsSpan(2), ClientIdOpcode);
        BinaryPrimitives.WriteUInt32LittleEndian(message.AsSpan(6), 0xFE);
        BinaryPrimitives.WriteUInt32LittleEndian(message.AsSpan(10), (uint)credential.Length);
        credential.CopyTo(message.AsSpan(14));
        int cursor = 14 + credential.Length;
        BinaryPrimitives.WriteUInt16LittleEndian(message.AsSpan(cursor), (ushort)clientVersion.Length);
        Encoding.ASCII.GetBytes(clientVersion, message.AsSpan(cursor + 2));
        return message;
    }

    public static ZonePermissions DecodePermissions(ReadOnlySpan<byte> message)
    {
        if (message.Length is not (9 or 10))
            throw new InvalidDataException("Permissions message must contain exactly three or four flags.");
        if (BinaryPrimitives.ReadUInt16LittleEndian(message) != 4 ||
            BinaryPrimitives.ReadUInt32LittleEndian(message[2..]) != PermissionsOpcode)
            throw new InvalidDataException("Unexpected permissions message header.");
        foreach (byte flag in message[6..])
            if (flag > 1)
                throw new InvalidDataException("Permissions flags must be Boolean bytes.");
        return new(message[6] != 0, message[7] != 0, message[8] != 0,
                   message.Length == 10 ? message[9] != 0 : null);
    }
}
