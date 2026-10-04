using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
if(args.Length!=4)throw new ArgumentException("asset-root configuration fixture output");
void Need(bool yes,string why){if(!yes)throw new InvalidDataException(why);}
Need(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[1])))=="22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524","configuration identity");
const string rootPin="E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243";
using var config=File.OpenText(args[1]);using var overlay=ConfiguredTreOverlay.Open(args[0],config);
var profile=SharedCreatureMovementInputProfile.Load(overlay.Lookup,"object/creature/player/shared_human_male.iff",rootPin);
Need(profile.Sources.Count==5&&profile.SourceBytes==8290,"one necessary original authored chain");
var authored=SharedCreatureAuthoredMovementInputs.FromProfile(profile);
var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes("poc/preview-profiles/scene-object-catalog.json"));
using var bankFile=File.OpenRead("poc/preview-profiles/human-locomotion.json");var bank=SkeletalAnimationBankProfile.Load(bankFile);
var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{SceneObjectResourceRecipe.CreatureDetail0,new SceneCreatureResourceOptions(idle,bank,true)},{SceneObjectResourceRecipe.StaticAppearance,new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,null)}};
var policy=new SceneWorldReplicationPolicy(SceneWorldReplicationMode.CreatureView3MixedComponents,new(1024,64,64,16,512),16,1024,new(1024,32,24,64,64,16),CreatureWoundsApplicationPolicy.LegacyBounded,new(1024,32,64,64,16),new(16,16));
using var fixture=JsonDocument.Parse(File.ReadAllBytes(args[2]));var data=fixture.RootElement;
Need(data.GetProperty("Cases").GetArrayLength()==144&&data.GetProperty("OriginalEffectiveValues").GetBoolean()==false,"bounded explicit join");
byte[] App(string key)=>Convert.FromHexString(data.GetProperty("Applications").GetProperty(key).GetString()!);
var create=SceneObjectLifecycleCodec.DecodeCreate(App("Create"));
Need(catalog.Require(create.TemplateCrc).ObjectSha256==rootPin&&LegacyAssetPath.Checksum(authored.RootPath)==create.TemplateCrc,"selected catalog root binding");
var rows=new List<object>();
foreach(var c in data.GetProperty("Cases").EnumerateArray())
{
    using var world=new SceneWorldCoordinator(catalog,options,(_,_)=>throw new InvalidDataException("No resource loads requested by native motion join"),SceneCreateZeroQuaternionPolicy.Reject,4,2,32,8192,16,4096,1024,policy,creatureView4PartialPolicy:new(new(MessageLimits:new(MaximumPayloadBytes:1024)),new(MaximumObjects:1)));
    long generation=world.BeginSelection(create.ObjectId).Generation;
    foreach(string key in new[]{"Start","Create","Baseline","End"})Need(world.Accept(generation,App(key)),"typed scene stage "+key);
    if(c.GetProperty("ReceivedWire").ValueKind!=JsonValueKind.Null)Need(world.Accept(generation,Convert.FromHexString(c.GetProperty("ReceivedWire").GetString()!)),"received scalar ingress");
    Need(world.CompleteMinimumSceneBarrier(generation,true) is not null,"active finalized scene");
    using var motions=new SceneWorldObjectMotionStore(world);
    using var receiver=new SceneWorldReceivedCreatureMovementInputs(world);
    var source=Enum.Parse<SceneWorldMovementSpeedSource>(c.GetProperty("Source").GetString()!);
    using var admission=new SceneWorldExplicitMovementAdmission(world,motions,create.ObjectId,source,authored,rootPin,receiver,0,10000,8);
    var db=c.GetProperty("DirectionBits");var direction=new Vector2(BitConverter.UInt32BitsToSingle(db[0].GetUInt32()),BitConverter.UInt32BitsToSingle(db[1].GetUInt32()));
    bool received=source is SceneWorldMovementSpeedSource.ReceivedField7 or SceneWorldMovementSpeedSource.ReceivedField11;
    var captured=admission.Submit(0,direction,received?receiver.Read(generation,create.ObjectId):null);
    if(captured is not null)Need(admission.IsSourceCurrent(captured),"new captured input currency");
    Need(world.Accept(generation,Convert.FromHexString(data.GetProperty("ChangedSpeedWire").GetString()!)),"later received speed update");
    if(received&&captured is not null)Need(!admission.IsSourceCurrent(captured),"old source receipt becomes stale while queued velocity stays captured");
    int partition=c.GetProperty("Partition").GetInt32();for(long t=partition;t<100000;t+=partition)admission.Advance(t);
    var state=admission.Advance(100000);var position=state.Transform.Translation;
    string? authoredSourcePath=null;
    if(captured?.AuthoredSelection is {} selection){var slot=source==SceneWorldMovementSpeedSource.AuthoredSpeed0?AuthoredMovementSlot.Speed0:AuthoredMovementSlot.Speed1;authoredSourcePath=selection.Inputs[slot].Field!.SourcePath;}
    uint[]? velocity=captured is null?null:new[]{BitConverter.SingleToUInt32Bits(captured.WorldVelocity.X),BitConverter.SingleToUInt32Bits(captured.WorldVelocity.Y),BitConverter.SingleToUInt32Bits(captured.WorldVelocity.Z)};
    rows.Add(new{Name=c.GetProperty("Name").GetString(),Accepted=captured is not null,CapturedBits=captured?.RawSpeedBits,VelocityBits=velocity,PositionBits=new[]{BitConverter.SingleToUInt32Bits(position.X),BitConverter.SingleToUInt32Bits(position.Y),BitConverter.SingleToUInt32Bits(position.Z)},Timestamp=state.TimestampMicroseconds,Dropped=state.DroppedMicroseconds,Fault=state.FaultType,SourceKind=source.ToString(),AuthoredSourcePath=authoredSourcePath,CapturedPolicy=true});
    world.Close();if(captured is not null)Need(!admission.IsSourceCurrent(captured),"close invalidates capture identity");Need(world.RetainedCreatureView4PartialBytes==0,"partial state cleared");
}
File.WriteAllText(args[3],JsonSerializer.Serialize(rows));Console.WriteLine($"Explicit captured movement composition cases={rows.Count}; original arbitration unclaimed");
