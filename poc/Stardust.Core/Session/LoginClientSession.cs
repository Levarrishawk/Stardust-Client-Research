using System.Security.Cryptography;
using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

public enum LoginClientState
{
    Offline,
    NegotiatingTransport,
    ReadyToAuthenticate,
    AwaitingResponses,
    Complete,
    Failed,
    Closed
}

public sealed record LoginSessionReceiveResult(byte[]? ControlDatagram, IReadOnlyList<uint> ApplicationOpcodes,
    bool RejectedFraming = false, bool PeerDisconnected = false)
{
    public IReadOnlyList<byte[]> ControlDatagrams { get; init; } = ControlDatagram is null
        ? Array.Empty<byte[]>() : new[] { ControlDatagram };
}

/// <summary>Owns login-phase state and sensitive token lifetime above the SOE transport.</summary>
public sealed class LoginClientSession : IDisposable
{
    private readonly SoeClientTransport transport = new();
    private SoeHandshakeScheduler? handshake;
    private byte[]? credentialBlob;
    private bool tokenReceived;
    private bool clusterStatusReceived;
    private bool charactersReceived;
    private long authenticationStartedAt;

    public LoginClientState State { get; private set; }
    public uint AccountId { get; private set; }
    public uint StationId { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public LoginClusterList? ClusterList { get; private set; }
    public IReadOnlyList<LoginClusterStatus> ClusterStatuses { get; private set; } = Array.Empty<LoginClusterStatus>();
    public IReadOnlyDictionary<uint, LoginCluster> ClustersById { get; private set; } =
        new Dictionary<uint, LoginCluster>();
    public IReadOnlyDictionary<uint, LoginClusterStatus> ClusterStatusById { get; private set; } =
        new Dictionary<uint, LoginClusterStatus>();
    public IReadOnlyList<LoginCharacter> Characters { get; private set; } = Array.Empty<LoginCharacter>();
    public LoginError? LastError { get; private set; }
    public int CredentialLength => credentialBlob?.Length ?? 0;

    public byte[] BeginTransport(
        uint connectionId, uint maximumDatagramSize, long nowMilliseconds = 0,
        long retryIntervalMilliseconds = 1_000, long timeoutMilliseconds = 15_000)
    {
        Require(LoginClientState.Offline);
        handshake = new(transport, retryIntervalMilliseconds, timeoutMilliseconds);
        byte[] request = handshake.Start(connectionId, maximumDatagramSize, nowMilliseconds);
        State = LoginClientState.NegotiatingTransport;
        return request;
    }

    public byte[]? PollHandshake(long nowMilliseconds)
    {
        Require(LoginClientState.NegotiatingTransport);
        try { return handshake!.Poll(nowMilliseconds); }
        catch (TimeoutException)
        {
            Fail();
            throw;
        }
    }

    public void AcceptSessionResponse(ReadOnlySpan<byte> response)
    {
        Require(LoginClientState.NegotiatingTransport);
        handshake!.AcceptResponse(response);
        State = LoginClientState.ReadyToAuthenticate;
    }

    public byte[] CreateLoginDatagram(
        string username, string password, string version, long nowMilliseconds)
    {
        Require(LoginClientState.ReadyToAuthenticate);
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));
        byte[] application = LoginProtocolCodec.EncodeLoginClientId(username, password, version);
        try
        {
            byte[] datagram = transport.CreateReliableDatagram(application, nowMilliseconds);
            State = LoginClientState.AwaitingResponses;
            authenticationStartedAt = nowMilliseconds;
            return datagram;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(application);
        }
    }

    public LoginSessionReceiveResult AcceptDatagram(ReadOnlySpan<byte> datagram)
    {
        if (State is not (LoginClientState.AwaitingResponses or LoginClientState.Complete))
            throw new InvalidOperationException("Login response is not expected.");
        try
        {
            SoeInboundDatagramResult received = transport.AcceptDatagram(datagram);
            if (received.PeerDisconnected) Fail();
            List<uint> opcodes = new(received.ApplicationPayloads.Count);
            try
            {
                foreach (byte[] application in received.ApplicationPayloads)
                {
                    if (State == LoginClientState.Failed) break;
                    DecodedLoginApplicationMessage decoded = LoginApplicationDecoder.Decode(application);
                    opcodes.Add(decoded.Opcode);
                    Apply(decoded.Value);
                }
            }
            finally
            {
                foreach (byte[] application in received.ApplicationPayloads)
                    CryptographicOperations.ZeroMemory(application);
            }
            return new(received.ControlDatagram, opcodes, received.RejectedFraming, received.PeerDisconnected)
                { ControlDatagrams = received.ControlDatagrams };
        }
        catch (InvalidDataException)
        {
            Fail();
            throw;
        }
    }

    public byte[] CopyCredentialBlob()
    {
        if (credentialBlob is null) throw new InvalidOperationException("No login credential has been received.");
        return (byte[])credentialBlob.Clone();
    }

    public void CheckResponseDeadline(long nowMilliseconds, long timeoutMilliseconds)
    {
        if (timeoutMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds));
        if (State != LoginClientState.AwaitingResponses) return;
        if (nowMilliseconds < authenticationStartedAt ||
            nowMilliseconds - authenticationStartedAt < timeoutMilliseconds) return;
        Fail();
        throw new TimeoutException("Login responses did not complete before the local deadline.");
    }

    public IReadOnlyList<byte[]> GetRetransmissionDatagrams(long nowMilliseconds, long timeoutMilliseconds)
    {
        if (State is not (LoginClientState.AwaitingResponses or LoginClientState.Complete))
            throw new InvalidOperationException("Reliable retransmission is not active in the current login state.");
        return transport.GetRetransmissionDatagrams(nowMilliseconds, timeoutMilliseconds);
    }

    public void Close()
    {
        if (credentialBlob is not null) CryptographicOperations.ZeroMemory(credentialBlob);
        credentialBlob = null;
        AccountId = 0;
        StationId = 0;
        Username = string.Empty;
        ClusterList = null;
        ClusterStatuses = Array.Empty<LoginClusterStatus>();
        ClustersById = new Dictionary<uint, LoginCluster>();
        ClusterStatusById = new Dictionary<uint, LoginClusterStatus>();
        Characters = Array.Empty<LoginCharacter>();
        LastError = null;
        tokenReceived = false;
        clusterStatusReceived = false;
        charactersReceived = false;
        transport.Close();
        handshake = null;
        State = LoginClientState.Closed;
    }

    public void Dispose() => Close();

    private void Apply(object message)
    {
        switch (message)
        {
            case LoginClientToken token:
                if (credentialBlob is not null) CryptographicOperations.ZeroMemory(credentialBlob);
                credentialBlob = token.CredentialBlob;
                AccountId = token.AccountId;
                StationId = token.StationId;
                Username = token.Username;
                tokenReceived = true;
                break;
            case LoginClusterList clusters:
                ClusterList = clusters;
                ClustersById = ToUniqueMap(clusters.Clusters, cluster => cluster.GalaxyId, "cluster list");
                break;
            case IReadOnlyList<LoginClusterStatus> statuses:
                ClusterStatuses = statuses;
                ClusterStatusById = ToUniqueMap(statuses, status => status.GalaxyId, "cluster status");
                clusterStatusReceived = true;
                break;
            case IReadOnlyList<LoginCharacter> characters:
                Characters = characters;
                charactersReceived = true;
                break;
            case LoginError error:
                LastError = error;
                if (error.Fatal) Fail();
                break;
            default:
                throw new InvalidDataException("Unsupported decoded login message type.");
        }
        TryComplete();
    }

    private void TryComplete()
    {
        if (State == LoginClientState.AwaitingResponses && tokenReceived && ClusterList is not null &&
            clusterStatusReceived && charactersReceived)
            State = LoginClientState.Complete;
    }

    private void Fail()
    {
        LoginError? failure = LastError;
        Close();
        LastError = failure;
        State = LoginClientState.Failed;
    }

    private void Require(LoginClientState expected)
    {
        if (State != expected) throw new InvalidOperationException($"Expected login state {expected}, found {State}.");
    }

    private static IReadOnlyDictionary<uint, T> ToUniqueMap<T>(
        IEnumerable<T> records, Func<T, uint> keySelector, string label)
    {
        Dictionary<uint, T> result = new();
        foreach (T record in records)
        {
            if (!result.TryAdd(keySelector(record), record))
                throw new InvalidDataException($"Duplicate galaxy ID in {label}.");
        }
        return result;
    }
}
