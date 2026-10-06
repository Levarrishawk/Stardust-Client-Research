using System;
using System.Linq;
using System.Text.Json;
using Godot;
using Stardust.Core.Session;

// Explicit local-current arithmetic; alternate object/global modifier branches remain unmodeled.
public partial class ScenePumpedWorldPreview
{
    private bool nominalCurrentRequested;
    private SceneWorldCreatureMovementCurrentInputs? nominalCurrentReader;
    private SceneWorldCreatureMovementCurrentReceipt? nominalCurrentView;
    private SceneWorldSuppliedCurrentSpeedInputs? nominalCurrentPairReader;
    private SceneWorldSuppliedCurrentSpeedReceipt? nominalCurrentPair;
    private SceneWorldSuppliedCurrentSpeedPlan? nominalCurrentPlanReader;
    private SceneWorldSuppliedCurrentSpeedPlanReceipt? nominalCurrentComposedPlan;
    private CreatureSuppliedCurrentSpeedResult? nominalCurrentRun,nominalCurrentWalk;
    private void PrepareCurrentNominalSource(CreatureNominalSpeedPrecision precision)
    {
        var inputs=originalHuman!.Resources.Creature!.Creature.AuthoredMovement!.Inputs;
        nominalCurrentReader=new(world!,humanId,inputs,
            "E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243",
            SceneCreatureMovementConstructorScope.CallerVerifiedConcreteClientCreatureRawInputs,true);
        try {
            nominalCurrentPairReader=new(nominalCurrentReader,CreatureSuppliedCurrentSpeedPolicy.SuppliedLocalOnly,precision);
            var pair=nominalCurrentPairReader.Read(generation)??throw new InvalidOperationException("Current paired source missing or stale");
            Check(nominalCurrentPairReader.IsCurrent(pair),"Actual paired source current before plan");
            var view=pair.View;ushort[] indices={7,11,4,5};var fields=new[]{pair.RunBase,pair.WalkBase,pair.FirstModifier,pair.SecondModifier};
            var run=pair.Run;var walk=pair.Walk;
            nominalCurrentPair=pair;nominalCurrentView=view;nominalCurrentRun=run;nominalCurrentWalk=walk;
            GD.Print("paired_current_speed_ready="+JsonSerializer.Serialize(new{pair.OutputBytes,pair.ReservedCopyBytes,pair.ReservedWork,pair.ActualWork,
                pair.ReaderCalls,pair.SuppliedWords,pair.Multiplications,pair.CallerF32Stores,
                ActualReferences=ReferenceEquals(pair.Run,run)&&ReferenceEquals(pair.Walk,walk)&&ReferenceEquals(pair.View,view)
                    &&indices.Select((index,i)=>ReferenceEquals(fields[i],view.Get(index))).All(x=>x),
                Current=nominalCurrentPairReader.IsCurrent(pair),OriginalGetterBranchParity=false}));
            GD.Print("current_nominal_source_ready="+JsonSerializer.Serialize(new{Current=true,view.RootSha256,view.Generation,
                Fields=fields.Select(f=>new{f.Index,f.RawBits,Origin=f.Origin.ToString(),Eligibility=f.Eligibility.ToString()}),Run=run,Walk=walk,
                ReaderCalls=2,Multiplications=run.Multiplications+walk.Multiplications,CallerF32Stores=run.CallerF32Stores+walk.CallerF32Stores,
                ActualFieldReferences=indices.Select((index,i)=>ReferenceEquals(fields[i],view.Get(index))).All(x=>x),
                SourcePublicationRetained=ReferenceEquals(visuals[humanId].Publication,originalHuman),calls,closed,
                OriginalGetterBranchParity=false,OriginalEffectiveMovementParity=false}));
        } catch {
            GD.Print("current_nominal_source_rejected="+JsonSerializer.Serialize(new{PublishedReaderResults=0,DriverConstructed=false,MotionPublication=false}));throw;
        }
    }
    private SceneWorldSuppliedCurrentSpeedPlanStep ReadCurrentNominalPlanStep(JsonElement step,uint seconds,CreatureNominalRate rate)
    {
        Check(!step.TryGetProperty("RunSpeedBits",out _)&&!step.TryGetProperty("WalkSpeedBits",out _),"Current trace takes actual reader outputs only");
        var words=step.GetProperty("DirectionWordsInRecoveredOrder");Check(words.GetArrayLength()==3,"Three supplied ordered direction words");
        var factor=step.GetProperty("Factor");
        return new(step.GetProperty("RunFlag").GetByte(),
            new(words[0].GetUInt32(),words[1].GetUInt32(),words[2].GetUInt32()),
            new(factor.GetProperty("Magnitude").GetUInt64(),factor.GetProperty("Exponent").GetInt32(),factor.GetProperty("IsNegative").GetBoolean()),seconds,rate);
    }
    private void FrameCurrentNominalSource()
    {
        if(!nominalCurrentRequested)return;
        Check(nominalCurrentPlanReader!.IsCurrent(nominalCurrentComposedPlan!),"Actual composed plan source remains current during bounded playback");
        GD.Print("current_nominal_source_state="+JsonSerializer.Serialize(new{Index=nominalIndex,Current=true,
            RunBits=nominalCurrentRun!.ReturnBits,WalkBits=nominalCurrentWalk!.ReturnBits,OriginalGetterBranchParity=false}));
    }
    private void CloseCurrentNominalSource()
    {
        nominalCurrentPlanReader?.Dispose();nominalCurrentPlanReader=null;nominalCurrentComposedPlan=null;
        nominalCurrentPairReader?.Dispose();nominalCurrentPairReader=null;nominalCurrentPair=null;
        nominalCurrentReader?.Dispose();nominalCurrentReader=null;nominalCurrentView=null;nominalCurrentRun=null;nominalCurrentWalk=null;
        if(nominalCurrentRequested)GD.Print("current_nominal_source_cleanup="+JsonSerializer.Serialize(new{Closed=true}));
    }
}
