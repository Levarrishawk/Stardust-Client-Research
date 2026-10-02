using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
try {
if(args.Length!=7)throw new ArgumentException("asset-root config catalog bank palette-profile peer export");
byte[] configuration=File.ReadAllBytes(args[1]);
Check(Convert.ToHexString(SHA256.HashData(configuration))==RetainedSkeletalDetailBundleReader.ConfigurationPin,"Configuration pin");
var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(args[2]));
using var bankFile=File.OpenRead(args[3]);var bank=SkeletalAnimationBankProfile.Load(bankFile);
using var colorFile=File.OpenRead(args[4]);var colors=SceneCreaturePaletteCustomization.LoadProfile(colorFile);
var peer=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(args[5]))!;
var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
var policy=new SceneWorldReplicationPolicy(SceneWorldReplicationMode.CreatureView3Mixed,new(65536,65536,64,64,262144),64,262144,new(65536,64,64,64,64,16),CreatureWoundsApplicationPolicy.LegacyBounded);
Task<SceneCreatureResources>? worker=null;int calls=0,closed=0,owner=Environment.CurrentManagedThreadId,workerThread=owner;bool sharing=false;
var reads=new Dictionary<string,int>();
using var world=new SceneWorldCoordinator(catalog,idle,bank,(request,token)=>{
 calls++;return worker=Task.Run(()=>{
 workerThread=Environment.CurrentManagedThreadId;
 try {
 using var text=new StringReader(Encoding.UTF8.GetString(configuration));using var overlay=ConfiguredTreOverlay.Open(args[0],text);
 TreLookupResult Read(string path){reads[path]=reads.GetValueOrDefault(path)+1;return overlay.Lookup(path);}
 var original=SceneCreatureResourceReader.Load(request.Snapshot.Create,request.Catalog,request.Idle,request.Movement,Read,token,loadAlbedoFactors:true);
 var selected=SceneCreaturePaletteCustomization.Apply(original,colors,token);
 sharing=ReferenceEquals(original.Creature.Geometries,selected.Creature.Geometries)&&ReferenceEquals(original.Creature.MaterialBundle!.Textures,selected.Creature.MaterialBundle!.Textures)&&ReferenceEquals(original.Creature.Sources,selected.Creature.Sources);
 return selected;
 }finally{Interlocked.Increment(ref closed);}
 },token);
},SceneCreateZeroQuaternionPolicy.Reject,replicationPolicy:policy);
var transport=new SoeClientTransport();var select=world.BeginSelection(0xF122334455667788);
transport.BeginSession(0x12345678,496);transport.AcceptSessionResponse(Convert.FromHexString(peer["response"]));var delivered=new List<string>();
void Receive(string name){foreach(var application in transport.AcceptDatagram(Convert.FromHexString(peer["peer_"+name])).ApplicationPayloads){delivered.Add(Convert.ToHexString(application));world.Accept(select.Generation,application);Array.Clear(application);}}
foreach(var name in new[]{"start","create","baseline","delta","end","contain"})Receive(name);
Check(world.DrainChanges(select.Generation) is null&&world.Snapshots().Count==0&&calls==0,"No staged state/change/resource publication");
world.CompleteMinimumSceneBarrier(select.Generation,true);
var states=new List<object>();
object Record(string name,SceneWorldChangeBatch batch){var state=world.FindReplication(select.Generation,0xF122334455667788)!;var wound=state.Wounds!;Check(batch.Entries.Count==1&&batch.Entries[0].Current!.Replication!.Revision==state.Revision&&world.IsCurrent(batch),"One current complete observer state");return new{Name=name,state.Revision,batch.MutationRevision,state.Scalars.Posture,state.Scalars.Rank,state.Scalars.LinkId,HeightBits=BitConverter.SingleToUInt32Bits(state.Scalars.Height),state.Scalars.Shock,state.Scalars.States,WoundsCounter=wound.Counter,Wounds=wound.Values,RetainedDecodedBytes=world.RetainedReplicationDecodedBytes};}
var initial=world.DrainChanges(select.Generation)!;states.Add(Record("mixed",initial));
var first=world.FindReplication(select.Generation,0xF122334455667788)!;
Check(world.PollResources().Prepared.Count==0&&calls==1,"One worker resource start");worker!.GetAwaiter().GetResult();
var publication=world.PollResources().Prepared.Single();Check(world.IsCurrent(publication)&&closed==1&&workerThread!=owner&&sharing&&reads.Count==39&&reads.Values.All(n=>n==1),"Owned original palette closure transfer");
Receive("serverReady");Check(world.IsCurrent(initial),"Resource/ready changes preserve observer batch");
world.Accept(select.Generation,Convert.FromHexString(peer["equal"]));
Check(!world.IsCurrent(first)&&world.IsCurrent(publication)&&!world.AcknowledgeChanges(initial),"State/resource freshness and stale acknowledgment retry");
var second=world.DrainChanges(select.Generation)!;states.Add(Record("equal",second));Check(world.AcknowledgeChanges(second)&&world.DrainChanges(select.Generation)!.Entries.Count==0,"Current acknowledgment frees slot");
world.Accept(select.Generation,Convert.FromHexString(peer["growth"]));var third=world.DrainChanges(select.Generation)!;states.Add(Record("growth",third));
Check(world.IsCurrent(publication)&&calls==1,"Growth state preserves original resource");
var last=world.FindReplication(select.Generation,0xF122334455667788)!;string originalBaseline=Convert.ToHexString(CreatureView3BaselineCodec.Encode(last.Baseline,policy.BaselineLimits));
bool rejected=false;try{world.Accept(select.Generation,Convert.FromHexString(peer["lateGrowth"]));}catch(InvalidDataException){rejected=true;}
Check(rejected&&world.State==ZoneSceneTransitionState.Failed&&world.Snapshots().Count==0&&world.DrainChanges(select.Generation) is null&&!world.IsCurrent(last)&&!world.IsCurrent(publication)&&!world.AcknowledgeChanges(third)&&world.RetainedReplicationDecodedBytes==0,"Late growth retires whole generation without scalar prefix");
world.Close();world.DrainCompletion.GetAwaiter().GetResult();transport.Close();Check(world.OutstandingJobs==0,"Drained");
File.WriteAllText(args[6],JsonSerializer.Serialize(new{Delivered=delivered,OriginalBaselineHex=originalBaseline,States=states,Colors=publication.Resources.Creature.CustomizationValues!.Select(p=>new{p.Key.VariableName,p.Key.Private,Value=p.Value}),publication.Resources.ExternalReads,publication.Resources.SourceBytes,SourceObject=publication.Resources.Recipe.ObjectSha256,WorkerOffOwner=true,ArchiveClosed=true,NoExtraReads=true,SharedGeometryAndTextures=true,StateSupersedesResourceRetained=true,StaleAckRetried=true,LateFailureRetired=true,Drained=true}));
Console.WriteLine("PASS: framed mixed world, original palette closure, current acknowledged changes and atomic late-failure cleanup");
}catch(Exception error){Console.Error.WriteLine(error);Environment.ExitCode=1;}
static void Check(bool condition,string message){if(!condition)throw new InvalidDataException(message);}
