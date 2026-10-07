using System.Text;
using System.Text.Json;
using Stardust.Core.Hosting;
using Stardust.Core.Session;

namespace Stardust.Core.Configuration;

/// <summary>Validated explicit operator assertions; no credentials or implicit contact authorization.</summary>
public sealed class ReplacementControlledLaunch
{
    internal ReplacementControlledLaunch(ReplacementLaunchProfile profile,LoginClientOptions options,
        ReplacementEndpointApproval approval,uint loginId,uint zoneId,long phase,long overall,long disconnect,
        ReplacementControlledMovementScenario? movement)
    {
        Profile=profile;Options=options;EndpointApproval=approval;
        LoginPeer=new(approval.Login.Ipv4Address,approval.Login.Port);ZonePeer=new(approval.Zone.Ipv4Address,approval.Zone.Port);
        ApprovedCharacterId=approval.ApprovedCharacterId!.Value;LoginConnectionId=loginId;ZoneConnectionId=zoneId;
        PhaseBudgetMilliseconds=phase;OverallBudgetMilliseconds=overall;DisconnectTimeoutMilliseconds=disconnect;MovementScenario=movement;
    }
    public ReplacementLaunchProfile Profile { get; }
    public LoginClientOptions Options { get; }
    public DatagramPeer LoginPeer { get; }
    public DatagramPeer ZonePeer { get; }
    public ReplacementEndpointApproval EndpointApproval { get; }
    public ulong ApprovedCharacterId { get; }
    public uint LoginConnectionId { get; }
    public uint ZoneConnectionId { get; }
    public long PhaseBudgetMilliseconds { get; }
    public long OverallBudgetMilliseconds { get; }
    public long DisconnectTimeoutMilliseconds { get; }
    public bool MinimumReadinessApproved=>true;
    public ReplacementControlledMovementScenario? MovementScenario { get; }
    public override string ToString()=>"controlled_launch_configured=true";
}

public static class ReplacementControlledLaunchCodec
{
    public static ReplacementControlledLaunch Decode(ReadOnlySpan<byte> json)
    {
        if(json.Length is <1 or >131072)throw new InvalidDataException("Controlled launch byte budget.");
        try
        {
            _=new UTF8Encoding(false,true).GetCharCount(json);
            using var document=JsonDocument.Parse(json.ToArray(),new(){MaxDepth=12});var root=document.RootElement;
            Keys(root,["Version","Profile","ClientVersion","Login","Zone","GalaxyId","ApprovedCharacterId","LoginConnectionId","ZoneConnectionId",
                "PhaseBudgetMilliseconds","OverallBudgetMilliseconds","DisconnectTimeoutMilliseconds","MinimumReadinessApproved"],"MovementPrerequisites");
            if(U32(root,"Version")!=1)throw new InvalidDataException("Unsupported controlled launch version.");
            var profile=ReplacementLaunchProfileCodec.Decode(Encoding.UTF8.GetBytes(root.GetProperty("Profile").GetRawText()));
            var login=Endpoint(root.GetProperty("Login"));var zone=Endpoint(root.GetProperty("Zone"));
            string version=Text(root,"ClientVersion");
            if(version.Length is <1 or >256)throw new InvalidDataException("Controlled client version budget.");
            var options=new LoginClientOptions(login.Hostname,login.Port,version);
            uint loginId=U32(root,"LoginConnectionId"),zoneId=U32(root,"ZoneConnectionId");
            if(loginId==zoneId)throw new InvalidDataException("Distinct controlled connection IDs required.");
            long phase=Bound(root,"PhaseBudgetMilliseconds",30000),overall=Bound(root,"OverallBudgetMilliseconds",120000),disconnect=Bound(root,"DisconnectTimeoutMilliseconds",30000);
            if(overall<phase)throw new InvalidDataException("Controlled budget order required.");
            if(!Boolean(root,"MinimumReadinessApproved"))throw new InvalidDataException("Explicit minimum readiness scenario approval required.");
            var character=root.GetProperty("ApprovedCharacterId");
            if(character.ValueKind!=JsonValueKind.Number||!character.TryGetUInt64(out ulong id)||id==0)throw new InvalidDataException("Nonzero approved character required.");
            var approval=new ReplacementEndpointApproval(login,zone,U32(root,"GalaxyId"),id);
            ReplacementControlledMovementScenario? movement=null;
            if(root.TryGetProperty("MovementPrerequisites",out var supplied))
            {
                Keys(supplied,["FreshStampZero","MatchingValidatedPosition","MovementPrerequisitesValidated"]);
                bool fresh=Boolean(supplied,"FreshStampZero"),matching=Boolean(supplied,"MatchingValidatedPosition"),validated=Boolean(supplied,"MovementPrerequisitesValidated");
                if(fresh&&matching&&validated)movement=new(id,true,true,true);
            }
            return new(profile,options,approval,loginId,zoneId,phase,overall,disconnect,movement);
        }
        catch(Exception error)when(error is JsonException or DecoderFallbackException or InvalidOperationException or KeyNotFoundException or FormatException or ArgumentException)
        {throw new InvalidDataException("Invalid controlled launch JSON.");}
    }
    private static ReplacementApprovedEndpoint Endpoint(JsonElement value)
    {
        Keys(value,["Hostname","Ipv4Address","Port"]);uint port=U32(value,"Port");
        if(port is 0 or >ushort.MaxValue)throw new InvalidDataException("Controlled endpoint port bound.");
        return new(Text(value,"Hostname"),U32(value,"Ipv4Address"),(ushort)port);
    }
    private static long Bound(JsonElement value,string name,uint maximum)
    {uint number=U32(value,name);if(number is 0||number>maximum)throw new InvalidDataException("Controlled duration bound.");return number;}
    private static uint U32(JsonElement value,string name)
    {var field=value.GetProperty(name);if(field.ValueKind!=JsonValueKind.Number||!field.TryGetUInt32(out uint result))throw new InvalidDataException("Controlled unsigned integer required.");return result;}
    private static string Text(JsonElement value,string name)
    {var field=value.GetProperty(name);if(field.ValueKind!=JsonValueKind.String)throw new InvalidDataException("Controlled string required.");return field.GetString()!;}
    private static bool Boolean(JsonElement value,string name)
    {var field=value.GetProperty(name);if(field.ValueKind is not (JsonValueKind.True or JsonValueKind.False))throw new InvalidDataException("Controlled boolean required.");return field.GetBoolean();}
    private static void Keys(JsonElement value,string[] required,string? optional=null)
    {
        if(value.ValueKind!=JsonValueKind.Object)throw new InvalidDataException("Controlled object required.");var seen=new HashSet<string>(StringComparer.Ordinal);
        foreach(var property in value.EnumerateObject())
            if(!seen.Add(property.Name)||(!required.Contains(property.Name,StringComparer.Ordinal)&&property.Name!=optional))throw new InvalidDataException("Duplicate or unknown controlled property.");
        if(required.Any(name=>!seen.Contains(name)))throw new InvalidDataException("Missing controlled property.");
    }
}
