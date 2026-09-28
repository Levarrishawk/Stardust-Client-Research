namespace Stardust.Core.Protocol;

public enum SoeHandshakeState
{
    Idle,
    WaitingForResponse,
    Established,
    TimedOut
}

/// <summary>Monotonic-clock session request retry and timeout policy without socket ownership.</summary>
public sealed class SoeHandshakeScheduler
{
    private readonly SoeClientTransport transport;
    private readonly long retryIntervalMilliseconds;
    private readonly long timeoutMilliseconds;
    private byte[]? request;
    private long startedAt;
    private long nextRetryAt;

    public SoeHandshakeScheduler(
        SoeClientTransport transport, long retryIntervalMilliseconds = 1_000, long timeoutMilliseconds = 15_000)
    {
        if (retryIntervalMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(retryIntervalMilliseconds));
        if (timeoutMilliseconds <= 0 || timeoutMilliseconds < retryIntervalMilliseconds)
            throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds));
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        this.retryIntervalMilliseconds = retryIntervalMilliseconds;
        this.timeoutMilliseconds = timeoutMilliseconds;
    }

    public SoeHandshakeState State { get; private set; }

    public byte[] Start(uint connectionId, uint maximumDatagramSize, long nowMilliseconds)
    {
        if (State != SoeHandshakeState.Idle) throw new InvalidOperationException("Handshake has already started.");
        request = transport.BeginSession(connectionId, maximumDatagramSize);
        startedAt = nowMilliseconds;
        nextRetryAt = checked(nowMilliseconds + retryIntervalMilliseconds);
        State = SoeHandshakeState.WaitingForResponse;
        return (byte[])request.Clone();
    }

    public byte[]? Poll(long nowMilliseconds)
    {
        if (State != SoeHandshakeState.WaitingForResponse) return null;
        if (nowMilliseconds < startedAt) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));
        if (nowMilliseconds - startedAt >= timeoutMilliseconds)
        {
            State = SoeHandshakeState.TimedOut;
            throw new TimeoutException("SOE session response timed out.");
        }
        if (nowMilliseconds < nextRetryAt) return null;
        nextRetryAt = checked(nowMilliseconds + retryIntervalMilliseconds);
        return (byte[])request!.Clone();
    }

    public void AcceptResponse(ReadOnlySpan<byte> response)
    {
        if (State != SoeHandshakeState.WaitingForResponse)
            throw new InvalidOperationException("SOE session response is not expected.");
        transport.AcceptSessionResponse(response);
        State = SoeHandshakeState.Established;
        request = null;
    }
}
