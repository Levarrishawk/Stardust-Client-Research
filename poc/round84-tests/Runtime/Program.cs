using System.Buffers.Binary;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Configuration;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

if(args.Length!=1)throw new ArgumentException("Explicit independent private fixture JSON required.");
using var data=JsonDocument.Parse(File.ReadAllBytes(args[0]));var root=data.RootElement;
byte[] Hex(string name)=>Convert.FromHexString(root.GetProperty(name).GetString()!);
const ulong Oid=0x1122334455667788;int checks=0,negatives=0;
void Need(bool condition){checks++;if(!condition)throw new Exception("Check "+checks);}
void Reject(Action action){try{action();throw new Exception("Expected rejection");}catch(Exception e)when(e is InvalidOperationException or InvalidDataException or TaskCanceledException or ArgumentOutOfRangeException){negatives++;}}
var options=new LoginClientOptions("login.invalid",44452,"20260817-SD:01");
ReplacementClientSession Session(bool permissions=true)
{
    var s=new ReplacementClientSession(options);s.StartLogin(0x12345678,0);s.AcceptIncoming(ReplacementConnection.Login,Hex("LoginResponse"),0);s.Authenticate("synthetic","synthetic",1);
    foreach(var wire in root.GetProperty("Login").EnumerateArray())s.AcceptIncoming(ReplacementConnection.Login,Convert.FromHexString(wire.GetString()!),2);
    s.BeginZone(Oid,0x87654321,3);Need(s.SelectedObjectId==Oid);
    s.AcceptIncoming(ReplacementConnection.Zone,Hex("ZoneResponse"),4);
    if(permissions)s.AcceptIncoming(ReplacementConnection.Zone,Hex("Permissions"),5);
    return s;
}
SceneWorldCoordinator World()
{
    const string pin="AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    var catalog=new ObjectTemplateCatalog([new(root.GetProperty("TemplateCrc").GetUInt32(),"object/synthetic/fixture.iff",pin,pin)]);
    var idle=new LogicalAnimationClipSelection("appearance/lat/authored.lat","idle",pin);
    return new(catalog,idle,new Dictionary<string,LogicalAnimationClipSelection>{{"walk",idle},{"run",idle}},(q,t)=>throw new InvalidOperationException("No visual loading in minimum-barrier test"),SceneCreateZeroQuaternionPolicy.Reject);
}
ReplacementClientExchange Apps(params byte[][] values)=>new([],values);
ReplacementClientExchange Receive(ReplacementClientSession session,ReplacementSceneBridge bridge,string name,long clock=6)=>bridge.ObserveExchange(session.AcceptIncoming(ReplacementConnection.Zone,Hex(name),clock));
var readyWires=new List<string>();
using(var session=Session())using(var world=World())
{
    var minimum=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);int calls=0;
    using var bridge=new ReplacementSceneBridge(session,world,(scene,token)=>{calls++;Need(scene.ObjectId==Oid);return minimum.Task;});
    bridge.BindSelection();Need(bridge.Generation==world.Generation&&session.SelectionSubmissions==1);
    var start=Receive(session,bridge,"PostSelection");Need(start.FailureType is null&&start.Datagrams.Count==1&&calls==1&&world.State==ZoneSceneTransitionState.Initializing);
    Receive(session,bridge,"Create");Receive(session,bridge,"Baseline");Receive(session,bridge,"End");
    Need(world.StagedCount==3&&world.Snapshots().Count==0);
    var unhandled=Receive(session,bridge,"Sentinel");Need(unhandled.ZoneApplications.Count==1&&unhandled.ZoneApplications[0].SequenceEqual(Hex("SentinelHex")));
    var poll=bridge.Poll(7);Need(bridge.ReadySubmissions==0&&world.StagedCount==3&&poll.Datagrams.Count==0);
    minimum.SetResult();Need(bridge.DrainCompletion.Wait(1000));
    poll=bridge.Poll(8);readyWires.AddRange(poll.Datagrams.Select(d=>Convert.ToHexString(d.Bytes)));
    Need(bridge.ReadySubmissions==1&&world.State==ZoneSceneTransitionState.AwaitingServerReady&&world.Snapshots().Count==1&&world.StagedCount==0);
    Need(world.Snapshots()[0].Object.Handle.ObjectId==Oid&&world.Snapshots()[0].Envelopes.Count==1);
    bridge.Poll(9);Need(bridge.ReadySubmissions==1);
    var ready=Receive(session,bridge,"ServerReady",10);Need(ready.FailureType is null&&world.State==ZoneSceneTransitionState.Ready);
    byte[] foreign=Hex("UnknownFamilyHex");var returned=bridge.ObserveExchange(Apps(foreign));Need(foreign.All(b=>b==0)&&returned.ZoneApplications[0].SequenceEqual(Hex("UnknownFamilyHex")));
    byte[] unknown=Hex("SentinelHex");returned=bridge.ObserveExchange(Apps(unknown));Need(unknown.All(b=>b==0)&&returned.ZoneApplications[0].SequenceEqual(Hex("SentinelHex")));
    bridge.Dispose();Need(session.Phase==ReplacementClientPhase.Selected&&world.State==ZoneSceneTransitionState.Ready&&bridge.DrainCompletion.IsCompleted);
}
foreach(string kind in new[]{"fault","resourceCanceled","cancel","stale","wrongId","earlyReady","malformed","doubleStart","failedExchange"})
{
    using var session=Session();using var world=World();var source=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);CancellationToken token=default;
    using var bridge=new ReplacementSceneBridge(session,world,(scene,t)=>{token=t;return source.Task;});bridge.BindSelection();
    if(kind=="wrongId")
    {
        byte[] start=Hex("PostSelectionHex");BinaryPrimitives.WriteUInt64LittleEndian(start.AsSpan(7),Oid+1);var x=bridge.ObserveExchange(Apps(start));Need(x.FailureType is not null&&start.All(b=>b==0));negatives++;
    }
    else if(kind=="earlyReady") {var x=bridge.ObserveExchange(Apps(Hex("ExpectedReady")));Need(x.FailureType is not null);negatives++;}
    else if(kind=="malformed") {byte[] baseline=Hex("BaselineHex");var x=bridge.ObserveExchange(Apps(baseline[..^1]));Need(x.FailureType is not null);negatives++;}
    else if(kind=="failedExchange") {var x=bridge.ObserveExchange(new([],[],true,"AuthoredFailure"));Need(x.RejectedFraming&&x.FailureType=="AuthoredFailure");negatives++;}
    else
    {
        bridge.ObserveExchange(Apps(Hex("PostSelectionHex")));
        if(kind=="fault") {source.SetException(new InvalidDataException("authored minimum failure"));Reject(()=>bridge.Poll(7));}
        if(kind=="resourceCanceled") {source.SetCanceled();Reject(()=>bridge.Poll(7));}
        if(kind=="cancel")
        {bridge.Dispose();Need(token.IsCancellationRequested&&!bridge.DrainCompletion.IsCompleted);source.SetResult();Need(bridge.DrainCompletion.Wait(1000));Need(session.Phase==ReplacementClientPhase.Selected&&world.State==ZoneSceneTransitionState.Initializing);Reject(()=>bridge.Poll(7));continue;}
        if(kind=="stale") {world.BeginSelection(Oid);source.SetResult();Reject(()=>bridge.Poll(7));}
        if(kind=="doubleStart") {var x=bridge.ObserveExchange(Apps(Hex("PostSelectionHex")));Need(x.FailureType is not null&&token.IsCancellationRequested);source.SetCanceled();negatives++;}
    }
    Need(bridge.ReadySubmissions==0&&session.Phase==ReplacementClientPhase.Closed&&world.State==ZoneSceneTransitionState.Closed);
    if(!source.Task.IsCompleted)source.SetCanceled();Need(bridge.DrainCompletion.Wait(1000));
}
using(var session=Session(false))using(var world=World())using(var bridge=new ReplacementSceneBridge(session,world,(scene,t)=>Task.CompletedTask))
{
    bridge.BindSelection();Need(world.State==ZoneSceneTransitionState.Selecting);Reject(()=>session.QueueZoneApplication(Hex("ExpectedReady"),5));Need(session.SelectedObjectId==0);
}
using(var session=Session())using(var world=World())using(var bridge=new ReplacementSceneBridge(session,world,(scene,t)=>Task.CompletedTask))
{bridge.BindSelection();Reject(()=>bridge.BindSelection());Need(session.Phase==ReplacementClientPhase.Closed&&world.State==ZoneSceneTransitionState.Closed);}
using(var session=Session())
{
    byte[] big=new byte[4096];Hex("ExpectedReady").CopyTo(big,0);var exchange=session.QueueZoneApplication(big,6);Array.Fill(big,(byte)0);Need(exchange.Datagrams.Count>1&&exchange.Datagrams.All(d=>d.Bytes.Length<=496));
    Reject(()=>session.QueueZoneApplication(new byte[5],7));Need(session.SelectedObjectId==0&&session.ZoneRetainedPackets==0);
}
using(var session=Session())using(var world=World())using(var bridge=new ReplacementSceneBridge(session,world,(scene,t)=>throw new InvalidDataException("synchronous authored provider failure")))
{bridge.BindSelection();var result=bridge.ObserveExchange(Apps(Hex("PostSelectionHex")));Need(result.FailureType is not null&&bridge.DrainCompletion.IsCompleted&&world.State==ZoneSceneTransitionState.Closed);negatives++;}
using(var session=Session())using(var world=World())using(var bridge=new ReplacementSceneBridge(session,world,(scene,t)=>Task.CompletedTask))
{
    bridge.BindSelection();bridge.ObserveExchange(Apps(Hex("PostSelectionHex"),Hex("EndHex")));
    // Structurally valid staged end, but no matching create: ordered barrier replay must fail with no ready.
    Reject(()=>bridge.Poll(7));Need(bridge.ReadySubmissions==0&&session.ZoneRetainedPackets==0&&world.State==ZoneSceneTransitionState.Closed);
}
using(var session=Session())using(var world=World())using(var bridge=new ReplacementSceneBridge(session,world,(scene,t)=>Task.CompletedTask))
{
    Reject(()=>bridge.Poll(7));Need(world.State==ZoneSceneTransitionState.Closed);
}
using(var session=Session())using(var world=World())using(var bridge=new ReplacementSceneBridge(session,world,(scene,t)=>Task.CompletedTask))
{
    bridge.BindSelection();Exception? wrongThread=null;var foreign=new Thread(()=>{try{bridge.Dispose();}catch(Exception e){wrongThread=e;}});foreign.Start();foreign.Join();Need(wrongThread is InvalidOperationException&&world.State==ZoneSceneTransitionState.Selecting);negatives++;
    byte[] wire=[1,2,3];var result=bridge.ObserveExchange(new([new(ReplacementConnection.Zone,wire)],[],true));Need(result.RejectedFraming&&ReferenceEquals(result.Datagrams[0].Bytes,wire)&&wire[0]==1);
}
using(var session=Session())using(var world=World())
{
    ReplacementSceneBridge? bridge=null;
    bridge=new(session,world,(scene,t)=>{Reject(()=>bridge!.Poll(7));return Task.CompletedTask;});
    using(bridge){bridge.BindSelection();var exchange=bridge.ObserveExchange(Apps(Hex("PostSelectionHex")));Need(exchange.FailureType is null&&bridge.ReadySubmissions==0);bridge.Poll(7);Need(bridge.ReadySubmissions==1);}
}
using(var session=Session())using(var world=World())using(var bridge=new ReplacementSceneBridge(session,world,(scene,t)=>Task.CompletedTask))
{bridge.BindSelection();var tooBig=new byte[SoeClientTransport.MaximumMessageSize+1];Hex("SentinelHex").CopyTo(tooBig,0);var result=bridge.ObserveExchange(Apps(tooBig));Need(result.FailureType is not null&&tooBig.All(b=>b==0)&&world.State==ZoneSceneTransitionState.Closed);negatives++;}
Console.WriteLine(JsonSerializer.Serialize(new{Passed=true,Checks=checks,NegativeCases=negatives,ReadyWires=readyWires,OpaqueBaselineSemantics=true,VisualPublication=false,LiveCompatibility=false}));
