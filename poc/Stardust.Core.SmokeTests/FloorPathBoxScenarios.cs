using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class FloorPathBoxScenarios
{
    internal static void Run()
    {
        var walker=new SingleCellFloorWalker(FloorPathScenarios.Fixture());Vector3 start=new(.25f,0,.25f),end=new(1.75f,0,1.75f),half=new(.05f,.1f,.05f);
        var path=walker.Trace(0,start,end);var low=new PortalBarrierCollider(new(.95f,0,.95f),new(1.05f,.3f,1.05f),AppearanceTransform.Identity);
        var hit=FloorPathBoxSweep.Sweep(path,low,half,false);
        Require(hit.Status==PortalBarrierContactStatus.Blocked && hit.SegmentIndex==0 && Math.Abs(hit.Parameter-(.9-.25)/1.5)<1e-6 && hit.Center.Y==half.Y,"Crease obstacle hits actual flat approach");
        Require(low.Sweep(start+new Vector3(0,half.Y,0),path.Result.Position+new Vector3(0,half.Y,0),half,false).Status==PortalBarrierContactStatus.Clear,"Endpoint chord misses crease obstacle");
        var overhead=new PortalBarrierCollider(new(.95f,.6f,.95f),new(1.05f,.9f,1.05f),AppearanceTransform.Identity);
        Require(FloorPathBoxSweep.Sweep(path,overhead,half,false).Status==PortalBarrierContactStatus.Clear && overhead.Sweep(start+new Vector3(0,half.Y,0),path.Result.Position+new Vector3(0,half.Y,0),half,false).Status==PortalBarrierContactStatus.Blocked,"Endpoint chord manufactures overhead contact");
        var ramp=new PortalBarrierCollider(new(1.4f,.7f,1.4f),new(1.5f,1,1.5f),AppearanceTransform.Identity);
        hit=FloorPathBoxSweep.Sweep(path,ramp,half,false);Require(hit.Status==PortalBarrierContactStatus.Blocked && hit.SegmentIndex==1 && Math.Abs(hit.Parameter-(1.35-.25)/1.5)<1e-6,"Ramp contact uses global request parameter");
        Require(FloorPathBoxSweep.Sweep(path,ramp,half,null).Status==PortalBarrierContactStatus.PermissionUnknown,"Unknown passage remains blocked");
        Require(FloorPathBoxSweep.Sweep(path,ramp,half,true).Status==PortalBarrierContactStatus.Clear,"Allowed passage skips box");
        var reverse=walker.Trace(1,path.Result.Position,start);Require(FloorPathBoxSweep.Sweep(reverse,ramp,half,false).SegmentIndex==0,"Reverse ramp collision");
        var overlap=new PortalBarrierCollider(new(.2f,0,.2f),new(.3f,.3f,.3f),AppearanceTransform.Identity);
        Require(FloorPathBoxSweep.Sweep(path,overlap,half,false).Status==PortalBarrierContactStatus.InitialOverlap,"Initial overlap explicit");
        var retreat=walker.Trace(0,start,new(.1f,0,.1f));var touching=new PortalBarrierCollider(new(.375f,0,.125f),new(.625f,.5f,.375f),AppearanceTransform.Identity);Vector3 h=new(.125f);
        Require(FloorPathBoxSweep.Sweep(retreat,touching,h,false).Status==PortalBarrierContactStatus.InitialOverlap && FloorPathBoxSweep.Sweep(retreat,touching,h,false,true).Status==PortalBarrierContactStatus.Clear,"Touching retreat optional");
        var prefix=walker.Trace(0,start,end,0);Require(FloorPathBoxSweep.Sweep(prefix,ramp,half,false).Status==PortalBarrierContactStatus.Clear,"No sweep beyond accepted prefix");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>FloorPathBoxSweep.Sweep(path,ramp,new(float.NaN,0,0),true));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>FloorPathBoxSweep.Sweep(path with{Segments=new[]{path.Segments[0],path.Segments[1] with{Start=new FloorPathPoint(8,8,8)}}},ramp,half,true));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>FloorPathBoxSweep.Sweep(path with{Segments=Enumerable.Repeat(path.Segments[0],4098).ToArray()},ramp,half,false));
        Console.WriteLine("PASS: piecewise box crease/chord disagreement, ramp/reverse/global contact, permission, overlap/retreat, prefixes and malformed path gates");
    }
    internal static void ProbeOptional(string[] args,bool withLanding=false)
    {
        if(args.Length!=4 || args[2]!="--expected-sha256")throw new ArgumentException("Pinned box fixture required.");
        byte[] bytes=File.ReadAllBytes(args[1]);if(bytes.Length>4_000_000 || Convert.ToHexString(SHA256.HashData(bytes))!=args[3])throw new InvalidDataException("Box fixture identity/budget.");
        using JsonDocument doc=JsonDocument.Parse(bytes);if(doc.RootElement.GetArrayLength()>10000)throw new InvalidDataException("Box fixture count.");
        Vector3 V(JsonElement p)=>new(p[0].GetSingle(),p[1].GetSingle(),p[2].GetSingle());
        var walker=new SingleCellFloorWalker(FloorPathScenarios.Fixture());int index=0;
        foreach(var row in doc.RootElement.EnumerateArray())
        {
            Vector3 start=V(row.GetProperty("start")),end=V(row.GetProperty("end"));var path=walker.Trace(row.GetProperty("triangle").GetInt32(),start,end);
            Require(path.Result.Status==FloorWalkStatus.Accepted,"Generated accepted path");
            var p=row.GetProperty("pose");var pose=new AppearanceTransform(V(p[0]),V(p[1]),V(p[2]),V(p[3]));
            var box=new PortalBarrierCollider(V(row.GetProperty("minimum")),V(row.GetProperty("maximum")),pose);
            JsonElement permission=row.GetProperty("allowed");bool? allowed=permission.ValueKind==JsonValueKind.Null?null:permission.GetBoolean();
            var hit=FloorPathBoxSweep.Sweep(path,box,V(row.GetProperty("half")),allowed,row.GetProperty("separating").GetBoolean());
            Console.WriteLine("path_box="+JsonSerializer.Serialize(new{index=index++,status=hit.Status.ToString(),parameter=hit.Parameter,segment=hit.SegmentIndex,center=new[]{hit.Center.X,hit.Center.Y,hit.Center.Z},normal=InteriorDoorScenarios.V(hit.Normal)}));
            if(withLanding)
            {
                var result=new SingleCellFloorBoxWalker(FloorPathScenarios.Fixture(),new[]{box}).Walk(row.GetProperty("triangle").GetInt32(),start,end,V(row.GetProperty("half")),_=>allowed,row.GetProperty("separating").GetBoolean());
                var resumed=walker.Walk(result.Floor.TriangleIndex,result.Floor.Position,result.Floor.Position);
                Console.WriteLine("floor_box_walk="+JsonSerializer.Serialize(new{index=index-1,status=result.Status.ToString(),floor_status=result.Floor.Status.ToString(),parameter=result.Floor.Parameter,triangle=result.Floor.TriangleIndex,position=InteriorDoorScenarios.V(result.Floor.Position),resume_status=resumed.Status.ToString()}));
            }
        }
    }
    private static void Require(bool value,string label){if(!value)throw new Exception("Floor path box: "+label);}
}
