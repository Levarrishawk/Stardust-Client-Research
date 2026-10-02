using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

public sealed record SkeletalTangentLightingMaterial(string PixelProgram,string VertexProgram,byte TangentCoordinateSet);
public sealed record SkeletalTangentLightingBundle(IReadOnlyDictionary<string,SkeletalTangentLightingMaterial> Materials,TangentLightingSourceIdentity VertexSources)
{
    public string VertexProgramSha256=>VertexSources.VertexProgramSha256;
    public string FunctionsSha256=>VertexSources.FunctionsSha256;
}

/// <summary>Bounded authored vertex-program closure for the already validated compressed-normal pixel families.</summary>
public static class SkeletalTangentLightingReader
{
    public const string VertexPath=TangentLightingSourcesReader.VertexPath;
    public const string FunctionsPath=TangentLightingSourcesReader.FunctionsPath;
    public static SkeletalTangentLightingBundle Load(SkeletalMaterialBundle source,Func<string,TreLookupResult> resolve)
    {
        ArgumentNullException.ThrowIfNull(source);ArgumentNullException.ThrowIfNull(resolve);
        var normals=source.CompressedNormals??throw new InvalidDataException("Owned compressed-normal programs required.");
        Need(normals.Materials.Count>0&&normals.Materials.Count<=64);
        var materials=new Dictionary<string,SkeletalTangentLightingMaterial>(StringComparer.Ordinal);
        foreach(var pair in normals.Materials)
        {
            Need(source.Materials.TryGetValue(pair.Key,out var shader)&&normals.Programs.ContainsKey(pair.Value.PixelProgram));
            Need(source.Effects.TryGetValue(shader!.References.EffectPath,out var effect));
            var passes=effect!.Implementations.Where(i=>i.Passes.Count==1).Select(i=>i.Passes[0]).Where(p=>p.PixelProgram==pair.Value.PixelProgram).ToArray();
            Need(passes.Length==1&&passes[0].VertexProgram==VertexPath&&!effect.ContainsPrecalculatedVertexLighting);
            Need(shader.Parameters.CoordinateSets.TryGetValue(0x4D41494E,out byte main)&&main==0&&shader.Parameters.CoordinateSets.TryGetValue(0x4E524D4C,out byte normal)&&normal==0);
            Need(shader.Parameters.CoordinateSets.TryGetValue(0x444F5433,out byte tangent)&&tangent==1);
            materials.Add(pair.Key,new(pair.Value.PixelProgram,VertexPath,tangent));
        }
        return new(new ReadOnlyDictionary<string,SkeletalTangentLightingMaterial>(materials),TangentLightingSourcesReader.Load(resolve));
    }
    private static void Need(bool value){if(!value)throw new InvalidDataException("Skeletal tangent-lighting vertex closure differs.");}
}
