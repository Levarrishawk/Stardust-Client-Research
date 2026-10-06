using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

public partial class ScenePumpedWorldPreview
{
    private bool pointRequested,pointStarted,pointComplete,pointClosed,pointStopStep;
    private JsonDocument? pointProfile;
    private SceneWorldTerrainPointTargetDriver? pointDriver;
    private SceneWorldFullTransformApplicationRouter? pointRouter;
    private string? pointFingerprint;
    private int pointIndex;
    private static float PointFloat(JsonElement profile,string name)=>BitConverter.UInt32BitsToSingle(profile.GetProperty(name).GetUInt32());
    private void LoadPointTargetArguments(string[] args)
    {
        int index=Array.IndexOf(args,"--terrain-point-target-profile");if(index<0)return;
        Check(index+1<args.Length&&args.Count(a=>a=="--terrain-point-target-profile")==1
            &&args.Contains("--local-input")&&args.Contains("--terrain-peer")&&args.Contains("--creature4-partial-peer")
            &&!nominalRequested&&!sourcedRequested&&!args.Contains("--input-loss")&&!args.Contains("--pump-cancel"),"Explicit bounded terrain point mode");
        using var stream=File.OpenRead(args[index+1]);
        Check(stream.Length is >0 and <=16384,"Bounded point policy");
        pointProfile=JsonDocument.Parse(stream,new JsonDocumentOptions{MaxDepth=4});
        Check(stream.Position==stream.Length,"Complete point policy input");
        var root=pointProfile.RootElement;
        pointConfigured=root.GetProperty("Version").GetInt32()==13&&root.GetProperty("Policy").GetString()=="SuppliedConfiguredOfflinePlayground";
        if(pointConfigured)offlineSettings=TerrainOfflinePlaygroundSettings.Parse(System.Text.Encoding.UTF8.GetBytes(root.GetProperty("PlaygroundSettings").GetRawText()));
        pointInteractive=pointConfigured||root.GetProperty("Version").GetInt32()==12&&root.GetProperty("Policy").GetString()=="SuppliedIdleOfflinePlayground";
        if(pointInteractive){_ = root.GetProperty("InteractiveReplay").GetBoolean();Check(!root.GetProperty("SyntheticFrameClock").GetBoolean()||root.GetProperty("InteractiveReplay").GetBoolean(),"Synthetic frame timing requires explicit replay");}
        pointSessionControlled=pointInteractive||root.GetProperty("Version").GetInt32()==11&&root.GetProperty("Policy").GetString()=="SuppliedOfflineSessionControls";
        if(pointSessionControlled&&!pointInteractive){Check(root.GetProperty("SessionStopAtStep").GetInt32()==600,"Bounded actual session input replay");_ = root.GetProperty("InvalidSessionInput").GetBoolean();}
        pointElapsedHosted=pointSessionControlled||root.GetProperty("Version").GetInt32()==10&&root.GetProperty("Policy").GetString()=="SuppliedElapsedOfflinePlayground";
        if(pointElapsedHosted){_ = root.GetProperty("SyntheticFrameClock").GetBoolean();_ = root.GetProperty("InvalidOfflineDrainAck").GetBoolean();}
        pointOfflineHosted=pointElapsedHosted||root.GetProperty("Version").GetInt32()==9&&root.GetProperty("Policy").GetString()=="SuppliedLongOfflinePlayground";
        Check(pointOfflineHosted||stream.Length<=4096,"Prior bounded profile admission");
        if(pointOfflineHosted)offlineSessionBudget=pointConfigured?offlineSettings!.CreateBudget():new(OfflineMaximumUpdates,16);
        pointCameraFollowHosted=pointOfflineHosted||root.GetProperty("Version").GetInt32()==8&&root.GetProperty("Policy").GetString()=="SuppliedFollowingCameraAndOuterPeerDisconnect";
        pointAcceptedLocomotionHosted=pointCameraFollowHosted||root.GetProperty("Version").GetInt32()==7&&root.GetProperty("Policy").GetString()=="SuppliedAcceptedLocomotionAndOfflinePeerDisconnect";
        pointCameraRelativeHosted=pointAcceptedLocomotionHosted||root.GetProperty("Version").GetInt32()==6&&root.GetProperty("Policy").GetString()=="SuppliedCameraRelativeOrbitAndOfflineSoeIngress";
        pointRunHosted=pointCameraRelativeHosted||root.GetProperty("Version").GetInt32()==5&&root.GetProperty("Policy").GetString()=="SuppliedViewportWalkRunAndOfflineSoeRootTransfer";
        pointManualHosted=pointRunHosted||root.GetProperty("Version").GetInt32()==4&&root.GetProperty("Policy").GetString()=="SuppliedViewportManualPointTargetAndOwnedOfflineQueue";
        pointViewportHosted=pointManualHosted||root.GetProperty("Version").GetInt32()==3&&root.GetProperty("Policy").GetString()=="SuppliedViewportPointTargetClearAndOfflineRootMotion";
        pointAdvanced=pointViewportHosted||root.GetProperty("Version").GetInt32()==2&&root.GetProperty("Policy").GetString()=="SuppliedTerrainInputPausedTargetWithQueuedFullRootCorrection";
        Check((pointAdvanced||(root.GetProperty("Version").GetInt32()==1&&root.GetProperty("Policy").GetString()=="SuppliedTerrainCameraPointTargetWithFullRootCorrection"))
            &&!root.GetProperty("OriginalMovementParity").GetBoolean()&&root.GetProperty("StepMicroseconds").GetInt32()==10000
            &&root.GetProperty("MaximumUpdates").GetInt32()==OfflineMaximumUpdates&&root.GetProperty("CorrectionAtStep").GetInt32()==20,"Explicit modern point and correction policy");
        foreach(string key in new[]{"TargetNativeXBits","TargetNativeZBits","SpeedBits","ArrivalRadiusBits","CorrectionXBits","CorrectionZBits"})
            Check(float.IsFinite(PointFloat(root,key)),"Finite supplied point policy");
        if(pointAdvanced){
            Check(root.GetProperty("PauseAtStep").GetInt32()==5&&root.GetProperty("ResumeAtStep").GetInt32()==8
                &&root.GetProperty("RetargetAtStep").GetInt32()==10,"Explicit bounded activity schedule");
            Check(float.IsFinite(PointFloat(root,"RetargetNativeXBits"))&&float.IsFinite(PointFloat(root,"RetargetNativeZBits")),"Finite retarget");
            _=root.GetProperty("MalformedCorrection").GetBoolean();
        }
        if(pointViewportHosted){
            Check(root.GetProperty("ClearAtStep").GetInt32()==15&&root.GetProperty("ResumeTargetAtStep").GetInt32()==18
                &&root.GetProperty("OutboundFlags").GetUInt32()==0x1B&&root.GetProperty("OutboundCounterBase").GetUInt32()==1000
                &&root.GetProperty("OutboundTimestampBase").GetUInt32()==0xF1234567&&!root.GetProperty("OriginalSenderParity").GetBoolean(),"Explicit clear and offline root policy");
            _=root.GetProperty("InvalidHostedClock").GetBoolean();
        }
        if(pointManualHosted){
            Check(root.GetProperty("ManualAtStep").GetInt32()==25&&root.GetProperty("ManualPointResumeAtStep").GetInt32()==35,
                "Explicit bounded manual schedule");
            _=root.GetProperty("InvalidManualClock").GetBoolean();
        }
        if(pointRunHosted){
            Check((pointConfigured||root.GetProperty("WalkSpeedBits").GetUInt32()==0x40400000&&root.GetProperty("RunSpeedBits").GetUInt32()==0x40800000)
                &&root.GetProperty("SoeConnectionId").GetUInt32()==0xF00D00DB&&root.GetProperty("SoeSeed").GetUInt32()==0xA1B2C3D4
                &&root.GetProperty("SoeMaximumDatagram").GetInt32()==496&&root.GetProperty("SoeCrcWidth").GetInt32()==2
                &&(pointInteractive||root.GetProperty("SoeAckHex").GetArrayLength()==9),"Explicit supplied speed and synthetic SOE policy");
            _=root.GetProperty("InvalidRunClock").GetBoolean();
        }
        if(pointConfigured)Check(PointFloat(root,"WalkSpeedBits")==offlineSettings!.WalkSpeed&&PointFloat(root,"RunSpeedBits")==offlineSettings.RunSpeed
            &&PointFloat(root,"SpeedBits")==offlineSettings.WalkSpeed&&PointFloat(root,"ArrivalRadiusBits")==offlineSettings.ArrivalRadius,"Typed settings and adapter fields agree");
        pointRequested=true;
    }
    private TerrainConfiguredOfflineSession? configuredSessionOwner;
    private void BeginPointTarget()
    {
        Check(pointRequested&&!pointStarted&&phase==47&&calls==3&&closed==3&&inputHost!.LastState!.Speed==0,"Point target follows completed offline host gates");
        var root=pointProfile!.RootElement;
        if(!pointAdvanced)SelectPointTarget(PointFloat(root,"TargetNativeXBits"),PointFloat(root,"TargetNativeZBits"));
        float speed=PointFloat(root,"SpeedBits"),radius=PointFloat(root,"ArrivalRadiusBits");
        Check(speed is >0 and <=10&&radius is >=1e-4f and <=1,"Bounded supplied modern speed and arrival radius");
        inputHost!.Close();inputHost=null;
        pointRouter=new(world!,motions!,SceneCreateZeroQuaternionPolicy.Identity);
        if(pointConfigured){
            configuredSessionOwner=new(world!,motions!,humanId,suppliedTerrainRayQuery!,offlineSettings!,localClock);
            pointDriver=configuredSessionOwner.PointDriver;offlineSessionBudget=configuredSessionOwner.Budget;
            GD.Print("configured_session_owner_ready="+JsonSerializer.Serialize(new{MaximumUpdates=offlineSettings!.MaximumUpdates,MaximumClicks=offlineSettings.MaximumClickAttempts,
                ActualOwnedPointDriver=ReferenceEquals(pointDriver,configuredSessionOwner.PointDriver),MatchingSettings=ReferenceEquals(offlineSettings,configuredSessionOwner.Settings),SocketCreated=false}));
        }else pointDriver=new(world!,motions!,humanId,suppliedTerrainRayQuery!,speed,radius,localClock,10000,maximumUpdates:OfflineMaximumUpdates,maximumTargets:OfflineMaximumClicks,budget:offlineSessionBudget,holdMissingTerrainDestination:pointInteractive);
        PrepareAcceptedLocomotion();PrepareCameraFollow();
        SceneWorldTerrainPointTargetReceipt? target=null;
        if(pointInteractive){pointInputAdapter=pointConfigured?new(pointPicker!,pointDriver,OfflineMaximumClicks,offlineSessionBudget,true):new(pointPicker!,pointDriver);PreparePointInputHost();}
        else if(pointAdvanced){pointInputAdapter=new(pointPicker!,pointDriver);PreparePointInputHost();SelectAdaptedPointTarget(PointFloat(root,"TargetNativeXBits"),PointFloat(root,"TargetNativeZBits"));target=pointInputResult!.Target!;}
        else target=pointDriver.SubmitTarget(pointPick!.Hit!,localClock);
        if(!pointInteractive)Check(pointDriver.IsCurrent(target!)&&ReferenceEquals(target!.Hit,pointPick!.Hit)&&ReferenceEquals(target.Query,suppliedTerrainRayQuery),"Actual picked target receipt");
        pointFingerprint=visuals[humanId].CreatureVisual!.Fingerprint();
        pointStarted=true;phase=72;PrepareOfflinePlayground();PrepareOfflineSessionControls();
        var start=pointDriver.State.DriverResult.Transform.Translation;
        GD.Print("point_target_ready="+JsonSerializer.Serialize(new{Start=new[]{start.X,start.Y,start.Z},Target=target is null?null:new[]{target.X,target.Z},Speed=speed,Radius=radius,
            TimestampMicroseconds=localClock,StepMicroseconds=10000,MaximumUpdates=OfflineMaximumUpdates,calls,closed,ActualTarget=target is not null,OriginalMovementParity=false}));
    }
    private void ApplyPointCorrection()
    {
        var root=pointProfile!.RootElement;float x=PointFloat(root,"CorrectionXBits"),z=PointFloat(root,"CorrectionZBits");
        var ground=suppliedTerrainSetQuery!.Query(x,z)??throw new InvalidOperationException("Correction outside accepted terrain");
        var raw=ObjectControllerTransformMessage.ServerToClient(ObjectControllerTransformKind.Root,0x1B,humanId,0,
            root.GetProperty("CorrectionCounter").GetUInt32(),null,new System.Numerics.Vector4(0,0,0,1),new(x,(float)ground.Hit.Height,z),0);
        byte[] frame=ObjectControllerTransformCodec.Encode(raw);
        try {
            var applied=pointRouter!.Accept(generation,frame,ObjectControllerTransformDirection.ServerToClient);
            Check(applied.Applied&&ReferenceEquals(applied.Motion,motions!.Find(applied.Snapshot!)),"Actual typed full root correction publication");
            var duplicate=pointRouter.Accept(generation,frame,ObjectControllerTransformDirection.ServerToClient);
            Check(duplicate.Status==SceneWorldFullTransformApplicationStatus.CounterRejected&&ReferenceEquals(duplicate.Motion,applied.Motion),"Duplicate correction preserves actual row");
            GD.Print("point_full_correction="+JsonSerializer.Serialize(new{AtStep=pointIndex,Hex=Convert.ToHexString(frame),Native=new[]{x,(float)ground.Hit.Height,z},
                Counter=raw.MovementCounter,Applied=true,DuplicateRejected=true,ActualMotion=true,Sent=false,OriginalMovementParity=false}));
        } finally {Array.Clear(frame);}
    }
    private void SetPointPaused(bool paused)
    {
        var before=pointDriver!.State;
        var snapshot=world!.Snapshots().Single(s=>s.Object.Handle.ObjectId==humanId);
        var row=motions!.Find(snapshot);
        pointDriver.SetPaused(paused,localClock);
        var after=pointDriver.State;
        Check(ReferenceEquals(before.DriverResult,after.DriverResult)&&ReferenceEquals(before.Target,after.Target)
            &&before.CompletedUpdates==after.CompletedUpdates&&ReferenceEquals(row,motions.Find(snapshot))&&pointDriver.IsPaused==paused,
            "Pause setter changes owned intent without driver or store effects");
        GD.Print("point_activity_intent="+JsonSerializer.Serialize(new{AtStep=pointIndex,Paused=paused,TimestampMicroseconds=localClock,
            MotionPreserved=true,DriverStatePreserved=true,TargetCurrent=pointDriver.IsCurrent(pointDriver.Target!),before.DriverResult.Speed}));
    }
    private void AdvancePointTarget()
    {
        if(!pointStarted||pointComplete||finishing)return;
        if(!pointInteractive&&pointAdvanced&&pointIndex==20&&!pointCorrectionQueued){QueuePointCorrection();return;}
        if(!pointAdvanced&&pointIndex==20)ApplyPointCorrection();
        if(pointInteractive)AdvanceOfflineInteractive();
        else if(pointSessionControlled)AdvanceOfflineSessionInputSchedule();
        else if(pointAdvanced&&(pointIndex==5||pointIndex==8))SetPointPaused(pointIndex==5);
        if(!pointInteractive&&pointAdvanced&&pointIndex==10)SelectAdaptedPointTarget(PointFloat(pointProfile!.RootElement,"RetargetNativeXBits"),PointFloat(pointProfile.RootElement,"RetargetNativeZBits"));
        if(!pointInteractive&&pointViewportHosted&&pointIndex==15)ClearHostedPointTarget();
        if(!pointInteractive&&pointViewportHosted&&pointIndex==18)SelectAdaptedPointTarget(PointFloat(pointProfile!.RootElement,"RetargetNativeXBits"),PointFloat(pointProfile.RootElement,"RetargetNativeZBits"));
        if(!pointInteractive){AdvanceManualInputSchedule();AdvanceOfflinePlaygroundSchedule();}
        pointStopStep=pointDriver!.State.Mode==SceneWorldTerrainPointTargetMode.Arrived;
        long next=checked(localClock+10000);
        var state=pointSessionControlled?offlineSessionController!.AdvanceNext(localClock,next).DriverResult:pointDriver.AdvanceNext(localClock,next);
        Check((state.Mode is SceneWorldTerrainPointTargetMode.Moving or SceneWorldTerrainPointTargetMode.Arrived or SceneWorldTerrainPointTargetMode.Paused
            ||pointViewportHosted&&state.Mode==SceneWorldTerrainPointTargetMode.Idle
            ||pointManualHosted&&state.Mode==SceneWorldTerrainPointTargetMode.Manual)
            &&state.DriverResult.FaultType is null&&state.DriverResult.StepsThisUpdate==1,"Healthy consecutive grounded point step");
        localClock=next;AdvanceAcceptedLocomotion();AdvanceCameraFollow();locomotion!.SetPlaying(state.DriverResult.Speed>0);
    }
    private void FramePointTarget()
    {
        var state=pointDriver!.State;var driver=state.DriverResult;var p=driver.Transform.Translation;
        var visual=visuals[humanId].CreatureVisual!;var engine=visual.Root.Position;
        Check(engine.DistanceTo(new Vector3(p.X,p.Y,-p.Z))<2e-5f&&visual.Fingerprint()==pointFingerprint
            &&ReferenceEquals(visuals[humanId].Publication,originalHuman)&&calls==3&&closed==3,"Point native engine and original visual retention");
        GD.Print("point_target_state="+JsonSerializer.Serialize(new{Index=pointIndex,Mode=state.Mode.ToString(),state.AttemptedUpdates,state.CompletedUpdates,
            Native=new[]{p.X,p.Y,p.Z},Engine=new[]{engine.X,engine.Y,engine.Z},driver.Speed,driver.TimestampMicroseconds,driver.StepsThisUpdate,
            driver.DroppedMicroseconds,driver.SuspendedMicroseconds,driver.FaultType,StopStep=pointStopStep,PublicationRetained=true,VisualRetained=true,calls,closed,
            OriginalMovementParity=false}));
        FrameAcceptedLocomotion();FrameCameraFollow();
        ObserveHostedRootMotion();FrameOfflinePlayground();FrameOfflineInteractive();
        pointIndex++;
        if(pointSessionControlled&&offlineSessionController!.State.Stopped){FinishOfflineSessionControls(state);return;}
        if(pointOfflineHosted){if(pointIndex==OfflineMaximumUpdates){Check(driver.Speed==0&&state.Mode==SceneWorldTerrainPointTargetMode.Idle&&state.Target is null,"Bounded long offline session terminal idle");AcceptFinalPeerDisconnect();pointComplete=true;phase=73;ScheduleFinish();}return;}
        if(pointStopStep){Check(driver.Speed==0&&state.Mode==SceneWorldTerrainPointTargetMode.Arrived,"Explicit consecutive zero velocity stop");
            locomotion!.SetPlaying(false);AcceptFinalPeerDisconnect();pointComplete=true;phase=73;ScheduleFinish();}
    }
    private void ClosePointTarget()
    {
        if(pointClosed)return;pointClosed=true;
        try {
            CloseOfflineSessionControls();CloseOfflinePlayground();CloseCameraFollow();CloseAcceptedLocomotion();
            ClosePointInputAdapter();
            if(pointDriver is not null){var owned=pointDriver;var driver=owned.Driver!;pointDriver=null;
                if(configuredSessionOwner is not null){var owner=configuredSessionOwner;configuredSessionOwner=null;owner.Dispose();owner.Dispose();
                    Check(owner.IsDisposed&&owned.State.Mode==SceneWorldTerrainPointTargetMode.Disposed,"Configured owner drains its actual point driver");
                    GD.Print("configured_session_owner_cleanup="+JsonSerializer.Serialize(new{Disposed=owner.IsDisposed,OwnedPointDisposed=true,OwnedControllerDisposed=true,BorrowedWorldPreserved=true,BorrowedTerrainPreserved=true}));
                }else{owned.Dispose();owned.Dispose();}
                Check(owned.State.Mode==SceneWorldTerrainPointTargetMode.Disposed&&!driver.State.Active,"Point owner drains actual driver");
                if(world!.State==ZoneSceneTransitionState.Closed)
                    GD.Print("point_target_closed_world_cleanup="+JsonSerializer.Serialize(new{Disposed=true,Updates=pointIndex,DriverInactive=true,WorldAlreadyClosedByPump=true,
                        BorrowedTerrainPreserved=suppliedTerrainSetQuery is {PatchCount:16}}));
                else GD.Print("point_target_cleanup="+JsonSerializer.Serialize(new{Disposed=true,Updates=pointIndex,BorrowedWorldPreserved=true,
                    BorrowedStorePreserved=motions!.Find(world.Snapshots().Single(s=>s.Object.Handle.ObjectId==humanId)) is not null,
                    BorrowedTerrainPreserved=suppliedTerrainSetQuery is {PatchCount:16}}));}
        } finally {CloseCombinedMotionIngress();pointRouter?.Dispose();pointRouter=null;ClosePointPicker();pointProfile?.Dispose();pointProfile=null;}
    }
}
