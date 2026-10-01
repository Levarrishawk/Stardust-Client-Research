using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class TriangleSphereScenarios
{
    internal static void Run()
    {
        var triangle=new CollisionTriangle(9,new(0,0,0),new(2,0,0),new(0,2,0));var collider=new StaticTriangleSphereCollider(new[]{triangle});
        var face=collider.Sweep(new(.5f,.5f,2),new(.5f,.5f,-2),.5f);
        Check(face,TriangleSphereContactStatus.Blocked,.375,9,Vector3.UnitZ);
        Check(collider.Sweep(new(.5f,-2,0),new(.5f,1,0),.5f),TriangleSphereContactStatus.Blocked,.5,9,-Vector3.UnitY);
        double vertex=(2-.5/Math.Sqrt(2))/1.75;
        Check(collider.Sweep(new(-2,-2,0),new(-.25f,-.25f,0),.5f),TriangleSphereContactStatus.Blocked,vertex,9,Vector3.Normalize(new(-1,-1,0)));
        if(collider.Sweep(new(-.4f,-.4f,0),new(-.4f,-.4f,0),.5f).Status!=TriangleSphereContactStatus.Clear||new StaticTriangleBoxCollider(new[]{triangle}).Sweep(new(-.4f,-.4f,0),new(-.4f,-.4f,0),new(.5f)).Status==TriangleBoxContactStatus.Clear)
            throw new Exception("Sphere rounded corner collapsed to cube expansion.");
        Check(collider.Sweep(new(-1,-.5f,0),new(3,-.5f,0),.5f),TriangleSphereContactStatus.Blocked,.25,9,-Vector3.UnitY);
        if(collider.Sweep(new(1,-.5f,0),new(3,-.5f,0),.5f,true).Status!=TriangleSphereContactStatus.Clear||collider.Sweep(new(.5f,.5f,.5f),new(.5f,.5f,2),.5f,true).Status!=TriangleSphereContactStatus.Clear)
            throw new Exception("Sphere tangent/outward touch did not clear.");
        Check(collider.Sweep(new(.5f,.5f,.25f),new(.5f,.5f,2),.5f,true),TriangleSphereContactStatus.InitialOverlap,0,9,Vector3.UnitZ);
        var thin=new StaticTriangleSphereCollider(new[]{new CollisionTriangle(2,Vector3.Zero,new(1000,0,0),new(1000,.00001f,0))});
        if(Math.Abs(thin.Sweep(new(750,.000005f,1),new(750,.000005f,-1),.1f).Parameter-(1-(double).1f)/2)>1e-10)throw new Exception("Skinny face contact lost to projection cancellation.");
        var tied=new StaticTriangleSphereCollider(new[]{triangle,triangle with{Id=3}});if(tied.Sweep(new(.5f,.5f,2),new(.5f,.5f,-2),.5f).TriangleId!=3)throw new Exception("Sphere equal-time ID ordering.");
        var mutable=new[]{triangle};var owned=new StaticTriangleSphereCollider(mutable);mutable[0]=triangle with{A=new(float.NaN,0,0)};
        if(owned.Sweep(new(.5f,.5f,2),new(.5f,.5f,-2),.5f)!=face)throw new Exception("Sphere collider borrowed source geometry.");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new StaticTriangleSphereCollider(new[]{triangle,triangle}));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new StaticTriangleSphereCollider(mutable));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new StaticTriangleSphereCollider(new[]{triangle with{C=triangle.B}}));
        HostAccountingScenarios.Reject<ArgumentException>(()=>collider.Sweep(Vector3.Zero,Vector3.One,0));
        HostAccountingScenarios.Reject<ArgumentException>(()=>collider.Sweep(Vector3.Zero,new(101,0,0),1));
        HostAccountingScenarios.Reject<ArgumentException>(()=>collider.Sweep(new(float.NaN,0,0),Vector3.Zero,1));
        var random=new Random(2026100110);var many=Enumerable.Range(0,128).Select(i=>
        {
            Vector3 a=new((float)random.NextDouble()*30-15,(float)random.NextDouble()*30-15,(float)random.NextDouble()*30-15);
            return new CollisionTriangle(i,a,a+new Vector3(1,.3f,.1f),a+new Vector3(.2f,1,.4f));
        }).ToArray();var bounds=new StaticTriangleSphereCollider(many);var exhaustive=new StaticTriangleSphereCollider(many,useBounds:false);long candidates=0;int contacts=0;
        for(int i=0;i<512;i++)
        {
            Vector3 a=new((float)random.NextDouble()*30-15,(float)random.NextDouble()*30-15,(float)random.NextDouble()*30-15),b=a+new Vector3((float)random.NextDouble()*6-3,(float)random.NextDouble()*6-3,(float)random.NextDouble()*6-3);
            float radius=.1f+(float)random.NextDouble();var hit=bounds.SweepWithStatistics(a,b,radius,true);var expected=exhaustive.Sweep(a,b,radius,true);
            if(hit.Contact!=expected)throw new Exception("Sphere conservative bounds changed exhaustive contact.");
            candidates+=hit.Statistics.CandidateTriangles;if(hit.Contact.Status!=TriangleSphereContactStatus.Clear)contacts++;
        }
        Console.WriteLine($"PASS: continuous sphere face/edge/vertex/grazing/overlap/retreat, rounded corners, skinny faces, ownership and 512 exact bounds/exhaustive queries contacts={contacts} candidates={candidates}/65536");
    }
    private static void Check(TriangleSphereContact hit,TriangleSphereContactStatus status,double parameter,int id,Vector3 normal)
    {if(hit.Status!=status||Math.Abs(hit.Parameter-parameter)>1e-10||hit.TriangleId!=id||Vector3.Distance(hit.Normal,normal)>1e-6)throw new Exception("Controlled sphere feature contact mismatch: "+hit);}
    internal static void ProbeOptional(string[] args)
    {
        if(args.Length!=2||new FileInfo(args[1]).Length>1048576)throw new ArgumentException("Bounded sphere fixture file required.");
        using var document=JsonDocument.Parse(File.ReadAllBytes(args[1]));if(document.RootElement.GetArrayLength()>4096)throw new InvalidDataException("Sphere fixture query budget.");
        Vector3 V(JsonElement a){if(a.GetArrayLength()!=3)throw new InvalidDataException("Sphere fixture vector width.");return new(a[0].GetSingle(),a[1].GetSingle(),a[2].GetSingle());}
        foreach(var row in document.RootElement.EnumerateArray())
        {
            var points=row.GetProperty("triangle");if(points.GetArrayLength()!=3)throw new InvalidDataException("Sphere fixture triangle width.");
            var collider=new StaticTriangleSphereCollider(new[]{new CollisionTriangle(0,V(points[0]),V(points[1]),V(points[2]))});
            var hit=collider.Sweep(V(row.GetProperty("start")),V(row.GetProperty("end")),row.GetProperty("radius").GetSingle());
            Console.WriteLine("triangle_sphere_result="+JsonSerializer.Serialize(new{id=row.GetProperty("id").GetInt32(),status=hit.Status.ToString(),parameter=hit.Parameter,triangle=hit.TriangleId,normal=InteriorDoorScenarios.V(hit.Normal)}));
        }
    }
}
