using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

try {
if(args.Length!=6)throw new ArgumentException("asset-root configuration catalog animation-bank peer-json output-json");
var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(args[2]));
using var bankFile=File.OpenRead(args[3]);var bank=SkeletalAnimationBankProfile.Load(bankFile);
var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
var peer=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(args[4]))!;
Task<SceneCreatureResources>? worker=null;int calls=0,closed=0,owner=Environment.CurrentManagedThreadId,workerThread=owner;
var policy=new SceneWorldReplicationPolicy(SceneWorldReplicationMode.CreatureView3Scalars,new(65536,65536,64,64,262144),64,262144);
using var world=new SceneWorldCoordinator(catalog,idle,bank,(request,token)=>{
 calls++;return worker=Task.Run(()=>{
 workerThread=Environment.CurrentManagedThreadId;
 try {
 using var text=new StringReader(File.ReadAllText(args[1]));
 using var overlay=ConfiguredTreOverlay.Open(args[0],text);
 return SceneCreatureResourceReader.Load(request.Snapshot.Create,request.Catalog,request.Idle,request.Movement,overlay.Lookup,token,loadAlbedoFactors:true);
 } finally {Interlocked.Increment(ref closed);}
 },token);
},SceneCreateZeroQuaternionPolicy.Reject,replicationPolicy:policy);
var transport=new SoeClientTransport();var select=world.BeginSelection(0xF122334455667788);
transport.BeginSession(0x12345678,496);transport.AcceptSessionResponse(Convert.FromHexString(peer["response"]));
var delivered=new List<string>();
void Receive(string name) {
 var incoming=transport.AcceptDatagram(Convert.FromHexString(peer["peer_"+name]));
 foreach(var application in incoming.ApplicationPayloads) {delivered.Add(Convert.ToHexString(application));world.Accept(select.Generation,application);Array.Clear(application);}
}
foreach(var name in new[]{"start","create","baseline","delta","end","contain"})Receive(name);
Check(world.StagedCount==5&&world.Snapshots().Count==0&&calls==0,"Ordered staging before caller barrier");
world.CompleteMinimumSceneBarrier(select.Generation,true);
var state=world.FindReplication(select.Generation,0xF122334455667788)!;
Check(world.IsCurrent(state)&&world.Snapshots().Single().Envelopes.Count==0,"Typed current state no history");
Check(world.PollResources().Prepared.Count==0&&calls==1,"One finalized resource job");
worker!.GetAwaiter().GetResult();
var publication=world.PollResources().Prepared.Single();
Check(world.IsCurrent(publication)&&closed==1&&workerThread!=owner&&publication.Resources.ExternalReads==39&&publication.Resources.SourceBytes==4686371&&publication.Resources.Creature.MaterialBundle!.AlbedoFactors is not null,"Original optional albedo ownership/transfer");
Receive("serverReady");
Check(world.State==ZoneSceneTransitionState.Ready&&world.IsCurrent(publication),"Ready remains separate");
world.Accept(select.Generation,Convert.FromHexString(peer["delta"]));
var next=world.FindReplication(select.Generation,state.Incarnation.ObjectId)!;
Check(!world.IsCurrent(state)&&world.IsCurrent(next)&&world.IsCurrent(publication)&&calls==1,"State supersedes without resource reload");
var originalBaseline=CreatureView3BaselineCodec.Encode(next.Baseline,policy.BaselineLimits);
var originalState=new {next.Scalars.Posture,next.Scalars.Rank,next.Scalars.LinkId,HeightBits=BitConverter.SingleToUInt32Bits(next.Scalars.Height),next.Scalars.Shock,next.Scalars.States};
var envelope=ReplicationEnvelopeCodec.DecodeDelta(Convert.FromHexString(peer["delta"]),65536);
var bad=new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,envelope.ObjectId,envelope.RawTag,envelope.View,envelope.DeclaredCount,envelope.CopyOpaqueFields().Concat(new byte[]{255,255}).ToArray(),65536);
bool rejected=false;try{world.Accept(select.Generation,ReplicationEnvelopeCodec.EncodeDelta(bad,65536));}catch(InvalidDataException){rejected=true;}
Check(rejected&&world.State==ZoneSceneTransitionState.Failed&&world.Snapshots().Count==0&&!world.IsCurrent(next)&&!world.IsCurrent(publication)&&world.RetainedReplicationDecodedBytes==0,"Malformed generation clears all current publications");
world.Close();world.DrainCompletion.GetAwaiter().GetResult();transport.Close();
File.WriteAllText(args[5],JsonSerializer.Serialize(new {Delivered=delivered,OriginalBaselineHex=Convert.ToHexString(originalBaseline),Scalars=originalState,publication.Resources.ExternalReads,publication.Resources.SourceBytes,SourceObject=publication.Resources.Recipe.ObjectSha256,StateSupersedesResourceRetained=true,MalformedGenerationRetired=true,WorkerOffOwner=true,ArchiveClosed=true,Drained=true}));
Console.WriteLine("PASS: offline typed SOE/world state -> original optional albedo resources; state-only update retains resources, malformed generation retires both");
} catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}
static void Check(bool v,string s){if(!v)throw new InvalidDataException(s);}
