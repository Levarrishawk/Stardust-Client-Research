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
void Reject(Action a){try{a();}catch(Exception e)when(e is InvalidOperationException or ArgumentException or IOException or InvalidDataException or OperationCanceledException){Need(!e.Message.Contains("zone.invalid")&&!e.Message.Contains("127.0.0.")&&!e.Message.Contains("synthetic"));negatives++;return;}throw new Exception("Expected rejection");}
byte[] Hex(string key)=>Convert.FromHexString(peer.GetProperty(key).GetString()!);
void Exclusive(){using var file=new FileStream(Path.Combine(profile.AssetRoot,"data.tre"),FileMode.Open,FileAccess.ReadWrite,FileShare.None);checks++;}
void Step(ReplacementClientApplication app,long now){var tick=app.Tick(now);Need(tick.FailureType is null);foreach(var a in tick.UnhandledApplications)Array.Clear(a);}
void Feed(ReplacementClientApplication app,FakePort port,DatagramPeer p,byte[] bytes,long now){port.Deliver(p,bytes);Step(app,now);Need(bytes.All(b=>b==0));}
var launch=ReplacementControlledLaunchCodec.Decode(Encoding.UTF8.GetBytes(f.GetProperty("Launch").GetRawText()));
Need(launch.Profile.Version==1&&launch.Options.ClientVersion=="20260817-SD:01"&&launch.LoginPeer==lp&&launch.ZonePeer==zp&&launch.MinimumReadinessApproved&&launch.MovementScenario is null);
Need(launch.EndpointApproval.ApprovedCharacterId==0x1122334455667788&&launch.ApprovedCharacterId==0x1122334455667788&&!launch.ToString().Contains("invalid"));
var moving=ReplacementControlledLaunchCodec.Decode(Encoding.UTF8.GetBytes(f.GetProperty("MovementLaunch").GetRawText()));Need(moving.MovementScenario?.SelectedObjectId==launch.ApprovedCharacterId);
var disabled=ReplacementControlledLaunchCodec.Decode(Encoding.UTF8.GetBytes(f.GetProperty("DisabledMovementLaunch").GetRawText()));Need(disabled.MovementScenario is null);
int invalidInputs=0;foreach(var raw in f.GetProperty("InvalidLaunchHex").EnumerateArray()){Reject(()=>ReplacementControlledLaunchCodec.Decode(Convert.FromHexString(raw.GetString()!)));invalidInputs++;}
Reject(()=>new ReplacementEndpointApproval(new("login.invalid",lp.Address,lp.Port),new("zone.invalid",zp.Address,zp.Port),7,0));
string auth="",select="";bool wrongCharacterPreserved=false;
{
    var login=new FakePort();var zone=new FakePort();int factories=0;
    // A received listed character is not the operator-approved identity; same endpoint/galaxy is insufficient.
    var wrongApproval=new ReplacementEndpointApproval(launch.EndpointApproval.Login,launch.EndpointApproval.Zone,7,99);
    var app=new ReplacementClientApplication(launch.Profile,launch.Options,launch.LoginPeer,launch.ZonePeer,(c,p)=>{if(c==ReplacementConnection.Zone){factories++;return zone;}return login;},(s,t,l,k)=>Task.CompletedTask,new(1000,10),endpointApproval:wrongApproval);
    app.StartLogin(launch.LoginConnectionId,0);Step(app,0);Feed(app,login,lp,Hex("LoginResponse"),1);app.Authenticate("synthetic","synthetic",2);Step(app,2);
    foreach(var raw in peer.GetProperty("LoginReplies").EnumerateArray())Feed(app,login,lp,Convert.FromHexString(raw.GetString()!),3);
    Reject(()=>app.SelectCharacter(launch.ApprovedCharacterId,launch.ZoneConnectionId,1000));
    Need(factories==0&&zone.Sent.Count==0&&login.Disposals==0&&!app.Closing&&app.Session.Phase==ReplacementClientPhase.CharacterList&&app.Session.CredentialLength==5&&app.Session.SelectedObjectId==0&&app.Session.SelectionSubmissions==0&&app.Run.Generation==0);
    Step(app,3);wrongCharacterPreserved=true;
    var budget=new ReplacementControlledAttempt(launch.PhaseBudgetMilliseconds,launch.OverallBudgetMilliseconds);budget.Start(0);
    Need(budget.Observe(3,new(app.Session.Phase,false,false,false)).Stage==ReplacementControlledAttemptStage.CharacterSelection);
    Need(budget.Observe(30003,new(app.Session.Phase,false,false,false)).StopReason==ReplacementControlledAttemptStopReason.PhaseTimeout);
    Need(!app.Closing&&login.Disposals==0&&app.Session.CredentialLength==5&&app.Session.Phase==ReplacementClientPhase.CharacterList);

    app.BeginClose(Task.CompletedTask);app.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();Need(app.TryFinalizeClose(Task.CompletedTask)&&app.CleanupFailures.Count==0);Exclusive();app.Dispose();
}
{
    var login=new FakePort();var zone=new FakePort();int factories=0;
    var app=new ReplacementClientApplication(launch.Profile,launch.Options,launch.LoginPeer,launch.ZonePeer,(c,p)=>{if(c==ReplacementConnection.Zone){factories++;return zone;}return login;},(s,t,l,k)=>Task.CompletedTask,new(1000,10),endpointApproval:launch.EndpointApproval);
    app.StartLogin(launch.LoginConnectionId,0);Step(app,0);Feed(app,login,lp,Hex("LoginResponse"),1);app.Authenticate("synthetic","synthetic",2);Step(app,2);
    foreach(var raw in peer.GetProperty("LoginReplies").EnumerateArray())Feed(app,login,lp,Convert.FromHexString(raw.GetString()!),3);
    Reject(()=>app.SelectCharacter(99,launch.ZoneConnectionId,1000));Need(factories==0&&app.Session.CredentialLength==5&&!app.Closing);Step(app,3);
    app.SelectCharacter(launch.ApprovedCharacterId,launch.ZoneConnectionId,4);Need(factories==1);app.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();Step(app,4);
    Feed(app,zone,zp,Hex("ZoneResponse"),5);Step(app,5);Feed(app,zone,zp,Convert.FromHexString(peer.GetProperty("ZoneAcks")[0].GetString()!),6);
    Feed(app,zone,zp,Hex("Permissions"),7);Step(app,7);Feed(app,zone,zp,Convert.FromHexString(peer.GetProperty("ZoneAcks")[1].GetString()!),8);
    var wires=zone.Sent.Where(x=>x.StartsWith("0009")).ToArray();Need(wires.Length==2&&app.Session.SelectedObjectId==launch.ApprovedCharacterId);auth=wires[0];select=wires[1];
    app.BeginClose(Task.CompletedTask);app.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();Need(app.TryFinalizeClose(Task.CompletedTask)&&app.CleanupFailures.Count==0);Exclusive();app.Dispose();
}
ReplacementControlledAttemptObservation Obs(ReplacementClientPhase phase,bool client=false,bool server=false,bool visible=false)=>new(phase,client,server,visible);
var outcomes=new List<object>();
foreach(var test in f.GetProperty("ExpectedAttemptCases").EnumerateArray())
{
    string name=test.GetProperty("Name").GetString()!;var times=test.GetProperty("Times").EnumerateArray().Select(x=>x.GetInt64()).ToArray();
    var attempt=new ReplacementControlledAttempt(30000,120000);Need(!attempt.State.Started&&attempt.State.Stage==ReplacementControlledAttemptStage.NotStarted);
    Reject(()=>attempt.Observe(0,Obs(ReplacementClientPhase.LoginHandshake)));attempt.Start(times[0]);
    for(int i=1;i<times.Length;i++)
    {
        var observation=name=="overall"?Obs(new[]{ReplacementClientPhase.LoginAuthentication,ReplacementClientPhase.LoginResponses,ReplacementClientPhase.CharacterList,ReplacementClientPhase.Selected,ReplacementClientPhase.Selected}[i-1]):name=="ready-stall"?Obs(ReplacementClientPhase.Selected,true):name=="visible-stall"?Obs(ReplacementClientPhase.Selected,true,true):Obs(ReplacementClientPhase.LoginHandshake);
        var state=attempt.Observe(times[i],observation);Need(state.StopReason==(i==times.Length-1?Enum.Parse<ReplacementControlledAttemptStopReason>(test.GetProperty("Reason").GetString()!):ReplacementControlledAttemptStopReason.None));
    }
    var terminal=attempt.State;Need(terminal.ElapsedMilliseconds<=120000&&attempt.Transitions.Count<=10);
    Need(attempt.Observe(times[^1]+100,Obs(ReplacementClientPhase.Closed))==terminal);Reject(()=>attempt.Start(times[^1]+100));
    outcomes.Add(new{Name=name,Reason=terminal.StopReason.ToString()});
}
foreach(var budget in new[]{(0L,120000L),(30001L,120000L),(30000L,120001L),(30000L,29999L)})Reject(()=>new ReplacementControlledAttempt(budget.Item1,budget.Item2));
{
    var attempt=new ReplacementControlledAttempt(30000,120000);attempt.Start(0);Reject(()=>attempt.Start(1));Reject(()=>attempt.Observe(-1,Obs(ReplacementClientPhase.LoginHandshake)));
    Need(attempt.Observe(1,Obs(ReplacementClientPhase.LoginAuthentication)).Stage==ReplacementControlledAttemptStage.Credentials);
    Reject(()=>attempt.Observe(0,Obs(ReplacementClientPhase.LoginHandshake)));Need(attempt.State.ElapsedMilliseconds==1);
    Task.Run(()=>Reject(()=>attempt.Observe(2,Obs(ReplacementClientPhase.LoginResponses)))).GetAwaiter().GetResult();
    Need(attempt.Observe(2,Obs(ReplacementClientPhase.LoginResponses)).Stage==ReplacementControlledAttemptStage.LoginResponses);
    Need(attempt.Observe(3,Obs(ReplacementClientPhase.CharacterList)).Stage==ReplacementControlledAttemptStage.CharacterSelection);
    Need(attempt.Observe(4,Obs(ReplacementClientPhase.ZoneHandshake)).Stage==ReplacementControlledAttemptStage.ZoneHandshake);
    Need(attempt.Observe(5,Obs(ReplacementClientPhase.ZonePermissions)).Stage==ReplacementControlledAttemptStage.ZonePermissions);
    Need(attempt.Observe(6,Obs(ReplacementClientPhase.Selected)).Stage==ReplacementControlledAttemptStage.SceneReadiness);
    Need(attempt.Observe(7,Obs(ReplacementClientPhase.Selected,true)).PhaseElapsedMilliseconds==1);
    Need(attempt.Observe(8,Obs(ReplacementClientPhase.Selected,true,true)).Stage==ReplacementControlledAttemptStage.PlayerPublication);
    Need(attempt.Observe(9,Obs(ReplacementClientPhase.Selected,true,true,true)).Stage==ReplacementControlledAttemptStage.Active);
    Need(attempt.State.StopReason==ReplacementControlledAttemptStopReason.None&&attempt.State.ElapsedMilliseconds==9&&attempt.Transitions.Count==9);
    Need(attempt.Observe(30009,Obs(ReplacementClientPhase.Selected,true,true,true)).StopReason==ReplacementControlledAttemptStopReason.PhaseTimeout);
}
foreach(int mode in Enumerable.Range(0,5))
{
    var attempt=new ReplacementControlledAttempt(30000,120000);attempt.Start(0);
    var observation=mode switch{1=>new ReplacementControlledAttemptObservation(ReplacementClientPhase.LoginHandshake,false,false,false,Faulted:true),2=>new(ReplacementClientPhase.LoginHandshake,false,false,false,Closing:true),3=>Obs(ReplacementClientPhase.Selected,false,true),4=>Obs(ReplacementClientPhase.Offline),_=>Obs(ReplacementClientPhase.LoginHandshake)};
    using var cancel=new CancellationTokenSource();if(mode==0)cancel.Cancel();
    var state=attempt.Observe(1,observation,cancel.Token);Need(state.StopReason==(mode==0?ReplacementControlledAttemptStopReason.Canceled:mode==1?ReplacementControlledAttemptStopReason.Faulted:mode==2?ReplacementControlledAttemptStopReason.Closing:ReplacementControlledAttemptStopReason.InvalidObservation));
    Need(!attempt.ToString().Contains("invalid")&&attempt.Transitions.Count==1);
}
{
    var attempt=new ReplacementControlledAttempt(30000,120000);attempt.Start(0);attempt.Observe(1,Obs(ReplacementClientPhase.LoginAuthentication));Need(attempt.Observe(2,Obs(ReplacementClientPhase.LoginHandshake)).StopReason==ReplacementControlledAttemptStopReason.InvalidObservation);
    var exact=new ReplacementControlledAttempt(30000,120000);exact.Start(0);Need(exact.Observe(30000,Obs(ReplacementClientPhase.LoginAuthentication)).StopReason==ReplacementControlledAttemptStopReason.PhaseTimeout);
}
Console.WriteLine(JsonSerializer.Serialize(new{Passed=true,Checks=checks,NegativeCases=negatives,InvalidInputs=invalidInputs,WrongCharacterPreserved=wrongCharacterPreserved,Auth=auth,Select=select,AttemptCases=outcomes,Synthetic=true,SocketCreated=false}));
}
catch(Exception error){Console.Error.WriteLine(error);Environment.ExitCode=1;}
sealed class FakePort:IDatagramPort
{
    public List<string> Sent=[];public int Disposals;private TaskCompletionSource<InboundDatagram>? receive;
    public ValueTask SendAsync(DatagramPeer p,ReadOnlyMemory<byte> bytes,CancellationToken t){Sent.Add(Convert.ToHexString(bytes.Span));return ValueTask.CompletedTask;}
    public ValueTask<InboundDatagram> ReceiveAsync(CancellationToken t){receive=new(TaskCreationOptions.RunContinuationsAsynchronously);var captured=receive;t.Register(()=>captured.TrySetCanceled(t));return new(captured.Task);}
    public void Deliver(DatagramPeer p,byte[] bytes){if(receive is null||!receive.TrySetResult(new(p,bytes)))throw new Exception("No receive");}
    public ValueTask DisposeAsync(){Disposals++;return ValueTask.CompletedTask;}
}
