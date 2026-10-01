using System.Numerics;
using Stardust.Core.Assets;

internal static class InteriorSphereSceneScenarios
{
    internal static void Run()
    {
        var f=InteriorDoorSceneScenarios.Fixture(); var initial=new CellFloorLocator(0,0,new(-.8f,0,0)); int transfers=0;
        foreach(var owner in new[]{AppearanceTransform.Identity,new AppearanceTransform(new(0,0,1),Vector3.UnitY,new(-1,0,0),new(20,4,-9))})
        {
            using var scene=new InteriorDoorActorScene(f.Layout,f.Floors,f.Bindings,initial,owner,actorSphere:new(new(0,1,0),.5f));
            var before=scene.State; int samples=0;
            HostAccountingScenarios.Reject<InvalidOperationException>(()=>scene.Tick(.05f,Vector3.Zero,l=>{if(++samples==2)throw new InvalidOperationException("Injected permission failure");return new(true,true);}));
            Require(ReferenceEquals(scene.State,before)&&scene.State.Doors.Values.All(d=>d.Controller.Fraction==0&&d.RetainedActors==0),"Permission failure precedes every mutation");
            HostAccountingScenarios.Reject<InvalidOperationException>(()=>scene.Tick(.05f,Vector3.Zero,l=>{scene.Reset();return new(true,true);}));
            Require(ReferenceEquals(scene.State,before),"Reentrant reset rejected atomically");
            HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(()=>scene.Tick(float.NaN,Vector3.Zero,_=>new(true,true)));
            HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(()=>scene.Tick(.05f,new(101,0,0),_=>new(true,true)));
            void Go(float x,int cell)
            {
                for(int i=0;i<160&&Math.Abs(scene.State.Locator.LocalPosition.X-x)>1e-5;i++)
                {
                    float delta=Math.Clamp(x-scene.State.Locator.LocalPosition.X,-.08f,.08f);
                    var state=scene.Tick(.05f,owner.TransformDirection(new(delta,0,0)),_=>new(true,true));
                    Require(state.Movement!.Movement.Status!=CellFloorWalkStatus.InvalidStart&&state.Movement.Movement.Transfers<=1,"Reusable one-transfer tick");
                }
                Require(scene.State.Locator.CellIndex==cell&&Math.Abs(scene.State.Locator.LocalPosition.X-x)<1e-5,$"Three-cell route reaches connected owner: desired {x}/{cell}; actual {scene.State.Locator}; movement {scene.State.Movement}; doors {string.Join(',',scene.State.Doors.Select(p=>$"{p.Key}:{p.Value.Controller.Fraction}"))}");
            }
            for(int trip=0;trip<10;trip++){Go(4.8f,2);Go(-.8f,0);}
            Require(before.Doors.Values.All(d=>d.Controller.Fraction==0&&d.RetainedActors==0),"Earlier scene snapshot remains immutable");
            Require(scene.State.TotalTransfers==40,"Two leaves and reverse incidences share exact ownership");transfers+=(int)scene.State.TotalTransfers;
            Go(4.8f,2);
            scene.Tick(.05f,Vector3.Zero,_=>new(true,true));
            Require(scene.State.Doors[0].RetainedActors==0,"Leaving both incident cells removes stale retention");
            scene.Reset();Require(scene.State.Frame==0&&scene.State.TotalTransfers==0&&scene.State.Doors.Values.All(d=>d.Controller.Fraction==0&&d.RetainedActors==0),"Reset clears every owned controller");
            for(int i=0;i<100;i++)scene.Tick(.05f,owner.TransformDirection(new(.08f,0,0)),l=>new(l.GeometryIndex!=1,true));
            Require(scene.State.Locator.CellIndex==1&&scene.State.TotalTransfers==1&&scene.State.Movement!.ContactKind==PortalActorContactKind.DoorLeaf&&scene.State.Doors[1].Controller.Fraction==0,"Denied closed second leaf retains owner without opening");
            var held=scene.State.WorldPosition;scene.Tick(.05f,owner.TransformDirection(new(-.08f,0,0)),l=>new(l.GeometryIndex!=1,true));
            Require(Vector3.Distance(held,scene.State.WorldPosition)>.07,"Retreat from denied second doorway");
            scene.Reset();Go(4.8f,2);Go(3.2f,1);
            long priorTransfers=scene.State.TotalTransfers;
            for(int i=0;i<20;i++)scene.Tick(0,owner.TransformDirection(new(.08f,0,0)),l=>new(l.GeometryIndex!=1,true));
            Require(scene.State.Locator.CellIndex==1&&scene.State.TotalTransfers==priorTransfers&&scene.State.Movement!.Movement.Status==CellFloorWalkStatus.EntryDenied,"Denied open second doorway stops at directional barrier");
            scene.Tick(0,owner.TransformDirection(new(.08f,0,0)),l=>new(l.GeometryIndex==1?null:true,true));
            Require(scene.State.Movement!.Movement.Status==CellFloorWalkStatus.PermissionUnknown&&scene.State.TotalTransfers==priorTransfers,"Unknown second doorway permission remains distinct");
            scene.Dispose();Require(scene.State.Doors.Values.All(d=>d.RetainedActors==0&&d.Controller.Fraction==0),"Dispose clears all shared leaves");
            HostAccountingScenarios.Reject<ObjectDisposedException>(()=>scene.Tick(.05f,Vector3.Zero,_=>new(true,true)));
        }
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new InteriorDoorActorScene(f.Layout,f.Floors,f.Bindings.Take(1).ToArray(),initial));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new InteriorDoorActorScene(f.Layout,f.Floors,new[]{f.Bindings[0],f.Bindings[0]},initial));
        Require(transfers==80,"Synthetic multi-door transfer accounting");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new InteriorDoorActorScene(f.Layout,f.Floors,f.Bindings,initial,actorSphere:new(Vector3.UnitY,0)));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new InteriorDoorActorScene(f.Layout,f.Floors,f.Bindings,initial,actorHalfExtents:Vector3.One,actorSphere:new(Vector3.UnitY,.5f)));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new InteriorDoorActorScene(f.Layout,f.Floors,f.Bindings,initial,stepPolicy:new(UseLocalRayCoverage:true),actorSphere:new(Vector3.UnitY,.5f)));
        int reads=0;TreLookupResult Never(string path){reads++;throw new Exception("Invalid actor profile options reached asset lookup.");}
        HostAccountingScenarios.Reject<ArgumentException>(()=>InteriorSceneContentBundleReader.Load(Never,new byte[]{1},new string('0',64),new string('0',64),"appearance/test.pob",new string('0',64),actorTemplatePath:"object/test.iff"));
        HostAccountingScenarios.Reject<ArgumentException>(()=>InteriorSceneContentBundleReader.Load(Never,new byte[]{1},new string('0',64),new string('0',64),"appearance/test.pob",new string('0',64),actorTemplatePath:"appearance/test.iff",actorTemplateSha256:new string('0',64)));
        HostAccountingScenarios.Reject<ArgumentException>(()=>InteriorSceneContentBundleReader.Load(Never,new byte[]{1},new string('0',64),new string('0',64),"appearance/test.pob",new string('0',64),actorTemplatePath:"object/test.iff",actorTemplateSha256:"invalid"));
        HostAccountingScenarios.Reject<ArgumentException>(()=>InteriorSceneContentBundleReader.Load(Never,new byte[]{1},new string('0',64),new string('0',64),"appearance/test.pob",new string('0',64),createTriangleGuard:true));
        if(reads!=0)throw new Exception("Actor profile option validation did not precede source lookup.");
        using(var guarded=new InteriorDoorActorScene(f.Layout,f.Floors,f.Bindings,new(0,0,new(-.58f,0,0)),holdClosingLeaves:true,actorSphere:new(Vector3.UnitY,.5f)))
        {
            for(int i=0;i<25;i++)guarded.Tick(.05f,Vector3.Zero,_=>new(true,true));
            if(guarded.State.Doors[0].Controller.Fraction!=1)throw new Exception("Sphere guarded opening.");
            for(int i=0;i<30;i++)guarded.Tick(.05f,Vector3.Zero,_=>new(false,true));
            if(!guarded.State.Doors[0].ClosingHeld||guarded.State.TotalTransfers!=0)throw new Exception("Sphere enclosing cube closing guard.");
            for(int i=0;i<15;i++)guarded.Tick(.05f,new(-.08f,0,0),_=>new(false,true));
            for(int i=0;i<45;i++)guarded.Tick(.05f,Vector3.Zero,_=>new(false,true));
            if(guarded.State.Doors[0].Controller.Fraction!=0||guarded.State.Doors[0].ClosingHeld)throw new Exception("Sphere retreat releases closing guard.");
        }
        Console.WriteLine("PASS: persistent sphere three-cell/two-leaf scene,80 transfers, directional permission/errors/reset/dispose and conservative closing guard");
    }
    private static void Require(bool value,string label){if(!value)throw new Exception("Interior sphere scene: "+label);}
}
