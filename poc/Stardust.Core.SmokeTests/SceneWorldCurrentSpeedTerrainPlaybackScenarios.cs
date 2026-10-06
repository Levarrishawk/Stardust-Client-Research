using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Geometry;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;
public static class SceneWorldCurrentSpeedTerrainPlaybackScenarios
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
    private static TerrainModernHeightPatch Make(JsonElement c)
    {
        var p=c.GetProperty("Profiles").EnumerateArray().Select(v=>(TerrainHeightArithmeticPrecision)v.GetInt32()).ToArray();
        var grid=TerrainSampleCoordinateGrid.Build(c.GetProperty("Request").Deserialize<TerrainSampleCoordinateRequest>(),p[0],p[1],p[2],p[3]);
        var heights=c.GetProperty("Heights").EnumerateArray().Select(v=>v.GetUInt32()).ToArray();
        var data=TerrainCoordinateHeightPatch.Build(grid,heights,Enumerable.Repeat(Vector3.UnitY,heights.Length).ToArray(),c.GetProperty("Colors").EnumerateArray().Select(v=>v.GetUInt32()).ToArray(),c.GetProperty("Mask").GetInt32(),BitConverter.UInt32BitsToSingle(c.GetProperty("ScaleBits").GetUInt32()));
        return TerrainModernHeightPatch.Build(data,TerrainModernNormalPolicy.ModernBinary64CentralDifference);
    }
    private static uint[] V(Vector3 v)=>[BitConverter.SingleToUInt32Bits(v.X),BitConverter.SingleToUInt32Bits(v.Y),BitConverter.SingleToUInt32Bits(v.Z)];
    public static object Run(string input)
    {
        checks=0;using var doc=JsonDocument.Parse(File.ReadAllText(input));var json=doc.RootElement;
        var baseline=Convert.FromHexString(json.GetProperty("Baseline").GetString()!);
        var profiles=json.GetProperty("Profiles").EnumerateArray().Select(Profile).ToArray();
        var set=TerrainModernHeightPatchSet.Build(json.GetProperty("Terrain").EnumerateArray().Select(Make).ToArray(),TerrainModernPatchEdgePolicy.RequireBitIdenticalActiveSharedEdges,TerrainModernPatchNormalPolicy.RequireBitIdenticalSharedNormals);
        var surface=TerrainModernPatchSetSurfaceQuery.Build(set);var output=new List<object>();int negatives=0,steps=0;
        foreach(var c in json.GetProperty("Cases").EnumerateArray())
        {
            var authored=profiles[c.GetProperty("Profile").GetInt32()];string pin=authored.Sources[path].Sha256;
            using var world=World(pin);long g=Stage(world,baseline,true,true);
            using var current=new SceneWorldCreatureMovementCurrentInputs(world,42,authored,pin,Scope,true);
            using var reader=new SceneWorldSuppliedCurrentSpeedInputs(current,Local,(CreatureNominalSpeedPrecision)c.GetProperty("Precision").GetInt32());
            Check(world.Accept(g,Convert.FromHexString(c.GetProperty("Wire").GetString()!)),"received current values");
            using var composition=new SceneWorldSuppliedCurrentSpeedPlan(reader);var pair=reader.Read(g)!;
            var source=composition.Build(pair,c.GetProperty("Initial").GetUInt32(),c.GetProperty("Steps").EnumerateArray().Select(Step).ToArray())!;
            var target=source.Pair.View.Target;using var motions=new SceneWorldObjectMotionStore(world);
            float start=c.GetProperty("StartZ").GetSingle();Check(motions.PublishLocal(target,AppearanceTransform.Identity with {Translation=new(1.25f,0,start)},0),"seed exact native store");
            SceneWorldCurrentSpeedTerrainPlayback? Bind(SceneWorldObjectMotionStore? m=null,SceneWorldCoordinator? w=null,SceneWorldTerrainMotionLimits? t=null,SceneWorldCurrentSpeedPlaybackLimits? p=null,long initial=0,CancellationToken token=default)=>SceneWorldCurrentSpeedTerrainPlayback.Bind(w??world,m??motions,composition,source,surface,PlaybackPolicy,initial,10000,.25f,t,p,token);
            var prior=motions.Find(target)!;
            using(var foreign=World(pin))using(var fm=new SceneWorldObjectMotionStore(foreign))
            {Stage(foreign,baseline,true,true);Check(Bind(fm)==null&&Bind(motions,foreign)==null,"foreign empty store and equal numeric incarnation world rejected");negatives+=2;}
            using(var closed=new SceneWorldObjectMotionStore(world)){closed.Dispose();Check(Bind(closed)==null,"closed empty store rejected");negatives++;}
            Reject(()=>Bind(p:new(MaximumSteps:0)));Reject(()=>Bind(p:new(MaximumDriverOperations:0)));Reject(()=>Bind(p:new(MaximumOutputBytes:511)));
            Reject(()=>Bind(t:new(MaximumPatches:surface.PatchCount-1)));Reject(()=>Bind(t:new(MaximumTriangleTestsPerCandidate:surface.TotalTriangles-1)));
            Reject(()=>Bind(t:new(MaximumFootOffset:.1f)));Reject(()=>Bind(t:new(MaximumPositionComponent:.1f)));negatives+=2;
            Reject(()=>Bind(initial:long.MaxValue));Reject(()=>Bind(token:new CancellationToken(true)));negatives+=7;
            Check(ReferenceEquals(prior,motions.Find(target)),"all preflight rejects preserve exact row");
            Check(motions.PublishLocal(target,prior.Transform,12),"store accepts speed above playback cap");
            var tooFast=motions.Find(target)!;Reject(()=>Bind());Check(ReferenceEquals(tooFast,motions.Find(target)),"speed state rejection before publication");negatives++;
            Check(motions.PublishLocal(target,prior.Transform,0),"restore supplied initial pose");
            using(var empty=new SceneWorldObjectMotionStore(world))using(var emptyBinding=Bind(empty))
            {Check(emptyBinding!=null&&empty.Find(target)!=null,"same-world empty store accepted and initialized");}
            using var bound=Bind(t:new(MaximumPatches:surface.PatchCount,MaximumTriangleTestsPerCandidate:surface.TotalTriangles),p:new(MaximumSteps:source.Plan.StepCount,MaximumDriverOperations:source.Plan.StepCount*3,MaximumOutputBytes:512))!;Check(bound!=null&&ReferenceEquals(bound.World,world)&&ReferenceEquals(bound.Motions,motions)&&ReferenceEquals(bound.Source,source)&&ReferenceEquals(bound.Surface,surface)&&ReferenceEquals(bound.Composition,composition),"actual borrowed identities");
            var driver=bound!.Driver!;var playback=bound.Playback!;
            Check(ReferenceEquals(surface.Source,set)&&surface.NormalBuilds==0&&surface.GeometryBuilds==0,"retained terrain no rebuild");
            var owner=Task.Run(()=>{try{_=bound.State;return false;}catch(InvalidOperationException){return true;}}).GetAwaiter().GetResult();Check(owner,"owner thread guard");negatives++;
            var admitted=motions.Find(target)!;
            Reject(()=>bound.AdvanceNext(0,10000,new CancellationToken(true)));Reject(()=>bound.AdvanceNext(0,10001));
            Check(playback.SubmitAttempts==0&&playback.AdvanceAttempts==0&&ReferenceEquals(admitted,motions.Find(target)),"cancel and invalid exact clock before driver operation");negatives+=2;
            var rows=new List<object>();SceneWorldCurrentSpeedPlaybackReceipt? last=null;
            for(int i=0;i<source.Plan.StepCount;i++)
            {
                if(i==20&&c.GetProperty("Correction").GetBoolean())
                {var old=motions.Find(target)!;Check(motions.PublishAuthoritative(target,old.Transform with {Translation=new(1.25f,123,old.Transform.Translation.Z+.25f)},0,1),"authoritative correction");Check(composition.IsCurrent(source),"motion-only correction preserves source");}
                var receipt=bound.AdvanceNext(i*10000,(i+1)*10000)!;Check(receipt!=null,"actual terrain playback");
                Check(ReferenceEquals(receipt!.Source,source)&&ReferenceEquals(receipt.Step,source.Plan.Steps[i])&&ReferenceEquals(receipt.DriverResult,playback.LastAttemptedDriverResult)&&ReferenceEquals(driver.State,receipt.DriverResult),"exact source step actual driver identities");
                var state=receipt.DriverResult;var hit=surface.Query(state.Transform.Translation.X,state.Transform.Translation.Z)!;
                Check(ReferenceEquals(hit.Chunk,set.Patches[hit.ChunkIndex])&&ReferenceEquals(hit.Child,surface.Children.Single(x=>x.ChunkIndex==hit.ChunkIndex&&x.PatchIndex==hit.PatchIndex).Query),"hit actual chunk child identities");
                Check(motions.Find(target)!.Transform==state.Transform&&motions.Find(target)!.Speed==state.Speed&&bound.IsCurrent(receipt),"native published row and receipt currency");
                rows.Add(new {Index=i,receipt.StoredBits,Time=state.TimestampMicroseconds,Speed=BitConverter.SingleToUInt32Bits(state.Speed),Position=V(state.Transform.Translation),X=V(state.Transform.RowX),Y=V(state.Transform.RowY),Z=V(state.Transform.RowZ),Chunk=hit.ChunkIndex,Patch=hit.PatchIndex,Triangle=hit.Hit.TriangleIndex,Height=hit.Hit.Height});last=receipt;steps++;
            }
            Check(bound.AdvanceNext(source.Plan.StepCount*10000,(source.Plan.StepCount+1)*10000)==null,"complete no extra step");
            var final=motions.Find(target)!;bound.Retire();Check(!driver.State.Active&&playback.State==SceneWorldCurrentSpeedPlaybackState.Disposed&&bound.Source==null&&bound.Surface==null,"owned retirement and reference release");
            Check(ReferenceEquals(final,motions.Find(target))&&composition.IsCurrent(source)&&surface.Query(1.25,start)!=null,"borrowed survival");
            if(last!=null)Check(last.DriverResult.Transform==final.Transform&&!bound.IsCurrent(last),"historical result survives retirement");
            using var replacement=Bind()!;Check(replacement!=null,"borrowed dependencies reusable");
            Check(world.Accept(g,Convert.FromHexString(c.GetProperty("Wire").GetString()!)),"source replacement");
            var beforeStale=motions.Find(target);Check(replacement!.AdvanceNext(0,10000)==null&&replacement.Playback!.SubmitAttempts==0&&replacement.Playback.AdvanceAttempts==0,"stale source no driver effects");
            Check(Bind()==null&&ReferenceEquals(beforeStale,motions.Find(target)),"stale bind no initial publication");negatives+=2;
            if(c.GetProperty("Name").GetString()=="0")
            {
                // Fresh source after replacement; retain an actual success prefix at the outer terrain boundary.
                var refreshed=composition.Build(reader.Read(g)!,c.GetProperty("Initial").GetUInt32(),c.GetProperty("Steps").EnumerateArray().Select(Step).ToArray())!;
                Check(motions.PublishLocal(refreshed.Pair.View.Target,AppearanceTransform.Identity with {Translation=new(1.25f,0,15.9f)},0),"late fault seed");
                using var late=SceneWorldCurrentSpeedTerrainPlayback.Bind(world,motions,composition,refreshed,surface,PlaybackPolicy,0,10000,.25f)!;
                int accepted=0;SceneWorldCurrentSpeedPlaybackReceipt? prefix=null;
                for(int i=0;i<60;i++){var r=late.AdvanceNext(i*10000,(i+1)*10000);if(r==null)break;prefix=r;accepted++;}
                Check(accepted>0&&accepted<60&&late.State==SceneWorldCurrentSpeedPlaybackState.Faulted&&prefix!=null,"real late outside-terrain failure after prefix");
                var published=motions.Find(refreshed.Pair.View.Target)!;Check(published.Transform==prefix!.DriverResult.Transform&&ReferenceEquals(prefix,late.Playback!.LastSuccessful),"late failure preserves actual accepted row/result prefix");
                Check(late.AdvanceNext(0,10000)==null&&ReferenceEquals(published,motions.Find(refreshed.Pair.View.Target)),"fault terminal no retry publication");negatives+=3;
            }
            if(c.GetProperty("Name").GetString()=="0")
            {
                var refreshed=composition.Build(reader.Read(g)!,c.GetProperty("Initial").GetUInt32(),c.GetProperty("Steps").EnumerateArray().Select(Step).ToArray())!;
                using var lifecycle=SceneWorldCurrentSpeedTerrainPlayback.Bind(world,motions,composition,refreshed,surface,PlaybackPolicy,0,10000,.25f)!;
                Check(world.Accept(g,SceneObjectLifecycleCodec.EncodeDestroy(new(42,0))),"destroy selected incarnation");
                Check(lifecycle.AdvanceNext(0,10000)==null&&lifecycle.Playback!.SubmitAttempts==0,"destroy stale before submission");
                Check(world.Accept(g,Create())&&world.Accept(g,baseline)&&world.Accept(g,SceneObjectLifecycleCodec.EncodeEndBaselines(new(42))),"recreate numeric id");
                Check(SceneWorldCurrentSpeedTerrainPlayback.Bind(world,motions,composition,refreshed,surface,PlaybackPolicy,0,10000)==null,"old source never reattaches recreated incarnation");negatives+=2;
            }
            var retirementTarget=world.Snapshots().Single();var beforeRetirement=motions.Find(retirementTarget);composition.Dispose();
            Check(Bind()==null&&ReferenceEquals(beforeRetirement,motions.Find(retirementTarget)),"retired source unavailable before publication");negatives++;
            output.Add(new {Name=c.GetProperty("Name").GetString(),Rows=rows});
        }
        return new {Cases=output,ExecutedSteps=steps,NativeChecks=checks,NegativeChecks=negatives};
    }
}
