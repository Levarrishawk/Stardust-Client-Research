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
    private readonly Action<LoginDiagnosticEvent>? diagnostics;

    public LoginClientCoordinator(LoginClientOptions options, Action<LoginDiagnosticEvent>? diagnostics = null)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.diagnostics = diagnostics;
    }

    public LoginClientSession Session => session;
    public int DiagnosticFailures { get; private set; }

    public LoginOutboundDatagram Start(uint connectionId, long nowMilliseconds)
    {
        byte[] request = session.BeginTransport(
            connectionId, options.RequestedMaximumDatagramSize, nowMilliseconds,
            ToMilliseconds(options.SessionRetryInterval), ToMilliseconds(options.SessionTimeout));
        Emit(new(LoginDiagnosticKind.SessionRequest, session.State, LoginDiagnosticOutcome.Accepted, request.Length));
        return new(LoginOutboundKind.SessionRequest, request);
    }

    public LoginOutboundDatagram Authenticate(
        string username, string password, long nowMilliseconds)
    {
        byte[] request = session.CreateLoginDatagram(username, password, options.ClientVersion, nowMilliseconds);
        Emit(new(LoginDiagnosticKind.LoginRequest, session.State, LoginDiagnosticOutcome.Accepted, request.Length));
        return new(LoginOutboundKind.LoginRequest, request);
    }

    public IReadOnlyList<LoginOutboundDatagram> AcceptIncoming(ReadOnlySpan<byte> datagram)
    {
        try
        {
            if (session.State == LoginClientState.NegotiatingTransport)
            {
                session.AcceptSessionResponse(datagram);
                Emit(new(LoginDiagnosticKind.Receive, session.State, LoginDiagnosticOutcome.Accepted, datagram.Length));
                return Array.Empty<LoginOutboundDatagram>();
            }

            LoginSessionReceiveResult result = session.AcceptDatagram(datagram);
            LoginDiagnosticOutcome outcome = result.RejectedFraming ? LoginDiagnosticOutcome.FramingRejected :
                result.PeerDisconnected ? LoginDiagnosticOutcome.PeerDisconnected :
                session.State == LoginClientState.Failed ? LoginDiagnosticOutcome.RemoteFailure : LoginDiagnosticOutcome.Accepted;
            Emit(new(LoginDiagnosticKind.Receive, session.State, outcome, datagram.Length,
                result.ControlDatagrams.Count, result.ApplicationOpcodes.Count));
            return result.ControlDatagrams.Select(bytes =>
                new LoginOutboundDatagram(LoginOutboundKind.ReliabilityControl, bytes)).ToList();
        }
        catch (InvalidDataException)
        {
            Emit(new(LoginDiagnosticKind.Receive, session.State, LoginDiagnosticOutcome.Malformed, datagram.Length));
            throw;
        }
    }

    public IReadOnlyList<LoginOutboundDatagram> Poll(long nowMilliseconds)
    {
        try
        {
            if (session.State == LoginClientState.NegotiatingTransport)
            {
                byte[]? retry = session.PollHandshake(nowMilliseconds);
                if (retry is not null)
                    Emit(new(LoginDiagnosticKind.SessionRequest, session.State, LoginDiagnosticOutcome.Accepted, retry.Length));
                return retry is null
                    ? Array.Empty<LoginOutboundDatagram>()
                    : new[] { new LoginOutboundDatagram(LoginOutboundKind.SessionRequest, retry) };
            }
            if (session.State is not (LoginClientState.AwaitingResponses or LoginClientState.Complete))
                return Array.Empty<LoginOutboundDatagram>();
            session.CheckResponseDeadline(nowMilliseconds, ToMilliseconds(options.LoginResponseTimeout));
            IReadOnlyList<LoginOutboundDatagram> retries = session.GetRetransmissionDatagrams(
                    nowMilliseconds, ToMilliseconds(options.ReliableRetransmitInterval))
                .Select(bytes => new LoginOutboundDatagram(LoginOutboundKind.Retransmission, bytes))
                .ToList();
            foreach (LoginOutboundDatagram retry in retries)
                Emit(new(LoginDiagnosticKind.Retransmission, session.State, LoginDiagnosticOutcome.Accepted, retry.Bytes.Length));
            return retries;
        }
        catch (TimeoutException)
        {
            Emit(new(LoginDiagnosticKind.Deadline, session.State, LoginDiagnosticOutcome.TimedOut));
            throw;
        }
    }

    public void Dispose()
    {
        if (session.State == LoginClientState.Closed) return;
        bool cancelled = session.State is not (LoginClientState.Complete or LoginClientState.Failed);
        session.Dispose();
        Emit(new(LoginDiagnosticKind.Closed, session.State,
            cancelled ? LoginDiagnosticOutcome.Cancelled : LoginDiagnosticOutcome.Accepted));
    }

    private void Emit(LoginDiagnosticEvent value)
    {
        try { diagnostics?.Invoke(value); }
        catch (Exception) { if (DiagnosticFailures < int.MaxValue) DiagnosticFailures++; }
    }

    private static long ToMilliseconds(TimeSpan value)
    {
        double milliseconds = value.TotalMilliseconds;
        if (milliseconds > long.MaxValue) throw new ArgumentOutOfRangeException(nameof(value));
        return checked((long)milliseconds);
    }
}
