using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class PortalCameraCellScenarios
{
    internal static void Run()
    {
        var query=new PortalCameraCellQuery(PortalVisibilityScenarios.Fixture(),new[]{0,1,2});
        void Located(CameraCellResult result,int cell,int crossings)
        {if(result.Status!=CameraCellStatus.Located||result.CellIndex!=cell||result.Crossings.Count!=crossings)throw new Exception("Camera cell tracking mismatch.");}
        Located(query.Locate(0,Vector3.Zero,new(0,0,1)),0,0);Located(query.Locate(0,Vector3.Zero,new(0,0,3)),1,1);
        Located(query.Locate(0,Vector3.Zero,new(0,0,5)),2,2);Located(query.Locate(2,new(0,0,5),Vector3.Zero),0,2);
        var owner=new AppearanceTransform(new(0,0,1),Vector3.UnitY,new(-1,0,0),new(20,4,-9));
        Located(query.LocateWorld(0,owner.TransformPoint(Vector3.Zero),owner.TransformPoint(new(0,0,5)),owner),2,2);
        Located(query.Locate(0,Vector3.Zero,new(10,0,5)),0,0);Located(query.Locate(0,Vector3.Zero,Vector3.Zero),0,0);
        if(query.Locate(0,Vector3.Zero,new(0,0,2)).Status!=CameraCellStatus.AmbiguousBoundary||query.Locate(0,new(0,0,2),new(0,0,3)).Status!=CameraCellStatus.AmbiguousBoundary)throw new Exception("Camera boundary must fail open.");
        if(new PortalCameraCellQuery(PortalVisibilityScenarios.Fixture(),new[]{0,1}).Locate(0,Vector3.Zero,new(0,0,5)).Status!=CameraCellStatus.UnloadedCell)throw new Exception("Unloaded camera room must fail open.");
        if(new PortalCameraCellQuery(PortalVisibilityScenarios.Fixture(),new[]{0,1,2},maximumCrossings:1).Locate(0,Vector3.Zero,new(0,0,5)).Status!=CameraCellStatus.CrossingBudget)throw new Exception("Camera cell budget must fail open.");
        var broken=PortalVisibilityScenarios.Fixture();broken=broken with{Cells=broken.Cells.Select((c,i)=>i==1?c with{Portals=new[]{c.Portals[1]}}:c).ToArray()};
        if(new PortalCameraCellQuery(broken,new[]{0,1,2}).Locate(0,Vector3.Zero,new(0,0,3)).Status!=CameraCellStatus.UnpairedPortal)throw new Exception("Unpaired camera portal must fail open.");
        var mutable=PortalVisibilityScenarios.Fixture();var selection=new[]{0,1,2};query=new(mutable,selection);Array.Clear((Vector3[])mutable.Geometry[0].Vertices);selection[0]=2;Located(query.Locate(0,Vector3.Zero,new(0,0,5)),2,2);
        HostAccountingScenarios.Reject<ArgumentException>(()=>query.Locate(9,Vector3.Zero,Vector3.Zero));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>query.Locate(0,new(float.NaN,0,0),Vector3.Zero));
        HostAccountingScenarios.Reject<ArgumentException>(()=>new PortalCameraCellQuery(PortalVisibilityScenarios.Fixture(),new[]{0,0}));
        Console.WriteLine("PASS: owned camera-cell portal traversal; forward/reverse/multiple/aperture/zero, ambiguous boundary/unloaded/unpaired/budget fallback, selection/source ownership and negative gates");
    }
    internal static void Probe(InteriorSceneContentBundle content)
    {
        var bundle=content.Scene;var query=new PortalCameraCellQuery(bundle.Layout,content.Rooms!.CellAppearances.Keys.ToArray());int located=0,changed=0,ambiguous=0;
        foreach(var doorway in bundle.Doorways)
        {
            var approach=doorway.ConnectedApproach(stepPolicy:new(UseLocalRayCoverage:true));var from=approach.Start.LocalPosition+Vector3.UnitY*1.7f;var to=approach.End+Vector3.UnitY*1.7f;
            foreach(bool reverse in new[]{false,true})foreach(float fraction in new[]{0f,.25f,.49f,.5f,.51f,.75f,1f})
            {
                int anchorCell=reverse?doorway.TargetCell:doorway.Cell;var anchor=reverse?to:from;var end=Vector3.Lerp(anchor,reverse?from:to,fraction);
                var result=query.Locate(anchorCell,anchor,end);if(result.Status==CameraCellStatus.Located){located++;changed+=result.CellIndex!=anchorCell?1:0;}else if(result.Status==CameraCellStatus.AmbiguousBoundary)ambiguous++;else throw new Exception("Original camera crossing unexpectedly unresolved.");
                Console.WriteLine("original_camera_cell="+JsonSerializer.Serialize(new{anchorCell,anchor=V(anchor),camera=V(end),reverse,fraction,result.Status,result.CellIndex,result.Crossings}));
            }
        }
        if(changed==0||ambiguous==0)throw new Exception("Original camera doorway crossing/boundary not exercised.");
        Console.WriteLine($"PASS: original independent camera cell queries=28 located={located} changed={changed} ambiguous={ambiguous} actor_state_inert=true");
    }
    private static float[] V(Vector3 value)=>new[]{value.X,value.Y,value.Z};
}
