using System.Buffers.Binary;
using System.Security.Cryptography;
using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

/// <summary>Explicit synthetic offline channel-zero ACK; never evidence of a server receive.</summary>
public static class OfflineSoeAcknowledgementFactory
{
    public static byte[] Create(ushort sequence,uint seed,int crcWidth=2,int maximumDatagram=496)
    {
        if(crcWidth is < 1 or > 4)throw new ArgumentOutOfRangeException(nameof(crcWidth));
        if(maximumDatagram is < 4 or > SoeClientTransport.MaximumWireDatagramSize)
            throw new ArgumentOutOfRangeException(nameof(maximumDatagram));
        if(maximumDatagram<5+crcWidth)throw new InvalidDataException("Maximum cannot hold the protected ACK.");
        Span<byte> packet=stackalloc byte[4];
        try
        {
            BinaryPrimitives.WriteUInt16BigEndian(packet,0x15);
            BinaryPrimitives.WriteUInt16BigEndian(packet[2..],sequence);
            return SoeDatagramCodec.EncodeUncompressed(packet,seed,crcWidth,maximumDatagram);
        }
        finally{CryptographicOperations.ZeroMemory(packet);}
    }
}
