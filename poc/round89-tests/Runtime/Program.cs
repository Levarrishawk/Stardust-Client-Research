using System.Text.Json;
using System.Text;
using Stardust.Core.Assets;
using Stardust.Core.Configuration;
using Stardust.Core.Hosting;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
try
{
using var doc=JsonDocument.Parse(File.ReadAllBytes(args[0]));var f=doc.RootElement;var peer=f.GetProperty("Peer");
var profile=ReplacementLaunchProfileCodec.Decode(Encoding.UTF8.GetBytes(f.GetProperty("Profile").GetRawText()));
var options=new LoginClientOptions("login.invalid",44452,"20260817-SD:01");var lp=new DatagramPeer(0x7f000001,44452);var zp=new DatagramPeer(0x7f000002,44453);
int checks=0,negatives=0;void Need(bool v){checks++;if(!v)throw new Exception("Check "+checks);}
void Reject(Action a){try{a();}catch(Exception e)when(e is InvalidOperationException or ArgumentException or IOException or OperationCanceledException){negatives++;return;}throw new Exception("Expected rejection");}
byte[] Hex(string key)=>Convert.FromHexString(peer.GetProperty(key).GetString()!);
void Exclusive(){using var file=new FileStream(Path.Combine(profile.AssetRoot,"data.tre"),FileMode.Open,FileAccess.ReadWrite,FileShare.None);checks++;}
ReplacementClientApplication App(FakePort login,FakePort zone,Func<CmdStartSceneMessage,ProceduralTerrainInput,Func<string,TreLookupResult>,CancellationToken,Task> minimum)
    =>new(profile,options,lp,zp,(connection,p)=>connection==ReplacementConnection.Login?login:zone,minimum,new(1000,10));
void Step(ReplacementClientApplication app,long now){var tick=app.Tick(now);Need(tick.FailureType is null);foreach(var a in tick.UnhandledApplications)Array.Clear(a);}
void Feed(ReplacementClientApplication app,FakePort port,DatagramPeer p,byte[] bytes,long now){port.Deliver(p,bytes);Step(app,now);Need(bytes.All(b=>b==0));}
void Admit(ReplacementClientApplication app,FakePort login,FakePort zone)
{
    app.StartLogin(0x12345678,0);Step(app,0);Feed(app,login,lp,Hex("LoginResponse"),1);app.Authenticate("synthetic","synthetic",2);Step(app,2);
    foreach(var v in peer.GetProperty("LoginReplies").EnumerateArray())Feed(app,login,lp,Convert.FromHexString(v.GetString()!),3);
    app.SelectCharacter(0x1122334455667788,0x87654321,4);app.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();Step(app,4);
    Feed(app,zone,zp,Hex("ZoneResponse"),5);Step(app,5);Feed(app,zone,zp,Convert.FromHexString(peer.GetProperty("ZoneAcks")[0].GetString()!),6);
    Feed(app,zone,zp,Hex("Permissions"),7);Step(app,7);Feed(app,zone,zp,Convert.FromHexString(peer.GetProperty("ZoneAcks")[1].GetString()!),8);
}
void LoginOnly(ReplacementClientApplication app,FakePort login)
{
    app.StartLogin(0x12345678,0);Step(app,0);Feed(app,login,lp,Hex("LoginResponse"),1);app.Authenticate("synthetic","synthetic",2);Step(app,2);
    foreach(var v in peer.GetProperty("LoginReplies").EnumerateArray())Feed(app,login,lp,Convert.FromHexString(v.GetString()!),3);
    Need(app.Session.Phase==ReplacementClientPhase.CharacterList);
}
bool delayed=false,faultFinalized=false,rollback=false,stale=false;object? sources=null;string auth="",select="";
{
    var login=new FakePort();var zone=new FakePort();var minimum=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var presentation=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    Func<string,TreLookupResult>? resolve=null;CancellationToken token=default;
    var app=App(login,zone,(scene,terrain,lookup,t)=>{Need(terrain.Version=="0014");resolve=lookup;token=t;Need(lookup("object/root.iff").Kind==TreLookupKind.Present);return minimum.Task;});
    sources=app.Creature.Sources;Admit(app,login,zone);auth=zone.Sent.Single(x=>x==peer.GetProperty("ExpectedAuthWire").GetString());select=zone.Sent.Single(x=>x==peer.GetProperty("ExpectedSelectWire").GetString());
    Feed(app,zone,zp,Convert.FromHexString(peer.GetProperty("WorldFrames")[0].GetString()!),9);
    var borrowed=app.Run;Reject(Exclusive);app.BeginClose(presentation.Task);Need(token.IsCancellationRequested&&app.Closing&&zone.Disposals==1&&!app.IsFinalized);
    delayed=!app.TryFinalizeClose(presentation.Task);Need(delayed);Reject(()=>resolve!("object/root.iff"));stale=true;presentation.TrySetException(new IOException("synthetic presentation"));
    Need(!app.TryFinalizeClose(presentation.Task));minimum.TrySetException(new IOException("late minimum"));
    try{app.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();}catch(IOException){negatives++;}
    Need(app.TryFinalizeClose(presentation.Task)&&app.CleanupFailures.Count>0);int observed=app.CleanupFailures.Count;
    Need(app.TryFinalizeClose(presentation.Task)&&app.CleanupFailures.Count==observed);faultFinalized=app.IsFinalized;Exclusive();Need(borrowed.ReadySubmissions==0);Reject(()=>_ = app.World);app.Dispose();app.Dispose();
}
// Returned factory port is retired even when run admission fails, and asynchronous rollback is observable.
{
    var port=new FakePort{PendingDisposal=true};ReplacementClientApplication.StartupFailure? startup=null;
    try{_ =new ReplacementClientApplication(profile,options,lp,zp,(c,p)=>port,(s,t,l,k)=>throw new Exception("Provider cannot run during startup"),new(0,10));}catch(ReplacementClientApplication.StartupFailure e){startup=e;negatives++;}
    Need(startup is not null&&startup.FailureType==nameof(ArgumentOutOfRangeException)&&port.Disposals==1&&!startup.RollbackCompletion.IsCompleted);Exclusive();
    port.Disposal.TrySetException(new IOException("synthetic rollback"));try{startup!.RollbackCompletion.GetAwaiter().GetResult();}catch(Exception e)when(e is AggregateException or IOException){rollback=true;negatives++;}Need(rollback);
}
foreach(string mode in new[]{"profile","factory","null","peer"})
{
    int calls=0;try
    {
        _=new ReplacementClientApplication(mode=="profile"?profile with{Version=2}:profile,options,
            mode=="peer"?new(lp.Address,1):lp,zp,(c,p)=>{calls++;if(mode=="factory")throw new IOException("factory");return null!;},(s,t,l,k)=>Task.CompletedTask,new(1000,10));
        throw new Exception("Expected bad startup");
    }catch(ReplacementClientApplication.StartupFailure e){e.RollbackCompletion.GetAwaiter().GetResult();negatives++;}
    catch(ArgumentException){negatives++;}
    Need(calls==((mode is "profile" or "peer")?0:1));Exclusive();
}
// Plain Dispose retires but cannot claim presentation completion or finalize installation handles.
{
    var login=new FakePort{PendingDisposal=true};var zone=new FakePort();var app=App(login,zone,(s,t,l,k)=>Task.CompletedTask);var completion=new TaskCompletionSource();
    Exception? wrong=null;var thread=new Thread(()=>{try{app.Dispose();}catch(Exception e){wrong=e;}});thread.Start();thread.Join();Need(wrong is InvalidOperationException&&!app.Closing);negatives++;
    app.Dispose();Need(app.Closing&&!app.IsFinalized&&!app.TryFinalizeClose(completion.Task));Reject(Exclusive);
    login.Disposal.TrySetResult();completion.TrySetResult();app.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();Need(app.TryFinalizeClose(completion.Task));Exclusive();Reject(()=>app.TryFinalizeClose(Task.FromException(new IOException())));app.Dispose();
}
// An unadmitted returned zone port and a failed disposal do not leak the original installation.
foreach(string mode in new[]{"throw","alias","peer"})
{
    var login=new FakePort();var zone=new FakePort();int calls=0;
    var app=new ReplacementClientApplication(profile,options,lp,mode=="peer"?new(zp.Address,44454):zp,(c,p)=>
    {calls++;if(c==ReplacementConnection.Login)return login;if(mode=="throw")throw new IOException("zone factory");return mode=="alias"?login:zone;},(s,t,l,k)=>Task.CompletedTask,new(1000,10));
    LoginOnly(app,login);Reject(()=>app.SelectCharacter(0x1122334455667788,0x87654321,4));
    Need(calls==2&&login.Disposals==1&&zone.Disposals==(mode=="peer"?1:0)&&app.Closing&&app.FailureType is not null);
    app.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();Need(app.TryFinalizeClose(Task.CompletedTask));Need(app.CleanupFailures.Count==0);Exclusive();app.Dispose();
}
// Provider callback reentry cannot tick or close the application while its outer Tick owns it.
{
    var login=new FakePort();var zone=new FakePort();ReplacementClientApplication? active=null;
    var minimum=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var app=App(login,zone,(s,t,l,k)=>{Reject(()=>active!.Tick(9));Reject(()=>active!.BeginClose(Task.CompletedTask));return minimum.Task;});active=app;
    Admit(app,login,zone);Feed(app,zone,zp,Convert.FromHexString(peer.GetProperty("WorldFrames")[0].GetString()!),9);
    Need(!app.Closing&&app.FailureType is null&&app.Run.ReadySubmissions==0);app.BeginClose(Task.CompletedTask);minimum.TrySetCanceled();
    app.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();Need(app.TryFinalizeClose(Task.CompletedTask));Need(app.CleanupFailures.Count==0);Exclusive();app.Dispose();
}
{
    var login=new FakePort();var zone=new FakePort{FailDisposal=true};var app=App(login,zone,(s,t,l,k)=>Task.CompletedTask);
    Reject(()=>app.SelectCharacter(1,2,0));Need(app.Closing&&zone.Disposals==1);var completion=Task.CompletedTask;
    try{app.DrainCompletion.GetAwaiter().GetResult();}catch(Exception e)when(e is AggregateException or IOException){negatives++;}
    Need(app.TryFinalizeClose(completion)&&app.CleanupFailures.Count>0);Exclusive();app.Dispose();
}
Console.WriteLine(JsonSerializer.Serialize(new{Passed=true,Checks=checks,NegativeCases=negatives,Sources=sources,AuthWire=auth,SelectWire=select,DelayedClose=delayed,FaultFinalized=faultFinalized,RollbackObserved=rollback,StaleResolverRejected=stale,SocketCreated=false}));
}
catch(Exception error){Console.Error.WriteLine(error);Environment.ExitCode=1;}
sealed class FakePort:IDatagramPort
{
    public List<string> Sent=[];public int Disposals;public bool PendingDisposal,FailDisposal;
    public TaskCompletionSource Disposal=new(TaskCreationOptions.RunContinuationsAsynchronously);private TaskCompletionSource<InboundDatagram>? receive;
    public ValueTask SendAsync(DatagramPeer p,ReadOnlyMemory<byte> bytes,CancellationToken t){Sent.Add(Convert.ToHexString(bytes.Span));return ValueTask.CompletedTask;}
    public ValueTask<InboundDatagram> ReceiveAsync(CancellationToken t){receive=new(TaskCreationOptions.RunContinuationsAsynchronously);var captured=receive;t.Register(()=>captured.TrySetCanceled(t));return new(captured.Task);}
    public void Deliver(DatagramPeer p,byte[] bytes){if(receive is null||!receive.TrySetResult(new(p,bytes)))throw new Exception("No receive");}
    public ValueTask DisposeAsync(){Disposals++;return PendingDisposal?new(Disposal.Task):FailDisposal?new(Task.FromException(new IOException("synthetic disposal"))):ValueTask.CompletedTask;}
}
