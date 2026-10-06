using System.Numerics;
using System.Reflection;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;
public static class SceneWorldSuppliedCurrentSpeedPlanScenarios
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
    public static object Run(string input)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(input));var json=doc.RootElement;byte[] baseline=Convert.FromHexString(json.GetProperty("Baseline").GetString()!);
        var profiles=json.GetProperty("Profiles").EnumerateArray().Select(Profile).ToArray();var output=new List<object>();int positive=0,steps=0;
        foreach(var c in json.GetProperty("Cases").EnumerateArray())
        {
            string name=c.GetProperty("Name").GetString()!;var p=profiles[c.GetProperty("Profile").GetInt32()];string pin=p.Sources[path].Sha256;
            using var w=World(pin);long g=Stage(w,baseline,c.GetProperty("Typed").GetBoolean(),c.GetProperty("Finalized").GetBoolean());
            using var current=new SceneWorldCreatureMovementCurrentInputs(w,42,p,pin,Scope,true);
            using var paired=new SceneWorldSuppliedCurrentSpeedInputs(current,Local,(CreatureNominalSpeedPrecision)c.GetProperty("Precision").GetInt32());
            if(c.TryGetProperty("Wire",out var wire))Check(w.Accept(g,Convert.FromHexString(wire.GetString()!)),"synthetic legacy framing");
            SceneWorldSuppliedCurrentSpeedReceipt? pair;
            try{pair=paired.Read(g);}catch(InvalidDataException){output.Add(new {Name=name,Reject="PairInput"});continue;}
            if(pair is null){output.Add(new {Name=name,Reject="NoPair"});continue;}
            long retained=w.RetainedCreatureView4PartialBytes;
            try
            {
                var source=c.GetProperty("Steps").EnumerateArray().Select(Step).ToArray();var original=source.ToArray();
                using var composer=new SceneWorldSuppliedCurrentSpeedPlan(paired,new(I(c,"MaximumSteps",128),I(c,"MaximumOutputBytes",112928),L(c,"MaximumCopyBytes",110592),L(c,"MaximumWork",9445504)));
                var r=composer.Build(pair,c.GetProperty("Initial").GetUInt32(),source,new CancellationToken(c.TryGetProperty("Cancel",out var cancel)&&cancel.GetBoolean()));
                Check(r is not null&&composer.IsCurrent(r)&&paired.IsCurrent(pair),"actual current composition");
                Check(ReferenceEquals(r!.Pair,pair)&&ReferenceEquals(r.Pair.View,pair.View)&&ReferenceEquals(r.Pair.Run,pair.Run)&&ReferenceEquals(r.Pair.Walk,pair.Walk),"pair/view/result identity");
                Check(r.Precision==pair.Precision&&r.Plan.Precision==pair.Precision&&r.Generation==g&&r.RootSha256==pin,"same current precision/provenance");
                if(source.Length>0)source[0]=default;
                for(int i=0;i<r.Steps.Count;i++)
                {
                    Check(r.Steps[i]==original[i],"owned caller step");var child=r.Plan.Steps[i];var s=original[i];
                    Check(child.Source==new CreatureNominalSpeedPlanStep(pair.Run.ReturnBits,pair.Walk.ReturnBits,s.RunFlag,s.Direction,s.Factor,s.SecondsBits,s.SuppliedRate),"actual mapped source");
                }
                Check(typeof(SceneWorldSuppliedCurrentSpeedPlanReceipt).GetProperties().All(v=>v.SetMethod is null),"immutable receipt");
                Reject(()=>((IList<SceneWorldSuppliedCurrentSpeedPlanStep>)r.Steps).Add(default));
                output.Add(new {Name=name,Reject="None",Pair=Pair(name,pair),Plan=Plan(name,r.Plan),Output=r.OutputBytes,Copy=r.ReservedCopyBytes,Reserved=r.ReservedWork,r.OwnWork,OwnCopy=r.OwnCopyBytes,ChildCopy=r.ChildCopyBytes,ChildReserved=r.ChildReservedWork,ActualChild=r.ActualChildWork,Reloads=r.ReaderReloads});positive++;steps+=r.Steps.Count;
            }
            catch(OperationCanceledException){output.Add(new {Name=name,Reject="Canceled"});}
            catch(Exception e) when(e is InvalidDataException or ArgumentException or OverflowException){output.Add(new {Name=name,Reject="Input"});}
            Check(w.RetainedCreatureView4PartialBytes==retained,"no source mutation");
        }
        Lifecycle(profiles[0],baseline);
        return new {Cases=output,PositiveIdentityChecks=positive,OwnedStepChecks=steps,NativeChecks=checks,Repeated=100,LifecyclePassed=true,PriorResultPreserved=true,BorrowedAdapterSurvivesDisposal=true,ReentryGuardInjectionOnly=true,OriginalExecuted=false};
    }
    private static void Lifecycle(SharedCreatureAuthoredMovementInputs p,byte[] baseline)
    {
        string pin=p.Sources[path].Sha256;using var w=World(pin);long g=Stage(w,baseline,true,true);
        using var current=new SceneWorldCreatureMovementCurrentInputs(w,42,p,pin,Scope,true);
        using var paired=new SceneWorldSuppliedCurrentSpeedInputs(current,Local,CreatureNominalSpeedPrecision.NearestEven64);
        Check(w.ApplyCreatureView4PartialDelta(g,null,new(42,0,[new(7,0x40000000),new(11,0x3f800000),new(4,0x3f800000),new(5,0x3f800000)])),"finite current fields");
        var pair=paired.Read(g)!;var good=new SceneWorldSuppliedCurrentSpeedPlanStep(1,new(0x3f800000,0,0),new(1,0),0x3f000000,new(1,0));
        using var composer=new SceneWorldSuppliedCurrentSpeedPlan(paired);var saved=composer.Build(pair,0,new[]{good})!;
        var savedChild=saved.Plan;var savedRow=saved.Plan.Steps[0];uint savedBits=saved.FinalCurrentBits;
        for(int i=0;i<100;i++)Check(composer.Build(pair,0,new[]{good})!.FinalCurrentBits==savedBits,"repeat");
        Reject(()=>composer.Build(pair,0,new[]{good},new CancellationToken(true)));Check(composer.IsCurrent(saved),"preCancel preserves current receipt");
        var overflow=good with {Factor=new(1,512)};Reject(()=>composer.Build(pair,0,new[]{good,overflow}));
        var missing=good with {SuppliedRate=null};Reject(()=>composer.Build(pair,0,new[]{good,missing}));
        Check(saved.FinalCurrentBits==savedBits&&ReferenceEquals(saved.Plan,savedChild)&&ReferenceEquals(saved.Plan.Steps[0],savedRow),"prior actual plan after late failure");
        using(var foreign=new SceneWorldSuppliedCurrentSpeedPlan(paired))Check(!foreign.IsCurrent(saved)&&!composer.IsCurrent(foreign.Build(pair,0,new[]{good})!),"foreign composition");
        using(var foreignPairReader=new SceneWorldSuppliedCurrentSpeedInputs(current,Local,CreatureNominalSpeedPrecision.NearestEven64))Check(composer.Build(foreignPairReader.Read(g)!,0,new[]{good}) is null,"foreign pair");
        Task.Run(()=>Reject(()=>composer.Build(pair,0,new[]{good}))).GetAwaiter().GetResult();Task.Run(()=>Reject(()=>composer.IsCurrent(saved))).GetAwaiter().GetResult();Task.Run(()=>Reject(()=>composer.Dispose())).GetAwaiter().GetResult();
        var busy=typeof(SceneWorldSuppliedCurrentSpeedPlan).GetField("busy",BindingFlags.Instance|BindingFlags.NonPublic)!;busy.SetValue(composer,true);Reject(()=>composer.Build(pair,0,new[]{good}));Reject(()=>composer.IsCurrent(saved));Reject(()=>composer.Dispose());busy.SetValue(composer,false);
        using(var motion=new SceneWorldObjectMotionStore(w)){Check(motion.PublishLocal(pair.View.Target,AppearanceTransform.Identity,1),"motion publication");Check(composer.IsCurrent(saved),"motion-only inputs remain current");}
        Check(w.ApplyCreatureView4PartialDelta(g,pair.View.Received!.Source,new(42,0,[new(7,0x40400000)])),"source revision update");
        Check(!composer.IsCurrent(saved)&&composer.Build(pair,0,new[]{overflow}) is null,"stale pair null before malformed arithmetic");
        var newPair=paired.Read(g)!;var fresh=composer.Build(newPair,0,new[]{good})!;Check(fresh.Plan.Steps[0].Source.RunBits==newPair.Run.ReturnBits,"actual fresh run word");
        composer.Dispose();composer.Dispose();Check(!composer.IsCurrent(fresh),"disposed composition freshness");Reject(()=>composer.Build(newPair,0,new[]{good}));Check(paired.IsCurrent(newPair)&&current.IsCurrent(newPair.View),"borrowed adapter and reader still open");
        using var after=new SceneWorldSuppliedCurrentSpeedPlan(paired);var beforeClose=after.Build(newPair,0,new[]{good})!;paired.Dispose();Check(!after.IsCurrent(beforeClose)&&after.Build(newPair,0,new[]{good}) is null,"borrowed adapter disposal stale null");
        Check(current.Read(g) is not null,"borrowed adapter disposal preserves deeper reader");
    }
}
