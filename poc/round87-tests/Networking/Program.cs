using System.Text.Json;
using Stardust.Core.Protocol;
try {
    using var document=JsonDocument.Parse(File.ReadAllText(args[0]));var root=document.RootElement;
    uint id=root.GetProperty("ZoneId").GetUInt32(),seed=root.GetProperty("ZoneSeed").GetUInt32();
    var plain=Convert.FromHexString(root.GetProperty("ExpectedDisconnectPlain").GetString()!);
    var wire=Convert.FromHexString(root.GetProperty("ExpectedDisconnectWire").GetString()!);
    if(!SoeDatagramCodec.EncodeUncompressed(plain,seed,2,496).SequenceEqual(wire))throw new InvalidDataException("Independent protected disconnect mismatch");
    if(!SoeDatagramCodec.TryDecode(wire,seed,2,496,out var decoded)||!decoded.SequenceEqual(plain))throw new InvalidDataException("Independent plain disconnect mismatch");
    int checks=2;var records=new List<object>();
    foreach(var item in root.GetProperty("DisconnectTestPackets").EnumerateObject()) {
        var transport=new SoeClientTransport();_=transport.BeginSession(id,496);
        transport.AcceptSessionResponse(Convert.FromHexString(root.GetProperty("ZoneResponse").GetString()!));
        _=transport.CreateReliableDatagram(new byte[]{1,2},0);transport.QueueReliableMessage(new byte[]{3,4},1);
        if(transport.RetainedOutboundPackets!=1||transport.PendingMessageCount!=1)throw new InvalidDataException("Retained prefix missing");checks++;
        string outcome;SoeInboundDatagramResult? receipt=null;
        try {receipt=transport.AcceptDatagram(Convert.FromHexString(item.Value.GetString()!));outcome=receipt.RejectedFraming?"RejectedFraming":receipt.PeerDisconnected?"PeerDisconnected":"Other";}
        catch(InvalidDataException) {outcome="InvalidDataException";}
        string expected=item.Name switch {"Disconnect" or "AlternateReason"=>"PeerDisconnected","BadCrc"=>"RejectedFraming",_=>"InvalidDataException"};
        if(outcome!=expected)throw new InvalidDataException("Disconnect outcome mismatch: "+item.Name);checks++;
        bool closed=outcome=="PeerDisconnected";
        if(transport.State!=(closed?SoeClientTransportState.Closed:SoeClientTransportState.Established)||transport.RetainedOutboundPackets!=(closed?0:1)||transport.PendingMessageCount!=(closed?0:1))throw new InvalidDataException("Prefix/close cleanup mismatch");checks++;
        if(receipt is not null&&(receipt.ApplicationPayloads.Count!=0||receipt.ControlDatagrams.Count!=0||receipt.ReleasedOutboundPackets!=0))throw new InvalidDataException("No application/control reply expected");checks++;
        records.Add(new{Name=item.Name,Outcome=outcome,State=transport.State.ToString(),Pending=transport.PendingMessageCount,Retained=transport.RetainedOutboundPackets,ControlReplies=receipt?.ControlDatagrams.Count??0});
        transport.Close();transport.Close();
        if(transport.State!=SoeClientTransportState.Closed||transport.PendingMessageCount!=0||transport.RetainedOutboundPackets!=0)throw new InvalidDataException("Explicit caller cleanup failed");checks++;
    }
    File.WriteAllText(args[1],JsonSerializer.Serialize(new{Passed=true,Checks=checks,Records=records,SessionSendExecuted=false,LiveInterop=false}));
    Console.WriteLine($"PASS existing native transport disconnect: {checks} checks; outbound send/server release separate");
} catch(Exception error) {Console.Error.WriteLine(error);Environment.ExitCode=1;}
