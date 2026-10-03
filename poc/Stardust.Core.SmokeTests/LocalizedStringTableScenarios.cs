using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class LocalizedStringTableScenarios
{
    private sealed record Fixture(string Name, string Hex, bool Reject);
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static object Receipt(LocalizedStringTable t) => new { t.Header, t.KeyPassIds, t.TotalValueUnits, t.TotalKeyBytes, Records = t.Records.Select(r => new { r.Id, r.ValueCrcRaw, KeyHex = Convert.ToHexString(r.Key.CopyBytes()), Units = r.Utf16Units, DisplayKey = r.Key.GetDisplayKey(), DisplayText = r.GetDisplayText() }) };
    private static void Emit(string tag, object value) => Console.WriteLine(tag + "=" + JsonSerializer.Serialize(value));
    internal static void Run(string[] args)
    {
        Check(args.Length == 3, "root/config/independent STF fixtures required");
        var fixtures = JsonSerializer.Deserialize<Fixture[]>(File.ReadAllBytes(args[2]))!; var cases = new List<object>();
        foreach (var fixture in fixtures)
        {
            byte[] raw = Convert.FromHexString(fixture.Hex);
            try { var table = LocalizedStringTable.Load(raw); Array.Clear(raw); Check(!fixture.Reject, "Expected malformed STF: " + fixture.Name); cases.Add(new { fixture.Name, Rejected = false, Table = Receipt(table) }); }
            catch (InvalidDataException) { Check(fixture.Reject, "Unexpected STF rejection: " + fixture.Name); cases.Add(new { fixture.Name, Rejected = true }); }
        }
        byte[] source = Convert.FromHexString(fixtures[0].Hex); var owned = LocalizedStringTable.Load(source); string fingerprint = JsonSerializer.Serialize(Receipt(owned));
        var rawKey = owned.Records[0].Key.CopyBytes(); var units = owned.Records[0].CopyUtf16Units(); var inputKey = (byte[])rawKey.Clone(); var independentKey = new LocalizedStringKey(inputKey); Array.Clear(inputKey); Array.Clear(source); Array.Clear(units);
        Check(owned.ByKey[independentKey].Id == 0 && owned.TryGetByKey(rawKey, out var found) && ReferenceEquals(found, owned.ById[0]), "Exact raw key/index ownership");
        Array.Clear(rawKey); Check(JsonSerializer.Serialize(Receipt(owned)) == fingerprint, "Value/key/source snapshots immutable");
        Check(!owned.TryGetByKey("missing"u8, out _) && !owned.ById.ContainsKey(123), "Unknown ID/key explicit dictionary misses");
        bool readOnly = false; try { ((IList<ushort>)owned.Records[0].Utf16Units)[0] = 65; } catch (NotSupportedException) { readOnly = true; } Check(readOnly, "Read-only raw UTF16 units");
        readOnly = false; try { ((IDictionary<uint, LocalizedStringRecord>)owned.ById).Clear(); } catch (NotSupportedException) { readOnly = true; } Check(readOnly, "Read-only ID map");
        int budget = 0; byte[] good = Convert.FromHexString(fixtures[0].Hex);
        void Reject(Action action) { try { action(); } catch (InvalidDataException) { budget++; return; } throw new Exception("Expected STF budget rejection"); }
        Reject(() => LocalizedStringTable.Load(good, new(MaximumBytes: good.Length - 1)));
        Reject(() => LocalizedStringTable.Load(good, new(MaximumRecords: 1)));
        Reject(() => LocalizedStringTable.Load(good, new(MaximumValueUnits: 8)));
        Reject(() => LocalizedStringTable.Load(good, new(MaximumKeyBytes: 5)));
        Reject(() => LocalizedStringTable.Load(good, new(MaximumTotalValueUnits: 10)));
        Reject(() => LocalizedStringTable.Load(good, new(MaximumTotalKeyBytes: 9)));
        var exact = LocalizedStringTable.Load(good, new(good.Length, 2, 9, 6, 11, 10)); Check(JsonSerializer.Serialize(Receipt(exact)) == fingerprint, "Exact STF limits accepted");
        bool invalidLimits = false; try { LocalizedStringTable.Load(good, new(MaximumRecords: -1)); } catch (ArgumentOutOfRangeException) { invalidLimits = true; } Check(invalidLimits, "Invalid policy settings");
        Parallel.For(0, 32, _ => Check(owned.ByKey[independentKey].Id == 0 && JsonSerializer.Serialize(Receipt(owned)) == fingerprint, "Independent owned lookups"));
        Check(JsonSerializer.Serialize(Receipt(owned)) == fingerprint, "Failed loads preserve prior table");
        var originals = new List<object>(); var inputs = new List<(string Path, int? Priority, byte[] Bytes)>();
        using (var config = File.OpenText(args[1]))
        using (var overlay = ConfiguredTreOverlay.Open(args[0], config))
        {
            Check(overlay.Lookup("string/en/terminal_n.stf").Kind == TreLookupKind.Missing, "Requested terminal_n absent in configured overlay");
            foreach (string path in new[] { "string/en/obj_n.stf", "string/en/terminal_name.stf" }) { var r = overlay.Lookup(path); Check(r.Kind == TreLookupKind.Present && r.Payload is not null, "Original STF present"); inputs.Add((path, r.Priority, r.Payload!)); }
        }
        foreach (var input in inputs)
        {
            string pin = Convert.ToHexString(SHA256.HashData(input.Bytes)); int size = input.Bytes.Length;
            Check(input.Priority == 53 && (input.Path.EndsWith("obj_n.stf", StringComparison.Ordinal) ? size == 2527 && pin == "3EEA0D2B5E785F593B9C9180877665A495CD67B68BF3D6DAF74F7820FCAFB054" : size == 4610 && pin == "CDEDCD3ECE3E5FE6C7455B70760DDDB047B2F16281D740A1034D465F081FF603"), "Original STF source pin");
            var table = LocalizedStringTable.Load(input.Bytes); Array.Clear(input.Bytes); int matches = 0;
            foreach (var record in table.Records) { Check(ReferenceEquals(table.ById[record.Id], record) && table.TryGetByKey(record.Key.CopyBytes(), out var byKey) && ReferenceEquals(byKey, record), "Original exact key/ID relation"); matches++; }
            originals.Add(new { input.Path, input.Priority, Bytes = size, Sha256 = pin, Table = Receipt(table), LookupMatches = matches });
        }
        Emit("stf_synthetic", new { Cases = cases, BudgetNegatives = budget, Owned = true, ExactLimits = true, ConcurrentLookups = 32 });
        Emit("stf_original", new { Tables = originals, ArchiveClosed = true, Owned = true });
        Console.WriteLine("PASS: owned STF variant1 raw UTF16/key bytes, exact dual lookup, bounded atomic failures and two pinned original tables.");
    }
}
