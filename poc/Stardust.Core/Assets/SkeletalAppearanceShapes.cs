using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

public sealed record SkeletalShapeSelection(IReadOnlyDictionary<string,int> Values,
    IReadOnlyDictionary<string,SkeletalMeshGeometryDescription> Geometries);

public static class SkeletalAppearanceShapes
{
    public static CreatureAppearanceDescriptorBundle Apply(CreatureAppearanceDescriptorBundle source,
        IReadOnlyDictionary<string,int> values,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(source);ArgumentNullException.ThrowIfNull(values);cancellationToken.ThrowIfCancellationRequested();
        if(source.Geometries is null||source.BlendShapes is null||source.Geometries.Count>64||source.Geometries.Count!=source.BlendShapes.Count)
            throw new InvalidDataException("Owned geometry and blend descriptions required.");
        var selected=new Dictionary<string,int>(values,StringComparer.Ordinal);
        var keys=source.BlendShapes.Values.SelectMany(s=>s.Targets).Select(t=>t.VariableName).ToHashSet(StringComparer.Ordinal);
        if(selected.Count!=keys.Count||selected.Keys.Any(k=>!keys.Contains(k))||selected.Values.Any(v=>v is <0 or >255))throw new InvalidDataException("Exact explicit shared-owner shape values required.");
        var geometries=new Dictionary<string,SkeletalMeshGeometryDescription>(StringComparer.Ordinal);
        foreach(var pair in source.Geometries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if(!source.BlendShapes.TryGetValue(pair.Key,out var description))throw new InvalidDataException("Missing owned blend description.");
            var subset=description.Targets.Select(t=>t.VariableName).Distinct(StringComparer.Ordinal).ToDictionary(k=>k,k=>selected[k],StringComparer.Ordinal);
            geometries.Add(pair.Key,SkeletalBlendShapeEvaluator.Apply(pair.Value,description,subset,cancellationToken));
        }
        return source with{ShapeSelection=new(new ReadOnlyDictionary<string,int>(selected),new ReadOnlyDictionary<string,SkeletalMeshGeometryDescription>(geometries))};
    }
    public static IReadOnlyDictionary<string,int> LoadProfile(Stream stream)
    {
        var profile=BlueprintFaceCustomizationProfile.Load(stream);
        if(profile.Keys.Any(k=>k.FactoryFlag))throw new InvalidDataException("Shape variables require shared-owner domain.");
        return new ReadOnlyDictionary<string,int>(profile.ToDictionary(p=>p.Key.VariableName,p=>p.Value,StringComparer.Ordinal));
    }
}
