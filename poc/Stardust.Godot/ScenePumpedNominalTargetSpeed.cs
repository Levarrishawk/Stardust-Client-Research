using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using Stardust.Core.Session;

// Supplied target construction is pure; motion publication starts only after all steps pass.
public partial class ScenePumpedWorldPreview
{
    private bool nominalTargetRequested;
    private readonly List<CreatureNominalTargetSpeedResult> nominalTargets=new();
    private static CreatureNominalSpeedPlanStep ReadNominalPlanStep(JsonElement step,uint seconds,CreatureNominalRate rate)
    {
        var words=step.GetProperty("DirectionWordsInRecoveredOrder");
        Check(words.GetArrayLength()==3,"Three explicit direction operands in recovered load order");
        var factor=step.GetProperty("Factor");
        return new(step.GetProperty("RunSpeedBits").GetUInt32(),step.GetProperty("WalkSpeedBits").GetUInt32(),step.GetProperty("RunFlag").GetByte(),
            new(words[0].GetUInt32(),words[1].GetUInt32(),words[2].GetUInt32()),
            new(factor.GetProperty("Magnitude").GetUInt64(),factor.GetProperty("Exponent").GetInt32(),factor.GetProperty("IsNegative").GetBoolean()),seconds,rate);
    }
    private uint ResolveNominalTarget(JsonElement step,CreatureNominalSpeedPrecision precision)=>step.GetProperty("TargetBits").GetUInt32();
    private void PrintNominalAtomicPlan()
    {
        var plan=nominalAtomicPlan!;
        bool actualReferences=plan.Steps.Select((s,i)=>ReferenceEquals(s.Target,nominalTargets[i])&&ReferenceEquals(s.Transition,nominalPlan[i])).All(x=>x);
        Check(actualReferences,"Actual atomic target/transition receipt references retained");
        GD.Print("nominal_speed_plan_ready="+JsonSerializer.Serialize(new{plan.StepCount,plan.TargetEvaluations,plan.TransitionEvaluations,
            plan.RatesUsed,plan.Equalities,plan.Clamps,plan.TargetF32Stores,plan.TransitionF32Stores,plan.ReservedCopyBytes,plan.ReservedWork,
            plan.ActualTargetWork,plan.ActualTransitionWork,plan.ActualWork,ActualReferences=actualReferences,OriginalEffectiveMovementParity=false}));
    }
    private static object ProjectNominalTarget(CreatureNominalTargetSpeedResult target)=>new{
        target.TargetBits,target.IntermediateBits,target.SelectedSpeedBits,
        RunSelected=target.Selection==CreatureNominalTargetSelection.Run,
        Magnitude=target.RetainedMagnitude,Intermediate=target.RetainedIntermediate,target.RetainedTarget,
        target.RetainedSquaredMagnitude,target.ReservedWork,target.ArithmeticOperations,target.F32Stores,
        target.Provenance,OriginalEffectiveMovementParity=false};
    private void FrameNominalTarget()
    {
        if(nominalTargetRequested)GD.Print("nominal_target_state="+JsonSerializer.Serialize(new{
            Index=nominalIndex,Target=ProjectNominalTarget(nominalTargets[nominalIndex])}));
    }
}

