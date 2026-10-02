using System.Text.Json;
using System.Security.Cryptography;
using Stardust.Core.Assets;
using Stardust.Core.Session;
if(args.Length!=5)throw new ArgumentException("asset-root configuration catalog animation-bank fixture-json");
var config=File.ReadAllBytes(args[1]);if(Convert.ToHexString(SHA256.HashData(config))!=RetainedSkeletalDetailBundleReader.ConfigurationPin)throw new Exception("Configuration pin.");
var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(args[2]));using var bankStream=File.OpenRead(args[3]);var bank=SkeletalAnimationBankProfile.Load(bankStream);
var fixture=TwoCreatureWorldPreview.ReadFixture(args[4]);var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
ulong a=TwoCreatureWorldPreview.A,b=TwoCreatureWorldPreview.B;int ownerThread=Environment.CurrentManagedThreadId,calls=0,closed=0;var jobs=new List<Task<SceneCreatureResources>>();var borrowed=new List<byte[]>();
Task<SceneCreatureResources> Load(SceneCreatureResourceRequest request,CancellationToken token)
{
 calls++;var task=Task.Run(()=>
 {
  var reads=new HashSet<string>();try
  {
   if(Environment.CurrentManagedThreadId==ownerThread)throw new Exception("Loader on owner.");
   using var text=new StringReader(System.Text.Encoding.UTF8.GetString(config));using var overlay=ConfiguredTreOverlay.Open(args[0],text);
   TreLookupResult Read(string path){token.ThrowIfCancellationRequested();if(!reads.Add(path))throw new Exception("Repeated read.");var row=overlay.Lookup(path);if(row.Payload is not null)lock(borrowed)borrowed.Add(row.Payload);return row;}
   var source=SceneCreatureResourceReader.Load(request.Snapshot.Create,request.Catalog,request.Idle,request.Movement,Read,token,loadAlbedoFactors:true);
   if(reads.Count!=39 || source.SourceBytes!=4686371)throw new Exception("Original closure differs.");return source;
  }
  finally{Interlocked.Increment(ref closed);}
 },token);jobs.Add(task);return task;
}
SceneWorldCoordinator Make(Func<SceneCreatureResourceRequest,CancellationToken,Task<SceneCreatureResources>> load,int pending=2)=>new(catalog,idle,bank,load,SceneCreateZeroQuaternionPolicy.Reject,maximumObjects:2,maximumOutstandingJobs:2,maximumStagedMessages:8,maximumPayloadBytes:1024,replicationPolicy:ReplicatedWorldEvidence.Policy(true),changePolicy:new(pending));
using var world=Make(Load);long gen=world.BeginSelection(a).Generation;TwoCreatureWorldPreview.Stage(world,gen,fixture);if(world.Snapshots().Count!=0 || world.OutstandingJobs!=0 || world.StagedCount!=6)throw new Exception("Staging differs.");world.CompleteMinimumSceneBarrier(gen,true);world.PollResources();Task.WhenAll(jobs).GetAwaiter().GetResult();var pubs=world.PollResources().Prepared.ToDictionary(p=>p.Ticket.Handle.ObjectId);if(pubs.Count!=2)throw new Exception("Two publications absent.");TwoCreatureWorldPreview.Accept(world,gen,fixture,"ready");
void Apply(string key)=>TwoCreatureWorldPreview.Accept(world,gen,fixture,key);
void Consume(string stage){var batch=world.DrainChanges(gen)!;Console.WriteLine("two_batch="+JsonSerializer.Serialize(TwoCreatureWorldPreview.Batch(stage,batch)));if(!world.IsCurrent(batch) || !world.AcknowledgeChanges(batch) || world.DrainChanges(gen)!.Entries.Count!=0)throw new Exception("Ack differs.");}
void State(string stage){Console.WriteLine("two_native_state="+JsonSerializer.Serialize(new{stage,calls,closed,actors=new[]{a,b}.Select(id=>world.FindReplication(gen,id)!).Select(s=>new{id=s.Incarnation.ObjectId,incarnation=s.Incarnation.CreationRevision,revision=s.Revision,heightBits=BitConverter.SingleToUInt32Bits(s.Scalars.Height),visible=s.Baseline.Visibility!=0,wounds=s.Wounds!.Values.ToArray()}).ToArray()}));}
Consume("initial");State("initial");foreach(var p in pubs.Values){Console.WriteLine("two_source_begin="+p.Ticket.Handle.ObjectId);ReplicatedWorldEvidence.Write(p.Resources,Console.WriteLine);}
var oldA=world.FindReplication(gen,a)!;var oldB=world.FindReplication(gen,b)!;
Apply("a_height");var held=world.DrainChanges(gen)!;Apply("b_height");if(world.AcknowledgeChanges(held) || world.DrainChanges(gen)!.Entries.Count!=2)throw new Exception("Stale ack lost IDs.");Consume("both_height");
if(world.IsCurrent(oldA) || world.IsCurrent(oldB) || !world.IsCurrent(pubs[a]) || !world.IsCurrent(pubs[b]) || calls!=2)throw new Exception("State/resource lifetime coupled.");
Apply("b_hidden");Consume("b_hidden");Apply("b_shown");Consume("b_shown");Apply("a_invalid_height");var invalid=world.FindReplication(gen,a)!;if(BitConverter.SingleToUInt32Bits(invalid.Scalars.Height)!=0x80000000 || !world.IsCurrent(invalid) || world.DrainChanges(gen)!.Entries.Count!=1)throw new Exception("Raw Height/pending lost.");Apply("a_height");Consume("height_retry");State("updated");
int negatives=0;
using(var foreign=Make((_,_)=>Task.FromResult(pubs[a].Resources)))
{
 long fg=foreign.BeginSelection(a).Generation;foreach(string key in new[]{"start","a_create","a_baseline","a_end"})TwoCreatureWorldPreview.Accept(foreign,fg,fixture,key);foreign.CompleteMinimumSceneBarrier(fg,true);foreign.PollResources();var fp=foreign.PollResources().Prepared.Single();
 if(world.IsCurrent(fp) || world.IsCurrent(foreign.FindReplication(fg,a)!) || world.AcknowledgeChanges(foreign.DrainChanges(fg)!))throw new Exception("Foreign accepted.");negatives+=3;
}
bool wrongThread=Task.Run(()=>{try{world.DrainChanges(gen);return false;}catch(InvalidOperationException){return true;}}).GetAwaiter().GetResult();if(!wrongThread)throw new Exception("Wrong thread accepted.");negatives++;
var before=world.FindReplication(gen,a)!;Apply("a_destroy");Consume("destroy");if(world.IsCurrent(pubs[a]) || world.IsCurrent(before) || !world.IsCurrent(pubs[b]) || !world.IsCurrent(world.FindReplication(gen,b)!))throw new Exception("Destroy lifetime differs.");negatives+=2;
foreach(string key in new[]{"a_create","a_baseline","a_end","a_height"})Apply(key);world.PollResources();jobs[^1].GetAwaiter().GetResult();var replacement=world.PollResources().Prepared.Single();Consume("recreate");State("recreated");
if(replacement.Ticket.Handle.ObjectId!=a || replacement.Resources.Create.ObjectId!=a || world.IsCurrent(pubs[a]) || world.IsCurrent(before) || !world.IsCurrent(pubs[b]) || calls!=3 || closed!=3)throw new Exception("Recreate affected B.");
// Every truncated mixed payload is rejected before any accepted prefix can survive.
byte[] mixed=Convert.FromHexString(fixture["a_height"]);
for(int size=0;size<mixed.Length;size++)
{
 using var broken=Make((_,_)=>Task.FromException<SceneCreatureResources>(new Exception("Must not load malformed staged data.")));
 long bg=broken.BeginSelection(a).Generation;TwoCreatureWorldPreview.Accept(broken,bg,fixture,"start");TwoCreatureWorldPreview.Accept(broken,bg,fixture,"a_create");TwoCreatureWorldPreview.Accept(broken,bg,fixture,"a_baseline");
 bool rejected=false;try{broken.Accept(bg,mixed.AsSpan(0,size));}catch(InvalidDataException){rejected=true;}
 if(!rejected || broken.State!=ZoneSceneTransitionState.Failed || broken.Snapshots().Count!=0 || broken.DrainChanges(bg)is not null || broken.OutstandingJobs!=0)throw new Exception("Truncation kept partial state.");negatives++;
}
using(var bounded=Make((_,_)=>Task.FromException<SceneCreatureResources>(new Exception("No load")),1))
{
 long bg=bounded.BeginSelection(a).Generation;TwoCreatureWorldPreview.Stage(bounded,bg,fixture);bool rejected=false;try{bounded.CompleteMinimumSceneBarrier(bg,true);}catch(InvalidDataException){rejected=true;}if(!rejected || bounded.State!=ZoneSceneTransitionState.Failed || bounded.Snapshots().Count!=0 || bounded.OutstandingJobs!=0)throw new Exception("Pending capacity retained replay prefix.");negatives++;
}
var source=pubs[a].Resources;
string OwnedHash()=>Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new{source.Creature.MaterialBundle!.AlbedoFactors!.Materials,Mips=source.Creature.MaterialBundle.Textures.OrderBy(p=>p.Key).Select(p=>new{p.Key,Hashes=p.Value.Mips.Select(m=>Convert.ToHexString(SHA256.HashData(m.Rgba.Span))).ToArray()}).ToArray()})));
string owned=OwnedHash();foreach(var bytes in borrowed)Array.Clear(bytes);if(OwnedHash()!=owned)throw new Exception("Borrowed source mutation changed owned factors/mips.");
bool lateRejected=false;try{Apply("a_bad");}catch(InvalidDataException){lateRejected=true;}if(!lateRejected || world.State!=ZoneSceneTransitionState.Failed || world.DrainChanges(gen)is not null || world.Snapshots().Count!=0 || world.IsCurrent(pubs[b]))throw new Exception("Late failure kept either actor.");negatives++;
world.Close();world.DrainCompletion.GetAwaiter().GetResult();if(world.OutstandingJobs!=0 || world.IsCurrent(replacement) || world.IsCurrent(pubs[b]))throw new Exception("Close leaked current resource.");negatives++;
Console.WriteLine($"PASS: two native negatives={negatives} truncations={mixed.Length} stale_ack_two_pending=true foreign_owner=true wrong_thread=true one_recreate=true unchanged_b_current=true closures=3 reads=39_per_load archives_closed=true borrowed_owned=true drain_complete=true");
