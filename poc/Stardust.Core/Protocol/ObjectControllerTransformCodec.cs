using System.Buffers.Binary;
using System.Numerics;

namespace Stardust.Core.Protocol;

public enum ObjectControllerTransformDirection { ClientToServer, ServerToClient }
public enum ObjectControllerTransformKind : uint { Root = 0x71, WithParent = 0xF1 }

/// <summary>Owned wire values. OuterWord is a U32 timestamp outbound and F32 bits inbound.</summary>
public sealed record ObjectControllerTransformMessage(
    ObjectControllerTransformDirection Direction, ObjectControllerTransformKind Kind,
    uint Flags, ulong ObjectId, uint OuterWord, uint MovementCounter, ulong? ParentId,
    float QuaternionX, float QuaternionY, float QuaternionZ, float QuaternionW,
    float PositionX, float PositionY, float PositionZ, float Speed)
{
    public static ObjectControllerTransformMessage ClientToServer(ObjectControllerTransformKind kind,
        uint flags, ulong objectId, uint timestamp, uint counter, ulong? parentId,
        Vector4 quaternion, Vector3 position, float speed) => new(
            ObjectControllerTransformDirection.ClientToServer, kind, flags, objectId, timestamp, counter, parentId,
            quaternion.X, quaternion.Y, quaternion.Z, quaternion.W, position.X, position.Y, position.Z, speed);
    public static ObjectControllerTransformMessage ServerToClient(ObjectControllerTransformKind kind,
        uint flags, ulong objectId, float scalar, uint counter, ulong? parentId,
        Vector4 quaternion, Vector3 position, float speed) => new(
            ObjectControllerTransformDirection.ServerToClient, kind, flags, objectId, BitConverter.SingleToUInt32Bits(scalar), counter, parentId,
            quaternion.X, quaternion.Y, quaternion.Z, quaternion.W, position.X, position.Y, position.Z, speed);
    public uint Timestamp => Direction == ObjectControllerTransformDirection.ClientToServer
        ? OuterWord : throw new InvalidOperationException("Timestamp requires client-to-server direction.");
    public float ServerScalar => Direction == ObjectControllerTransformDirection.ServerToClient
        ? BitConverter.UInt32BitsToSingle(OuterWord) : throw new InvalidOperationException("Scalar requires server-to-client direction.");
    public ObjectControllerTransformMessage WithTimestamp(uint timestamp) =>
        Direction == ObjectControllerTransformDirection.ClientToServer ? this with { OuterWord = timestamp }
        : throw new InvalidOperationException("Timestamp requires client-to-server direction.");
    public ObjectControllerTransformMessage WithServerScalar(float scalar) =>
        Direction == ObjectControllerTransformDirection.ServerToClient ? this with { OuterWord = BitConverter.SingleToUInt32Bits(scalar) }
        : throw new InvalidOperationException("Scalar requires server-to-client direction.");
}

/// <summary>Full ObjectController frame only; no transport framing or world-state policy.</summary>
public static class ObjectControllerTransformCodec
{
    public const ushort OperandCount = 5;
    public const uint Opcode = 0x80CE5E46;
    public const int RootLength = 62;
    public const int ParentLength = 70;

