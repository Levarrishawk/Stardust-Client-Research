using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Session;

// Explicit synthetic supplied-rate trace through the real native motion driver.
public partial class ScenePumpedWorldPreview
{
    private bool nominalRequested,nominalStarted,nominalComplete,nominalClosed;
    private JsonDocument? nominalFixture;
    private byte[]? nominalEncoded;
    private SceneWorldLocalMotionDriver? nominalDriver;
    private CreatureNominalSpeedResult[] nominalPlan=Array.Empty<CreatureNominalSpeedResult>();
    private CreatureNominalSpeedPlan? nominalAtomicPlan;
    private int nominalIndex;
    private string? nominalVisualFingerprint;
    private void LoadNominalSpeedArguments(string[] args)
    {
        int i=Array.IndexOf(args,"--nominal-speed-fixture");if(i<0)return;
        Check(args.Count(a=>a=="--nominal-speed-fixture")==1&&i+1<args.Length,"One explicit nominal fixture");
        Check(args.Contains("--local-input")&&args.Contains("--creature4-partial-peer")
            &&!args.Contains("--sourced-input")&&!args.Contains("--terrain-peer")&&!args.Contains("--input-loss")&&!args.Contains("--pump-cancel"),"Bounded separate nominal host");
        using(var stream=File.OpenRead(args[i+1])){
            Check(stream.Length is >0 and <=262144,"Bounded nominal fixture");
            nominalEncoded=new byte[(int)stream.Length];stream.ReadExactly(nominalEncoded);
            Check(stream.ReadByte()==-1,"Stable bounded nominal input");
        }
        // JsonDocument borrows this memory; preserve it until the document is disposed.
        nominalFixture=JsonDocument.Parse(nominalEncoded,new JsonDocumentOptions{MaxDepth=12});
        var root=nominalFixture.RootElement;
        int version=root.GetProperty("Version").GetInt32();nominalCurrentRequested=version==3;nominalTargetRequested=version is 2 or 3;
        Check((!nominalTargetRequested&&root.GetProperty("Version").GetInt32()==1&&root.GetProperty("Policy").GetString()=="supplied_nominal_rate_modern_planar_trace"
            ||nominalTargetRequested&&((version==2&&root.GetProperty("Policy").GetString()=="supplied_nominal_target_and_rate_modern_planar_trace")
            ||(version==3&&root.GetProperty("Policy").GetString()=="supplied_local_current_reader_and_nominal_plan_modern_planar_trace"
                &&root.GetProperty("CurrentInputPolicy").GetString()=="SuppliedLocalOnly"&&root.GetProperty("Synthetic").GetBoolean()&&!root.GetProperty("OriginalGetterBranchParity").GetBoolean())))
            &&!root.GetProperty("OriginalEffectiveMovementParity").GetBoolean(),"Explicit synthetic nominal policy");
        Check(root.GetProperty("Steps").GetArrayLength() is >0 and <=128
            &&root.GetProperty("InitialSpeedBits").GetUInt32()==0
            &&root.GetProperty("StepMicroseconds").GetInt32()==10000
            &&root.GetProperty("SecondsBits").GetUInt32()==0x3C23D70A,"Bounded explicit zero start and clock mapping");
        nominalRequested=true;
    }
    private void BeginNominalSpeed()
    {
        Check(nominalRequested&&!nominalStarted&&phase==47&&calls==3&&closed==3
            &&inputHost!.LastState!.Speed==0,"Nominal trace follows completed offline host gates");
        var root=nominalFixture!.RootElement;var r=root.GetProperty("Rate");
        var rate=new CreatureNominalRate(r.GetProperty("Magnitude").GetUInt64(),r.GetProperty("Exponent").GetInt32(),r.GetProperty("IsNegative").GetBoolean());
        var plan=new List<CreatureNominalSpeedResult>();uint current=root.GetProperty("InitialSpeedBits").GetUInt32();
        try {
            if(nominalTargetRequested){
                if(nominalCurrentRequested)PrepareCurrentNominalSource((CreatureNominalSpeedPrecision)root.GetProperty("Precision").GetInt32());
                var owned=root.GetProperty("Steps").EnumerateArray().Select(step=>nominalCurrentRequested
                    ?ReadCurrentNominalPlanStep(step,root.GetProperty("SecondsBits").GetUInt32(),rate)
                    :ReadNominalPlanStep(step,root.GetProperty("SecondsBits").GetUInt32(),rate)).ToArray();
                var atomic=CreatureNominalSpeedPlan.Build(current,owned,(CreatureNominalSpeedPrecision)root.GetProperty("Precision").GetInt32(),new(128,81920,8388608));
                foreach(var receipt in atomic.Steps){
                    float stored=BitConverter.UInt32BitsToSingle(receipt.CurrentAfterBits);
                    Check(float.IsFinite(stored)&&stored>=0&&stored<=10,"Explicit bounded modern planar stored speed");
                }
                current=atomic.FinalCurrentBits;nominalAtomicPlan=atomic;
                plan.AddRange(atomic.Steps.Select(receipt=>receipt.Transition));
                nominalTargets.AddRange(atomic.Steps.Select(receipt=>receipt.Target));
            } else {
            foreach(var step in root.GetProperty("Steps").EnumerateArray()){
                var result=CreatureNominalSpeedTransition.Evaluate(current,ResolveNominalTarget(step,(CreatureNominalSpeedPrecision)root.GetProperty("Precision").GetInt32()),
                    root.GetProperty("SecondsBits").GetUInt32(),(CreatureNominalSpeedPrecision)root.GetProperty("Precision").GetInt32(),rate);
                float stored=BitConverter.UInt32BitsToSingle(result.StoredBits);
                Check(float.IsFinite(stored)&&stored>=0&&stored<=10,"Explicit bounded modern planar stored speed");
                plan.Add(result);current=result.StoredBits;
            }
            }
            Check(current==0,"Trace ends at stored positive zero");
        } catch {
            GD.Print("nominal_speed_preflight_failure="+JsonSerializer.Serialize(new{CompletedSteps=plan.Count,DriverConstructed=false,MotionPublication=false}));
            throw;
        }
        // All pure candidates are accepted before transferring the motion writer.
        nominalPlan=plan.ToArray();
        if(nominalAtomicPlan is not null)PrintNominalAtomicPlan();
        inputHost!.Close();inputHost=null;
        nominalDriver=new(world!,motions!,humanId,localClock,10000,1);
        nominalVisualFingerprint=visuals[humanId].CreatureVisual!.Fingerprint();
        nominalStarted=true;phase=70;
        var start=nominalDriver.State.Transform.Translation;
        GD.Print("nominal_speed_ready="+JsonSerializer.Serialize(new{Steps=nominalPlan.Length,DriverConstructed=true,Start=new[]{start.X,start.Y,start.Z},
            TimestampMicroseconds=localClock,calls,closed,Policy=root.GetProperty("Policy").GetString(),OriginalEffectiveMovementParity=false}));
    }
    private void AdvanceNominalSpeed()
    {
        if(!nominalStarted||nominalComplete||finishing)return;
        Check(nominalIndex<nominalPlan.Length,"Bounded nominal trace index");
        float speed=BitConverter.UInt32BitsToSingle(nominalPlan[nominalIndex].StoredBits);
        nominalDriver!.Submit(localClock,new(new System.Numerics.Vector3(0,0,speed),null));
        localClock=checked(localClock+10000);
        var state=nominalDriver.Advance(localClock);
        Check(state.FaultType is null&&state.StepsThisUpdate==1&&state.DroppedMicroseconds==0,"Healthy one-step nominal native driver");
        locomotion!.SetPlaying(speed>0);
    }
    private void FrameNominalSpeed()
    {
        var state=nominalDriver!.State;var position=state.Transform.Translation;
        var visual=visuals[humanId].CreatureVisual!;var engine=visual.Root.Position;
        Check(engine.DistanceTo(new Vector3(position.X,position.Y,-position.Z))<2e-5f,"Nominal native/engine reflection");
        bool resource=ReferenceEquals(visuals[humanId].Publication,originalHuman);
        bool retained=visual.Fingerprint()==nominalVisualFingerprint;
        Check(resource&&retained&&calls==3&&closed==3,"Nominal trace retains actual original publication without I/O");
        FrameCurrentNominalSource();
        FrameNominalTarget();
        GD.Print("nominal_speed_state="+JsonSerializer.Serialize(new{Index=nominalIndex,Transition=nominalPlan[nominalIndex],
            Native=new[]{position.X,position.Y,position.Z},Engine=new[]{engine.X,engine.Y,engine.Z},state.Speed,
            state.TimestampMicroseconds,state.StepsThisUpdate,state.DroppedMicroseconds,state.FaultType,
            PublicationRetained=resource,VisualRetained=retained,calls,closed,OriginalEffectiveMovementParity=false}));
        nominalIndex++;
        if(nominalIndex==nominalPlan.Length){
            nominalDriver.Submit(localClock,new(System.Numerics.Vector3.Zero,null));
            var stopped=nominalDriver.Advance(localClock,false);
            Check(stopped.Speed==0&&!stopped.Active,"Explicit modern inactive end");
            GD.Print("nominal_speed_stopped="+JsonSerializer.Serialize(new{stopped.Speed,stopped.Active,Keys=0}));
            locomotion!.SetPlaying(false);nominalComplete=true;phase=71;ScheduleFinish();
        }
    }
    private void CloseNominalSpeed()
    {
        if(nominalClosed)return;nominalClosed=true;
        try {
            if(nominalDriver is not null){
                var driver=nominalDriver;nominalDriver=null;driver.Dispose();
                GD.Print("nominal_speed_cleanup="+JsonSerializer.Serialize(new{Closed=true,Steps=nominalIndex,DriverDisposed=true}));
            }
        } finally {
            CloseCurrentNominalSource();
            nominalFixture?.Dispose();nominalFixture=null;
            if(nominalEncoded is not null){Array.Clear(nominalEncoded);nominalEncoded=null;}
            nominalPlan=Array.Empty<CreatureNominalSpeedResult>();nominalTargets.Clear();nominalAtomicPlan=null;
        }
    }
}
