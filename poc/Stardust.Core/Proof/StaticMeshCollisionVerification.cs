using System.Buffers.Binary;
using System.Security.Cryptography;
using Stardust.Core.Assets;

namespace Stardust.Core.Proof;

/// <summary>Canonical extraction fingerprint; not a game asset or protocol encoding.</summary>
public static class StaticMeshCollisionVerification
{
    public static string ComputeSha256(StaticMeshCollisionGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        if(geometry.SourceTriangleCount is <0 or >65536||geometry.EligibleTriangleCount<0||geometry.EligibleTriangleCount>geometry.SourceTriangleCount||
            geometry.DegenerateTriangleCount<0||geometry.EligibleTriangleCount!=geometry.DegenerateTriangleCount+geometry.Triangles.Count)
            throw new InvalidDataException("Invalid collision extraction accounting.");
        using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);Span<byte> header=stackalloc byte[16];
        BinaryPrimitives.WriteInt32LittleEndian(header,geometry.SourceTriangleCount);BinaryPrimitives.WriteInt32LittleEndian(header[4..],geometry.EligibleTriangleCount);
        BinaryPrimitives.WriteInt32LittleEndian(header[8..],geometry.DegenerateTriangleCount);BinaryPrimitives.WriteInt32LittleEndian(header[12..],geometry.Triangles.Count);hash.AppendData(header);
        Span<byte> row=stackalloc byte[40];int previous=-1;
        foreach(var t in geometry.Triangles)
        {
            if(t.Id<=previous||t.Id>=geometry.SourceTriangleCount)throw new InvalidDataException("Invalid collision extraction ID ordering.");previous=t.Id;
            BinaryPrimitives.WriteInt32LittleEndian(row,t.Id);int cursor=4;
            foreach(var v in new[]{t.A,t.B,t.C})foreach(float scalar in new[]{v.X,v.Y,v.Z})
            {
                if(!float.IsFinite(scalar)||Math.Abs(scalar)>1e6f)throw new InvalidDataException("Invalid collision fingerprint position.");
                BinaryPrimitives.WriteInt32LittleEndian(row[cursor..],BitConverter.SingleToInt32Bits(scalar));cursor+=4;
            }
            hash.AppendData(row);
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }
}
