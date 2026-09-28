namespace Stardust.Core.Protocol;

public static class SoeVariableLengthCodec
{
    public static byte[] Encode(uint value)
    {
        if (value < 0xFE) return new[] { (byte)value };
        if (value < 0xFFFF)
            return new[] { (byte)0xFF, (byte)(value >> 8), (byte)value };
        return new[]
        {
            (byte)0xFF, (byte)0xFF, (byte)0xFF,
            (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value
        };
    }

    public static uint Decode(ReadOnlySpan<byte> source, out int bytesConsumed)
    {
        if (source.IsEmpty) throw new InvalidDataException("Variable-length value is truncated.");
        if (source[0] != 0xFF)
        {
            bytesConsumed = 1;
            return source[0];
        }
        if (source.Length < 3) throw new InvalidDataException("Variable-length 16-bit value is truncated.");
        uint shortValue = (uint)((source[1] << 8) | source[2]);
        if (shortValue != 0xFFFF)
        {
            bytesConsumed = 3;
            return shortValue;
        }
        if (source.Length < 7) throw new InvalidDataException("Variable-length 32-bit value is truncated.");
        bytesConsumed = 7;
        return ((uint)source[3] << 24) | ((uint)source[4] << 16) | ((uint)source[5] << 8) | source[6];
    }
}
