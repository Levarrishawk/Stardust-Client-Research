using System;
using System.Linq;
using System.Text.Json;
using Godot;
using Stardust.Core.Session;

public partial class ScenePumpedWorldPreview
{
    private bool pointRunHosted;
    private TerrainRunInputHost? runEventHost;
    private void PrepareRunInputHost()
    {
        if(!pointRunHosted)return;
        runEventHost=new TerrainRunInputHost{Name="OwnedRunInputHost"};AddChild(runEventHost);
        runEventHost.Configure(()=>pointProfile!.RootElement.GetProperty("InvalidRunClock").GetBoolean()&&pointIndex==27?checked(localClock+1):localClock,
            (running,clock)=>pointDriver!.SetMovementSpeed(PointFloat(pointProfile!.RootElement,running?"RunSpeedBits":"WalkSpeedBits"),clock));
        PrepareRunTransport();
        PrepareSoeIngress();
    }
    private void PrintMovementSpeedIntent(string source,SceneWorldTerrainPointTargetState before,SceneWorldObjectMotionSnapshot? motion,System.Numerics.Vector2 input,bool paused)
    {
        var snapshot=world!.Snapshots().Single(s=>s.Object.Handle.ObjectId==humanId);var after=pointDriver!.State;
        Check(ReferenceEquals(before.DriverResult,after.DriverResult)&&ReferenceEquals(before.Target,after.Target)
            &&before.SuccessfulTargets==after.SuccessfulTargets&&ReferenceEquals(motion,motions!.Find(snapshot))
            &&pointDriver.ManualInput==input&&pointDriver.IsPaused==paused,"Speed setter changes owned supplied speed only");
        GD.Print("movement_speed_intent="+JsonSerializer.Serialize(new{AtStep=pointIndex,Source=source,Speed=pointDriver.MovementSpeed,
            Running=runEventHost!.IsRunning,TimestampMicroseconds=localClock,DriverStatePreserved=true,TargetPreserved=true,
            MotionPreserved=true,ManualInputPreserved=true,PausedPreserved=true,OriginalMovementParity=false}));
    }
    private void PushRunKey(bool pressed)
    {
        var owned=runEventHost!;var before=pointDriver!.State;var input=pointDriver.ManualInput;bool paused=pointDriver.IsPaused;
        var snapshot=world!.Snapshots().Single(s=>s.Object.Handle.ObjectId==humanId);var motion=motions!.Find(snapshot);
        int handled=owned.HandledCount;
        using var key=new InputEventKey{PhysicalKeycode=Key.Shift,Keycode=Key.Shift,Location=KeyLocation.Left,Pressed=pressed};
        GetViewport().PushInput(key,true);
        if(owned.LastFailure is not null){
            GD.Print("run_input_host_failure="+JsonSerializer.Serialize(new{AtStep=pointIndex,Failure=owned.LastFailure,owned.IsFaulted,
                owned.ClockReadCount,owned.SinkCallCount,owned.AcceptedCount,owned.HandledCount,SinkInvoked=owned.LastDiagnostic!.SinkInvoked,
                SinkReturned=owned.LastDiagnostic.SinkReturned,StateCommitted=owned.LastDiagnostic.StateCommitted,
                ActualViewportEvent=owned.LastDiagnostic.FromGodotEvent,Speed=pointDriver.MovementSpeed,
                ManualInputPreserved=pointDriver.ManualInput==input,TargetPreserved=ReferenceEquals(before.Target,pointDriver.Target),
                DriverStatePreserved=ReferenceEquals(before.DriverResult,pointDriver.State.DriverResult)}));
            throw new InvalidOperationException("Hosted run input failed: "+owned.LastFailure);
        }
        Check(owned.HandledCount==handled+1&&owned.LastDiagnostic?.FromGodotEvent==true&&owned.IsRunning==pressed,"Actual viewport Shift selects run intent");
        PrintMovementSpeedIntent(pressed?"ShiftPressed":"ShiftReleased",before,motion,input,paused);
        GD.Print("run_viewport_input="+JsonSerializer.Serialize(new{AtStep=pointIndex,owned.IsRunning,owned.HeldKeyMask,owned.ClockReadCount,
            owned.SinkCallCount,owned.AcceptedCount,owned.HandledCount,ActualViewportEvent=true,Clock=owned.LastDiagnostic!.TimestampMicroseconds}));
    }
    private void AdvanceRunInputSchedule()
    {
        if(!pointRunHosted)return;
        switch(pointIndex){
            case 25:
                var before=pointDriver!.State;var snapshot=world!.Snapshots().Single(s=>s.Object.Handle.ObjectId==humanId);var row=motions!.Find(snapshot);
                var input=pointDriver.ManualInput;bool paused=pointDriver.IsPaused;
                pointDriver.SetMovementSpeed(PointFloat(pointProfile!.RootElement,"WalkSpeedBits"),localClock);
                PrintMovementSpeedIntent("InitialWalk",before,row,input,paused);break;
            case 27:PushRunKey(true);break;
            case 29:PushRunKey(false);break;
            case 31:Check(runEventHost!.SetEnabled(false),"Disable run keys before actual GUI focus");break;
            case 32:Check(runEventHost!.SetEnabled(true),"Resume empty run key set");break;
            case 34:PushRunKey(true);break;
        }
    }
    private void CloseRunInputHost()
    {
        var owned=runEventHost;runEventHost=null;
        try {
            if(owned is not null){float speed=pointDriver!.MovementSpeed;owned.Close();owned.Close();
                Check(owned.IsClosed&&!owned.IsRunning&&pointDriver.MovementSpeed==speed,"Run close releases local state without borrowed speed mutation");
                GD.Print("run_input_host_cleanup="+JsonSerializer.Serialize(new{Closed=true,owned.ClockReadCount,owned.SinkCallCount,owned.AcceptedCount,owned.HandledCount,
                    owned.IsFaulted,ControllerPreserved=true,SpeedPreserved=true,Speed=speed}));
                if(GodotObject.IsInstanceValid(owned)){if(owned.GetParent() is Node parent)parent.RemoveChild(owned);owned.Free();}
            }
        }finally{CloseSoeIngress();CloseRunTransport();}
    }
}
