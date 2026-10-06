using System;
using System.Linq;
using System.Text.Json;
using Godot;
using Stardust.Core.Session;

// Caller-selected modern replacement checkpoint, using the real owned slot.
public partial class ScenePumpedWorldPreview
{
    private bool nominalSlotRequested;
    private SceneWorldTerrainPlaybackSlot? nominalPlaybackSlot;
    private SceneWorldCurrentSpeedPlaybackReceipt? nominalSlotLastReceipt;
    private int nominalPlaybackOffset;
    private void BeginTerrainPlaybackSlot()
    {
        nominalPlaybackSlot=new(world!,motions!);
        Check(nominalPlaybackSlot.TryReplace(nominalCurrentPlanReader!,nominalCurrentComposedPlan!,suppliedTerrainSetQuery!,
            SceneWorldCurrentSpeedPlaybackPolicy.SuppliedLocalOnlyStoredSpeedOnWorldZ,localClock,10000),"Initial actual slot binding");
        nominalTerrainPlayback=nominalPlaybackSlot.Current!;
        GD.Print("terrain_playback_slot_ready="+JsonSerializer.Serialize(new{Revision=nominalPlaybackSlot.ReplacementRevision,
            ActualBinding=ReferenceEquals(nominalPlaybackSlot.Current,nominalTerrainPlayback),OwnedSlot=true,OriginalEffectiveMovementParity=false}));
    }
    private void ReplaceTerrainPlaybackAtCheckpoint()
    {
        if(nominalIndex!=30)return;
        var slot=nominalPlaybackSlot!;var old=slot.Current!;
        var oldDriver=old.Driver!;var oldPlayback=old.Playback!;var previous=nominalSlotLastReceipt!;
        var motionBefore=nominalDriver!.State;
        bool rejected=false;
        try { slot.TryReplace(nominalCurrentPlanReader!,nominalCurrentComposedPlan!,suppliedTerrainSetQuery!,
            SceneWorldCurrentSpeedPlaybackPolicy.SuppliedLocalOnlyStoredSpeedOnWorldZ,localClock,999); }
        catch(ArgumentOutOfRangeException){rejected=true;}
        Check(rejected&&ReferenceEquals(slot.Current,old)&&slot.ReplacementRevision==1&&slot.IsCurrent(previous)
            &&ReferenceEquals(nominalDriver.State,motionBefore),"Invalid replacement preserves owned binding and driver state");
        using(var foreign=new SceneWorldSuppliedCurrentSpeedPlan(nominalCurrentPairReader!))
        {
            Check(!slot.TryReplace(foreign,nominalCurrentComposedPlan!,suppliedTerrainSetQuery!,
                SceneWorldCurrentSpeedPlaybackPolicy.SuppliedLocalOnlyStoredSpeedOnWorldZ,localClock,10000)
                &&ReferenceEquals(slot.Current,old)&&slot.IsCurrent(previous),"Unavailable foreign source preserves slot");
        }
        var root=nominalFixture!.RootElement;var r=root.GetProperty("Rate");
        var rate=new CreatureNominalRate(r.GetProperty("Magnitude").GetUInt64(),r.GetProperty("Exponent").GetInt32(),r.GetProperty("IsNegative").GetBoolean());
        var remaining=root.GetProperty("Steps").EnumerateArray().Skip(nominalIndex)
            .Select(s=>ReadCurrentNominalPlanStep(s,root.GetProperty("SecondsBits").GetUInt32(),rate)).ToArray();
        var source=nominalCurrentPlanReader!.Build(nominalCurrentPair!,nominalPlan[nominalIndex-1].StoredBits,remaining)!;
        Check(source is not null&&source.Plan.Steps.Select(s=>s.CurrentAfterBits).SequenceEqual(nominalPlan.Skip(nominalIndex).Select(s=>s.StoredBits)),"Independent continuation equals retained full plan words");
        Check(slot.TryReplace(nominalCurrentPlanReader,source!,suppliedTerrainSetQuery!,
            SceneWorldCurrentSpeedPlaybackPolicy.SuppliedLocalOnlyStoredSpeedOnWorldZ,localClock,10000),"Valid continuation replacement");
        nominalTerrainPlayback=slot.Current!;nominalCurrentComposedPlan=source;nominalAtomicPlan=source!.Plan;
        nominalDriver=nominalTerrainPlayback.Driver;nominalPlayback=nominalTerrainPlayback.Playback;nominalPlaybackOffset=nominalIndex;
        var after=nominalDriver!.State;
        Check(old.State==SceneWorldCurrentSpeedPlaybackState.Disposed&&!oldDriver.State.Active
            &&oldPlayback.State==SceneWorldCurrentSpeedPlaybackState.Disposed&&!slot.IsCurrent(previous)
            &&after.Transform.Translation==motionBefore.Transform.Translation&&after.TimestampMicroseconds==localClock,
            "Old owned children retired; replacement preserves grounded pose and exact clock");
        GD.Print("terrain_playback_slot_replaced="+JsonSerializer.Serialize(new{GlobalStep=nominalIndex,Revision=slot.ReplacementRevision,
            RemainingSteps=source.Plan.StepCount,InitialCurrentBits=source.InitialCurrentBits,InvalidPreserved=rejected,UnavailablePreserved=true,
            OldBindingDisposed=true,OldDriverInactive=true,OldPlaybackDisposed=true,PriorReceiptCurrent=false,
            ActualSource=ReferenceEquals(nominalTerrainPlayback.Source,source),ActualTerrain=ReferenceEquals(nominalTerrainPlayback.Surface,suppliedTerrainSetQuery),
            PosePreserved=true,TimestampMicroseconds=localClock,OriginalEffectiveMovementParity=false}));
    }
    private void CloseTerrainPlaybackSlot()
    {
        var slot=nominalPlaybackSlot!;nominalPlaybackSlot=null;
        slot.RetireSelection();slot.Dispose();slot.Dispose();
        Check(slot.Current is null&&slot.IsDisposed,"Owned slot idempotent cleanup");
        GD.Print("terrain_playback_slot_cleanup="+JsonSerializer.Serialize(new{Disposed=true,CurrentCleared=true,Revision=slot.ReplacementRevision,
            BorrowedSourcePreserved=nominalCurrentPlanReader!.IsCurrent(nominalCurrentComposedPlan!),BorrowedTerrainPreserved=suppliedTerrainSetQuery is {PatchCount:16}}));
        nominalSlotLastReceipt=null;
    }
}
