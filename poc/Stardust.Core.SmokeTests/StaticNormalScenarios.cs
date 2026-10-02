using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class StaticNormalScenarios
{
    internal static void Probe(InteriorSceneContentBundle content)
    {
        TangentLightingScenarios.Probe(content);
        var bundle=content.StaticNormals!;
        foreach(var kind in new[]{TreLookupKind.Missing,TreLookupKind.Masked})HostAccountingScenarios.Reject<InvalidDataException>(()=>StaticNormalMaterialReader.Load(_=>new(kind),content.Materials!.Meshes));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>StaticNormalMaterialReader.Load(_=>new(TreLookupKind.Present,new byte[65537]),content.Materials!.Meshes));
        float[] V(Vector4 v)=>new[]{v.X,v.Y,v.Z,v.W};float[] V3(Vector3 v)=>new[]{v.X,v.Y,v.Z};int vertices=0,cases=0;
        foreach(var pair in bundle.Materials)
        {
            var m=pair.Value;Console.WriteLine("original_static_normal_material="+JsonSerializer.Serialize(new{path=pair.Key,m.PixelProgram,m.VertexProgram,m.MainTexture,m.NormalTexture,m.TangentCoordinateSet,specular=V(m.Material.SpecularRgba),power=m.Material.SpecularPower}));
            foreach(var mesh in content.Materials!.Meshes.OrderBy(p=>p.Key,StringComparer.Ordinal))foreach(var primitive in mesh.Value.Mesh.Primitives.Where(p=>p.ShaderPath==pair.Key))
            {
                using var bytes=new MemoryStream();using(var writer=new BinaryWriter(bytes,System.Text.Encoding.UTF8,true))foreach(var v in primitive.Vertices){var t=StaticNormalMaterialReader.ReadTangent(v,m.TangentCoordinateSet);foreach(float f in V(t))writer.Write(f);vertices++;}
                Console.WriteLine("original_static_tangents="+JsonSerializer.Serialize(new{path=mesh.Key,count=primitive.Vertices.Count,degenerate=primitive.Vertices.Count(v=>{var t=StaticNormalMaterialReader.ReadTangent(v,m.TangentCoordinateSet);return t.X*t.X+t.Y*t.Y+t.Z*t.Z<1e-12f;}),sha256=Convert.ToHexString(SHA256.HashData(bytes.ToArray()))}));
                int fallbacks=0;var preview=primitive.Vertices.Select(v=>{var tangent=StaticNormalMaterialReader.PreviewTangent(v,m.TangentCoordinateSet,out bool fallback);if(fallback)fallbacks++;return V(tangent);}).ToArray();
                Console.WriteLine("original_static_preview_tangents="+JsonSerializer.Serialize(new{path=mesh.Key,count=primitive.Vertices.Count,fallbacks,values=preview}));
                var example=primitive.Vertices[0];HostAccountingScenarios.Reject<InvalidDataException>(()=>StaticNormalMaterialReader.ReadTangent(example,2));
                HostAccountingScenarios.Reject<InvalidDataException>(()=>StaticNormalMaterialReader.ReadTangent(example with{AdditionalTextureCoordinates=new[]{new MeshTextureCoordinate(new(1,0,0,1),3)}},1));HostAccountingScenarios.Reject<InvalidDataException>(()=>StaticNormalMaterialReader.ReadTangent(example,0));HostAccountingScenarios.Reject<InvalidDataException>(()=>StaticNormalMaterialReader.ReadTangent(example with{AdditionalTextureCoordinates=null},1));
                foreach(var value in new[]{new Vector4(1,0,0,0),new Vector4(float.NaN,0,0,1)})HostAccountingScenarios.Reject<InvalidDataException>(()=>StaticNormalMaterialReader.ReadTangent(example with{AdditionalTextureCoordinates=new[]{new MeshTextureCoordinate(value,4)}},1));
            }
            foreach(var cn in new[]{new Vector4(.2f,.7f,.3f,.6f),new Vector4(.4f,0,.6f,1)})foreach(float mask in new[]{0f,.25f,.75f,1f})foreach(var light in new[]{new Vector3(.2f,.3f,.9f),new Vector3(0,0,-1)})foreach(bool bloom in new[]{false,true})foreach(bool fade in new[]{false,true})
            {
                var main=new Vector4(.2f,.4f,.8f,mask);var l=new SkeletalPixelLighting(light,new(.3f,.1f,1),new(.7f,.4f,.2f),new(.5f,.6f,.7f),new(.2f,.1f,.3f),new(.1f,.05f,.2f),bloom,fade,.6f);
                var result=StaticNormalMaterialReader.Evaluate(main,cn,m,l);
                Console.WriteLine("original_static_normal_case="+JsonSerializer.Serialize(new{path=pair.Key,main=V(main),normal=V(cn),light=V3(light),half=V3(l.TangentHalfAngle),diffuse=V3(l.Dot3DiffuseColor),specular=V3(l.Dot3SpecularColor),vertexDiffuse=V3(l.VertexDiffuse),vertexSpecular=V3(l.VertexSpecular),bloom,fade,opacity=.6f,result=V(result)}));cases++;
            }
        }
        foreach(var pair in bundle.Programs)
        {
            var program=pair.Value;Console.WriteLine("original_static_normal_program="+JsonSerializer.Serialize(new{path=pair.Key,program.InstructionsSha256}));
            var code=program.Metadata.Children[0].Children[1].Payload.ToArray();var fixture=MeshScenarios.Form("PSHP",MeshScenarios.Form("0000",MeshScenarios.Chunk("PSRC",System.Text.Encoding.ASCII.GetBytes("synthetic\0")),MeshScenarios.Chunk("PEXE",code)));
            for(int i=0;i<fixture.Length;i++)HostAccountingScenarios.Reject<InvalidDataException>(()=>StaticNormalPixelProgramDecoder.Decode(fixture.AsSpan(0,i)));
            code[^5]^=1;HostAccountingScenarios.Reject<InvalidDataException>(()=>StaticNormalPixelProgramDecoder.Decode(MeshScenarios.Form("PSHP",MeshScenarios.Form("0000",MeshScenarios.Chunk("PSRC",System.Text.Encoding.ASCII.GetBytes("synthetic\0")),MeshScenarios.Chunk("PEXE",code)))));
        }
        if(vertices!=110||bundle.Materials.Count!=1||bundle.Programs.Count!=1||cases!=64)throw new Exception("Original static-normal fixture denominator changed.");
        Console.WriteLine($"PASS: original static compressed-normal materials=1 tangents={vertices} pixel_cases={cases} malformed_input_gates=true");
    }
}
