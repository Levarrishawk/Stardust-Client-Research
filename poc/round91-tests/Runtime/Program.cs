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
const ulong Oid=0x1122334455667788;
foreach(var host in new[]{"", "host.","-host","host-","a..b","https://host","host/path","a_b","::1","127.1","0x7f000001","127.00.0.1","999.0.0.1","host bad","�.invalid"})Reject(()=>new ReplacementApprovedEndpoint(host,lp.Address,lp.Port));
Reject(()=>new ReplacementApprovedEndpoint("127.0.0.2",lp.Address,lp.Port));Reject(()=>new ReplacementApprovedEndpoint("login.invalid",0,lp.Port));Reject(()=>new ReplacementApprovedEndpoint("login.invalid",lp.Address,0));
var loginMapping=new ReplacementApprovedEndpoint("LOGIN.INVALID",lp.Address,lp.Port);
Need(loginMapping.Hostname=="login.invalid"&&!loginMapping.ToString().Contains("invalid"));
foreach(int bad in Enumerable.Range(0,4))
{
    int factories=0;var approval=new ReplacementEndpointApproval(new("other.invalid",lp.Address,lp.Port),new("zone.invalid",zp.Address,zp.Port),7);
    var wrongOptions=bad==1?new LoginClientOptions("LOGIN.INVALID",44451,"20260817-SD:01"):options;
    if(bad!=0)approval=new(new("login.invalid",bad==2?0x7f000002:lp.Address,bad==1?(ushort)44451:lp.Port),new("zone.invalid",zp.Address,zp.Port),7);
    var wrongPeer=bad==3?new DatagramPeer(lp.Address,44451):lp;
    Reject(()=>new ReplacementClientApplication(profile,wrongOptions,wrongPeer,zp,(c,p)=>{factories++;return new FakePort();},(s,t,l,k)=>Task.CompletedTask,new(1000,10),endpointApproval:approval));
    Need(factories==0);Exclusive();
}
var results=new List<object>();
foreach(var test in f.GetProperty("Cases").EnumerateArray())
{
    string name=test.GetProperty("Name").GetString()!;bool good=test.GetProperty("Accepted").GetBoolean(),receiveFault=test.GetProperty("ReceiveFault").GetBoolean();
    var login=new FakePort();var zone=new FakePort();int zoneFactories=0;
    var pinnedZone=new DatagramPeer(test.GetProperty("FixedAddress").GetUInt32(),test.GetProperty("FixedPort").GetUInt16());
    var approval=new ReplacementEndpointApproval(loginMapping,new(test.GetProperty("ApprovedHost").GetString()!,zp.Address,zp.Port),7);
    Need(!approval.ToString().Contains("invalid")&&!approval.Zone.ToString().Contains("127"));
    var app=new ReplacementClientApplication(profile,options,lp,pinnedZone,(c,p)=>{if(c==ReplacementConnection.Zone){zoneFactories++;return zone;}return login;},(s,t,l,k)=>Task.CompletedTask,new(1000,10),endpointApproval:approval);
    app.StartLogin(0x12345678,0);Step(app,0);Feed(app,login,lp,Hex("LoginResponse"),1);app.Authenticate("synthetic","synthetic",2);Step(app,2);
    bool faulted=false;
    foreach(var reply in test.GetProperty("LoginReplies").EnumerateArray())
    {
        var bytes=Convert.FromHexString(reply.GetString()!);login.Deliver(lp,bytes);var tick=app.Tick(3);Need(bytes.All(b=>b==0));
        foreach(var unhandled in tick.UnhandledApplications)Array.Clear(unhandled);
        if(tick.FailureType is not null){faulted=true;break;}
    }
    if(faulted!=receiveFault)throw new Exception($"Case {name} receiveFault={faulted} expected={receiveFault} type={app.FailureType}");Need(faulted==receiveFault);string auth="",select="";
    if(receiveFault)
    {Need(app.Closing&&zoneFactories==0&&zone.Sent.Count==0&&app.Session.CredentialLength==0&&app.Session.SelectionSubmissions==0);}
    else
    {
        var session=app.Session;Need(session.Phase==ReplacementClientPhase.CharacterList);int credential=session.CredentialLength;Need(credential==5);
        if(good)
        {
            var observed=session.PreflightZone(Oid,0x87654321);Need(observed.GalaxyId==7&&observed.ObjectId==Oid&&observed.Endpoint.Port==44453);
            Need(!observed.ToString().Contains("invalid")&&session.Phase==ReplacementClientPhase.CharacterList&&login.Disposals==0&&session.SelectedObjectId==0);
            Task.Run(()=>Reject(()=>session.PreflightZone(Oid,0x87654321))).GetAwaiter().GetResult();
            app.SelectCharacter(Oid,0x87654321,4);Need(zoneFactories==1);app.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();Step(app,4);
            Feed(app,zone,pinnedZone,Hex("ZoneResponse"),5);Step(app,5);Feed(app,zone,pinnedZone,Convert.FromHexString(peer.GetProperty("ZoneAcks")[0].GetString()!),6);
            Feed(app,zone,pinnedZone,Hex("Permissions"),7);Step(app,7);Feed(app,zone,pinnedZone,Convert.FromHexString(peer.GetProperty("ZoneAcks")[1].GetString()!),8);
            var wires=zone.Sent.Where(x=>x.StartsWith("0009")).ToArray();Need(wires.Length==2&&app.Session.SelectedObjectId==Oid&&app.Session.SelectionSubmissions==1);
            auth=wires[0];select=wires[1];
        }
        else
        {
            Reject(()=>app.SelectCharacter(Oid,0x87654321,1000));
            Need(zoneFactories==0&&zone.Sent.Count==0&&login.Disposals==0&&!app.Closing&&app.FailureType is null);
            Need(session.Phase==ReplacementClientPhase.CharacterList&&session.CredentialLength==credential&&session.SelectedObjectId==0&&session.SelectionSubmissions==0&&session.ZoneEndpoint is null&&app.Run.Generation==0);
            // A rejected future selection must not consume either run/session clock.
            Step(app,3);Need(session.CredentialLength==credential);
        }
    }
    results.Add(new{Name=name,Accepted=good,ReceiveFault=receiveFault,Auth=auth,Select=select,ZoneFactories=zoneFactories,ZoneWires=zone.Sent.Count});
    app.BeginClose(Task.CompletedTask);app.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();Need(app.TryFinalizeClose(Task.CompletedTask)&&app.CleanupFailures.Count==0);Exclusive();app.Dispose();
}
Console.WriteLine(JsonSerializer.Serialize(new{Passed=true,Checks=checks,NegativeCases=negatives,Cases=results,Synthetic=true,SocketCreated=false}));
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
