using System.Numerics;
using Stardust.Core.Assets;

internal static class LocomotionPreviewScenarios
{
    internal static void Run()
    {
        var idle=new PreviewLocomotionClip(30,120,1,0);var walk=new PreviewLocomotionClip(30,33,1,1.5f);var run=new PreviewLocomotionClip(30,22,1,5);
        LocomotionPreviewClock Create()=>new(idle,walk,run,3);
        var clock=Create();clock.Reset(Vector3.UnitX);clock.Advance(.1,Vector3.Zero);
        if(clock.State.Mode!=PreviewLocomotionMode.Idle||clock.State.Frame!=3||clock.State.Heading!=Vector3.UnitX)throw new Exception("Idle presentation clock mismatch.");
        double phase=clock.State.NormalizedPhase;clock.Advance(.1,new(0,.8f,.15f));
        if(clock.State.Mode!=PreviewLocomotionMode.Walk||Math.Abs(clock.State.Frame-(phase*33+3))>1e-6||clock.State.Heading!=Vector3.UnitZ)throw new Exception("Accepted planar walk phase mismatch.");
        clock.Advance(.1,new(.5f,0,0));if(clock.State.Mode!=PreviewLocomotionMode.Run||clock.State.Switches!=2||clock.State.Heading!=Vector3.UnitX)throw new Exception("Run switch/heading mismatch.");
        var held=clock.State;clock.Advance(.2,new(1,0,0),active:false);if(clock.State!=held)throw new Exception("Suspended locomotion advanced.");
        clock.Advance(.1,Vector3.Zero);if(clock.State.Mode!=PreviewLocomotionMode.Idle||clock.State.AcceptedDistance!=held.AcceptedDistance)throw new Exception("Blocked movement should select idle without foot sliding phase.");
        var whole=Create();var split=Create();whole.Advance(.2,new(0,0,.3f));for(int i=0;i<10;i++)split.Advance(.02,new(0,0,.03f));
        if(Math.Abs(whole.State.NormalizedPhase-split.State.NormalizedPhase)>1e-7||Math.Abs(whole.State.AcceptedDistance-split.State.AcceptedDistance)>1e-7)throw new Exception("Distance-linked phase depends on update subdivision.");
        var reversed=Create();reversed.Advance(.1,new(0,0,.15f));reversed.Advance(.1,new(0,0,-.15f));if(reversed.State.AcceptedDistance<.29||reversed.State.Heading!=-Vector3.UnitZ)throw new Exception("Opposite accepted steps lost traveled distance.");
        void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
        foreach(var bad in new[]{double.NaN,-1,.251,double.PositiveInfinity})Reject(()=>clock.Advance(bad,Vector3.Zero));
        Reject(()=>clock.Advance(0,Vector3.UnitZ));Reject(()=>clock.Advance(.1,new(float.NaN,0,0)));Reject(()=>clock.Reset(Vector3.UnitY));Reject(()=>new LocomotionPreviewClock(idle,walk with{AuthoredSpeed=0},run,3));Reject(()=>new LocomotionPreviewClock(idle,walk,run,0));
        var snapshot=clock.State;Reject(()=>clock.Advance(.1,new(101,0,0)));if(clock.State!=snapshot)throw new Exception("Rejected locomotion tick changed state.");
        clock.Reset(-Vector3.UnitZ);if(clock.State.Frame!=0||clock.State.AcceptedDistance!=0||clock.State.Switches!=0)throw new Exception("Locomotion reset retained phase/distance.");
        Console.WriteLine("PASS: explicit accepted-displacement locomotion policy; gait/heading/shared phase, blocked idle, pause/reset, subdivision/reversal and transactional negative gates");
    }
}
