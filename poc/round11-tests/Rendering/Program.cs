using System.Text.Json;
using System.Security.Cryptography;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
if(args.Length!=5)throw new ArgumentException("asset-root configuration catalog animation-bank peer-json");
var config=File.ReadAllBytes(args[1]);if(Convert.ToHexString(SHA256.HashData(config))!=RetainedSkeletalDetailBundleReader.ConfigurationPin)throw new Exception("Configuration pin differs.");
var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(args[2]));using var profile=File.OpenRead(args[3]);var bank=SkeletalAnimationBankProfile.Load(profile);
var peer=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(args[4]))!;
var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
int ownerThread=Environment.CurrentManagedThreadId,workerThread=ownerThread,closed=0,calls=0;Task<SceneCreatureResources>? worker=null;
var reads=new Dictionary<string,int>();var borrowed=new List<byte[]>();
using var world=new SceneWorldCoordinator(catalog,idle,bank,(request,token)=>
{
 calls++;return worker=Task.Run(()=>
 {
  workerThread=Environment.CurrentManagedThreadId;
  try
  {
   using var text=new StringReader(System.Text.Encoding.UTF8.GetString(config));using var overlay=ConfiguredTreOverlay.Open(args[0],text);
   TreLookupResult Read(string path){reads[path]=reads.GetValueOrDefault(path)+1;var value=overlay.Lookup(path);if(value.Payload is not null)borrowed.Add(value.Payload);return value;}
   return SceneCreatureResourceReader.Load(request.Snapshot.Create,request.Catalog,request.Idle,request.Movement,Read,token,loadAlbedoFactors:true);
  }
  finally{Interlocked.Increment(ref closed);}
 },token);
},SceneCreateZeroQuaternionPolicy.Reject,replicationPolicy:ReplicatedWorldEvidence.Policy());
using var driver=new ReplicatedWorldPeer(world,peer);peer.Clear();driver.SelectAndStage();driver.OpenBarrier();Console.WriteLine("replicated_phase=Finalized state=AwaitingServerReady prepared=false");
if(world.PollResources().Prepared.Count!=0 || calls!=1)throw new Exception("Resource start differs.");worker!.GetAwaiter().GetResult();var publication=world.PollResources().Prepared.Single();
var state=world.FindReplication(driver.Generation,driver.ObjectId)!;
if(!SceneWorldCreatureStatePresentation.Matches(world,publication,state) || closed!=1 || workerThread==ownerThread || reads.Count!=39 || reads.Values.Any(n=>n!=1))throw new Exception("Original publication transfer differs.");
Console.WriteLine("replicated_phase=Prepared state=AwaitingServerReady finalized=true");driver.ServerReady();Console.WriteLine("replicated_phase=Ready finalized=true prepared=true");
var source=publication.Resources;ReplicatedWorldEvidence.Write(source,Console.WriteLine);
Console.WriteLine("state_create="+Convert.ToHexString(SceneObjectLifecycleCodec.EncodeCreate(source.Create)));
Console.WriteLine("state_base="+JsonSerializer.Serialize(CreatureStatePreview.PoseEvidence(ReplicatedWorldEvidence.BaseTransform(publication))));
Console.WriteLine("replicated_baseline="+Convert.ToHexString(CreatureView3BaselineCodec.Encode(state.Baseline,ReplicatedWorldEvidence.Policy().BaselineLimits)));
string Fingerprint()=>Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new{source.Creature.MaterialBundle!.AlbedoFactors!.Materials,Programs=source.Creature.MaterialBundle.AlbedoFactors.Programs.Select(p=>new{p.Key,hash=Convert.ToHexString(SHA256.HashData(p.Value.Metadata.Children[0].Children[1].Payload.Span))}),Mips=source.Creature.MaterialBundle.Textures.Select(p=>new{p.Key,hashes=p.Value.Mips.Select(m=>Convert.ToHexString(SHA256.HashData(m.Rgba.Span)))})})));
string retained=Fingerprint();foreach(var bytes in borrowed)Array.Clear(bytes);if(Fingerprint()!=retained)throw new Exception("Original borrowed bytes changed owned factors/pixels.");
driver.Receive("peer_height");var next=world.FindReplication(driver.Generation,driver.ObjectId)!;
if(SceneWorldCreatureStatePresentation.Matches(world,publication,state) || !SceneWorldCreatureStatePresentation.Matches(world,publication,next) || calls!=1 || next.Baseline.Scalars.Height!=1 || next.Scalars.Height!=1.5f)throw new Exception("State revision and resource attempt coupled.");
Console.WriteLine("state_height="+JsonSerializer.Serialize(CreatureStatePreview.PoseEvidence(SceneCreatureStatePresentation.DeriveTransform(ReplicatedWorldEvidence.BaseTransform(publication),next.Scalars.Height))));
driver.Receive("peer_hidden");var hidden=world.FindReplication(driver.Generation,driver.ObjectId)!;if(hidden.Baseline.Visibility!=0)throw new Exception("Hidden baseline differs.");driver.Receive("peer_shown");var shown=world.FindReplication(driver.Generation,driver.ObjectId)!;if(shown.Baseline.Visibility!=255 || !world.IsCurrent(publication) || calls!=1)throw new Exception("Visibility reloaded resources.");
Console.WriteLine("replicated_wire="+JsonSerializer.Serialize(driver.Export()));
int negatives=0;
foreach(string key in new[]{"zero","negativeZero","negative","small","large"})
{
 driver.Apply(key);var current=world.FindReplication(driver.Generation,driver.ObjectId)!;bool rejected=false;
 try{SceneCreatureStatePresentation.DeriveTransform(ReplicatedWorldEvidence.BaseTransform(publication),current.Scalars.Height);}catch(ArgumentOutOfRangeException){rejected=true;}
 if(!rejected || !world.IsCurrent(current) || !world.IsCurrent(publication) || calls!=1)throw new Exception("Raw unsupported state was lost/reloaded.");
 Console.WriteLine("height_bits="+JsonSerializer.Serialize(new{key,bits=BitConverter.SingleToUInt32Bits(current.Scalars.Height).ToString("X8")}));negatives++;
}
foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity}){bool rejected=false;try{SceneCreatureStatePresentation.DeriveTransform(ReplicatedWorldEvidence.BaseTransform(publication),invalid);}catch(ArgumentOutOfRangeException){rejected=true;}if(!rejected)throw new Exception("Nonfinite presentation accepted.");negatives++;}
var retainedState=world.FindReplication(driver.Generation,driver.ObjectId)!;
world.RestartResource(driver.Generation,driver.ObjectId);if(world.IsCurrent(publication) || !world.IsCurrent(retainedState) || SceneWorldCreatureStatePresentation.Matches(world,publication,retainedState))throw new Exception("Restart state/resource freshness differs.");negatives++;
long generation=driver.Generation;driver.Reset();if(world.Accept(generation,new byte[]{1}) || world.IsCurrent(next))throw new Exception("Stale generation accepted.");negatives++;
world.Close();world.DrainCompletion.GetAwaiter().GetResult();if(world.OutstandingJobs!=0 || world.IsCurrent(publication) || world.IsCurrent(next))throw new Exception("Closed world retained publication/state.");negatives++;
using(var shifted=new SceneWorldCoordinator(catalog,idle,bank,(_,_)=>Task.FromResult(source),SceneCreateZeroQuaternionPolicy.Reject,replicationPolicy:ReplicatedWorldEvidence.Policy()))
using(var shiftPeer=new ReplicatedWorldPeer(shifted,JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(args[4]))!))
{
 shiftPeer.SelectAndStage();shiftPeer.OpenBarrier();bool rejected=false;try{shiftPeer.Apply("bad");}catch(InvalidDataException){rejected=true;}if(!rejected)throw new Exception("Shift fixture failure absent.");
 shiftPeer.SelectAndStage();shiftPeer.OpenBarrier();shifted.PollResources();var resource=shifted.PollResources().Prepared.Single();var current=shifted.FindReplication(shiftPeer.Generation,shiftPeer.ObjectId)!;
 if((ulong)resource.Generation==resource.Ticket.Handle.SceneGeneration || !SceneWorldCreatureStatePresentation.Matches(shifted,resource,current))throw new Exception("World generation/registry epoch coupled.");negatives++;
}
Console.WriteLine($"PASS: native replicated negatives={negatives} borrowed_ownership=true worker_off_owner=true archives_closed=true resources=39 bytes={source.SourceBytes} state_resource_separate=true epoch_independent=true drain_complete=true GPU_allocations=0");
