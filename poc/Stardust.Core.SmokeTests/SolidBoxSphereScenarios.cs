using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class SolidBoxSphereScenarios
{
    internal static void Run()
    {
        var box=new SolidBoxSphereCollider(new(Vector3.Zero,Vector3.One,AppearanceTransform.Identity));
        void Check(Vector3 a,Vector3 b,float r,double expected,Vector3 normal)
        {
            var hit=box.Sweep(a,b,r);
            if(hit.Status!=PortalBarrierContactStatus.Blocked||Math.Abs(hit.Parameter-expected)>1e-10||Vector3.Distance(hit.Normal,normal)>1e-6)throw new Exception("Solid sphere feature mismatch: "+hit);
        }
        Check(new(-2,.5f,.5f),new(2,.5f,.5f),.5f,.375,-Vector3.UnitX);
        Check(new(-2,-2,.5f),new(0,0,.5f),.5f,(2-.5/Math.Sqrt(2))/2,Vector3.Normalize(new(-1,-1,0)));
        Check(new(-2,-2,-2),Vector3.Zero,.5f,(2-.5/Math.Sqrt(3))/2,Vector3.Normalize(new(-1,-1,-1)));
        Check(new(-1,-.5f,.5f),new(2,-.5f,.5f),.5f,1d/3,-Vector3.UnitY);
        if(box.Sweep(new(-.4f,-.4f,.5f),new(-.4f,-.4f,.5f),.5f).Status!=PortalBarrierContactStatus.Clear)throw new Exception("Solid sphere rounded corner collapsed to expanded cube.");
        if(box.Sweep(new(.5f),new(2),.1f,false,true).Status!=PortalBarrierContactStatus.InitialOverlap)throw new Exception("Solid interior escaped hollow surface approximation.");
        if(box.Sweep(new(-.5f,.5f,.5f),new(-1,.5f,.5f),.5f,false,true).Status!=PortalBarrierContactStatus.Clear||
            box.Sweep(new(-.5f,.5f,.5f),new(-.5f,2,.5f),.5f,false,true).Status!=PortalBarrierContactStatus.Clear)throw new Exception("Solid sphere outward/tangent touch.");
        if(box.Sweep(new(-2,.5f,.5f),new(2,.5f,.5f),.5f,null).Status!=PortalBarrierContactStatus.PermissionUnknown||
            box.Sweep(new(.5f),new(.5f),.5f,true).Status!=PortalBarrierContactStatus.Clear)throw new Exception("Solid sphere permission state.");
        var plane=new SolidBoxSphereCollider(new(Vector3.Zero,new(0,1,1),AppearanceTransform.Identity));
        if(Math.Abs(plane.Sweep(new(-2,.5f,.5f),new(2,.5f,.5f),.5f).Parameter-.375)>1e-10)throw new Exception("Zero-thickness sphere box.");
        AppearanceTransform pose=new(new(0,0,1),Vector3.UnitY,new(-1,0,0),new(30,4,-11));
        var rotated=new SolidBoxSphereCollider(new(Vector3.Zero,Vector3.One,pose));
        var result=rotated.Sweep(pose.TransformPoint(new(-2,.5f,.5f)),pose.TransformPoint(new(2,.5f,.5f)),.5f);
        if(result.Parameter!=.375||result.Normal!=pose.TransformDirection(-Vector3.UnitX))throw new Exception("Solid sphere rotated owner.");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>box.Sweep(Vector3.Zero,Vector3.One,0,true));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>box.Sweep(Vector3.Zero,new(101,0,0),1));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>box.Sweep(new(float.NaN),Vector3.Zero,1));
        FloorWalkPath Path(params FloorWalkSegment[] segments)=>new(new(FloorWalkStatus.Accepted,segments[^1].TriangleIndex,segments[^1].End.ToVector3(),segments[^1].EndParameter,-1,-1,segments.Length-1),segments);
        var path=Path(new(0,0,.25,new(-3,0,.5),new(-1,0,.5)),new(1,.25,1,new(-1,0,.5),new(3,0,.5)));
        var contact=FloorPathSolidBoxSphereSweep.Sweep(path,box,new(0,.5f,0),.5f,false);
        if(contact.Parameter!=.34375||contact.SegmentIndex!=1||contact.Center!=new FloorPathPoint(-.5,.5,.5))throw new Exception("Solid sphere floor path progress/offset.");
        var bad=Path(new(0,0,.25,new(-3,0,.5),new(-1,0,.5)),new(1,.25,1,new(-1,0,.5),new(double.NaN,0,.5)));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>FloorPathSolidBoxSphereSweep.Sweep(bad,box,Vector3.UnitY,.5f,true));
        bad=Path(new(0,0,.25,new(-3,0,.5),new(-1,0,.5)),new(1,.25,1,new(-.9,0,.5),new(3,0,.5)));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>FloorPathSolidBoxSphereSweep.Sweep(bad,box,Vector3.UnitY,.5f,false));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>FloorPathSolidBoxSphereSweep.Sweep(path,box,Vector3.UnitY,0,true));
        Console.WriteLine("PASS: solid sphere precise floor path progress and full validation before permission shortcuts");
        Console.WriteLine("PASS: solid box sphere faces/edges/corners/grazing, interior overlap, retreat, permissions, zero thickness and owner transforms");
    }
    internal static void ProbeOptional(string[] args)
    {
        if(args.Length!=2||new FileInfo(args[1]).Length>1048576)throw new ArgumentException("Bounded solid sphere fixture required.");
        using var document=JsonDocument.Parse(File.ReadAllBytes(args[1]));if(document.RootElement.GetArrayLength()>4096)throw new InvalidDataException("Solid sphere query budget.");
        Vector3 V(JsonElement p){if(p.GetArrayLength()!=3)throw new InvalidDataException("Solid sphere vector width.");return new(p[0].GetSingle(),p[1].GetSingle(),p[2].GetSingle());}
        foreach(var row in document.RootElement.EnumerateArray())
        {
            var p=row.GetProperty("pose");if(p.GetArrayLength()!=4)throw new InvalidDataException("Solid sphere pose width.");
            var collider=new SolidBoxSphereCollider(new(V(row.GetProperty("minimum")),V(row.GetProperty("maximum")),new(V(p[0]),V(p[1]),V(p[2]),V(p[3]))));
            var hit=collider.Sweep(V(row.GetProperty("start")),V(row.GetProperty("end")),row.GetProperty("radius").GetSingle());
            Console.WriteLine("solid_sphere_result="+JsonSerializer.Serialize(new{id=row.GetProperty("id").GetInt32(),status=hit.Status.ToString(),parameter=hit.Parameter,normal=InteriorDoorScenarios.V(hit.Normal)}));
        }
    }
}
