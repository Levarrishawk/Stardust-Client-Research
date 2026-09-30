using System.Security.Cryptography;
using System.Text;
using Stardust.Core.Assets;

namespace Stardust.Core.Proof;

/// <summary>Independent-verifier interchange: ordered LE32 counts, all declared LE floats/vertex, LE16 indices.</summary>
public static class StaticMeshVerification
{
    public static string ComputeSortedIndicesSha256(StaticMeshDescription mesh)
    {
        using MemoryStream bytes=new();
        using(BinaryWriter writer=new(bytes,Encoding.ASCII,true))
            foreach(MeshPrimitive primitive in mesh.Primitives)
            {
                writer.Write((uint)(primitive.SortedIndices?.Count??0));
                if(primitive.SortedIndices is null)continue;
                foreach(MeshSortedIndexVariant variant in primitive.SortedIndices)
                {
                    writer.Write(variant.Direction.X);writer.Write(variant.Direction.Y);writer.Write(variant.Direction.Z);
                    writer.Write((uint)variant.Indices.Count);foreach(ushort index in variant.Indices)writer.Write(index);
                }
            }
        return Convert.ToHexString(SHA256.HashData(bytes.GetBuffer().AsSpan(0,checked((int)bytes.Length))));
    }
    public static string ComputeGeometrySha256(StaticMeshDescription mesh)
    {
        using MemoryStream bytes = new();
        using (BinaryWriter writer = new(bytes, Encoding.ASCII, leaveOpen: true))
            foreach (MeshPrimitive primitive in mesh.Primitives)
            {
                writer.Write(checked((uint)primitive.Vertices.Count));
                writer.Write(checked((uint)primitive.Indices.Count));
                foreach (MeshVertex vertex in primitive.Vertices)
                {
                    writer.Write(vertex.Position.X); writer.Write(vertex.Position.Y); writer.Write(vertex.Position.Z);
                    writer.Write(vertex.Normal.X); writer.Write(vertex.Normal.Y); writer.Write(vertex.Normal.Z);
                    if(vertex.PackedColor0 is uint color)writer.Write(color);
                    writer.Write(vertex.TextureCoordinate.X); writer.Write(vertex.TextureCoordinate.Y);
                    if(vertex.AdditionalTextureCoordinates is not null)
                        foreach(MeshTextureCoordinate coordinate in vertex.AdditionalTextureCoordinates)
                        {writer.Write(coordinate.Values.X);
                         if(coordinate.Dimension>=2)writer.Write(coordinate.Values.Y);
                         if(coordinate.Dimension>=3)writer.Write(coordinate.Values.Z);
                         if(coordinate.Dimension==4)writer.Write(coordinate.Values.W);}
                }
                foreach (ushort index in primitive.Indices) writer.Write(index);
            }
        return Convert.ToHexString(SHA256.HashData(bytes.GetBuffer().AsSpan(0, checked((int)bytes.Length))));
    }
}
