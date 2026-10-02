using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

public sealed record SkeletalSpecularMaterial(string PixelProgram,StaticShaderMaterial Material,uint SpecularTextureTag,bool MaskUsesMainAlpha);

public static class SkeletalSpecularMaterials
{
    public static IReadOnlyDictionary<string,SkeletalSpecularMaterial> Describe(SkeletalMaterialBundle source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if(source.CompressedNormals is null)throw new InvalidDataException("Specular description requires validated compressed-normal programs.");
        var result=new Dictionary<string,SkeletalSpecularMaterial>(StringComparer.Ordinal);
        foreach(var pair in source.CompressedNormals.Materials)
        {
            var shader=source.Materials[pair.Key];var program=source.CompressedNormals.Programs[pair.Value.PixelProgram];
            var passes=source.Effects[shader.References.EffectPath].Implementations.SelectMany(i=>i.Passes).Where(p=>p.PixelProgram==pair.Value.PixelProgram).ToArray();
            if(passes.Length!=1||!passes[0].Fields.TryGetValue(0x7C,out uint tag)||tag!=0x4D41494E||!shader.Parameters.Materials.TryGetValue(tag,out var scalars))throw new InvalidDataException("Specular MAIN material selection differs.");
            uint mask=program.UsesAlphaHueMask?0x53504543u:0x4D41494Eu;
            var stages=passes[0].PixelTextures;
            if(program.UsesAlphaHueMask&&(stages.Count!=3||stages[2].Stage!=2||stages[2].TextureTag!=mask)||!program.UsesAlphaHueMask&&stages.Count!=2)throw new InvalidDataException("Specular sampler selection differs.");
            var textures=shader.References.Textures.Where(t=>t.Tag==mask).ToArray();
            if(textures.Length!=1||textures[0].SourceFlag!=0||textures[0].AssetPath is null||!source.Textures.ContainsKey(textures[0].AssetPath!))throw new InvalidDataException("Specular mask texture is not owned.");
            if(!shader.Parameters.CoordinateSets.TryGetValue(0x4D41494E,out byte set)||set!=0)throw new InvalidDataException("Specular mask requires MAIN UV0.");
            result.Add(pair.Key,new(pair.Value.PixelProgram,StaticShaderMaterialDecoder.Decode(scalars),mask,!program.UsesAlphaHueMask));
        }
        return new ReadOnlyDictionary<string,SkeletalSpecularMaterial>(result);
    }
}
