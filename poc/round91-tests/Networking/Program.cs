using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Configuration;
using Stardust.Core.Hosting;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

try
{
    using var document=JsonDocument.Parse(File.ReadAllBytes(args[0]));var f=document.RootElement;
    var profile=ReplacementLaunchProfileCodec.Decode(Encoding.UTF8.GetBytes(f.GetProperty("Profile").GetRawText()));
    var lp=new DatagramPeer(0x7F000001,44452);var zp=new DatagramPeer(0x7F000002,44453);
    var options=new LoginClientOptions("login.invalid",44452,"20260817-SD:01");
    var rows=new List<object>();int checks=0;
    void Need(bool v,string label){checks++;if(!v)throw new Exception(label);}
    byte[] Bytes(string key)=>Convert.FromHexString(f.GetProperty(key).GetString()!);
    bool Redacted(string text)=>!text.Contains(".invalid",StringComparison.OrdinalIgnoreCase)&&!text.Contains("127.0.0.",StringComparison.Ordinal)&&!text.Contains("synthetic",StringComparison.OrdinalIgnoreCase);
    foreach(var c in f.GetProperty("Cases").EnumerateArray())
    {
        string name=c.GetProperty("Name").GetString()!;bool expected=c.GetProperty("ExpectedAccepted").GetBoolean();
        var login=new FakePort();var zone=new FakePort();ReplacementClientApplication? app=null;
        int loginFactories=0,zoneFactories=0;bool admitted=false,preserved=false,preflightObserved=false;string? failure=null;
        string stage="mapping";object? sources=null;
        uint U(string key)=>c.GetProperty(key).GetUInt32();ushort Port(string key)=>c.GetProperty(key).GetUInt16();string S(string key)=>c.GetProperty(key).GetString()!;
        void Tick(long now)
        {
            var tick=app!.Tick(now);foreach(var message in tick.UnhandledApplications)Array.Clear(message);
            if(tick.FailureType is not null)throw new IOException("Peer admission failed");
        }
        void Feed(FakePort port,DatagramPeer peer,byte[] bytes,long now)
        {
            port.Deliver(peer,bytes);try{Tick(now);}finally{Need(bytes.All(b=>b==0),name+" inbound wipe");}
        }
        try
        {
            var approval=new ReplacementEndpointApproval(new(S("ApprovedLoginHost"),U("ApprovedLoginAddress"),Port("ApprovedLoginPort")),
                new(S("ApprovedZoneHost"),U("ApprovedZoneAddress"),Port("ApprovedZonePort")),U("ApprovedGalaxy"));
            Need(Redacted(approval.ToString()!)&&Redacted(approval.Login.ToString()!)&&Redacted(approval.Zone.ToString()!),name+" policy redaction");
            stage="startup";
            app=new(profile,options,lp,zp,(connection,peer)=>
            {
                if(connection==ReplacementConnection.Login){loginFactories++;Need(peer==lp,name+" login peer");return login;}
                zoneFactories++;Need(peer==zp,name+" zone peer");
                Need(app!.Session.Phase==ReplacementClientPhase.CharacterList&&app.Session.CredentialLength==5&&app.Session.SelectedObjectId==0&&app.Session.SelectionSubmissions==0&&app.Session.ZoneEndpoint is null,name+" factory before credential transfer");
                return zone;
            },(scene,terrain,lookup,token)=>throw new Exception("Resources must not start"),new(1000,10),endpointApproval:approval);
            sources=app.Creature.Sources;stage="login";
            app.StartLogin(0x12345678,0);Tick(0);Feed(login,lp,Bytes("LoginResponse"),1);app.Authenticate("synthetic","synthetic",2);Tick(2);
            foreach(var reply in c.GetProperty("LoginReplies").EnumerateArray())Feed(login,lp,Convert.FromHexString(reply.GetString()!),3);
            Need(app.Session.Phase==ReplacementClientPhase.CharacterList,name+" received list");
            stage="preflight";
            var session=app.Session;int credential=session.CredentialLength;
            try
            {
                var selected=session.PreflightZone(0x1122334455667788,0x87654321);
                Need(selected.ObjectId==0x1122334455667788&&selected.GalaxyId==U("CharacterGalaxy"),name+" selection observation");
                Need(Redacted(selected.ToString()!),name+" selection redaction");preflightObserved=true;
            }
            catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException or IOException)
            {Need(!expected,name+" expected grammar admission");}
            Need(session.Phase==ReplacementClientPhase.CharacterList&&session.CredentialLength==credential&&session.SelectedObjectId==0&&session.ZoneEndpoint is null&&session.SelectionSubmissions==0&&zoneFactories==0&&login.Disposals==0,name+" preflight nonmutation");
            try{app.SelectCharacter(0x1122334455667788,0x87654321,4);}
            catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException or IOException)
            {
                failure=error.GetType().Name;Need(Redacted(error.Message),name+" rejection redaction");
                preserved=!app.Closing&&app.FailureType is null&&session.Phase==ReplacementClientPhase.CharacterList&&session.CredentialLength==credential&&session.SelectedObjectId==0&&session.ZoneEndpoint is null&&session.SelectionSubmissions==0&&zoneFactories==0&&login.Disposals==0&&zone.Sent.Count==0;
                Need(preserved,name+" mapping rejection preserves handoff");
                // A rejected attempted handoff at4 must not advance session clock past3.
                var poll=session.Poll(3);Need(poll.FailureType is null&&poll.Datagrams.Count==0,name+" preflight clock preserved");
                throw;
            }
            stage="zone";app.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();Tick(4);
            Need(zone.Sent.Single()==f.GetProperty("ExpectedZoneRequest").GetString(),name+" exact zone request");
            Feed(zone,zp,Bytes("ZoneResponse"),5);Tick(5);
            Need(app.Session.CredentialLength==0,name+" credential retired after zone response");
            Feed(zone,zp,Convert.FromHexString(f.GetProperty("ZoneAcks")[0].GetString()!),6);
            Feed(zone,zp,Bytes("Permissions"),7);Tick(7);
            Feed(zone,zp,Convert.FromHexString(f.GetProperty("ZoneAcks")[1].GetString()!),8);
            Need(app.Session.Phase==ReplacementClientPhase.Selected&&app.Session.SelectionSubmissions==1,name+" one selection");
            admitted=true;
        }
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or InvalidDataException or IOException or ReplacementClientApplication.StartupFailure)
        {
            failure=error.GetType().Name;Need(!expected,name+" unexpected failure at "+stage);Need(Redacted(error.Message),name+" failure redaction");
            if(error is ReplacementClientApplication.StartupFailure startup)startup.RollbackCompletion.GetAwaiter().GetResult();
        }
        Need(admitted==expected,name+" admission expectation");
        if(!admitted)Need(zoneFactories==0&&zone.Sent.Count==0&&zone.Disposals==0,name+" no acquired zone or credential wire");
        if(stage is "mapping" or "startup")Need(loginFactories==0,name+" mapping before login factory");
        if(app is not null)
        {
            app.BeginClose(Task.CompletedTask);app.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
            Need(app.TryFinalizeClose(Task.CompletedTask)&&app.IsFinalized&&app.CleanupFailures.Count==0,name+" deterministic drain");app.Dispose();app.Dispose();
            Need(login.Disposals==1&&zone.Disposals==(admitted?1:0),name+" exact owned disposal");
        }
        using(var file=new FileStream(Path.Combine(profile.AssetRoot,"data.tre"),FileMode.Open,FileAccess.ReadWrite,FileShare.None))checks++;
        rows.Add(new{Name=name,Accepted=admitted,Stage=stage,FailureType=failure,LoginFactories=loginFactories,ZoneFactories=zoneFactories,
            Preserved=preserved,PreflightObserved=preflightObserved,Sources=sources,ZoneWire=zone.Sent,LoginDisposals=login.Disposals,ZoneDisposals=zone.Disposals});
    }
    Console.WriteLine(JsonSerializer.Serialize(new{Passed=true,Checks=checks,Rows=rows,Synthetic=true,SocketCreated=false,DnsResolved=false}));
}
catch(Exception error){Console.Error.WriteLine(error);Environment.ExitCode=1;}

sealed class FakePort:IDatagramPort
{
    public List<string> Sent=[];public int Disposals;private TaskCompletionSource<InboundDatagram>? receive;
    public ValueTask SendAsync(DatagramPeer peer,ReadOnlyMemory<byte> bytes,CancellationToken token){Sent.Add(Convert.ToHexString(bytes.Span));return ValueTask.CompletedTask;}
    public ValueTask<InboundDatagram> ReceiveAsync(CancellationToken token){receive=new(TaskCreationOptions.RunContinuationsAsynchronously);var captured=receive;token.Register(()=>captured.TrySetCanceled(token));return new(captured.Task);}
    public void Deliver(DatagramPeer peer,byte[] bytes){if(receive is null||!receive.TrySetResult(new(peer,bytes)))throw new Exception("No receive");}
    public ValueTask DisposeAsync(){Disposals++;return ValueTask.CompletedTask;}
}
