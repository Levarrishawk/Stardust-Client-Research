using Stardust.Core.Configuration;

namespace Stardust.Core.Session;

public enum LoginOutboundKind
{
    SessionRequest,
    LoginRequest,
    ReliabilityControl,
    Retransmission
}

public sealed record LoginOutboundDatagram(LoginOutboundKind Kind, byte[] Bytes);

/// <summary>Clock-driven, socket-independent coordinator for the login connection.</summary>
public sealed class LoginClientCoordinator : IDisposable
{
    private readonly LoginClientOptions options;
    private readonly LoginClientSession session = new();

    public LoginClientCoordinator(LoginClientOptions options)
        => this.options = options ?? throw new ArgumentNullException(nameof(options));

    public LoginClientSession Session => session;

    public LoginOutboundDatagram Start(uint connectionId, long nowMilliseconds)
    {
        byte[] request = session.BeginTransport(
            connectionId, options.RequestedMaximumDatagramSize, nowMilliseconds,
            ToMilliseconds(options.SessionRetryInterval), ToMilliseconds(options.SessionTimeout));
        return new(LoginOutboundKind.SessionRequest, request);
    }

    public LoginOutboundDatagram Authenticate(
        string username, string password, long nowMilliseconds)
        => new(LoginOutboundKind.LoginRequest,
            session.CreateLoginDatagram(username, password, options.ClientVersion, nowMilliseconds));

    public IReadOnlyList<LoginOutboundDatagram> AcceptIncoming(ReadOnlySpan<byte> datagram)
    {
        if (session.State == LoginClientState.NegotiatingTransport)
        {
            session.AcceptSessionResponse(datagram);
            return Array.Empty<LoginOutboundDatagram>();
        }

        LoginSessionReceiveResult result = session.AcceptDatagram(datagram);
        if (result.ControlDatagram is null) return Array.Empty<LoginOutboundDatagram>();
        return new[] { new LoginOutboundDatagram(LoginOutboundKind.ReliabilityControl, result.ControlDatagram) };
    }

    public IReadOnlyList<LoginOutboundDatagram> Poll(long nowMilliseconds)
    {
        if (session.State == LoginClientState.NegotiatingTransport)
        {
            byte[]? retry = session.PollHandshake(nowMilliseconds);
            return retry is null
                ? Array.Empty<LoginOutboundDatagram>()
                : new[] { new LoginOutboundDatagram(LoginOutboundKind.SessionRequest, retry) };
        }
        if (session.State is not (LoginClientState.AwaitingResponses or LoginClientState.Complete))
            return Array.Empty<LoginOutboundDatagram>();
        return session.GetRetransmissionDatagrams(
                nowMilliseconds, ToMilliseconds(options.ReliableRetransmitInterval))
            .Select(bytes => new LoginOutboundDatagram(LoginOutboundKind.Retransmission, bytes))
            .ToList();
    }

    public void Dispose() => session.Dispose();

    private static long ToMilliseconds(TimeSpan value)
    {
        double milliseconds = value.TotalMilliseconds;
        if (milliseconds > long.MaxValue) throw new ArgumentOutOfRangeException(nameof(value));
        return checked((long)milliseconds);
    }
}
