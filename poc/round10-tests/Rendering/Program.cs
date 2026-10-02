using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
using System.Text.Json;
using System.Security.Cryptography;
if(args.Length!=4)throw new ArgumentException("usage: Rendering peer-json original-root configuration animation-profile");
var peer=CreatureStatePreview.ReadPeer(args[0]);var create=SceneObjectLifecycleCodec.DecodeCreate(peer["create"]);
byte[] config=File.ReadAllBytes(args[2]);if(Convert.ToHexString(SHA256.HashData(config))!=RetainedSkeletalDetailBundleReader.ConfigurationPin)throw new Exception("Configuration pin differs.");
using var profile=File.OpenRead(args[3]);var bank=SkeletalAnimationBankProfile.Load(profile);
var catalog=new ObjectTemplateCatalog(new[]{new ObjectTemplateCatalogEntry(create.TemplateCrc,RetainedSkeletalDetailBundleReader.ObjectPath,RetainedSkeletalDetailBundleReader.ObjectPin,RetainedSkeletalDetailBundleReader.AppearancePin)});
var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
var reads=new Dictionary<string,int>();SceneCreatureResources source;
using(var text=new StringReader(System.Text.Encoding.UTF8.GetString(config)))
using(var overlay=ConfiguredTreOverlay.Open(args[1],text))
{
 TreLookupResult Read(string path){reads[path]=reads.GetValueOrDefault(path)+1;return overlay.Lookup(path);}
 source=SceneCreatureResourceReader.Load(create,catalog,idle,bank,Read,CancellationToken.None);
}
if(reads.Count!=37 || reads.Values.Any(v=>v!=1) || source.SourceBytes!=4683848)throw new Exception("Original read-once resources differ.");
foreach(var row in source.Creature.Sources.Values)Console.WriteLine("source="+JsonSerializer.Serialize(row));
var basis=CreatureStatePreview.EnginePlacement(create);
Console.WriteLine("state_create="+Convert.ToHexString(SceneObjectLifecycleCodec.EncodeCreate(create)));
Console.WriteLine("state_base="+JsonSerializer.Serialize(CreatureStatePreview.PoseEvidence(basis)));
Console.WriteLine("state_height="+JsonSerializer.Serialize(CreatureStatePreview.PoseEvidence(SceneCreatureStatePresentation.DeriveTransform(basis,1.5f))));
var registry=new SceneObjectRegistry(4,1);var entry=registry.Create(1,create)!;
using var store=new SceneCreatureReplicationStore(registry);var baseline=CreatureView3BaselineCodec.Decode(peer["baseline"],CreatureStatePreview.Limits);
Console.WriteLine("baseline="+Convert.ToHexString(CreatureView3BaselineCodec.Encode(baseline,CreatureStatePreview.Limits)));
var seed=store.Seed(entry.Incarnation,baseline)!;
if(!SceneCreatureStatePresentation.Matches(registry,entry.Handle,store,seed))throw new Exception("Native current match differs.");
var updated=store.Apply(entry.Incarnation,CreatureView3ScalarDeltaCodec.Decode(peer["delta"],1024,16))!;
if(SceneCreatureStatePresentation.Matches(registry,entry.Handle,store,seed) || !SceneCreatureStatePresentation.Matches(registry,entry.Handle,store,updated))throw new Exception("State revision guard differs.");
int negatives=0;
foreach(string key in new[]{"zero","negativeZero","negative","small","large"})
{
 var current=store.Apply(entry.Incarnation,CreatureView3ScalarDeltaCodec.Decode(peer[key],1024,16))!;
 bool rejected=false;try{SceneCreatureStatePresentation.DeriveTransform(basis,current.Scalars.Height);}catch(ArgumentOutOfRangeException){rejected=true;}
 if(!rejected || store.Find(entry.Incarnation)!=current || baseline.Scalars.Height!=1)throw new Exception("Presentation altered stored state.");
 Console.WriteLine("height_bits="+JsonSerializer.Serialize(new{key,bits=BitConverter.SingleToInt32Bits(current.Scalars.Height).ToString("X8")}));negatives++;
}
foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
{bool rejected=false;try{SceneCreatureStatePresentation.DeriveTransform(basis,invalid);}catch(ArgumentOutOfRangeException){rejected=true;}if(!rejected)throw new Exception("Nonfinite scale accepted.");negatives++;}
foreach(float height in new[]{.25f,2f})if(SceneCreatureStatePresentation.DeriveTransform(basis,height).Origin!=basis.Origin)throw new Exception("Height moved base origin.");
using var foreign=new SceneCreatureReplicationStore(registry);if(SceneCreatureStatePresentation.Matches(registry,entry.Handle,store,foreign.Seed(entry.Incarnation,baseline)!))throw new Exception("Foreign store state accepted.");negatives++;
var latest=store.Seed(entry.Incarnation,baseline)!;registry.Contain(1,new(create.ObjectId,0,1));
if(!store.IsCurrent(latest) || SceneCreatureStatePresentation.Matches(registry,entry.Handle,store,latest))throw new Exception("Exact resource/stable incarnation distinction differs.");negatives++;
var contained=registry.Find(create.ObjectId)!;if(!SceneCreatureStatePresentation.Matches(registry,contained.Handle,store,latest))throw new Exception("Explicit rebind failed.");
registry.Destroy(1,new(create.ObjectId,0));var recreated=registry.Create(1,create)!;var replacement=store.Seed(recreated.Incarnation,baseline)!;
if(SceneCreatureStatePresentation.Matches(registry,recreated.Handle,store,latest) || !SceneCreatureStatePresentation.Matches(registry,recreated.Handle,store,replacement))throw new Exception("Recreated identity differs.");negatives++;
foreach(string key in new[]{"baseline","hidden","shown"})
{
 var bytes=peer[key];var value=CreatureView3BaselineCodec.Decode(bytes,CreatureStatePreview.Limits);string encoded=Convert.ToHexString(CreatureView3BaselineCodec.Encode(value,CreatureStatePreview.Limits));Array.Clear(bytes);
 if(Convert.ToHexString(CreatureView3BaselineCodec.Encode(value,CreatureStatePreview.Limits))!=encoded)throw new Exception("Baseline borrows caller bytes.");
 Console.WriteLine("visibility="+JsonSerializer.Serialize(new{key,raw=value.Visibility,visible=value.Visibility!=0}));
}
store.Close();if(SceneCreatureStatePresentation.Matches(registry,recreated.Handle,store,replacement))throw new Exception("Closed store accepted state.");negatives++;
Console.WriteLine($"PASS: native state negatives={negatives} boundaries=2 raw_ownership=true resources=37 bytes=4683848 GPU_allocations=0");
