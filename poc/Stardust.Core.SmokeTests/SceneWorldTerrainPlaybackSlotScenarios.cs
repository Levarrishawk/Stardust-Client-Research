using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Geometry;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;
public static class SceneWorldTerrainPlaybackSlotScenarios
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
        checks=0;using var document=JsonDocument.Parse(File.ReadAllText(input));var fixture=document.RootElement;
        var baseline=Convert.FromHexString(fixture.GetProperty("Baseline").GetString()!);
        var profiles=fixture.GetProperty("Profiles").EnumerateArray().Select(Profile).ToArray();
        TerrainModernPatchSetSurfaceQuery Surface(string key)=>TerrainModernPatchSetSurfaceQuery.Build(TerrainModernHeightPatchSet.Build(fixture.GetProperty(key).EnumerateArray().Select(Make).ToArray(),TerrainModernPatchEdgePolicy.RequireBitIdenticalActiveSharedEdges,TerrainModernPatchNormalPolicy.RequireBitIdenticalSharedNormals));
        var first=Surface("Terrain");var second=Surface("ChangedTerrain");var outside=Surface("OutsideTerrain");
        var traces=new List<object>();int negatives=0;
        foreach(var test in fixture.GetProperty("Cases").EnumerateArray())
        {
            var authored=profiles[0];string pin=authored.Sources[path].Sha256;
            using var world=World(pin);long generation=Stage(world,baseline,true,true);
            using var motions=new SceneWorldObjectMotionStore(world);using var slot=new SceneWorldTerrainPlaybackSlot(world,motions);
            using var currentInputs=new SceneWorldCreatureMovementCurrentInputs(world,42,authored,pin,Scope,true);
            using var reader=new SceneWorldSuppliedCurrentSpeedInputs(currentInputs,Local,(CreatureNominalSpeedPrecision)test.GetProperty("Precision").GetInt32());
            using var composer=new SceneWorldSuppliedCurrentSpeedPlan(reader);
            Check(slot.Current==null&&slot.ReplacementRevision==0&&slot.AdvanceNext(0,10000)==null,"empty slot");
            var outputs=new List<object>();long clock=0;SceneWorldCurrentSpeedPlaybackReceipt? previousReceipt=null;
            foreach(var segment in test.GetProperty("Segments").EnumerateArray())
            {
                int index=outputs.Count;var old=slot.Current;var oldDriver=old?.Driver;var oldPlayback=old?.Playback;
                Check(world.Accept(generation,Convert.FromHexString(test.GetProperty("Wire").GetString()!)),"fresh received source boundary");
                var pair=reader.Read(generation)!;var source=composer.Build(pair,segment.GetProperty("Initial").GetUInt32(),segment.GetProperty("Steps").EnumerateArray().Select(Step).ToArray())!;
                var target=source.Pair.View.Target;
                if(index==0)Check(motions.PublishLocal(target,AppearanceTransform.Identity with {Translation=new(1.25f,0,7.5f)},0),"initial supplied pose");
                var before=motions.Find(target)!;ulong revision=slot.ReplacementRevision;
                bool Replace(TerrainModernPatchSetSurfaceQuery? q=null,SceneWorldCurrentSpeedPlaybackLimits? limits=null,long? initial=null,CancellationToken token=default)=>slot.TryReplace(composer,source,q??(index==1?second:first),PlaybackPolicy,initial??clock,10000,.25f,playbackLimits:limits,cancellationToken:token);
                Reject(()=>Replace(limits:new(MaximumSteps:3)));Reject(()=>Replace(initial:long.MaxValue));Reject(()=>Replace(token:new CancellationToken(true)));Reject(()=>Replace(outside));negatives+=4;
                Check(ReferenceEquals(old,slot.Current)&&slot.ReplacementRevision==revision&&ReferenceEquals(before,motions.Find(target)),"failed candidate keeps prior owner/revision/row");
                Check(Replace(),"actual successful candidate replacement");var owned=slot.Current!;
                Check(slot.ReplacementRevision==(ulong)index+1&&ReferenceEquals(owned.Source,source)&&ReferenceEquals(owned.World,world)&&ReferenceEquals(owned.Motions,motions)&&ReferenceEquals(owned.Surface,index==1?second:first),"actual published identities/revision/surface");
                if(old!=null)
                {Check(old.State==SceneWorldCurrentSpeedPlaybackState.Disposed&&old.Source==null&&!oldDriver!.State.Active&&oldPlayback!.State==SceneWorldCurrentSpeedPlaybackState.Disposed,"old children retired before new owner publication");Check(!slot.IsCurrent(previousReceipt!),"previous receipt no longer slot-current");}
                var rows=new List<object>();
                for(int step=0;step<4;step++)
                {
                    if(index==2&&step==1)
                    {var beforeCorrection=motions.Find(target)!;Check(motions.PublishAuthoritative(target,beforeCorrection.Transform with {Translation=new(1.25f,99,beforeCorrection.Transform.Translation.Z+.125f)},0,1),"authoritative correction");}
                    var result=slot.AdvanceNext(clock,clock+10000)!;Check(result!=null&&ReferenceEquals(result.Source,source)&&ReferenceEquals(result.DriverResult,owned.Playback!.LastAttemptedDriverResult)&&slot.IsCurrent(result),"delegated actual result/source");
                    var pose=result!.DriverResult;Check(pose.Transform==motions.Find(target)!.Transform,"actual selected publication");
                    rows.Add(new {Segment=index,Step=step,result.StoredBits,Time=pose.TimestampMicroseconds,Speed=BitConverter.SingleToUInt32Bits(pose.Speed),Position=V(pose.Transform.Translation),X=V(pose.Transform.RowX),Y=V(pose.Transform.RowY),Z=V(pose.Transform.RowZ)});clock+=10000;previousReceipt=result;
                }
                Check(owned.State==SceneWorldCurrentSpeedPlaybackState.Complete,"completed plan replace at actual current driver clock");
                // Foreign actual source has equal numeric identifiers but unrelated object identity.
                using(var foreign=World(pin))using(var fm=new SceneWorldObjectMotionStore(foreign))
                {
                    long fg=Stage(foreign,baseline,true,true);Check(foreign.Accept(fg,Convert.FromHexString(test.GetProperty("Wire").GetString()!)),"foreign frame");
                    using var ci=new SceneWorldCreatureMovementCurrentInputs(foreign,42,authored,pin,Scope,true);
                    using var rd=new SceneWorldSuppliedCurrentSpeedInputs(ci,Local,(CreatureNominalSpeedPrecision)test.GetProperty("Precision").GetInt32());using var cp=new SceneWorldSuppliedCurrentSpeedPlan(rd);
                    var fr=cp.Build(rd.Read(fg)!,0,segment.GetProperty("Steps").EnumerateArray().Select(Step).ToArray())!;
                    var retained=motions.Find(target);Check(!slot.TryReplace(cp,fr,first,PlaybackPolicy,clock,10000)&&ReferenceEquals(owned,slot.Current)&&ReferenceEquals(retained,motions.Find(target)),"foreign world source no candidate publication");
                    Reject(()=>new SceneWorldTerrainPlaybackSlot(world,fm));negatives+=2;
                }
                outputs.Add(new {Index=index,Revision=slot.ReplacementRevision,Rows=rows});
            }
            var retainedBinding=slot.Current!;var retainedDriver=retainedBinding.Driver!;var retainedPlayback=retainedBinding.Playback!;var lastMotion=motions.Find(world.Snapshots().Single())!;
            slot.RetireSelection();slot.RetireSelection();Check(slot.Current==null&&slot.ReplacementRevision==3&&!slot.IsCurrent(previousReceipt!)&&!retainedDriver.State.Active&&retainedPlayback.State==SceneWorldCurrentSpeedPlaybackState.Disposed,"explicit idempotent selection retirement");
            Check(ReferenceEquals(lastMotion,motions.Find(world.Snapshots().Single()))&&first.Query(1.25,7.5)!=null&&second.Query(1.25,7.5)!=null,"borrowed row/queries survive");
            var sourceAgain=composer.Build(reader.Read(generation)!,0,test.GetProperty("Segments")[0].GetProperty("Steps").EnumerateArray().Select(Step).ToArray())!;
            Check(slot.TryReplace(composer,sourceAgain,first,PlaybackPolicy,clock,10000,.25f)&&slot.ReplacementRevision==4,"replacement after explicit retirement");
            var installed=slot.Current!;var motionBeforeInvalid=motions.Find(sourceAgain.Pair.View.Target)!;
            Check(world.Accept(generation,Convert.FromHexString(test.GetProperty("Wire").GetString()!)),"invalidate installed source");
            Check(!slot.TryReplace(composer,sourceAgain,second,PlaybackPolicy,clock,10000)&&ReferenceEquals(installed,slot.Current)&&ReferenceEquals(motionBeforeInvalid,motions.Find(world.Snapshots().Single()))&&slot.ReplacementRevision==4,"stale candidate retains owner/publication/revision");negatives++;
            var refreshed=composer.Build(reader.Read(generation)!,0,test.GetProperty("Segments")[0].GetProperty("Steps").EnumerateArray().Select(Step).ToArray())!;
            Check(slot.TryReplace(composer,refreshed,second,PlaybackPolicy,clock,10000,.25f)&&slot.ReplacementRevision==5,"actual fresh source replacement");
            using(var separateStore=new SceneWorldObjectMotionStore(world))using(var separateSlot=new SceneWorldTerrainPlaybackSlot(world,separateStore))
            {
                Check(separateSlot.TryReplace(composer,refreshed,first,PlaybackPolicy,clock,10000),"install before borrowed store closure");var beforeClose=separateSlot.Current!;
                separateStore.Dispose();Check(!separateSlot.TryReplace(composer,refreshed,second,PlaybackPolicy,clock,10000)&&ReferenceEquals(beforeClose,separateSlot.Current)&&separateSlot.ReplacementRevision==1,"closed store retains installed owner/revision");negatives++;
            }
            var ownerCheck=Task.Run(()=>{try{_=slot.Current;return false;}catch(InvalidOperationException){return true;}}).GetAwaiter().GetResult();Check(ownerCheck,"foreign thread rejected");negatives++;
            Check(world.Accept(generation,SceneObjectLifecycleCodec.EncodeDestroy(new(42,0)))&&world.Accept(generation,Create())&&world.Accept(generation,baseline)&&world.Accept(generation,SceneObjectLifecycleCodec.EncodeEndBaselines(new(42))),"reincarnated numeric object");
            Check(slot.AdvanceNext(clock,clock+10000)==null&&!slot.TryReplace(composer,refreshed,first,PlaybackPolicy,clock,10000)&&slot.ReplacementRevision==5,"old incarnation unavailable no reattachment");negatives++;
            composer.Dispose();Check(!slot.TryReplace(composer,refreshed,first,PlaybackPolicy,clock,10000)&&slot.ReplacementRevision==5,"retired composition candidate unavailable");negatives++;
            slot.Dispose();slot.Dispose();Check(slot.Current==null&&slot.IsDisposed,"idempotent slot disposal");Reject(()=>slot.TryReplace(composer,refreshed,first,PlaybackPolicy,clock,10000));negatives++;
            using(var closedWorld=World(pin))using(var closedStore=new SceneWorldObjectMotionStore(closedWorld))using(var closedSlot=new SceneWorldTerrainPlaybackSlot(closedWorld,closedStore))
            {closedStore.Dispose();Check(!closedSlot.TryReplace(composer,refreshed,first,PlaybackPolicy,clock,10000)&&closedSlot.Current==null&&closedSlot.ReplacementRevision==0,"closed store candidate unavailable");negatives++;}
            traces.Add(new {Precision=test.GetProperty("Precision").GetInt32(),Segments=outputs,FinalRevision=slot.ReplacementRevision});
        }
        return new {Traces=traces,NativeChecks=checks,NegativeChecks=negatives};
    }
}
