using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
using System.Text.Json;
using System.Security.Cryptography;
if(args.Length!=4)throw new ArgumentException("usage: Rendering peer-json original-root configuration profile");
byte[] config=File.ReadAllBytes(args[2]);
if(Convert.ToHexString(SHA256.HashData(config))!=RetainedSkeletalDetailBundleReader.ConfigurationPin)throw new Exception("Configuration pin differs.");
using var profile=File.OpenRead(args[3]);var bank=SkeletalAnimationBankProfile.Load(profile);
var peer=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(args[0]))!;
byte[] encoded=Convert.FromHexString(peer["create"]);var create=SceneObjectLifecycleCodec.DecodeCreate(encoded);Array.Clear(encoded);
var catalog=new ObjectTemplateCatalog(new[]{new ObjectTemplateCatalogEntry(create.TemplateCrc,RetainedSkeletalDetailBundleReader.ObjectPath,RetainedSkeletalDetailBundleReader.ObjectPin,RetainedSkeletalDetailBundleReader.AppearancePin)});
var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
var reads=new Dictionary<string,int>();var raw=new List<byte[]>();int ownerThread=System.Environment.CurrentManagedThreadId,workerThread=ownerThread,closed=0;
Task<SceneCreatureResources>? worker=null;
using var world=new SceneWorldCoordinator(catalog,idle,bank,(request,token)=>worker=Task.Run(()=>
{
 workerThread=System.Environment.CurrentManagedThreadId;
 try
 {
  using var text=new StringReader(System.Text.Encoding.UTF8.GetString(config));using var overlay=ConfiguredTreOverlay.Open(args[1],text);
  TreLookupResult Read(string p){token.ThrowIfCancellationRequested();reads[p]=reads.GetValueOrDefault(p)+1;var r=overlay.Lookup(p);if(r.Payload is not null)raw.Add(r.Payload);return r;}
  return SceneCreatureResourceReader.Load(request.Snapshot.Create,request.Catalog,request.Idle,request.Movement,Read,token);
 }
 finally{Interlocked.Increment(ref closed);}
},token),SceneCreateZeroQuaternionPolicy.Reject,maximumObjects:1,maximumOutstandingJobs:2,maximumStagedMessages:8,maximumEnvelopes:2,maximumEnvelopeBytes:56);
var borrowedPeer=new Dictionary<string,string>(peer);using var driver=new OfflineWorldPeerDriver(world,borrowedPeer);borrowedPeer.Clear();driver.SelectAndStage();if(world.OutstandingJobs!=0)throw new Exception("Pre-barrier job.");
driver.OpenCallerBarrier();Console.WriteLine("phase=Finalized AwaitingServerReady prepared=false");world.PollResources();worker!.GetAwaiter().GetResult();
var publication=world.PollResources().Prepared.Single();
if(!SceneCreatureVisual.MatchesWorld(world,publication)||closed!=1||ownerThread==workerThread)throw new Exception("Original world owner transfer differs.");
var source=publication.Resources;Console.WriteLine("phase=Prepared AwaitingServerReady finalized=true");driver.ServerReady();Console.WriteLine("phase=Ready finalized=true prepared=true");
Console.WriteLine("wire="+JsonSerializer.Serialize(driver.Export()));
for(int i=0;i<80;i++)driver.ServerReady();
using(var retainedTrace=JsonDocument.Parse(JsonSerializer.Serialize(driver.Export())))if(retainedTrace.RootElement.GetProperty("Controls").GetArrayLength()>64||retainedTrace.RootElement.GetProperty("DeliveredApplications").GetArrayLength()!=7)throw new Exception("Diagnostic trace grew unbounded or replayed duplicates.");
if(reads.Count!=37||reads.Values.Any(n=>n!=1)||source.SourceBytes!=4683848)throw new Exception("Original closure/read-once differs.");
var placement=publication.Placement;
var adapter=typeof(SceneCreatureVisual).Assembly.GetType("GodotAppearanceTransform")!.GetMethod("ToGodot",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!;
var engine=(Transform3D)adapter.Invoke(null,new object[]{placement.Transform})!;
Console.WriteLine("placement="+JsonSerializer.Serialize(AsyncSceneCreaturePreview.PlacementEvidence(placement,engine)));
if(engine.Origin!=new Vector3(123.5f,create.PositionY,45.25f)||BitConverter.SingleToInt32Bits(placement.RawCreate.QuaternionX)!=int.MinValue||!placement.AppliedNormalization||placement.UsedZeroIdentity)throw new Exception("Placement/raw bits differ.");
string Fingerprint()
{
 using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
 foreach(var g in source.Creature.Geometries!.Values)foreach(var p in g.Skin.Positions)hash.AppendData(JsonSerializer.SerializeToUtf8Bytes(p,new JsonSerializerOptions{IncludeFields=true}));
 foreach(var t in source.Creature.MaterialBundle!.Textures.Values)foreach(var m in t.Mips)hash.AppendData(m.Rgba.Span);
 var binding=SkeletalJointBindingBuilder.Create(source.Creature.Appearance,source.Creature.Skeletons!,source.Creature.Appearance.Skeletons.ToDictionary(r=>r.Path,_=>0));
 foreach(var clip in source.Creature.AdditionalAnimations!.Values.Append(source.Creature.PreparedAnimation!))hash.AppendData(JsonSerializer.SerializeToUtf8Bytes(clip.CreateLocalPose(binding,clip.EndFrame*.5),new JsonSerializerOptions{IncludeFields=true}));
 return Convert.ToHexString(hash.GetHashAndReset());
}
var before=Fingerprint();foreach(var bytes in raw)Array.Clear(bytes);if(Fingerprint()!=before)throw new Exception("Borrowed bytes changed owned visual inputs.");
int negatives=0;
foreach(string action in new[]{"restart","selection","destroy","contain","close"})
{
 using var owner=new SceneWorldCoordinator(catalog,idle,bank,(_,_)=>Task.FromResult(source),SceneCreateZeroQuaternionPolicy.Reject);
 using var d=new OfflineWorldPeerDriver(owner,peer);d.SelectAndStage();d.OpenCallerBarrier();owner.PollResources();var p=owner.PollResources().Prepared.Single();
 if(!SceneCreatureVisual.MatchesWorld(owner,p)||SceneCreatureVisual.MatchesWorld(world,p)||SceneCreatureVisual.MatchesWorld(owner,publication))throw new Exception("World publication ownership differs.");
 negatives+=2;
 if(action=="restart")owner.RestartResource(owner.Generation,d.ObjectId);if(action=="selection")d.ResetSelection();
 if(action=="destroy"){d.ServerReady();d.Destroy();}if(action=="contain"){d.ServerReady();d.ContainAgain();}if(action=="close")owner.Close();
 if(SceneCreatureVisual.MatchesWorld(owner,p))throw new Exception("Stale world publication accepted.");negatives++;
 owner.Close();owner.DrainCompletion.GetAwaiter().GetResult();
}
foreach(var row in source.Creature.Sources.Values)Console.WriteLine("source="+JsonSerializer.Serialize(row));
Console.WriteLine("create="+Convert.ToHexString(SceneObjectLifecycleCodec.EncodeCreate(create)));
Console.WriteLine($"PASS: original world transfer and owned buffers; world publication negatives={negatives}; no native Godot allocation or GPU claim");
world.Close();world.DrainCompletion.GetAwaiter().GetResult();if(world.OutstandingJobs!=0||SceneCreatureVisual.Allocations!=0)throw new Exception("Native drain/allocation differs.");
