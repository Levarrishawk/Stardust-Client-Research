using System.Security.Cryptography;
using Stardust.Core.Assets;
namespace Stardust.Core.Proof;
public static class PathGraphVerification
{
    public static string ComputeSha256(PathGraphDescription graph)
    {
        using MemoryStream s=new();using(BinaryWriter w=new(s,System.Text.Encoding.ASCII,true))
        {
            w.Write(graph.RawType);w.Write((uint)graph.Nodes.Count);foreach(var n in graph.Nodes){w.Write(n.AuthoredId);w.Write(n.Field04);w.Write(n.GlobalKey);w.Write(n.RawType);w.Write(n.SerializedPosition.X);w.Write(n.SerializedPosition.Y);w.Write(n.SerializedPosition.Z);w.Write(n.Radius);}
            w.Write((uint)graph.Edges.Count);foreach(var e in graph.Edges){w.Write(e.From);w.Write(e.To);w.Write(e.LaneWidth0);w.Write(e.LaneWidth1);}
            w.Write((uint)graph.EdgeCounts.Count);foreach(int c in graph.EdgeCounts)w.Write(c);w.Write((uint)graph.EdgeStarts.Count);foreach(int start in graph.EdgeStarts)w.Write(start);
        }
        return Convert.ToHexString(SHA256.HashData(s.GetBuffer().AsSpan(0,(int)s.Length)));
    }
}
