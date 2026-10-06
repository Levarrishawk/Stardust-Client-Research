using System.Buffers.Binary;
using System.Reflection;
using System.Text.Json;
using Stardust.Core.Configuration;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

if(args.Length!=1)throw new ArgumentException("Explicit independent private fixture JSON required.");
using var fixture=JsonDocument.Parse(File.ReadAllBytes(args[0]));var root=fixture.RootElement;
byte[] Hex(string name)=>Convert.FromHexString(root.GetProperty(name).GetString()!);
int checks=0,negatives=0;
void Need(bool value){checks++;if(!value)throw new Exception("Check failed "+checks);}
var options=new LoginClientOptions("login.invalid",44452,"20260817-SD:01",496,TimeSpan.FromMilliseconds(5),TimeSpan.FromMilliseconds(50),TimeSpan.FromMilliseconds(2),TimeSpan.FromMilliseconds(100));
ReplacementClientSession Ready(string name="Login")
{
    var session=new ReplacementClientSession(options);
    var start=session.StartLogin(0x12345678,0);Need(start.Datagrams.Count==1&&start.Datagrams[0].Connection==ReplacementConnection.Login);
    session.AcceptIncoming(ReplacementConnection.Login,Hex("LoginResponse"),0);
    Need(session.Phase==ReplacementClientPhase.LoginAuthentication);
    session.Authenticate("synthetic","opaque-test-password",1);
    foreach(var frame in root.GetProperty(name).EnumerateArray())
    {var output=session.AcceptIncoming(ReplacementConnection.Login,Convert.FromHexString(frame.GetString()!),2);Need(output.FailureType is null&&output.Datagrams.All(d=>d.Connection==ReplacementConnection.Login));}
    Need(session.Phase==ReplacementClientPhase.CharacterList);return session;
}
ReplacementClientSession Zone(string name="Login",bool small=false)
{
    var session=Ready(name);session.BeginZone(0x1122334455667788,0x87654321,3);
    Need(session.Phase==ReplacementClientPhase.ZoneHandshake&&session.CredentialLength>0);
    Need(session.ZoneEndpoint!.Port==44453&&session.ZoneEndpoint.ToString()=="zone_endpoint_configured=true");
    var output=session.AcceptIncoming(ReplacementConnection.Zone,Hex(small?"SmallZoneResponse":"ZoneResponse"),4);
    Need(output.FailureType is null&&session.Phase==ReplacementClientPhase.ZonePermissions&&session.CredentialLength==0);
    return session;
}
byte[] Plain(byte[] wire,int max=496){Need(SoeDatagramCodec.TryDecode(wire,0x0BADCAFE,2,max,out var plain));return plain;}
void Reject(Action action,ReplacementClientSession? session=null)
{
    try{action();throw new Exception("Expected rejection");}
    catch(Exception error)when(error is InvalidOperationException or InvalidDataException or ArgumentOutOfRangeException or TimeoutException){negatives++;}
    if(session is not null)Need(session.Phase==ReplacementClientPhase.Faulted&&session.CredentialLength==0&&session.ZonePendingMessages==0&&session.ZoneRetainedPackets==0);
}
using(var session=Ready())
{
    Need(session.Characters.Count==1);
    session.BeginZone(0x1122334455667788,0x87654321,3);
    var auth=session.AcceptIncoming(ReplacementConnection.Zone,Hex("ZoneResponse"),4);
    Need(auth.Datagrams.Count==1&&Plain(auth.Datagrams[0].Bytes).AsSpan(4).SequenceEqual(Hex("ExpectedAuth")));
    var retries=session.Poll(6);Need(retries.Datagrams.Count==1&&retries.Datagrams[0].Bytes.SequenceEqual(auth.Datagrams[0].Bytes));
    var permission=session.AcceptIncoming(ReplacementConnection.Zone,Hex("Permissions"),7);
    Need(session.Phase==ReplacementClientPhase.Selected&&session.SelectionSubmissions==1&&permission.Datagrams.Count==2);
    Need(Plain(permission.Datagrams.Last().Bytes).AsSpan(4).SequenceEqual(Hex("ExpectedSelect")));
    var next=session.AcceptIncoming(ReplacementConnection.Zone,Hex("PostSelection"),8);
    Need(next.ZoneApplications.Count==1&&next.ZoneApplications[0].SequenceEqual(Hex("PostSelectionHex"))&&next.Datagrams.Count==1);
    var scene=ZoneSceneProtocolCodec.DecodeCmdStartScene(next.ZoneApplications[0]);
    Need(scene.ObjectId==0x1122334455667788&&scene.X==1.25f&&scene.Z==2.5f&&scene.Y==-3.75f&&scene.GalacticTime==123456789);
    Need(!session.ToString().Contains("zone.invalid")&&!session.ToString().Contains("synthetic"));
    Reject(()=>session.BeginZone(0x1122334455667788,0x11111111,9),session);
}
foreach(string name in new[]{"Denied","Malformed","DuplicateMultipayload","PrematureScene"})
{
    using var session=Zone();var result=session.AcceptIncoming(ReplacementConnection.Zone,Hex(name),5);
    Need(result.FailureType is not null&&session.Phase==ReplacementClientPhase.Faulted&&session.SelectionSubmissions==0);
    Need(result.Datagrams.Count==1&&Plain(result.Datagrams[0].Bytes)[1]==0x15&&result.ZoneApplications.Count==0);
    Need(session.ZonePendingMessages==0&&session.ZoneRetainedPackets==0&&session.CredentialLength==0);negatives++;
}
foreach(string name in new[]{"DuplicateLogin","MissingClusterLogin","EmptyHostLogin","UriHostLogin","ZeroPortLogin","OversizedCredentialLogin"})
{using var session=Ready(name);Reject(()=>session.BeginZone(0x1122334455667788,0x87654321,3),session);}
foreach(ulong id in new[]{0UL,1UL}){using var session=Ready();Reject(()=>session.BeginZone(id,0x87654321,3),session);}
using(var session=Ready())Reject(()=>session.BeginZone(0x1122334455667788,0x12345678,3),session);
using(var session=Ready())
{
    session.BeginZone(0x1122334455667788,0x87654321,3);
    var result=session.AcceptIncoming(ReplacementConnection.Zone,Hex("WrongZoneResponse"),4);
    Need(result.FailureType is not null&&session.CredentialLength==0&&session.Phase==ReplacementClientPhase.Faulted);negatives++;
}
using(var session=new ReplacementClientSession(options))
{
    session.StartLogin(0x12345678,0);Need(session.Poll(5).Datagrams.Count==1);
    Reject(()=>session.Poll(50),session);
}
using(var session=Ready())
{
    session.BeginZone(0x1122334455667788,0x87654321,3);Need(session.Poll(8).Datagrams.Count==1);
    Reject(()=>session.Poll(53),session);
}
using(var session=Zone())Reject(()=>session.Poll(104),session);
using(var session=new ReplacementClientSession(options))
{session.StartLogin(0x12345678,0);session.AcceptIncoming(ReplacementConnection.Login,Hex("LoginResponse"),0);session.Authenticate("u","p",1);Reject(()=>session.Poll(101),session);}
using(var session=new ReplacementClientSession(options))
{session.StartLogin(0x12345678,0);var result=session.AcceptIncoming(ReplacementConnection.Login,Hex("LoginResponse"),50);Need(result.FailureType is not null&&session.Phase==ReplacementClientPhase.Faulted);negatives++;}
using(var session=Zone())
{session.AcceptIncoming(ReplacementConnection.Zone,Hex("Permissions"),5);var result=session.AcceptIncoming(ReplacementConnection.Zone,Hex("DuplicateAfterSelection"),6);Need(result.FailureType is not null&&session.SelectionSubmissions==1&&session.ZoneRetainedPackets==0);negatives++;}
using(var session=new ReplacementClientSession(options))Reject(()=>session.Authenticate("u","p",0),session);
using(var session=Ready())
{
    Reject(()=>session.Poll(1));Need(session.Phase==ReplacementClientPhase.CharacterList); // regression rejected before mutation
    Exception? foreign=null;var thread=new Thread(()=>{try{session.Dispose();}catch(Exception error){foreign=error;}});thread.Start();thread.Join();Need(foreign is InvalidOperationException&&session.Phase==ReplacementClientPhase.CharacterList);negatives++;
}
using(var session=Zone())
{
    byte[] corrupt=Hex("Permissions");corrupt[^1]^=1;
    var result=session.AcceptIncoming(ReplacementConnection.Zone,corrupt,5);Need(result.RejectedFraming&&session.Phase==ReplacementClientPhase.ZonePermissions&&session.SelectionSubmissions==0);negatives++;
}
var largeAuthWires=new List<string>();
using(var session=Ready("LargeLogin"))
{
    session.BeginZone(0x1122334455667788,0x87654321,3);
    var output=session.AcceptIncoming(ReplacementConnection.Zone,Hex("SmallZoneResponse"),4);
    Need(output.FailureType is null&&session.ZonePendingMessages==1&&session.ZoneRetainedPackets==50);
    largeAuthWires.AddRange(output.Datagrams.Select(d=>Convert.ToHexString(d.Bytes)));
    int sequence=0;
    while(session.ZonePendingMessages!=0||session.ZoneRetainedPackets!=0)
    {
        var ack=Convert.FromHexString(root.GetProperty("ZoneAcks")[sequence++].GetString()!);
        var pumped=session.AcceptIncoming(ReplacementConnection.Zone,ack,5);Need(pumped.FailureType is null&&session.SelectionSubmissions==0);
        largeAuthWires.AddRange(pumped.Datagrams.Select(d=>Convert.ToHexString(d.Bytes)));if(sequence>1500)throw new Exception("Unbounded test");
    }
    var collected=new List<byte>();int index=0;
    foreach(string wire in largeAuthWires){var plain=Plain(Convert.FromHexString(wire),64);Need(plain[1]==13&&BinaryPrimitives.ReadUInt16BigEndian(plain.AsSpan(2))==index);collected.AddRange(plain.Skip(index++==0?8:4));}
    Need(collected.SequenceEqual(Hex("ExpectedLargeAuth"))&&largeAuthWires.Count>50);
    var permission=session.AcceptIncoming(ReplacementConnection.Zone,Hex("Permissions"),6);Need(permission.FailureType is null&&session.SelectionSubmissions==1);
}
foreach(bool timeout in new[]{false,true})
{
    using var session=Zone("LargeLogin",true);
    var transport=typeof(ReplacementClientSession).GetField("zone",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(session)!;
    var pending=(Array)transport.GetType().GetField("pending",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(transport)!;
    byte[] owned=(byte[])pending.GetValue(0)!.GetType().GetProperty("Bytes")!.GetValue(pending.GetValue(0))!;
    var channels=(Array)transport.GetType().GetField("reliable",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(transport)!;
    var retained=(System.Collections.IDictionary)channels.GetValue(0)!.GetType().GetField("outbound",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(channels.GetValue(0))!;
    var retainedBytes=retained.Values.Cast<object>().Select(value=>(byte[])value.GetType().GetProperty("Packet")!.GetValue(value)!).ToArray();
    Need(owned.Any(b=>b!=0));
    if(timeout)Reject(()=>session.Poll(104),session);else session.Dispose();
    Need(owned.All(b=>b==0)&&session.ZonePendingMessages==0&&session.ZoneRetainedPackets==0&&session.CredentialLength==0&&session.SelectionSubmissions==0);
    Need(retainedBytes.Length==50&&retainedBytes.All(value=>value.All(b=>b==0)));
}
using(var session=Ready())
{
    session.BeginZone(0x1122334455667788,0x87654321,3);
    byte[] owned=(byte[])typeof(ReplacementClientSession).GetField("credential",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(session)!;
    session.Dispose();Need(owned.All(b=>b==0)&&session.Phase==ReplacementClientPhase.Closed&&session.Characters.Count==0&&session.ZoneEndpoint is null);
    session.Dispose();Reject(()=>session.Poll(4));Need(session.Phase==ReplacementClientPhase.Closed);
}
Console.WriteLine(JsonSerializer.Serialize(new{Passed=true,Checks=checks,NegativeCases=negatives,SyntheticInMemoryOnly=true,LargeAuthDatagrams=largeAuthWires,LiveCompatibility=false,SceneReady=false}));
