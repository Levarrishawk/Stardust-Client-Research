using System;
using System.Linq;
using System.Text.Json;
using Godot;
using Stardust.Core.Session;

public partial class ScenePumpedWorldPreview
{
    private bool pointViewportHosted;
    private TerrainPointTargetInputHost? pointEventHost;
    private SceneWorldClientRootMotionBuilder? pointOutboundBuilder;
    private SceneWorldTerrainPointTargetReceipt? pointClearedTarget;
    private int pointOutboundPackets;
    private void PreparePointInputHost()
    {
        if(!pointViewportHosted)return;
        pointEventHost=new TerrainPointTargetInputHost{Name="OwnedPointInputEventHost"};AddChild(pointEventHost);
        pointEventHost.Configure(pointInputAdapter!,()=>pointProfile!.RootElement.GetProperty("InvalidHostedClock").GetBoolean()?checked(localClock+1):localClock);
        if(!pointManualHosted)pointOutboundBuilder=new(world!,motions!,new SceneWorldClientRootMotionPolicy(1000000,10));
        using var released=new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=false};
        using var right=new InputEventMouseButton{ButtonIndex=MouseButton.Right,Pressed=true};
        using var motion=new InputEventMouseMotion();
        foreach(var input in new InputEvent[]{released,right,motion})GetViewport().PushInput(input,true);
        Check(pointEventHost.ClockReadCount==0&&pointEventHost.AcceptedCount==0&&pointEventHost.HandledCount==0
            &&pointInputAdapter!.Attempts==0&&pointPicker!.Attempts==0,"Actual ignored viewport events charge nothing");
        PrepareManualInputHost();
    }
    private bool PushPointViewportInput(InputEventMouseButton button)
    {
        var owned=pointEventHost!;int before=owned.HandledCount;
        GetViewport().PushInput(button,true);
        if(owned.LastFailure is not null){
            GD.Print("point_input_host_failure="+JsonSerializer.Serialize(new{AtStep=pointIndex,Failure=owned.LastFailure,
                owned.IsFaulted,owned.ClockReadCount,owned.AcceptedCount,owned.HandledCount,PickerAttempts=pointPicker!.Attempts,
                AdapterAttempts=pointInputAdapter!.Attempts,TargetPublished=pointDriver!.Target is not null,
                ActualViewportEvent=owned.LastDiagnostic?.FromGodotEvent==true}));
            throw new InvalidOperationException("Hosted point input failed: "+owned.LastFailure);
        }
        pointInputResult=owned.LastResult;
        Check(owned.HandledCount==before+1&&owned.LastDiagnostic?.FromGodotEvent==true
            &&ReferenceEquals(pointInputResult,pointInputAdapter!.LastResult),"Actual Godot unhandled-input owner consumed exact result");
        GD.Print("point_viewport_input="+JsonSerializer.Serialize(new{AtStep=pointIndex,owned.ClockReadCount,owned.AcceptedCount,owned.HandledCount,
            ActualViewportEvent=true,ActualAdapterResult=true,Clock=owned.LastDiagnostic!.Clock,Sent=false}));
        return true;
    }
    private void ClearHostedPointTarget()
    {
        var owner=pointDriver!;var before=owner.State;
        var snapshot=world!.Snapshots().Single(s=>s.Object.Handle.ObjectId==humanId);var motion=motions!.Find(snapshot);
        pointClearedTarget=owner.Target;
        owner.ClearTarget(localClock);
        var after=owner.State;
        Check(pointClearedTarget is not null&&owner.Target is null&&!owner.IsCurrent(pointClearedTarget)
            &&ReferenceEquals(before.DriverResult,after.DriverResult)&&before.SuccessfulTargets==after.SuccessfulTargets
            &&ReferenceEquals(motion,motions.Find(snapshot))&&!owner.IsPaused&&after.Mode==SceneWorldTerrainPointTargetMode.Idle,
            "Clear releases only owned intent and invalidates exact old target");
        GD.Print("point_target_cleared="+JsonSerializer.Serialize(new{AtStep=pointIndex,TimestampMicroseconds=localClock,
            PreviousRevision=pointClearedTarget!.Revision,after.SuccessfulTargets,TargetNull=true,PreviousCurrent=false,
            DriverStatePreserved=true,MotionPreserved=true,Paused=owner.IsPaused,OriginalMovementParity=false}));
    }
    private void ObserveHostedRootMotion()
    {
        if(pointOfflineHosted){ObserveOfflineRootMotion();return;}
        if(pointManualHosted){ObserveManualOutbound();return;}
        if(!pointViewportHosted||!(pointIndex is 4 or 6 or 16 or 21||pointStopStep))return;
        var snapshot=world!.Snapshots().Single(s=>s.Object.Handle.ObjectId==humanId);
        var motion=motions!.Find(snapshot)??throw new InvalidOperationException("Current local motion for outbound fixture");
        var profile=pointProfile!.RootElement;
        uint counter=checked(profile.GetProperty("OutboundCounterBase").GetUInt32()+(uint)pointIndex);
        uint timestamp=checked(profile.GetProperty("OutboundTimestampBase").GetUInt32()+(uint)pointIndex);
        var packet=pointOutboundBuilder!.Build(snapshot,motion,profile.GetProperty("OutboundFlags").GetUInt32(),counter,timestamp);
        try {
            Check(ReferenceEquals(packet.SourceMotion,motion)&&ReferenceEquals(packet.SourceSnapshot,snapshot)&&motions.IsCurrent(motion)
                &&!motion.Authoritative&&packet.Application.Length==62,"Actual local motion and exclusively owned full-root fixture");
            GD.Print("point_outbound_root="+JsonSerializer.Serialize(new{AtStep=pointIndex,Hex=Convert.ToHexString(packet.Application),
                Counter=counter,Timestamp=timestamp,Native=new[]{motion.Transform.Translation.X,motion.Transform.Translation.Y,motion.Transform.Translation.Z},
                motion.Speed,ActualCurrentLocalRow=true,CallerOwnedBytes=true,Sent=false,OriginalSenderParity=false}));
            pointOutboundPackets++;
        } finally {Array.Clear(packet.Application);}
    }
    private void ClosePointInputHost()
    {
        CloseManualInputHost();
        var owned=pointEventHost;pointEventHost=null;
        try {
            if(owned is not null){owned.Close();owned.Close();
                Check(owned.IsClosed&&owned.LastResult is null,"Point event host terminal reference cleanup");
                GD.Print("point_input_host_cleanup="+JsonSerializer.Serialize(new{Closed=true,owned.ClockReadCount,owned.AcceptedCount,owned.HandledCount,
                    owned.IsFaulted,OutboundPackets=pointOutboundPackets,BorrowedAdapterPreserved=pointInputAdapter is {IsDisposed:false},
                    BorrowedPickerPreserved=pointPicker is {IsDisposed:false},BorrowedControllerPreserved=pointDriver is not null&&pointDriver.State.Mode!=SceneWorldTerrainPointTargetMode.Disposed}));
                if(GodotObject.IsInstanceValid(owned)){if(owned.GetParent() is Node parent)parent.RemoveChild(owned);owned.Free();}
            }
        } finally {pointOutboundBuilder?.Dispose();pointOutboundBuilder=null;pointClearedTarget=null;}
    }
}
