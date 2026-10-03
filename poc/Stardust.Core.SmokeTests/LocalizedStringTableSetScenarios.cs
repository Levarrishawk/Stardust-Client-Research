using System.Buffers;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class LocalizedStringTableSetScenarios
{
    private sealed record Resource(string Path, string? Hex);
    private sealed record Fixture(string Name, string[] Paths, Resource[] Resources, LocalizedStringTableSetLimits Limits, string Reject, string[] ExpectedCalls);
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static object TableReceipt(LocalizedStringTable table) => new { table.Header, table.KeyPassIds, table.TotalValueUnits, table.TotalKeyBytes, Records = table.Records.Select(r => new { r.Id, r.ValueCrcRaw, KeyHex = Convert.ToHexString(r.Key.CopyBytes()), Units = r.Utf16Units }) };
    private static object Receipt(LocalizedStringTableSet set) => new { set.RequestedPaths, set.MissingPaths, set.SourceBytes, set.TotalRecords, set.TotalValueUnits, set.TotalKeyBytes, Tables = set.Tables.ToDictionary(p => p.Key, p => TableReceipt(p.Value), StringComparer.Ordinal) };
    private static void Emit(string tag, object value) => Console.WriteLine(tag + "=" + JsonSerializer.Serialize(value));
    private sealed class CancelMemory(byte[] buffer, CancellationTokenSource cancellation) : MemoryManager<byte>
    {
        public bool Enabled;
        public override Span<byte> GetSpan() { if (Enabled) cancellation.Cancel(); return buffer; }
        public override MemoryHandle Pin(int elementIndex = 0) => throw new NotSupportedException();
        public override void Unpin() { }
        protected override void Dispose(bool disposing) { }
    }
    internal static void Run(string[] args)
    {
        Check(args.Length == 3, "root/config/preload fixtures required");
        var fixtures = JsonSerializer.Deserialize<Fixture[]>(File.ReadAllBytes(args[2]))!; var cases = new List<object>();
        LocalizedStringTableSet? prior = null;
        foreach (var f in fixtures)
        {
            var resources = f.Resources.ToDictionary(r => r.Path, r => r.Hex is null ? null : Convert.FromHexString(r.Hex), StringComparer.Ordinal); var calls = new List<string>();
            ReadOnlyMemory<byte>? Resolve(string path) { calls.Add(path); if (resources.TryGetValue(path, out var raw) && raw is not null) return new ReadOnlyMemory<byte>(raw); return null; }
            try { var set = LocalizedStringTableSet.Build(f.Paths, Resolve, f.Limits); Check(f.Reject == "None", "Expected failure: " + f.Name); cases.Add(new { f.Name, Reject = "None", Calls = calls, Set = Receipt(set) }); prior ??= set; }
            catch (InvalidDataException error) { Check(f.Reject == "Input", "Unexpected input failure: " + f.Name + ": " + error.Message); cases.Add(new { f.Name, Reject = "Input", Calls = calls }); }
            catch (ArgumentOutOfRangeException) { Check(f.Reject == "Options", "Unexpected option failure: " + f.Name); cases.Add(new { f.Name, Reject = "Options", Calls = calls }); }
            Check(calls.SequenceEqual(f.ExpectedCalls), "Read cardinality/order: " + f.Name);
            foreach (var resource in f.Resources.Where(r => r.Hex is not null)) Check(Convert.ToHexString(resources[resource.Path]!) == resource.Hex, "Caller memory untouched");
        }
        string a = "string/en/a.stf", b = "string/en/b.stf"; byte[] ra = Convert.FromHexString(fixtures[0].Resources[0].Hex!), rb = Convert.FromHexString(fixtures[0].Resources[1].Hex!);
        var limits = fixtures[0].Limits; string fingerprint = JsonSerializer.Serialize(Receipt(prior!)); int checks = 0;
        void Assert(bool value, string message) { Check(value, message); checks++; }
        void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { checks++; return; } throw new Exception("Expected " + typeof(T).Name); }
        ReadOnlyMemory<byte>? Lookup(string path) { if (path == a) return new ReadOnlyMemory<byte>(ra); if (path == b) return new ReadOnlyMemory<byte>(rb); return null; }
        Throws<NotSupportedException>(() => ((IDictionary<string, LocalizedStringTable>)prior!.Tables).Clear());
        Throws<NotSupportedException>(() => ((IList<string>)prior!.MissingPaths).Clear());
        Throws<NotSupportedException>(() => ((IList<string>)prior!.RequestedPaths)[0] = b);
        Assert(ReferenceEquals(prior!.Find(a), prior.Tables[a]), "Canonical pure Find");
        Assert(prior.Find("string/en/missing.stf") is null && prior.Find("string/en/unknown.stf") is null && prior.Find("string/en/A.stf") is null, "Absent/unrequested/case misses");
        Throws<ArgumentException>(() => prior.Find("../bad.stf"));
        var failure = new IOException("controlled"); bool exact = false;
        try { LocalizedStringTableSet.Build(new[] { a, b }, p => p == a ? new ReadOnlyMemory<byte>(ra) : throw failure); } catch (IOException e) { exact = ReferenceEquals(e, failure); }
        Assert(exact, "Later callback exception identity");
        using (var cancelled = new CancellationTokenSource()) { cancelled.Cancel(); int reads = 0; Throws<OperationCanceledException>(() => LocalizedStringTableSet.Build(new[] { a }, p => { reads++; return Lookup(p); }, cancellationToken: cancelled.Token)); Check(reads == 0, "Precancel no reads"); }
        using (var cancelled = new CancellationTokenSource()) { int reads = 0; Throws<OperationCanceledException>(() => LocalizedStringTableSet.Build(new[] { a, b }, p => { reads++; cancelled.Cancel(); return Lookup(p); }, cancellationToken: cancelled.Token)); Check(reads == 1, "Callback cancellation stops later reads"); }
        using (var cancelled = new CancellationTokenSource()) { int reads = 0; Throws<OperationCanceledException>(() => LocalizedStringTableSet.Build(new[] { a, b }, p => { reads++; if (p == b) cancelled.Cancel(); return Lookup(p); }, cancellationToken: cancelled.Token)); Check(reads == 2, "Last callback cancellation rejects publication"); }
        using (var cancelled = new CancellationTokenSource())
        {
            IEnumerable<string> CancelAfterEnumeration() { try { yield return a; } finally { cancelled.Cancel(); } }
            int reads = 0; Throws<OperationCanceledException>(() => LocalizedStringTableSet.Build(CancelAfterEnumeration(), p => { reads++; return Lookup(p); }, cancellationToken: cancelled.Token)); Check(reads == 0, "Final enumeration cancellation before reads");
        }
        Throws<InvalidOperationException>(() => LocalizedStringTableSet.Build(new[] { a }, p => { LocalizedStringTableSet.Build(Array.Empty<string>(), Lookup); return Lookup(p); }));
        Throws<InvalidOperationException>(() => LocalizedStringTableSet.Build(new[] { a }, p => { try { LocalizedStringTableSet.Build(Array.Empty<string>(), Lookup); } catch (InvalidOperationException) { } return Lookup(p); }));
        IEnumerable<string> NestedEnumeration() { try { LocalizedStringTableSet.Build(Array.Empty<string>(), Lookup); } catch (InvalidOperationException) { } yield return a; }
        int nestedReads = 0; Throws<InvalidOperationException>(() => LocalizedStringTableSet.Build(NestedEnumeration(), p => { nestedReads++; return Lookup(p); })); Check(nestedReads == 0, "Enumeration reentry rejects before reads");
        Assert(LocalizedStringTableSet.Build(new[] { a }, Lookup).TotalRecords == 2, "Guard recovery");
        int traversed = 0, infiniteReads = 0; IEnumerable<string> Endless() { for (int i = 0; ; i++) { traversed++; yield return "string/en/a" + i + ".stf"; } }
        Throws<InvalidDataException>(() => LocalizedStringTableSet.Build(Endless(), p => { infiniteReads++; return Lookup(p); }, limits with { MaximumPaths = 1 })); Check(traversed == 2 && infiniteReads == 0, "Bounded enumeration cap plus one");
        var paths = new List<string> { a, b }; var snapshot = LocalizedStringTableSet.Build(paths, p => { paths.Clear(); paths.Add("../bad.stf"); return Lookup(p); });
        Assert(snapshot.RequestedPaths.SequenceEqual(new[] { a, b }) && snapshot.Tables.Count == 2, "Requested paths owned before callbacks");
        byte[] reused = new byte[Math.Max(ra.Length, rb.Length)]; var reusedSet = LocalizedStringTableSet.Build(new[] { a, b }, p => { Array.Clear(reused); byte[] raw = p == a ? ra : rb; raw.CopyTo(reused, 0); return new ReadOnlyMemory<byte>(reused, 0, raw.Length); });
        bool callerUntouched = reused.AsSpan(0, rb.Length).SequenceEqual(rb); Array.Clear(reused);
        Assert(callerUntouched && reusedSet.Find(a)!.ById[0].Utf16Units.SequenceEqual(new ushort[] { 65, 0xD800, 0 }) && reusedSet.Find(b)!.ById[7].Key.CopyBytes().SequenceEqual(new byte[] { 98, 255 }), "Callback buffer reuse owned before next callback");
        using (var cancelled = new CancellationTokenSource())
        {
            using var manager = new CancelMemory(ra, cancelled); ReadOnlyMemory<byte> memory = manager.Memory;
            Throws<OperationCanceledException>(() => LocalizedStringTableSet.Build(new[] { a }, _ => { manager.Enabled = true; return memory; }, cancellationToken: cancelled.Token)); Check(ra.SequenceEqual(Convert.FromHexString(fixtures[0].Resources[0].Hex!)), "Cancellation does not clear caller memory");
        }
        Assert(LocalizedStringTableSet.Build(new[] { a }, p => { var other = Task.Run(() => LocalizedStringTableSet.Build(Array.Empty<string>(), Lookup)).GetAwaiter().GetResult(); Check(other.Tables.Count == 0, "Independent thread callback"); return Lookup(p); }).Tables.Count == 1, "Thread-local guard");
        var keyCopy = prior.Find(a)!.Records[0].Key.CopyBytes(); Array.Clear(keyCopy);
        Assert(JsonSerializer.Serialize(Receipt(prior)) == fingerprint, "Earlier set stable after all failure/copy mutations");
        Check(checks == 21, "Controlled checks accounting");
        Parallel.For(0, 32, _ => Check(JsonSerializer.Serialize(Receipt(LocalizedStringTableSet.Build(fixtures[0].Paths, Lookup, limits))) == fingerprint, "Independent concurrent sets"));
        var originalInputs = new List<object>(); var originalResources = new Dictionary<string, byte[]>(StringComparer.Ordinal); var originalPaths = new[] { "string/en/obj_n.stf", "string/en/terminal_name.stf" };
        using (var config = File.OpenText(args[1])) using (var overlay = ConfiguredTreOverlay.Open(args[0], config))
            foreach (string path in originalPaths)
            {
                var found = overlay.Lookup(path); Check(found.Kind == TreLookupKind.Present && found.Payload is not null && found.Priority == 53, "Original present/priority"); byte[] raw = found.Payload!;
                string pin = Convert.ToHexString(SHA256.HashData(raw)); int size = raw.Length;
                Check(path.EndsWith("obj_n.stf", StringComparison.Ordinal) ? size == 2527 && pin == "3EEA0D2B5E785F593B9C9180877665A495CD67B68BF3D6DAF74F7820FCAFB054" : size == 4610 && pin == "CDEDCD3ECE3E5FE6C7455B70760DDDB047B2F16281D740A1034D465F081FF603", "Original pin");
                originalResources.Add(path, raw); originalInputs.Add(new { Path = path, found.Priority, Bytes = size, Sha256 = pin });
            }
        var originalCalls = new List<string>(); var originalSet = LocalizedStringTableSet.Build(originalPaths, p => { originalCalls.Add(p); return new ReadOnlyMemory<byte>(originalResources[p]); }, new(2, 7137, 97, 1724, 1723));
        foreach (byte[] raw in originalResources.Values) Array.Clear(raw);
        foreach (var table in originalSet.Tables.Values) foreach (var record in table.Records) Check(table.TryGetByKey(record.Key.CopyBytes(), out var match) && ReferenceEquals(match, table.ById[record.Id]), "Original exact key/ID canonical lookup");
        Emit("set_synthetic", new { Cases = cases, ControlledChecks = checks, ConcurrentBuilds = 32, Owned = true });
        Emit("set_original", new { Inputs = originalInputs, Calls = originalCalls, ArchivesClosed = true, Set = Receipt(originalSet) });
        Console.WriteLine("PASS: atomic bounded explicit STF preload, pure owned lookup, absence/budgets/cancellation/reentry and two original tables.");
    }
}
