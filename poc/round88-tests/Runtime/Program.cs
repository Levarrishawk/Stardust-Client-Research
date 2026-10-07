using System.Text.Json;
using System.Numerics;
using Stardust.Core.Assets;
using Stardust.Core.Configuration;
using Stardust.Core.Hosting;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

try
{
if(args.Length!=1)throw new ArgumentException("Explicit authored private fixture JSON required.");
using var doc=JsonDocument.Parse(File.ReadAllBytes(args[0]));var f=doc.RootElement;
byte[] Hex(string key)=>Convert.FromHexString(f.GetProperty(key).GetString()!);
byte[] Item(string key,int index)=>Convert.FromHexString(f.GetProperty(key)[index].GetString()!);
const ulong Oid=0x1122334455667788;int checks=0,negatives=0;
void Need(bool value){checks++;if(!value)throw new Exception("Check "+checks);}
void Reject(Action action)
{try{action();}catch(Exception e)when(e is InvalidOperationException or InvalidDataException or ArgumentException){negatives++;return;}throw new Exception("Expected rejection");}
void Drain(ReplacementClientRun run,bool fault=false)
{try{run.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();Need(!fault);}catch(Exception e)when(e is AggregateException or InvalidOperationException){Need(fault);negatives++;}}
var options=new LoginClientOptions("login.invalid",44452,"20260817-SD:01");
var lp=new DatagramPeer(0x7f000001,44452);var zp=new DatagramPeer(0x7f000002,44453);
SceneWorldCoordinator World()
{
    const string pin="AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    var catalog=new ObjectTemplateCatalog([new(f.GetProperty("TemplateCrc").GetUInt32(),f.GetProperty("ObjectPath").GetString()!,pin,pin)]);
    var idle=new LogicalAnimationClipSelection("appearance/lat/authored.lat","idle",pin);
    return new(catalog,idle,new Dictionary<string,LogicalAnimationClipSelection>{{"walk",idle},{"run",idle}},(q,t)=>throw new InvalidOperationException("No visual loading"),SceneCreateZeroQuaternionPolicy.Reject,
        replicationPolicy:new(SceneWorldReplicationMode.CreatureView3MixedComponents,new(1024,64,64,16,512),16,1024,new(1024,32,24,64,64,16),CreatureWoundsApplicationPolicy.LegacyBounded,new(1024,32,64,64,16),new(16,16)),
        creatureView4FullPolicy:new(new(MaximumPayloadBytes:1024),new(MaximumPayloadBytes:1024),new(),new()),
        creatureView6FullPolicy:new(new(MaximumPayloadBytes:1024),new(MaximumPayloadBytes:1024),new(),new()));
}
ReplacementClientRun Run(ReplacementClientSession s,ReplacementDatagramPump p,SceneWorldCoordinator w,SceneWorldObjectMotionStore m,Func<CmdStartSceneMessage,CancellationToken,Task> minimum)
    =>new(s,p,w,m,minimum,new(1000,10),SceneCreateZeroQuaternionPolicy.Reject);
ReplacementClientRunTick Step(ReplacementClientRun run,long now,bool fail=false)
{var tick=run.Tick(now);Need(fail?tick.FailureType is not null:tick.FailureType is null);return tick;}
ReplacementClientRunTick Feed(ReplacementClientRun run,FakePort port,DatagramPeer peer,byte[] bytes,long now,bool fail=false)
{port.Deliver(peer,bytes);var tick=Step(run,now,fail);Need(bytes.All(b=>b==0));return tick;}
void Admit(ReplacementClientRun run,ReplacementClientSession s,FakePort login,FakePort zone)
{
    Need(Step(run,0).UnhandledApplications.Count==0&&s.Phase==ReplacementClientPhase.Offline);
    run.StartLogin(0x12345678,0);Step(run,0);Feed(run,login,lp,Hex("LoginResponse"),1);
    run.Authenticate("synthetic","opaque-test-password",2);Step(run,2);
    foreach(var reply in f.GetProperty("LoginReplies").EnumerateArray())Feed(run,login,lp,Convert.FromHexString(reply.GetString()!),3);
    Need(s.Phase==ReplacementClientPhase.CharacterList);run.SelectCharacter(Oid,0x87654321,zone,zp,4);Drain(run);Step(run,4);
    Feed(run,zone,zp,Hex("ZoneResponse"),5);Step(run,5);
    Feed(run,zone,zp,Item("ZoneAcks",0),6);Feed(run,zone,zp,Hex("Permissions"),7);Step(run,7);Feed(run,zone,zp,Item("ZoneAcks",1),8);Step(run,8);
    Need(s.Phase==ReplacementClientPhase.Selected&&run.SelectedObjectId==Oid&&run.Generation==1&&login.Disposals==1);
}
List<byte[]> Stage(ReplacementClientRun run,FakePort zone)
{
    var result=new List<byte[]>();foreach(var wire in f.GetProperty("WorldFrames").EnumerateArray())result.AddRange(Feed(run,zone,zp,Convert.FromHexString(wire.GetString()!),9).UnhandledApplications);
    return result;
}
void Ready(ReplacementClientRun run,FakePort zone,TaskCompletionSource minimum)
{
    Need(run.ReadySubmissions==0);Task.Run(()=>minimum.TrySetResult()).GetAwaiter().GetResult();Step(run,10);Step(run,10);
    Need(run.ReadySubmissions==1);Feed(run,zone,zp,Hex("ServerReady"),11);Feed(run,zone,zp,Item("ZoneAcks",2),12);
}
var wires=new List<string>();var unknown=new List<string>();float[] correctionPosition=[];uint? correctionCounter=null;int applied=0,rejected=0,providerCalls=0,readyCount=0;
bool completed=false,borrowedPreserved=false,providerCanceled=false,faultDrain=false,suffixDropped=false;
// Full application loop with real protected frames, async minimum barrier and exact caller-owned remainder.
using(var s=new ReplacementClientSession(options))using(var world=World())using(var store=new SceneWorldObjectMotionStore(world))
{
    var login=new FakePort();var zone=new FakePort();using var pump=new ReplacementDatagramPump(s,login,lp);
    var minimum=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);using var run=Run(s,pump,world,store,(scene,t)=>{providerCalls++;return minimum.Task;});
    Admit(run,s,login,zone);var unhandled=Stage(run,zone);Need(world.StagedCount==5&&world.Snapshots().Count==0&&providerCalls==1);Ready(run,zone,minimum);
    Need(world.State==ZoneSceneTransitionState.Ready&&world.Snapshots().Count==1);
    using(var unadmitted=new FakePort()){Reject(()=>run.SelectCharacter(Oid,42,unadmitted,zp,12));Need(unadmitted.Disposals==0&&run.FailureType is null);}
    for(int i=0;i<3;i++)
    {
        var snapshot=world.Snapshots().Single();var xyz=f.GetProperty("ExpectedLocalMotions")[i].GetProperty("Position");
        var transform=new AppearanceTransform(Vector3.UnitX,Vector3.UnitY,Vector3.UnitZ,new(xyz[0].GetSingle(),xyz[1].GetSingle(),xyz[2].GetSingle()));
        Need(store.PublishLocal(snapshot,transform,2));var row=store.Find(snapshot)!;
        Need(run.TryQueueLocal(snapshot,row,0x23,(uint)(10+i),(uint)(100*(i+1)),13+i));Step(run,13+i);Feed(run,zone,zp,Item("MovementAcks",i),13+i);
    }
    for(int i=0;i<7;i++)unhandled.AddRange(Feed(run,zone,zp,Item("CorrectionFrames",i),16+i).UnhandledApplications);
    var current=store.Find(world.Snapshots().Single())!;correctionPosition=[current.Transform.Translation.X,current.Transform.Translation.Y,current.Transform.Translation.Z];correctionCounter=current.MovementCounter;
    applied=run.AppliedCorrections;rejected=run.CounterRejectedCorrections;readyCount=run.ReadySubmissions;Need(applied==2&&rejected==2&&run.LastInboundStatus==SceneWorldFullTransformApplicationStatus.Applied);
    unknown.AddRange(unhandled.Select(Convert.ToHexString));Need(unhandled.Count==15);
    run.BeginDisconnect(30);Step(run,30);Step(run,31);Drain(run);completed=run.DisconnectSendCompleted;
    Need(completed&&run.DisconnectRequested&&run.DisconnectSubmitted&&run.FailureType is null&&zone.Disposals==1);
    wires.AddRange(zone.Sent.Where(v=>v.Hex.StartsWith("0009",StringComparison.Ordinal)||v.Hex.StartsWith("0005",StringComparison.Ordinal)).Select(v=>v.Hex));
    run.Dispose();run.Dispose();Need(world.State==ZoneSceneTransitionState.Ready&&store.IsCurrent(current)&&unknown.SequenceEqual(unhandled.Select(Convert.ToHexString)));
    Need(run.Generation==1&&run.SelectedObjectId==Oid&&run.ReadySubmissions==1&&run.AppliedCorrections==2&&run.DisconnectSendCompleted);
    Need(!run.ToString().Contains("synthetic",StringComparison.Ordinal)&&!run.ToString().Contains("opaque-test-password",StringComparison.Ordinal));Reject(()=>run.Tick(32));
}
// Normal Dispose cancels only owned minimum work; borrowed session/pump/world/store remain alive.
using(var s=new ReplacementClientSession(options))using(var world=World())using(var store=new SceneWorldObjectMotionStore(world))
{
    var login=new FakePort();var zone=new FakePort();using var pump=new ReplacementDatagramPump(s,login,lp);
    var minimum=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);CancellationToken token=default;
    using var run=Run(s,pump,world,store,(scene,t)=>{token=t;t.Register(()=>minimum.TrySetCanceled(t));return minimum.Task;});Admit(run,s,login,zone);
    Feed(run,zone,zp,Item("WorldFrames",0),9);run.Dispose();Drain(run);providerCanceled=token.IsCancellationRequested;
    borrowedPreserved=s.Phase==ReplacementClientPhase.Selected&&world.State==ZoneSceneTransitionState.Initializing&&zone.Disposals==0;
    Need(providerCanceled&&borrowedPreserved&&run.ReadySubmissions==0&&run.FailureType is null);
    Need(pump.Tick(10).All(e=>e.FailureType is null));pump.Dispose();world.Close();s.Dispose();Drain(run);
}
// Disconnect during initialization cancels minimum work and suppresses all later bridge polling.
using(var s=new ReplacementClientSession(options))using(var world=World())using(var store=new SceneWorldObjectMotionStore(world))
{
    var login=new FakePort();var zone=new FakePort();using var pump=new ReplacementDatagramPump(s,login,lp);var minimum=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    using var run=Run(s,pump,world,store,(scene,t)=>{t.Register(()=>minimum.TrySetCanceled(t));return minimum.Task;});Admit(run,s,login,zone);Feed(run,zone,zp,Item("WorldFrames",0),9);
    run.BeginDisconnect(10);Reject(()=>run.BeginDisconnect(10));Step(run,10);Step(run,11);Drain(run);
    Need(run.DisconnectSendCompleted&&run.ReadySubmissions==0&&minimum.Task.IsCanceled&&world.State==ZoneSceneTransitionState.Initializing&&run.FailureType is null);
}
foreach(string mode in new[]{"provider","suffix","stale","send","cleanup","pressure"})
using(var s=new ReplacementClientSession(options))using(var world=World())using(var store=new SceneWorldObjectMotionStore(world))
{
    var login=new FakePort();var zone=new FakePort();using var pump=new ReplacementDatagramPump(s,login,lp);var minimum=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    using var run=Run(s,pump,world,store,(scene,t)=>minimum.Task);Admit(run,s,login,zone);var transferred=Stage(run,zone);byte[] earlier=transferred[0];byte[] earlierCopy=(byte[])earlier.Clone();
    if(mode=="provider")
    {
        minimum.TrySetException(new IOException("synthetic provider"));var tick=Step(run,10,true);Need(tick.FailureType==nameof(IOException)&&run.ReadySubmissions==0&&world.State==ZoneSceneTransitionState.Closed&&s.Phase==ReplacementClientPhase.Closed);Drain(run);Need(earlier.SequenceEqual(earlierCopy));continue;
    }
    Ready(run,zone,minimum);
    if(mode=="suffix")
    {
        var tick=Feed(run,zone,zp,Hex("UnknownThenMalformed"),13,true);suffixDropped=tick.UnhandledApplications.Count==0&&tick.FailureType==nameof(InvalidDataException);
        Need(suffixDropped&&world.State==ZoneSceneTransitionState.Closed&&zone.Disposals==1);Drain(run);Need(earlier.SequenceEqual(earlierCopy));
    }
    else if(mode=="stale")
    {world.BeginSelection(Oid);Need(Step(run,13,true).FailureType==nameof(InvalidOperationException)&&world.State==ZoneSceneTransitionState.Closed);Drain(run);}
    else if(mode=="pressure")
    {
        byte[] application=new byte[100000];Convert.FromHexString(f.GetProperty("Applications").GetProperty("NonworldSentinel").GetString()!).CopyTo(application,0);
        pump.Submit(s.QueueZoneApplication(application,13));Need(s.ZonePendingMessages==1&&!run.TryQueueLocal(null!,null!,0,0,0,13)&&run.FailureType is null&&s.ZonePendingMessages==1);
        run.Dispose();Need(s.Phase==ReplacementClientPhase.Selected&&world.State==ZoneSceneTransitionState.Ready);pump.Dispose();world.Close();s.Dispose();Drain(run);
    }
    else
    {
        zone.FailSend=mode=="send";zone.FailDispose=mode=="cleanup";run.BeginDisconnect(13);Step(run,13);var tick=Step(run,14,true);
        Need(mode=="send"?tick.FailureType==nameof(IOException)&&!run.DisconnectSendCompleted:tick.FailureType==nameof(AggregateException)&&run.DisconnectSendCompleted);
        Drain(run,true);faultDrain|=run.DrainCompletion.IsFaulted;Need(world.State==ZoneSceneTransitionState.Closed&&run.FailureType is not null);
    }
}
// Identity/admission rejection acquires no adapters or borrowed cleanup ownership.
using(var s=new ReplacementClientSession(options))using(var other=new ReplacementClientSession(options))using(var world=World())using(var otherWorld=World())
using(var store=new SceneWorldObjectMotionStore(world))using(var foreignStore=new SceneWorldObjectMotionStore(otherWorld))
{
    var login=new FakePort();var otherLogin=new FakePort();using var pump=new ReplacementDatagramPump(s,login,lp);using var otherPump=new ReplacementDatagramPump(other,otherLogin,lp);
    Reject(()=>Run(s,otherPump,world,store,(scene,t)=>Task.CompletedTask));Reject(()=>Run(s,pump,world,foreignStore,(scene,t)=>Task.CompletedTask));
    Reject(()=>new ReplacementClientRun(s,pump,world,store,(scene,t)=>Task.CompletedTask,new(0,10),SceneCreateZeroQuaternionPolicy.Reject));
    Need(login.Disposals==0&&otherLogin.Disposals==0&&s.Phase==ReplacementClientPhase.Offline&&world.State==ZoneSceneTransitionState.Idle);
    Exception? wrong=null;var thread=new Thread(()=>{try{Run(s,pump,world,store,(scene,t)=>Task.CompletedTask);}catch(Exception e){wrong=e;}});thread.Start();thread.Join();Need(wrong is InvalidOperationException);negatives++;
    using var run=Run(s,pump,world,store,(scene,t)=>Task.CompletedTask);Reject(()=>run.TryQueueLocal(null!,null!,0,0,0,0));Reject(()=>run.Tick(-1));
    wrong=null;thread=new Thread(()=>{try{_ = run.Generation;}catch(Exception e){wrong=e;}});thread.Start();thread.Join();Need(wrong is InvalidOperationException);negatives++;
    run.StartLogin(0x12345678,0);Step(run,0);Reject(()=>run.Tick(-1));Need(run.FailureType is null&&s.Phase==ReplacementClientPhase.LoginHandshake);
}
// A provider's caught reentry cannot advance the run or fault the healthy prefix.
foreach(string mode in new[]{"session","world","pump","store"})
using(var s=new ReplacementClientSession(options))using(var world=World())using(var store=new SceneWorldObjectMotionStore(world))
{
    var login=new FakePort();using var pump=new ReplacementDatagramPump(s,login,lp);
    if(mode=="session")s.StartLogin(1,0);if(mode=="world")world.BeginSelection(Oid);if(mode=="pump")pump.Dispose();if(mode=="store")store.Dispose();
    Reject(()=>Run(s,pump,world,store,(scene,t)=>Task.CompletedTask));Need(login.Disposals==(mode=="pump"?1:0)&&s.Phase==(mode=="session"?ReplacementClientPhase.LoginHandshake:ReplacementClientPhase.Offline));
}
// External selection through the borrowed pump has not bound this run; applications cannot enter its world.
using(var s=new ReplacementClientSession(options))using(var world=World())using(var store=new SceneWorldObjectMotionStore(world))
{
    var login=new FakePort();var zone=new FakePort();using var pump=new ReplacementDatagramPump(s,login,lp);using var run=Run(s,pump,world,store,(scene,t)=>Task.CompletedTask);
    run.StartLogin(0x12345678,0);Step(run,0);Feed(run,login,lp,Hex("LoginResponse"),1);run.Authenticate("synthetic","synthetic",2);Step(run,2);
    foreach(var reply in f.GetProperty("LoginReplies").EnumerateArray())Feed(run,login,lp,Convert.FromHexString(reply.GetString()!),3);
    pump.BeginZone(Oid,0x87654321,zone,zp,4);Drain(run);Step(run,4);Feed(run,zone,zp,Hex("ZoneResponse"),5);Step(run,5);Feed(run,zone,zp,Item("ZoneAcks",0),6);Feed(run,zone,zp,Hex("Permissions"),7);Step(run,7);
    Need(run.Generation==0&&run.SelectedObjectId==0);var tick=Feed(run,zone,zp,Item("WorldFrames",0),8,true);
    Need(tick.FailureType==nameof(InvalidDataException)&&tick.UnhandledApplications.Count==0&&world.State==ZoneSceneTransitionState.Closed&&zone.Disposals==1);Drain(run);
}
// Owned cancellation callback failure is retained as cleanup failure without closing borrowed owners.
using(var s=new ReplacementClientSession(options))using(var world=World())using(var store=new SceneWorldObjectMotionStore(world))
{
    var login=new FakePort();var zone=new FakePort();using var pump=new ReplacementDatagramPump(s,login,lp);using var run=Run(s,pump,world,store,(scene,t)=>Task.CompletedTask);
    run.StartLogin(0x12345678,0);Step(run,0);Feed(run,login,lp,Hex("LoginResponse"),1);run.Authenticate("synthetic","synthetic",2);Step(run,2);
    foreach(var reply in f.GetProperty("LoginReplies").EnumerateArray())Feed(run,login,lp,Convert.FromHexString(reply.GetString()!),3);
    Reject(()=>run.SelectCharacter(Oid,0x87654321,zone,new(zp.Address,44454),4));
    Need(run.FailureType==nameof(ArgumentException)&&zone.Disposals==0&&s.Phase==ReplacementClientPhase.Closed&&world.State==ZoneSceneTransitionState.Closed);Drain(run);
}
using(var s=new ReplacementClientSession(options))using(var world=World())using(var store=new SceneWorldObjectMotionStore(world))
{
    var login=new FakePort();var zone=new FakePort();using var pump=new ReplacementDatagramPump(s,login,lp);var minimum=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    using var run=Run(s,pump,world,store,(scene,t)=>{t.Register(()=>{minimum.TrySetCanceled(t);throw new IOException("synthetic cancellation callback");});return minimum.Task;});
    Admit(run,s,login,zone);Feed(run,zone,zp,Item("WorldFrames",0),9);
    try{run.Dispose();throw new Exception("Expected cleanup error");}catch(AggregateException){negatives++;}
    Need(run.FailureType==nameof(AggregateException)&&s.Phase==ReplacementClientPhase.Selected&&world.State==ZoneSceneTransitionState.Initializing&&zone.Disposals==0);Drain(run,true);
    pump.Dispose();world.Close();s.Dispose();run.Dispose();
}
using(var s=new ReplacementClientSession(options))using(var world=World())using(var store=new SceneWorldObjectMotionStore(world))
{
    var login=new FakePort();var zone=new FakePort();using var pump=new ReplacementDatagramPump(s,login,lp);ReplacementClientRun? current=null;
    var minimum=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);using var run=Run(s,pump,world,store,(scene,t)=>{Reject(()=>current!.Tick(9));return minimum.Task;});current=run;Admit(run,s,login,zone);Feed(run,zone,zp,Item("WorldFrames",0),9);
    Need(run.FailureType is null&&run.ReadySubmissions==0);minimum.TrySetResult();
}
Console.WriteLine(JsonSerializer.Serialize(new{Passed=true,Checks=checks,NegativeCases=negatives,Wires=wires,Unhandled=unknown,Applied=applied,CounterRejected=rejected,ReadySubmissions=readyCount,DisconnectSendCompleted=completed,CorrectionPosition=correctionPosition,CorrectionCounter=correctionCounter,MinimumProviderCalls=providerCalls,BorrowedOwnersPreserved=borrowedPreserved,ProviderCanceled=providerCanceled,FaultedDrainObserved=faultDrain,FailureSuffixDropped=suffixDropped,SocketCreated=false}));
}
catch(Exception error){Console.Error.WriteLine(error);Environment.ExitCode=1;}

sealed class FakePort:IDatagramPort,IDisposable
{
    public readonly List<(DatagramPeer Peer,string Hex)> Sent=[];public int Disposals;public bool FailSend,FailDispose;
    private TaskCompletionSource<InboundDatagram>? receive;
    public ValueTask SendAsync(DatagramPeer peer,ReadOnlyMemory<byte> bytes,CancellationToken token)
    {Sent.Add((peer,Convert.ToHexString(bytes.Span)));return FailSend?new(Task.FromException(new IOException("synthetic send"))):ValueTask.CompletedTask;}
    public ValueTask<InboundDatagram> ReceiveAsync(CancellationToken token)
    {receive=new(TaskCreationOptions.RunContinuationsAsynchronously);var captured=receive;token.Register(()=>captured.TrySetCanceled(token));return new(captured.Task);}
    public void Deliver(DatagramPeer peer,byte[] bytes){if(receive is null||!receive.TrySetResult(new(peer,bytes)))throw new Exception("No outstanding receive");}
    public ValueTask DisposeAsync(){Disposals++;return FailDispose?new(Task.FromException(new IOException("synthetic disposal"))):ValueTask.CompletedTask;}
    public void Dispose() { }
}
