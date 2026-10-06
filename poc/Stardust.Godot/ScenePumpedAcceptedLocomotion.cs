using System;
using System.Linq;
using System.Text.Json;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Session;

public partial class ScenePumpedWorldPreview
{
    private bool pointAcceptedLocomotionHosted;
    private SceneWorldTerrainLocomotionObserver? acceptedLocomotionObserver;
    private SceneWorldTerrainLocomotionReceipt? acceptedLocomotionReceipt;
    private SceneWorldAcceptedLocomotionVisual? acceptedLocomotionVisual;
    private LocomotionPreviewState? acceptedLocomotionState;
    private int acceptedLocomotionEvents,acceptedLocomotionRebases;

    private ISceneWorldObjectPresentation WrapAcceptedLocomotion(ISceneWorldObjectPresentation ownedPresentation,SceneWorldObjectMotionVisual motion)
    {
        if(!pointAcceptedLocomotionHosted)return ownedPresentation;
        acceptedLocomotionVisual=new(world!,motion,()=>acceptedLocomotionState??throw new InvalidOperationException("Observer state required after activation"),
            ownedPresentation,()=>pointStarted);
        return acceptedLocomotionVisual;
    }
    private void PrepareAcceptedLocomotion()
    {
        if(!pointAcceptedLocomotionHosted)return;
        var creature=originalHuman!.Resources.Creature!.Creature;
        var idle=creature.PreparedAnimation!;var walk=creature.AdditionalAnimations!["walk"];var run=creature.AdditionalAnimations["run"];
        var idleEvents=CompressedLocomotion.Create(CompressedKeyframeDecoder.Create(creature.SelectedAnimation!.Clip));
        float threshold=pointProfile!.RootElement.GetProperty("LocomotionRunThreshold").GetSingle();
        Check(threshold==3.9f&&idle.ClipSha256=="798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B"
            &&walk.ClipSha256=="FF86567A4CDBFC1EB574C5C8FC399144A37DAD218AA49DE0070AC9F1F19F65A6"
            &&run.ClipSha256=="72D5842D2959D8438884B44F4220F3E323D221A745D849CB8726E905A8AAF2F4","Explicit pinned original locomotion records");
        acceptedLocomotionObserver=new(world!,motions!,pointDriver!.Driver!,10000,
            LocomotionPreviewClock.Describe(idle),LocomotionPreviewClock.Describe(walk),LocomotionPreviewClock.Describe(run),threshold,
            idleEvents,walk.Locomotion!,run.Locomotion!,OfflineMaximumUpdates,4,16,budget:offlineSessionBudget);
        acceptedLocomotionState=acceptedLocomotionObserver.State.Locomotion;
        GD.Print("accepted_locomotion_ready="+JsonSerializer.Serialize(new{Idle=idle.ClipPath,IdleSha256=idle.ClipSha256,Walk=walk.ClipPath,
            WalkSha256=walk.ClipSha256,Run=run.ClipPath,RunSha256=run.ClipSha256,RunThreshold=threshold,StepMicroseconds=10000,
            ActualOwnedPointDriver=true,OriginalAssetRecords=true,ModernScheduling=true,RootMotionApplied=false}));
    }
    private void RebaseAcceptedLocomotion(SceneWorldObjectMotionSnapshot authoritative)
    {
        if(!pointAcceptedLocomotionHosted)return;
        var before=acceptedLocomotionObserver!.State;
        acceptedLocomotionObserver.Rebase(authoritative,localClock);
        var after=acceptedLocomotionObserver.State;
        Check(ReferenceEquals(before.Locomotion,after.Locomotion)&&before.Observations==after.Observations
            &&before.TimestampMicroseconds==after.TimestampMicroseconds&&before.UnwrappedPhase==after.UnwrappedPhase,
            "Authoritative rebase excludes teleport phase and events");
        acceptedLocomotionRebases++;
        GD.Print("accepted_locomotion_rebase="+JsonSerializer.Serialize(new{AtStep=pointIndex,after.Observations,after.TimestampMicroseconds,
            Baseline=new[]{after.Baseline.X,after.Baseline.Y,after.Baseline.Z},after.MotionRevision,after.Generation,
            after.Locomotion.AcceptedDistance,after.Locomotion.Frame,PhasePreserved=true,EventsPreserved=true,ActualAuthoritativeRow=true}));
    }
    private void AdvanceAcceptedLocomotion()
    {
        if(!pointAcceptedLocomotionHosted)return;
        acceptedLocomotionReceipt=acceptedLocomotionObserver!.Advance(localClock);
        acceptedLocomotionState=acceptedLocomotionReceipt.State.Locomotion;
        acceptedLocomotionEvents+=acceptedLocomotionReceipt.Events.Count;
    }
    private void FrameAcceptedLocomotion()
    {
        if(!pointAcceptedLocomotionHosted)return;
        var receipt=acceptedLocomotionReceipt!;var state=receipt.State;var gait=state.Locomotion;
        Check(acceptedLocomotionObserver!.IsCurrent(receipt)&&ReferenceEquals(receipt.Driver,pointDriver!.State.DriverResult)
            &&ReferenceEquals(receipt.Motion,motions!.Find(world!.Snapshots().Single(s=>s.Object.Handle.ObjectId==humanId))),
            "Current actual observer driver/publication receipt");
        Check(!acceptedLocomotionVisual!.IsRetired&&acceptedLocomotionVisual.Frame==gait.Frame
            &&acceptedLocomotionVisual.SelectedClip==gait.Mode.ToString().ToLowerInvariant(),"Actual accepted locomotion frame presentation");
        GD.Print("accepted_locomotion_state="+JsonSerializer.Serialize(new{Index=pointIndex,state.Observations,state.TimestampMicroseconds,
            state.MotionRevision,state.Generation,Mode=gait.Mode.ToString(),gait.Frame,gait.NormalizedPhase,gait.AcceptedDistance,gait.Switches,
            Heading=new[]{gait.Heading.X,gait.Heading.Y,gait.Heading.Z},state.UnwrappedPhase,state.GenerationSeconds,state.CompletedCycles,
            Events=receipt.Events.ToArray(),Current=true,ActualDriver=true,ActualLocalRow=true,
            PresentedFrame=acceptedLocomotionVisual.Frame,PresentedClip=acceptedLocomotionVisual.SelectedClip,NoSecondElapsedClock=true,
            OriginalAssetRecords=true,ModernScheduling=true,RootMotionApplied=false}));
    }
    private void CloseAcceptedLocomotion()
    {
        var observer=acceptedLocomotionObserver;acceptedLocomotionObserver=null;if(observer is null)return;
        var before=observer.State;var driver=pointDriver!.Driver!;var actual=driver.State;var worldState=world!.State;
        var snapshot=world.Snapshots().SingleOrDefault(s=>s.Object.Handle.ObjectId==humanId);
        var row=snapshot is null?null:motions!.Find(snapshot);
        observer.Dispose();observer.Dispose();
        Check(observer.State==before&&ReferenceEquals(driver.State,actual)&&world.State==worldState
            &&(snapshot is null||ReferenceEquals(motions!.Find(snapshot),row)),"Observer disposal preserves borrowed driver/world/store and historical state");
        GD.Print("accepted_locomotion_cleanup="+JsonSerializer.Serialize(new{Disposed=true,Observations=before.Observations,
            Events=acceptedLocomotionEvents,Rebases=acceptedLocomotionRebases,AcceptedDistance=before.Locomotion.AcceptedDistance,
            BorrowedDriverPreserved=true,BorrowedDriverActive=actual.Active,BorrowedDriverFault=actual.FaultType,
            BorrowedWorldPreserved=true,BorrowedStorePreserved=true,HistoricStatePreserved=true,
            WorldAlreadyClosedByHost=worldState==ZoneSceneTransitionState.Closed}));
    }
    private void LogAcceptedLocomotionVisualCleanup()
    {
        if(acceptedLocomotionVisual is null)return;
        Check(acceptedLocomotionVisual.IsRetired,"Owning accepted presentation chain retired");
        GD.Print("accepted_locomotion_visual_cleanup="+JsonSerializer.Serialize(new{Retired=true,OwnedPresentationDrained=true,OwnsObserver=false}));
    }
}
