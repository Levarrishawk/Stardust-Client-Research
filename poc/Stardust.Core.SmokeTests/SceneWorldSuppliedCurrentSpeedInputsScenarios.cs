using System.Numerics;
using System.Reflection;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;
public static class SceneWorldSuppliedCurrentSpeedInputsScenarios
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
    private static int I(JsonElement c,string k,int d)=>c.TryGetProperty(k,out var v)?v.GetInt32():d;
    private static readonly CreatureSuppliedCurrentSpeedPolicy Local=CreatureSuppliedCurrentSpeedPolicy.SuppliedLocalOnly;
    public static object Run(string input)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(input));var json=doc.RootElement;byte[] baseline=Convert.FromHexString(json.GetProperty("Baseline").GetString()!);
        var profiles=json.GetProperty("Profiles").EnumerateArray().Select(Profile).ToArray();var output=new List<object>();int positives=0;
        foreach(var c in json.GetProperty("Cases").EnumerateArray())
        {
            var authored=profiles[c.GetProperty("Profile").GetInt32()];string pin=authored.Sources[path].Sha256;
            using var world=World(pin);long g=Stage(world,baseline,c.GetProperty("Typed").GetBoolean(),c.GetProperty("Finalized").GetBoolean());
            using var current=new SceneWorldCreatureMovementCurrentInputs(world,42,authored,pin,Scope,true);
            if(c.TryGetProperty("Wire",out var wire))Check(world.Accept(g,Convert.FromHexString(wire.GetString()!)),"literal original-framing synthetic wire");
            string name=c.GetProperty("Name").GetString()!;long retained=world.RetainedCreatureView4PartialBytes;
            try
            {
                var limits=new SceneWorldSuppliedCurrentSpeedLimits(I(c,"MaximumOutputBytes",2080),I(c,"MaximumCopyBytes",256),I(c,"MaximumWork",16384),I(c,"MaximumReaders",2),I(c,"MaximumReaderCopyBytes",128),I(c,"MaximumReaderWork",8192));
                using var pair=new SceneWorldSuppliedCurrentSpeedInputs(current,(CreatureSuppliedCurrentSpeedPolicy)I(c,"Policy",1),(CreatureNominalSpeedPrecision)c.GetProperty("Precision").GetInt32(),limits);
                var r=pair.Read(g,new CancellationToken(c.TryGetProperty("Cancel",out var cancel)&&cancel.GetBoolean()));
                if(r is null){output.Add(new {Name=name,Reject="Null"});continue;}
                Check(pair.IsCurrent(r)&&current.IsCurrent(r.View),"actual view currency");
                Check(ReferenceEquals(r.RunBase,r.View.Get(7))&&ReferenceEquals(r.WalkBase,r.View.Get(11))&&ReferenceEquals(r.FirstModifier,r.View.Get(4))&&ReferenceEquals(r.SecondModifier,r.View.Get(5)),"actual field identity");
                Check(!ReferenceEquals(r.Run,r.Walk)&&r.Run.BaseBits==r.RunBase.RawBits&&r.Walk.BaseBits==r.WalkBase.RawBits,"actual independent speed receipts");
                Check(r.Generation==g&&r.RootSha256==pin&&r.Incarnation==r.View.Incarnation,"context provenance");
                Check(world.RetainedCreatureView4PartialBytes==retained,"no received mutation");
                Check(typeof(SceneWorldSuppliedCurrentSpeedReceipt).GetProperties().All(p=>p.SetMethod is null),"immutable properties");
                var fields=new[]{r.RunBase,r.WalkBase,r.FirstModifier,r.SecondModifier}.Select(f=>new {f.Index,f.RawBits,Origin=f.Origin.ToString(),Eligibility=f.Eligibility.ToString()});
                output.Add(new {Name=name,Reject="None",r.RootSha256,Fields=fields,Run=Speed(r.Run),Walk=Speed(r.Walk),Output=r.OutputBytes,Copy=r.ReservedCopyBytes,Reserved=r.ReservedWork,r.ActualWork,Counts=new[]{r.ReaderCalls,r.SuppliedWords,r.Multiplications,r.CallerF32Stores}});positives++;
            }
            catch(OperationCanceledException){output.Add(new {Name=name,Reject="Canceled"});}
            catch(Exception e) when(e is InvalidDataException or ArgumentException or OverflowException){output.Add(new {Name=name,Reject="Input"});}
            Check(world.RetainedCreatureView4PartialBytes==retained,"failed reads never seed/mutate");
        }
        Lifecycle(profiles[0],baseline);
        return new {Cases=output,PositiveIdentityChecks=positives,NativeChecks=checks,Repeated=100,LifecyclePassed=true,PriorResultPreserved=true,BorrowedReaderSurvivesAdapterDisposal=true,GuardInjectionOnlyForReentry=true,OriginalExecuted=false};
    }
    private static void Lifecycle(SharedCreatureAuthoredMovementInputs p,byte[] baseline)
    {
        string pin=p.Sources[path].Sha256;using var w=World(pin);long g=Stage(w,baseline,false,false);
        using var current=new SceneWorldCreatureMovementCurrentInputs(w,42,p,pin,Scope,true);
        using var pair=new SceneWorldSuppliedCurrentSpeedInputs(current,Local,CreatureNominalSpeedPrecision.NearestEven64);
        Check(pair.Read(g) is null,"pending/missing typed null");Check(w.Accept(g,baseline),"late typed");Check(pair.Read(g) is null,"pending typed null");
        Check(w.Accept(g,SceneObjectLifecycleCodec.EncodeEndBaselines(new(42))),"finalize");
        Check(w.ApplyCreatureView4PartialDelta(g,null,new(42,0,[new(7,0x40000000),new(11,0x3f800000),new(4,0x3f800000),new(5,0x3f800000)])),"finite seed");
        var saved=pair.Read(g)!;Check(pair.IsCurrent(saved),"initial pair");uint oldRun=saved.Run.ReturnBits,oldWalk=saved.Walk.ReturnBits;
        for(int i=0;i<100;i++)Check(pair.Read(g)!.Run.ReturnBits==oldRun,"repeat");
        Check(pair.Read(g-1) is null,"foreign generation null");using(var other=new SceneWorldSuppliedCurrentSpeedInputs(current,Local,CreatureNominalSpeedPrecision.NearestEven64))Check(!other.IsCurrent(saved)&&!pair.IsCurrent(other.Read(g)!),"foreign adapter");
        Task.Run(()=>Reject(()=>pair.Read(g))).GetAwaiter().GetResult();Task.Run(()=>Reject(()=>pair.IsCurrent(saved))).GetAwaiter().GetResult();Task.Run(()=>Reject(()=>pair.Dispose())).GetAwaiter().GetResult();
        var busy=typeof(SceneWorldSuppliedCurrentSpeedInputs).GetField("busy",BindingFlags.Instance|BindingFlags.NonPublic)!;
        busy.SetValue(pair,true);Reject(()=>pair.Read(g));Reject(()=>pair.IsCurrent(saved));Reject(()=>pair.Dispose());busy.SetValue(pair,false);Check(pair.IsCurrent(saved),"reentry guard recovery");
        Reject(()=>pair.Read(g,new CancellationToken(true)));Check(pair.IsCurrent(saved),"preCancel preserves pair");
        using(var motion=new SceneWorldObjectMotionStore(w))
        { Check(motion.PublishLocal(saved.View.Target,AppearanceTransform.Identity,1),"motion publication");Check(pair.IsCurrent(saved),"motion-only independent source remains current"); }
        Check(w.ApplyCreatureView4PartialDelta(g,saved.View.Received!.Source,new(42,0,[new(7,0x3f800000),new(11,0x7f7fffff),new(4,0x40000000),new(5,0x3f800000)])),"late walk overflow seed");
        Check(!pair.IsCurrent(saved),"source update stale");Reject(()=>pair.Read(g));Check(saved.Run.ReturnBits==oldRun&&saved.Walk.ReturnBits==oldWalk,"prior pair after late arithmetic failure");
        var failedView=current.Read(g)!;
        Check(w.ApplyCreatureView4PartialDelta(g,failedView.Received!.Source,new(42,0,[new(7,0),new(11,0),new(4,0),new(5,0x7fc12345)])),"last field nan seed");Reject(()=>pair.Read(g));
        var nanView=current.Read(g)!;Check(w.ApplyCreatureView4PartialDelta(g,nanView.Received!.Source,new(42,0,[new(7,0x40000000),new(11,0x3f800000),new(4,0x3f800000),new(5,0x3f800000)])),"finite restore");
        var restored=pair.Read(g)!;Check(restored.Run.ReturnBits==oldRun,"recover after failure");
        pair.Dispose();pair.Dispose();Check(!pair.IsCurrent(restored),"disposed pair stale");Reject(()=>pair.Read(g));Check(current.IsCurrent(restored.View)&&current.Read(g) is not null,"borrowed reader remains open");
        using var freshPair=new SceneWorldSuppliedCurrentSpeedInputs(current,Local,CreatureNominalSpeedPrecision.NearestEven64);
        var prior=freshPair.Read(g)!;Check(w.Accept(g,SceneObjectLifecycleCodec.EncodeContainment(new(42,0,1))),"containment");Check(!freshPair.IsCurrent(prior),"registry object stale");
        var beforeDestroy=freshPair.Read(g)!;Check(w.Accept(g,SceneObjectLifecycleCodec.EncodeDestroy(new(42,0))),"destroy");Check(freshPair.Read(g) is null&&!freshPair.IsCurrent(beforeDestroy),"destroy null");
        Check(w.Accept(g,Create()),"recreate");Check(freshPair.Read(g) is null,"old incarnation never rebound");
        long next=Stage(w,baseline,true,true);using var nextCurrent=new SceneWorldCreatureMovementCurrentInputs(w,42,p,pin,Scope,true);
        using var nextPair=new SceneWorldSuppliedCurrentSpeedInputs(nextCurrent,Local,CreatureNominalSpeedPrecision.NearestEven64);
        var bootstrap=nextPair.Read(next);Check(bootstrap is not null,"constructor precedence pair");
        Check(w.ApplyCreatureView4PartialDelta(next,null,new(42,0,[new(6,123)])),"late listen-only first publication");Check(!nextPair.IsCurrent(bootstrap!),"late first receipt stales absence pair");
        Check(freshPair.Read(next) is null&&!freshPair.IsCurrent(prior),"generation stale");
        var beforeClose=nextPair.Read(next)!;w.Close();Check(!nextPair.IsCurrent(beforeClose)&&nextPair.Read(next) is null,"world close null");nextCurrent.Dispose();Check(!nextPair.IsCurrent(beforeClose),"borrowed reader disposal stales pair");Reject(()=>nextPair.Read(next));Reject(()=>new SceneWorldSuppliedCurrentSpeedInputs(nextCurrent,Local,CreatureNominalSpeedPrecision.NearestEven64,new(MaximumOutputBytes:2079)));
    }
}
