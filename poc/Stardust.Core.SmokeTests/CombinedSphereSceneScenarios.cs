using System.Numerics;
using Stardust.Core.Assets;

internal static class CombinedSphereSceneScenarios
{
    internal static void Run()
    {
        var f=InteriorDoorSceneScenarios.Fixture();var initial=new CellFloorLocator(0,0,new(-.8f,0,0));
        CollisionTriangle[] Wall(float x)=>new[]{new CollisionTriangle(12,new(x,-10,-10),new(x,10,-10),new(x,0,10))};
        foreach(var pose in new[]{AppearanceTransform.Identity,new AppearanceTransform(new(0,0,1),Vector3.UnitY,new(-1,0,0),new(20,4,-9))})
        {
            var supplied=Wall(-.2f);
            using var source=new InteriorDoorActorScene(f.Layout,f.Floors,f.Bindings,initial,pose,actorSphere:new(Vector3.UnitY,.5f),
                staticTriangles:new Dictionary<int,IReadOnlyList<CollisionTriangle>>{{0,supplied}});
            supplied[0]=supplied[0] with{A=new(float.NaN)};
            int calls=0;var hit=source.Tick(.05f,pose.TransformDirection(new(.8f,0,0)),l=>{calls++;return new(true,true);});
            if(calls!=source.Links.Count||hit.Movement!.ContactKind!=PortalActorContactKind.StaticTriangle||hit.Movement.ObstacleCell!=0||hit.Movement.ObstacleTriangle!=12||
                Math.Abs(hit.Locator.LocalPosition.X+.7f)>1e-5||hit.TotalTransfers!=0)throw new Exception("Static triangle before closed door, owned geometry, metadata and callback count.");
            var held=hit.Locator;
            for(int i=0;i<100;i++)
                if(source.Tick(.05f,pose.TransformDirection(new(.8f,0,0)),_=>new(true,true)).Locator!=held)throw new Exception("Combined source contact crept.");
            if(Vector3.Distance(source.Tick(.05f,pose.TransformDirection(new(-.1f,0,0)),_=>new(true,true)).Locator.LocalPosition,initial.LocalPosition)>1e-4)
                throw new Exception("Combined source contact retreat.");
            using var target=new InteriorDoorActorScene(f.Layout,f.Floors,f.Bindings,initial,pose,actorSphere:new(Vector3.UnitY,.5f),
                staticTriangles:new Dictionary<int,IReadOnlyList<CollisionTriangle>>{{1,Wall(2)}});
            hit=target.Tick(.05f,pose.TransformDirection(new(2.3f,0,0)),_=>new(true,true));
            if(hit.Movement!.ContactKind!=PortalActorContactKind.DoorLeaf||hit.TotalTransfers!=0)throw new Exception("Closed door before target wall.");
            for(int i=0;i<30;i++)target.Tick(.05f,Vector3.Zero,_=>new(true,true));
            hit=target.Tick(.05f,pose.TransformDirection(new(3,0,0)),_=>new(true,true));
            if(hit.Movement!.ContactKind!=PortalActorContactKind.StaticTriangle||hit.Locator.CellIndex!=1||hit.TotalTransfers!=1||Math.Abs(hit.Locator.LocalPosition.X-1.5f)>1e-5||hit.Movement.Diagnostic is not null)
                throw new Exception("Open door then target static contact and supported landing.");
            held=hit.Locator;
            for(int i=0;i<100;i++)if(target.Tick(.05f,pose.TransformDirection(new(.5f,0,0)),_=>new(true,true)).Locator!=held)throw new Exception("Combined target contact crept.");
            hit=target.Tick(.05f,pose.TransformDirection(new(-2.3f,0,0)),_=>new(true,true));
            if(hit.Locator.CellIndex!=0||hit.TotalTransfers!=2||Vector3.Distance(hit.Locator.LocalPosition,initial.LocalPosition)>1e-4)throw new Exception("Combined reverse owner transfer.");
            target.Reset();if(target.State.TotalTransfers!=0||target.State.Doors.Values.Any(d=>d.RetainedActors!=0))throw new Exception("Combined reset lifecycle.");
            var before=target.State;HostAccountingScenarios.Reject<InvalidOperationException>(()=>target.Tick(.05f,Vector3.Zero,_=>throw new InvalidOperationException("policy")));
            if(!ReferenceEquals(before,target.State))throw new Exception("Combined permission failure adopted state.");
        }
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new InteriorDoorActorScene(f.Layout,f.Floors,f.Bindings,initial,
            staticTriangles:new Dictionary<int,IReadOnlyList<CollisionTriangle>>{{0,Wall(2)}}));
        Console.WriteLine("PASS: combined sphere triangle/door earliest contact, source/target ownership,400 held contacts, retreat, immutable geometry, exact callbacks and lifecycle over two poses");
    }
}
