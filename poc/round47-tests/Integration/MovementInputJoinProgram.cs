using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

if(args.Length!=4)throw new ArgumentException("asset-root configuration fixture output");
void Need(bool yes,string why){if(!yes)throw new InvalidDataException(why);}
Need(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[1])))=="22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524","configuration identity");
using var text=File.OpenText(args[1]);using var overlay=ConfiguredTreOverlay.Open(args[0],text);
var profile=SharedCreatureMovementInputProfile.Load(overlay.Lookup,"object/creature/player/shared_human_male.iff","E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243");
var authored=SharedCreatureAuthoredMovementInputs.FromProfile(profile);
Need(profile.Sources.Count==5&&profile.SourceBytes==8290,"one pinned necessary authored chain");
var policy=new AuthoredMovementEligibilityPolicy("explicit authored nonnegative selection",true);
using var fixture=JsonDocument.Parse(File.ReadAllBytes(args[2]));
Need(fixture.RootElement.GetProperty("Version").GetInt32()==1&&fixture.RootElement.GetProperty("Synthetic").GetBoolean()&&fixture.RootElement.GetProperty("Cases").GetArrayLength()==72,"bounded independent fixture");
var rows=new List<object>();
foreach(var c in fixture.RootElement.GetProperty("Cases").EnumerateArray())
{
    bool finalized=c.GetProperty("Phase").GetString()=="Finalized";
    string path="object/synthetic_round47_join.iff",pin=new('0',64);uint crc=LegacyAssetPath.Checksum(path);
    var idle=new LogicalAnimationClipSelection("appearance/lat/synthetic.lat","synthetic_idle",pin);
    var catalog=new ObjectTemplateCatalog(new[]{new ObjectTemplateCatalogEntry(crc,path,pin,pin,SceneObjectResourceRecipe.CreatureDetail0)});
    using var world=new SceneWorldCoordinator(catalog,new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{SceneObjectResourceRecipe.CreatureDetail0,new SceneCreatureResourceOptions(idle,new Dictionary<string,LogicalAnimationClipSelection>{{"walk",idle},{"run",idle}},false)}},(_,_)=>throw new InvalidDataException("No scene resources requested by this state-only join"),SceneCreateZeroQuaternionPolicy.Reject,maximumObjects:1,maximumStagedMessages:16,maximumPayloadBytes:1024,creatureView4PartialPolicy:new(new(MessageLimits:new(MaximumPayloadBytes:1024)),new(MaximumObjects:1)));
    long generation=world.BeginSelection(1).Generation;
    using(var stream=new MemoryStream())
    {
        using(var writer=new BinaryWriter(stream,System.Text.Encoding.UTF8,true)){writer.Write((ushort)9);writer.Write(ZoneSceneProtocolCodec.CmdStartSceneOpcode);writer.Write((byte)0);writer.Write(1UL);writer.Write((ushort)0);writer.Write(0f);writer.Write(0f);writer.Write(0f);writer.Write((ushort)0);writer.Write(0UL);}
        Need(world.Accept(generation,stream.ToArray()),"scene start");
    }
    Need(world.Accept(generation,SceneObjectLifecycleCodec.EncodeCreate(new(1,0,0,0,1,0,0,0,crc,0))),"create");
    foreach(var wire in c.GetProperty("PrefixWires").EnumerateArray())Need(world.Accept(generation,Convert.FromHexString(wire.GetString()!)),"received-only staged prefix");
    if(finalized)Need(world.Accept(generation,SceneObjectLifecycleCodec.EncodeEndBaselines(new(1))),"end baseline");
    Need(world.CompleteMinimumSceneBarrier(generation,true) is not null,"bounded barrier");
    long before=world.RetainedCreatureView4PartialBytes;
    using var receiver=new SceneWorldReceivedCreatureMovementInputs(world);
    var receipt=receiver.Read(generation,1)??throw new InvalidDataException("Received receipt missing");Need(receiver.IsCurrent(receipt),"current received receipt");
    using var finalOnly=new SceneWorldReceivedCreatureMovementInputs(world,new(SceneReceivedMovementInspectionPhase.FinalizedOnly));
    Need((finalOnly.Read(generation,1) is not null)==finalized,"explicit phase policy");
    ushort field=c.GetProperty("Field").GetUInt16();var observed=receipt.Get(field);uint? bits=observed.RawBits;
    bool receivedReady=receipt.IsOperationalPhase&&observed.Eligibility==SceneReceivedMovementEligibility.FiniteReceived&&BitConverter.UInt32BitsToSingle(bits!.Value)>=0;
    var db=c.GetProperty("DirectionBits");var direction=new Vector2(BitConverter.UInt32BitsToSingle(db[0].GetUInt32()),BitConverter.UInt32BitsToSingle(db[1].GetUInt32()));
    uint[]? receivedVelocity=null;
    if(receivedReady){float speed=BitConverter.UInt32BitsToSingle(bits!.Value);receivedVelocity=[BitConverter.SingleToUInt32Bits(direction.X*speed),0,BitConverter.SingleToUInt32Bits(direction.Y*speed)];}
    var slot=c.GetProperty("AuthoredSlot").GetString()=="speed[0]"?AuthoredMovementSlot.Speed0:AuthoredMovementSlot.Speed1;
    Need(authored.TrySelect(new[]{slot},policy,out var selection),"explicit authored selection");
    Need(authored.TryPlanarVelocity(slot,direction,policy,out var velocity),"explicit authored planar adapter");
    var chosen=selection!.Inputs[slot];var v=velocity!.Value;
    Need(receiver.IsCurrent(receipt)&&world.RetainedCreatureView4PartialBytes==before,"pure reads/selection preserve source and journal");
    rows.Add(new{Name=c.GetProperty("Name").GetString(),Phase=receipt.Phase.ToString(),ReceivedFields=receipt.Fields.Select(f=>new{f.Index,Bits=f.RawBits,f.Provenance}).ToArray(),ReceivedBits=bits,ReceivedReady=receivedReady,ReceivedVelocityBits=receivedVelocity,AuthoredBits=chosen.Bits,AuthoredSourcePath=chosen.Field!.SourcePath,AuthoredSelected=true,AuthoredVelocityBits=new uint[]{BitConverter.SingleToUInt32Bits(v.X),0,BitConverter.SingleToUInt32Bits(v.Y)},AuthoredPhaseEligible=receipt.IsOperationalPhase});
    world.Close();Need(!receiver.IsCurrent(receipt)&&world.RetainedCreatureView4PartialBytes==0,"close invalidates received context");
}
File.WriteAllText(args[3],JsonSerializer.Serialize(rows));Console.WriteLine($"Explicit authored/received join cases={rows.Count}; no source arbitration/defaults/motion-driver claim");
