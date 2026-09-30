using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class PlanarActorScenarios
{
    internal static readonly (string Name, int Ticks, float Forward, float Right)[] Phases =
    {
        ("wait",30,0,0),("right",8,0,1),("diagonal-forward",40,1,-.2f),
        ("left",8,0,-1),("diagonal-return",40,-1,.2f),("settle",8,0,0)
    };
    internal static void Run()
    {
        foreach (Vector3 forward in new[] { Vector3.UnitX, Vector3.UnitZ, new Vector3(-1,0,1), new Vector3(float.MaxValue,0,float.MaxValue) })
        foreach (float a in new[] {-1f, -.3f, 0f, .3f, 1f})
        foreach (float b in new[] {-1f, -.3f, 0f, .3f, 1f})
        {
            var input=PlanarActorInputMapping.Create(forward,a,b,1.6f,null);
            Require(input.WorldVelocity.Y==0 && input.WorldVelocity.Length()<=1.600001f && input.EntryAllowed is null,"Bounded horizontal speed and nullable permission");
            double magnitude=Math.Min(1,Math.Sqrt((double)a*a+(double)b*b))*1.6;
            Require(Math.Abs(input.WorldVelocity.Length()-magnitude)<1e-6,"Analog magnitude and normalized diagonal");
        }
        Require(PlanarActorInputMapping.Create(Vector3.UnitZ,0,1,1.6f,true).WorldVelocity==new Vector3(1.6f,0,0),"Native right convention");
        foreach(Vector3 invalid in new[]{Vector3.Zero,Vector3.UnitY,new Vector3(float.NaN,0,0)})HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(()=>PlanarActorInputMapping.Create(invalid,1,0,1,true));
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(()=>PlanarActorInputMapping.Create(Vector3.UnitX,1.01f,0,1,true));
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(()=>PlanarActorInputMapping.Create(Vector3.UnitX,1,0,10.01f,true));
        var bundle=InteriorDoorScenarios.Fixture();
        foreach(var owner in new[]{AppearanceTransform.Identity,new AppearanceTransform(new(0,0,1),Vector3.UnitY,new(-1,0,0),new(30,4,-11))})
        {
            using var actor=new PortalDoorActorSimulation(bundle,new(0,0,new(-.8f,0,0)),owner);
            Vector3 forward=owner.TransformDirection(Vector3.UnitX);var host=new FixedStepPortalActorHost(actor);var timeline=new PortalActorInputTimeline(host,0,new(Vector3.Zero,true));long now=0;
            foreach(var phase in Phases)
            {
                timeline.Submit(new(now,PlanarActorInputMapping.Create(forward,phase.Forward,phase.Right,1.6f,true)));
                for(int i=0;i<phase.Ticks;i++)timeline.AdvanceTo(now+=20_000);
            }
            Require(actor.State.TotalTransfers==2 && actor.State.Locator.CellIndex==0,"Planar diagonal round trip retains ownership");
            Require(Vector3.Distance(actor.State.WorldPosition,owner.TransformPoint(new(-.8f,0,0)))<1e-4,"Planar path returns to start");
            // A lateral move beyond the authored floor must stop instead of inventing a floor/portal transfer.
            timeline.Submit(new(now,PlanarActorInputMapping.Create(forward,0,1,1.6f,true)));
            for(int i=0;i<100;i++)timeline.AdvanceTo(now+=20_000);
            Require(actor.State.Locator.CellIndex==0 && Math.Abs(actor.State.Locator.LocalPosition.Z)<=2.0001f && actor.State.Movement?.Movement.Status==CellFloorWalkStatus.FloorStopped,"Lateral floor boundary explicit stop");
        }
        Console.WriteLine("PASS: normalized planar/analog input, coordinate conventions, transformed diagonal round trip and lateral floor stop");
    }
    private static void Require(bool value,string label){if(!value)throw new Exception("Planar actor: "+label);}
    internal static void ProbeOptional(string[] args)
    {
        if(args.Length!=16 || args[2]!="--configuration" || args[4]!="--member" || args[6]!="--expected-sha256" || args[8]!="--cell" || args[10]!="--portal" || args[12]!="--table-sha256" || args[14]!="--leaf-sha256")throw new ArgumentException("Planar probe requires pinned doorway.");
        using StreamReader cfg=File.OpenText(args[3]);using ConfiguredTreOverlay overlay=ConfiguredTreOverlay.Open(args[1],cfg);
        var bundle=PortalSingleLeafDoorBundleReader.Load(overlay.Lookup,args[5],args[7],int.Parse(args[9]),int.Parse(args[11]),args[13],args[15]);var approach=bundle.InspectionApproach();Vector3 forward=approach.End-approach.Start.LocalPosition;forward.Y=0;forward=Vector3.Normalize(forward);
        foreach(var source in bundle.Sources.Values)Console.WriteLine("planar_source="+JsonSerializer.Serialize(source));
        foreach(bool transformed in new[]{false,true})
        {
            var owner=transformed?new AppearanceTransform(new(0,0,1),Vector3.UnitY,new(-1,0,0),new(30,4,-11)):AppearanceTransform.Identity;
            using var actor=new PortalDoorActorSimulation(bundle,approach.Start,owner);var timeline=new PortalActorInputTimeline(new(actor),0,new(Vector3.Zero,true));long now=0;
            foreach(var phase in Phases)
            {
                var input=PlanarActorInputMapping.Create(owner.TransformDirection(forward),phase.Forward,phase.Right,1.6f,true);timeline.Submit(new(now,input));
                for(int i=0;i<phase.Ticks;i++)
                {
                    var s=timeline.AdvanceTo(now+=20_000);var a=s.Actor;
                    Console.WriteLine("planar_tick="+JsonSerializer.Serialize(new{transformed,phase=phase.Name,now,velocity=InteriorDoorScenarios.V(input.WorldVelocity),frame=a.Frame,transfers=a.TotalTransfers,cell=a.Locator.CellIndex,local=InteriorDoorScenarios.V(a.Locator.LocalPosition),world=InteriorDoorScenarios.V(a.WorldPosition),fraction=a.Door.Controller.Fraction,curve=a.Door.DisplacementFraction,retained=a.Door.RetainedActors,status=a.Movement?.Movement.Status.ToString(),kind=a.Movement?.ContactKind?.ToString()}));
                }
            }
        }
    }
}
