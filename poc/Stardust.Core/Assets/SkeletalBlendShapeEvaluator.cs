using System.Numerics;

namespace Stardust.Core.Assets;

/// <summary>Explicit bounded shape selection before weighted skinning; float32 modern arithmetic policy.</summary>
public static class SkeletalBlendShapeEvaluator
{
    public static SkeletalMeshGeometryDescription Apply(SkeletalMeshGeometryDescription geometry,
        SkeletalBlendShapeDescription description,IReadOnlyDictionary<string,int> values,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(geometry);ArgumentNullException.ThrowIfNull(description);ArgumentNullException.ThrowIfNull(values);
        cancellationToken.ThrowIfCancellationRequested();
        if(description.Targets.Count>4096||geometry.Skin.Positions.Count>262144||geometry.Skin.Normals.Count>262144||geometry.Tangents.Count>262144)
            throw new InvalidDataException("Shape target/vector budget.");
        var selected=new Dictionary<string,int>(values,StringComparer.Ordinal);
        var keys=description.Targets.Select(t=>t.VariableName).Distinct(StringComparer.Ordinal).ToArray();
        if(selected.Count!=keys.Length||keys.Any(k=>!selected.ContainsKey(k))||selected.Any(v=>v.Value is <0 or >255))
            throw new InvalidDataException("Shape selection requires exact shared-owner names and explicit values 0..255.");
        long deltaCount=0;
        // Revalidate publicly constructible descriptions against the owned mesh before indexing.
        foreach(var target in description.Targets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            deltaCount+=target.Positions.Count+target.Normals.Count+(long)target.Tangents.Count;
            if(deltaCount>1_048_576)throw new InvalidDataException("Shape aggregate delta budget.");
            if(target.HardpointMetadata is not null)throw new InvalidDataException("Shape hardpoint execution is unsupported.");
            Check(target.Positions,geometry.Skin.Positions.Count);Check(target.Normals,geometry.Skin.Normals.Count);Check(target.Tangents,geometry.Tangents.Count);
        }
        var positions=geometry.Skin.Positions.ToArray();var normals=geometry.Skin.Normals.ToArray();var tangents=geometry.Tangents.ToArray();bool active=false;
        if(positions.Any(v=>!Finite(v))||normals.Any(v=>!Finite(v))||tangents.Any(v=>!Finite(new(v.X,v.Y,v.Z))||!float.IsFinite(v.W)))throw new InvalidDataException("Nonfinite shape source vector.");
        foreach(var target in description.Targets)
        {
            cancellationToken.ThrowIfCancellationRequested();float weight=selected[target.VariableName]*0.003921568859368563f;
            if(MathF.Abs(weight)<=0.008999999612569809f)continue;active=true;
            foreach(var d in target.Positions)positions[d.Index]=Add(positions[d.Index],d.Delta,weight);
            foreach(var d in target.Normals)normals[d.Index]=Add(normals[d.Index],d.Delta,weight);
            foreach(var d in target.Tangents){var t=tangents[d.Index];tangents[d.Index]=new(Add(new(t.X,t.Y,t.Z),d.Delta,weight),t.W);}
        }
        if(active)
        {
            for(int i=0;i<normals.Length;i++)normals[i]=Normalize(normals[i]);
            for(int i=0;i<tangents.Length;i++){var t=tangents[i];tangents[i]=new(Normalize(new(t.X,t.Y,t.Z)),t.W);}
        }
        cancellationToken.ThrowIfCancellationRequested();
        return geometry with{Skin=geometry.Skin with{Positions=Array.AsReadOnly(positions),Normals=Array.AsReadOnly(normals)},Tangents=Array.AsReadOnly(tangents)};
    }
    private static Vector3 Add(Vector3 value,Vector3 delta,float weight)=>new(value.X+delta.X*weight,value.Y+delta.Y*weight,value.Z+delta.Z*weight);
    private static Vector3 Normalize(Vector3 value)
    {
        float length=value.Length();return length<=0.000009999999747378752f?value:value/length;
    }
    private static bool Finite(Vector3 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z)&&Math.Abs(v.X)<=1e6&&Math.Abs(v.Y)<=1e6&&Math.Abs(v.Z)<=1e6;
    private static void Check(IReadOnlyList<SkeletalBlendDelta> deltas,int count)
    {
        if(deltas.Count>1_048_576)throw new InvalidDataException("Shape delta budget.");
        foreach(var d in deltas)if(d.Index>=count||!float.IsFinite(d.Delta.X)||!float.IsFinite(d.Delta.Y)||!float.IsFinite(d.Delta.Z)||Math.Abs(d.Delta.X)>1e6||Math.Abs(d.Delta.Y)>1e6||Math.Abs(d.Delta.Z)>1e6)
            throw new InvalidDataException("Invalid shape sparse delta.");
    }
}
