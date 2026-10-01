using System.Numerics;
using Stardust.Core.Assets;

internal static class PortalVisibilityScenarios
{
    internal static PortalLayoutDescription Fixture(bool offset=false,bool cycle=false)
    {
        var basis=PortalLayoutDecoder.Decode(PortalLayoutScenarios.Fixture());
        PortalGeometry Quad(float z,float x=0)=>new(new[]{new Vector3(x-1,-1,z),new Vector3(x+1,-1,z),new Vector3(x+1,1,z),new Vector3(x-1,1,z)},new uint[]{0,1,2,0,2,3},false);
        PortalReference Link(int geometry,int target)=>basis.Cells[0].Portals[0] with{GeometryIndex=geometry,TargetCell=target};
        var cells=new[]{basis.Cells[0] with{Portals=new[]{Link(0,1)}},basis.Cells[1] with{Portals=new[]{Link(0,0),Link(1,2)}},basis.Cells[1] with{Portals=cycle?new[]{Link(1,1),Link(2,0)}:new[]{Link(1,1)}}};
        if(cycle)cells[0]=cells[0] with{Portals=new[]{Link(0,1),Link(2,2)}};
        return basis with{Cells=cells,Geometry=cycle?new[]{Quad(2),Quad(4,offset?3.5f:0),Quad(6)}:new[]{Quad(2),Quad(4,offset?3.5f:0)}};
    }
    internal static void Run()
    {
        var view=new PortalPerspectiveView(Vector3.Zero,Vector3.UnitZ,Vector3.UnitY,MathF.PI/2,1,100);
        void Cells(PortalCellVisibilityResult result,params int[] expected){if(result.BudgetFallback||!result.Cells.SequenceEqual(expected))throw new Exception("Portal visibility mismatch.");}
        var query=new PortalCellVisibilityQuery(Fixture(),new[]{0,1,2});
        Cells(query.Query(0,view),0,1,2);Cells(query.Query(0,view with{Forward=-Vector3.UnitZ}),0);Cells(query.Query(0,view with{Forward=Vector3.UnitX}),0);
        Cells(new PortalCellVisibilityQuery(Fixture(offset:true),new[]{0,1,2}).Query(0,view),0,1);
        Cells(query.Query(0,view with{Eye=new(0,0,2)}),0,1,2);
        Cells(query.Query(2,view with{Eye=new(0,0,5),Forward=-Vector3.UnitZ}),0,1,2);
        Cells(new PortalCellVisibilityQuery(Fixture(cycle:true),new[]{0,1,2}).Query(0,view),0,1,2);
        var fallback=new PortalCellVisibilityQuery(Fixture(),new[]{0,1,2},maximumStates:1).Query(0,view);
        if(!fallback.BudgetFallback||!fallback.Cells.SequenceEqual(new[]{0,1,2}))throw new Exception("Visibility budget hid rooms.");
        if(!new PortalCellVisibilityQuery(Fixture(),new[]{0,1,2},maximumPlanes:6).Query(0,view).BudgetFallback)throw new Exception("Visibility plane budget did not fail open.");
        var mutable=Fixture();query=new(mutable,new[]{0,1,2});((Vector3[])mutable.Geometry[0].Vertices)[0]=new(float.NaN,0,0);((uint[])mutable.Geometry[0].Indices)[0]=99;Cells(query.Query(0,view),0,1,2);
        var selection=new[]{0,1};query=new(Fixture(),selection);selection[1]=2;Cells(query.Query(0,view),0,1);
        HostAccountingScenarios.Reject<ArgumentException>(()=>query.Query(2,view));
        HostAccountingScenarios.Reject<ArgumentException>(()=>query.Query(0,view with{Eye=new(float.NaN,0,0)}));
        HostAccountingScenarios.Reject<ArgumentException>(()=>query.Query(0,view with{Up=Vector3.UnitZ}));
        HostAccountingScenarios.Reject<ArgumentException>(()=>query.Query(0,view with{Aspect=0}));
        HostAccountingScenarios.Reject<ArgumentException>(()=>new PortalCellVisibilityQuery(Fixture(),new[]{0,0}));
        var bad=Fixture();bad=bad with{Geometry=new[]{bad.Geometry[0] with{Indices=new uint[]{0,1,99}},bad.Geometry[1]}};
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new PortalCellVisibilityQuery(bad,new[]{0,1,2}));
        Console.WriteLine("PASS: bounded conservative portal visibility; aperture/behind/side/plane/reverse/cycle, selection ownership, fail-open budget and invalid views");
    }
    internal static void ProbeOptional(string[] args)
    {
        if(args.Length!=8||args[2]!="--configuration"||args[4]!="--member"||args[6]!="--expected-sha256")throw new ArgumentException("Pinned portal visibility probe options.");
        using var config=File.OpenText(args[3]);using var overlay=ConfiguredTreOverlay.Open(args[1],config);
        var input=overlay.Lookup(args[5]);if(input.Payload is null||!Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(input.Payload)).Equals(args[7],StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Visibility input identity mismatch.");
        var layout=PortalLayoutDecoder.Decode(input.Payload);var query=new PortalCellVisibilityQuery(layout,new[]{5,6,7});
        Vector3 Center(int cell,int portal){var geometry=layout.Geometry[layout.Cells[cell].Portals[portal].GeometryIndex];return geometry.Vertices.Aggregate(Vector3.Zero,(sum,v)=>sum+v)/geometry.Vertices.Count;}
        var eyes=new[]{Center(5,1)-Vector3.UnitZ*.8f,(Center(6,0)+Center(6,1))*.5f,Center(7,0)+Vector3.UnitZ*.8f};
        for(int i=0;i<3;i++)foreach(int sign in new[]{1,-1})
        {
            var view=new PortalPerspectiveView(eyes[i],Vector3.UnitZ*sign,Vector3.UnitY,MathF.PI/2,1,100);var result=query.Query(5+i,view);
            Console.WriteLine("portal_visibility="+System.Text.Json.JsonSerializer.Serialize(new{cell=5+i,eye=new[]{view.Eye.X,view.Eye.Y,view.Eye.Z},forward=sign,cells=result.Cells,states=result.TraversalStates,triangles=result.ClippedTriangles,fallback=result.BudgetFallback}));
        }
    }
}
