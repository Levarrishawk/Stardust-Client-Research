using System.Buffers.Binary;
using System.Reflection;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;
public static class SceneWorldReceivedCreatureMovementInputScenarios
{
    private static int checks;
    private static void Check(bool value,string why) { checks++; if(!value) throw new InvalidDataException(why); }
    private static void Reject(Action action)
    { bool failed=false;try{action();}catch(Exception e)when(e is InvalidDataException or InvalidOperationException or ArgumentException or NotSupportedException){failed=true;}Check(failed,"expected rejection"); }
    private static readonly string path="object/synthetic_round47.iff",pin=new('0',64);
    private static readonly uint crc=LegacyAssetPath.Checksum(path);
    private static readonly LogicalAnimationClipSelection idle=new("appearance/lat/synthetic.lat","synthetic_idle",pin);
    private static readonly IReadOnlyDictionary<string,LogicalAnimationClipSelection> movement=new Dictionary<string,LogicalAnimationClipSelection>{{"walk",idle},{"run",idle}};
    private static SceneWorldCoordinator World(bool enabled=true)=>new(
        new ObjectTemplateCatalog(new[]{new ObjectTemplateCatalogEntry(crc,path,pin,pin,SceneObjectResourceRecipe.CreatureDetail0)}),
        new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{SceneObjectResourceRecipe.CreatureDetail0,new SceneCreatureResourceOptions(idle,movement,false)}},
        (_,_)=>throw new InvalidDataException("No synthetic resource loads"),SceneCreateZeroQuaternionPolicy.Reject,4,2,32,4194304,32,4194304,1048576,
        creatureView4PartialPolicy:enabled?new(new(MessageLimits:new(MaximumPayloadBytes:1048576)),new()):null);
    private static byte[] Start(){using var s=new MemoryStream();using var w=new BinaryWriter(s);w.Write((ushort)9);w.Write(ZoneSceneProtocolCodec.CmdStartSceneOpcode);w.Write((byte)0);w.Write(42UL);w.Write((ushort)0);w.Write(0f);w.Write(0f);w.Write(0f);w.Write((ushort)0);w.Write(0UL);return s.ToArray();}
    private static byte[] Create()=>SceneObjectLifecycleCodec.EncodeCreate(new(42,0,0,0,1,0,0,0,crc,0));
    private static long Bare(SceneWorldCoordinator w){long g=w.BeginSelection(42).Generation;Check(w.Accept(g,Start()),"start");Check(w.Accept(g,Create()),"create");Check(w.CompleteMinimumSceneBarrier(g,true)is not null,"barrier");return g;}
    private static CreatureView4Delta Delta(ushort field=6,ulong bits=ulong.MaxValue)=>new(42,65535,[new(field,bits)]);
    private static object Row(SceneWorldReceivedCreatureMovementReceipt r)=>new{r.Generation,r.Incarnation.SceneGeneration,r.Incarnation.CreationRevision,HandleRevision=r.Object.Handle.Revision,r.PublicationRevision,r.StateRevision,r.Phase,r.IsOperationalPhase,r.DecodedBytes,SourceKnownMask=r.Source.State.KnownMask,Fields=r.Fields.Select(f=>new{f.Index,f.RawBits,f.Provenance,f.Kind,f.Eligibility}).ToArray()};
    public static void Run(string fixture,string output)
    {
        using var json=JsonDocument.Parse(File.ReadAllText(fixture));var cases=new List<object>();
        foreach(var c in json.RootElement.EnumerateArray())
        {
            using var w=World();long g=Bare(w);using var reader=new SceneWorldReceivedCreatureMovementInputs(w);
            using var finalized=new SceneWorldReceivedCreatureMovementInputs(w,new(SceneReceivedMovementInspectionPhase.FinalizedOnly));
            Check(reader.Read(g,42UL)is null,"no received publication means no fabricated receipt");
            SceneWorldReceivedCreatureMovementReceipt? previous=null;var rows=new List<object>();
            foreach(var a in c.GetProperty("Actions").EnumerateArray())
            {
                string kind=a.GetProperty("Kind").GetString()!;
                switch(kind)
                {
                    case "Delta":Check(w.Accept(g,Convert.FromHexString(a.GetProperty("Wire").GetString()!)),"literal delta");break;
                    case "End":Check(w.Accept(g,SceneObjectLifecycleCodec.EncodeEndBaselines(new(42))),"end");break;
                    case "Contain":Check(w.Accept(g,SceneObjectLifecycleCodec.EncodeContainment(new(42,0,1))),"contain");break;
                    case "Destroy":Check(w.Accept(g,SceneObjectLifecycleCodec.EncodeDestroy(new(42,0))),"destroy");break;
                    case "Create":Check(w.Accept(g,Create()),"recreate");break;
                    case "Reset":g=Bare(w);break;
                    default:throw new InvalidDataException("unknown synthetic action");
                }
                bool oldCurrent=previous is not null&&reader.IsCurrent(previous),sourceCurrent=previous is not null&&w.IsCurrent(previous.Source);
                var current=reader.Read(g,42UL);var final=finalized.Read(g,42UL);Check(current is null||reader.IsCurrent(current),"current received receipt");
                if(current is not null)
                {
                    Check(current.Fields.Count==10&&current.DecodedBytes==544,"fixed output");
                    foreach(var f in current.Fields)Check(ReferenceEquals(current.Get(f.Index),f),"fixed exact slot lookup");
                    Check(final is not null==current.IsOperationalPhase,"finalized-only inspection");
                    var again=reader.Read(g,current.Source)!;Check(reader.IsCurrent(current)&&reader.IsCurrent(again),"repeat reads do not invalidate source");
                }
                rows.Add(new{Kind=kind,Current=current is null?null:Row(current),OldCurrent=oldCurrent,SourceCurrent=sourceCurrent,FinalizedRead=final is not null});previous=current;
            }
            cases.Add(rows);
        }
        Negatives();File.WriteAllText(output,JsonSerializer.Serialize(new{Cases=cases,Checks=checks}));Console.WriteLine($"R47_RECEIVED_MOVEMENT_PASS cases={cases.Count} checks={checks}");
    }
    private static void Negatives()
    {
        using var w=World();long g=Bare(w);Check(w.ApplyCreatureView4PartialDelta(g,null,Delta()),"listen-only publication");var source=w.FindCreatureView4Partial(g,42)!;
        using var exact=new SceneWorldReceivedCreatureMovementInputs(w,new(MaximumOutputBytes:544));var absent=exact.Read(g,source)!;
        Check(absent.Fields.All(f=>f.RawBits is null&&f.Provenance is null&&f.Kind==SceneReceivedMovementValueKind.Missing),"listen is not movement/default seed");
        Check(!absent.IsOperationalPhase&&absent.Fields.All(f=>f.Eligibility==SceneReceivedMovementEligibility.Missing),"pending inspection has no operational fields");
        foreach(int cap in new[]{0,543})using(var limited=new SceneWorldReceivedCreatureMovementInputs(w,new(MaximumOutputBytes:cap)))
        {
            var batch=w.DrainCreatureView4PartialEvents(g)!;var changes=w.DrainChanges(g)!;long bytes=w.RetainedCreatureView4PartialBytes;
            Reject(()=>limited.Read(g,source));Check(w.IsCurrent(source)&&exact.IsCurrent(absent)&&w.IsCurrent(changes)&&w.IsCurrentCreatureView4PartialEvent(batch,0)&&w.RetainedCreatureView4PartialBytes==bytes,"atomic read budget failure");
        }
        using(var bounded=new SceneWorldReceivedCreatureMovementInputs(w,new(MaximumOutputBytes:65536)))Check(bounded.Read(g,source)!.DecodedBytes==544,"bounded maximum policy still fixed output");
        Reject(()=>new SceneWorldReceivedCreatureMovementInputs(w,new(MaximumOutputBytes:-1)));Reject(()=>new SceneWorldReceivedCreatureMovementInputs(w,new(MaximumOutputBytes:65537)));Reject(()=>new SceneWorldReceivedCreatureMovementInputs(w,new((SceneReceivedMovementInspectionPhase)99)));
        foreach(ushort index in new ushort[]{2,3,6,13,ushort.MaxValue})Reject(()=>absent.Get(index));
        Reject(()=>((IList<SceneReceivedCreatureMovementField>)absent.Fields)[0]=absent.Fields[1]);Check(absent.Get(0).RawBits is null,"immutable fields");
        using var other=World();long og=Bare(other);Check(other.ApplyCreatureView4PartialDelta(og,null,Delta(0,0)),"foreign source");
        Check(exact.Read(g,other.FindCreatureView4Partial(og,42)!)is null&&exact.Read(g-1,source)is null&&exact.Read(g,99UL)is null,"foreign/stale/missing reads");
        using(var foreignReader=new SceneWorldReceivedCreatureMovementInputs(other))Check(!foreignReader.IsCurrent(absent),"foreign-world receipt");
        using(var sameWorldReader=new SceneWorldReceivedCreatureMovementInputs(w))Check(!sameWorldReader.IsCurrent(absent),"foreign consumer exact owner");
        Task.Run(()=>Reject(()=>exact.Read(g,source))).GetAwaiter().GetResult();Task.Run(()=>Reject(()=>exact.IsCurrent(absent))).GetAwaiter().GetResult();Task.Run(()=>Reject(()=>exact.Dispose())).GetAwaiter().GetResult();Task.Run(()=>Reject(()=>new SceneWorldReceivedCreatureMovementInputs(w))).GetAwaiter().GetResult();Check(exact.IsCurrent(absent),"off-thread failures preserve owner");
        w.Accept(g,SceneObjectLifecycleCodec.EncodeEndBaselines(new(42)));Check(!exact.IsCurrent(absent)&&w.IsCurrent(source),"phase-context receipt invalidation independent of partial publication");
        var finalized=exact.Read(g,source)!;Check(finalized.IsOperationalPhase&&finalized.Fields.All(f=>f.RawBits is null),"phase eligibility never fills missing fields");
        Check(w.ApplyCreatureView4PartialDelta(g,source,new(42,0,[new(7,0x80000000),new(11,0xBF800000),new(0,0x7F800001),new(1,0xFFC01234),new(4,0)])),"raw finite/nonfinite inputs");
        var raw=exact.Read(g,42UL)!;Check(raw.Get(7).RawBits==0x80000000&&raw.Get(11).RawBits==0xBF800000&&raw.Get(7).Eligibility==SceneReceivedMovementEligibility.FiniteReceived&&raw.Get(11).Eligibility==SceneReceivedMovementEligibility.FiniteReceived,"finite raw negative words preserved without speed selection");
        Check(raw.Get(0).RawBits==0x7F800001&&raw.Get(1).RawBits==0xFFC01234&&raw.Get(0).Kind==SceneReceivedMovementValueKind.NonFinite&&raw.Get(0).Eligibility==SceneReceivedMovementEligibility.NonFinite&&raw.Get(4).RawBits==0,"nonfinite is present and zero is present");Check(!exact.IsCurrent(finalized),"publication currency");
        var retainedFields=raw.Fields.Select(f=>(f.Index,f.RawBits,f.Provenance)).ToArray();Check(w.ApplyCreatureView4PartialDelta(g,raw.Source,Delta(6,123)),"listen-only source revision");var listened=exact.Read(g,42UL)!;
        Check(!exact.IsCurrent(raw)&&listened.Fields.Select(f=>(f.Index,f.RawBits,f.Provenance)).SequenceEqual(retainedFields)&&listened.StateRevision==raw.StateRevision+1,"exact publication changes even when all movement data stays equal");
        w.Accept(g,SceneObjectLifecycleCodec.EncodeEndBaselines(new(42)));Check(exact.IsCurrent(listened),"already finalized no-op preserves exact registry context");
        w.Accept(g,SceneObjectLifecycleCodec.EncodeCreate(new(99,0,0,0,1,0,0,0,crc,0)));Check(exact.IsCurrent(listened),"unrelated object does not invalidate source context");
        var registry=(SceneObjectRegistry)typeof(SceneWorldCoordinator).GetField("registry",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(w)!;registry.Replace(registry.SceneGeneration,registry.Find(42)!.Create);Check(!exact.IsCurrent(raw)&&exact.Read(g,raw.Source)is null,"replacement stale");Check(w.ApplyCreatureView4PartialDelta(g,null,Delta(0,0)),"replacement fresh");var fresh=exact.Read(g,42UL)!;Check(fresh.Get(0).RawBits==0&&fresh.Get(7).RawBits is null,"replacement unknown fields");
        exact.Dispose();exact.Dispose();Check(!exact.IsCurrent(fresh)&&w.IsCurrent(fresh.Source),"dispose clears consumer but borrows world");Check(typeof(SceneWorldReceivedCreatureMovementInputs).GetField("world",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(exact)is null,"dispose clears borrowed reference");Reject(()=>exact.Read(g,fresh.Source));
        using(var closedReader=new SceneWorldReceivedCreatureMovementInputs(w)){var before=closedReader.Read(g,42UL)!;w.Close();Check(!closedReader.IsCurrent(before)&&closedReader.Read(g,before.Source)is null&&closedReader.Read(g,42UL)is null,"closed world rejection");}
        using(var opaque=World(false))Reject(()=>new SceneWorldReceivedCreatureMovementInputs(opaque));
    }
}
