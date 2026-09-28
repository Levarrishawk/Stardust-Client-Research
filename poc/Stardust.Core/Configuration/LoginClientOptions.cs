using System.Text;

namespace Stardust.Core.Configuration;

/// <summary>Validated login connection settings. Credentials are deliberately not part of this object.</summary>
public sealed class LoginClientOptions
{
    public LoginClientOptions(
        string host, int port, string clientVersion,
        uint requestedMaximumDatagramSize = 496,
        TimeSpan? sessionRetryInterval = null,
        TimeSpan? sessionTimeout = null,
        TimeSpan? reliableRetransmitInterval = null)
    {
        if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("Login host is required.", nameof(host));
        if (port is < 1 or > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(port));
        if (string.IsNullOrEmpty(clientVersion) || clientVersion.Any(character => character > 0x7F))
            throw new ArgumentException("Client version must be nonempty ASCII.", nameof(clientVersion));
        if (Encoding.ASCII.GetByteCount(clientVersion) > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(clientVersion));
        if (requestedMaximumDatagramSize is < 64 or > 65_507)
            throw new ArgumentOutOfRangeException(nameof(requestedMaximumDatagramSize));

        TimeSpan retry = sessionRetryInterval ?? TimeSpan.FromSeconds(1);
        TimeSpan timeout = sessionTimeout ?? TimeSpan.FromSeconds(15);
        TimeSpan retransmit = reliableRetransmitInterval ?? TimeSpan.FromSeconds(2);
        if (retry.TotalMilliseconds < 1) throw new ArgumentOutOfRangeException(nameof(sessionRetryInterval));
        if (timeout < retry) throw new ArgumentOutOfRangeException(nameof(sessionTimeout));
        if (retransmit.TotalMilliseconds < 1) throw new ArgumentOutOfRangeException(nameof(reliableRetransmitInterval));

        Host = host;
        Port = (ushort)port;
        ClientVersion = clientVersion;
        RequestedMaximumDatagramSize = requestedMaximumDatagramSize;
        SessionRetryInterval = retry;
        SessionTimeout = timeout;
        ReliableRetransmitInterval = retransmit;
    }

    public string Host { get; }
    public ushort Port { get; }
    public string ClientVersion { get; }
    public uint RequestedMaximumDatagramSize { get; }
    public TimeSpan SessionRetryInterval { get; }
    public TimeSpan SessionTimeout { get; }
    public TimeSpan ReliableRetransmitInterval { get; }

    public string ToSafeSummary() =>
        $"endpoint_configured=true version={ClientVersion} max_datagram={RequestedMaximumDatagramSize} " +
        $"session_retry_ms={SessionRetryInterval.TotalMilliseconds:F0} session_timeout_ms={SessionTimeout.TotalMilliseconds:F0} " +
        $"retransmit_ms={ReliableRetransmitInterval.TotalMilliseconds:F0}";

    public override string ToString() => ToSafeSummary();
}
