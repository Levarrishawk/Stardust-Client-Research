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
var allWires=new List<List<string>>();
for(int mode=0;mode<10;mode++)
{
    using var s=new ReplacementClientSession(options);using var world=World();using var store=new SceneWorldObjectMotionStore(world);
    var login=new FakePort();var zone=new FakePort();using var pump=new ReplacementDatagramPump(s,login,lp);
    var minimum=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);using var run=Run(s,pump,world,store,(scene,t)=>minimum.Task);
    Admit(run,s,login,zone);
    var scenario=new ReplacementControlledMovementScenario(Oid,true,true,true);
    Reject(()=>new ReplacementControlledMovement(run,s,world,store,scenario with{FreshStampZero=false},Vector3.UnitZ));
    Reject(()=>new ReplacementControlledMovement(run,s,world,store,scenario with{MatchingValidatedPosition=false},Vector3.UnitZ));
    Reject(()=>new ReplacementControlledMovement(run,s,world,store,scenario with{MovementPrerequisitesValidated=false},Vector3.UnitZ));
    Reject(()=>new ReplacementControlledMovement(run,s,world,store,scenario with{SelectedObjectId=1},Vector3.UnitZ));
    Reject(()=>new ReplacementControlledMovement(run,s,world,store,scenario,Vector3.UnitY));
    using var movement=new ReplacementControlledMovement(run,s,world,store,scenario,Vector3.UnitZ);
    Need(movement.Tick(8)==ReplacementControlledMovementStatus.WaitingReady&&!movement.Armed&&movement.RootsQueued==0);
    Stage(run,zone);minimum.TrySetResult();Step(run,10);Step(run,10);
    Need(run.ReadySubmissions==1&&world.State==ZoneSceneTransitionState.AwaitingServerReady);
    Need(movement.Tick(10)==ReplacementControlledMovementStatus.WaitingReady&&!movement.Armed&&store.Find(world.Snapshots().Single()) is null);
    Feed(run,zone,zp,Hex("ServerReady"),11);Feed(run,zone,zp,Item("ZoneAcks",2),12);
    Need(movement.Tick(12)==ReplacementControlledMovementStatus.WaitingTime&&movement.Armed&&movement.LastAcceptedMilliseconds==12);
    Reject(()=>movement.Tick(11));Need(movement.RootsQueued==0&&movement.LastAcceptedMilliseconds==12);
    Task.Run(()=>Reject(()=>movement.Tick(12))).GetAwaiter().GetResult();
    Need(movement.Tick(411)==ReplacementControlledMovementStatus.WaitingTime&&movement.RootsQueued==0);
    if(mode==2)
    {using var cancel=new CancellationTokenSource();cancel.Cancel();Need(movement.Tick(412,cancel.Token)==ReplacementControlledMovementStatus.Stopped&&movement.StopReason=="Canceled"&&movement.RootsQueued==0);}
    else if(mode==3)
    {run.BeginDisconnect(412);Need(movement.Tick(412)==ReplacementControlledMovementStatus.Stopped&&movement.RootsQueued==0);}
    else if(mode==4)
    {Feed(run,zone,zp,Item("CorrectionFrames",0),412);Need(run.AppliedCorrections==1);Need(movement.Tick(412)==ReplacementControlledMovementStatus.Stopped&&movement.StopReason=="CorrectionRisk"&&movement.RootsQueued==0);}
    else if(mode==5)
    {var snapshot=world.Snapshots().Single();Need(store.PublishLocal(snapshot,snapshot.Placement.Transform,0));Need(movement.Tick(412)==ReplacementControlledMovementStatus.Stopped&&movement.StopReason=="CorrectionOrForeignMotion");}
    else if(mode==6)
    {movement.Dispose();Need(movement.Tick(412)==ReplacementControlledMovementStatus.Stopped&&movement.RootsQueued==0);}
    else if(mode==8)
    {Need(world.Accept(world.Generation,SceneObjectLifecycleCodec.EncodeCreate(world.Snapshots().Single().Object.Create with{ObjectId=99})));Need(world.Accept(world.Generation,SceneObjectLifecycleCodec.EncodeContainment(new(Oid,99,0))));Need(movement.Tick(412)==ReplacementControlledMovementStatus.Stopped&&movement.StopReason=="UnsupportedPlacement"&&movement.RootsQueued==0);}
    else if(mode==9)
    {world.Close();Need(movement.Tick(412)==ReplacementControlledMovementStatus.Stopped&&movement.RootsQueued==0);}
    else if(mode==7)
    {
        var row=store.Find(world.Snapshots().Single());var large=new byte[1500000];large[0]=5;
        var exchange=s.QueueZoneApplication(large,12);foreach(var bytes in exchange.Datagrams)Array.Clear(bytes.Bytes);
        Need(s.ZonePendingMessages==1);
        Need(movement.Tick(412)==ReplacementControlledMovementStatus.Backpressure&&movement.RootsQueued==0&&movement.LastAcceptedMilliseconds==12);
        Need(movement.Tick(2000)==ReplacementControlledMovementStatus.Backpressure&&ReferenceEquals(row,store.Find(world.Snapshots().Single())));
    }
    else
    {
        Need(movement.Tick(412)==ReplacementControlledMovementStatus.Queued&&movement.RootsQueued==1);Step(run,412);
        var row=store.Find(world.Snapshots().Single());Need(row is {Speed:0.5f});
        long second=mode==0?812:2000;
        Feed(run,zone,zp,Item("MovementAcks",0),second);
        Need(movement.Tick(second)==ReplacementControlledMovementStatus.Queued&&movement.RootsQueued==2);Step(run,second);
        Need(movement.Tick(second)==ReplacementControlledMovementStatus.WaitingTime);
        Feed(run,zone,zp,Item("MovementAcks",1),second);
        Need(movement.Tick(second+399)==ReplacementControlledMovementStatus.WaitingTime&&movement.RootsQueued==2);
        Need(movement.Tick(second+400)==ReplacementControlledMovementStatus.Queued&&movement.RootsQueued==3);Step(run,second+400);
        Feed(run,zone,zp,Item("MovementAcks",2),second+400);
        Need(movement.Tick(second+800)==ReplacementControlledMovementStatus.Completed&&movement.RootsQueued==3);
        allWires.Add(zone.Sent.Where(x=>x.Hex.StartsWith("0009")).Select(x=>x.Hex).TakeLast(3).ToList());
    }
    movement.Dispose();Need(s.Phase is ReplacementClientPhase.Selected or ReplacementClientPhase.Closed);world.Close();pump.Dispose();s.Dispose();Drain(run);
}
Console.WriteLine(JsonSerializer.Serialize(new{Passed=true,Checks=checks,NegativeCases=negatives,Wires=allWires,TimingCases=2,StopCases=8,Synthetic=true,SocketCreated=false}));
}
catch(Exception error){Console.Error.WriteLine(error);Environment.ExitCode=1;}

sealed class FakePort:IDatagramPort,IDisposable
{
    public readonly List<(DatagramPeer Peer,string Hex)> Sent=[];public int Disposals;
    private TaskCompletionSource<InboundDatagram>? receive;
    public ValueTask SendAsync(DatagramPeer peer,ReadOnlyMemory<byte> bytes,CancellationToken token)
    {Sent.Add((peer,Convert.ToHexString(bytes.Span)));return ValueTask.CompletedTask;}
    public ValueTask<InboundDatagram> ReceiveAsync(CancellationToken token)
    {receive=new(TaskCreationOptions.RunContinuationsAsynchronously);var captured=receive;token.Register(()=>captured.TrySetCanceled(token));return new(captured.Task);}
    public void Deliver(DatagramPeer peer,byte[] bytes){if(receive is null||!receive.TrySetResult(new(peer,bytes)))throw new Exception("No outstanding receive");}
    public ValueTask DisposeAsync(){Disposals++;return ValueTask.CompletedTask;}
    public void Dispose() { }
}
