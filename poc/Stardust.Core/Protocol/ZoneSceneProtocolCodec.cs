using System.Buffers.Binary;
using System.Text;

namespace Stardust.Core.Protocol;

public sealed record SelectCharacterMessage(ulong ObjectId);
public sealed record CmdStartSceneMessage(byte RawFlag, ulong ObjectId, string Terrain, float X, float Z, float Y, string Template, ulong GalacticTime);

/// <summary>Offline application messages only; does not validate session state or apply scenes.</summary>
public static class ZoneSceneProtocolCodec
{
    public const uint SelectCharacterOpcode = 0xB5098D76;
    public const uint CmdStartSceneOpcode = 0x3AE6DFAE;

    public static byte[] EncodeSelectCharacter(ulong objectId)
    {
        byte[] result = new byte[14];
        BinaryPrimitives.WriteUInt16LittleEndian(result, 2);
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(2), SelectCharacterOpcode);
        BinaryPrimitives.WriteUInt64LittleEndian(result.AsSpan(6), objectId);
        return result;
    }

    public static SelectCharacterMessage DecodeSelectCharacter(ReadOnlySpan<byte> message)
    {
        Reader reader = new(message);
        reader.Header(2, SelectCharacterOpcode);
        ulong id = reader.U64();
        reader.End();
        return new(id);
    }

    public static CmdStartSceneMessage DecodeCmdStartScene(ReadOnlySpan<byte> message)
    {
        Reader reader = new(message);
        reader.Header(9, CmdStartSceneOpcode);
        byte flag = reader.Bytes(1)[0];
        ulong id = reader.U64();
        string terrain = reader.Ascii16();
        float x = reader.F32(), z = reader.F32(), y = reader.F32();
        string template = reader.Ascii16();
        ulong time = reader.U64();
        reader.End();
        return new(flag, id, terrain, x, z, y, template, time);
    }

    private ref struct Reader
    {
        private readonly ReadOnlySpan<byte> source;
        private int cursor;
        public Reader(ReadOnlySpan<byte> source) { this.source = source; cursor = 0; }
        public ReadOnlySpan<byte> Bytes(int count)
        {
            if (count < 0 || count > source.Length - cursor)
                throw new InvalidDataException("Zone scene message is truncated.");
            ReadOnlySpan<byte> value = source.Slice(cursor, count);
            cursor += count;
            return value;
        }
        public void Header(ushort count, uint opcode)
        {
            ushort actualCount = BinaryPrimitives.ReadUInt16LittleEndian(Bytes(2));
            uint actualOpcode = BinaryPrimitives.ReadUInt32LittleEndian(Bytes(4));
            if (actualCount != count || actualOpcode != opcode)
                throw new InvalidDataException("Unexpected zone scene application header.");
        }
        public ulong U64() => BinaryPrimitives.ReadUInt64LittleEndian(Bytes(8));
        public float F32()
        {
            float value = BinaryPrimitives.ReadSingleLittleEndian(Bytes(4));
            if (!float.IsFinite(value)) throw new InvalidDataException("Scene coordinate is not finite.");
            return value;
        }
        public string Ascii16()
        {
            int count = BinaryPrimitives.ReadUInt16LittleEndian(Bytes(2));
            ReadOnlySpan<byte> value = Bytes(count);
            if (value.ContainsAnyInRange((byte)0x80, byte.MaxValue))
                throw new InvalidDataException("Scene string contains non-ASCII bytes.");
            return Encoding.ASCII.GetString(value);
        }
        public void End()
        {
            if (cursor != source.Length) throw new InvalidDataException("Zone scene message has trailing bytes.");
        }
    }
}
