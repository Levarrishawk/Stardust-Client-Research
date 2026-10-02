using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Security.Cryptography;

namespace Stardust.Core.Assets;

public sealed record StaticNormalMaterial(string PixelProgram,string VertexProgram,string MainTexture,string NormalTexture,byte TangentCoordinateSet,StaticShaderMaterial Material);
public sealed record StaticNormalBundle(IReadOnlyDictionary<string,SkeletalCompressedNormalProgram> Programs,IReadOnlyDictionary<string,StaticNormalMaterial> Materials,TangentLightingSourceIdentity? VertexSources=null)
{
    public string? VertexFunctionsSha256=>VertexSources?.FunctionsSha256;
}

public static class StaticNormalPixelProgramDecoder
{
    public static SkeletalCompressedNormalProgram Decode(ReadOnlySpan<byte> source)
    {
        var root=IffDocument.Parse(source,new(MaximumBytes:65536,MaximumNodes:16,MaximumDepth:4)).Root;
        Need(root.IsForm("PSHP")&&root.Children.Count==1&&root.Children[0].IsForm("0000"));var parts=root.Children[0].Children;
        Need(parts.Count==2&&parts[0].Tag=="PSRC"&&parts[1].Tag=="PEXE");var text=parts[0].Payload.Span;
        Need(text.Length>0&&text[^1]==0&&text[..^1].IndexOf((byte)0)<0);
        var code=parts[1].Payload.Span;Need(code.Length>=8&&code.Length%4==0&&Word(code,0)==0xFFFF0200);int at=1,count=code.Length/4;
        while(at<count&&(Word(code,at)&65535)==65534)
        {uint comment=Word(code,at);Need((comment&0x80000000)==0);int length=(int)((comment>>16)&32767);Need(length<=count-at-1);at+=length+1;}
        string hash=Convert.ToHexString(SHA256.HashData(code[(at*4)..]));
        Need(hash=="923751B744946EC571EC39AAB8DF590A8AC962D36AC70730B68E03043ECEBE0C");return new(false,hash,root);
    }
    private static uint Word(ReadOnlySpan<byte> bytes,int index)=>BinaryPrimitives.ReadUInt32LittleEndian(bytes[(index*4)..]);
    private static void Need(bool value){if(!value)throw new InvalidDataException("Unsupported static compressed-normal PS20 instruction identity.");}
}


