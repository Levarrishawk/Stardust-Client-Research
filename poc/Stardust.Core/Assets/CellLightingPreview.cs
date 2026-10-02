using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record PreviewLight(int SourceIndex,bool Point,Vector3 DirectionOrPosition,Vector3 Diffuse,Vector3 Specular,Vector3 Attenuation);
public sealed record PreviewVertexLighting(Vector3 Diffuse,Vector3 Specular);

/// <summary>Explicit replacement policy: first authored directional is the pixel light; all other lights are vertex inputs.</summary>
public sealed class CellLightingPreview
{
    public const int MaximumLights=8;
    public const float MinimumAttenuationDenominator=.01f;
    public LegacyLightSlots? LegacySelection {get;private set;}
    public bool UsesSourcePointAttenuation {get;private set;}
    public PreviewLight? Primary { get; private set; }
    public IReadOnlyList<PreviewLight> Additional { get; private set; }
    public Vector3 Ambient { get; }
    public int SourceCount { get; private set; }
    public CellLightingPreview(IReadOnlyList<PortalCellLight> source,AppearanceTransform owner,Vector3 ambient)
    {
        ArgumentNullException.ThrowIfNull(source);PortalBarrierCollider.ValidatePose(owner);
        if(source.Count>MaximumLights)throw new InvalidDataException("Cell lighting preview budget exceeded.");
        Validate(ambient);if(Vector3.Min(ambient,Vector3.Zero)!=Vector3.Zero)throw new InvalidDataException("Negative preview ambient.");
        var extra=new List<PreviewLight>();Vector3 sum=ambient;
        for(int i=0;i<source.Count;i++)
        {
            var row=source[i]??throw new InvalidDataException("Null preview light record.");Vector3 diffuse=new(row.DiffuseRgba.X,row.DiffuseRgba.Y,row.DiffuseRgba.Z),specular=new(row.SpecularRgba.X,row.SpecularRgba.Y,row.SpecularRgba.Z);
            Validate(diffuse);Validate(specular);Validate(row.Attenuation);
            if(Vector3.Min(diffuse,Vector3.Zero)!=Vector3.Zero||Vector3.Min(specular,Vector3.Zero)!=Vector3.Zero)throw new InvalidDataException("Negative preview light color.");
            if(row.Kind==PortalCellLightKind.Ambient){sum+=diffuse;continue;}
            if(row.Kind is not (PortalCellLightKind.Directional or PortalCellLightKind.Point))throw new InvalidDataException("Unsupported preview light kind.");
            bool point=row.Kind==PortalCellLightKind.Point;
            var input=point?owner.TransformPoint(row.RuntimeTransform.Translation):Unit(owner.TransformDirection(row.TowardLight));Validate(input);
            if(point&&Vector3.Min(row.Attenuation,Vector3.Zero)!=Vector3.Zero)throw new InvalidDataException("Negative preview attenuation coefficient.");
            var light=new PreviewLight(i,point,input,diffuse,specular,row.Attenuation);
            if(!point&&Primary is null)Primary=light;else extra.Add(light);
        }
        Validate(sum);Ambient=sum;Additional=Array.AsReadOnly(extra.ToArray());SourceCount=source.Count;
    }
    public static CellLightingPreview FromLegacyCell(IReadOnlyList<PortalCellLight> source,AppearanceTransform owner,Vector3 ambient,bool precalculated=false,bool scaleLightColors=true)
    {
        var inputs=LegacyLightSelector.CellInputs(source,precalculated);var slots=LegacyLightSelector.Select(inputs,ambient,scaleLightColors);var selected=new List<LegacyLightInput>();
        if(slots.SpecularDirectional is not null)selected.Add(slots.SpecularDirectional);
        selected.AddRange(slots.DiffuseDirectional.Select(r=>r with{Specular=Vector3.Zero}));
        if(slots.SpecularPoint is not null)selected.Add(slots.SpecularPoint);
        selected.AddRange(slots.DiffusePoint.Select(r=>r with{Specular=Vector3.Zero}));
        var records=selected.Select(r=>source[r.Key] with{DiffuseRgba=new(r.Diffuse,1),SpecularRgba=new(r.Specular,1)}).ToArray();
        var result=new CellLightingPreview(records,owner,slots.Ambient);result.LegacySelection=slots;result.UsesSourcePointAttenuation=true;result.SourceCount=inputs.Count;
        if(result.Primary is not null)result.Primary=result.Primary with{SourceIndex=selected[result.Primary.SourceIndex].Key};
        result.Additional=Array.AsReadOnly(result.Additional.Select(r=>r with{SourceIndex=selected[r.SourceIndex].Key}).ToArray());return result;
    }
    public PreviewVertexLighting EvaluateVertex(Vector3 position,Vector3 normal,Vector3 viewer,Vector3 materialSpecular,float power,bool includePrimary=false)
    {
        Validate(position);Validate(viewer);Validate(materialSpecular);normal=Unit(normal);
        if(Vector3.Min(materialSpecular,Vector3.Zero)!=Vector3.Zero)throw new InvalidDataException("Negative preview material specular.");
        if(!float.IsFinite(power)||power<0||power>1024)throw new ArgumentOutOfRangeException(nameof(power));
        var view=SafeUnit(viewer-position);Vector3 diffuse=Ambient,specular=Vector3.Zero;
        void Accumulate(PreviewLight light)
        {
            var delta=light.Point?light.DirectionOrPosition-position:light.DirectionOrPosition;
            float distance=light.Point?delta.Length():0;var direction=light.Point?SafeUnit(delta):delta;
            float attenuation=1;
            if(light.Point)
            {
                if(UsesSourcePointAttenuation)
                {
                    // Explicit replacement singular policy: omit coincident/zero-denominator points.
                    if(distance<=1e-6f||light.Attenuation.X+light.Attenuation.Y*distance+light.Attenuation.Z*distance*distance<=0)return;
                    attenuation=LegacyPointAttenuation.Evaluate(light.Attenuation,delta.LengthSquared());
                }
                else attenuation=1/Math.Max(MinimumAttenuationDenominator,light.Attenuation.X+light.Attenuation.Y*distance+light.Attenuation.Z*distance*distance);
            }
            float nDotL=Vector3.Dot(normal,direction);diffuse+=light.Diffuse*(Math.Max(0,nDotL)*attenuation);
            if(nDotL>0)specular+=light.Specular*materialSpecular*(MathF.Pow(Math.Clamp(Vector3.Dot(normal,SafeUnit(direction+view)),0,1),power)*attenuation);
        }
        if(includePrimary&&Primary is not null&&LegacySelection is not null)Accumulate(Primary);
        foreach(var light in Additional)Accumulate(light);
        if(includePrimary&&Primary is not null&&LegacySelection is null)Accumulate(Primary);
        Validate(diffuse);Validate(specular);return new(diffuse,specular);
    }
    private static Vector3 SafeUnit(Vector3 value)=>value/Math.Max(value.Length(),1e-6f);
    private static Vector3 Unit(Vector3 value){Validate(value);if(value.LengthSquared()<1e-12f)throw new InvalidDataException("Zero preview direction.");return Vector3.Normalize(value);}
    private static void Validate(Vector3 value){if(!float.IsFinite(value.X)||!float.IsFinite(value.Y)||!float.IsFinite(value.Z)||Math.Abs(value.X)>1e6||Math.Abs(value.Y)>1e6||Math.Abs(value.Z)>1e6)throw new InvalidDataException("Bounded finite preview lighting vector required.");}
}
