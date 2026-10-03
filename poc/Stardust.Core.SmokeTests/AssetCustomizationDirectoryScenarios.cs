using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class AssetCustomizationDirectoryScenarios
{
    private sealed record Fixture(string Name, string Hex, bool Reject);
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static object Row(AssetCustomizationVariable value) => new
    {
        Kind = value is AssetPaletteCustomizationVariable ? "Palette" : "Ranged", value.FullName, value.AuthoredDefault,
        PalettePath = (value as AssetPaletteCustomizationVariable)?.PalettePath,
        MinimumInclusive = (value as AssetRangedCustomizationVariable)?.MinimumInclusive,
        MaximumExclusive = (value as AssetRangedCustomizationVariable)?.MaximumExclusive
    };
    private static object Query(AssetCustomizationDirectoryQuery query) => new { query.FilenameCrc, query.Found, query.RootKey, Variables = query.Variables.Select(Row) };
    private static void Emit(string tag, object value) => Console.WriteLine(tag + "=" + JsonSerializer.Serialize(value));
    internal static void Run(string[] args)
    {
        Check(args.Length == 3, "root/config/independent synthetic fixtures required");
        var fixtures = JsonSerializer.Deserialize<Fixture[]>(File.ReadAllBytes(args[2]))!;
        var cases = new List<object>(); int malformed = 0;
        foreach (var fixture in fixtures)
        {
            byte[] raw = Convert.FromHexString(fixture.Hex);
            try
            {
                var directory = AssetCustomizationDirectory.Load(raw); Array.Clear(raw);
                Check(!fixture.Reject, "Expected malformed fixture rejection: " + fixture.Name);
                cases.Add(new { fixture.Name, Rejected = false, directory.Metadata, Queries = new[] { Query(directory.Query(0xAA22)), Query(directory.Query(0xAA22, true)), Query(directory.Query(99)) } });
            }
            catch (InvalidDataException)
            { Check(fixture.Reject, "Unexpected rejection: " + fixture.Name); malformed++; cases.Add(new { fixture.Name, Rejected = true }); }
        }
        var goodBytes = Convert.FromHexString(fixtures[0].Hex); var owned = AssetCustomizationDirectory.Load(goodBytes);
        var preserved = owned.Query(0xAA22); string fingerprint = JsonSerializer.Serialize(Query(preserved));
        Check(preserved.Found && preserved.RootKey == 10 && preserved.Variables[0] is AssetRangedCustomizationVariable { FullName: "/private/direct", AuthoredDefault: -7 }, "Direct-before-inherited wins");
        Check(preserved.Variables.Any(v => v.FullName == "/private/Direct") && owned.Query(0xAA22, true).Variables.All(v => !v.FullName.Contains("/shared_owner/")), "Exact case and shared-owner suppression");
        bool immutable = false; try { ((IList<AssetCustomizationVariable>)preserved.Variables).Clear(); } catch (NotSupportedException) { immutable = true; } Check(immutable, "Immutable query result");
        immutable = false; try { ((IDictionary<string, int>)owned.Metadata.TableCounts)["UCMP"] = 0; } catch (NotSupportedException) { immutable = true; } Check(immutable, "Immutable table metadata");
        int budgetNegatives = 0;
        void Budget(Action action) { try { action(); } catch (InvalidDataException) { budgetNegatives++; return; } throw new Exception("Expected ACST budget rejection"); }
        Budget(() => AssetCustomizationDirectory.Load(goodBytes, new(MaximumBytes: goodBytes.Length - 1)));
        Budget(() => AssetCustomizationDirectory.Load(goodBytes, new(MaximumTableRecords: 1)));
        Budget(() => AssetCustomizationDirectory.Load(goodBytes, new(MaximumNameBytes: 1)));
        Budget(() => AssetCustomizationDirectory.Load(goodBytes, new(MaximumDepth: 1)));
        Budget(() => AssetCustomizationDirectory.Load(goodBytes, new(MaximumValidationVisits: 1)));
        Budget(() => AssetCustomizationDirectory.Load(goodBytes, new(MaximumTraversalVisits: 1)).Query(0xAA22));
        Budget(() => AssetCustomizationDirectory.Load(goodBytes, new(MaximumExpandedValues: 1)).Query(0xAA22));
        Check(AssetCustomizationDirectory.Load(goodBytes, new(MaximumBytes: goodBytes.Length, MaximumExpandedValues: preserved.Variables.Count)).Query(0xAA22).Variables.Count == preserved.Variables.Count, "Exact source/output limits accepted");
        bool badLimits = false; try { AssetCustomizationDirectory.Load(goodBytes, new(MaximumDepth: 257)); } catch (ArgumentOutOfRangeException) { badLimits = true; } Check(badLimits, "Invalid limit options rejected");
        Check(JsonSerializer.Serialize(Query(preserved)) == fingerprint, "Failures retain prior query result");
        Parallel.For(0, 32, _ => Check(JsonSerializer.Serialize(Query(owned.Query(0xAA22))) == fingerprint, "Independent immutable concurrent queries"));
        Check(!owned.Query(99).Found && owned.Query(99).RootKey is null && owned.Query(99).Variables.Count == 0, "Explicit unknown root");
        byte[] original; int? priority;
        using (var config = File.OpenText(args[1]))
        using (var overlay = ConfiguredTreOverlay.Open(args[0], config))
        {
            var found = overlay.Lookup("customization/asset_customization_manager.iff"); Check(found.Kind == TreLookupKind.Present && found.Payload is not null, "Original ACST present"); original = found.Payload!; priority = found.Priority;
        }
        string pin = Convert.ToHexString(SHA256.HashData(original));
        Check(priority == 53 && original.Length == 458340 && pin == "5A4F052E92B5E29CD3001CA1E0FE0438358CAA45E201E72E062BE65A6886DA20", "Original source pin");
        var installed = AssetCustomizationDirectory.Load(original); Array.Clear(original);
        var queries = new List<object>();
        foreach (string path in new[] { "appearance/hum_m.sat", "appearance/togruta_f.sat" })
        {
            uint crc = LegacyAssetPath.Checksum(path); var query = installed.Query(crc, normalizedAssetPath: path);
            Check(query.Found && query.Variables.Count > 0, "Original targeted query found");
            queries.Add(Query(query)); queries.Add(Query(installed.Query(crc, true, path)));
        }
        Check(installed.Metadata.CrcOrderingInversions.SequenceEqual(new[] { new AssetCustomizationCrcInversion(26079, 0xFFFD72EB, 0xE201EE84) }), "Appended CIDX inversion retained diagnostically");
        int pathNegatives = 0;
        foreach (var attempt in new Action[] { () => installed.Query(99, normalizedAssetPath: "appearance/hum_m.sat"), () => installed.Query(LegacyAssetPath.Checksum("appearance/hum_m.sat"), normalizedAssetPath: "Appearance/HUM_M.SAT") })
        { try { attempt(); } catch (ArgumentException) { pathNegatives++; } }
        Check(pathNegatives == 2, "Explicit CRC and canonical path cross-check");
        Emit("synthetic", new { Cases = cases, Malformed = malformed, BudgetNegatives = budgetNegatives, PathNegatives = pathNegatives, Owned = true, IndependentQueries = 32 });
        Emit("acst", new { Path = "customization/asset_customization_manager.iff", Priority = priority, Bytes = 458340, Sha256 = pin, installed.Metadata, Queries = queries, Owned = true, ArchiveClosed = true });
        Console.WriteLine("PASS: bounded owned ACST directory, complete structural/cycle validation and independent-ready human/appended-Togruta queries.");
    }
}
