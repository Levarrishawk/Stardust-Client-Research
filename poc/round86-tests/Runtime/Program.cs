using System.Buffers.Binary;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Configuration;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

if(args.Length!=1)throw new ArgumentException("Explicit independent private fixture JSON required.");
using var doc=JsonDocument.Parse(File.ReadAllBytes(args[0]));var root=doc.RootElement;
byte[] Hex(string name)=>Convert.FromHexString(root.GetProperty(name).GetString()!);
const ulong Oid=0x1122334455667788;int checks=0,negatives=0;
void Need(bool value){checks++;if(!value)throw new Exception("Check "+checks);}
void Reject(Action action){try{action();throw new Exception("Expected rejection");}catch(Exception e)when(e is InvalidOperationException or InvalidDataException or ArgumentException){negatives++;}}
ReplacementClientSession Session()
{
    var s=new ReplacementClientSession(new LoginClientOptions("login.invalid",44452,"20260817-SD:01"));s.StartLogin(0x12345678,0);s.AcceptIncoming(ReplacementConnection.Login,Hex("LoginResponse"),0);s.Authenticate("synthetic","synthetic",1);
    foreach(var wire in root.GetProperty("Login").EnumerateArray())s.AcceptIncoming(ReplacementConnection.Login,Convert.FromHexString(wire.GetString()!),2);
    s.BeginZone(Oid,0x87654321,3);s.AcceptIncoming(ReplacementConnection.Zone,Hex("ZoneResponse"),4);s.AcceptIncoming(ReplacementConnection.Zone,Hex("Permissions"),5);return s;
}
SceneWorldCoordinator World()
{
    const string pin="AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    var catalog=new ObjectTemplateCatalog([new(root.GetProperty("TemplateCrc").GetUInt32(),"object/synthetic/fixture.iff",pin,pin)]);
    var idle=new LogicalAnimationClipSelection("appearance/lat/authored.lat","idle",pin);
    SceneWorldCreatureView4FullPolicy four=new(new(MaximumPayloadBytes:1024),new(MaximumPayloadBytes:1024),new(),new());
    SceneWorldCreatureView6FullPolicy six=new(new(MaximumPayloadBytes:1024),new(MaximumPayloadBytes:1024),new(),new());
    return new(catalog,idle,new Dictionary<string,LogicalAnimationClipSelection>{{"walk",idle},{"run",idle}},(q,t)=>throw new InvalidOperationException("No visual loading"),SceneCreateZeroQuaternionPolicy.Reject,
        replicationPolicy:new(SceneWorldReplicationMode.CreatureView3MixedComponents,new(1024,64,64,16,512),16,1024,new(1024,32,24,64,64,16),CreatureWoundsApplicationPolicy.LegacyBounded,new(1024,32,64,64,16),new(16,16)),
        creatureView4FullPolicy:four,creatureView6FullPolicy:six);
}
ReplacementClientExchange Apps(params byte[][] values)=>new([],values);
try
{
var movementWires=new List<string>();float[] correctionPosition=[];uint? correctionCounter=null;int applied=0,counterRejected=0,unhandled=0;
using(var session=Session())using(var world=World())using(var store=new SceneWorldObjectMotionStore(world))
using(var motion=new ReplacementPlayerMotionSession(session,world,store,new(1000,20),SceneCreateZeroQuaternionPolicy.Reject))
using(var ingress=new ReplacementSelectedPlayerIngress())
using(var bridge=new ReplacementSceneBridge(session,world,(scene,t)=>Task.CompletedTask,ingress,motion))
{
    bridge.BindSelection();Need(motion.LastInboundStatus is null);
    Need(motion.TryHandleIncoming(bridge.Generation,Hex("CorrectionHex"))&&motion.LastInboundStatus==SceneWorldFullTransformApplicationStatus.InactiveWorld&&motion.AppliedCorrections==0);
    Reject(()=>motion.Bind(bridge.Generation,Oid));Reject(()=>motion.TryHandleIncoming(bridge.Generation+1,Hex("CorrectionHex")));
    Reject(()=>motion.TryQueueLocal(null!,null!,0,0,0,6));
    bridge.ObserveExchange(session.AcceptIncoming(ReplacementConnection.Zone,Hex("PostSelection"),6));
    Need(motion.TryHandleIncoming(bridge.Generation,Hex("CorrectionHex"))&&motion.LastInboundStatus==SceneWorldFullTransformApplicationStatus.InactiveWorld);
    var initial=bridge.ObserveExchange(Apps(Hex("CreateHex"),Hex("BaselineHex"),Hex("Baseline4Hex"),Hex("Baseline6Hex"),Hex("EndHex")));
    Need(initial.FailureType is null);bridge.DrainCompletion.Wait(1000);bridge.Poll(7);Need(bridge.ReadySubmissions==1);
    Reject(()=>motion.TryQueueLocal(null!,null!,0,0,0,7));
    Need(bridge.ObserveExchange(Apps(Convert.FromHexString(root.GetProperty("ExpectedReady").GetString()!))).FailureType is null&&world.State==ZoneSceneTransitionState.Ready);
    Reject(()=>motion.TryQueueLocal(null!,null!,0,0,0,8));
    session.AcceptIncoming(ReplacementConnection.Zone,Hex("ReadyAck"),8);Need(session.ZonePendingMessages==0);
    var snapshot=world.Snapshots().Single();
    var transform=new AppearanceTransform(System.Numerics.Vector3.UnitX,System.Numerics.Vector3.UnitY,System.Numerics.Vector3.UnitZ,new(7,8,9));
    Need(store.PublishLocal(snapshot,transform,3));var local=store.Find(snapshot)!;
    Reject(()=>motion.TryQueueLocal(snapshot,local with { },0,0,0,9));
    using(var foreign=new SceneWorldObjectMotionStore(world))
    {Need(foreign.PublishLocal(snapshot,transform,3));Reject(()=>motion.TryQueueLocal(snapshot,foreign.Find(snapshot)!,0,0,0,9));}
    var queued=motion.TryQueueLocal(snapshot,local,0x89abcdef,77,0x7fc00001,9)!;
    movementWires.AddRange(queued.Datagrams.Select(v=>Convert.ToHexString(v.Bytes)));Need(queued.FailureType is null&&queued.Datagrams.Count==1&&store.IsCurrent(local));
    byte[] correction=Hex("CorrectionHex");Need(motion.TryHandleIncoming(bridge.Generation,correction));Array.Fill(correction,(byte)0);
    var corrected=store.Find(snapshot)!;Need(corrected.Transform.Translation==new System.Numerics.Vector3(4,5,6)&&corrected.MovementCounter==10&&corrected.Authoritative);
    Need(motion.TryHandleIncoming(bridge.Generation,Hex("CorrectionHex"))&&motion.LastInboundStatus==SceneWorldFullTransformApplicationStatus.CounterRejected);
    Need(motion.TryHandleIncoming(bridge.Generation,Hex("OlderHex"))&&ReferenceEquals(store.Find(snapshot),corrected));
    Need(motion.TryHandleIncoming(bridge.Generation,Hex("NewerHex"))&&motion.LastInboundStatus==SceneWorldFullTransformApplicationStatus.Applied);
    var current=store.Find(snapshot)!;correctionPosition=[current.Transform.Translation.X,current.Transform.Translation.Y,current.Transform.Translation.Z];correctionCounter=current.MovementCounter;
    Reject(()=>motion.TryQueueLocal(snapshot,current,0,0,0,10));Reject(()=>motion.TryQueueLocal(snapshot,local,0,0,0,10));
    var unsupported=new[]{Hex("UnrelatedHex"),Hex("ParentHex"),Hex("UnknownHex")};var remaining=bridge.ObserveExchange(Apps(unsupported));
    Need(remaining.FailureType is null&&unsupported.All(v=>v.All(b=>b==0)));unhandled=remaining.ZoneApplications.Count;Need(unhandled==3);
    foreach(string name in new[]{"UnrelatedHex","ParentHex","UnknownHex"})Need(remaining.ZoneApplications.Any(v=>v.SequenceEqual(Hex(name))));
    foreach(string name in new[]{"CorrectionHex","UnrelatedHex","ParentHex"})Reject(()=>motion.TryHandleIncoming(bridge.Generation,Hex(name)[..^1]));
    byte[] bad=Hex("CorrectionHex");bad[0]=4;Reject(()=>motion.TryHandleIncoming(bridge.Generation,bad));
    bad=Hex("CorrectionHex");BinaryPrimitives.WriteUInt32LittleEndian(bad.AsSpan(46),0x7fc00001);Reject(()=>motion.TryHandleIncoming(bridge.Generation,bad));
    Reject(()=>motion.TryHandleIncoming(bridge.Generation,new byte[5]));
    Need(!motion.TryHandleIncoming(bridge.Generation,Hex("SentinelHex")));
    Exception? threadError=null;var thread=new Thread(()=>{try{motion.TryHandleIncoming(bridge.Generation,Hex("NewerHex"));}catch(Exception e){threadError=e;}});thread.Start();thread.Join();Need(threadError is InvalidOperationException);negatives++;
    applied=motion.AppliedCorrections;counterRejected=motion.CounterRejectedCorrections;Need(applied==2&&counterRejected==2);
    byte[] pressure=new byte[100000];Hex("SentinelHex").CopyTo(pressure,0);session.QueueZoneApplication(pressure,10);
    Need(session.ZonePendingMessages==1&&motion.TryQueueLocal(null!,null!,0,0,0,10) is null); // No pose access or build under pressure.
    bridge.Dispose();Need(store.IsCurrent(current)&&world.State==ZoneSceneTransitionState.Ready);
    motion.Dispose();motion.Dispose();Need(store.IsCurrent(current)&&session.Phase==ReplacementClientPhase.Selected);
    Reject(()=>motion.TryHandleIncoming(bridge.Generation,Hex("CorrectionHex")));
}
using(var session=Session())using(var world=World())using(var store=new SceneWorldObjectMotionStore(world))
using(var motion=new ReplacementPlayerMotionSession(session,world,store,new(1000,20),SceneCreateZeroQuaternionPolicy.Reject))
using(var bridge=new ReplacementSceneBridge(session,world,(scene,t)=>Task.CompletedTask,playerMotion:motion))
{
    Reject(()=>motion.TryHandleIncoming(1,Hex("CorrectionHex")));Reject(()=>motion.Bind(0,Oid));Reject(()=>motion.Bind(world.Generation,Oid+1));
    bridge.BindSelection();var bad=Hex("CorrectionHex")[..^1];var output=bridge.ObserveExchange(Apps(bad));
    Need(output.FailureType==nameof(InvalidDataException)&&bad.All(b=>b==0)&&session.Phase==ReplacementClientPhase.Closed&&world.State==ZoneSceneTransitionState.Closed);negatives++;
    Reject(()=>motion.TryHandleIncoming(bridge.Generation,Hex("CorrectionHex")));
}
using(var session=Session())using(var world=World())using(var store=new SceneWorldObjectMotionStore(world))
using(var motion=new ReplacementPlayerMotionSession(session,world,store,new(1000,20),SceneCreateZeroQuaternionPolicy.Reject))
{
    long generation=world.BeginSelection(Oid).Generation;motion.Bind(generation,Oid);
    world.BeginSelection(Oid);Reject(()=>motion.TryHandleIncoming(generation,Hex("CorrectionHex")));
    Reject(()=>motion.TryQueueLocal(null!,null!,0,0,0,6));
}
Console.WriteLine(JsonSerializer.Serialize(new{Passed=true,Checks=checks,NegativeCases=negatives,MovementWires=movementWires,CorrectionPosition=correctionPosition,CorrectionCounter=correctionCounter,Applied=applied,CounterRejected=counterRejected,Unhandled=unhandled,Synthetic=true,LiveInterop=false}));

}
catch(Exception error){Console.Error.WriteLine(error);Environment.ExitCode=1;}
