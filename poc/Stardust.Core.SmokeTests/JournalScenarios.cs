using System.Text;
using System.Text.Json;
using Stardust.Core.Hosting;
using Stardust.Core.Session;

internal static class JournalScenarios
{
    private static readonly LoginDiagnosticEvent Event = new(LoginDiagnosticKind.Receive,
        LoginClientState.AwaitingResponses, LoginDiagnosticOutcome.Accepted, 43, 1, 2);

    internal static async Task RunAsync()
    {
        byte[] expected = Encoding.UTF8.GetBytes(LoginDiagnostics.Encode(Event) + "\n");
        using MemoryStream stream = new();
        using (BoundedLoginJournal journal = new(stream, leaveOpen: true, maximumBytes: expected.Length, maximumEvents: 2))
        {
            journal.Write(Event); journal.Write(Event);
            if (journal.WrittenEvents != 1 || journal.WrittenBytes != expected.Length || journal.DroppedEvents != 1)
                throw new Exception("Journal byte limit split or admitted an extra record.");
            journal.Flush();
        }
        if (!stream.ToArray().AsSpan().SequenceEqual(expected)) throw new Exception("Journal JSONL bytes mismatch.");
        AssertAllowlisted(stream.ToArray());

        using MemoryStream eventStream = new();
        using (BoundedLoginJournal journal = new(eventStream, true, maximumEvents: 1))
        {
            journal.Write(Event); journal.Write(Event);
            if (journal.WrittenEvents != 1 || journal.DroppedEvents != 1) throw new Exception("Journal event cap failed.");
        }
        using MemoryStream tooSmall = new();
        using (BoundedLoginJournal journal = new(tooSmall, true, maximumBytes: 1))
        {
            journal.Write(Event);
            if (journal.WrittenEvents != 0 || tooSmall.Length != 0) throw new Exception("Journal wrote a partial capped record.");
        }
        using PartialFailureStream partial = new();
        using (BoundedLoginJournal journal = new(partial, true))
        {
            journal.Write(Event); journal.Write(Event);
            if (!journal.HasUncertainTail || journal.IoFailures != 1 || journal.WrittenEvents != 0 || journal.DroppedEvents != 1 || partial.Length != 5)
                throw new Exception("Partial write failure allowed subsequent JSON records.");
        }
        using FlushFailureStream flush = new();
        BoundedLoginJournal flushing = new(flush, true);
        flushing.Write(Event); flushing.Dispose();
        if (flushing.IoFailures != 1 || !flushing.HasUncertainTail) throw new Exception("Journal flush failure was hidden.");
        using MemoryStream nonempty = new(new byte[] { 1 });
        HostAccountingScenarios.Reject<ArgumentException>(() => new BoundedLoginJournal(nonempty));
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(() => new BoundedLoginJournal(new MemoryStream(), maximumBytes: 0));

        string path = Path.Combine(Path.GetTempPath(), "stardust-journal-" + Guid.NewGuid().ToString("N") + ".jsonl");
        try
        {
            using (BoundedLoginJournal persisted = BoundedLoginJournal.CreateNew(path)) persisted.Write(Event);
            if (!File.ReadAllBytes(path).AsSpan().SequenceEqual(expected)) throw new Exception("Persisted journal mismatch.");
            HostAccountingScenarios.Reject<IOException>(() => BoundedLoginJournal.CreateNew(path));
            if (!File.ReadAllBytes(path).AsSpan().SequenceEqual(expected)) throw new Exception("Existing journal was overwritten.");
        }
        finally { File.Delete(path); }

        using MemoryStream failureJournal = new();
        using BoundedLoginJournal sink = new(failureJournal, true);
        LoginHostScenarios.ScriptedPort port = new() { FailSendAt = 1 };
        await using LoginDatagramHost host = new(LoginHostScenarios.Options, LoginHostScenarios.Peer, port,
            new HostAccountingScenarios.ManualClock(), new LoginHostScenarios.ManualTicks(), sink.Write);
        try { using LoginConnectionResult result = await host.RunAsync(0x12345678, "SYNTHETIC_PRIVATE_USER", "SYNTHETIC_PRIVATE_PASSWORD"); }
        catch (LoginHostFailureException failure) when (failure.Outcome == LoginHostOutcome.IoFailure) { }
        if (host.Outcome != LoginHostOutcome.IoFailure) throw new Exception("Journal changed host failure outcome.");
        sink.Flush();
        string text = Encoding.UTF8.GetString(failureJournal.ToArray());
        if (!text.Contains("HostOutcome") || !text.Contains("IoFailed") ||
            text.Contains("SYNTHETIC_PRIVATE") || text.Contains("synthetic private") || text.Contains("peer.invalid"))
            throw new Exception("Persisted host failure classification/redaction mismatch.");
        AssertAllowlisted(failureJournal.ToArray());
        Console.WriteLine("PASS: bounded persisted allowlisted JSONL, record caps, no-overwrite, partial-I/O stop and host-failure redaction");
    }

    private static void AssertAllowlisted(byte[] bytes)
    {
        string[] keys = { "kind", "state", "outcome", "datagram_bytes", "controls", "applications" };
        foreach (string line in Encoding.UTF8.GetString(bytes).Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            using JsonDocument record = JsonDocument.Parse(line);
            string[] actual = record.RootElement.EnumerateObject().Select(property => property.Name).ToArray();
            if (!actual.Order().SequenceEqual(keys.Order())) throw new Exception("Journal schema grew outside allowlist.");
        }
    }

    private sealed class PartialFailureStream : MemoryStream
    {
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            base.Write(buffer[..5]);
            throw new IOException("synthetic private exception content");
        }
    }

    private sealed class FlushFailureStream : MemoryStream
    {
        public override void Flush() => throw new IOException("synthetic private flush content");
    }
}
