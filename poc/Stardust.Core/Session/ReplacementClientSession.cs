using System.Buffers.Binary;
using System.Security.Cryptography;
using Stardust.Core.Configuration;
using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

public enum ReplacementClientPhase { Offline, LoginHandshake, LoginAuthentication, LoginResponses, CharacterList, ZoneHandshake, ZonePermissions, Selected, Faulted, Closed }
public enum ReplacementConnection { Login, Zone }
public sealed record ReplacementClientDatagram(ReplacementConnection Connection, byte[] Bytes);
public sealed record ReplacementClientExchange(IReadOnlyList<ReplacementClientDatagram> Datagrams,
    IReadOnlyList<byte[]> ZoneApplications, bool RejectedFraming = false, string? FailureType = null);
public sealed record ReplacementClientDiagnostic(ReplacementClientPhase Phase, string? FailureType);
public sealed class ReplacementZoneEndpoint
{
    internal ReplacementZoneEndpoint(string hostname, ushort port) { Hostname = hostname; Port = port; }
    public string Hostname { get; }
    public ushort Port { get; }
    public override string ToString() => "zone_endpoint_configured=true";
}

/// <summary>Single-thread, socket-independent login-to-permission-gated selection owner.</summary>
public sealed class ReplacementClientSession : IDisposable
{
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private readonly LoginClientOptions options;
    private readonly Action<ReplacementClientDiagnostic>? diagnostics;
    private LoginClientCoordinator? login;
    private readonly SoeClientTransport zone = new();
    private SoeHandshakeScheduler? zoneHandshake;
    private byte[]? credential;
    private uint loginId;
    private ulong selectedObject;
    private long lastClock = -1, phaseStarted;
    private bool busy;
    public ReplacementClientSession(LoginClientOptions options, Action<ReplacementClientDiagnostic>? diagnostics = null)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        if (options.ClientVersion.Length > 256) throw new ArgumentOutOfRangeException(nameof(options));
        this.diagnostics = diagnostics;
    }
    public ReplacementClientPhase Phase { get; private set; }
    public ReplacementZoneEndpoint? ZoneEndpoint { get; private set; }
    public string? FailureType { get; private set; }
    public int DiagnosticFailures { get; private set; }
    public int SelectionSubmissions { get; private set; }
    public int CredentialLength => credential?.Length ?? login?.Session.CredentialLength ?? 0;
    public int ZonePendingMessages => zone.PendingMessageCount;
    public int ZoneRetainedPackets => zone.RetainedOutboundPackets;
    public IReadOnlyList<LoginCharacter> Characters => Array.AsReadOnly(login?.Session.Characters.ToArray() ?? []);
    public override string ToString() => $"replacement_phase={Phase} selections={SelectionSubmissions}";

    public ReplacementClientExchange StartLogin(uint connectionId, long nowMilliseconds) => Execute(nowMilliseconds, () =>
    {
        Require(ReplacementClientPhase.Offline); loginId = connectionId; login = new(options);
        var output = login.Start(connectionId, nowMilliseconds); Change(ReplacementClientPhase.LoginHandshake, nowMilliseconds);
        return LoginOutput([output]);
    });
    public ReplacementClientExchange Authenticate(string username, string password, long nowMilliseconds) => Execute(nowMilliseconds, () =>
    {
        Require(ReplacementClientPhase.LoginAuthentication);
        var output = login!.Authenticate(username, password, nowMilliseconds);
        Change(ReplacementClientPhase.LoginResponses, nowMilliseconds); return LoginOutput([output]);
    });
    public ReplacementClientExchange BeginZone(ulong objectId, uint zoneConnectionId, long nowMilliseconds) => Execute(nowMilliseconds, () =>
    {
        Require(ReplacementClientPhase.CharacterList);
        if (objectId == 0) throw new InvalidDataException("Nonzero selected identity required.");
        if (zoneConnectionId == loginId) throw new InvalidDataException("Distinct zone connection required.");
        var session = login!.Session;
        if (session.Characters.GroupBy(c => c.ObjectId).Any(group => group.Count() != 1))
            throw new InvalidDataException("Unique character object identities required.");
        var characters = session.Characters.Where(c => c.ObjectId == objectId).ToArray();
        if (characters.Length != 1) throw new InvalidDataException("Unique selected character required.");
        var character = characters[0];
        var clusters = session.ClusterList!.Clusters.Where(c => c.GalaxyId == character.GalaxyId).ToArray();
        var statuses = session.ClusterStatuses.Where(c => c.GalaxyId == character.GalaxyId).ToArray();
        if (clusters.Length != 1 || statuses.Length != 1) throw new InvalidDataException("Unique matching cluster required.");
        var status = statuses[0];
        if (string.IsNullOrWhiteSpace(status.Hostname) || status.Hostname.Length > 253
            || status.Hostname.Any(c => char.IsWhiteSpace(c) || char.IsControl(c)) || status.ZonePort == 0)
            throw new InvalidDataException("Valid zone endpoint required.");
        if (status.Hostname.Contains('/') || status.Hostname.Contains('\\')
            || status.Hostname.Contains(':') && !System.Net.IPAddress.TryParse(status.Hostname, out _))
            throw new InvalidDataException("Host literal without URI or path required.");
        credential = session.CopyCredentialBlob();
        if (credential.Length is < 4 or > 65536) throw new InvalidDataException("Bounded opaque credential required.");
        ZoneEndpoint = new(status.Hostname, status.ZonePort); selectedObject = objectId;
        login.Dispose(); login = null;
        zoneHandshake = new(zone, Milliseconds(options.SessionRetryInterval), Milliseconds(options.SessionTimeout));
        byte[] request = zoneHandshake.Start(zoneConnectionId, options.RequestedMaximumDatagramSize, nowMilliseconds);
        Change(ReplacementClientPhase.ZoneHandshake, nowMilliseconds); return ZoneOutput([request]);
    });

    public ReplacementClientExchange AcceptIncoming(ReplacementConnection connection, ReadOnlySpan<byte> datagram, long nowMilliseconds)
    {
        Enter(nowMilliseconds);
        var outgoing = new List<ReplacementClientDatagram>(); var applications = new List<byte[]>();
        try
        {
            EnsureActive(); CheckDeadline(nowMilliseconds);
            if (connection == ReplacementConnection.Login)
            {
                if (Phase is not (ReplacementClientPhase.LoginHandshake or ReplacementClientPhase.LoginResponses or ReplacementClientPhase.CharacterList))
                    throw new InvalidOperationException("Login receive phase required.");
                outgoing.AddRange(login!.AcceptIncoming(datagram).Select(d => new ReplacementClientDatagram(connection, d.Bytes)));
                if (login.Session.State == LoginClientState.ReadyToAuthenticate) Change(ReplacementClientPhase.LoginAuthentication, nowMilliseconds);
                else if (login.Session.State == LoginClientState.Complete) Change(ReplacementClientPhase.CharacterList, nowMilliseconds);
                else if (login.Session.State is LoginClientState.Failed or LoginClientState.Closed) throw new InvalidDataException("Login ended before handoff.");
                return new(outgoing.AsReadOnly(), applications.AsReadOnly());
            }
            if (connection != ReplacementConnection.Zone) throw new ArgumentOutOfRangeException(nameof(connection));
            if (Phase == ReplacementClientPhase.ZoneHandshake)
            {
                zoneHandshake!.AcceptResponse(datagram);
                byte[] message = ZoneAuthenticationCodec.EncodeClientId(credential!, options.ClientVersion);
                try { zone.QueueReliableMessage(message); }
                finally { CryptographicOperations.ZeroMemory(message); ClearCredential(); }
                outgoing.AddRange(zone.PumpQueuedDatagrams(nowMilliseconds).Select(d => new ReplacementClientDatagram(connection, d)));
                Change(ReplacementClientPhase.ZonePermissions, nowMilliseconds);
                return new(outgoing.AsReadOnly(), applications.AsReadOnly());
            }
            if (Phase is not (ReplacementClientPhase.ZonePermissions or ReplacementClientPhase.Selected)) throw new InvalidOperationException("Zone receive phase required.");
            var result = zone.AcceptDatagram(datagram);
            outgoing.AddRange(result.ControlDatagrams.Select(d => new ReplacementClientDatagram(connection, d)));
            if (result.PeerDisconnected) throw new InvalidDataException("Zone peer disconnected.");
            if (result.RejectedFraming) return new(outgoing.AsReadOnly(), applications.AsReadOnly(), true);
            bool selectedThisReceive = false;
            try
            {
                foreach (byte[] message in result.ApplicationPayloads)
                {
                    bool permissions = message.Length >= 6 && BinaryPrimitives.ReadUInt32LittleEndian(message.AsSpan(2)) == ZoneAuthenticationCodec.PermissionsOpcode;
                    if (Phase == ReplacementClientPhase.ZonePermissions && !selectedThisReceive)
                    {
                        if (!permissions || zone.PendingMessageCount != 0) throw new InvalidDataException("Permissions required after submitted authentication.");
                        var value = ZoneAuthenticationCodec.DecodePermissions(message);
                        if (!value.CanLogin) throw new InvalidDataException("Zone login denied.");
                        byte[] select = ZoneSceneProtocolCodec.EncodeSelectCharacter(selectedObject);
                        try { zone.QueueReliableMessage(select); } finally { CryptographicOperations.ZeroMemory(select); }
                        selectedThisReceive = true;
                    }
                    else
                    {
                        if (permissions) throw new InvalidDataException("Duplicate zone permissions.");
                        applications.Add((byte[])message.Clone());
                    }
                }
            }
            finally { foreach (byte[] message in result.ApplicationPayloads) CryptographicOperations.ZeroMemory(message); }
            if (selectedThisReceive) { SelectionSubmissions++; Change(ReplacementClientPhase.Selected, nowMilliseconds); }
            outgoing.AddRange(zone.PumpQueuedDatagrams(nowMilliseconds).Select(d => new ReplacementClientDatagram(connection, d)));
            return new(outgoing.AsReadOnly(), applications.AsReadOnly());
        }
        catch (Exception error) when (error is InvalidDataException or InvalidOperationException or ArgumentException or TimeoutException or NotSupportedException)
        {
            foreach (byte[] message in applications) CryptographicOperations.ZeroMemory(message); applications.Clear();
            Fault(error); return new(outgoing.AsReadOnly(), applications.AsReadOnly(), FailureType: FailureType);
        }
        finally { busy = false; }
    }
    public ReplacementClientExchange Poll(long nowMilliseconds) => Execute(nowMilliseconds, () =>
    {
        EnsureActive(); CheckDeadline(nowMilliseconds);
        if (login is not null) return LoginOutput(login.Poll(nowMilliseconds));
        if (Phase == ReplacementClientPhase.ZoneHandshake)
        { byte[]? request = zoneHandshake!.Poll(nowMilliseconds); return ZoneOutput(request is null ? [] : [request]); }
        var output = zone.GetRetransmissionDatagrams(nowMilliseconds, Milliseconds(options.ReliableRetransmitInterval)).ToList();
        output.AddRange(zone.PumpQueuedDatagrams(nowMilliseconds)); return ZoneOutput(output);
    });
    public void Dispose()
    {
        Enter(null); try { Release(); Change(ReplacementClientPhase.Closed, lastClock); } finally { busy = false; }
    }
    private ReplacementClientExchange Execute(long clock, Func<ReplacementClientExchange> action)
    {
        Enter(clock);
        try { return action(); }
        catch (Exception error) { Fault(error); throw; }
        finally { busy = false; }
    }
    private void Enter(long? clock)
    {
        if (Environment.CurrentManagedThreadId != ownerThread || busy) throw new InvalidOperationException("Session owner thread without reentry required.");
        if (clock is long value)
        { if (value < 0 || value < lastClock) throw new ArgumentOutOfRangeException(nameof(clock)); lastClock = value; }
        busy = true;
    }
    private void CheckDeadline(long now)
    {
        if (Phase == ReplacementClientPhase.LoginResponses) login!.Session.CheckResponseDeadline(now, Milliseconds(options.LoginResponseTimeout));
        if (Phase is ReplacementClientPhase.LoginHandshake or ReplacementClientPhase.ZoneHandshake && now - phaseStarted >= Milliseconds(options.SessionTimeout)
            || Phase is ReplacementClientPhase.LoginAuthentication or ReplacementClientPhase.ZonePermissions && now - phaseStarted >= Milliseconds(options.LoginResponseTimeout))
            throw new TimeoutException("Session phase deadline elapsed.");
    }
    private void Require(ReplacementClientPhase phase) { if (Phase != phase) throw new InvalidOperationException("Session phase mismatch."); }
    private void EnsureActive() { if (Phase is ReplacementClientPhase.Offline or ReplacementClientPhase.Faulted or ReplacementClientPhase.Closed) throw new InvalidOperationException("Active session required."); }
    private void ClearCredential() { if (credential is not null) CryptographicOperations.ZeroMemory(credential); credential = null; }
    private void Release() { ClearCredential(); login?.Dispose(); login = null; zone.Close(); zoneHandshake = null; ZoneEndpoint = null; selectedObject = 0; }
    private void Fault(Exception error) { FailureType = error.GetType().Name; Release(); if (Phase != ReplacementClientPhase.Closed) Change(ReplacementClientPhase.Faulted, lastClock); }
    private void Change(ReplacementClientPhase phase, long clock)
    {
        if (Phase == phase) return; Phase = phase; phaseStarted = clock;
        try { diagnostics?.Invoke(new(Phase, FailureType)); } catch (Exception) { if (DiagnosticFailures < int.MaxValue) DiagnosticFailures++; }
    }
    private static long Milliseconds(TimeSpan value) => checked((long)value.TotalMilliseconds);
    private static ReplacementClientExchange LoginOutput(IEnumerable<LoginOutboundDatagram> output) => new(output.Select(d => new ReplacementClientDatagram(ReplacementConnection.Login, d.Bytes)).ToArray(), []);
    private static ReplacementClientExchange ZoneOutput(IEnumerable<byte[]> output) => new(output.Select(d => new ReplacementClientDatagram(ReplacementConnection.Zone, d)).ToArray(), []);
}
