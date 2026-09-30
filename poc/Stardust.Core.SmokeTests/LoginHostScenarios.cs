using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Channels;
using Stardust.Core.Configuration;
using Stardust.Core.Hosting;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

internal static class LoginHostScenarios
{
    internal static readonly DatagramPeer Peer = new(0xC0000201, 1); // documentation-only IPv4
    internal static LoginClientOptions Options => new("peer.invalid", 1, "20260817-SD:01");

    internal sealed class ManualTicks : IHostTickSource
    {
        private readonly Channel<bool> channel = Channel.CreateUnbounded<bool>();
        public ValueTask WaitAsync(CancellationToken token) => WaitCore(token);
        private async ValueTask WaitCore(CancellationToken token) => await channel.Reader.ReadAsync(token);
        internal void Pulse() => channel.Writer.TryWrite(true);
    }

    internal sealed class ScriptedPort : IDatagramPort
    {
        private readonly Channel<InboundDatagram> inbound = Channel.CreateUnbounded<InboundDatagram>();
        private readonly Channel<int> sent = Channel.CreateUnbounded<int>();
        private int activeReceives;
        internal int ReceiveCalls { get; private set; }
        internal int MaximumActiveReceives { get; private set; }
        internal int DisposeCalls { get; private set; }
        internal int SendCalls { get; private set; }
        internal int FailSendAt { get; set; }
        internal int BlockSendAt { get; set; }
        internal bool FailReceive { get; set; }
        internal bool FailDispose { get; set; }
        internal Action<int>? OnSend { get; set; }
        internal List<byte[]> WireCopies { get; } = new();
        internal List<byte[]> BorrowedBuffers { get; } = new();
        internal List<byte[]> ReceiveBuffers { get; } = new();
        internal TaskCompletionSource Receiving { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ValueTask SendAsync(DatagramPeer destination, ReadOnlyMemory<byte> bytes, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (destination != Peer) throw new Exception("Host changed pinned destination.");
            SendCalls++;
            if (MemoryMarshal.TryGetArray(bytes, out ArraySegment<byte> segment)) BorrowedBuffers.Add(segment.Array!);
            WireCopies.Add(bytes.ToArray());
            sent.Writer.TryWrite(SendCalls);
            if (SendCalls == FailSendAt) throw new IOException("synthetic private endpoint/token text");
            if (SendCalls == BlockSendAt) return new(Task.Delay(Timeout.Infinite, token));
            OnSend?.Invoke(SendCalls);
            return ValueTask.CompletedTask;
        }
        public async ValueTask<InboundDatagram> ReceiveAsync(CancellationToken token)
        {
            ReceiveCalls++;
            int active = Interlocked.Increment(ref activeReceives);
            MaximumActiveReceives = Math.Max(MaximumActiveReceives, active);
            Receiving.TrySetResult();
            try
            {
                if (FailReceive) throw new IOException("synthetic private receive endpoint");
                return await inbound.Reader.ReadAsync(token);
            }
            finally { Interlocked.Decrement(ref activeReceives); }
        }
        internal void Enqueue(byte[] bytes, DatagramPeer? source = null)
        {
            ReceiveBuffers.Add(bytes);
            if (!inbound.Writer.TryWrite(new(source ?? Peer, bytes))) throw new Exception("Scripted port closed.");
        }
        internal async Task WaitForSend(int count)
        {
            while (await sent.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)) < count) { }
        }
        public ValueTask DisposeAsync()
        {
            DisposeCalls++;
            inbound.Writer.TryComplete();
            while (inbound.Reader.TryRead(out InboundDatagram? pending)) CryptographicOperations.ZeroMemory(pending.Bytes);
            if (FailDispose) throw new IOException("synthetic private dispose endpoint");
            return ValueTask.CompletedTask;
        }
        internal void AssertCleared()
        {
            if (BorrowedBuffers.Concat(ReceiveBuffers).Any(bytes => bytes.Any(value => value != 0)))
                throw new Exception("Host left an owned or borrowed wire buffer uncleared.");
        }
    }

    internal static byte[] Data(ushort sequence, byte[] body)
    {
        byte[] plain = new byte[body.Length + 4];
        plain[1] = 9;
        BinaryPrimitives.WriteUInt16BigEndian(plain.AsSpan(2), sequence);
        body.CopyTo(plain, 4);
        return IndependentPeerScenarios.Protect(plain);
    }

    internal static async Task RunAsync(byte[] response, byte[] token, byte[] clusters,
        byte[] statuses, byte[] characters, byte[] error)
    {
        await SuccessAsync(response, token, clusters, statuses, characters);
        await CancellationAsync();
        await HandshakeDeadlineAsync();
        await PartialDeadlineAsync(response, token);
        await FailureAsync(response, error, LoginHostOutcome.RemoteRejected);
        await FailureAsync(response, new byte[] { 0, 1, 2 }, LoginHostOutcome.Malformed);
        await SendFailureAsync();
        await ClockFailureAsync();
        await SendCancellationAsync();
        await ReceiveFailureAsync();
        await DisposalAndInputAsync();
        await FailureAsync(response, Convert.FromHexString("0005123456780006"), LoginHostOutcome.PeerDisconnected, transportControl: true);
        Console.WriteLine("PASS: injected login host endpoint filtering, retries/deadlines, owned completion, cancellation and sanitized I/O failure");
    }

    private static async Task SuccessAsync(byte[] response, byte[] token, byte[] clusters, byte[] statuses, byte[] characters)
    {
        ScriptedPort port = new();
        port.OnSend = count =>
        {
            if (count == 1)
            {
                port.Enqueue((byte[])response.Clone(), Peer with { Address = Peer.Address + 1 });
                port.Enqueue((byte[])response.Clone(), Peer with { Port = 2 });
                port.Enqueue(new byte[] { 0, 2 }); // malformed pinned response, retry remains live
                port.Enqueue((byte[])response.Clone());
            }
            if (count == 2)
            {
                byte[] damaged = Data(0, token); damaged[^1] ^= 1;
                port.Enqueue(damaged);
                port.Enqueue(Data(2, statuses));
                port.Enqueue(Data(3, characters));
                port.Enqueue(Data(0, token));
                port.Enqueue(Data(1, clusters));
            }
        };
        await using LoginDatagramHost host = new(Options, Peer, port, new HostAccountingScenarios.ManualClock(), new ManualTicks());
        using LoginConnectionResult result = await host.RunAsync(0x12345678, "u", "p").WaitAsync(TimeSpan.FromSeconds(5));
        if (host.State != LoginClientState.Closed || host.Outcome != LoginHostOutcome.Completed ||
            result.AccountId != 0x11223344 || result.CredentialLength != 5 || result.Clusters.Count == 0)
            throw new Exception("Host completion transfer mismatch.");
        if (result.Counters.SendAttempts != 6 || result.Counters.PeerDatagramsReceived != 7 ||
            result.Counters.ForeignDatagramsDropped != 2 || port.MaximumActiveReceives != 1 || port.DisposeCalls != 1)
            throw new Exception("Host I/O/filter accounting mismatch.");
        byte[] credential = result.CopyCredential();
        if (!credential.AsSpan().SequenceEqual(Convert.FromHexString("5444332211"))) throw new Exception("Transferred credential mismatch.");
        CryptographicOperations.ZeroMemory(credential);
        port.AssertCleared();
        result.Dispose();
        if (result.CredentialLength != 0) throw new Exception("Result disposal retained credential.");
        HostAccountingScenarios.Reject<ObjectDisposedException>(() => result.CopyCredential());
        if (result.ToString().Contains("11223344") || result.ToString().Contains("peer.invalid"))
            throw new Exception("Host result summary exposed identity/endpoint.");
    }

    private static async Task CancellationAsync()
    {
        ScriptedPort port = new();
        LoginDatagramHost host = new(Options, Peer, port, new HostAccountingScenarios.ManualClock(), new ManualTicks());
        Task<LoginConnectionResult> running = host.RunAsync(0x12345678, "u", "p");
        await port.Receiving.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await host.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        try { await running; throw new Exception("Pending receive was not cancelled."); }
        catch (OperationCanceledException) { }
        if (host.Outcome != LoginHostOutcome.Cancelled || host.State != LoginClientState.Closed || port.DisposeCalls != 1)
            throw new Exception("Cancellation cleanup mismatch.");
        port.AssertCleared();
        await host.DisposeAsync();
        if (port.DisposeCalls != 1) throw new Exception("Port disposed twice.");
    }

    private static async Task HandshakeDeadlineAsync()
    {
        ScriptedPort port = new(); ManualTicks ticks = new(); HostAccountingScenarios.ManualClock clock = new();
        await using LoginDatagramHost host = new(Options, Peer, port, clock, ticks);
        Task<LoginConnectionResult> running = host.RunAsync(0x12345678, "u", "p");
        await port.Receiving.Task.WaitAsync(TimeSpan.FromSeconds(5));
        clock.Now = 1000; ticks.Pulse();
        await port.WaitForSend(2);
        if (!port.WireCopies[0].AsSpan().SequenceEqual(port.WireCopies[1])) throw new Exception("Host handshake retry changed bytes.");
        clock.Now = 15000; ticks.Pulse();
        await ExpectFailureAsync(running, LoginHostOutcome.TimedOut);
        if (port.ReceiveCalls != 1 || port.MaximumActiveReceives != 1) throw new Exception("Ticks restarted the pending receive.");
        port.AssertCleared();
    }

    private static async Task PartialDeadlineAsync(byte[] response, byte[] token)
    {
        ScriptedPort port = new(); ManualTicks ticks = new(); HostAccountingScenarios.ManualClock clock = new();
        port.OnSend = count =>
        {
            if (count == 1) port.Enqueue((byte[])response.Clone());
            if (count == 2) port.Enqueue(Data(0, token));
        };
        await using LoginDatagramHost host = new(Options, Peer, port, clock, ticks);
        Task<LoginConnectionResult> running = host.RunAsync(0x12345678, "u", "p");
        await port.WaitForSend(3);
        clock.Now = 30000; ticks.Pulse();
        await ExpectFailureAsync(running, LoginHostOutcome.TimedOut);
        if (host.State != LoginClientState.Closed) throw new Exception("Partial deadline retained session.");
        port.AssertCleared();
    }

    private static async Task FailureAsync(byte[] response, byte[] application, LoginHostOutcome expected, bool transportControl = false)
    {
        ScriptedPort port = new();
        port.OnSend = count =>
        {
            if (count == 1) port.Enqueue((byte[])response.Clone());
            if (count == 2) port.Enqueue(transportControl ? IndependentPeerScenarios.Protect(application) : Data(0, application));
        };
        await using LoginDatagramHost host = new(Options, Peer, port, new HostAccountingScenarios.ManualClock(), new ManualTicks());
        await ExpectFailureAsync(host.RunAsync(0x12345678, "u", "p"), expected);
        if (host.State != LoginClientState.Closed) throw new Exception("Failure retained session.");
        port.AssertCleared();
    }

    private static async Task SendFailureAsync()
    {
        ScriptedPort port = new() { FailSendAt = 1 };
        await using LoginDatagramHost host = new(Options, Peer, port, new HostAccountingScenarios.ManualClock(), new ManualTicks());
        await ExpectFailureAsync(host.RunAsync(0x12345678, "u", "p"), LoginHostOutcome.IoFailure);
        if (host.Counters.SendFailures != 1 || host.Counters.SendAttempts != 1) throw new Exception("Failed I/O attempt not counted.");
        port.AssertCleared();
    }

    private static async Task ClockFailureAsync()
    {
        ScriptedPort port = new(); HostAccountingScenarios.ManualClock clock = new() { Now = -1 };
        await using LoginDatagramHost host = new(Options, Peer, port, clock, new ManualTicks());
        await ExpectFailureAsync(host.RunAsync(0x12345678, "u", "p"), LoginHostOutcome.ClockFailure);
        if (port.SendCalls != 0) throw new Exception("Invalid clock mutated/sent protocol state.");
    }

    private static async Task SendCancellationAsync()
    {
        ScriptedPort port = new() { BlockSendAt = 1 };
        LoginDatagramHost host = new(Options, Peer, port, new HostAccountingScenarios.ManualClock(), new ManualTicks());
        Task<LoginConnectionResult> running = host.RunAsync(0x12345678, "u", "p");
        await port.WaitForSend(1);
        await host.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        try { await running; throw new Exception("Pending send was not cancelled."); }
        catch (OperationCanceledException) { }
        if (host.Counters.SendAttempts != 1 || host.Counters.SendFailures != 1 || port.ReceiveCalls != 0)
            throw new Exception("Cancelled send ownership/accounting mismatch.");
        port.AssertCleared();
    }

    private static async Task ReceiveFailureAsync()
    {
        ScriptedPort port = new() { FailReceive = true, FailDispose = true };
        await using LoginDatagramHost host = new(Options, Peer, port, new HostAccountingScenarios.ManualClock(), new ManualTicks());
        await ExpectFailureAsync(host.RunAsync(0x12345678, "u", "p"), LoginHostOutcome.IoFailure);
        if (host.CleanupFailures != 1 || host.State != LoginClientState.Closed) throw new Exception("Cleanup failure masked original I/O outcome.");
        port.AssertCleared();
    }

    private static async Task DisposalAndInputAsync()
    {
        ScriptedPort untouched = new();
        LoginDatagramHost neverStarted = new(Options, Peer, untouched);
        await neverStarted.DisposeAsync(); await neverStarted.DisposeAsync();
        if (untouched.DisposeCalls != 1 || untouched.SendCalls != 0) throw new Exception("Unused host disposal not idempotent.");
        try { await neverStarted.RunAsync(1, "u", "p"); throw new Exception("Disposed host started."); }
        catch (ObjectDisposedException) { }
        ScriptedPort invalidPort = new();
        await using LoginDatagramHost invalid = new(Options, Peer, invalidPort);
        await ExpectFailureAsync(invalid.RunAsync(1, "unicode-\u2603", "p"), LoginHostOutcome.InvalidInput);
        if (invalidPort.SendCalls != 0 || invalidPort.DisposeCalls != 1) throw new Exception("Invalid credentials produced network I/O.");

        ScriptedPort activePort = new();
        LoginDatagramHost active = new(Options, Peer, activePort, new HostAccountingScenarios.ManualClock(), new ManualTicks());
        Task<LoginConnectionResult> running = active.RunAsync(1, "u", "p");
        await activePort.Receiving.Task.WaitAsync(TimeSpan.FromSeconds(5));
        try { await active.RunAsync(1, "u", "p"); throw new Exception("Concurrent host run accepted."); }
        catch (InvalidOperationException) { }
        await active.DisposeAsync();
        try { await running; }
        catch (OperationCanceledException) { }
    }

    private static async Task ExpectFailureAsync(Task<LoginConnectionResult> running, LoginHostOutcome expected)
    {
        try { using LoginConnectionResult result = await running.WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (LoginHostFailureException failure)
        {
            if (failure.Outcome != expected || failure.InnerException is not null || failure.ToString().Contains("synthetic private"))
                throw new Exception("Host failure category/redaction mismatch.");
            return;
        }
        throw new Exception("Expected host failure " + expected);
    }
}