    private static void DirectionCheck(ObjectControllerTransformDirection direction)
    {
        if (direction is not (ObjectControllerTransformDirection.ClientToServer or ObjectControllerTransformDirection.ServerToClient))
            throw new InvalidDataException("Invalid transform direction.");
    }
    private static void Validate(ObjectControllerTransformMessage value)
    {
        DirectionCheck(value.Direction);
        if (value.Kind is not (ObjectControllerTransformKind.Root or ObjectControllerTransformKind.WithParent)
            || (value.Kind == ObjectControllerTransformKind.WithParent) != value.ParentId.HasValue)
            throw new InvalidDataException("Invalid transform kind/parent shape.");
        if (value.Direction == ObjectControllerTransformDirection.ServerToClient && !float.IsFinite(value.ServerScalar))
            throw new InvalidDataException("Nonfinite server scalar.");
        foreach (float field in new[] { value.QuaternionX, value.QuaternionY, value.QuaternionZ, value.QuaternionW,
            value.PositionX, value.PositionY, value.PositionZ, value.Speed })
            if (!float.IsFinite(field)) throw new InvalidDataException("Nonfinite transform component.");
    }
    public static ObjectControllerTransformMessage Decode(ReadOnlySpan<byte> frame, ObjectControllerTransformDirection direction)
    {
        DirectionCheck(direction);
        if (frame.Length < 14 || BinaryPrimitives.ReadUInt16LittleEndian(frame) != OperandCount
            || BinaryPrimitives.ReadUInt32LittleEndian(frame[2..]) != Opcode)
            throw new InvalidDataException("Invalid ObjectController header.");
        var kind = (ObjectControllerTransformKind)BinaryPrimitives.ReadUInt32LittleEndian(frame[10..]);
        int length = kind switch { ObjectControllerTransformKind.Root => RootLength,
            ObjectControllerTransformKind.WithParent => ParentLength, _ => throw new InvalidDataException("Unsupported transform type.") };
        if (frame.Length != length) throw new InvalidDataException("Transform length mismatch.");
        uint flags = BinaryPrimitives.ReadUInt32LittleEndian(frame[6..]);
        ulong id = BinaryPrimitives.ReadUInt64LittleEndian(frame[14..]);
        uint word = BinaryPrimitives.ReadUInt32LittleEndian(frame[22..]);
        uint counter = BinaryPrimitives.ReadUInt32LittleEndian(frame[26..]);
        int cursor = 30;
        ulong? parent = null;
        if (kind == ObjectControllerTransformKind.WithParent) { parent = BinaryPrimitives.ReadUInt64LittleEndian(frame[cursor..]); cursor += 8; }
        float qx = Read(frame, ref cursor), qy = Read(frame, ref cursor), qz = Read(frame, ref cursor), qw = Read(frame, ref cursor);
        float x = Read(frame, ref cursor), z = Read(frame, ref cursor), y = Read(frame, ref cursor), speed = Read(frame, ref cursor);
        var value = new ObjectControllerTransformMessage(direction, kind, flags, id, word, counter, parent, qx, qy, qz, qw, x, y, z, speed);
        Validate(value);
        return value;
    }
    public static byte[] Encode(ObjectControllerTransformMessage value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Validate(value);
        byte[] frame = new byte[value.Kind == ObjectControllerTransformKind.Root ? RootLength : ParentLength];
        BinaryPrimitives.WriteUInt16LittleEndian(frame, OperandCount);
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(2), Opcode);
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(6), value.Flags);
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(10), (uint)value.Kind);
        BinaryPrimitives.WriteUInt64LittleEndian(frame.AsSpan(14), value.ObjectId);
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(22), value.OuterWord);
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(26), value.MovementCounter);
        int cursor = 30;
        if (value.ParentId is { } parent) { BinaryPrimitives.WriteUInt64LittleEndian(frame.AsSpan(cursor), parent); cursor += 8; }
        foreach (float field in new[] { value.QuaternionX, value.QuaternionY, value.QuaternionZ, value.QuaternionW,
            value.PositionX, value.PositionZ, value.PositionY, value.Speed }) Write(frame, ref cursor, field);
        if (cursor != frame.Length) throw new InvalidDataException("Internal transform size mismatch.");
        return frame;
    }
    private static float Read(ReadOnlySpan<byte> frame, ref int cursor)
    { float value = BinaryPrimitives.ReadSingleLittleEndian(frame[cursor..]); cursor += 4; return value; }
    private static void Write(Span<byte> frame, ref int cursor, float value)
    { BinaryPrimitives.WriteSingleLittleEndian(frame[cursor..], value); cursor += 4; }
}
