using System.Text.Json;
using Stardust.Core.Assets;

internal static class CameraDistanceClockScenarios
{
    internal static void Run()
    {
        var clock=new ChaseCameraDistanceClock();clock.Advance(5,0);
        if(clock.Advance(1,0,false).Distance!=1)throw new Exception("Camera collision must retract even while paused.");
        double half=clock.Advance(4,.2*Math.Log(2)).Distance;if(Math.Abs(half-2.5)>1e-12)throw new Exception("Camera hand-calculated half recovery mismatch.");
        var whole=new ChaseCameraDistanceClock();whole.Advance(1,0);whole.Advance(4,.25);
        var parts=new ChaseCameraDistanceClock();parts.Advance(1,0);for(int i=0;i<25;i++)parts.Advance(4,.01);
        if(Math.Abs(whole.State!.Distance-parts.State!.Distance)>1e-12)throw new Exception("Camera constant-clearance partition mismatch.");
        var held=clock.Advance(4,.1,false);if(held.Distance!=half||clock.Advance(4,0).Distance!=half)throw new Exception("Camera paused/zero-time release changed distance.");
        if(clock.Advance(0,0).Distance!=0)throw new Exception("Camera overlap did not hold anchor.");clock.Reset();if(clock.State is not null||clock.Advance(2,0,false).Distance!=2)throw new Exception("Camera reset/initial safe pose mismatch.");
        var before=clock.State;HostAccountingScenarios.Reject<InvalidDataException>(()=>clock.Advance(float.NaN,.1));HostAccountingScenarios.Reject<InvalidDataException>(()=>clock.Advance(3,.3));HostAccountingScenarios.Reject<InvalidDataException>(()=>new ChaseCameraDistanceClock(0));if(!ReferenceEquals(before,clock.State))throw new Exception("Invalid camera recovery changed state.");
        Console.WriteLine("PASS: camera immediate collision retraction, hand-calculated exponential release, partition/pause/zero/reset and atomic negative gates");
    }
    internal static void Probe()
    {
        var clock=new ChaseCameraDistanceClock();
        for(int i=0;i<256;i++)
        {
            float safe=(i%32)<8?.25f+(i%8)*.1f:(i%32)<24?5f:0;double seconds=(i%5)*.025;bool active=i%11!=0;
            if(i%64==0)clock.Reset();var result=clock.Advance(safe,seconds,active);
            Console.WriteLine("camera_distance_clock="+JsonSerializer.Serialize(new{i,safe,seconds,active,reset=i%64==0,result.Distance,result.Recovering}));
        }
    }
}
