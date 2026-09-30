using System.Security.Cryptography;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.Hosting;

/// <summary>Caller-owned completed enumeration and credential copy; dispose after transfer/use.</summary>
public sealed class LoginConnectionResult : IDisposable
{
    private byte[]? credential;
    internal LoginConnectionResult(LoginClientSession session, DatagramCounters counters)
    {
        AccountId = session.AccountId;
        StationId = session.StationId;
        Username = session.Username;
        Clusters = Array.AsReadOnly(session.ClusterList!.Clusters.ToArray());
        ClusterStatuses = Array.AsReadOnly(session.ClusterStatuses.ToArray());
        Characters = Array.AsReadOnly(session.Characters.ToArray());
        credential = session.CopyCredentialBlob();
        Counters = counters;
    }
    public uint AccountId { get; }
    public uint StationId { get; }
    public string Username { get; }
    public IReadOnlyList<LoginCluster> Clusters { get; }
    public IReadOnlyList<LoginClusterStatus> ClusterStatuses { get; }
    public IReadOnlyList<LoginCharacter> Characters { get; }
    public DatagramCounters Counters { get; }
    public int CredentialLength => credential?.Length ?? 0;
    public byte[] CopyCredential() => credential is null
        ? throw new ObjectDisposedException(nameof(LoginConnectionResult)) : (byte[])credential.Clone();
    public void Dispose()
    {
        if (credential is not null) CryptographicOperations.ZeroMemory(credential);
        credential = null;
    }
    public override string ToString() => $"login_completed=true clusters={Clusters.Count} characters={Characters.Count}";
}

public enum LoginHostOutcome { None, Completed, Cancelled, TimedOut, RemoteRejected, PeerDisconnected, Malformed, IoFailure, ClockFailure, InvalidInput }

/// <summary>Safe failure category without endpoint/payload/error text or an inner exception.</summary>
public sealed class LoginHostFailureException(LoginHostOutcome outcome)
    : Exception("Login host failed: " + outcome)
{
    public LoginHostOutcome Outcome { get; } = outcome;
}