public static class StaticNormalMaterialReader
{
    public static StaticNormalBundle Load(Func<string,TreLookupResult> resolve,IReadOnlyDictionary<string,TexturedStaticMesh> meshes)
    {
        ArgumentNullException.ThrowIfNull(resolve);ArgumentNullException.ThrowIfNull(meshes);
        if(meshes.Count>64)throw new InvalidDataException("Static normal mesh budget.");
        const string pixel="pixel_program/a_specmap_cbmp_ps20.psh",vertex="vertex_program/a_specmap_bump_vs20_for_ps20.vsh";
        var programs=new Dictionary<string,SkeletalCompressedNormalProgram>(StringComparer.Ordinal);var materials=new Dictionary<string,StaticNormalMaterial>(StringComparer.Ordinal);
        byte[] Read(string path){var input=resolve(path);Need(input.Kind==TreLookupKind.Present&&input.Payload is not null);Need(input.Payload!.Length<=65536);return input.Payload;}
        foreach(var mesh in meshes.Values)foreach(var pair in mesh.Materials)
        {
            if(pair.Value.References.EffectPath!="effect/a_specmap_cbmp.eft")continue;
            if(!materials.TryGetValue(pair.Key,out var description))
            {
                var source=pair.Value;var effect=mesh.Effects[source.References.EffectPath];Need(!effect.ContainsPrecalculatedVertexLighting);
                var passes=effect.Implementations.Where(i=>i.Passes.Count==1).Select(i=>i.Passes[0]).Where(p=>p.PixelProgram==pixel&&p.VertexProgram==vertex).ToArray();Need(passes.Length==1);var pass=passes[0];
                uint Field(int offset){Need(pass.Fields.TryGetValue(offset,out uint value));return value;}
                Need(Field(0x19)==1&&Field(0x1A)==1&&Field(0x20)==0&&Field(0x30)==0&&Field(0x3C)==15&&Field(0x7C)==0x4D41494E);
                Need(pass.PixelTextures.Count==2&&pass.PixelTextures[0].Stage==0&&pass.PixelTextures[0].TextureTag==0x4D41494E&&pass.PixelTextures[1].Stage==1&&pass.PixelTextures[1].TextureTag==0x434E524D);
                Need(source.Parameters.CoordinateSets.TryGetValue(0x4D41494E,out byte main)&&main==0&&source.Parameters.CoordinateSets.TryGetValue(0x4E524D4C,out byte normal)&&normal==0);
                Need(source.Parameters.CoordinateSets.TryGetValue(0x444F5433,out byte tangent)&&tangent==1);
                string Texture(uint tag){var bindings=source.References.Textures.Where(t=>t.Tag==tag).ToArray();Need(bindings.Length==1&&bindings[0].SourceFlag==0&&bindings[0].AssetPath is not null&&mesh.Textures.ContainsKey(bindings[0].AssetPath!));return bindings[0].AssetPath!;}
                var mainTexture=Texture(0x4D41494E);var normalTexture=Texture(0x434E524D);Need(mesh.Textures[normalTexture].Format==DdsFormat.Bc3);
                Need(source.Parameters.Materials.TryGetValue(0x4D41494E,out var scalars));var material=StaticShaderMaterialDecoder.Decode(scalars!);Need(material.SpecularPower>0&&material.SpecularPower<=4096);
                if(programs.Count==0)programs.Add(pixel,StaticNormalPixelProgramDecoder.Decode(Read(pixel)));
                description=new(pixel,vertex,mainTexture,normalTexture,tangent,material);Need(materials.Count<256);materials.Add(pair.Key,description);
            }
            foreach(var primitive in mesh.Mesh.Primitives.Where(p=>p.ShaderPath==pair.Key))foreach(var v in primitive.Vertices)ReadTangent(v,description.TangentCoordinateSet);
        }
        var vertexSources=programs.Count>0?TangentLightingSourcesReader.Load(resolve):null;
        return new(new ReadOnlyDictionary<string,SkeletalCompressedNormalProgram>(programs),new ReadOnlyDictionary<string,StaticNormalMaterial>(materials),vertexSources);
    }
    public static Vector4 ReadTangent(MeshVertex vertex,byte set)
    {
        Need(set>0&&vertex.AdditionalTextureCoordinates is not null&&set<=vertex.AdditionalTextureCoordinates.Count);
        var coordinate=vertex.AdditionalTextureCoordinates![set-1];var value=coordinate.Values;
        Need(coordinate.Dimension==4&&float.IsFinite(value.X)&&float.IsFinite(value.Y)&&float.IsFinite(value.Z)&&float.IsFinite(value.W)&&Math.Abs(value.W)==1);
        var tangent=new Vector3(value.X,value.Y,value.Z);Need(tangent.LengthSquared()<1e12f&&vertex.Normal.LengthSquared()>1e-12f&&vertex.Normal.LengthSquared()<1e12f);
        return value;
    }
    /// <summary>Explicit modern orthonormal preview basis; preserve source records, replace degenerate projected XYZ only.</summary>
    public static Vector4 PreviewTangent(MeshVertex vertex,byte set,out bool fallback)
    {
        var raw=ReadTangent(vertex,set);var normal=Vector3.Normalize(vertex.Normal);var tangent=new Vector3(raw.X,raw.Y,raw.Z);tangent-=normal*Vector3.Dot(normal,tangent);
        fallback=tangent.LengthSquared()<1e-12f;
        if(fallback){var axis=Math.Abs(normal.X)<=Math.Abs(normal.Y)&&Math.Abs(normal.X)<=Math.Abs(normal.Z)?Vector3.UnitX:Math.Abs(normal.Y)<=Math.Abs(normal.Z)?Vector3.UnitY:Vector3.UnitZ;tangent=axis-normal*Vector3.Dot(normal,axis);}
        tangent=Vector3.Normalize(tangent);return new(tangent,raw.W);
    }
    public static Vector4 Evaluate(Vector4 main,Vector4 normal,StaticNormalMaterial material,SkeletalPixelLighting lighting)
        =>SkeletalSpecularEvaluator.Evaluate(main,normal,main.W,new(material.PixelProgram,false,0,0,uint.MaxValue,uint.MaxValue),material.Material,lighting);
    private static void Need(bool value,[System.Runtime.CompilerServices.CallerArgumentExpression("value")] string? expression=null){if(!value)throw new InvalidDataException("Static normal material boundary differs: "+expression);}
}
