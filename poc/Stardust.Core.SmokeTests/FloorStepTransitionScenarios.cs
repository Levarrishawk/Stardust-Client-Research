using System.Numerics;
using Stardust.Core.Assets;

internal static class FloorStepTransitionScenarios
{
    internal static void Run()
    {
        var basis=FloorMeshDecoder.Decode(FloorMeshScenarios.Fixture());
        FloorMeshDescription Fixture(float height=.1f,bool down=false)
        {
            Vector3[] vertices={new(0,0,.5f),new(1,0,0),new(1,0,1),new(1,height,0),new(1,height,1),new(2,height,.5f)};
            if(down)vertices=vertices.Select(v=>v with{Y=height-v.Y}).ToArray();
            FloorTriangle Row(int[] indices,int[] neighbors,byte[] flags)=>new(indices,0,neighbors,Vector3.UnitY,flags,0,0,new[]{-1,-1,-1});
            return basis with{Vertices=vertices,Triangles=new[]{Row(new[]{0,1,2},new[]{-1,1,-1},new byte[]{0,1,0}),Row(new[]{2,1,3},new[]{0,-1,2},new byte[]{1,0,1}),Row(new[]{2,3,4},new[]{1,3,-1},new byte[]{1,1,0}),Row(new[]{4,3,5},new[]{2,-1,-1},new byte[]{1,0,0})}};
        }
        FloorStepTransition Probe(FloorMeshDescription floor,FloorStepPolicy? policy=null,float startY=0)
        {
            var desired=new Vector3(1.5f,startY,.5f);var path=new SingleCellFloorWalker(floor).Trace(0,new(.5f,startY,.5f),desired);
            return new FloorStepTransitionQuery(floor).Probe(path,desired,policy??new());
        }
        foreach(bool down in new[]{false,true})
        {
            var floor=Fixture(down:down);var result=Probe(floor,startY:down?.1f:0);
            Require(result.Status==FloorStepStatus.Accepted&&result.TargetTriangle==3&&result.StripTriangles.SequenceEqual(new[]{1,2}),"Reciprocal two-face step proposal");
            Require(Math.Abs(result.TargetContact.Y-result.SourceContact.Y-(down?-.1f:.1f))<1e-7,"Signed rise/drop");
            Require(new SingleCellFloorWalker(floor).Walk(result.TargetTriangle,result.Landing,result.Landing).Status==FloorWalkStatus.Accepted,"Reusable proposed landing");
        }
        Require(Probe(Fixture(.21f)).Status==FloorStepStatus.HeightLimit,"Excessive wall/step height");
        Require(Probe(Fixture(),new(MaximumStripTriangles:1)).Status==FloorStepStatus.StripLimit,"Strip traversal budget");
        var original=Fixture();
        var broken=original with{Triangles=original.Triangles.Select((t,i)=>i==1?t with{Neighbors=new[]{-1,-1,2}}:t).ToArray()};
        Require(Probe(broken).Status==FloorStepStatus.InvalidTopology,"Missing reciprocal source edge");
        var nonSolid=original with{Triangles=original.Triangles.Select((t,i)=>i==2?t with{NonSolidByte=1}:t).ToArray()};
        Require(Probe(nonSolid).Status==FloorStepStatus.NonSolid,"Non-solid strip rejected");
        var portal=original with{Triangles=original.Triangles.Select((t,i)=>i==1?t with{PortalIds=new[]{-1,-1,0}}:t).ToArray()};
        Require(Probe(portal).Status==FloorStepStatus.PortalBoundary,"No unaudited portal in step");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>Probe(original,new(MaximumHeight:double.NaN)));
        var start=new Vector3(.5f,0,.5f);var end=new Vector3(1.5f,0,.5f);var half=new Vector3(.25f,.9f,.25f);
        var clear=new SingleCellFloorStepBoxWalker(original,Array.Empty<PortalBarrierCollider>());
        var moved=clear.Walk(0,start,end,half,_=>throw new Exception("No collider policy expected"));
        Require(moved.Steps==1&&moved.Movement.Status==FloorBoxWalkStatus.Accepted&&moved.Movement.Floor.Position==new Vector3(1.5f,.1f,.5f),"Clear vertical rise and horizontal continuation");
        var ceiling=new PortalBarrierCollider(new(.9f,1.85f,.3f),new(1.1f,2,.7f),AppearanceTransform.Identity);
        var blockedWalker=new SingleCellFloorStepBoxWalker(original,new[]{ceiling});int decisions=0;
        var blocked=blockedWalker.Walk(0,start,end,half,_=>{decisions++;return false;});
        Require(decisions==1&&blocked.Steps==0&&blocked.Diagnostic=="VerticalStepBlocked"&&blocked.Movement.Floor.Position==new Vector3(1,0,.5f),"Low ceiling retains supported source");
        for(int i=0;i<100;i++)
        {
            var repeat=blockedWalker.Walk(blocked.Movement.Floor.TriangleIndex,blocked.Movement.Floor.Position,end,half,_=>false);
            Require(repeat.Steps==0&&repeat.Movement.Floor.Position==blocked.Movement.Floor.Position,"Repeated blocked rise has no drift");
        }
        var retreat=blockedWalker.Walk(blocked.Movement.Floor.TriangleIndex,blocked.Movement.Floor.Position,start,half,_=>false,true);
        Require(retreat.Movement.Status==FloorBoxWalkStatus.Accepted&&retreat.Movement.Floor.Position==start,"Blocked rise can retreat on source floor");
        var permitted=blockedWalker.Walk(0,start,end,half,_=>true);
        Require(permitted.Steps==1&&permitted.Movement.Status==FloorBoxWalkStatus.Accepted,"Explicit collider passability");
        var unknown=blockedWalker.Walk(0,start,end,half,_=>null);
        Require(unknown.Steps==0&&unknown.Movement.Contact?.Status==PortalBarrierContactStatus.PermissionUnknown,"Unknown vertical clearance holds source");
        var budget=clear.Walk(0,start,end,half,_=>true,maximumCrossings:2);
        Require(budget.Steps==0&&budget.Diagnostic=="StepTraversalLimit","Combined transition budget");
        decisions=0;var invalid=blockedWalker.Walk(0,new(-1,0,.5f),end,half,_=>{decisions++;return false;});
        Require(decisions==0&&invalid.Movement.Floor.Status==FloorWalkStatus.InvalidStart,"Invalid source has no callbacks");
        for(int i=0;i<100;i++)
        {
            var reverse=clear.Walk(moved.Movement.Floor.TriangleIndex,moved.Movement.Floor.Position,start,half,_=>true);
            Require(reverse.Steps==1&&reverse.Movement.Status==FloorBoxWalkStatus.Accepted&&reverse.Movement.Floor.Position==start,"Repeated clear reverse drop");
            moved=clear.Walk(reverse.Movement.Floor.TriangleIndex,reverse.Movement.Floor.Position,end,half,_=>true);
            Require(moved.Steps==1&&moved.Movement.Status==FloorBoxWalkStatus.Accepted&&moved.Movement.Floor.Position==new Vector3(1.5f,.1f,.5f),"Repeated clear forward rise");
        }
        var obstacle=new PortalBarrierCollider(new(1.8f,0,.3f),new(2,2,.7f),AppearanceTransform.Identity);
        var later=new SingleCellFloorStepBoxWalker(original,new[]{obstacle}).Walk(0,start,new(1.7f,0,.5f),half,_=>false);
        Require(later.Steps==1&&later.Movement.Status==FloorBoxWalkStatus.BoxStopped&&later.Movement.Floor.TriangleIndex==3&&later.Movement.Floor.Parameter>.87&&later.Movement.Floor.Parameter<.88,"Post-step obstacle owns target and global progress");
        Require(new SingleCellFloorWalker(original).Walk(3,later.Movement.Floor.Position,later.Movement.Floor.Position).Status==FloorWalkStatus.Accepted,"Post-step contact reusable");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new SingleCellFloorStepBoxWalker(original,Array.Empty<PortalBarrierCollider>(),new(MaximumHeight:double.NaN)));
        var branches=original with{Vertices=original.Vertices.Append(new Vector3(2,.1f,0)).ToArray(),Triangles=original.Triangles.Select((t,i)=>i==1?t with{Neighbors=new[]{0,4,2},EdgeFlags=new byte[]{1,1,1}}:t)
            .Append(new FloorTriangle(new[]{3,1,6},4,new[]{1,-1,-1},Vector3.UnitZ,new byte[]{1,0,0},0,0,new[]{-1,-1,-1})).ToArray()};
        Require(Probe(branches).Status==FloorStepStatus.StripLimit,"Full-component policy retains side-wall rejection");
        Require(Probe(branches,new(UseLocalRayCoverage:true)).Status==FloorStepStatus.Accepted,"Local vertical ray excludes distant side wall");
        Require(Probe(original,new(UseLocalRayCoverage:true)).Status==FloorStepStatus.Accepted,"Local ray covers the two-triangle riser");
        FloorMeshDescription HoledStrip()
        {
            var vs=new List<Vector3>();float[] ys={0,.04f,.06f,.1f},zs={0,.4f,.6f,1};
            foreach(float y in ys)foreach(float z in zs)vs.Add(new(1,y,z));
            vs.Add(new(0,0,.5f));vs.Add(new(2,.1f,.5f));var indices=new List<int[]>{new[]{16,2,1}};
            for(int y=0;y<3;y++)for(int z=0;z<3;z++)
            {
                if(y==1&&z==1)continue;int a=y*4+z,b=a+1,c=a+4,d=c+1;
                indices.Add(new[]{a,b,c});indices.Add(new[]{b,d,c});
            }
            indices.Add(new[]{13,14,17});var map=new Dictionary<(int,int),(int Triangle,int Edge)>();
            for(int i=0;i<indices.Count;i++)for(int e=0;e<3;e++)map.Add((indices[i][e],indices[i][(e+1)%3]),(i,e));
            var rows=new List<FloorTriangle>();
            for(int i=0;i<indices.Count;i++)
            {
                int[] neighbors={-1,-1,-1};byte[] flags=new byte[3];
                for(int e=0;e<3;e++)if(map.TryGetValue((indices[i][(e+1)%3],indices[i][e]),out var other)){neighbors[e]=other.Triangle;flags[e]=1;}
                rows.Add(new(indices[i],(uint)i,neighbors,Vector3.UnitY,flags,0,0,new[]{-1,-1,-1}));
            }
            return basis with{Vertices=vs,Triangles=rows};
        }
        Require(Probe(HoledStrip(),new(MaximumStripTriangles:32,UseLocalRayCoverage:true)).Status==FloorStepStatus.NoLanding,"Connected strip cannot route around a gap in the vertical ray");
        Console.WriteLine("PASS: bounded modern reciprocal riser proposals, signed rise/drop, reusable landing and height/topology/portal/solid/resource rejection");
        Console.WriteLine("PASS: one-step box movement, low-ceiling rollback, 100 repeat holds, retreat, unknown/passable clearance and traversal/input guards");
    }
    private static void Require(bool condition,string message){if(!condition)throw new Exception("Floor step proposal: "+message);}
}
