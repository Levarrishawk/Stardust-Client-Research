using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Security.Cryptography;

namespace Stardust.Core.Assets;

public enum StaticRoomPixelKind { SpecularPs11,EmissivePs20,SpecularEmissivePs20 }
public sealed record StaticRoomPixelProgram(StaticRoomPixelKind Kind,string InstructionsSha256,IffNode Metadata);
public sealed record StaticRoomMaterial(string PixelProgram,string VertexProgram,StaticRoomPixelKind Kind,string MainTexture,string? SecondaryTexture,uint TextureFactorTag,Vector4? TextureFactor,StaticShaderMaterial Material);
public sealed record StaticRoomLightingBundle(IReadOnlyDictionary<string,StaticRoomPixelProgram> Programs,IReadOnlyDictionary<string,StaticRoomMaterial> Materials);

public static class StaticRoomPixelProgramDecoder
{
    public static StaticRoomPixelProgram Decode(ReadOnlySpan<byte> source)
    {
        var root=IffDocument.Parse(source,new(MaximumBytes:65536,MaximumNodes:16,MaximumDepth:4)).Root;
        Need(root.IsForm("PSHP")&&root.Children.Count==1&&root.Children[0].IsForm("0000"));var parts=root.Children[0].Children;
        Need(parts.Count==2&&parts[0].Tag=="PSRC"&&parts[1].Tag=="PEXE");var text=parts[0].Payload.Span;Need(text.Length>0&&text[^1]==0&&text[..^1].IndexOf((byte)0)<0);
        var code=parts[1].Payload.Span;Need(code.Length>=8&&code.Length%4==0);uint version=Word(code,0);Need(version is 0xFFFF0101 or 0xFFFF0200);int at=1,count=code.Length/4;
        while(at<count&&(Word(code,at)&65535)==65534){uint comment=Word(code,at);Need((comment&0x80000000)==0);int length=(int)((comment>>16)&32767);Need(length<=count-at-1);at+=length+1;}
        string hash=Convert.ToHexString(SHA256.HashData(code[(at*4)..]));var kind=hash switch{
            "0D943F264DCD800D634B2BE47C90A8641C59E0606A3F97F4D133DD4229D83712"=>StaticRoomPixelKind.SpecularPs11,
            "3349DDA1BF62ABD59E299CB91EB3F955348AFC8E6165AF1BF21885B626621513"=>StaticRoomPixelKind.EmissivePs20,
            "BD0F40D439A90D3DCCAB51DC9D50B274DBAFF85E00A99CE9822825A6A76C631B"=>StaticRoomPixelKind.SpecularEmissivePs20,
            _=>throw new InvalidDataException("Unsupported static-room pixel instruction identity.")};
        Need(version==(kind==StaticRoomPixelKind.SpecularPs11?0xFFFF0101u:0xFFFF0200u));return new(kind,hash,root);
    }
    private static uint Word(ReadOnlySpan<byte> data,int index)=>BinaryPrimitives.ReadUInt32LittleEndian(data[(index*4)..]);
    private static void Need(bool value){if(!value)throw new InvalidDataException("Unsupported static-room pixel envelope.");}
}

public static class StaticRoomPixelEvaluator
{
    public static Vector4 Evaluate(StaticRoomPixelKind kind,Vector4 main,float secondaryAlpha,Vector3 diffuse,Vector3 specular,float emissiveBloomStrength,bool bloom=false,bool fade=false,float opacity=1)
    {
        bool Finite(float value)=>float.IsFinite(value)&&Math.Abs(value)<=1e6;
        foreach(float value in new[]{main.X,main.Y,main.Z,main.W,secondaryAlpha,diffuse.X,diffuse.Y,diffuse.Z,specular.X,specular.Y,specular.Z,emissiveBloomStrength,opacity})if(!Finite(value))throw new InvalidDataException("Finite bounded static-room pixel inputs required.");
        if(main.W<0||main.W>1||secondaryAlpha<0||secondaryAlpha>1||opacity<0||opacity>1||emissiveBloomStrength<0)throw new InvalidDataException("Static-room mask/fade/bloom strength bounds.");
        var color=new Vector3(main.X,main.Y,main.Z);Vector3 rgb;float alpha;
        switch(kind)
        {
            case StaticRoomPixelKind.SpecularPs11:rgb=color*diffuse+specular*secondaryAlpha;alpha=opacity;break;
            case StaticRoomPixelKind.EmissivePs20:rgb=color*(diffuse*(1-main.W)+new Vector3(main.W));alpha=fade?opacity:emissiveBloomStrength*main.W;break;
            case StaticRoomPixelKind.SpecularEmissivePs20:
                var raw=specular*main.W;rgb=color*Vector3.Clamp(diffuse+new Vector3(secondaryAlpha),Vector3.Zero,Vector3.One)+raw*(bloom?.5f:1);
                alpha=fade?opacity:Vector3.Dot(raw,new(.3f,.59f,.11f))*(bloom?.65f:1)+emissiveBloomStrength*secondaryAlpha;break;
            default:throw new InvalidDataException("Unsupported static-room pixel kind.");
        }
        if(!Finite(rgb.X)||!Finite(rgb.Y)||!Finite(rgb.Z)||!Finite(alpha))throw new InvalidDataException("Static-room pixel output overflow.");return new(rgb,alpha);
    }
}

