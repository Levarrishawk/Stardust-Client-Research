using System.Buffers.Binary;
using System.Text.Json;
using Stardust.Core.Configuration;
using Stardust.Core.Hosting;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

try
{
if(args.Length!=1)throw new ArgumentException("Explicit synthetic fixture JSON required.");
using var doc=JsonDocument.Parse(File.ReadAllBytes(args[0]));var f=doc.RootElement;
byte[] Hex(string key)=>Convert.FromHexString(f.GetProperty(key).GetString()!);
const ulong Oid=0x1122334455667788;int checks=0,negatives=0;
void Need(bool value){checks++;if(!value)throw new Exception("Check "+checks);}
void Reject(Action action)
{try{action();}catch(Exception e)when(e is InvalidOperationException or InvalidDataException or ArgumentException or OverflowException){negatives++;return;}throw new Exception("Expected rejection");}
void Drain(ReplacementDatagramPump p,bool fault=false)
{try{p.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();Need(!fault);}catch(AggregateException e){Need(fault&&e.InnerExceptions.All(x=>x.Message=="IOException"));negatives++;}}
var options=new LoginClientOptions("login.invalid",44452,"20260817-SD:01");
var lp=new DatagramPeer(0x7f000001,44452);var zp=new DatagramPeer(0x7f000002,44453);
ReplacementClientSession Selected()
{
    var s=new ReplacementClientSession(options);s.StartLogin(0x12345678,0);s.AcceptIncoming(ReplacementConnection.Login,Hex("LoginResponse"),0);s.Authenticate("synthetic","synthetic",1);
    foreach(var wire in f.GetProperty("Login").EnumerateArray())s.AcceptIncoming(ReplacementConnection.Login,Convert.FromHexString(wire.GetString()!),2);
    s.BeginZone(Oid,0x87654321,3);s.AcceptIncoming(ReplacementConnection.Zone,Hex("ZoneResponse"),4);s.AcceptIncoming(ReplacementConnection.Zone,Hex("Permissions"),5);return s;
}
void Empty(IReadOnlyList<ReplacementClientExchange> rows)
{foreach(var e in rows){Need(e.Datagrams.Count==0&&e.ZoneApplications.Count==0&&e.FailureType is null);}}
void Step(ReplacementDatagramPump p,long now)=>Empty(p.Tick(now));
void Feed(ReplacementDatagramPump p,FakePort port,DatagramPeer peer,byte[] bytes,long now)
{port.Deliver(peer,bytes);Step(p,now);Need(bytes.All(b=>b==0));}
void Admit(ReplacementDatagramPump p,ReplacementClientSession s,FakePort login,FakePort zone)
{
    p.StartLogin(0x12345678,0);Step(p,0);Feed(p,login,lp,Hex("LoginResponse"),1);
    p.Authenticate("synthetic","synthetic",2);Step(p,2);
    foreach(var reply in f.GetProperty("Login").EnumerateArray())Feed(p,login,lp,Convert.FromHexString(reply.GetString()!),3);
    Need(s.Phase==ReplacementClientPhase.CharacterList);p.BeginZone(Oid,0x87654321,zone,zp,4);Drain(p);Step(p,4);
    Feed(p,zone,zp,Hex("ZoneResponse"),5);Step(p,5);
    Feed(p,zone,zp,Convert.FromHexString(f.GetProperty("ZoneAcks")[0].GetString()!),6);
    Feed(p,zone,zp,Hex("Permissions"),7);Step(p,7);
    Feed(p,zone,zp,Convert.FromHexString(f.GetProperty("ZoneAcks")[1].GetString()!),8);Step(p,8);
    Need(s.Phase==ReplacementClientPhase.Selected&&s.SelectionSubmissions==1&&login.Disposals==1&&p.FailureType is null);
}
var transportWires=new List<string>();var sessionWires=new List<string>();var pumpWires=new List<string>();var failures=new List<string>();
var receiveFailures=new List<string>();bool receiveFaultDrainObserved=false,cleanupFaultObserved=false,priorDeadlineObserved=false;
bool lateCleared=false,priorPreserved=false,unsentCleared=false;
foreach(var c in f.GetProperty("DisconnectCases").EnumerateArray())
{
    uint id=c.GetProperty("ConnectionId").GetUInt32();ushort reason=c.GetProperty("Reason").GetUInt16();
    var t=new SoeClientTransport();t.BeginSession(id,496);t.AcceptSessionResponse(Convert.FromHexString(c.GetProperty("Response").GetString()!));
    byte[] first=t.CreateReliableDatagram([1,2,3],0);t.QueueReliableMessage(new byte[100000],1);
    int retained=t.RetainedOutboundPackets,pending=t.PendingMessageCount;byte[] wire=t.CreateDisconnectDatagram(reason);transportWires.Add(Convert.ToHexString(wire));
    Need(t.State==SoeClientTransportState.Established&&retained==1&&t.RetainedOutboundPackets==retained&&pending==1&&t.PendingMessageCount==pending);
    Need(Convert.ToHexString(wire)==c.GetProperty("Wire").GetString());byte[] second=t.CreateReliableDatagram([4,5,6],1);
    byte[] decoded=SoeDatagramCodec.Decode(second,c.GetProperty("Seed").GetUInt32(),c.GetProperty("CrcWidth").GetInt32(),c.GetProperty("Maximum").GetInt32());Need(BinaryPrimitives.ReadUInt16BigEndian(decoded.AsSpan(2))==1);
    if(reason==6)Need(t.CreateDisconnectDatagram().SequenceEqual(wire));
    byte[] copy=(byte[])wire.Clone();Array.Clear(wire);byte[] again=t.CreateDisconnectDatagram(reason);Need(again.SequenceEqual(copy));t.Close();Need(again.SequenceEqual(copy)&&t.PendingMessageCount==0&&t.RetainedOutboundPackets==0);Reject(()=>t.CreateDisconnectDatagram());
}
{var t=new SoeClientTransport();Reject(()=>t.CreateDisconnectDatagram());t.BeginSession(1,496);Reject(()=>t.CreateDisconnectDatagram());Need(t.State==SoeClientTransportState.SessionRequested);}
foreach(var c in f.GetProperty("DisconnectCases").EnumerateArray().Where(c=>c.GetProperty("ConnectionId").GetUInt32()==0x87654321))
using(var s=Selected())
{
    byte[] app=new byte[100000];Hex("SentinelHex").CopyTo(app,0);s.QueueZoneApplication(app,6);Need(s.ZonePendingMessages==1&&s.ZoneRetainedPackets>0);
    var closed=s.CloseWithDisconnect(7,c.GetProperty("Reason").GetUInt16());Need(closed.Datagrams.Count==1&&closed.Datagrams[0].Connection==ReplacementConnection.Zone&&closed.ZoneApplications.Count==0);
    sessionWires.Add(Convert.ToHexString(closed.Datagrams[0].Bytes));Need(s.Phase==ReplacementClientPhase.Closed&&s.CredentialLength==0&&s.SelectedObjectId==0&&s.ZonePendingMessages==0&&s.ZoneRetainedPackets==0);
    s.Dispose();Need(Convert.ToHexString(closed.Datagrams[0].Bytes)==c.GetProperty("Wire").GetString());Reject(()=>s.CloseWithDisconnect(8));Need(closed.Datagrams.Count==1);
}
using(var s=new ReplacementClientSession(options)){Reject(()=>s.CloseWithDisconnect(0));Need(s.Phase==ReplacementClientPhase.Faulted&&s.FailureType==nameof(InvalidOperationException));s.Dispose();s.Dispose();}
using(var s=Selected())
{
    Reject(()=>s.CloseWithDisconnect(4));Need(s.Phase==ReplacementClientPhase.Selected);Exception? error=null;
    var thread=new Thread(()=>{try{s.CloseWithDisconnect(6);}catch(Exception e){error=e;}});thread.Start();thread.Join();Need(error is InvalidOperationException&&s.Phase==ReplacementClientPhase.Selected);negatives++;
}
// Actual pending prior send, queued unsent wire, completed application receive and late retirement receive.
using(var s=new ReplacementClientSession(options))
{
    var login=new FakePort();var zone=new FakePort();using var p=new ReplacementDatagramPump(s,login,lp);Admit(p,s,login,zone);
    zone.BlockSend=true;zone.IgnoreCancellation=true;byte[] prior=[1,2,3],unsent=[4,5,6];p.Submit(new([new(ReplacementConnection.Zone,prior)],[]));Step(p,9);
    var priorTask=zone.SendCompletion;p.Submit(new([new(ReplacementConnection.Zone,unsent)],[]));int before=zone.Sent.Count;
    p.BeginDisconnect(10);Need(p.DisconnectRequested&&!p.DisconnectSubmitted&&!p.DisconnectSendCompleted&&s.Phase==ReplacementClientPhase.Closed);
    priorPreserved=prior.Any(b=>b!=0);unsentCleared=unsent.All(b=>b==0);Need(priorPreserved&&unsentCleared&&zone.Disposals==0);
    Reject(()=>p.BeginDisconnect(11));Need(p.DisconnectRequested&&p.FailureType is null&&zone.Sent.Count==before);
    byte[] forbidden=[7,8];Reject(()=>p.Submit(new([new(ReplacementConnection.Zone,forbidden)],[])));Need(forbidden.All(b=>b==0)&&p.FailureType is null);
    Reject(()=>p.Authenticate("x","x",11));Need(p.FailureType is null);
    Step(p,11);Need(zone.Sent.Count==before&&prior.Any(b=>b!=0));
    priorTask.TrySetResult();Step(p,12);Need(prior.All(b=>b==0)&&p.DisconnectSubmitted&&!p.DisconnectSendCompleted&&zone.Sent.Count==before+1);
    pumpWires.Add(zone.Sent.Last().Hex);Need(zone.Sent.Last().Peer==zp);var finalBorrow=zone.Borrowed;
    byte[] late=Hex("PostSelection");zone.SendCompletion.TrySetResult();Step(p,13);Need(p.DisconnectSendCompleted&&p.FailureType is null&&zone.Disposals==1&&finalBorrow.Span.ContainsAnyExcept((byte)0)==false&&!p.DrainCompletion.IsCompleted);
    zone.Deliver(zp,late);Drain(p);lateCleared=late.All(b=>b==0);Need(lateCleared&&s.Phase==ReplacementClientPhase.Closed);p.Dispose();p.Dispose();Reject(()=>p.Tick(14));
}
// A completed receive is cleared during disconnect without application dispatch or retry generation.
using(var s=new ReplacementClientSession(options))
{
    var login=new FakePort();var zone=new FakePort();using var p=new ReplacementDatagramPump(s,login,lp);Admit(p,s,login,zone);
    byte[] incoming=Hex("PostSelection");zone.Deliver(zp,incoming);int before=zone.Sent.Count;
    zone.OnSend=()=>Reject(()=>p.BeginDisconnect(9));p.BeginDisconnect(9);Step(p,9);
    Need(incoming.All(b=>b==0)&&zone.Sent.Count==before+1&&s.Phase==ReplacementClientPhase.Closed&&p.FailureType is null);Step(p,10);Need(p.DisconnectSendCompleted);Drain(p);
}
// Exact timeout preflight and wrong phase/thread reject before closing the borrowed session.
using(var s=new ReplacementClientSession(options))
{
    var login=new FakePort();var zone=new FakePort();using var p=new ReplacementDatagramPump(s,login,lp);
    Reject(()=>p.BeginDisconnect(0));Need(s.Phase==ReplacementClientPhase.Offline&&p.FailureType is null&&!p.DisconnectRequested);
    Admit(p,s,login,zone);
    foreach(long timeout in new long[]{0,-1,30001})Reject(()=>p.BeginDisconnect(9,timeout));
    Reject(()=>p.BeginDisconnect(7));Reject(()=>p.BeginDisconnect(long.MaxValue,3000));
    Need(s.Phase==ReplacementClientPhase.Selected&&p.FailureType is null&&!p.DisconnectRequested);
    Exception? error=null;var thread=new Thread(()=>{try{p.BeginDisconnect(9);}catch(Exception e){error=e;}});thread.Start();thread.Join();Need(error is InvalidOperationException);negatives++;
    p.Dispose();Drain(p);Need(s.Phase==ReplacementClientPhase.Selected);Reject(()=>p.BeginDisconnect(9));
}
// Final synchronous/async failure, explicit canceled send and deadline while it remains borrowed.
using(var s=new ReplacementClientSession(options))
{
    var login=new FakePort{IgnoreCancellation=true};var zone=new FakePort();using var p=new ReplacementDatagramPump(s,login,lp);
    p.StartLogin(0x12345678,0);Step(p,0);Feed(p,login,lp,Hex("LoginResponse"),1);p.Authenticate("synthetic","synthetic",2);Step(p,2);
    foreach(var reply in f.GetProperty("Login").EnumerateArray())Feed(p,login,lp,Convert.FromHexString(reply.GetString()!),3);
    p.BeginZone(Oid,0x87654321,zone,zp,4);
    s.AcceptIncoming(ReplacementConnection.Zone,Hex("ZoneResponse"),5);s.AcceptIncoming(ReplacementConnection.Zone,Hex("Permissions"),6);
    Reject(()=>p.BeginDisconnect(7));Need(s.Phase==ReplacementClientPhase.Selected&&!p.DisconnectRequested&&zone.Sent.Count==0);
    byte[] late=[1];login.Deliver(lp,late);Drain(p);Need(late.All(b=>b==0));p.BeginDisconnect(8,30000,0xfedc);Step(p,8);Step(p,9);Drain(p);Need(p.DisconnectSendCompleted&&zone.Sent.Count==1);
}
foreach(string mode in new[]{"sync","async","cancel","deadline","prior-failure","prior-cancel"})
using(var s=new ReplacementClientSession(options))
{
    var login=new FakePort();var zone=new FakePort();using var p=new ReplacementDatagramPump(s,login,lp);Admit(p,s,login,zone);
    if(mode.StartsWith("prior"))
    {zone.BlockSend=true;p.Submit(new([new(ReplacementConnection.Zone,new byte[]{1,2})],[]));Step(p,9);}
    else {zone.ThrowSend=mode=="sync";zone.FailSend=mode=="async";zone.BlockSend=mode is "cancel" or "deadline";}
    p.BeginDisconnect(10,5);var rows=p.Tick(10);
    if(mode is "cancel" or "prior-cancel")zone.SendCompletion.TrySetCanceled();
    if(mode=="prior-failure")zone.SendCompletion.TrySetException(new IOException("synthetic"));
    if(mode=="deadline")rows=p.Tick(15);else if(mode!="sync")rows=p.Tick(11);
    Need(p.FailureType is not null&&rows.Any(e=>e.FailureType==p.FailureType)&&!p.DisconnectSendCompleted&&zone.Disposals==1&&s.Phase==ReplacementClientPhase.Closed);
    Need(p.DisconnectSubmitted==!mode.StartsWith("prior"));failures.Add(p.FailureType!);Drain(p,mode is "async" or "prior-failure");Need(!zone.Borrowed.Span.ContainsAnyExcept((byte)0));
}
// Deadline before the final send preserves an uncooperative prior borrower until it actually completes.
using(var s=new ReplacementClientSession(options))
{
    var login=new FakePort();var zone=new FakePort();using var p=new ReplacementDatagramPump(s,login,lp);Admit(p,s,login,zone);
    zone.BlockSend=true;zone.IgnoreCancellation=true;byte[] prior=[1,2,3];p.Submit(new([new(ReplacementConnection.Zone,prior)],[]));Step(p,9);int count=zone.Sent.Count;
    p.BeginDisconnect(10,5);var rows=p.Tick(15);Need(rows.Any(e=>e.FailureType==nameof(TimeoutException))&&!p.DisconnectSubmitted&&!p.DisconnectSendCompleted&&zone.Sent.Count==count&&prior.Any(b=>b!=0)&&!p.DrainCompletion.IsCompleted);
    zone.SendCompletion.TrySetResult();byte[] late=[1];zone.Deliver(zp,late);Drain(p);priorDeadlineObserved=prior.All(b=>b==0)&&late.All(b=>b==0);Need(priorDeadlineObserved);
}
// Abort preserves pending final buffer until actual completion, never claims send success.
foreach(bool cancel in new[]{false,true})using(var s=new ReplacementClientSession(options))
{
    var login=new FakePort();var zone=new FakePort();using var p=new ReplacementDatagramPump(s,login,lp);Admit(p,s,login,zone);
    zone.BlockSend=true;p.BeginDisconnect(9);Step(p,9);zone.EndReceive(cancel);var rows=p.Tick(10);
    Need(rows.Any(e=>e.FailureType is not null)&&p.DisconnectSubmitted&&!p.DisconnectSendCompleted&&zone.Disposals==1&&s.Phase==ReplacementClientPhase.Closed);
    receiveFailures.Add(p.FailureType!);Drain(p,!cancel);Need(!zone.Borrowed.Span.ContainsAnyExcept((byte)0));if(!cancel)receiveFaultDrainObserved=p.DrainCompletion.IsFaulted;
}
// A new disposal failure is a drain failure after successful send, distinct from a reobserved I/O task.
using(var s=new ReplacementClientSession(options))
{
    var login=new FakePort();var zone=new FakePort();using var p=new ReplacementDatagramPump(s,login,lp);Admit(p,s,login,zone);
    zone.FailDispose=true;p.BeginDisconnect(9);Step(p,9);Step(p,10);Need(p.DisconnectSendCompleted&&p.FailureType is null&&zone.Disposals==1);
    Drain(p,true);cleanupFaultObserved=p.DrainCompletion.IsFaulted;Need(cleanupFaultObserved&&!zone.Borrowed.Span.ContainsAnyExcept((byte)0));
}
using(var s=new ReplacementClientSession(options))
{
    var login=new FakePort();var zone=new FakePort();using var p=new ReplacementDatagramPump(s,login,lp);Admit(p,s,login,zone);
    zone.BlockSend=true;zone.IgnoreCancellation=true;p.BeginDisconnect(9);Step(p,9);var borrowed=zone.Borrowed;p.Dispose();Need(!p.DisconnectSendCompleted&&borrowed.Span.ContainsAnyExcept((byte)0)&&!p.DrainCompletion.IsCompleted);
    zone.SendCompletion.TrySetResult();byte[] late=[1,2];zone.Deliver(zp,late);Drain(p);Need(!borrowed.Span.ContainsAnyExcept((byte)0)&&late.All(b=>b==0)&&!p.DisconnectSendCompleted);p.Dispose();
}
Console.WriteLine(JsonSerializer.Serialize(new{Passed=true,Checks=checks,NegativeCases=negatives,TransportWires=transportWires,SessionWires=sessionWires,PumpDisconnectWires=pumpWires,SuccessSends=1,Failures=failures,ReceiveFailures=receiveFailures,ReceiveFaultDrainObserved=receiveFaultDrainObserved,CleanupFaultObserved=cleanupFaultObserved,PriorDeadlineObserved=priorDeadlineObserved,LateReceiveCleared=lateCleared,PriorSendPreserved=priorPreserved,UnsentCleared=unsentCleared,NoServerReleaseClaim=true,SocketCreated=false}));
}
catch(Exception error){Console.Error.WriteLine(error);Environment.ExitCode=1;}

