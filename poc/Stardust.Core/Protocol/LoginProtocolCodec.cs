using System.Buffers.Binary;
using System.Text;

namespace Stardust.Core.Protocol;

public sealed record LoginClientToken(byte[] CredentialBlob, uint AccountId, uint StationId, string Username);
public sealed record LoginError(string Type, string Text, bool Fatal);

public static class LoginProtocolCodec
{
    public const uint LoginClientIdOpcode = 0x41131F96;
    public const uint LoginClientTokenOpcode = 0xAAB296C6;
    public const uint ErrorMessageOpcode = 0xB5ABF91A;

    public static byte[] EncodeLoginClientId(string username, string password, string version)
    {
        byte[] user = EncodeAscii(username, nameof(username));
        byte[] secret = EncodeAscii(password, nameof(password));
        byte[] build = EncodeAscii(version, nameof(version));
        int length = checked(6 + 2 + user.Length + 2 + secret.Length + 2 + build.Length);
        byte[] message = new byte[length];
        BinaryPrimitives.WriteUInt16LittleEndian(message, 4);
        BinaryPrimitives.WriteUInt32LittleEndian(message.AsSpan(2), LoginClientIdOpcode);
        int cursor = 6;
        cursor = WriteAscii16(message, cursor, user);
        cursor = WriteAscii16(message, cursor, secret);
        WriteAscii16(message, cursor, build);
        return message;
    }

    public static LoginClientToken DecodeLoginClientToken(ReadOnlySpan<byte> message)
    {
        Reader reader = new(message);
        reader.ExpectHeader(4, LoginClientTokenOpcode);
        uint credentialLength = reader.ReadUInt32();
        if (credentialLength < sizeof(uint))
            throw new InvalidDataException("Login credential blob is shorter than its embedded account ID.");
        byte[] credential = reader.ReadBytes(credentialLength);
        uint accountId = BinaryPrimitives.ReadUInt32LittleEndian(credential.AsSpan(credential.Length - 4));
        uint stationId = reader.ReadUInt32();
        string username = reader.ReadAscii16();
        reader.RequireEnd();
        return new LoginClientToken(credential, accountId, stationId, username);
    }

    public static LoginError DecodeErrorMessage(ReadOnlySpan<byte> message)
    {
        Reader reader = new(message);
        reader.ExpectHeader(3, ErrorMessageOpcode);
        string type = reader.ReadAscii16();
        string text = reader.ReadAscii16();
        bool fatal = reader.ReadByte() != 0;
        reader.RequireEnd();
        return new LoginError(type, text, fatal);
    }

    private static byte[] EncodeAscii(string value, string parameter)
    {
        if (value.Any(character => character > 0x7F))
            throw new ArgumentException("SOE ASCII16 fields must contain ASCII characters.", parameter);
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        if (bytes.Length > ushort.MaxValue) throw new ArgumentOutOfRangeException(parameter);
        return bytes;
    }

    private static int WriteAscii16(Span<byte> target, int cursor, ReadOnlySpan<byte> value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(target[cursor..], checked((ushort)value.Length));
        value.CopyTo(target[(cursor + 2)..]);
        return cursor + 2 + value.Length;
    }

    private ref struct Reader
    {
        private readonly ReadOnlySpan<byte> source;
        private int cursor;

        public Reader(ReadOnlySpan<byte> source) => this.source = source;

        public void ExpectHeader(ushort operand, uint opcode)
        {
            if (ReadUInt16() != operand || ReadUInt32() != opcode)
                throw new InvalidDataException("Unexpected SWG application message header.");
        }

        public byte ReadByte()
        {
            Require(1);
            return source[cursor++];
        }

        public ushort ReadUInt16()
        {
            Require(2);
            ushort value = BinaryPrimitives.ReadUInt16LittleEndian(source[cursor..]);
            cursor += 2;
            return value;
        }

        public uint ReadUInt32()
        {
            Require(4);
            uint value = BinaryPrimitives.ReadUInt32LittleEndian(source[cursor..]);
            cursor += 4;
            return value;
        }

        public byte[] ReadBytes(uint length)
        {
            if (length > int.MaxValue) throw new InvalidDataException("Length exceeds supported bounds.");
            Require((int)length);
            byte[] value = source.Slice(cursor, (int)length).ToArray();
            cursor += (int)length;
            return value;
        }

        public string ReadAscii16()
        {
            ushort length = ReadUInt16();
            Require(length);
            ReadOnlySpan<byte> value = source.Slice(cursor, length);
            if (value.ContainsAnyInRange((byte)0x80, byte.MaxValue))
                throw new InvalidDataException("ASCII16 field contains non-ASCII bytes.");
            cursor += length;
            return Encoding.ASCII.GetString(value);
        }

        public void RequireEnd()
        {
            if (cursor != source.Length) throw new InvalidDataException("Application message has trailing bytes.");
        }

        private void Require(int length)
        {
            if (length < 0 || cursor > source.Length - length)
                throw new InvalidDataException("Application message is truncated.");
        }
    }
}
