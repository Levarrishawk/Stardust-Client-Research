using System.Numerics;

namespace Stardust.Core.Assets;

/// <summary>Explicit diagnostic ambient plus directional diffuse slice; original light selection/specular remain separate.</summary>
public static class SkeletalDiffuseEvaluator
{
    public static Vector3 Evaluate(Vector3 sampledAlbedo,Vector3 normalWorld,Vector3 directionToLightWorld,Vector3 ambient,Vector3 directional)
    {
        bool Color(Vector3 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z)&&v.X>=0&&v.X<=1&&v.Y>=0&&v.Y<=1&&v.Z>=0&&v.Z<=1;
        bool Unit(Vector3 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z)&&Math.Abs(v.LengthSquared()-1)<1e-4;
        if(!Color(sampledAlbedo)||!Color(ambient)||!Color(directional)||!Unit(normalWorld)||!Unit(directionToLightWorld))throw new InvalidDataException("Invalid explicit diffuse sample/light.");
        float intensity=Math.Clamp(Vector3.Dot(normalWorld,directionToLightWorld),0,1);
        var light=Vector3.Clamp(ambient+directional*intensity,Vector3.Zero,Vector3.One);return sampledAlbedo*light;
    }
}