public static class StaticRoomLightingReader
{
    public static StaticRoomLightingBundle Load(Func<string,TreLookupResult> resolve,IReadOnlyDictionary<string,TexturedStaticMesh> meshes)
    {
        ArgumentNullException.ThrowIfNull(resolve);ArgumentNullException.ThrowIfNull(meshes);
        if(meshes.Count>64)throw new InvalidDataException("Static-room mesh budget.");
        var programs=new Dictionary<string,StaticRoomPixelProgram>(StringComparer.Ordinal);var materials=new Dictionary<string,StaticRoomMaterial>(StringComparer.Ordinal);var vertices=new HashSet<string>(StringComparer.Ordinal);
        var families=new Dictionary<string,(string Pixel,string Vertex,string Hash)>(StringComparer.Ordinal){
            ["effect/c_specmap.eft"]=("pixel_program/smap_mt.psh","vertex_program/tfcsl_2uv.vsh","25562D04E35B6F034E1E51C25FD54C2A3981FE28664B5EEA1D97EF0B09AB3995"),
            ["effect/c_emismap_bloom.eft"]=("pixel_program/a_emismap_bloom_ps20_nodot3.psh","vertex_program/c_simple_vs11.vsh","958E3295FD78D0CF9EAD90C3724C631133E898787ED322EF58D2ECA7C26C25EB"),
            ["effect/c_specmap_emismap_bloom.eft"]=("pixel_program/a_specmap_emismap_bloom_ps20_nodot3.psh","vertex_program/c_spec_vs20_nodot3.vsh","662D045AD7EC199D2FD285C87609F3F753F02E82AEB4349C0223C63C35E37F1B")};
        byte[] Read(string path){var input=resolve(path);if(input.Kind!=TreLookupKind.Present||input.Payload is null)throw new InvalidDataException("Required room program is missing/masked.");if(input.Payload.Length>65536)throw new InvalidDataException("Room program byte budget.");return input.Payload;}
        foreach(var mesh in meshes.Values)foreach(var pair in mesh.Materials)
        {
            if(materials.ContainsKey(pair.Key)||!families.TryGetValue(pair.Value.References.EffectPath,out var family))continue;
            var source=pair.Value;var effect=mesh.Effects[source.References.EffectPath];Need(effect.ContainsPrecalculatedVertexLighting);
            var passes=effect.Implementations.Where(i=>i.Passes.Count==1).Select(i=>i.Passes[0]).Where(p=>p.PixelProgram==family.Pixel&&p.VertexProgram==family.Vertex).ToArray();Need(passes.Length==1);var pass=passes[0];
            if(!programs.TryGetValue(family.Pixel,out var program)){program=StaticRoomPixelProgramDecoder.Decode(Read(family.Pixel));programs.Add(family.Pixel,program);}
            if(vertices.Add(family.Vertex))Need(Convert.ToHexString(SHA256.HashData(Read(family.Vertex)))==family.Hash);
            uint Field(int offset){Need(pass.Fields.TryGetValue(offset,out var value));return value;}
            Need(Field(0x19)==1&&Field(0x1A)==1&&Field(0x20)==0&&Field(0x30)==0&&Field(0x3C)==15&&Field(0x7C)==0x4D41494E);
            uint secondary=program.Kind==StaticRoomPixelKind.SpecularPs11?0x53504543u:0x454D4953u;int samplers=program.Kind==StaticRoomPixelKind.EmissivePs20?1:2;
            Need(pass.PixelTextures.Count==samplers&&pass.PixelTextures[0].Stage==0&&pass.PixelTextures[0].TextureTag==0x4D41494E&&(samplers==1||pass.PixelTextures[1].Stage==1&&pass.PixelTextures[1].TextureTag==secondary));
            string Texture(uint tag){var bindings=source.References.Textures.Where(t=>t.Tag==tag).ToArray();Need(bindings.Length==1&&bindings[0].SourceFlag==0&&bindings[0].AssetPath is not null&&mesh.Textures.ContainsKey(bindings[0].AssetPath!));Need(source.Parameters.CoordinateSets.TryGetValue(0x4D41494E,out byte set)&&set==0);return bindings[0].AssetPath!;}
            var factors=StaticShaderFactors.Decode(source.References);uint factorTag=Field(0x40);Vector4? factor=null;
            if(factorTag!=0){Need(factors.TryGetValue(factorTag,out uint packed));factor=PackedVertexColor.ToRgba(packed);}
            var material=source.Parameters.Materials.TryGetValue(0x4D41494E,out var values)?StaticShaderMaterialDecoder.Decode(values):StaticShaderMaterialDecoder.MissingSelectedTagFallback;
            Need(materials.Count<256);materials.Add(pair.Key,new(family.Pixel,family.Vertex,program.Kind,Texture(0x4D41494E),samplers==1?null:Texture(secondary),factorTag,factor,material));
        }
        return new(new ReadOnlyDictionary<string,StaticRoomPixelProgram>(programs),new ReadOnlyDictionary<string,StaticRoomMaterial>(materials));
    }
    private static void Need(bool value,[System.Runtime.CompilerServices.CallerArgumentExpression("value")] string? expression=null){if(!value)throw new InvalidDataException("Static-room selected material boundary differs: "+expression);}
}
