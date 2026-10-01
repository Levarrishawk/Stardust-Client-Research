using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class DoorClosingSafetyScenarios
{
    private static SingleLeafDoorRuntime Open(AppearanceTransform owner)
    {
        var basis=InteriorDoorScenarios.Fixture();var extent=basis.Leaf.Extent with{Minimum=new(-.1f,0,-1),Maximum=new(.1f,2,1)};
        var leaf=new SingleLeafDoorRuntime(basis.Leaf.Selection.Style,extent,owner,7);
        var foot=owner.TransformPoint(Vector3.Zero);leaf.NotifyContact(1,7,7,foot+new Vector3(0,.9f,0),true,0);
        leaf.Advance(0,1,_=>foot,true);leaf.Advance(1,0,_=>foot,false);return leaf;
    }
    internal static void Run()
    {
        foreach(var owner in new[]{AppearanceTransform.Identity,new AppearanceTransform(new(0,0,1),Vector3.UnitY,new(-1,0,0),new(20,4,-9))})
        {
            var feet=owner.TransformPoint(Vector3.Zero);var body=new DoorActorBody(1,7,feet,new(.25f,.9f,.25f));
            using var leaf=Open(owner);var before=leaf.State;
            var held=leaf.Advance(2,2,_=>feet,false,new[]{body});
            Require(held.ClosingHeld&&held.Controller.Fraction==1&&held.LeafPose==before.LeafPose&&held.Controller.Transition==DoorTransitionEvent.None&&!held.Controller.Opening,"Obstructed full close holds previous pose without opening/effect pulse");
            for(int frame=3;frame<23;frame++)Require(leaf.Advance(frame,2,_=>feet,false,new[]{body}).ClosingHeld&&leaf.State.Controller.Fraction==1,"Repeated obstruction has no fraction/pose drift");
            var clear=body with{Feet=owner.TransformPoint(new(1,0,0))};var started=leaf.Advance(23,.1f,_=>clear.Feet,false,new[]{clear});
            Require(!started.ClosingHeld&&started.Controller.Fraction<1&&started.Controller.Transition==DoorTransitionEvent.CloseBegin,"Clearance releases hold with one actual close event");
            var closed=leaf.Advance(24,2,_=>clear.Feet,false,new[]{clear});Require(closed.Controller.Fraction==0&&closed.Controller.Transition==DoorTransitionEvent.CloseEnd&&!closed.ClosingHeld,"Clear close completes normally");
            using var legacy=Open(owner);Require(legacy.Advance(2,2,_=>feet,false).Controller.Fraction==0&&!legacy.State.ClosingHeld,"Historical default remains unguarded");
            using var wrong=Open(owner);Require(wrong.Advance(2,2,_=>feet,false,new[]{body with{Cell=9}}).Controller.Fraction==0,"Unrelated owner body ignored");
            using var invalid=Open(owner);var initial=invalid.State;
            HostAccountingScenarios.Reject<InvalidDataException>(()=>invalid.Advance(2,.1f,_=>feet,false,new[]{body with{HalfExtents=new(float.NaN,1,1)}}));
            HostAccountingScenarios.Reject<InvalidDataException>(()=>invalid.Advance(2,.1f,_=>feet,false,new[]{body,body}));
            HostAccountingScenarios.Reject<InvalidOperationException>(()=>invalid.Advance(2,.1f,_=>throw new InvalidOperationException("Injected position failure"),false,new[]{body}));
            Require(invalid.State==initial,"Invalid snapshots/callback failure leave controller and contact state unchanged");
            var mutable=new List<DoorActorBody>{body};var frozen=invalid.Advance(2,2,_=>{mutable[0]=clear;return feet;},false,mutable);
            Require(frozen.ClosingHeld,"Closing bodies copied before position callbacks");
            invalid.Reset();Require(!invalid.State.ClosingHeld&&invalid.State.Controller.Fraction==0&&invalid.State.RetainedActors==0,"Reset clears closing hold/contact");
        }
        var controller=new PortalDoorController(1,2);controller.RequestOpen(0);var preview=controller.PreviewAdvance(0,1);
        Require(preview.Fraction==1&&controller.State.Fraction==0,"Controller preview is pure and does not consume frame");controller.Advance(0,1);
        var fixture=InteriorDoorSceneScenarios.Fixture();
        using(var scene=new InteriorDoorActorScene(fixture.Layout,fixture.Floors,fixture.Bindings,new(0,0,new(-.32f,0,0)),holdClosingLeaves:true))
        {
            for(int i=0;i<25;i++)scene.Tick(.05f,Vector3.Zero,_=>new(true,true));
            var opened=scene.State.Doors[0];Require(opened.Controller.Fraction==1,"Guarded scene opens at occupied threshold");
            for(int i=0;i<30;i++)scene.Tick(.05f,Vector3.Zero,_=>new(false,true));
            Require(scene.State.Doors[0].ClosingHeld&&scene.State.Doors[0].Controller.Fraction>0&&scene.State.TotalTransfers==0,"Stationary actor prevents temporal close without gaining entry");
            var denied=scene.Tick(.05f,new(.08f,0,0),_=>new(false,true));Require(denied.Movement!.Movement.Status==CellFloorWalkStatus.EntryDenied&&denied.TotalTransfers==0,"Held close preserves directional entry barrier");
            for(int i=0;i<20;i++)scene.Tick(.05f,new(-.08f,0,0),_=>new(false,true));
            for(int i=0;i<45;i++)scene.Tick(.05f,Vector3.Zero,_=>new(false,true));
            Require(scene.State.Doors[0].Controller.Fraction==0&&!scene.State.Doors[0].ClosingHeld,"Retreat clears occupied closing sweep and completes close");
        }
        Console.WriteLine("PASS: explicit closing leaf sweeps, transformed actors, repeated pose holds, released close events, historical default, owner filtering, snapshot/failure atomicity and preview/reset");
    }
    internal static void Probe()
    {
        foreach(bool transformed in new[]{false,true})foreach(bool safety in new[]{false,true})
        {
            var owner=transformed?new AppearanceTransform(new(0,0,1),Vector3.UnitY,new(-1,0,0),new(20,4,-9)):AppearanceTransform.Identity;
            using var leaf=Open(owner);
            for(int step=0;step<60;step++)
            {
                var feet=owner.TransformPoint(step<40?Vector3.Zero:new Vector3(1,0,0));var body=new DoorActorBody(1,7,feet,new(.25f,.9f,.25f));
                var state=leaf.Advance(step+2,.1f,_=>feet,false,safety?new[]{body}:null);
                Console.WriteLine("door_closing="+JsonSerializer.Serialize(new{step,transformed,safety,feet=InteriorDoorScenarios.V(feet),fraction=state.Controller.Fraction,curve=state.DisplacementFraction,held=state.ClosingHeld,
                    transition=(int)state.Controller.Transition,retained=state.RetainedActors,position=InteriorDoorScenarios.V(state.LeafPose.Translation)}));
            }
        }
    }
    private static void Require(bool value,string text){if(!value)throw new Exception("Closing safety: "+text);}
}
