using System.Security.Cryptography;

namespace Stardust.Core.Protocol;

/// <summary>Codec for the 00 19 application multipayload carried inside a data channel packet.</summary>
public static class SoeMultiPayloadCodec
{
    private static readonly byte[] Header = { 0x00, 0x19 };

    public static byte[] Encode(IReadOnlyList<byte[]> messages, int maximumPayloadSize)
    {
        if (messages.Count == 0) throw new ArgumentException("Multipayload must contain a message.", nameof(messages));
        using MemoryStream output = new();
        output.Write(Header);
        foreach (byte[] message in messages)
        {
            if (message.Length == 0) throw new ArgumentException("Multipayload messages cannot be empty.", nameof(messages));
            byte[] length = SoeVariableLengthCodec.Encode(checked((uint)message.Length));
            if (output.Length + length.Length + message.Length > maximumPayloadSize)
                throw new InvalidDataException("SOE multipayload exceeds its configured bound.");
            output.Write(length);
            output.Write(message);
        }
        return output.ToArray();
    }

    public static IReadOnlyList<byte[]> Decode(ReadOnlySpan<byte> payload, int maximumMessages, int maximumPayloadSize)
    {
        if (maximumMessages <= 0) throw new ArgumentOutOfRangeException(nameof(maximumMessages));
        if (payload.Length > maximumPayloadSize) throw new InvalidDataException("SOE multipayload exceeds its configured bound.");
        if (payload.Length < Header.Length || !payload[..2].SequenceEqual(Header))
            throw new InvalidDataException("Invalid SOE multipayload header.");
        List<byte[]> messages = new();
        int cursor = 2;
        try
        {
            while (cursor < payload.Length)
            {
                if (messages.Count == maximumMessages) throw new InvalidDataException("SOE multipayload contains too many messages.");
                uint length = SoeVariableLengthCodec.Decode(payload[cursor..], out int consumed);
                cursor += consumed;
                if (length == 0 || length > int.MaxValue || cursor > payload.Length - (int)length)
                    throw new InvalidDataException("Invalid SOE multipayload message length.");
                messages.Add(payload.Slice(cursor, (int)length).ToArray());
                cursor += (int)length;
            }
            if (messages.Count == 0) throw new InvalidDataException("SOE multipayload is empty.");
            return messages;
        }
        catch
        {
            foreach (byte[] message in messages) CryptographicOperations.ZeroMemory(message);
            throw;
        }
    }
}
