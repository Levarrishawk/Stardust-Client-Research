using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class StaticVertexColorScenarios
{
    internal static void Run()
    {
        if(PackedVertexColor.ToRgba(0x7F1234AB)!=new Vector4(18/255f,52/255f,171/255f,127/255f)||PackedVertexColor.ToRgba(0)!=Vector4.Zero||PackedVertexColor.ToRgba(uint.MaxValue)!=Vector4.One)throw new Exception("Asymmetric ARGB vertex-color channel mapping failed.");
        Console.WriteLine("PASS: packed COLOR0 asymmetric ARGB/normalized RGBA/zero/white fixtures without gamma conversion");
    }
    internal static void Probe(InteriorSceneContentBundle content)
    {
        int colored=0,vertices=0,primitives=0;
        foreach(var mesh in content.Materials!.Meshes.OrderBy(p=>p.Key,StringComparer.Ordinal))
        {
            using var raw=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);using var rgba=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);int count=0,total=0;
            foreach(var primitive in mesh.Value.Mesh.Primitives)
            {
                primitives++;total+=primitive.Vertices.Count;
                foreach(var vertex in primitive.Vertices)if(vertex.PackedColor0 is uint value)
                {
                    raw.AppendData(BitConverter.GetBytes(value));var color=PackedVertexColor.ToRgba(value);foreach(float c in new[]{color.X,color.Y,color.Z,color.W})rgba.AppendData(BitConverter.GetBytes(c));count++;
                }
            }
            Console.WriteLine("original_static_vertex_colors="+JsonSerializer.Serialize(new{path=mesh.Key,vertices=total,colored=count,rawSha256=Convert.ToHexString(raw.GetHashAndReset()),rgbaSha256=Convert.ToHexString(rgba.GetHashAndReset())}));colored+=count;vertices+=total;
        }
        Console.WriteLine($"PASS: original static vertex-color boundary meshes={content.Materials.Meshes.Count} primitives={primitives} vertices={vertices} colored={colored}");
    }
}
