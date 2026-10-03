using System.Buffers.Binary;
using System.Numerics;

namespace Stardust.Core.Protocol;

/// <summary>Owned raw compact APPLICATION transform. ParentId null selects the quarter-unit branch.</summary>
public sealed record CompactSceneTransformMessage(ulong ObjectId, ulong? ParentId,
    short RawX, short RawZ, short RawY, uint MovementCounter, sbyte SpeedRaw, sbyte DirectionRaw)
{
    public float CoordinateScale => ParentId.HasValue ? 0.125f : 0.25f;
    public Vector3 NativePosition => new(RawX * CoordinateScale, RawY * CoordinateScale, RawZ * CoordinateScale);
}

/// <summary>Exact fixed-length top-level codecs; no object lookup, heading policy or interpolation.</summary>
public static class CompactSceneTransformCodec
{
    public const ushort OperandCount = 8;
    public const uint UpdateTransformOpcode = 0x1B24F808;
    public const uint UpdateTransformWithParentOpcode = 0xC867AB5A;
    public const int UpdateTransformLength = 26;
    public const int UpdateTransformWithParentLength = 34;

    public static CompactSceneTransformMessage Decode(ReadOnlySpan<byte> message)
    {
        if (message.Length < 6 || BinaryPrimitives.ReadUInt16LittleEndian(message) != OperandCount)
            throw new InvalidDataException("Invalid compact transform header.");
        uint opcode = BinaryPrimitives.ReadUInt32LittleEndian(message[2..]);
        bool parent = opcode switch
        {
            UpdateTransformOpcode => false,
            UpdateTransformWithParentOpcode => true,
            _ => throw new InvalidDataException("Unsupported compact transform opcode.")
        };
        if (message.Length != (parent ? UpdateTransformWithParentLength : UpdateTransformLength))
            throw new InvalidDataException("Compact transform length mismatch.");
        int cursor = 6;
        ulong? parentId = null;
        if (parent) { parentId = BinaryPrimitives.ReadUInt64LittleEndian(message[cursor..]); cursor += 8; }
        ulong objectId = BinaryPrimitives.ReadUInt64LittleEndian(message[cursor..]); cursor += 8;
        short x = BinaryPrimitives.ReadInt16LittleEndian(message[cursor..]); cursor += 2;
        short z = BinaryPrimitives.ReadInt16LittleEndian(message[cursor..]); cursor += 2;
        short y = BinaryPrimitives.ReadInt16LittleEndian(message[cursor..]); cursor += 2;
        uint counter = BinaryPrimitives.ReadUInt32LittleEndian(message[cursor..]); cursor += 4;
        return new(objectId, parentId, x, z, y, counter, unchecked((sbyte)message[cursor]), unchecked((sbyte)message[cursor + 1]));
    }

    public static byte[] Encode(CompactSceneTransformMessage value)
    {
        ArgumentNullException.ThrowIfNull(value);
        byte[] message = new byte[value.ParentId.HasValue ? UpdateTransformWithParentLength : UpdateTransformLength];
        BinaryPrimitives.WriteUInt16LittleEndian(message, OperandCount);
        BinaryPrimitives.WriteUInt32LittleEndian(message.AsSpan(2), value.ParentId.HasValue ? UpdateTransformWithParentOpcode : UpdateTransformOpcode);
        int cursor = 6;
        if (value.ParentId is { } parent) { BinaryPrimitives.WriteUInt64LittleEndian(message.AsSpan(cursor), parent); cursor += 8; }
        BinaryPrimitives.WriteUInt64LittleEndian(message.AsSpan(cursor), value.ObjectId); cursor += 8;
        BinaryPrimitives.WriteInt16LittleEndian(message.AsSpan(cursor), value.RawX); cursor += 2;
        BinaryPrimitives.WriteInt16LittleEndian(message.AsSpan(cursor), value.RawZ); cursor += 2;
        BinaryPrimitives.WriteInt16LittleEndian(message.AsSpan(cursor), value.RawY); cursor += 2;
        BinaryPrimitives.WriteUInt32LittleEndian(message.AsSpan(cursor), value.MovementCounter); cursor += 4;
        message[cursor] = unchecked((byte)value.SpeedRaw); message[cursor + 1] = unchecked((byte)value.DirectionRaw);
        return message;
    }
}
