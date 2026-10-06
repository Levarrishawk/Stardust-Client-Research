using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using Stardust.Core.Session;
using NativeVector2=System.Numerics.Vector2;

public partial class ScenePumpedWorldPreview
{
    private bool pointManualHosted;
    private TerrainManualInputHost? manualEventHost;
    private TerrainManualInputGuiProbe? manualGui;
    private SceneWorldClientRootMotionQueue? manualOutboundQueue;
    private readonly Queue<int> manualOutboundSteps=new();
    private int manualTransferred;
    private void PrepareManualInputHost()
    {
        if(!pointManualHosted)return;
        manualEventHost=new TerrainManualInputHost{Name="OwnedManualInputHost"};AddChild(manualEventHost);
        manualEventHost.Configure(()=>pointProfile!.RootElement.GetProperty("InvalidManualClock").GetBoolean()&&pointIndex==25?checked(localClock+1):localClock,
            (direction,clock)=>PublishCameraRelativeManual(direction,clock));
        manualGui=new TerrainManualInputGuiProbe{Name="OwnedManualGuiProbe",FocusMode=Control.FocusModeEnum.All,MouseFilter=Control.MouseFilterEnum.Ignore,Position=new Vector2(8,8),Size=new Vector2(16,16)};
        AddChild(manualGui);
        manualOutboundQueue=new(world!,motions!,new SceneWorldClientRootMotionPolicy(1000000,10),8,496);
        using var unrelated=new InputEventKey{PhysicalKeycode=Key.F,Pressed=true};
        using var echo=new InputEventKey{PhysicalKeycode=Key.W,Pressed=true,Echo=true};
        GetViewport().PushInput(unrelated,true);GetViewport().PushInput(echo,true);
        Check(manualEventHost.ClockReadCount==0&&manualEventHost.AcceptedCount==0,"Unrelated and echo viewport keys have no sink effects");
        PrepareRunInputHost();
    }
    private void PublishManualDiagnostic(bool fromEvent,SceneWorldTerrainPointTargetState before,SceneWorldObjectMotionSnapshot? motion)
    {
        var owned=manualEventHost!;
        var snapshot=world!.Snapshots().Single(s=>s.Object.Handle.ObjectId==humanId);
        Check(ReferenceEquals(before.DriverResult,pointDriver!.State.DriverResult)&&ReferenceEquals(motion,motions!.Find(snapshot)),"Manual intent setter preserves driver and motion");
        GD.Print("manual_input_intent="+JsonSerializer.Serialize(new{AtStep=pointIndex,Direction=new[]{pointDriver.ManualInput.X,pointDriver.ManualInput.Y},
            TimestampMicroseconds=localClock,owned.ClockReadCount,owned.AcceptedCount,ActualViewportEvent=fromEvent,
            DriverStatePreserved=true,MotionPreserved=true,TargetNull=pointDriver.Target is null,OriginalMovementParity=false}));
    }
    private void PushManualKey(Key key,bool pressed)
    {
        var owned=manualEventHost!;var before=pointDriver!.State;
        var snapshot=world!.Snapshots().Single(s=>s.Object.Handle.ObjectId==humanId);var motion=motions!.Find(snapshot);
        int handled=owned.HandledCount;
        using var input=new InputEventKey{PhysicalKeycode=key,Keycode=key,Pressed=pressed};
        GetViewport().PushInput(input,true);
        if(owned.LastFailure is not null){
            GD.Print("manual_input_host_failure="+JsonSerializer.Serialize(new{AtStep=pointIndex,Failure=owned.LastFailure,owned.IsFaulted,
                owned.ClockReadCount,owned.AcceptedCount,owned.HandledCount,SinkInvoked=owned.LastDiagnostic!.SinkInvoked,
                SinkReturned=owned.LastDiagnostic.SinkReturned,StateCommitted=owned.LastDiagnostic.StateCommitted,
                ActualViewportEvent=owned.LastDiagnostic.FromGodotEvent,TargetPreserved=ReferenceEquals(before.Target,pointDriver.Target),
                ManualInputZero=pointDriver.ManualInput==NativeVector2.Zero,DriverStatePreserved=ReferenceEquals(before.DriverResult,pointDriver.State.DriverResult)}));
            throw new InvalidOperationException("Hosted manual input failed: "+owned.LastFailure);
        }
        Check(owned.HandledCount==handled+1&&owned.LastDiagnostic?.FromGodotEvent==true&&MapCameraRelativeInput(owned.CurrentInput)==pointDriver.ManualInput,"Actual viewport key publishes exact native manual intent");
        PublishManualDiagnostic(true,before,motion);
    }
    private void AdvanceManualInputSchedule()
    {
        if(!pointManualHosted)return;
        AdvanceCameraOrbitSchedule();
        RefreshCameraRelativeManual();
        AdvanceRunInputSchedule();
        switch(pointIndex){
            case 25:PushManualKey(Key.W,true);break;
            case 27:PushManualKey(Key.D,true);break;
            case 28:PushManualKey(Key.W,false);break;
            case 29:PushManualKey(Key.D,false);break;
            case 30:PushManualKey(Key.S,true);break;
            case 31:
                var before=pointDriver!.State;var snapshot=world!.Snapshots().Single(s=>s.Object.Handle.ObjectId==humanId);var row=motions!.Find(snapshot);
                Check(manualEventHost!.SetEnabled(false),"Disable held gameplay input before GUI focus");PublishManualDiagnostic(false,before,row);
                manualGui!.GrabFocus();Check(manualGui.HasFocus(),"Actual GUI key focus");
                int clocks=manualEventHost.ClockReadCount,accepted=manualEventHost.AcceptedCount;
                using(var press=new InputEventKey{PhysicalKeycode=Key.A,Keycode=Key.A,Pressed=true})GetViewport().PushInput(press,true);
                using(var release=new InputEventKey{PhysicalKeycode=Key.A,Keycode=Key.A,Pressed=false})GetViewport().PushInput(release,true);
                Check(manualGui.Events==2&&manualEventHost.ClockReadCount==clocks&&manualEventHost.AcceptedCount==accepted&&pointDriver.ManualInput==NativeVector2.Zero,"Actual GUI consumes keys while manual intent remains stopped");
                GD.Print("manual_gui_consumed="+JsonSerializer.Serialize(new{AtStep=pointIndex,GuiEvents=manualGui.Events,manualEventHost.ClockReadCount,
                    manualEventHost.AcceptedCount,manualEventHost.HandledCount,IntentZero=true,ActualFocusedControl=true}));break;
            case 32:manualGui!.ReleaseFocus();Check(manualEventHost!.SetEnabled(true),"Resume empty gameplay key set");PushManualKey(Key.D,true);break;
            case 34:PushManualKey(Key.D,false);break;
            case 35:SelectAdaptedPointTarget(PointFloat(pointProfile!.RootElement,"RetargetNativeXBits"),PointFloat(pointProfile.RootElement,"RetargetNativeZBits"));break;
        }
    }
    private void ObserveManualOutbound()
    {
        if(!(pointIndex is 4 or 6 or 16 or 21 or 26 or 28 or 31 or 33||pointStopStep))return;
        var snapshot=world!.Snapshots().Single(s=>s.Object.Handle.ObjectId==humanId);var motion=motions!.Find(snapshot)!;var profile=pointProfile!.RootElement;
        manualOutboundQueue!.Enqueue(snapshot,motion,profile.GetProperty("OutboundFlags").GetUInt32(),
            checked(profile.GetProperty("OutboundCounterBase").GetUInt32()+(uint)pointIndex),checked(profile.GetProperty("OutboundTimestampBase").GetUInt32()+(uint)pointIndex));
        manualOutboundSteps.Enqueue(pointIndex);
        GD.Print("manual_outbound_queued="+JsonSerializer.Serialize(new{AtStep=pointIndex,manualOutboundQueue.Count,manualOutboundQueue.QueuedBytes,ActualCurrentLocalRow=motions.IsCurrent(motion),Sent=false}));
        if(pointIndex is 16 or 31||pointStopStep)DrainManualOutbound();
    }
    private void DrainManualOutbound()
    {
        if(pointRunHosted){DrainRunTransport();return;}
        while(manualOutboundQueue!.TryTake(generation,out var bytes)){
            try {
                int index=manualOutboundSteps.Dequeue();
                var raw=Stardust.Core.Protocol.ObjectControllerTransformCodec.Decode(bytes!,Stardust.Core.Protocol.ObjectControllerTransformDirection.ClientToServer);
                GD.Print("point_outbound_root="+JsonSerializer.Serialize(new{AtStep=index,ReadAtStep=pointIndex,Hex=Convert.ToHexString(bytes!),
                    Counter=raw.MovementCounter,Timestamp=raw.Timestamp,Native=new[]{raw.PositionX,raw.PositionY,raw.PositionZ},
                    raw.Speed,ActualIssuedLocalRowAtEnqueue=true,HistoricalOwnedFrame=true,CallerOwnedBytes=true,TransferredOwnedQueueBytes=true,Sent=false,OriginalSenderParity=false}));
                pointOutboundPackets++;manualTransferred++;
            }finally{Array.Clear(bytes!);}
        }
        Check(manualOutboundSteps.Count==0&&manualOutboundQueue.Count==0&&manualOutboundQueue.QueuedBytes==0,"Offline FIFO arrays transferred and caller cleared");
    }
    private void CloseManualInputHost()
    {
        CloseRunInputHost();
        var owned=manualEventHost;manualEventHost=null;
        if(owned is not null){owned.Close();owned.Close();
            if(manualGui is not null&&GodotObject.IsInstanceValid(manualGui)){RemoveChild(manualGui);manualGui.Free();}manualGui=null;
            GD.Print("manual_input_host_cleanup="+JsonSerializer.Serialize(new{Closed=owned.IsClosed,owned.ClockReadCount,owned.AcceptedCount,owned.HandledCount,owned.IsFaulted,
                IntentSinkBorrowed=true,ControllerPreserved=pointDriver is not null&&pointDriver.State.Mode!=SceneWorldTerrainPointTargetMode.Disposed,GuiFreed=true}));
            if(GodotObject.IsInstanceValid(owned)){if(owned.GetParent() is Node parent)parent.RemoveChild(owned);owned.Free();}
        }
        if(manualOutboundQueue is not null){var queue=manualOutboundQueue;manualOutboundQueue=null;int pending=queue.Count,bytes=queue.QueuedBytes;
            queue.Dispose();queue.Dispose();manualOutboundSteps.Clear();
            GD.Print("manual_outbound_queue_cleanup="+JsonSerializer.Serialize(new{Disposed=true,PendingBefore=pending,QueuedBytesBefore=bytes,queue.Count,queue.QueuedBytes,
                Transferred=manualTransferred,OwnsBorrowedWorld=false,OwnsBorrowedStore=false,WorldAlreadyClosed=world!.State==ZoneSceneTransitionState.Closed,Sent=false}));
        }
    }
}
