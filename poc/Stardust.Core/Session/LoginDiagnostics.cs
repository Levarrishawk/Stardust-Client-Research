using System.Text.Json;

namespace Stardust.Core.Session;

public enum LoginDiagnosticKind { SessionRequest, LoginRequest, Receive, Retransmission, Deadline, Closed, HostSend, HostOutcome, ForeignSource }
public enum LoginDiagnosticOutcome { Accepted, FramingRejected, Malformed, RemoteFailure, PeerDisconnected, TimedOut, Cancelled, IoFailed, ClockFailed, InvalidInput, Dropped }

/// <summary>Only allowlisted identifiers/counts; no payload, endpoint, username, or error text.</summary>
public sealed record LoginDiagnosticEvent(
    LoginDiagnosticKind Kind, LoginClientState State, LoginDiagnosticOutcome Outcome,
    int DatagramBytes = 0, int ControlCount = 0, int ApplicationCount = 0);

public static class LoginDiagnostics
{
    public static string Encode(LoginDiagnosticEvent value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!Enum.IsDefined(value.Kind) || !Enum.IsDefined(value.State) || !Enum.IsDefined(value.Outcome) ||
            value.DatagramBytes < 0 || value.ControlCount < 0 || value.ApplicationCount < 0)
            throw new ArgumentOutOfRangeException(nameof(value));
        return JsonSerializer.Serialize(new
        {
            kind = value.Kind.ToString(), state = value.State.ToString(), outcome = value.Outcome.ToString(),
            datagram_bytes = value.DatagramBytes, controls = value.ControlCount, applications = value.ApplicationCount
        });
    }
}
