using System.Buffers.Binary;
using System.Text;

namespace Stardust.Core.Protocol;

public sealed record LoginCluster(uint GalaxyId, string Name, uint Metadata);
public sealed record LoginClusterList(IReadOnlyList<LoginCluster> Clusters, uint Footer);
public sealed record LoginClusterStatus(
    uint GalaxyId, string Hostname, ushort ZonePort, ushort PingPort,
    uint Population, uint Capacity, uint Reserved, uint Metadata, uint Status, bool Recommended);
public sealed record LoginCharacter(string Name, uint RaceCrc, ulong ObjectId, uint GalaxyId, uint CharacterType);

public static class LoginServerListCodec
{
    public const uint LoginEnumClusterOpcode = 0xC11C63B9;
    public const uint LoginClusterStatusOpcode = 0x3436AEB6;
    public const uint EnumerateCharacterIdOpcode = 0x65EA4574;

    public static LoginClusterList DecodeClusterList(ReadOnlySpan<byte> message, int maximumRecords = 1024)
    {
        Reader reader = new(message);
        reader.ExpectHeader(2, LoginEnumClusterOpcode);
        int count = reader.ReadCount(maximumRecords);
        List<LoginCluster> clusters = new(count);
        for (int i = 0; i < count; i++)
            clusters.Add(new(reader.ReadUInt32(), reader.ReadAscii16(), reader.ReadUInt32()));
        uint footer = reader.ReadUInt32();
        reader.RequireEnd();
        return new(clusters, footer);
    }

    public static IReadOnlyList<LoginClusterStatus> DecodeClusterStatus(
        ReadOnlySpan<byte> message, int maximumRecords = 1024)
    {
        Reader reader = new(message);
        reader.ExpectHeader(3, LoginClusterStatusOpcode);
        int count = reader.ReadCount(maximumRecords);
        List<LoginClusterStatus> statuses = new(count);
        for (int i = 0; i < count; i++)
        {
            statuses.Add(new(
                reader.ReadUInt32(), reader.ReadAscii16(), reader.ReadUInt16(), reader.ReadUInt16(),
                reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(),
                reader.ReadUInt32(), reader.ReadByte() != 0));
        }
        reader.RequireEnd();
        return statuses;
    }

    public static IReadOnlyList<LoginCharacter> DecodeCharacters(
        ReadOnlySpan<byte> message, int maximumRecords = 1024)
    {
        Reader reader = new(message);
        reader.ExpectHeader(2, EnumerateCharacterIdOpcode);
        int count = reader.ReadCount(maximumRecords);
        List<LoginCharacter> characters = new(count);
        for (int i = 0; i < count; i++)
        {
            characters.Add(new(
                reader.ReadUnicode32(), reader.ReadUInt32(), reader.ReadUInt64(),
                reader.ReadUInt32(), reader.ReadUInt32()));
        }
        reader.RequireEnd();
        return characters;
    }

    private ref struct Reader
    {
        private readonly ReadOnlySpan<byte> source;
        private int cursor;

        public Reader(ReadOnlySpan<byte> source) => this.source = source;

        public void ExpectHeader(ushort operands, uint opcode)
        {
            if (ReadUInt16() != operands || ReadUInt32() != opcode)
                throw new InvalidDataException("Unexpected login server-list message header.");
        }

        public int ReadCount(int maximum)
        {
            uint count = ReadUInt32();
            if (count > maximum) throw new InvalidDataException("Login server-list record count exceeds its bound.");
            return (int)count;
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

        public ulong ReadUInt64()
        {
            Require(8);
            ulong value = BinaryPrimitives.ReadUInt64LittleEndian(source[cursor..]);
            cursor += 8;
            return value;
        }

        public string ReadAscii16()
        {
            ushort length = ReadUInt16();
            Require(length);
            ReadOnlySpan<byte> bytes = source.Slice(cursor, length);
            if (bytes.ContainsAnyInRange((byte)0x80, byte.MaxValue))
                throw new InvalidDataException("ASCII16 field contains non-ASCII bytes.");
            cursor += length;
            return Encoding.ASCII.GetString(bytes);
        }

        public string ReadUnicode32()
        {
            uint codeUnits = ReadUInt32();
            if (codeUnits > int.MaxValue / 2) throw new InvalidDataException("Unicode32 length exceeds local bounds.");
            int byteLength = checked((int)codeUnits * 2);
            Require(byteLength);
            ReadOnlySpan<byte> bytes = source.Slice(cursor, byteLength);
            cursor += byteLength;
            try { return new UnicodeEncoding(false, false, true).GetString(bytes); }
            catch (DecoderFallbackException exception)
            {
                throw new InvalidDataException("Unicode32 field contains invalid UTF-16LE.", exception);
            }
        }

        public void RequireEnd()
        {
            if (cursor != source.Length) throw new InvalidDataException("Login server-list message has trailing bytes.");
        }

        private void Require(int length)
        {
            if (cursor > source.Length - length) throw new InvalidDataException("Login server-list message is truncated.");
        }
    }
}
