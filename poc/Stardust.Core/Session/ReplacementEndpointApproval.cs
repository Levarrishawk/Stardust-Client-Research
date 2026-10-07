using Stardust.Core.Configuration;
using Stardust.Core.Hosting;

namespace Stardust.Core.Session;

/// <summary>Explicit operator mapping, never a DNS result or live authorization.</summary>
public sealed class ReplacementApprovedEndpoint
{
    public string Hostname { get; }
    public uint Ipv4Address { get; }
    public ushort Port { get; }
    public ReplacementApprovedEndpoint(string hostname, uint ipv4Address, ushort port)
    {
        if(ipv4Address is 0 or uint.MaxValue||port==0)throw new ArgumentException("Bounded approved endpoint required.");
        Hostname=CanonicalHost(hostname);Ipv4Address=ipv4Address;Port=port;
        if(Hostname.All(c=>char.IsAsciiDigit(c)||c=='.')&&LiteralAddress(Hostname)!=ipv4Address)
            throw new ArgumentException("Approved literal mapping mismatch.");
    }
    internal static string CanonicalHost(string host)
    {
        if(string.IsNullOrEmpty(host)||host.Length>253||host.Any(c=>!(char.IsAsciiLetterOrDigit(c)||c is '.' or '-')))
            throw new ArgumentException("Canonical approved host required.");
        var labels=host.Split('.');
        if(labels.Any(l=>l.Length is <1 or >63||l[0]=='-'||l[^1]=='-'))throw new ArgumentException("Canonical approved host required.");
        bool numeric=host.All(c=>char.IsAsciiDigit(c)||c=='.');
        if(numeric)_=LiteralAddress(host);
        else if(System.Net.IPAddress.TryParse(host,out _))throw new ArgumentException("Canonical IPv4 literal required.");
        return host.ToLowerInvariant();
    }
    private static uint LiteralAddress(string host)
    {
        var labels=host.Split('.');uint value=0;
        if(labels.Length!=4)throw new ArgumentException("Canonical IPv4 literal required.");
        foreach(var label in labels)
        {
            if(!byte.TryParse(label,out byte part)||part.ToString(System.Globalization.CultureInfo.InvariantCulture)!=label)
                throw new ArgumentException("Canonical IPv4 literal required.");
            value=(value<<8)|part;
        }
        return value;
    }
    public override string ToString()=>"approved_endpoint_configured=true";
}

public sealed class ReplacementEndpointApproval
{
    public ReplacementApprovedEndpoint Login { get; }
    public ReplacementApprovedEndpoint Zone { get; }
    public uint GalaxyId { get; }
    public ulong? ApprovedCharacterId { get; }
    public ReplacementEndpointApproval(ReplacementApprovedEndpoint login, ReplacementApprovedEndpoint zone, uint galaxyId, ulong? approvedCharacterId=null)
    {
        if(approvedCharacterId==0)throw new ArgumentException("Nonzero approved character required.");
        Login=login??throw new ArgumentNullException(nameof(login));Zone=zone??throw new ArgumentNullException(nameof(zone));GalaxyId=galaxyId;ApprovedCharacterId=approvedCharacterId;
    }
    internal void ValidateLogin(LoginClientOptions options, DatagramPeer peer)
    {
        if(ReplacementApprovedEndpoint.CanonicalHost(options.Host)!=Login.Hostname||options.Port!=Login.Port
            ||peer.Address!=Login.Ipv4Address||peer.Port!=Login.Port)throw new ArgumentException("Approved login mismatch.");
    }
    internal void ValidateZone(ReplacementZoneSelection selection, DatagramPeer peer)
    {
        string host;
        try{host=ReplacementApprovedEndpoint.CanonicalHost(selection.Endpoint.Hostname);}
        catch(ArgumentException){throw new InvalidDataException("Approved zone mismatch.");}
        if(ApprovedCharacterId is { } approved&&selection.ObjectId!=approved||selection.GalaxyId!=GalaxyId||host!=Zone.Hostname||selection.Endpoint.Port!=Zone.Port
            ||peer.Address!=Zone.Ipv4Address||peer.Port!=Zone.Port)throw new InvalidDataException("Approved zone mismatch.");
    }
    public override string ToString()=>"endpoint_approval_configured=true";
}

/// <summary>Credential-free observation of the received selected advertisement.</summary>
public sealed class ReplacementZoneSelection
{
    internal ReplacementZoneSelection(ulong objectId,uint galaxyId,ReplacementZoneEndpoint endpoint)
    {ObjectId=objectId;GalaxyId=galaxyId;Endpoint=endpoint;}
    public ulong ObjectId { get; }
    public uint GalaxyId { get; }
    public ReplacementZoneEndpoint Endpoint { get; }
    public override string ToString()=>"zone_selection_preflight=true";
}
