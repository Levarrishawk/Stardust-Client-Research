using Stardust.Core.Protocol;

namespace Stardust.Core.Hosting;

public readonly record struct DatagramCounters(ulong SendAttempts, ulong AttemptedSendBytes,
    ulong SendFailures, ulong PeerDatagramsReceived, ulong ReceivedBytes, ulong ForeignDatagramsDropped);

/// <summary>Single-owner I/O accounting, independent of application/reliable message counts.</summary>
public sealed class DatagramAccounting
{
    public DatagramCounters Counters { get; private set; }

    public void RecordSendAttempt(int byteCount)
    {
        ValidateLength(byteCount);
        Counters = Counters with
        {
            SendAttempts = checked(Counters.SendAttempts + 1),
            AttemptedSendBytes = checked(Counters.AttemptedSendBytes + (ulong)byteCount)
        };
    }

    public void RecordSendFailure() => Counters = Counters with
        { SendFailures = checked(Counters.SendFailures + 1) };

    public void RecordPeerReceive(int byteCount)
    {
        ValidateLength(byteCount);
        Counters = Counters with
        {
            PeerDatagramsReceived = checked(Counters.PeerDatagramsReceived + 1),
            ReceivedBytes = checked(Counters.ReceivedBytes + (ulong)byteCount)
        };
    }

    public void RecordForeignReceive() => Counters = Counters with
        { ForeignDatagramsDropped = checked(Counters.ForeignDatagramsDropped + 1) };

    /// <summary>Take this before sending a status reply; uint conversion preserves timestamp wrap.</summary>
    public SoeStatusSnapshot CreateStatusSnapshot(long monotonicMilliseconds)
    {
        if (monotonicMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(monotonicMilliseconds));
        return new(unchecked((uint)monotonicMilliseconds), Counters.SendAttempts, Counters.PeerDatagramsReceived);
    }

    public void Clear() => Counters = default;

    private static void ValidateLength(int byteCount)
    {
        if (byteCount is < 0 or > SoeClientTransport.MaximumWireDatagramSize)
            throw new ArgumentOutOfRangeException(nameof(byteCount));
    }
}
