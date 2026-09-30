using Stardust.Core.Hosting;
using Stardust.Core.Protocol;

internal static class HostAccountingScenarios
{
    internal sealed class ManualClock : IMonotonicClock
    {
        internal long Now { get; set; }
        public long ReadMilliseconds() => Now;
    }

    internal static void Run()
    {
        ManualClock clock = new();
        MonotonicClockGuard guard = new(clock);
        if (guard.ReadMilliseconds() != 0) throw new Exception("Clock origin mismatch.");
        clock.Now = 100;
        if (guard.ReadMilliseconds() != 100 || guard.ReadMilliseconds() != 100)
            throw new Exception("Equal monotonic samples rejected.");
        clock.Now = 99;
        Reject<InvalidOperationException>(() => guard.ReadMilliseconds());
        clock.Now = -1;
        Reject<InvalidOperationException>(() => guard.ReadMilliseconds());
        clock.Now = 101;
        if (guard.ReadMilliseconds() != 101) throw new Exception("Invalid sample changed clock guard.");
        StopwatchMonotonicClock realClock = new();
        long first = realClock.ReadMilliseconds(), second = realClock.ReadMilliseconds();
        if (first < 0 || second < first) throw new Exception("Stopwatch clock regressed.");

        DatagramAccounting accounting = new();
        accounting.RecordSendAttempt(14); // session request actually handed to I/O
        accounting.RecordSendAttempt(14); // retry is another datagram, not another application
        accounting.RecordSendFailure();
        accounting.RecordPeerReceive(17);
        accounting.RecordPeerReceive(7); // damaged peer framing still reaches receive accounting
        accounting.RecordForeignReceive();
        DatagramCounters expected = new(2, 28, 1, 2, 24, 1);
        if (accounting.Counters != expected) throw new Exception("I/O accounting mismatch.");
        SoeStatusSnapshot snapshot = accounting.CreateStatusSnapshot((long)uint.MaxValue + 3);
        if (snapshot != new SoeStatusSnapshot(2, 2, 2)) throw new Exception("Pre-send counter/timestamp snapshot mismatch.");
        accounting.RecordSendAttempt(43);
        if (snapshot.PacketsSent != 2 || accounting.Counters.SendAttempts != 3)
            throw new Exception("Status snapshot included its own later send.");
        foreach (int length in new[] { -1, SoeClientTransport.MaximumWireDatagramSize + 1 })
        {
            DatagramCounters before = accounting.Counters;
            Reject<ArgumentOutOfRangeException>(() => accounting.RecordSendAttempt(length));
            Reject<ArgumentOutOfRangeException>(() => accounting.RecordPeerReceive(length));
            if (accounting.Counters != before) throw new Exception("Invalid length changed accounting.");
        }
        Reject<ArgumentOutOfRangeException>(() => accounting.CreateStatusSnapshot(-1));
        accounting.Clear();
        if (accounting.Counters != default) throw new Exception("Accounting survived reset.");
        Console.WriteLine("PASS: monotonic clock guards, I/O datagram accounting, pre-send snapshots and timestamp wrap");
    }

    internal static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name);
    }
}
