using System.Text.Json;
using Stardust.Core.Configuration;
using Stardust.Core.Hosting;
using Stardust.Core.Session;

using var doc = JsonDocument.Parse(File.ReadAllBytes(args[0])); var f=doc.RootElement;
byte[] Hex(string key)=>Convert.FromHexString(f.GetProperty(key).GetString()!);
int checks=0, negatives=0;
void Need(bool value){checks++;if(!value)throw new Exception("Check "+checks);}
void Reject(Action action){try{action();}catch(Exception e)when(e is InvalidOperationException or InvalidDataException or ArgumentException or ObjectDisposedException){negatives++;return;}throw new Exception("Expected rejection");}
var lp=new DatagramPeer(0x7F000001,44452);var zp=new DatagramPeer(0x7F000002,44453);
var options=new LoginClientOptions("login.invalid",44452,"20260817-SD:01",496,TimeSpan.FromMilliseconds(50),TimeSpan.FromMilliseconds(500),TimeSpan.FromMilliseconds(50),TimeSpan.FromMilliseconds(500));
ReplacementClientSession Session()=>new(options);
void Clear(IReadOnlyList<ReplacementClientExchange> rows){foreach(var e in rows){Need(e.Datagrams.Count==0);foreach(var a in e.ZoneApplications)Array.Clear(a);}}
void Step(ReplacementDatagramPump pump,long now)=>Clear(pump.Tick(now));
void Feed(ReplacementDatagramPump pump,FakePort port,DatagramPeer peer,byte[] bytes,long now){port.Deliver(peer,bytes);Step(pump,now);Need(bytes.All(b=>b==0));}
void Login(ReplacementDatagramPump pump,ReplacementClientSession s,FakePort port)
{
 pump.StartLogin(0x12345678,0);Step(pump,0);Feed(pump,port,lp,Hex("LoginResponse"),1);Need(s.Phase==ReplacementClientPhase.LoginAuthentication);
 pump.Authenticate("synthetic","opaque-test-password",2);Step(pump,2);
 foreach(var reply in f.GetProperty("LoginReplies").EnumerateArray())Feed(pump,port,lp,Convert.FromHexString(reply.GetString()!),3);
 Need(s.Phase==ReplacementClientPhase.CharacterList);
}
// Successful real83 login/session path; deferred late-login receive prevents zone traffic.
using(var s=Session())
{
 var login=new FakePort();var zone=new FakePort();using var pump=new ReplacementDatagramPump(s,login,lp);Login(pump,s,login);
 login.IgnoreCancellation=true; // Existing receive registration checks this when retirement cancels.
 pump.BeginZone(f.GetProperty("ObjectId").GetUInt64(),0x87654321,zone,zp,4);
 Need(login.Disposals==1 && zone.Sent.Count==0 && !pump.DrainCompletion.IsCompleted);
 Step(pump,4);Need(zone.Sent.Count==0);
 byte[] late=Hex("LoginResponse");login.Deliver(lp,late);
 pump.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();Need(late.All(b=>b==0));Step(pump,5);Need(zone.Sent.Count==1&&s.Phase==ReplacementClientPhase.ZoneHandshake);
 Feed(pump,zone,zp,Hex("ZoneResponse"),6);Need(s.Phase==ReplacementClientPhase.ZonePermissions);Step(pump,6);
 string auth=zone.Sent.Last().Hex;Need(auth==f.GetProperty("ExpectedAuthWire").GetString());
 Feed(pump,zone,zp,Convert.FromHexString(f.GetProperty("ZoneAcks")[0].GetString()!),7);
 Feed(pump,zone,zp,Hex("Permissions"),8);Step(pump,8);Need(s.Phase==ReplacementClientPhase.Selected&&s.SelectionSubmissions==1);
 string select=zone.Sent.Single(v=>v.Hex==f.GetProperty("ExpectedSelectWire").GetString()).Hex;
 Feed(pump,zone,zp,Convert.FromHexString(f.GetProperty("ZoneAcks")[1].GetString()!),9);
 // Deliver earlier ordered world payloads then verify owned application transfer and empty wire lists.
 int apps=0;foreach(var wire in f.GetProperty("WorldFrames").EnumerateArray())
 {byte[] owned=Convert.FromHexString(wire.GetString()!);zone.Deliver(zp,owned);var rows=pump.Tick(10);foreach(var e in rows){Need(e.Datagrams.Count==0);apps+=e.ZoneApplications.Count;foreach(var a in e.ZoneApplications)Array.Clear(a);}Need(owned.All(b=>b==0));}
 Need(apps==5);Step(pump,10);
 // frozen83 has no queue-application API: exact independently framed ready is injected through Submit.
 byte[] ready=Hex("ExpectedReadyWire");pump.Submit(new([new(ReplacementConnection.Zone,ready)],[]));Step(pump,11);Step(pump,11);
 Need(ready.All(b=>b==0));string readyWire=zone.Sent.Last(v=>v.Hex==f.GetProperty("ExpectedReadyWire").GetString()).Hex;
 File.WriteAllText(args[1],JsonSerializer.Serialize(new{AuthWire=auth,SelectWire=select,ReadyWire=readyWire}));
 pump.Dispose();pump.Dispose();pump.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();Need(zone.Disposals==1&&login.Disposals==1&&s.Phase==ReplacementClientPhase.Selected);
}
// Foreign address OR port and genuine asynchronous completion cannot mutate until Tick.
using(var s=Session())
{
 var port=new FakePort();using var pump=new ReplacementDatagramPump(s,port,lp);pump.StartLogin(1,0);Step(pump,0);
 foreach(var peer in new[]{new DatagramPeer(lp.Address+1,lp.Port),new DatagramPeer(lp.Address,(ushort)(lp.Port+1))})
 {byte[] owned=Hex("LoginResponse");Task.Run(()=>port.Deliver(peer,owned)).GetAwaiter().GetResult();Need(s.Phase==ReplacementClientPhase.LoginHandshake);Step(pump,1);Need(owned.All(b=>b==0)&&s.Phase==ReplacementClientPhase.LoginHandshake);}
 Reject(()=>pump.Tick(-1));Reject(()=>pump.Tick(0));
 Exception? wrong=null;var thread=new Thread(()=>{try{pump.Tick(1);}catch(Exception e){wrong=e;}});thread.Start();thread.Join();Need(wrong is InvalidOperationException);
 port.OnSend=()=>Reject(()=>pump.Tick(50));Step(pump,50);Need(s.Phase==ReplacementClientPhase.LoginHandshake);
 pump.Dispose();pump.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();Reject(()=>pump.Tick(51));
}
// Serialized sends hold borrowed bytes until actual completion, then clear; cancel/late disposal too.
using(var s=Session())
{
 var port=new FakePort{BlockSend=true};using var pump=new ReplacementDatagramPump(s,port,lp);pump.StartLogin(1,0);Step(pump,0);
 var sending=port.Borrowed;byte[] second=[1,2,3];pump.Submit(new([new(ReplacementConnection.Login,second)],[]));Step(pump,1);Need(port.Sent.Count==1&&sending.Span.ContainsAnyExcept((byte)0));
 port.SendCompletion.TrySetResult();Step(pump,2);Need(sending.Span.ContainsAnyExcept((byte)0)==false&&port.Sent.Count==2);
 port.IgnoreCancellation=true;pump.Dispose();Need(!pump.DrainCompletion.IsCompleted&&!second.All(b=>b==0));
 port.SendCompletion.TrySetResult();byte[] late=[1,2];port.Deliver(lp,late);pump.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();Need(second.All(b=>b==0)&&late.All(b=>b==0));
}
foreach(bool synchronous in new[]{false,true})using(var s=Session())
{
 var port=new FakePort{FailSend=true,ThrowSend=synchronous};using var pump=new ReplacementDatagramPump(s,port,lp);pump.StartLogin(1,0);Step(pump,0);if(!synchronous)Step(pump,1);
 Need(pump.FailureType is not null&&port.Disposals==1);try{pump.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();}catch(AggregateException e){negatives++;Need(e.InnerExceptions.All(v=>v.Message=="IOException"));}
 Need(port.Borrowed.Span.ContainsAnyExcept((byte)0)==false);Need(s.Phase==ReplacementClientPhase.LoginHandshake);
}
using(var s=Session())
{
 var port=new FakePort();using var pump=new ReplacementDatagramPump(s,port,lp);pump.StartLogin(1,0);
 var wires=Enumerable.Range(0,256).Select(_=>new ReplacementClientDatagram(ReplacementConnection.Login,new byte[]{1})).ToArray();
 Reject(()=>pump.Submit(new(wires,[])));Need(wires.All(w=>w.Bytes.All(b=>b==0))&&port.Disposals==1);pump.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
}
using(var s=Session())
{
 var port=new FakePort();using var pump=new ReplacementDatagramPump(s,port,lp);pump.StartLogin(1,0);Step(pump,0);var r=pump.Tick(500);Need(r.Any(e=>e.FailureType is not null)&&port.Disposals==1);pump.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
}
using(var s=Session())
{
 var login=new FakePort();var zone=new FakePort();using var pump=new ReplacementDatagramPump(s,login,lp);Login(pump,s,login);
 Reject(()=>pump.BeginZone(f.GetProperty("ObjectId").GetUInt64(),0x87654321,zone,new(zp.Address,44454),4));Need(zone.Disposals==0&&login.Disposals==1&&zone.Sent.Count==0);pump.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
}
// Handoff waits for both send and receive, not just cancellation/disposal invocation.
using(var s=Session())
{
 var login=new FakePort();var zone=new FakePort();using var pump=new ReplacementDatagramPump(s,login,lp);Login(pump,s,login);
 login.BlockSend=true;login.IgnoreCancellation=true;byte[] owned=[1,2,3];pump.Submit(new([new(ReplacementConnection.Login,owned)],[]));Step(pump,3);
 pump.BeginZone(f.GetProperty("ObjectId").GetUInt64(),0x87654321,zone,zp,4);Step(pump,4);Need(zone.Sent.Count==0&&!owned.All(b=>b==0));
 byte[] late=[1,2];login.Deliver(lp,late);Step(pump,4);Need(zone.Sent.Count==0&&!pump.DrainCompletion.IsCompleted);
 login.SendCompletion.TrySetResult();pump.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();Need(owned.All(b=>b==0)&&late.All(b=>b==0));Step(pump,5);Need(zone.Sent.Count==1);
 pump.Dispose();pump.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
}
using(var s=Session())
{
 var port=new FakePort();using var pump=new ReplacementDatagramPump(s,port,lp);pump.StartLogin(1,0);
 var wires=Enumerable.Range(0,17).Select(_=>new ReplacementClientDatagram(ReplacementConnection.Login,new byte[65507])).ToArray();foreach(var w in wires)w.Bytes[0]=1;
 Reject(()=>pump.Submit(new(wires,[])));Need(wires.All(w=>w.Bytes.All(b=>b==0)));pump.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
}
using(var s=Session())
{
 var port=new FakePort();using var pump=new ReplacementDatagramPump(s,port,lp);pump.StartLogin(1,0);Step(pump,0);
 byte[] tooLarge=new byte[65508];tooLarge[0]=1;port.Deliver(lp,tooLarge);var failed=pump.Tick(1);Need(failed.Any(e=>e.FailureType is not null)&&tooLarge.All(b=>b==0));pump.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
}
using(var s=Session())
{
 var login=new FakePort();var zone=new FakePort();using var pump=new ReplacementDatagramPump(s,login,lp);Login(pump,s,login);
 login.FailDispose=true;pump.BeginZone(f.GetProperty("ObjectId").GetUInt64(),0x87654321,zone,zp,4);
 var failed=pump.Tick(5);Need(failed.Any(e=>e.FailureType is not null)&&zone.Sent.Count==0&&zone.Disposals==1);
 try{pump.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();}catch(AggregateException e){negatives++;Need(e.InnerExceptions.All(v=>v.Message=="IOException"));}
}
using(var s=Session())
{
 var port=new FakePort{BlockSend=true,IgnoreCancellation=true};using var pump=new ReplacementDatagramPump(s,port,lp);pump.StartLogin(1,0);Step(pump,0);
 System.Runtime.InteropServices.MemoryMarshal.TryGetArray(port.Borrowed,out ArraySegment<byte> segment);byte[] alias=segment.Array!;
 Reject(()=>pump.Submit(new([new(ReplacementConnection.Login,alias)],[])));Need(alias.Any(b=>b!=0)&&!pump.DrainCompletion.IsCompleted);
 port.SendCompletion.TrySetResult();port.Deliver(lp,new byte[]{1});pump.DrainCompletion.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();Need(alias.All(b=>b==0));
}
File.WriteAllText(args[2],JsonSerializer.Serialize(new{Passed=true,Checks=checks,Rejections=negatives,SocketCreated=false,NativeReadySubmittedThroughPumpOnly=true,SceneBarrierNotTested=true}));
Console.WriteLine($"PASS checks={checks} rejections={negatives}");

sealed class FakePort:IDatagramPort
{
 public readonly List<(DatagramPeer Peer,string Hex)> Sent=[];
 public int Disposals;public bool IgnoreCancellation,BlockSend,FailSend,ThrowSend,FailDispose;public Action? OnSend;
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
 public ValueTask DisposeAsync(){Disposals++;return FailDispose?new(Task.FromException(new IOException("Synthetic dispose failure"))):ValueTask.CompletedTask;}
}
