using System.Buffers.Binary;
using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record AppearanceExtent(string Kind,string Version,Vector3 Center,float Radius,Vector3? Minimum=null,Vector3? Maximum=null);

/// <summary>Owned bounded primary/secondary APPR extents. Unsupported collision families remain explicit.</summary>
public static class AppearanceExtentDecoder
{
    public static AppearanceExtent? DecodeBase(IffNode appearance)
    {
        Require(appearance.IsForm("APPR") && appearance.Children.Count==1 && appearance.Children[0].IsForm("0003"),"Unsupported appearance base version.");
        IReadOnlyList<IffNode> children=appearance.Children[0].Children;
        if(children.Count==0)return null;
        return DecodeExtent(children[0]);
    }
    /// <summary>Authored secondary extent, not the selected runtime actor collider or primary fallback.</summary>
    public static AppearanceExtent? DecodeCollisionBase(IffNode appearance)
    {
        Require(appearance.IsForm("APPR") && appearance.Children.Count==1 && appearance.Children[0].IsForm("0003"),"Unsupported appearance base version.");
        IReadOnlyList<IffNode> children=appearance.Children[0].Children;
        Require(children.Count>=2,"Missing authored secondary appearance extent.");
        return DecodeExtent(children[1]);
    }
    /// <summary>NULL, EXSP/0001 or EXBX/0001 only; no mesh/composite extent substitution.</summary>
    public static AppearanceExtent? DecodeExtent(IffNode extent)
    {
        if(extent.IsForm("NULL")){Require(extent.Children.Count==0,"Invalid NULL extent.");return null;}
        if(extent.IsForm("EXSP"))return Sphere(extent);
        Require(extent.IsForm("EXBX") && extent.Children.Count==1 && extent.Children[0].IsForm("0001"),"Unsupported extent family/version.");
        IReadOnlyList<IffNode> box=extent.Children[0].Children;
        Require(box.Count==2 && box[0].IsForm("EXSP") && box[1].Tag=="BOX " && box[1].Payload.Length==24,"Invalid box extent.");
        AppearanceExtent sphere=Sphere(box[0]);ReadOnlySpan<byte> data=box[1].Payload.Span;
        // Authored BOX stores maximum first, minimum second; the sphere center is retained independently.
        Vector3 maximum=Vector(data),minimum=Vector(data[12..]);
        Require(maximum.X>=minimum.X && maximum.Y>=minimum.Y && maximum.Z>=minimum.Z,"Reversed box bounds are outside the valid extent slice.");
        return new("EXBX","0001",sphere.Center,sphere.Radius,minimum,maximum);
    }
    private static AppearanceExtent Sphere(IffNode sphere)
    {
        Require(sphere.IsForm("EXSP") && sphere.Children.Count==1 && sphere.Children[0].IsForm("0001"),"Unsupported sphere extent version.");
        IReadOnlyList<IffNode> children=sphere.Children[0].Children;
        Require(children.Count==1 && children[0].Tag=="SPHR" && children[0].Payload.Length==16,"Invalid sphere extent.");
        ReadOnlySpan<byte> data=children[0].Payload.Span;Vector3 center=Vector(data);float radius=Float(data[12..]);
        Require(radius>=0,"Negative sphere radius is outside the valid extent slice.");
        return new("EXSP","0001",center,radius);
    }
    private static Vector3 Vector(ReadOnlySpan<byte> data)=>new(Float(data),Float(data[4..]),Float(data[8..]));
    private static float Float(ReadOnlySpan<byte> data)
    {float value=BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(data));Require(float.IsFinite(value),"Nonfinite appearance extent.");return value;}
    private static void Require(bool condition,string message){if(!condition)throw new InvalidDataException(message);}
}

public static class DetailAppearanceDistance
{
    /// <summary>PIVT=1 uses translation; PIVT=0/absent uses the primary extent sphere center (zero for no extent).</summary>
    public static double Measure(byte? pivot,AppearanceExtent? extent,AppearanceTransform world,Vector3 cameraWorld)
    {
        if(pivot is >1)throw new ArgumentOutOfRangeException(nameof(pivot));
        Vector3 origin=pivot==1?world.Translation:world.TransformPoint(extent?.Center??Vector3.Zero);
        if(!Finite(cameraWorld) || !Finite(origin))throw new ArgumentOutOfRangeException(nameof(cameraWorld),"Camera and transformed origin must be finite.");
        double x=(double)cameraWorld.X-origin.X,y=(double)cameraWorld.Y-origin.Y,z=(double)cameraWorld.Z-origin.Z;
        return Math.Sqrt(x*x+y*y+z*z);
    }
    private static bool Finite(Vector3 value)=>float.IsFinite(value.X)&&float.IsFinite(value.Y)&&float.IsFinite(value.Z);
}
