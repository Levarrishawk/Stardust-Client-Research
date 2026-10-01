using System.Numerics;
using Stardust.Core.Assets;

internal static class PortalFloorStepScenarios
{
    internal static void Run()
    {
        var layout=PortalLayoutDecoder.Decode(PortalLayoutScenarios.Fixture());var basis=FloorMeshDecoder.Decode(FloorMeshScenarios.Fixture());
        FloorTriangle Row(int[] vertices,int[] neighbors,byte[] flags,int[]? portals=null)=>new(vertices,0,neighbors,Vector3.UnitY,flags,0,0,portals??new[]{-1,-1,-1});
        var source=basis with{Vertices=new Vector3[]{new(0,0,.5f),new(1,0,0),new(1,0,1),new(1,.1f,0),new(1,.1f,1),new(2,.1f,0)},
            Triangles=new[]{Row(new[]{0,1,2},new[]{-1,1,-1},new byte[]{0,1,0}),Row(new[]{2,1,3},new[]{0,-1,2},new byte[]{1,0,1}),Row(new[]{2,3,4},new[]{1,3,-1},new byte[]{1,1,0}),Row(new[]{4,3,5},new[]{2,-1,-1},new byte[]{1,0,0},new[]{-1,-1,0})}};
        var target=basis with{Vertices=new Vector3[]{new(1,.1f,1),new(2,.1f,0),new(2,.1f,1)},Triangles=new[]{Row(new[]{0,1,2},new[]{-1,-1,-1},new byte[3],new[]{0,-1,-1})}};
        var floors=new Dictionary<int,FloorMeshDescription>{{0,source},{1,target}};var half=new Vector3(.25f,.9f,.25f);
        foreach(bool transformed in new[]{false,true})
        {
            AppearanceTransform pose=transformed?new(new(0,0,1),Vector3.UnitY,new(-1,0,0),new(30,4,-11)):AppearanceTransform.Identity;
            var poses=floors.Keys.ToDictionary(c=>c,_=>pose);var query=new PortalFloorStepBoxWalker(layout,floors,poses);
            var start=new CellFloorLocator(0,0,new(.5f,0,.5f));Vector3 end=new(1.75f,.1f,.5f);int permissions=0;
            var forward=query.Walk(start,pose.TransformPoint(end),half,Array.Empty<PortalFloorBox>(),_=>{permissions++;return new(true,true);},true);
            Require(forward.Status==FloorBoxWalkStatus.Accepted&&forward.Steps==1&&forward.Movement.Transfers==1&&forward.Movement.Locator.CellIndex==1&&permissions==1,"Step then transfer; one permission decision");
            permissions=0;var reverse=query.Walk(forward.Movement.Locator,pose.TransformPoint(start.LocalPosition),half,Array.Empty<PortalFloorBox>(),_=>{permissions++;return new(true,true);},true);
            Require(reverse.Status==FloorBoxWalkStatus.Accepted&&reverse.Steps==1&&reverse.Movement.Transfers==1&&reverse.Movement.Locator.CellIndex==0&&permissions==1&&Vector3.Distance(reverse.Movement.WorldPosition,pose.TransformPoint(start.LocalPosition))<1e-5,"Transfer then step reverse; world owner");
            var ceiling=new PortalBarrierCollider(new(.9f,1.85f,.3f),new(1.1f,2,.7f),pose);var ceilingBoxes=new[]{new PortalFloorBox(0,0,ceiling,false)};
            var blocked=query.Walk(start,pose.TransformPoint(end),half,ceilingBoxes,_=>new(true,true),true);
            Require(blocked.Status==FloorBoxWalkStatus.BoxStopped&&blocked.Steps==0&&blocked.Movement.Transfers==0&&blocked.Diagnostic=="VerticalStepBlocked"&&blocked.Movement.Locator.CellIndex==0&&blocked.Movement.Locator.LocalPosition.Y==0,"Blocked rise owns source floor");
            for(int i=0;i<100;i++)
            {
                var repeat=query.Walk(blocked.Movement.Locator,pose.TransformPoint(end),half,ceilingBoxes,_=>new(true,true),true);
                Require(repeat.Steps==0&&repeat.Movement.Locator==blocked.Movement.Locator,"Repeated portal rise hold has no drift");
            }
            var dropBlock=new PortalBarrierCollider(new(.9f,.04f,.3f),new(1.1f,.05f,.7f),pose);
            var blockedDrop=query.Walk(forward.Movement.Locator,pose.TransformPoint(start.LocalPosition),half,new[]{new PortalFloorBox(0,0,dropBlock,false)},_=>new(true,true),true);
            Require(blockedDrop.Status==FloorBoxWalkStatus.BoxStopped&&blockedDrop.Steps==0&&blockedDrop.Movement.Transfers==1&&blockedDrop.Movement.Locator.CellIndex==0&&blockedDrop.Movement.Locator.LocalPosition.Y==.1f,"Blocked drop preserves completed transfer and supported upper floor");
            var budget=query.Walk(start,pose.TransformPoint(end),half,Array.Empty<PortalFloorBox>(),_=>new(true,true),maximumCrossings:2);
            Require(budget.Steps==0&&budget.Diagnostic=="StepTraversalLimit","Combined portal step budget");
            var far=new PortalBarrierCollider(new(100,100,100),new(101,101,101),pose);var snapshots=new[]{new PortalFloorBox(0,0,far,false)};
            var captured=query.Walk(start,pose.TransformPoint(end),half,snapshots,_=>{snapshots[0]=ceilingBoxes[0];return new(true,true);},true);
            Require(captured.Status==FloorBoxWalkStatus.Accepted&&captured.Steps==1,"Full request captures boxes before portal callbacks");
            Require(query.Walk(start,pose.TransformPoint(end),half,snapshots,_=>new(true,true),true).Diagnostic=="VerticalStepBlocked","Next request sees new snapshot");
        }
        var noTransfer=new PortalFloorTransferQuery(layout,floors).Walk(new(0,3,new(1.1f,.1f,.5f)),new(1.75f,.1f,.5f),_=>throw new Exception("Transfer cap called permission"),maximumTransfers:0);
        Require(noTransfer.Status==CellFloorWalkStatus.TransferLimit&&noTransfer.Transfers==0,"Zero-transfer continuation stops before callback");
        Console.WriteLine("PASS: world step/portal order, upright owner poses, vertical rollback before/after transfer, 200 repeat holds, permission/snapshot/resource/transfer caps");
    }
    private static void Require(bool value,string message){if(!value)throw new Exception("Portal step: "+message);}
}
