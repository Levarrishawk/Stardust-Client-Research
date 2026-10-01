using System.Numerics;
using Stardust.Core.Assets;

internal static class ActorViewScenarios
{
    internal static void Run()
    {
        var initial=new ActorViewOrientation();var right=initial.Rotate(MathF.PI/2,.4f);
        if(Vector3.Distance(initial.LookForward(Vector3.UnitZ),Vector3.UnitZ)>1e-6f||Vector3.Distance(right.PlanarForward(Vector3.UnitZ),Vector3.UnitX)>1e-6f||MathF.Abs(right.LookForward(Vector3.UnitZ).Length()-1)>1e-6f)throw new Exception("Asymmetric view orientation failed.");
        var velocity=PlanarActorInputMapping.Create(right.PlanarForward(Vector3.UnitZ),1,1,1.6f,true).WorldVelocity;
        if(velocity.Y!=0||MathF.Abs(velocity.Length()-1.6f)>1e-6f||velocity.X<=0||velocity.Z>=0)throw new Exception("Pitched view changed planar locomotion.");
        if(new ActorViewOrientation(0,100).PitchRadians!=ActorViewOrientation.MaximumPitchRadians||new ActorViewOrientation(0,-100).PitchRadians!=-ActorViewOrientation.MaximumPitchRadians||MathF.Abs(new ActorViewOrientation(4*MathF.PI,0).HeadingRadians)>1e-6f)throw new Exception("View wrap/pitch limits failed.");
        var owner=new AppearanceTransform(new(0,0,1),Vector3.UnitY,new(-1,0,0),new(20,4,-9));
        if(Vector3.Distance(owner.TransformDirection(right.PlanarForward(Vector3.UnitZ)),-Vector3.UnitZ)>1e-6f)throw new Exception("View owner direction conversion failed.");
        foreach(var pose in new[]{AppearanceTransform.Identity,owner})foreach(var times in new[]{new long[]{20_000,40_000,60_000},new long[]{10_000,20_000,30_000,40_000,50_000,60_000}})
        {
            Vector3 position=pose.Translation;
            var host=new FixedStepActorHost<Vector3>(()=>position,(_,delta,_)=>position+=delta,()=>position=pose.Translation);
            var timeline=new PortalActorInputTimeline<Vector3>(host,0,PlanarActorInputMapping.Create(pose.TransformDirection(initial.PlanarForward(Vector3.UnitZ)),1,0,1.6f,true));
            timeline.Submit(new(35_000,PlanarActorInputMapping.Create(pose.TransformDirection(right.PlanarForward(Vector3.UnitZ)),1,0,1.6f,true)));
            foreach(long time in times)timeline.AdvanceTo(time);
            if(Vector3.Distance(position,pose.TransformPoint(new(.032f,0,.064f)))>2e-6f)throw new Exception("Between-render view heading changed tick-start movement.");
        }
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(()=>initial.Rotate(float.NaN,0));
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(()=>new ActorViewOrientation(float.PositiveInfinity,0));
        HostAccountingScenarios.Reject<ArgumentException>(()=>initial.PlanarForward(Vector3.Zero));
        HostAccountingScenarios.Reject<ArgumentException>(()=>initial.PlanarForward(Vector3.UnitY));
        Console.WriteLine("PASS: immutable view heading/pitch, wrap/clamp, planar diagonal speed, owner mapping and invalid angles/axes");
    }
}
