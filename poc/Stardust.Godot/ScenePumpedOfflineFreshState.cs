using System;
using System.Text.Json;
using Godot;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

public partial class ScenePumpedWorldPreview
{
    private void RecordOfflineFreshPreviewState()
    {
        if(!pointConfigured)return;
        Check(configuredSessionOwner is not null&&!configuredSessionOwner.IsDisposed&&configuredRootOwner is not null
            &&pointDriver is not null&&offlineSessionController is not null&&offlineRootSession is not null
            &&manualEventHost is not null&&runEventHost is not null&&offlineSessionInput is not null
            &&cameraOrbitHost is not null&&orbitCamera is not null&&offlineFrameClock is not null
            &&pointInputAdapter is not null&&manualOutboundQueue is not null&&runTransport is not null,"Complete actual configured fresh owners");
        var point=pointDriver!.State;var session=offlineSessionController!.State;var frame=offlineFrameClock!.State;
        var roots=offlineRootSession!.State;var profile=pointProfile!.RootElement;
        bool ownedPoint=ReferenceEquals(pointDriver,configuredSessionOwner!.PointDriver);
        bool ownedController=ReferenceEquals(offlineSessionController,configuredSessionOwner.Controller);
        bool ownedRoots=ReferenceEquals(offlineRootSession,configuredRootOwner!.Session);
        bool ownedTransport=ReferenceEquals(runTransport,configuredRootOwner.Transport);
        Check(ownedPoint&&ownedController&&ownedRoots&&ownedTransport&&point.CompletedUpdates==0&&point.AttemptedUpdates==0
            &&session.CompletedUpdates==0&&!session.Paused&&!session.StopRequested&&!session.Stopped&&!session.IsFaulted
            &&point.Mode==SceneWorldTerrainPointTargetMode.Idle&&point.DriverResult.Speed==0&&point.Target is null
            &&pointDriver.ManualInput==System.Numerics.Vector2.Zero&&!pointDriver.IsPaused&&pointIndex==0
            &&localClock==offlineStartClock&&localClock==point.DriverResult.TimestampMicroseconds,"Actual fresh point/controller and clock");
        Check(manualEventHost!.HeldKeyMask==0&&runEventHost!.HeldKeyMask==0&&!runEventHost!.IsRunning
            &&!offlineSessionInput!.FinishRequested&&pointInputAdapter!.Attempts==0
            &&pointInputAdapter!.RemainingAttempts==OfflineMaximumClicks&&offlineFramePlans==0&&offlineClampedMicroseconds==0
            &&frame.Steps==0&&frame.TotalReservedSteps==0&&frame.RemainderMicroseconds==0&&frame.TotalDroppedMicroseconds==0
            &&roots.Samples==0&&roots.Queued==0&&roots.Drained==0&&roots.NextSequence==0&&!roots.IsFaulted
            &&roots.RelativeElapsedMicroseconds==0&&roots.LastNowMilliseconds==-1
            &&manualOutboundQueue!.Count==0&&manualOutboundQueue!.QueuedBytes==0&&manualOutboundSteps.Count==0&&runDatagrams==0
            &&runTransport!.State==SoeClientTransportState.Established&&runTransport!.PendingMessageCount==0&&runTransport!.RetainedOutboundPackets==0,"Actual fresh input/frame/root buffers");
        Check(cameraOrbitHost!.IsConfigured&&!cameraOrbitHost.IsClosed&&!cameraOrbitHost.IsFaulted
            &&ReferenceEquals(cameraOrbitHost.Camera,orbitCamera)&&cameraOrbitHost.AcceptedCount==0&&cameraOrbitHost.HandledCount==0
            &&cameraOrbitHost.Yaw==Math.IEEERemainder(profile.GetProperty("OrbitYaw").GetDouble(),2*Math.PI)
            &&cameraOrbitHost.Pitch==Math.Clamp(profile.GetProperty("OrbitPitch").GetDouble(),profile.GetProperty("OrbitMinimumPitch").GetDouble(),profile.GetProperty("OrbitMaximumPitch").GetDouble())
            &&cameraOrbitHost.Distance==Math.Clamp(profile.GetProperty("OrbitDistance").GetDouble(),profile.GetProperty("OrbitMinimumDistance").GetDouble(),profile.GetProperty("OrbitMaximumDistance").GetDouble()),"Actual configured orbit reset seeds");
        GD.Print("offline_fresh_preview_state="+JsonSerializer.Serialize(new{
            SchemaVersion=1,PreviewInstanceId=GetInstanceId(),CameraInstanceId=orbitCamera!.GetInstanceId(),SceneGeneration=generation,
            OwnedPoint=ownedPoint,OwnedController=ownedController,OwnedRoots=ownedRoots,OwnedTransport=ownedTransport,
            point.CompletedUpdates,point.AttemptedUpdates,PointIndex=pointIndex,Mode=point.Mode.ToString(),point.DriverResult.Speed,
            LocalClock=localClock,OfflineStartClock=offlineStartClock,DriverClock=point.DriverResult.TimestampMicroseconds,
            ManualHeld=manualEventHost!.HeldKeyMask,RunHeld=runEventHost!.HeldKeyMask,Running=runEventHost!.IsRunning,
            Paused=session.Paused,FinishRequested=offlineSessionInput!.FinishRequested,TargetActive=point.Target is not null,
            ManualX=pointDriver.ManualInput.X,ManualZ=pointDriver.ManualInput.Y,ClickAttempts=pointInputAdapter!.Attempts,ClickRemaining=pointInputAdapter!.RemainingAttempts,
            FramePlans=offlineFramePlans,ClampedMicroseconds=offlineClampedMicroseconds,FrameSteps=frame.Steps,ReservedSteps=frame.TotalReservedSteps,
            RemainderMicroseconds=frame.RemainderMicroseconds,DroppedMicroseconds=frame.TotalDroppedMicroseconds,
            OrbitYaw=cameraOrbitHost.Yaw,OrbitPitch=cameraOrbitHost.Pitch,OrbitDistance=cameraOrbitHost.Distance,
            OrbitSeedYaw=profile.GetProperty("OrbitYaw").GetDouble(),OrbitSeedPitch=profile.GetProperty("OrbitPitch").GetDouble(),OrbitSeedDistance=profile.GetProperty("OrbitDistance").GetDouble(),
            OrbitAccepted=cameraOrbitHost.AcceptedCount,OrbitHandled=cameraOrbitHost.HandledCount,
            RootSamples=roots.Samples,RootQueued=roots.Queued,RootDrained=roots.Drained,RootNextSequence=roots.NextSequence,
            RootRelativeMicroseconds=roots.RelativeElapsedMicroseconds,RootLastNowMilliseconds=roots.LastNowMilliseconds,
            QueueCount=manualOutboundQueue!.Count,QueueBytes=manualOutboundQueue!.QueuedBytes,OutboundStepCount=manualOutboundSteps.Count,
            TransportState=runTransport!.State.ToString(),PendingMessages=runTransport!.PendingMessageCount,RetainedPackets=runTransport!.RetainedOutboundPackets,
            RunDatagrams=runDatagrams,SocketCreated=false}));
    }
}
