using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class SharedTemplateLocalizedNameScenarios
{
    private sealed record Resource(string Path, string Kind, string? Hex);
    private sealed record Fixture(string Name, string RootPath, string RootSha256, Resource[] Resources,
        SharedTemplateLocalizedNameLimits Limits, bool FollowParents, string Reject, string[] ExpectedCalls);
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static object? Pair(SharedTemplateLocalizedNamePair? p) => p is null ? null : new { TableHex = Convert.ToHexString(p.Table.CopyBytes()), KeyHex = Convert.ToHexString(p.Key.CopyBytes()), p.IsEmptyPair, p.Field };
    private static object Receipt(SharedTemplateLocalizedNameProfile p) => new { p.RootPath, ObjectName = Pair(p.ObjectName), DetailedDescription = Pair(p.DetailedDescription), p.Complete,
        Visits = p.Visits.Select(v => new { v.SourcePath, v.Version, v.ParentPath, ObjectNameKind = v.ObjectNameKind.ToString(), DetailedDescriptionKind = v.DetailedDescriptionKind.ToString() }), p.Sources, p.SourceBytes, Stop = p.Stop.ToString() };
    private static void Emit(string tag, object value) => Console.WriteLine(tag + "=" + JsonSerializer.Serialize(value));
    internal static void Run(string[] args)
    {
        Check(args.Length == 3, "root/config/template-name fixtures required");
        var fixtures = JsonSerializer.Deserialize<Fixture[]>(File.ReadAllBytes(args[2]))!; var cases = new List<object>();
        foreach (var f in fixtures)
        {
            var resources = f.Resources.ToDictionary(r => r.Path, r => new TreLookupResult(Enum.Parse<TreLookupKind>(r.Kind), r.Hex is null ? null : Convert.FromHexString(r.Hex), 77), StringComparer.Ordinal); var calls = new List<string>();
            TreLookupResult Lookup(string path) { calls.Add(path); return resources.GetValueOrDefault(path, new(TreLookupKind.Missing)); }
            try { var profile = SharedTemplateLocalizedNameProfile.Load(Lookup, f.RootPath, f.RootSha256, f.Limits, f.FollowParents); Check(f.Reject == "None", "Expected reject: " + f.Name); cases.Add(new { f.Name, Reject = "None", Calls = calls, Profile = Receipt(profile) }); }
            catch (InvalidDataException error) { Check(f.Reject == "Input", "Unexpected input reject: " + f.Name + ": " + error.Message); cases.Add(new { f.Name, Reject = "Input", Calls = calls }); }
            catch (AppearanceGraphBudgetException error) { Check(f.Reject == "Input", "Unexpected source budget reject: " + f.Name + ": " + error.Message); cases.Add(new { f.Name, Reject = "Input", Calls = calls }); }
            catch (ArgumentOutOfRangeException) { Check(f.Reject == "Options", "Unexpected options reject: " + f.Name); cases.Add(new { f.Name, Reject = "Options", Calls = calls }); }
            Check(calls.SequenceEqual(f.ExpectedCalls), "Callback cardinality/order: " + f.Name);
            foreach (var r in f.Resources.Where(r => r.Hex is not null)) Check(Convert.ToHexString(resources[r.Path].Payload!) == r.Hex, "Caller bytes untouched");
        }
        var inherited = fixtures.Single(f => f.Name == "inherited-name"); var complete = fixtures.Single(f => f.Name == "version-0007");
        Dictionary<string, byte[]> Bytes(Fixture f) => f.Resources.ToDictionary(r => r.Path, r => Convert.FromHexString(r.Hex!), StringComparer.Ordinal);
        var completeBytes = Bytes(complete); TreLookupResult Ready(string path) => new(TreLookupKind.Present, completeBytes[path], 77);
        var prior = SharedTemplateLocalizedNameProfile.Load(Ready, complete.RootPath, complete.RootSha256); string fingerprint = JsonSerializer.Serialize(Receipt(prior)); int checks = 0;
        void Assert(bool value, string message) { Check(value, message); checks++; }
        void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { checks++; return; } throw new Exception("Expected " + typeof(T).Name); }
        var borrowed = Bytes(inherited); var owned = SharedTemplateLocalizedNameProfile.Load(p => { if (p != inherited.RootPath) Array.Clear(borrowed[inherited.RootPath]); return new(TreLookupKind.Present, borrowed[p], 77); }, inherited.RootPath, inherited.RootSha256);
        foreach (byte[] raw in borrowed.Values) Array.Clear(raw); byte[] tableCopy = owned.ObjectName!.Table.CopyBytes(); Array.Clear(tableCopy);
        Assert(owned.ObjectName.Table.CopyBytes().SequenceEqual("tab"u8.ToArray()) && owned.ObjectName.Key.CopyBytes().SequenceEqual("value"u8.ToArray()) && owned.DetailedDescription!.Table.CopyBytes().SequenceEqual("desc"u8.ToArray()), "Owned pair before parent callbacks/source reuse");
        Throws<NotSupportedException>(() => ((IList<SharedTemplateLocalizedNameVisit>)prior.Visits).Clear());
        Throws<NotSupportedException>(() => ((IDictionary<string, AssetSourceIdentity>)prior.Sources).Clear());
        var failure = new IOException("controlled"); bool exact = false; var fresh = Bytes(inherited);
        try { SharedTemplateLocalizedNameProfile.Load(p => p == inherited.RootPath ? new(TreLookupKind.Present, fresh[p], 77) : throw failure, inherited.RootPath, inherited.RootSha256); } catch (IOException e) { exact = ReferenceEquals(e, failure); }
        Assert(exact, "Later callback exception identity");
        Throws<InvalidOperationException>(() => SharedTemplateLocalizedNameProfile.Load(p => { try { SharedTemplateLocalizedNameProfile.Load(Ready, complete.RootPath, complete.RootSha256); } catch (InvalidOperationException) { } return Ready(p); }, complete.RootPath, complete.RootSha256));
        Throws<InvalidOperationException>(() => SharedTemplateLocalizedNameProfile.Load(p => { SharedTemplateLocalizedNameProfile.Load(Ready, complete.RootPath, complete.RootSha256); return Ready(p); }, complete.RootPath, complete.RootSha256));
        using (var cancelled = new CancellationTokenSource()) { cancelled.Cancel(); int reads = 0; Throws<OperationCanceledException>(() => SharedTemplateLocalizedNameProfile.Load(p => { reads++; return Ready(p); }, complete.RootPath, complete.RootSha256, cancellationToken: cancelled.Token)); Check(reads == 0, "Precancel no reads"); }
        using (var cancelled = new CancellationTokenSource()) { int reads = 0; Throws<OperationCanceledException>(() => SharedTemplateLocalizedNameProfile.Load(p => { reads++; if (p != inherited.RootPath) cancelled.Cancel(); return new(TreLookupKind.Present, fresh[p], 77); }, inherited.RootPath, inherited.RootSha256, cancellationToken: cancelled.Token)); Check(reads == 2, "Parent cancellation no publication"); }
        using (var cancelled = new CancellationTokenSource()) { int reads = 0; Throws<OperationCanceledException>(() => SharedTemplateLocalizedNameProfile.Load(p => { reads++; cancelled.Cancel(); return Ready(p); }, complete.RootPath, complete.RootSha256, cancellationToken: cancelled.Token)); Check(reads == 1, "Root callback cancellation"); }
        Assert(JsonSerializer.Serialize(Receipt(SharedTemplateLocalizedNameProfile.Load(Ready, complete.RootPath, complete.RootSha256))) == fingerprint && JsonSerializer.Serialize(Receipt(prior)) == fingerprint, "Guard recovery/prior result stable");
        int disabledReads = 0; var disabled = SharedTemplateLocalizedNameProfile.Load(p => { disabledReads++; return new(TreLookupKind.Present, fresh[p], 77); }, inherited.RootPath, inherited.RootSha256, followParents: false);
        Assert(disabledReads == 1 && disabled.ObjectName is null && disabled.DetailedDescription is not null && disabled.Stop == SharedTemplateLocalizedNameStop.ParentTraversalDisabled, "Explicitly disabled unresolved parent traversal");
        Assert(SharedTemplateLocalizedNameProfile.Load(p => { var other = Task.Run(() => SharedTemplateLocalizedNameProfile.Load(Ready, complete.RootPath, complete.RootSha256)).GetAwaiter().GetResult(); Check(other.Complete, "Independent callback thread"); return Ready(p); }, complete.RootPath, complete.RootSha256).Complete, "Thread-local guard");
        Check(checks == 12, "Controlled check accounting");
        Parallel.For(0, 32, _ => Check(JsonSerializer.Serialize(Receipt(SharedTemplateLocalizedNameProfile.Load(Ready, complete.RootPath, complete.RootSha256))) == fingerprint, "Independent concurrent profile loads"));
        var rootPins = new Dictionary<string, string>(StringComparer.Ordinal) { ["object/creature/player/shared_human_male.iff"] = "E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243", ["object/tangible/terminal/shared_terminal_mission.iff"] = "E9FE68A85ED45CA33AC2715BD7275C01AFB1CFC837E5CEB61E8E3AB567586281" };
        var originalResources = new Dictionary<string, TreLookupResult>(StringComparer.Ordinal); var stfInputs = new List<object>();
        using (var config = File.OpenText(args[1])) using (var overlay = ConfiguredTreOverlay.Open(args[0], config))
        {
            foreach (string path in rootPins.Keys.Concat(new[] { "string/en/species.stf", "string/en/terminal_name.stf" }))
            {
                var source = overlay.Lookup(path); Check(source.Kind == TreLookupKind.Present && source.Payload is not null, "Original resource present"); originalResources.Add(path, source);
                if (path.StartsWith("string/", StringComparison.Ordinal))
                {
                    int size = source.Payload!.Length; string pin = Convert.ToHexString(SHA256.HashData(source.Payload)); var table = LocalizedStringTable.Load(source.Payload);
                    Check(path.EndsWith("species.stf", StringComparison.Ordinal) ? size == 10076 && pin == "75CDD0D8142C95022E06A440D665D3D765394762A456FF8E5CE0AC992E14E2CA" && source.Priority == 42 && table.Header.RecordCount == 227 : size == 4610 && pin == "CDEDCD3ECE3E5FE6C7455B70760DDDB047B2F16281D740A1034D465F081FF603" && source.Priority == 53 && table.Header.RecordCount == 57, "Original STF pin/count");
                    stfInputs.Add(new { Path = path, source.Priority, Bytes = size, Sha256 = pin, Records = table.Header.RecordCount });
                }
            }
        }
        var originals = new List<object>(); var keyExistence = new List<object>();
        foreach (var root in rootPins)
        {
            var calls = new List<string>(); var profile = SharedTemplateLocalizedNameProfile.Load(p => { calls.Add(p); Check(p == root.Key, "Both original root fields explicit; no parent resource reads"); return originalResources[p]; }, root.Key, root.Value);
            Check(profile.Complete && profile.Visits.Count == 1 && profile.Sources[root.Key].Priority == 39, "Original nearest root profile");
            var name = profile.ObjectName!; byte[] tableBytes = name.Table.CopyBytes(); Check(tableBytes.All(b => b is >= 97 and <= 122 or 95), "Bounded original table identity ASCII");
            string tablePath = "string/en/" + Encoding.ASCII.GetString(tableBytes) + ".stf"; Check(tablePath is "string/en/species.stf" or "string/en/terminal_name.stf", "Only two authorized referenced tables");
            var table = LocalizedStringTable.Load(originalResources[tablePath].Payload!); bool found = table.TryGetByKey(name.Key.CopyBytes(), out var record); Check(found, "Original authored name exact key exists");
            originals.Add(new { Calls = calls, Profile = Receipt(profile) }); keyExistence.Add(new { Path = tablePath, MatchedId = record!.Id, Found = found });
        }
        foreach (var resource in originalResources.Values) Array.Clear(resource.Payload!);
        Emit("template_name_synthetic", new { Cases = cases, ControlledChecks = checks, ConcurrentLoads = 32, Owned = true });
        Emit("template_name_original", new { Profiles = originals, NameKeyExistence = keyExistence, StfInputs = stfInputs, ArchivesClosed = true });
        Console.WriteLine("PASS: owned bounded SHOT localized-name pairs, nearest explicit/empty policy, two pinned roots and independent referenced-key checks.");
    }
}
