using System.Numerics;

namespace Stardust.Core.Assets;

/// <summary>Already mask/visibility-selected runtime input; caller owns the original registration order.</summary>
public sealed record LegacyLightInput(int Key,int RuntimeType,Vector3 Diffuse,Vector3 Specular,float DiffuseScale=1,float SpecularScale=1);
public sealed record LegacyLightSlots(Vector3 Ambient,LegacyLightInput? SpecularDirectional,IReadOnlyList<LegacyLightInput> DiffuseDirectional,LegacyLightInput? SpecularPoint,IReadOnlyList<LegacyLightInput> DiffusePoint,bool Scaled);

public static class LegacyLightSelector
{
    public const int MaximumInputs=32;
    /// <summary>Original cached absolute-RGB intensity coefficients, followed by one F32 store.</summary>
    public static float Score(Vector3 rgb)
    {
        Validate(rgb);return (float)(Math.Abs((double)rgb.Z)*.10999999940395355+Math.Abs((double)rgb.Y)*.5899999737739563+Math.Abs((double)rgb.X)*.30000001192092896);
    }
    public static LegacyLightSlots Select(IReadOnlyList<LegacyLightInput> inputs,Vector3 globalAmbient,bool scaled=false)
    {
        ArgumentNullException.ThrowIfNull(inputs);Validate(globalAmbient);if(inputs.Count>MaximumInputs)throw new InvalidDataException("Legacy active-vector budget exceeded.");
        var keys=new HashSet<int>();var directional=new List<LegacyLightInput>();var point=new List<LegacyLightInput>();var ambient=globalAmbient;
        foreach(var row in inputs)
        {
            if(row is null||!keys.Add(row.Key)||row.RuntimeType<0||row.RuntimeType>255||!float.IsFinite(row.DiffuseScale)||!float.IsFinite(row.SpecularScale)||Math.Abs(row.DiffuseScale)>1e6||Math.Abs(row.SpecularScale)>1e6)throw new InvalidDataException("Invalid legacy active light input.");
            Validate(row.Diffuse);Validate(row.Specular);var selected=scaled?row with{Diffuse=row.Diffuse*row.DiffuseScale,Specular=row.Specular*row.SpecularScale}:row;Validate(selected.Diffuse);Validate(selected.Specular);
            if(row.RuntimeType==0){ambient+=selected.Diffuse;Validate(ambient);}
            else if(row.RuntimeType==1)directional.Add(selected);
            else if(row.RuntimeType==3)point.Add(selected);
            // Original classifier ignores other type codes, including2/4.
        }
        (LegacyLightInput?,IReadOnlyList<LegacyLightInput>) Partition(List<LegacyLightInput> rows,int diffuseCount)
        {
            LegacyLightInput? first=null;var overflow=new List<LegacyLightInput>();
            foreach(var candidate in rows){if(first is null)first=candidate;else if(Score(candidate.Specular)>Score(first.Specular)){overflow.Add(first);first=candidate;}else overflow.Add(candidate);}
            var rest=overflow.OrderByDescending(r=>Score(r.Diffuse)).Take(diffuseCount).ToArray();
            return(first,Array.AsReadOnly(rest));
        }
        var d=Partition(directional,2);var p=Partition(point,4);return new(ambient,d.Item1,d.Item2,p.Item1,p.Item2,scaled);
    }
    /// <summary>Authored-order cell fixture, independent of unknown live visibility/registration ordering.</summary>
    public static IReadOnlyList<LegacyLightInput> CellInputs(IReadOnlyList<PortalCellLight> source,bool precalculated)
    {
        ArgumentNullException.ThrowIfNull(source);if(source.Count>MaximumInputs)throw new InvalidDataException("Cell input budget exceeded.");
        var result=new List<LegacyLightInput>();for(int i=0;i<source.Count;i++)
        {
            var row=source[i]??throw new InvalidDataException("Null cell light input.");var d=new Vector3(row.DiffuseRgba.X,row.DiffuseRgba.Y,row.DiffuseRgba.Z);var s=new Vector3(row.SpecularRgba.X,row.SpecularRgba.Y,row.SpecularRgba.Z);
            if(!precalculated)result.Add(new(i,row.RuntimeLightType,d,row.Kind==PortalCellLightKind.Ambient?Vector3.Zero:s));
            else if(row.HasSpecularRgb)result.Add(new(i,row.RuntimeLightType,Vector3.Zero,row.Kind==PortalCellLightKind.Ambient?Vector3.Zero:s));
        }
        return Array.AsReadOnly(result.ToArray());
    }
    private static void Validate(Vector3 value){if(!float.IsFinite(value.X)||!float.IsFinite(value.Y)||!float.IsFinite(value.Z)||Math.Abs(value.X)>1e6||Math.Abs(value.Y)>1e6||Math.Abs(value.Z)>1e6)throw new InvalidDataException("Bounded finite legacy light vector required.");}
}