sealed class FakePort:IDatagramPort
{
    public readonly List<(DatagramPeer Peer,string Hex)> Sent=[];
    public int Disposals;public bool IgnoreCancellation,BlockSend,FailSend,ThrowSend,FailDispose;
    public Action? OnSend;
    public ReadOnlyMemory<byte> Borrowed;
    public TaskCompletionSource SendCompletion=new(TaskCreationOptions.RunContinuationsAsynchronously);
    private TaskCompletionSource<InboundDatagram>? receive;
    public ValueTask SendAsync(DatagramPeer destination,ReadOnlyMemory<byte> bytes,CancellationToken token)
    {
        Borrowed=bytes;Sent.Add((destination,Convert.ToHexString(bytes.Span)));OnSend?.Invoke();
        if(ThrowSend)throw new IOException("Synthetic send failure");
        if(FailSend)return new(Task.FromException(new IOException("Synthetic send failure")));
        if(!BlockSend)return ValueTask.CompletedTask;
        SendCompletion=new(TaskCreationOptions.RunContinuationsAsynchronously);var captured=SendCompletion;
        token.Register(()=>{if(!IgnoreCancellation)captured.TrySetCanceled(token);});return new(captured.Task);
    }
    public ValueTask<InboundDatagram> ReceiveAsync(CancellationToken token)
    {
        receive=new(TaskCreationOptions.RunContinuationsAsynchronously);var captured=receive;
        token.Register(()=>{if(!IgnoreCancellation)captured.TrySetCanceled(token);});return new(captured.Task);
    }
    public void Deliver(DatagramPeer peer,byte[] bytes){if(receive is null||!receive.TrySetResult(new(peer,bytes)))throw new Exception("No outstanding receive");}
    public void EndReceive(bool cancel){if(cancel)receive!.TrySetCanceled();else receive!.TrySetException(new IOException("synthetic receive"));}
    public ValueTask DisposeAsync(){Disposals++;return FailDispose?new(Task.FromException(new IOException("synthetic disposal"))):ValueTask.CompletedTask;}
}
