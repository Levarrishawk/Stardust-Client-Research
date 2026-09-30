using System.Security.Cryptography;
using Stardust.Core.Assets;
namespace Stardust.Core.Proof;
public static class FloorMeshVerification
{
    public static string ComputeSha256(FloorMeshDescription floor)
    {
        using MemoryStream stream=new();using(BinaryWriter w=new(stream,System.Text.Encoding.ASCII,true))
        {
            w.Write((uint)floor.Vertices.Count);foreach(var p in floor.Vertices){w.Write(p.X);w.Write(p.Y);w.Write(p.Z);}
            w.Write((uint)floor.Triangles.Count);foreach(var t in floor.Triangles)
            {
                foreach(int i in t.VertexIndices)w.Write(i);w.Write(t.Id);foreach(int n in t.Neighbors)w.Write(n);
                w.Write(t.AuthoredNormal.X);w.Write(t.AuthoredNormal.Y);w.Write(t.AuthoredNormal.Z);
                foreach(byte f in t.EdgeFlags)w.Write(f);w.Write(t.NonSolidByte);w.Write(t.Tag);foreach(int p in t.PortalIds)w.Write(p);
            }
            w.Write((uint)floor.BoundaryEdges.Count);foreach(var e in floor.BoundaryEdges){w.Write(e.TriangleIndex);w.Write(e.EdgeIndex);w.Write(e.RawFlag);}
        }
        return Convert.ToHexString(SHA256.HashData(stream.GetBuffer().AsSpan(0,(int)stream.Length)));
    }
}
