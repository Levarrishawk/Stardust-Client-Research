using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Stardust.Core.Protocol;

public sealed class SoeFragmentReassembler
{
    private readonly int maximumMessageSize;
    private byte[]? buffer;
    private int written;

    public SoeFragmentReassembler(int maximumMessageSize = 1_500_000)
    {
        if (maximumMessageSize <= 0) throw new ArgumentOutOfRangeException(nameof(maximumMessageSize));
        this.maximumMessageSize = maximumMessageSize;
    }

    public bool IsActive => buffer is not null;

    public byte[]? Accept(ReadOnlySpan<byte> fragmentPayload)
    {
        if (buffer is null)
        {
            if (fragmentPayload.Length < 4)
                throw new InvalidDataException("First SOE fragment lacks its total length.");
            uint total = BinaryPrimitives.ReadUInt32BigEndian(fragmentPayload);
            if (total == 0 || total > maximumMessageSize)
                throw new InvalidDataException("Invalid SOE fragmented message size.");
            buffer = new byte[(int)total];
            written = 0;
            fragmentPayload = fragmentPayload[4..];
        }

        if (fragmentPayload.Length > buffer.Length - written)
        {
            Reset();
            throw new InvalidDataException("SOE fragment overruns its declared message size.");
        }
        fragmentPayload.CopyTo(buffer.AsSpan(written));
        written += fragmentPayload.Length;
        if (written != buffer.Length) return null;
        byte[] completed = buffer;
        buffer = null;
        written = 0;
        return completed;
    }

    public void Reset()
    {
        if (buffer is not null) CryptographicOperations.ZeroMemory(buffer);
        buffer = null;
        written = 0;
    }
}
