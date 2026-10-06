using System.Buffers.Binary;
using System.Security.Cryptography;
using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

public sealed record OfflineSoeSessionBootstrapReceipt(SoeClientTransport Transport,
    string RequestHex,string ResponseHex);

/// <summary>Explicit synthetic offline handshake only; caller owns and closes the returned transport.</summary>
public static class OfflineSoeSessionBootstrap
{
    public static OfflineSoeSessionBootstrapReceipt Create(uint connectionId,uint seed)
    {
        var transport=new SoeClientTransport();
        byte[] request=Array.Empty<byte>(),response=Array.Empty<byte>();
        try
        {
            request=transport.BeginSession(connectionId,496);
            response=new byte[SoeSessionCodec.ResponseLength];
            BinaryPrimitives.WriteUInt16BigEndian(response,2);
            BinaryPrimitives.WriteUInt32BigEndian(response.AsSpan(2),connectionId);
            BinaryPrimitives.WriteUInt32BigEndian(response.AsSpan(6),seed);
            response[10]=2;response[11]=1;response[12]=4;
            BinaryPrimitives.WriteUInt32BigEndian(response.AsSpan(13),496);
            _=SoeSessionCodec.DecodeResponse(response,connectionId);
            transport.AcceptSessionResponse(response);
            return new(transport,Convert.ToHexString(request),Convert.ToHexString(response));
        }
        catch{transport.Close();throw;}
        finally{CryptographicOperations.ZeroMemory(request);CryptographicOperations.ZeroMemory(response);}
    }
}
