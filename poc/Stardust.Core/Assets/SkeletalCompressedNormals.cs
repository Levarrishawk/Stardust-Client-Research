using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Security.Cryptography;

namespace Stardust.Core.Assets;

public sealed record SkeletalCompressedNormalProgram(bool UsesAlphaHueMask,string InstructionsSha256,IffNode Metadata);
public sealed record SkeletalCompressedNormalMaterial(string PixelProgram,uint TextureTag,byte CoordinateSet);
public sealed record SkeletalCompressedNormalBundle(IReadOnlyDictionary<string,SkeletalCompressedNormalProgram> Programs,
    IReadOnlyDictionary<string,SkeletalCompressedNormalMaterial> Materials);

/// <summary>Exact deployed PS20 inspection families, identified by instruction hash after bounded leading comments.</summary>
public static class SkeletalCompressedNormalProgramDecoder
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
        bool mask=hash=="B53EC0E8F576B033748323AAB8F01C355F3300D6EB51217765179FBD796B39E0";
        Need(mask||hash=="3DA984253F860A45C6B0915FDAD4DE9DDB4685EC45FDDDD8B0E4108EF5F0B0B3");return new(mask,hash,root);
    }
    private static uint Word(ReadOnlySpan<byte> bytes,int index)=>BinaryPrimitives.ReadUInt32LittleEndian(bytes[(index*4)..]);
    private static void Need(bool value){if(!value)throw new InvalidDataException("Unsupported compressed-normal PS20 inspection family.");}
}

internal static class SkeletalCompressedNormalReader
{
    internal static SkeletalCompressedNormalBundle Load(SkeletalMaterialBundle bundle,OwnedAssetSourceCache sources)
    {
        if(bundle.AlbedoFactors is null)throw new InvalidDataException("Compressed-normal preview requires validated albedo factors.");
        var programs=new Dictionary<string,SkeletalCompressedNormalProgram>(StringComparer.Ordinal);var materials=new Dictionary<string,SkeletalCompressedNormalMaterial>(StringComparer.Ordinal);
        foreach(var pair in bundle.Materials)
        {
            var material=pair.Value;var normal=material.References.Textures.Where(t=>t.Tag==0x434E524D).ToArray();
            if(normal.Length==0)continue;
            Need(normal.Length==1&&normal[0].SourceFlag==0&&normal[0].AssetPath is not null,"One named CNRM binding required.");
            Need(material.Parameters.CoordinateSets.TryGetValue(0x4E524D4C,out byte coordinate)&&coordinate==0,"Compressed-normal inspection requires NRML UV0.");
            Need(bundle.Textures[normal[0].AssetPath!].Format==DdsFormat.Bc3,"Compressed-normal inspection requires legacy BC3.");
            var passes=bundle.Effects[material.References.EffectPath].Implementations.Where(i=>i.Passes.Count==1).Select(i=>i.Passes[0])
                .Where(p=>p.PixelProgram?.EndsWith("_cbmp_ps20.psh",StringComparison.Ordinal)==true).ToArray();Need(passes.Length==1,"One compressed-normal PS20 candidate required.");
            var pass=passes[0];string path=pass.PixelProgram!;
            if(!programs.TryGetValue(path,out var program))
            {
                var source=sources.Read(path);Need(source.Kind==TreLookupKind.Present&&source.Payload is not null,"Compressed-normal program missing/masked.");
                program=SkeletalCompressedNormalProgramDecoder.Decode(source.Payload!);programs.Add(path,program);
            }
            Need(pass.PixelTextures.Any(t=>t.Stage==1&&t.TextureTag==0x434E524D),"Compressed-normal sampler1 differs.");
            uint Field(int offset){Need(pass.Fields.TryGetValue(offset,out uint value),"Compressed-normal pass field absent.");return value;}
            Need(Field(0x19)==1&&Field(0x1A)==1&&Field(0x20)==0&&Field(0x30)==0&&Field(0x3C)==15&&Field(0x40)==0x4D41494E&&Field(0x44)==(program.UsesAlphaHueMask?0x48554542u:0u),"Compressed-normal opaque/factor state differs.");
            Need(pass.PixelTextures.Any(t=>t.Stage==0&&t.TextureTag==0x4D41494E),"Compressed-normal diffuse sampler0 differs.");
            Need(program.UsesAlphaHueMask==bundle.AlbedoFactors.Materials[pair.Key].UsesAlphaHueMask,"Normal/albedo factor family differs.");
            materials.Add(pair.Key,new(path,0x434E524D,coordinate));
        }
        return new(new ReadOnlyDictionary<string,SkeletalCompressedNormalProgram>(programs),new ReadOnlyDictionary<string,SkeletalCompressedNormalMaterial>(materials));
    }
    private static void Need(bool value,string message){if(!value)throw new InvalidDataException(message);}
}

public static class SkeletalCompressedNormalEvaluator
{
    /// <summary>Decode filtered source-encoding alpha/green using the selected compiled RSQ/RCP absolute-radicand behavior.</summary>
    public static Vector3 Decode(Vector4 sampledRgba)
    {
        if(!float.IsFinite(sampledRgba.X)||!float.IsFinite(sampledRgba.Y)||!float.IsFinite(sampledRgba.Z)||!float.IsFinite(sampledRgba.W)||
            sampledRgba.X<0||sampledRgba.X>1||sampledRgba.Y<0||sampledRgba.Y>1||sampledRgba.Z<0||sampledRgba.Z>1||sampledRgba.W<0||sampledRgba.W>1)
            throw new InvalidDataException("Invalid compressed-normal sample.");
        float x=(sampledRgba.W-.5f)*2,y=(sampledRgba.Y-.5f)*2,z=MathF.Sqrt(MathF.Abs(1-(x*x+y*y)));
        return Vector3.Normalize(new(x,y,z));
    }
    public static Vector3 ToObject(Vector3 tangentNormal,Vector3 vertexNormal,Vector4 tangent)
    {
        var t=new Vector3(tangent.X,tangent.Y,tangent.Z);var b=Vector3.Cross(vertexNormal,t)*tangent.W;
        var value=t*tangentNormal.X+b*tangentNormal.Y+vertexNormal*tangentNormal.Z;
        if(!float.IsFinite(value.X)||!float.IsFinite(value.Y)||!float.IsFinite(value.Z))throw new InvalidDataException("Invalid tangent basis.");
        return value;
    }
}
