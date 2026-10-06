using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;
public static class SceneWorldCurrentSpeedPlanPlaybackScenarios
{
    private const SceneCreatureMovementConstructorScope Scope = SceneCreatureMovementConstructorScope.CallerVerifiedConcreteClientCreatureRawInputs;
    private static int checks;
    private static readonly string path = "object/root.iff", zero = new('0', 64);
    private static readonly uint crc = LegacyAssetPath.Checksum(path);
    private static void Check(bool value, string why) { checks++; if (!value) throw new InvalidDataException(why); }
    private static void Reject(Action action)
    { try { action(); } catch (Exception e) when (e is InvalidDataException or InvalidOperationException or ArgumentException or NotSupportedException or ObjectDisposedException or OperationCanceledException) { checks++; return; } throw new Exception("Expected rejection"); }
    private static SceneWorldCoordinator World(string pin, uint? rootCrc = null) => new(
        new ObjectTemplateCatalog([new(rootCrc ?? crc, rootCrc is null ? path : "object/wrong.iff", pin, zero, SceneObjectResourceRecipe.CreatureDetail0)]),
        new Dictionary<SceneObjectResourceRecipe, SceneObjectResourceOptions> { [SceneObjectResourceRecipe.CreatureDetail0] = new SceneCreatureResourceOptions(
            new("appearance/lat/synthetic.lat", "idle", zero), new Dictionary<string, LogicalAnimationClipSelection> {
                ["walk"] = new("appearance/lat/synthetic.lat", "idle", zero), ["run"] = new("appearance/lat/synthetic.lat", "idle", zero) }, false) },
        (_, _) => throw new InvalidDataException("No resource loads"), SceneCreateZeroQuaternionPolicy.Reject, 4, 2, 32, 4194304, 32, 4194304, 1048576,
        replicationPolicy: new(SceneWorldReplicationMode.CreatureView3MixedComponents, new(1024, 64, 64, 16, 512), 16, 1024,
            new(1024, 32, 24, 64, 64, 16), CreatureWoundsApplicationPolicy.LegacyBounded, new(1024, 32, 64, 64, 16), new(16, 16)),
        creatureView4PartialPolicy: new(new(MessageLimits: new(MaximumPayloadBytes: 1048576)), new()));
    private static byte[] Start()
    { using var s = new MemoryStream(); using var w = new BinaryWriter(s); w.Write((ushort)9); w.Write(ZoneSceneProtocolCodec.CmdStartSceneOpcode);
        w.Write((byte)0); w.Write(42UL); w.Write((ushort)0); w.Write(0f); w.Write(0f); w.Write(0f); w.Write((ushort)0); w.Write(0UL); return s.ToArray(); }
    private static byte[] Create(uint? rootCrc = null) => SceneObjectLifecycleCodec.EncodeCreate(new(42, 0, 0, 0, 1, 0, 0, 0, rootCrc ?? crc, 0));
    private static long Stage(SceneWorldCoordinator w, byte[] baseline, bool typed, bool final, uint? rootCrc = null)
    { long g = w.BeginSelection(42).Generation; Check(w.Accept(g, Start()), "start"); Check(w.Accept(g, Create(rootCrc)), "create");
        if (typed) Check(w.Accept(g, baseline), "typed3"); if (final) Check(w.Accept(g, SceneObjectLifecycleCodec.EncodeEndBaselines(new(42))), "end");
        Check(w.CompleteMinimumSceneBarrier(g, true) is not null, "barrier"); return g; }
    private static SharedCreatureAuthoredMovementInputs Profile(JsonElement p)
    { var resources = p.GetProperty("Resources").EnumerateArray().ToDictionary(r => r.GetProperty("Path").GetString()!, r => new TreLookupResult(TreLookupKind.Present, Convert.FromHexString(r.GetProperty("Hex").GetString()!), 77));
        return SharedCreatureAuthoredMovementInputs.FromProfile(SharedCreatureMovementInputProfile.Load(q => resources.GetValueOrDefault(q, new(TreLookupKind.Missing)), path, p.GetProperty("RootSha256").GetString()!)); }
    private static string F(CreatureNominalRetainedValue v)
    { if(v.Magnitude==0)return v.IsNegative?"-0/1":"0/1";BigInteger n=v.Magnitude,d=1;if(v.IsNegative)n=-n;if(v.Exponent>=0)n<<=v.Exponent;else d<<=-v.Exponent;var g=BigInteger.GreatestCommonDivisor(BigInteger.Abs(n),d);return $"{n/g}/{d/g}"; }
    private static object Speed(CreatureSuppliedCurrentSpeedResult r)=>new {Base=r.BaseBits,First=r.FirstModifierSnapshotBits,Second=r.SecondModifierBits,Precision=(int)r.Precision,Modifiers=F(r.RetainedModifierProduct),Speed=F(r.RetainedSpeed),r.ReturnBits,Work=r.ReservedWork,Copy=r.ReservedCopyBytes,Counts=new[]{r.SuppliedWords,r.Multiplications,r.CallerF32Stores},Policy=r.Policy.ToString(),Provenance=r.Provenance.ToString()};
    private static object Target(CreatureNominalTargetSpeedResult r)=>new {Selection=r.Selection.ToString(),Selected=r.SelectedSpeedBits,Sum=F(r.RetainedSquaredMagnitude),Magnitude=F(r.RetainedMagnitude),Intermediate=F(r.RetainedIntermediate),r.IntermediateBits,RetainedTarget=F(r.RetainedTarget),r.TargetBits,Work=r.ReservedWork,Counts=new[]{r.SuppliedGetterWords,r.DirectionWords,r.SelectedSpeedUses,r.FactorValuesUsed,r.ArithmeticOperations,r.F32Stores},Provenance=r.Provenance.ToString()};
    private static object Transition(CreatureNominalSpeedResult r)=>new {r.StoredBits,r.CandidateBits,Retained=F(r.RetainedCandidate),Step=r.RetainedStep.HasValue?F(r.RetainedStep.Value):null,Branch=r.Branch.ToString(),r.Clamped,Work=r.ReservedWork,r.RatesUsed,Provenance=r.RateProvenance.ToString()};
    private static int I(JsonElement c,string k,int d)=>c.TryGetProperty(k,out var v)?v.GetInt32():d;
    private static long L(JsonElement c,string k,long d)=>c.TryGetProperty(k,out var v)?v.GetInt64():d;
    private static readonly CreatureSuppliedCurrentSpeedPolicy Local=CreatureSuppliedCurrentSpeedPolicy.SuppliedLocalOnly;
    private static SceneWorldSuppliedCurrentSpeedPlanStep Step(JsonElement s)
    {
        var d=s.GetProperty("Direction").EnumerateArray().Select(v=>v.GetUInt32()).ToArray();if(d.Length!=3)throw new InvalidDataException("Direction shape.");
        var f=s.GetProperty("Factor");CreatureNominalRate? r=null;
        if(s.TryGetProperty("Rate",out var rr)&&rr.ValueKind!=JsonValueKind.Null)r=new(rr.GetProperty("Magnitude").GetUInt64(),rr.GetProperty("Exponent").GetInt32(),rr.GetProperty("Negative").GetBoolean());
        return new(checked((byte)s.GetProperty("Flag").GetInt32()),new(d[0],d[1],d[2]),new(f.GetProperty("Magnitude").GetUInt64(),f.GetProperty("Exponent").GetInt32(),f.GetProperty("Negative").GetBoolean()),s.GetProperty("Seconds").GetUInt32(),r);
    }
    private static object Pair(string name,SceneWorldSuppliedCurrentSpeedReceipt p)=>new {Name=name,Reject="None",p.RootSha256,Fields=new[]{p.RunBase,p.WalkBase,p.FirstModifier,p.SecondModifier}.Select(f=>new {f.Index,f.RawBits,Origin=f.Origin.ToString(),Eligibility=f.Eligibility.ToString()}),Run=Speed(p.Run),Walk=Speed(p.Walk),Output=p.OutputBytes,Copy=p.ReservedCopyBytes,Reserved=p.ReservedWork,p.ActualWork,Counts=new[]{p.ReaderCalls,p.SuppliedWords,p.Multiplications,p.CallerF32Stores}};
    private static object Plan(string name,CreatureNominalSpeedPlan p)=>new {Name=name,Reject="None",Initial=p.InitialCurrentBits,Final=p.FinalCurrentBits,Rows=p.Steps.Select(r=>new {r.Index,r.CurrentBeforeBits,SourceMatches=true,Target=Target(r.Target),Transition=Transition(r.Transition)}),Copy=p.ReservedCopyBytes,Reserved=p.ReservedWork,ActualTarget=p.ActualTargetWork,ActualTransition=p.ActualTransitionWork,Counts=new[]{p.StepCount,p.TargetEvaluations,p.TransitionEvaluations,p.RatesUsed,p.Equalities,p.Clamps,p.TargetF32Stores,p.TransitionF32Stores}};
    private static readonly SceneWorldCurrentSpeedPlaybackPolicy PlaybackPolicy=SceneWorldCurrentSpeedPlaybackPolicy.SuppliedLocalOnlyStoredSpeedOnWorldZ;
    private sealed class CountingDriver(ISceneWorldLocalMotionDriver inner):ISceneWorldLocalMotionDriver
    {
        public int Reads,Submits,Advances,Resets,Disposals;public SceneWorldLocalMotionDriverState? LastResult;
        public SceneWorldLocalMotionDriverState State {get{Reads++;return inner.State;}}
        public void Submit(long t,PortalActorInput i){Submits++;inner.Submit(t,i);}
        public SceneWorldLocalMotionDriverState Advance(long t,bool a=true){Advances++;return LastResult=inner.Advance(t,a);}
        public void Reset(long t){Resets++;inner.Reset(t);}public void Dispose(){Disposals++;inner.Dispose();}
    }
    private sealed class ControlledDriver:ISceneWorldLocalMotionDriver
    {
        public int Reads,Submits,Advances,Resets,Disposals,ThrowAtAdvance;public string Mode="Healthy";
        public Action? OnState,OnSubmit,OnAdvance;
        private SceneWorldLocalMotionDriverState state=new(42,AppearanceTransform.Identity,0,0,0,0,0,true,null);
        private PortalActorInput input;
        public SceneWorldLocalMotionDriverState State
        {
            get{Reads++;OnState?.Invoke();if(Mode=="StateThrow")throw new InvalidOperationException("Controlled getter error");
                return Mode switch {"WrongId"=>state with {ObjectId=43},"WrongClock"=>state with {TimestampMicroseconds=7},"PreFault"=>state with {FaultType="Controlled",Active=false},"PreNonfinite"=>state with {Transform=state.Transform with {Translation=new(float.NaN,0,0)}},_=>state};}
        }
        public void Submit(long t,PortalActorInput i){Submits++;input=i;OnSubmit?.Invoke();if(Mode=="SubmitThrow")throw new InvalidOperationException("Controlled submit error");}
        public SceneWorldLocalMotionDriverState Advance(long t,bool active=true)
        {
            Advances++;state=new(42,state.Transform with {Translation=state.Transform.Translation+input.WorldVelocity*.01f},input.WorldVelocity.Length(),t,1,0,0,true,null);OnAdvance?.Invoke();
            if(Mode=="AdvanceThrow"||ThrowAtAdvance==Advances)throw new InvalidOperationException("Controlled advance error after motion");
            return Mode switch {"ZeroSteps"=>state with {StepsThisUpdate=0},"TwoSteps"=>state with {StepsThisUpdate=2},"Dropped"=>state with {DroppedMicroseconds=1},"Suspended"=>state with {SuspendedMicroseconds=1},"FaultReturn"=>state with {FaultType="Controlled",Active=false},"WrongResultTime"=>state with {TimestampMicroseconds=t+1},"WrongResultId"=>state with {ObjectId=43},"NonfiniteResult"=>state with {Transform=state.Transform with {Translation=new(float.NaN,0,0)}},_=>state};
        }
        public void Reset(long t){Resets++;}public void Dispose(){Disposals++;}
    }
    public static object Run(string input)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(input));var json=doc.RootElement;byte[] baseline=Convert.FromHexString(json.GetProperty("Baseline").GetString()!);
        var profiles=json.GetProperty("Profiles").EnumerateArray().Select(Profile).ToArray();var output=new List<object>();int steps=0;
        foreach(var c in json.GetProperty("Cases").EnumerateArray())
        {
            string name=c.GetProperty("Name").GetString()!;var p=profiles[c.GetProperty("Profile").GetInt32()];string pin=p.Sources[path].Sha256;
            using var w=World(pin);long g=Stage(w,baseline,true,true);using var current=new SceneWorldCreatureMovementCurrentInputs(w,42,p,pin,Scope,true);
            using var pairReader=new SceneWorldSuppliedCurrentSpeedInputs(current,Local,(CreatureNominalSpeedPrecision)c.GetProperty("Precision").GetInt32());
            Check(w.Accept(g,Convert.FromHexString(c.GetProperty("Wire").GetString()!)),"actual received synthetic frame");var pair=pairReader.Read(g)!;
            using var composer=new SceneWorldSuppliedCurrentSpeedPlan(pairReader);var plan=composer.Build(pair,c.GetProperty("Initial").GetUInt32(),c.GetProperty("Steps").EnumerateArray().Select(Step).ToArray())!;
            using var motions=new SceneWorldObjectMotionStore(w);using var actualDriver=new SceneWorldLocalMotionDriver(w,motions,42,0,10000,8);var driver=new CountingDriver(actualDriver);
            using var playback=new SceneWorldCurrentSpeedPlanPlayback(composer,plan,driver,PlaybackPolicy,0,10000);
            var rows=new List<object>();long time=0;SceneWorldCurrentSpeedPlaybackReceipt? last=null;
            for(int i=0;i<plan.Plan.StepCount;i++)
            {
                var r=playback.AdvanceNext(time,time+10000);Check(r is not null,"healthy native playback");
                Check(ReferenceEquals(r!.Source,plan)&&ReferenceEquals(r.Step,plan.Plan.Steps[i])&&ReferenceEquals(r.DriverResult,driver.LastResult),"actual source/step/result identity");
                Check(r.Index==i&&playback.IsCurrent(r)&&composer.IsCurrent(plan)&&r.StoredBits==r.Step.Transition.StoredBits,"index/rawbits/currency");
                var d=r.DriverResult;rows.Add(new {Index=i,StoredBits=r.StoredBits,Submit=time,Advance=time+10000,ResultTime=d.TimestampMicroseconds,Steps=d.StepsThisUpdate,d.Active,d.FaultType,SpeedBits=BitConverter.SingleToUInt32Bits(d.Speed),PositionBits=new[]{BitConverter.SingleToUInt32Bits(d.Transform.Translation.X),BitConverter.SingleToUInt32Bits(d.Transform.Translation.Y),BitConverter.SingleToUInt32Bits(d.Transform.Translation.Z)}});time+=10000;last=r;steps++;
            }
            Check(playback.State==SceneWorldCurrentSpeedPlaybackState.Complete&&playback.AdvanceNext(time,time+10000) is null,"complete/empty no calls");
            Check(driver.Reads==plan.Plan.StepCount&&driver.Submits==plan.Plan.StepCount&&driver.Advances==plan.Plan.StepCount&&playback.NextIndex==plan.Plan.StepCount,"exact call accounting");
            Check(playback.StateReads==driver.Reads&&playback.SubmitAttempts==driver.Submits&&playback.AdvanceAttempts==driver.Advances,"attempt counters");
            if(last is not null)Check(ReferenceEquals(playback.LastSuccessful,last),"last result identity");
            playback.Dispose();playback.Dispose();Check(playback.State==SceneWorldCurrentSpeedPlaybackState.Disposed&&driver.Resets==0&&driver.Disposals==0&&composer.IsCurrent(plan),"no borrowed reset/disposal");
            if(last is not null)Check(!playback.IsCurrent(last),"disposed currency false");
            output.Add(new {Name=name,Pair=Pair(name,pair),Plan=Plan(name,plan.Plan),Rows=rows,Counts=new[]{driver.Reads,driver.Submits,driver.Advances},BorrowedAlive=actualDriver.State.Active&&pairReader.IsCurrent(pair)&&composer.IsCurrent(plan)});
        }
        var negatives=Negatives(profiles[0],baseline);
        return new {Cases=output,ExecutedSteps=steps,NativeChecks=checks,NegativeChecks=negatives,OriginalExecuted=false,NoImplicitBorrowedResetOrDispose=true};
    }
    private static int Negatives(SharedCreatureAuthoredMovementInputs p,byte[] baseline)
    {
        int beforeChecks=checks;string pin=p.Sources[path].Sha256;using var w=World(pin);long g=Stage(w,baseline,true,true);
        using var current=new SceneWorldCreatureMovementCurrentInputs(w,42,p,pin,Scope,true);using var pairReader=new SceneWorldSuppliedCurrentSpeedInputs(current,Local,CreatureNominalSpeedPrecision.NearestEven64);
        Check(w.ApplyCreatureView4PartialDelta(g,null,new(42,0,[new(7,0x40000000),new(11,0x3f800000),new(4,0x3f800000),new(5,0x3f800000)])),"finite source");
        var pair=pairReader.Read(g)!;using var composer=new SceneWorldSuppliedCurrentSpeedPlan(pairReader);var good=new SceneWorldSuppliedCurrentSpeedPlanStep(1,new(0x3f800000,0,0),new(1,0),0x3f000000,new(1,0));
        var plan=composer.Build(pair,0,new[]{good,good})!;
        foreach(var mode in new[]{"StateThrow","WrongId","WrongClock","PreFault","PreNonfinite","SubmitThrow","AdvanceThrow","ZeroSteps","TwoSteps","Dropped","Suspended","FaultReturn","WrongResultTime","WrongResultId","NonfiniteResult"})
        {
            var fake=new ControlledDriver{Mode=mode};using var play=new SceneWorldCurrentSpeedPlanPlayback(composer,plan,fake,PlaybackPolicy,0,10000);
            Check(play.AdvanceNext(0,10000) is null&&play.NextIndex==0,"rejected driver leaves index");int reads=fake.Reads,submits=fake.Submits,advances=fake.Advances;
            Check(play.AdvanceNext(0,10000) is null&&fake.Reads==reads&&fake.Submits==submits&&fake.Advances==advances,"terminal no implicit retry");
            Check(play.State==(mode.StartsWith("State")||mode.StartsWith("WrongI")||mode=="WrongClock"||mode.StartsWith("Pre")?SceneWorldCurrentSpeedPlaybackState.Unavailable:SceneWorldCurrentSpeedPlaybackState.Faulted),"error stage classification");
            play.Dispose();Check(fake.Resets==0&&fake.Disposals==0,"fake remains borrowed");
        }
        var healthy=new ControlledDriver();using var valid=new SceneWorldCurrentSpeedPlanPlayback(composer,plan,healthy,PlaybackPolicy,0,10000);
        foreach(var t in new[]{(-1L,10000L),(0L,9999L),(1L,10000L),(0L,long.MaxValue)})Reject(()=>valid.AdvanceNext(t.Item1,t.Item2));
        Reject(()=>valid.AdvanceNext(0,10000,new CancellationToken(true)));Check(healthy.Reads+healthy.Submits+healthy.Advances==0&&valid.State==SceneWorldCurrentSpeedPlaybackState.Ready,"invalid/cancel zero calls");
        Task.Run(()=>Reject(()=>valid.AdvanceNext(0,10000))).GetAwaiter().GetResult();Task.Run(()=>Reject(()=>valid.Dispose())).GetAwaiter().GetResult();Task.Run(()=>Reject(()=>_ = valid.State)).GetAwaiter().GetResult();
        healthy.OnState=()=>Reject(()=>_ = valid.State);healthy.OnSubmit=()=>Reject(()=>valid.AdvanceNext(0,10000));var saved=valid.AdvanceNext(0,10000)!;healthy.OnState=healthy.OnSubmit=null;
        healthy.ThrowAtAdvance=2;Check(valid.AdvanceNext(10000,20000) is null&&valid.State==SceneWorldCurrentSpeedPlaybackState.Faulted&&valid.NextIndex==1&&ReferenceEquals(valid.LastSuccessful,saved),"late fail preserves success prefix");
        Check(saved.DriverResult.TimestampMicroseconds==10000&&healthy.Advances==2,"no rollback claim after thrown motion");
        var overflowTimeDriver=new ControlledDriver();using(var overflowTime=new SceneWorldCurrentSpeedPlanPlayback(composer,plan,overflowTimeDriver,PlaybackPolicy,long.MaxValue,10000)){Reject(()=>overflowTime.AdvanceNext(long.MaxValue,long.MaxValue));Check(overflowTimeDriver.Reads+overflowTimeDriver.Submits+overflowTimeDriver.Advances==0,"time overflow before calls");}
        foreach(var limits in new[]{new SceneWorldCurrentSpeedPlaybackLimits(MaximumSteps:1),new(MaximumDriverOperations:5),new(MaximumOutputBytes:511),new(MaximumSteps:129),new(MaximumDriverOperations:385),new(MaximumOutputBytes:65537)})Reject(()=>new SceneWorldCurrentSpeedPlanPlayback(composer,plan,new ControlledDriver(),PlaybackPolicy,0,10000,limits));
        using(var exact=new SceneWorldCurrentSpeedPlanPlayback(composer,plan,new ControlledDriver(),PlaybackPolicy,0,10000,new(2,6,512))){Check(exact.AdvanceNext(0,10000) is not null&&exact.AdvanceNext(10000,20000) is not null,"exact caps");}
        using(var foreignComposer=new SceneWorldSuppliedCurrentSpeedPlan(pairReader))
        {var fd=new ControlledDriver();using var foreign=new SceneWorldCurrentSpeedPlanPlayback(foreignComposer,plan,fd,PlaybackPolicy,0,10000);Check(foreign.AdvanceNext(0,10000) is null&&foreign.State==SceneWorldCurrentSpeedPlaybackState.Unavailable&&fd.Reads+fd.Submits+fd.Advances==0,"foreign source before calls");}
        using(var motion=new SceneWorldObjectMotionStore(w))
        {Check(motion.PublishLocal(pair.View.Target,AppearanceTransform.Identity,1),"motion-only update");var fd=new ControlledDriver();using var live=new SceneWorldCurrentSpeedPlanPlayback(composer,plan,fd,PlaybackPolicy,0,10000);Check(live.AdvanceNext(0,10000) is not null,"motion-only source fresh");}
        var boundedSpeedPlan=composer.Build(pair,0x41300000,new[]{good with{SecondsBits=0,SuppliedRate=new(0,0)}})!;var boundedDriver=new ControlledDriver();using(var bounded=new SceneWorldCurrentSpeedPlanPlayback(composer,boundedSpeedPlan,boundedDriver,PlaybackPolicy,0,10000)){Reject(()=>bounded.AdvanceNext(0,10000));Check(boundedDriver.Reads+boundedDriver.Submits+boundedDriver.Advances==0,"stored speed11 before calls");}
        void UpdateSource(){var v=current.Read(g)!;Check(w.ApplyCreatureView4PartialDelta(g,v.Received!.Source,new(42,0,[new(7,0x40400000)])),"controlled source mutation");}
        foreach(var stage in new[]{"State","Submit","Advance"})
        {var fresh=pairReader.Read(g)!;var pp=composer.Build(fresh,0,new[]{good})!;var fd=new ControlledDriver();using var changed=new SceneWorldCurrentSpeedPlanPlayback(composer,pp,fd,PlaybackPolicy,0,10000);if(stage=="State")fd.OnState=UpdateSource;else if(stage=="Submit")fd.OnSubmit=UpdateSource;else fd.OnAdvance=UpdateSource;
         Check(changed.AdvanceNext(0,10000) is null&&changed.NextIndex==0,"source mutation no successful publish");Check(changed.State==(stage=="State"?SceneWorldCurrentSpeedPlaybackState.Unavailable:SceneWorldCurrentSpeedPlaybackState.Faulted),"mutation phase policy");Check(fd.Submits==(stage=="State"?0:1)&&fd.Advances==(stage=="Advance"?1:0),"exact effects by stage");}
        var staleDriver=new ControlledDriver();using var stale=new SceneWorldCurrentSpeedPlanPlayback(composer,plan,staleDriver,PlaybackPolicy,0,10000);Check(stale.AdvanceNext(0,10000) is null&&staleDriver.Reads+staleDriver.Submits+staleDriver.Advances==0,"stale zero calls");
        var newest=pairReader.Read(g)!;var latestPlan=composer.Build(newest,0,new[]{good})!;
        using(var cancel=new CancellationTokenSource()){var fd=new ControlledDriver{OnSubmit=cancel.Cancel};using var canceled=new SceneWorldCurrentSpeedPlanPlayback(composer,latestPlan,fd,PlaybackPolicy,0,10000);Reject(()=>canceled.AdvanceNext(0,10000,cancel.Token));Check(canceled.State==SceneWorldCurrentSpeedPlaybackState.Faulted&&fd.Submits==1&&fd.Advances==0,"postsubmit cancellation latches");}
        var disposedDriver=new ControlledDriver();using var disposedSource=new SceneWorldCurrentSpeedPlanPlayback(composer,latestPlan,disposedDriver,PlaybackPolicy,0,10000);composer.Dispose();Check(disposedSource.AdvanceNext(0,10000) is null&&disposedDriver.Reads+disposedDriver.Submits+disposedDriver.Advances==0,"disposed composition unavailable before calls");
        valid.Dispose();valid.Dispose();Reject(()=>valid.AdvanceNext(20000,30000));Check(healthy.Resets==0&&healthy.Disposals==0&&pairReader.IsCurrent(newest)&&current.IsCurrent(newest.View),"dispose leaves deeper source alive");
        return checks-beforeChecks;
    }
}
