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
ReplacementSelectedPlayerIngress Ingress(bool create=true)
{var p=new ReplacementSelectedPlayerIngress();p.Bind(1,Oid);if(create)Need(p.Route(1,Hex("CreateHex")));return p;}
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
    return new(catalog,idle,new Dictionary<string,LogicalAnimationClipSelection>{{"walk",idle},{"run",idle}},(q,t)=>throw new InvalidOperationException("No visual loading"),SceneCreateZeroQuaternionPolicy.Reject,creatureView4FullPolicy:four,creatureView6FullPolicy:six);
}
ReplacementClientExchange Apps(params byte[][] values)=>new([],values);
int deferred=0,unhandledCount=0;ulong listen=0;int[] encumbrances=[];uint passive=0;var readyWires=new List<string>();
using(var session=Session())using(var world=World())using(var ingress=new ReplacementSelectedPlayerIngress())
{
    var minimum=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    using var bridge=new ReplacementSceneBridge(session,world,(scene,t)=>minimum.Task,ingress);bridge.BindSelection();
    bridge.ObserveExchange(session.AcceptIncoming(ReplacementConnection.Zone,Hex("PostSelection"),6));
    var payloads=new List<byte[]> {Hex("CreateHex"),Hex("Baseline1Hex"),Hex("BaselineHex"),Hex("Baseline4Hex"),Hex("Baseline6Hex"),Hex("Unsupported8Hex")};
    payloads.AddRange(root.GetProperty("Nearby").EnumerateArray().Select(v=>Convert.FromHexString(v.GetString()!)));
    payloads.AddRange(root.GetProperty("Player").EnumerateArray().Select(v=>Convert.FromHexString(v.GetString()!)));
    payloads.Add(Hex("EndHex"));payloads.Add(Hex("SentinelHex"));
    var result=bridge.ObserveExchange(Apps(payloads.ToArray()));
    Need(result.FailureType is null&&payloads.All(v=>v.All(b=>b==0))&&ingress.RequiredBaselinesReceived);
    deferred=ingress.DeferredApplications;unhandledCount=result.ZoneApplications.Count;Need(deferred==12&&unhandledCount==13&&world.StagedCount==5&&world.Snapshots().Count==0);
    Need(result.ZoneApplications[0].SequenceEqual(Hex("Baseline1Hex")));bridge.Poll(7);Need(bridge.ReadySubmissions==0);
    minimum.SetResult();bridge.DrainCompletion.Wait(1000);var output=bridge.Poll(8);readyWires.AddRange(output.Datagrams.Select(v=>Convert.ToHexString(v.Bytes)));
    var four=world.FindCreatureView4Full(bridge.Generation,Oid)!;var six=world.FindCreatureView6Full(bridge.Generation,Oid)!;
    listen=four.Model.ListenTo;encumbrances=four.Model.Encumbrances.Values.ToArray();passive=six.Model.Passive;
    Need(listen==123&&encumbrances.SequenceEqual(new[]{1,2,3})&&passive==0x76&&world.Snapshots().Count==1&&world.Snapshots()[0].Object.Handle.ObjectId==Oid);
    Need(bridge.ReadySubmissions==1);bridge.Dispose();Need(ingress.RequiredBaselinesReceived); // Borrowed ingress remains caller-owned.
}
foreach(string absent in new[]{"BaselineHex","Baseline4Hex","Baseline6Hex"})
{
    using var p=Ingress();foreach(string name in new[]{"BaselineHex","Baseline4Hex","Baseline6Hex"}.Where(n=>n!=absent))Need(p.Route(1,Hex(name)));
    Need(!p.RequiredBaselinesReceived);Reject(()=>p.Route(1,Hex("EndHex")));
}
foreach(string name in new[]{"BaselineHex","Baseline4Hex","Baseline6Hex","Baseline1Hex","Unsupported8Hex"})
{using var p=Ingress(false);Reject(()=>p.Route(1,Hex(name)));}
foreach(string name in new[]{"BaselineHex","Baseline4Hex","Baseline6Hex"})
{using var p=Ingress();Need(p.Route(1,Hex(name)));Reject(()=>p.Route(1,Hex(name)));}
using(var p=Ingress()){Reject(()=>p.Route(1,Hex("CreateHex")));}
using(var p=Ingress()){Reject(()=>p.Route(1,Hex("Delta3Hex")));}
using(var p=Ingress())
{foreach(string n in new[]{"BaselineHex","Baseline4Hex","Baseline6Hex","EndHex"})Need(p.Route(1,Hex(n)));Need(p.Route(1,Hex("Delta3Hex")));Reject(()=>p.Route(1,Hex("EndHex")));}
using(var p=Ingress())
{foreach(string n in new[]{"BaselineHex","Baseline4Hex","Baseline6Hex","EndHex"})p.Route(1,Hex(n));Need(p.Route(1,Hex("ContainmentHex"))&&p.Route(1,Hex("DestroyHex")));Reject(()=>p.Route(1,Hex("Baseline1Hex")));}
foreach(string name in new[]{"CreateHex","Baseline4Hex","Baseline6Hex","EndHex","ContainmentHex","DestroyHex"})
{
    using var p=Ingress(false);byte[] bytes=Hex(name);BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(6),Oid+1);Reject(()=>p.Route(1,bytes[..^1]));
}
using(var p=Ingress()){Reject(()=>p.Route(2,Hex("CreateHex")));}
using(var p=new ReplacementSelectedPlayerIngress()){Reject(()=>p.Route(1,Hex("CreateHex")));}
using(var p=Ingress()){Reject(()=>p.Bind(2,Oid));}
foreach((long g,ulong id) in new[]{(0L,Oid),(1L,0UL)}){using var p=new ReplacementSelectedPlayerIngress();Reject(()=>p.Bind(g,id));}
using(var p=Ingress())
{
    Exception? error=null;var t=new Thread(()=>{try{p.Route(1,Hex("BaselineHex"));}catch(Exception e){error=e;}});t.Start();t.Join();Need(error is InvalidOperationException&&!p.RequiredBaselinesReceived);negatives++;
    p.Dispose();p.Dispose();Reject(()=>p.Route(1,Hex("EndHex")));Need(!p.RequiredBaselinesReceived);
}
using(var session=Session())using(var world=World())using(var ingress=new ReplacementSelectedPlayerIngress())using(var bridge=new ReplacementSceneBridge(session,world,(scene,t)=>Task.CompletedTask,ingress))
{
    bridge.BindSelection();bridge.ObserveExchange(Apps(Hex("PostSelectionHex"),Hex("CreateHex"),Hex("BaselineHex"),Hex("Baseline4Hex")));
    var result=bridge.ObserveExchange(Apps(Hex("EndHex")));Need(result.FailureType is not null&&bridge.ReadySubmissions==0&&session.Phase==ReplacementClientPhase.Closed&&world.State==ZoneSceneTransitionState.Closed);negatives++;
}
using(var p=Ingress())
{
    foreach(uint tag in new[]{0x504C4159U,0x54414E4FU})
    {byte[] bytes=Hex("BaselineHex");BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(14),tag);Need(!p.Route(1,bytes));}
    Need(p.DeferredApplications==2&&!p.RequiredBaselinesReceived);
}
using(var p=Ingress())
{byte[] bytes=Hex("Baseline1Hex");Reject(()=>p.Route(1,bytes[..^1]));}
using(var p=Ingress())
{byte[] delta=Hex("Delta3Hex");delta[18]=1;Reject(()=>p.Route(1,delta));}
using(var p=Ingress(false))
{
    byte[] create=Hex("CreateHex");Need(p.Route(1,create));Array.Fill(create,(byte)0);
    byte[] baseline=Hex("BaselineHex");Need(p.Route(1,baseline));Array.Fill(baseline,(byte)0);
    Need(p.Route(1,Hex("Baseline4Hex"))&&p.Route(1,Hex("Baseline6Hex"))&&p.RequiredBaselinesReceived);
}
using(var session=Session())using(var world=World())using(var ingress=new ReplacementSelectedPlayerIngress())using(var bridge=new ReplacementSceneBridge(session,world,(scene,t)=>Task.CompletedTask,ingress))
{
    bridge.BindSelection();byte[] bad4=Hex("Baseline4Hex")[..25];BinaryPrimitives.WriteUInt32LittleEndian(bad4.AsSpan(19),2);
    var result=bridge.ObserveExchange(Apps(Hex("PostSelectionHex"),Hex("CreateHex"),Hex("BaselineHex"),bad4,Hex("Baseline6Hex"),Hex("EndHex")));
    Need(result.FailureType is null&&ingress.RequiredBaselinesReceived); // Envelope presence is not decoded completeness.
    Reject(()=>bridge.Poll(7));Need(bridge.ReadySubmissions==0&&session.Phase==ReplacementClientPhase.Closed);
}
Console.WriteLine(JsonSerializer.Serialize(new{Passed=true,Checks=checks,NegativeCases=negatives,Deferred=deferred,Unhandled=unhandledCount,RequiredBaselinesReceived=true,ListenTo=listen,Encumbrances=encumbrances,Passive=passive,ReadyWires=readyWires,OpaqueCREO1=true,OpaquePLAY=true,VisualPublication=false,LiveInterop=false}));
