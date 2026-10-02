using System.Buffers.Binary;
namespace Stardust.Core.Protocol;

public sealed record SceneCreateObjectByCrcMessage(ulong ObjectId, float QuaternionX, float QuaternionY, float QuaternionZ, float QuaternionW, float PositionX, float PositionZ, float PositionY, uint TemplateCrc, byte RawHyperspaceFlag);
public sealed record SceneDestroyObjectMessage(ulong ObjectId, byte RawHyperspaceFlag);
public sealed record UpdateContainmentMessage(ulong ObjectId, ulong ContainerId, uint ArrangementBits);
public sealed record SceneEndBaselinesMessage(ulong ObjectId);

/// <summary>Exact application boundaries only. Does not normalize transforms or apply object lifecycle state.</summary>
public static class SceneObjectLifecycleCodec
{
    public const uint CreateOpcode = 0xFE89DDEA;
    public const uint DestroyOpcode = 0x4D45D504;
    public const uint ContainmentOpcode = 0x56CBDE9E;
    public const uint EndBaselinesOpcode = 0x2C436037;

    public static byte[] EncodeCreate(SceneCreateObjectByCrcMessage value)
    {
        ArgumentNullException.ThrowIfNull(value);
        float[] floats = [value.QuaternionX, value.QuaternionY, value.QuaternionZ, value.QuaternionW, value.PositionX, value.PositionZ, value.PositionY];
        if (floats.Any(f => !float.IsFinite(f))) throw new InvalidDataException("Lifecycle transform must be finite.");
        byte[] result = Header(47, 5, CreateOpcode);
        BinaryPrimitives.WriteUInt64LittleEndian(result.AsSpan(6), value.ObjectId);
        for (int i = 0; i < floats.Length; i++) BinaryPrimitives.WriteSingleLittleEndian(result.AsSpan(14 + i * 4), floats[i]);
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(42), value.TemplateCrc);
        result[46] = value.RawHyperspaceFlag;
        return result;
    }
    public static SceneCreateObjectByCrcMessage DecodeCreate(ReadOnlySpan<byte> message)
    {
        CheckHeader(message, 47, 5, CreateOpcode);
        float[] values = new float[7];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = BinaryPrimitives.ReadSingleLittleEndian(message[(14 + i * 4)..]);
            if (!float.IsFinite(values[i])) throw new InvalidDataException("Lifecycle transform must be finite.");
        }
        return new(BinaryPrimitives.ReadUInt64LittleEndian(message[6..]), values[0], values[1], values[2], values[3], values[4], values[5], values[6], BinaryPrimitives.ReadUInt32LittleEndian(message[42..]), message[46]);
    }
    public static byte[] EncodeDestroy(SceneDestroyObjectMessage value)
    {
        ArgumentNullException.ThrowIfNull(value);
        byte[] result = Header(15, 3, DestroyOpcode);
        BinaryPrimitives.WriteUInt64LittleEndian(result.AsSpan(6), value.ObjectId);
        result[14] = value.RawHyperspaceFlag;
        return result;
    }
    public static SceneDestroyObjectMessage DecodeDestroy(ReadOnlySpan<byte> message)
    {
        CheckHeader(message, 15, 3, DestroyOpcode);
        return new(BinaryPrimitives.ReadUInt64LittleEndian(message[6..]), message[14]);
    }
    public static byte[] EncodeContainment(UpdateContainmentMessage value)
    {
        ArgumentNullException.ThrowIfNull(value);
        byte[] result = Header(26, 4, ContainmentOpcode);
        BinaryPrimitives.WriteUInt64LittleEndian(result.AsSpan(6), value.ObjectId);
        BinaryPrimitives.WriteUInt64LittleEndian(result.AsSpan(14), value.ContainerId);
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(22), value.ArrangementBits);
        return result;
    }
    public static UpdateContainmentMessage DecodeContainment(ReadOnlySpan<byte> message)
    {
        CheckHeader(message, 26, 4, ContainmentOpcode);
        return new(BinaryPrimitives.ReadUInt64LittleEndian(message[6..]), BinaryPrimitives.ReadUInt64LittleEndian(message[14..]), BinaryPrimitives.ReadUInt32LittleEndian(message[22..]));
    }
    public static byte[] EncodeEndBaselines(SceneEndBaselinesMessage value)
    {
        ArgumentNullException.ThrowIfNull(value);
        byte[] result = Header(14, 2, EndBaselinesOpcode);
        BinaryPrimitives.WriteUInt64LittleEndian(result.AsSpan(6), value.ObjectId);
        return result;
    }
    public static SceneEndBaselinesMessage DecodeEndBaselines(ReadOnlySpan<byte> message)
    {
        CheckHeader(message, 14, 2, EndBaselinesOpcode);
        return new(BinaryPrimitives.ReadUInt64LittleEndian(message[6..]));
    }
    private static byte[] Header(int length, ushort count, uint opcode)
    {
        byte[] result = new byte[length];
        BinaryPrimitives.WriteUInt16LittleEndian(result, count);
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(2), opcode);
        return result;
    }
    private static void CheckHeader(ReadOnlySpan<byte> message, int length, ushort count, uint opcode)
    {
        if (message.Length != length || BinaryPrimitives.ReadUInt16LittleEndian(message) != count || BinaryPrimitives.ReadUInt32LittleEndian(message[2..]) != opcode)
            throw new InvalidDataException("Unexpected lifecycle message length/count/opcode.");
    }
}
