using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;

/// <summary>Synthetic offline oracle against accepted codecs, registry and standalone TANO store.</summary>
public static class TangibleMixedTranscriptScenarios
{
    private const uint Seed=0xA1B2C3D4;
    private const ulong Generation=42;
    private static readonly CreatureView3BaselineLimits BaseLimits=new(4096,1024,1024,64,4096);
    private static readonly TangibleView3DeltaLimits TanoLimits=new(4096,64,64,64,1024,4096,64,64,64,64,1024,1024);
    private static readonly CreatureView3NamedDeltaLimits CreoLimits=new(new(new(4096,64,64,64,1024,4096,64,64,64,64,64,64),64),64,1024,1024);
    private static void Check(bool condition,string reason){if(!condition)throw new Exception(reason);}
    private static byte[] Bytes(JsonElement e,string name)=>Convert.FromHexString(e.GetProperty(name).GetString()!);
    private static string Hex(byte[] bytes)=>Convert.ToHexString(bytes);
    private static (object Value,byte[] Encoded) Owned(string kind,byte[] wire)
    {
        object value;Func<byte[]> encode;
        switch(kind)
        {
            case "create":var c=SceneObjectLifecycleCodec.DecodeCreate(wire);value=c;encode=()=>SceneObjectLifecycleCodec.EncodeCreate(c);break;
            case "destroy":var d=SceneObjectLifecycleCodec.DecodeDestroy(wire);value=d;encode=()=>SceneObjectLifecycleCodec.EncodeDestroy(d);break;
            case "end":var e=SceneObjectLifecycleCodec.DecodeEndBaselines(wire);value=e;encode=()=>SceneObjectLifecycleCodec.EncodeEndBaselines(e);break;
            case "creo_baseline":var cb=CreatureView3BaselineCodec.Decode(wire,BaseLimits);value=cb;encode=()=>CreatureView3BaselineCodec.Encode(cb,BaseLimits);break;
            case "tano_baseline":var tb=TangibleView3BaselineCodec.Decode(wire,BaseLimits);value=tb;encode=()=>TangibleView3BaselineCodec.Encode(tb,BaseLimits);break;
            case "creo_delta":var cd=CreatureView3NamedDeltaCodec.Decode(wire,CreoLimits);value=cd;encode=()=>CreatureView3NamedDeltaCodec.Encode(cd,CreoLimits);break;
            case "tano_delta":var td=TangibleView3DeltaCodec.Decode(wire,TanoLimits);value=td;encode=()=>TangibleView3DeltaCodec.Encode(td,TanoLimits);break;
            default:throw new ArgumentException("Unknown application kind");
        }
        var original=wire.ToArray();Array.Clear(wire);
        var first=encode();Check(first.SequenceEqual(original),"Owned DTO exact replay");Array.Clear(first);
        return(value,encode());
    }
    private static object Row(string name,SceneTangibleReplicationSnapshot v,long retained)=>new
    {
        Name=name,NameFileHex=Hex(v.Names.NameFile.CopyBytes()),NameKeyHex=Hex(v.Names.NameKey.CopyBytes()),
        v.Names.NameSpacer,NameCustomUnits=v.Names.CustomNameUnits.ToArray(),
        NameFileExtended=v.Names.NameFile.UsesExtendedLength,NameKeyExtended=v.Names.NameKey.UsesExtendedLength,
        CustomizationHex=Hex(v.Customization.CopyBytes()),CustomizationExtended=v.Customization.UsesExtendedLength,
        Components=v.Components.Keys.ToArray(),ComponentCounter=v.Components.Counter,v.TangibleScalars.Visibility,
        AdditionalNames=v.Names.AdditionalDecodedBytes(v.Baseline),RetainedDecodedBytes=retained,
        ComplexityBits=BitConverter.SingleToUInt32Bits(v.TangibleScalars.Complexity).ToString("X8"),
        v.TangibleScalars.ConditionDamage,v.TangibleScalars.UseCount
    };
    public static void Run(string peerPath,string exportPath)
    {
        Check(new FileInfo(peerPath).Length<=1048576,"Bounded input file");
        using var doc=JsonDocument.Parse(File.ReadAllBytes(peerPath));var root=doc.RootElement;
        var apps=root.GetProperty("Applications").EnumerateArray().ToArray();
        var packetInputs=root.GetProperty("PacketsHex").EnumerateArray().ToArray();
        var transcript=root.GetProperty("TranscriptHex").EnumerateArray().ToArray();
        Check(apps.Length<=32 && packetInputs.Length<=64 && transcript.Length<=72,"Replay bounds");
        var catalogBytes=Bytes(root,"CatalogHex");
        Check(Hex(SHA256.HashData(catalogBytes))==root.GetProperty("CatalogSha256").GetString(),"Catalog identity");
        var catalog=ObjectTemplateCatalogProfile.Decode(catalogBytes);Array.Clear(catalogBytes);
        var registry=new SceneObjectRegistry(2,Generation);
        using var store=new SceneTangibleReplicationStore(registry,new(Capacity:1,MaximumDecodedBytes:512));
        var transport=new SoeClientTransport();transport.BeginSession(0x12345678,496);transport.AcceptSessionResponse(Bytes(root,"SessionResponseHex"));
        var packets=new List<string>();var wires=new List<string>();var traces=new List<object>();
        foreach(var p in packetInputs)
        {
            var packet=Convert.FromHexString(p.GetString()!);var wire=SoeDatagramCodec.EncodeUncompressed(packet,Seed,2,496);
            Check(wire.Length<=496,"Datagram cap");packets.Add(Hex(SoeDatagramCodec.Decode(wire,Seed,2,496)));wires.Add(Hex(wire));
        }
        Check(packetInputs.Count(p=>Convert.FromHexString(p.GetString()!)[1]==13) is >0 and <=8,"Fragment bound and occurrence");
        var encodedApps=new List<string>();var rows=new List<object>();var revisions=new List<ulong>();
        SceneObjectIncarnation? oldIncarnation=null;SceneTangibleReplicationSnapshot? oldSnapshot=null;
        bool incarnationChecks=false;int index=0,negatives=0;
        void Reject(Action action){negatives++;try{action();}catch(InvalidDataException){return;}throw new Exception("Negative accepted");}
        foreach(var text in transcript)
        {
            var incoming=transport.AcceptDatagram(Convert.FromHexString(text.GetString()!));
            traces.Add(new{Rejected=incoming.RejectedFraming,Delivered=incoming.ApplicationPayloads.Select(Hex).ToArray(),Controls=incoming.ControlDatagrams.Select(Hex).ToArray()});
            foreach(var bytes in incoming.ApplicationPayloads)
            {
                Check(index<apps.Length,"No duplicate delivery");var app=apps[index++];
                Check(Hex(bytes)==app.GetProperty("Hex").GetString(),"Ordered whole message delivery");
                string kind=app.GetProperty("Kind").GetString()!,name=app.GetProperty("Name").GetString()!;
                var(value,encoded)=Owned(kind,bytes);encodedApps.Add(Hex(encoded));
                switch(value)
                {
                    case SceneCreateObjectByCrcMessage c:
                        _=catalog.Require(c.TemplateCrc);var created=registry.Create(Generation,c)!;
                        if(name=="static_recreate")
                        {
                            Check(oldIncarnation is {} prior && prior!=created.Incarnation && !registry.IsCurrent(prior),"Recreate changes incarnation");
                            Check(oldSnapshot is not null && store.Apply(oldSnapshot,TangibleView3DeltaCodec.Decode(Bytes(root.GetProperty("TangibleStage").GetProperty("Updates")[0],"Hex"),TanoLimits)) is null,"Stale store snapshot rejected");
                            incarnationChecks=true;
                        }
                        break;
                    case SceneEndBaselinesMessage e:Check(registry.EndBaselines(Generation,e),"End barrier");break;
                    case SceneDestroyObjectMessage d:
                        oldIncarnation=registry.Find(d.ObjectId)!.Incarnation;oldSnapshot=store.Find(oldIncarnation.Value);
                        Check(registry.Destroy(Generation,d) && store.Find(oldIncarnation.Value) is null && store.RetainedDecodedBytes==0,"Destroy retires tangible");break;
                    case TangibleView3BaselineSnapshot b:
                        Check(b.Components.Values.SequenceEqual(new[]{9,9,-3}),"Raw baseline duplicate order retained");
                        _=store.Seed(registry.Find(b.ObjectId)!.Incarnation,b);break;
                    case TangibleView3DeltaPlan p:
                        Check(registry.Find(p.ObjectId)!.Phase==SceneObjectPhase.Finalized,"Finalized static delta");
                        _=store.Apply(registry.Find(p.ObjectId)!.Incarnation,p);break;
                    case CreatureView3BaselineSnapshot b:Check(registry.Find(b.ObjectId) is not null,"Created CREO baseline recipient");break;
                    case CreatureView3NamedDeltaPlan p:Check(registry.Find(p.ObjectId)!.Phase==SceneObjectPhase.Finalized,"Finalized CREO delta recipient");break;
                }
                if(app.TryGetProperty("Expected",out var expected))
                {
                    var v=store.Snapshots().Single();var row=Row(name,v,store.RetainedDecodedBytes);
                    Check(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(row),JsonNode.Parse(expected.GetRawText())),"Independent tangible state row: "+name);
                    rows.Add(row);revisions.Add(v.Revision);
                }
                if(name=="restored_bytes")
                {
                    var stage=root.GetProperty("TangibleStage");var current=store.Snapshots().Single();long before=store.RetainedDecodedBytes;ulong revision=store.Revision;
                    Reject(()=>TangibleView3DeltaCodec.Decode(Bytes(stage,"LateUnsupported"),TanoLimits));
                    var growth=TangibleView3DeltaCodec.Decode(Bytes(stage,"IntermediateOverBudget"),TanoLimits);
                    Reject(()=>store.Apply(current,growth));
                    Check(ReferenceEquals(current,store.Find(current.Incarnation)) && before==store.RetainedDecodedBytes && revision==store.Revision,"Late growth atomic rollback");
                }
                int[] offsets=kind.Contains("baseline")||kind.Contains("delta")?[0,2,14,18,19]:[0,2];
                foreach(int offset in offsets){var bad=encoded.ToArray();bad[offset]^=1;Reject(()=>Owned(kind,bad));}
                foreach(int n in new[]{0,1,5,13,encoded.Length-1}.Distinct())Reject(()=>Owned(kind,encoded[..n]));
                Reject(()=>Owned(kind,encoded.Concat(new byte[]{0}).ToArray()));
            }
        }
        Check(index==apps.Length && rows.Count==9 && revisions.SequenceEqual(Enumerable.Range(1,9).Select(i=>(ulong)i)),"All replay records consumed");
        // A reliable packet without its full sequence field must fail before application delivery.
        var shortPacket=SoeDatagramCodec.EncodeUncompressed(new byte[]{0,9,0},Seed,2,496);
        var isolated=new SoeClientTransport();isolated.BeginSession(0x12345678,496);isolated.AcceptSessionResponse(Bytes(root,"SessionResponseHex"));
        Reject(()=>isolated.AcceptDatagram(shortPacket));isolated.Close();
        var channel=new SoeReliableChannel();
        Reject(()=>channel.ReceiveDataPacket(new byte[]{0,9,0,51,1}));
        Check(channel.ExpectedInboundSequence==0,"Far future sequence rejection preserves cursor");
        Check(channel.ReceiveDataPacket(new byte[]{0,9,0,0,1}).DeliveredPayloads.Single().SequenceEqual(new byte[]{1}),"Sequence recovery");
        var fragment=new SoeFragmentReassembler(4096);
        foreach(var bad in new[]{new byte[]{0,0,0},new byte[]{0,0,0,0},new byte[]{0,0,16,1},new byte[]{0,0,0,1,1,2}})Reject(()=>fragment.Accept(bad));
        Check(!fragment.IsActive,"Fragment failure drains");
        Reject(()=>TangibleView3BaselineCodec.Decode(Bytes(apps[2],"Hex"),BaseLimits));
        Reject(()=>CreatureView3BaselineCodec.Decode(Bytes(apps[3],"Hex"),BaseLimits));
        transport.Close();store.Close();Check(store.RetainedDecodedBytes==0,"Store close drains");
        File.WriteAllText(exportPath,JsonSerializer.Serialize(new{Transport=traces,PacketsHex=packets,WiresHex=wires,ApplicationsHex=encodedApps,StaticRows=rows,Revisions=revisions,IncarnationChecks=incarnationChecks,NegativeChecks=negatives}));
        Console.WriteLine($"Mixed replay: {index} applications, {packets.Count} datagrams, {rows.Count} tangible states, {negatives} bounded negatives");
    }
}
