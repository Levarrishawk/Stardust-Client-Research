using System.Security.Cryptography;
using System.Text;
using Stardust.Core.Assets;

internal static class SkeletalGeometryScenarios
{
    internal static void Run()
    {
        byte[] source=Fixture();var geometry=SkeletalMeshGeometryDecoder.Decode(source);Array.Clear(source);
        if(geometry.SourceTriangles!=2||geometry.DegenerateTriangles!=1||geometry.Triangles!=1||geometry.Shaders[0].Triangles[0].SourceIndex!=0||geometry.Shaders[0].Triangles[0].OcclusionCombination!=3||geometry.Shaders[0].Coordinates[0].Dimension!=2||geometry.TextureRenderers[0].Bindings[0].TextureTag!=0x4D41494E)throw new Exception("Owned skeletal geometry mismatch.");
        if(SkeletalMeshGeometryDecoder.Decode(Fixture(occluded:false)).Shaders[0].Triangles[0].OcclusionCombination is not null)throw new Exception("ITL acquired synthetic occlusion.");
        void Reject(Action a)=>HostAccountingScenarios.Reject<InvalidDataException>(a);
        byte[] valid=Fixture();for(int i=0;i<valid.Length;i++)Reject(()=>SkeletalMeshGeometryDecoder.Decode(valid.AsSpan(0,i)));
        Reject(()=>SkeletalMeshGeometryDecoder.Decode(Fixture(index:3)));Reject(()=>SkeletalMeshGeometryDecoder.Decode(Fixture(uv:float.NaN)));
        Reject(()=>SkeletalMeshGeometryDecoder.Decode(Fixture(renderer:1)));Reject(()=>SkeletalMeshGeometryDecoder.Decode(Fixture(),maximumVertices:2));
        Reject(()=>SkeletalMeshGeometryDecoder.Decode(Fixture(),maximumTriangles:1));
        Console.WriteLine("PASS: owned shader vertex/normal/UV maps, ITL/OITL source IDs/degenerate filtering/TRTS retention and malformed index/float/resource/truncation gates");
    }
    private static byte[] Fixture(bool occluded=true,uint index=2,float uv=1,uint renderer=0)
    {
        byte[] F(params float[] f)=>f.SelectMany(BitConverter.GetBytes).ToArray();byte[] U(params uint[] u)=>u.SelectMany(BitConverter.GetBytes).ToArray();
        byte[] Text(string s)=>Encoding.ASCII.GetBytes(s+"\0");
        List<byte> triangles=new(U(2));foreach(uint[] triangle in new[]{new[]{0u,1u,index},new[]{0u,0u,2u}}){if(occluded)triangles.AddRange(BitConverter.GetBytes((short)3));triangles.AddRange(U(triangle));}
        byte[] shader=MeshScenarios.Form("PSDT",MeshScenarios.Chunk("NAME",Text("shader/test.sht")),MeshScenarios.Chunk("PIDX",U(3,0,1,2)),MeshScenarios.Chunk("NIDX",U(0,1,2)),
            MeshScenarios.Chunk("TXCI",U(1,2)),MeshScenarios.Form("TCSF",MeshScenarios.Chunk("TCSD",F(0,0,uv,0,0,1))),
            MeshScenarios.Form("PRIM",MeshScenarios.Chunk("INFO",U(1)),MeshScenarios.Chunk(occluded?"OITL":"ITL ",triangles.ToArray())));
        return MeshScenarios.Form("SKMG",MeshScenarios.Form("0004",MeshScenarios.Chunk("INFO",new[]{0,0,1,1,3,3,3,1,0}.SelectMany(BitConverter.GetBytes).Concat(new byte[8]).ToArray()),
            MeshScenarios.Chunk("SKTM",Text("appearance/skeleton/body.skt")),MeshScenarios.Chunk("XFNM",Text("root")),MeshScenarios.Chunk("POSN",F(0,0,0,1,0,0,0,1,0)),
            MeshScenarios.Chunk("TWHD",U(1,1,1)),MeshScenarios.Chunk("TWDT",U(0).Concat(F(1)).Concat(U(0)).Concat(F(1)).Concat(U(0)).Concat(F(1)).ToArray()),MeshScenarios.Chunk("NORM",F(0,0,1,0,0,1,0,0,1)),shader,
            MeshScenarios.Form("TRTS",MeshScenarios.Chunk("INFO",U(1,1)),MeshScenarios.Chunk("TRT ",Text("texturerenderer/test.trt").Concat(U(1,renderer,0x4D41494E)).ToArray()))));
    }
    internal static string CanonicalHash(SkeletalMeshGeometryDescription g)
    {
        using var stream=new MemoryStream();using var w=new BinaryWriter(stream);
        void Text(string s){w.Write(Encoding.ASCII.GetBytes(s));w.Write((byte)0);}
        void Indices(IReadOnlyList<uint>? values){w.Write(values is not null);if(values is not null)foreach(uint i in values)w.Write(i);}
        w.Write(g.SourceTriangles);w.Write(g.DegenerateTriangles);w.Write(g.Triangles);w.Write(g.Shaders.Count);w.Write(g.Tangents.Count);
        foreach(var v in g.Tangents){w.Write(v.X);w.Write(v.Y);w.Write(v.Z);w.Write(v.W);}
        foreach(var s in g.Shaders)
        {
            Text(s.ShaderPath);w.Write(s.PositionIndices.Count);foreach(uint i in s.PositionIndices)w.Write(i);Indices(s.NormalIndices);Indices(s.TangentIndices);
            w.Write(s.VertexColors is not null);if(s.VertexColors is not null)w.Write(s.VertexColors.Value.Span);
            w.Write(s.Coordinates.Count);foreach(var c in s.Coordinates){w.Write(c.Dimension);foreach(float f in c.Values)w.Write(f);}
            w.Write(s.SerializedTriangles.Count);foreach(var t in s.SerializedTriangles){w.Write(t.SourceIndex);w.Write(t.OcclusionCombination is not null);if(t.OcclusionCombination is not null)w.Write(t.OcclusionCombination.Value);w.Write(t.A);w.Write(t.B);w.Write(t.C);}
            w.Write(s.Triangles.Count);foreach(var t in s.Triangles)w.Write(t.SourceIndex);
        }
        w.Write(g.TextureRenderers.Count);foreach(var r in g.TextureRenderers){Text(r.Path);w.Write(r.Bindings.Count);foreach(var b in r.Bindings){w.Write(b.ShaderIndex);w.Write(b.TextureTag);}}
        return Convert.ToHexString(SHA256.HashData(stream.ToArray()));
    }
}
