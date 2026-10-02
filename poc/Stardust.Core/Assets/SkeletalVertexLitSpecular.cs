using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Security.Cryptography;

namespace Stardust.Core.Assets;

public sealed record SkeletalVertexLitSpecularProgram(bool UsesAlphaHueMask,bool ScalesBloomRgb,string InstructionsSha256,IffNode Metadata);
public sealed record SkeletalVertexLitSpecularMaterial(string PixelProgram,uint TextureTag,byte CoordinateSet,StaticShaderMaterial Material);
public sealed record SkeletalVertexLitSpecularBundle(IReadOnlyDictionary<string,SkeletalVertexLitSpecularProgram> Programs,
    IReadOnlyDictionary<string,SkeletalVertexLitSpecularMaterial> Materials);

public static class SkeletalVertexLitSpecularProgramDecoder
{
    public static SkeletalVertexLitSpecularProgram Decode(ReadOnlySpan<byte> source)
    {
        var root=IffDocument.Parse(source,new(MaximumBytes:65536,MaximumNodes:16,MaximumDepth:4)).Root;
        Need(root.IsForm("PSHP")&&root.Children.Count==1&&root.Children[0].IsForm("0000"));var parts=root.Children[0].Children;
        Need(parts.Count==2&&parts[0].Tag=="PSRC"&&parts[1].Tag=="PEXE");var text=parts[0].Payload.Span;Need(text.Length>0&&text[^1]==0&&text[..^1].IndexOf((byte)0)<0);
        var code=parts[1].Payload.Span;Need(code.Length>=8&&code.Length%4==0&&Word(code,0)==0xFFFF0200);int at=1,count=code.Length/4;
        while(at<count&&(Word(code,at)&65535)==65534){uint token=Word(code,at);Need((token&0x80000000)==0);int size=(int)((token>>16)&32767);Need(size<=count-at-1);at+=size+1;}
        string hash=Convert.ToHexString(SHA256.HashData(code[(at*4)..]));bool hue=hash=="55E5591A9D73350E633491F80DE030921C7702D2E08BC3581DAE406D4EBC3D25";
        Need(hue||hash=="D794150290AC181EEF093C4FD974832CFF55039DA606FE749C6BC4A8E94212F2");return new(hue,!hue,hash,root);
    }
    private static uint Word(ReadOnlySpan<byte> data,int index)=>BinaryPrimitives.ReadUInt32LittleEndian(data[(index*4)..]);
    private static void Need(bool value){if(!value)throw new InvalidDataException("Unsupported vertex-lit specular PS20 family.");}
}

internal static class SkeletalVertexLitSpecularReader
{
    internal static SkeletalVertexLitSpecularBundle Load(SkeletalMaterialBundle source,OwnedAssetSourceCache cache)
    {
        if(source.AlbedoFactors is null)throw new InvalidDataException("Vertex-lit specular requires albedo factors.");
        var programs=new Dictionary<string,SkeletalVertexLitSpecularProgram>(StringComparer.Ordinal);var materials=new Dictionary<string,SkeletalVertexLitSpecularMaterial>(StringComparer.Ordinal);
        foreach(var pair in source.Materials)
        {
            var shader=pair.Value;if(shader.References.Textures.Any(t=>t.Tag==0x434E524D))continue;
            var passes=source.Effects[shader.References.EffectPath].Implementations.Where(i=>i.Passes.Count==1).Select(i=>i.Passes[0]).Where(p=>p.PixelProgram?.EndsWith("_ps20_nodot3.psh",StringComparison.Ordinal)==true).ToArray();
            Need(passes.Length==1);var pass=passes[0];string path=pass.PixelProgram!;
            if(!programs.TryGetValue(path,out var program)){var data=cache.Read(path);Need(data.Kind==TreLookupKind.Present&&data.Payload is not null);program=SkeletalVertexLitSpecularProgramDecoder.Decode(data.Payload!);programs.Add(path,program);}
            uint Field(int offset){Need(pass.Fields.TryGetValue(offset,out uint value));return value;}
            Need(Field(0x19)==1&&Field(0x1A)==1&&Field(0x20)==0&&Field(0x30)==0&&Field(0x3C)==15&&Field(0x7C)==0x4D41494E&&Field(0x40)==0x4D41494E&&Field(0x44)==(program.UsesAlphaHueMask?0x48554542u:0u));
            Need(pass.PixelTextures.Count==2&&pass.PixelTextures[0].Stage==0&&pass.PixelTextures[0].TextureTag==0x4D41494E&&pass.PixelTextures[1].Stage==1&&pass.PixelTextures[1].TextureTag==0x53504543);
            var spec=shader.References.Textures.Where(t=>t.Tag==0x53504543).ToArray();Need(spec.Length==1&&spec[0].SourceFlag==0&&spec[0].AssetPath is not null&&source.Textures.ContainsKey(spec[0].AssetPath!));
            Need(shader.Parameters.CoordinateSets.TryGetValue(0x4D41494E,out byte set)&&set==0&&program.UsesAlphaHueMask==source.AlbedoFactors.Materials[pair.Key].UsesAlphaHueMask);
            Need(shader.Parameters.Materials.TryGetValue(0x4D41494E,out var values));materials.Add(pair.Key,new(path,0x53504543,set,StaticShaderMaterialDecoder.Decode(values!)));
        }
        return new(new ReadOnlyDictionary<string,SkeletalVertexLitSpecularProgram>(programs),new ReadOnlyDictionary<string,SkeletalVertexLitSpecularMaterial>(materials));
    }
    private static void Need(bool value){if(!value)throw new InvalidDataException("Vertex-lit specular material boundary differs.");}
}

public static class SkeletalVertexLitSpecularEvaluator
{
    public static Vector4 Evaluate(Vector4 diffuseSample,float specularMask,SkeletalAlbedoFactors factors,SkeletalVertexLitSpecularProgram program,
        Vector3 vertexDiffuse,Vector3 vertexSpecular,bool bloomEnabled=false,bool fadeEnabled=false,float fadeOpacity=1)
    {
        ArgumentNullException.ThrowIfNull(program);
        if(program.UsesAlphaHueMask!=factors.UsesAlphaHueMask||!Finite(vertexDiffuse)||!Finite(vertexSpecular)||!float.IsFinite(specularMask)||specularMask<0||specularMask>1||!float.IsFinite(fadeOpacity)||fadeOpacity<0||fadeOpacity>1)throw new InvalidDataException("Invalid vertex-lit specular input.");
        var raw=vertexSpecular*specularMask;var rgb=SkeletalAlbedoEvaluator.Evaluate(diffuseSample,factors)*vertexDiffuse+raw*(bloomEnabled&&program.ScalesBloomRgb?.5f:1);
        float alpha=fadeEnabled?fadeOpacity:Vector3.Dot(raw,new(.3f,.59f,.11f))*(bloomEnabled?.65f:1);
        if(!Finite(rgb)||!float.IsFinite(alpha))throw new InvalidDataException("Vertex-lit specular overflow.");return new(rgb,alpha);
    }
    private static bool Finite(Vector3 value)=>float.IsFinite(value.X)&&float.IsFinite(value.Y)&&float.IsFinite(value.Z);
}
