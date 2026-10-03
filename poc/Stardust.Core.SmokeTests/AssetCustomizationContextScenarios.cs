using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class AssetCustomizationContextScenarios
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static object Binding(AssetCustomizationContextBinding b) => new { b.Key.VariableName, b.Key.Private, b.Id, b.FullName, Kind = b.Kind.ToString(), b.MinimumInclusive, b.MaximumExclusive, b.AuthoredDefault, b.PalettePath };
    private static object Receipt(AssetCustomizationContext c) => new { Bindings = c.Bindings.Select(Binding), Defaults = c.AuthoredDefaults.OrderBy(p => p.Key.VariableName).ThenBy(p => p.Key.Private).Select(p => new { p.Key.VariableName, p.Key.Private, Value = p.Value }) };
    private static PaletteDescription Palette(int count) => new(0x0300, Array.AsReadOnly(Enumerable.Repeat(new PaletteEntry(1, 2, 3, 4), count).ToArray()), ReadOnlyMemory<byte>.Empty, Array.Empty<string>());
    private static void Emit(string tag, object value) => Console.WriteLine(tag + "=" + JsonSerializer.Serialize(value));
    internal static void Run(string[] args)
    {
        Check(args.Length == 7, "root/config/catalog/bank/face-profile/full-profile/synthetic-fixtures required");
        var fixtures = JsonSerializer.Deserialize<Synthetic[]>(File.ReadAllBytes(args[6]))!;
        var cases = new List<object>();
        foreach (var fixture in fixtures)
        {
            var dictionary = CustomizationIdDictionary.Load(Convert.FromHexString(fixture.Dictionary));
            var descriptions = fixture.Descriptions.Select(d => d.Kind == "Palette" ? (AssetCustomizationVariable)new AssetPaletteCustomizationVariable(d.FullName, d.PalettePath!, d.Default) : new AssetRangedCustomizationVariable(d.FullName, d.Minimum, d.Maximum, d.Default)).ToList();
            var query = new AssetCustomizationDirectoryQuery(1, true, 1, descriptions); var keys = fixture.Keys.Select(k => new ShaderCustomizationKey(k.Name, k.Private)).ToList(); int calls = 0;
            try
            {
                var result = AssetCustomizationContextBuilder.Build(query, dictionary, keys, path => { calls++; return fixture.Palettes.TryGetValue(path, out int count) ? Palette(count) : null; });
                Check(!fixture.Reject, "Expected synthetic rejection: " + fixture.Name); cases.Add(new { fixture.Name, Rejected = false, ResolverCalls = calls, Result = Receipt(result) });
            }
            catch (InvalidDataException) { Check(fixture.Reject, "Unexpected rejection: " + fixture.Name); cases.Add(new { fixture.Name, Rejected = true }); }
        }
        var positive = fixtures[0]; var dict = CustomizationIdDictionary.Load(Convert.FromHexString(positive.Dictionary));
        var rangeKey = new ShaderCustomizationKey("ranged", true); var paletteKey = new ShaderCustomizationKey("color", false);
        var values = new List<AssetCustomizationVariable> { new AssetRangedCustomizationVariable("/private/ranged", -8, 8, -2), new AssetPaletteCustomizationVariable("/shared_owner/color", "palette/synthetic.pal", 1) };
        var selected = new List<ShaderCustomizationKey> { rangeKey, paletteKey }; var baseQuery = new AssetCustomizationDirectoryQuery(1, true, 1, values);
        var mutableEntries = new List<PaletteEntry> { new(1, 2, 3, 4), new(5, 6, 7, 8) }; byte[] trailing = { 9 }; var diagnostics = new List<string>();
        var mutablePalette = new PaletteDescription(0x0300, mutableEntries, trailing, diagnostics);
        var owned = AssetCustomizationContextBuilder.Build(baseQuery, dict, selected, _ => mutablePalette);
        string fingerprint = JsonSerializer.Serialize(Receipt(owned)); values.Clear(); selected.Clear(); mutableEntries.Clear(); trailing[0] = 0; diagnostics.Add("changed");
        Check(owned.RangeContext.Ranges[rangeKey] == new CustomizationVariableRange(-8, 8) && owned.RangeContext.Ranges[paletteKey] == new CustomizationVariableRange(0, 2) && JsonSerializer.Serialize(Receipt(owned)) == fingerprint, "Returned context owns only bound numbers/names");
        bool readOnly = false; try { ((IDictionary<ShaderCustomizationKey, int>)owned.AuthoredDefaults).Clear(); } catch (NotSupportedException) { readOnly = true; } Check(readOnly, "Immutable defaults");
        readOnly = false; try { ((IList<AssetCustomizationContextBinding>)owned.Bindings).Clear(); } catch (NotSupportedException) { readOnly = true; } Check(readOnly, "Immutable bindings");
        values.Add(new AssetRangedCustomizationVariable("/private/ranged", -8, 8, -2)); values.Add(new AssetPaletteCustomizationVariable("/shared_owner/color", "palette/synthetic.pal", 1)); selected.Add(rangeKey); selected.Add(paletteKey);
        var failures = new List<string>();
        void Reject<T>(string name, Action action) where T : Exception
        { try { action(); } catch (T) { failures.Add(name); return; } throw new Exception("Expected context rejection: " + name); }
        Reject<InvalidDataException>("selection-mutation", () => AssetCustomizationContextBuilder.Build(baseQuery, dict, selected, _ => { selected.Clear(); return Palette(2); })); selected.Add(rangeKey); selected.Add(paletteKey);
        Reject<InvalidDataException>("query-mutation", () => AssetCustomizationContextBuilder.Build(baseQuery, dict, selected, _ => { values[0] = new AssetRangedCustomizationVariable("/private/ranged", -8, 8, -1); return Palette(2); })); values[0] = new AssetRangedCustomizationVariable("/private/ranged", -8, 8, -2);
        Reject<InvalidOperationException>("resolver-exception", () => AssetCustomizationContextBuilder.Build(baseQuery, dict, selected, _ => throw new InvalidOperationException("fixture")));
        Reject<InvalidOperationException>("swallowed-reentry", () => AssetCustomizationContextBuilder.Build(baseQuery, dict, selected, _ => { try { AssetCustomizationContextBuilder.Build(baseQuery, dict, selected, _ => Palette(2)); } catch (InvalidOperationException) { } return Palette(2); }));
        using (var cancel = new CancellationTokenSource()) { cancel.Cancel(); int calls = 0; Reject<OperationCanceledException>("pre-cancel", () => AssetCustomizationContextBuilder.Build(baseQuery, dict, selected, _ => { calls++; return Palette(2); }, cancel.Token)); Check(calls == 0, "Precancellation no resolver"); }
        using (var cancel = new CancellationTokenSource()) Reject<OperationCanceledException>("resolver-cancel", () => AssetCustomizationContextBuilder.Build(baseQuery, dict, selected, _ => { cancel.Cancel(); return Palette(2); }, cancel.Token));
        using (var cancel = new CancellationTokenSource()) Reject<OperationCanceledException>("final-palette-cancel", () => AssetCustomizationContextBuilder.Build(baseQuery, dict, selected, _ => new(0x0300, new FinalCancelPaletteEntries(cancel), ReadOnlyMemory<byte>.Empty, Array.Empty<string>()), cancel.Token));
        Reject<InvalidDataException>("selected-budget", () => AssetCustomizationContextBuilder.Build(baseQuery, dict, selected, _ => Palette(2), limits: new(MaximumSelectedKeys: 1)));
        Reject<InvalidDataException>("descriptor-budget", () => AssetCustomizationContextBuilder.Build(baseQuery, dict, selected, _ => Palette(2), limits: new(MaximumDescriptors: 1)));
        Reject<InvalidDataException>("palette-entry-budget", () => AssetCustomizationContextBuilder.Build(baseQuery, dict, selected, _ => Palette(2), limits: new(MaximumPaletteEntries: 1)));
        Reject<InvalidDataException>("palette-aggregate-budget", () => AssetCustomizationContextBuilder.Build(baseQuery, dict, selected, _ => Palette(2), limits: new(MaximumTotalPaletteEntries: 1)));
        Reject<InvalidDataException>("fallback-palette", () => AssetCustomizationContextBuilder.Build(baseQuery, dict, selected, _ => PaletteDecoder.CreateMissingFallback()));
        Reject<InvalidDataException>("unknown-query", () => AssetCustomizationContextBuilder.Build(baseQuery with { Found = false, RootKey = null }, dict, selected, _ => Palette(2)));
        Reject<ArgumentOutOfRangeException>("invalid-limits", () => AssetCustomizationContextBuilder.Build(baseQuery, dict, selected, _ => Palette(2), limits: new(MaximumSelectedKeys: 4097)));
        var two = baseQuery with { Variables = new AssetCustomizationVariable[] { new AssetPaletteCustomizationVariable("/private/ranged", "palette/first.pal", 0), new AssetPaletteCustomizationVariable("/shared_owner/color", "palette/second.pal", 0) } };
        var first = new List<PaletteEntry> { new(1, 2, 3, 4) };
        Reject<InvalidDataException>("earlier-palette-mutation", () => AssetCustomizationContextBuilder.Build(two, dict, selected, p => { if (p == "palette/first.pal") return new(0x0300, first, ReadOnlyMemory<byte>.Empty, Array.Empty<string>()); first[0] = new(9, 2, 3, 4); return Palette(1); }));
        Reject<InvalidDataException>("palette-path-budget", () => AssetCustomizationContextBuilder.Build(two, dict, selected, _ => Palette(1), limits: new(MaximumPalettePaths: 1)));
        Reject<InvalidDataException>("lying-palette-list", () => AssetCustomizationContextBuilder.Build(baseQuery, dict, selected, _ => new(0x0300, new EndlessPaletteEntries(), ReadOnlyMemory<byte>.Empty, Array.Empty<string>())));
        var exact = AssetCustomizationContextBuilder.Build(baseQuery, dict, selected, _ => Palette(2), limits: new(2, 2, 1, 2, 2)); Check(JsonSerializer.Serialize(Receipt(exact)) == fingerprint, "Exact admission budgets and guard recovery");
        Parallel.For(0, 16, _ => Check(JsonSerializer.Serialize(Receipt(AssetCustomizationContextBuilder.Build(baseQuery, dict, selected, _ => Palette(2)))) == fingerprint, "Independent concurrent builds"));
        Check(JsonSerializer.Serialize(Receipt(owned)) == fingerprint, "Failures preserve previously published context");
        using var bankFile = File.OpenRead(args[3]); var bank = SkeletalAnimationBankProfile.Load(bankFile); var catalog = ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(args[2]));
        using var faceFile = File.OpenRead(args[4]); var face = BlueprintFaceCustomizationProfile.Load(faceFile); using var profileFile = File.OpenRead(args[5]); var profile = SkeletalAppearanceCustomization.LoadProfile(profileFile);
        var borrowed = new List<byte[]>(); SceneObjectResources resources; AssetCustomizationDirectory directory; CustomizationIdDictionary installedDictionary;
        using (var cfg = File.OpenText(args[1]))
        using (var overlay = ConfiguredTreOverlay.Open(args[0], cfg))
        {
            byte[] Required(string path, string pin) { var r = overlay.Lookup(path); Check(r.Priority == 53 && r.Payload is not null && Convert.ToHexString(SHA256.HashData(r.Payload!)) == pin, "Original table pin"); borrowed.Add(r.Payload!); return r.Payload!; }
            directory = AssetCustomizationDirectory.Load(Required("customization/asset_customization_manager.iff", "5A4F052E92B5E29CD3001CA1E0FE0438358CAA45E201E72E062BE65A6886DA20"));
            installedDictionary = CustomizationIdDictionary.Load(Required("customization/customization_id_manager.iff", CustomizationIdDictionaryScenarios.OriginalPin));
            var idle = new LogicalAnimationClipSelection("appearance/lat/all_m.lat", "emt_standing_idle_ambient_1_ag", "798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
            resources = SceneObjectResourceRouter.Load(new(101, 0, 0, 0, 1, 10, 20, 30, 2937962913, 0), catalog, new SceneCreatureResourceOptions(idle, bank, true, 128, 33554432, new(face)), path => { var r = overlay.Lookup(path); if (r.Payload is not null) borrowed.Add(r.Payload); return r; });
        }
        foreach (var b in borrowed) Array.Clear(b);
        var creature = resources.Creature!.Creature; var paletteUnion = new Dictionary<string, PaletteDescription>(StringComparer.Ordinal);
        foreach (var p in creature.FaceResources!.Palettes.Concat(creature.MaterialBundle!.PaletteDefaults!.Values.SelectMany(v => v.Palettes)))
        {
            if (paletteUnion.TryGetValue(p.Key, out var prior)) Check(prior.Version == p.Value.Version && prior.Entries.SequenceEqual(p.Value.Entries) && prior.IgnoredTrailingBytes.Span.SequenceEqual(p.Value.IgnoredTrailingBytes.Span), "Equivalent owned palette alias");
            else paletteUnion.Add(p.Key, p.Value);
        }
        var queryOriginal = directory.Query(LegacyAssetPath.Checksum("appearance/hum_m.sat"), normalizedAssetPath: "appearance/hum_m.sat"); var selectedOriginal = profile.Keys.OrderBy(k => k.VariableName).ThenBy(k => k.Private).ToArray(); var paths = new List<string>();
        var bound = AssetCustomizationContextBuilder.Build(queryOriginal, installedDictionary, selectedOriginal, path => { paths.Add(path); return paletteUnion.GetValueOrDefault(path); });
        Check(bound.Bindings.Count == 8 && bound.AuthoredDefaults.Values.All(v => v == 0) && profile.All(p => bound.AuthoredDefaults[p.Key] == p.Value), "Original eight authored zero defaults");
        Check(paths.Count == paths.Distinct(StringComparer.Ordinal).Count() && paths.Count == 5 && resources.ExternalReads == 76 && resources.SourceBytes == 5940390, "Selected five paths resolved once from unchanged76-source closure");
        Emit("synthetic_context", new { Cases = cases, Failures = failures, Owned = true, ConcurrentBuilds = 16, ExactBudgets = true });
        Emit("asset_context", new { QueryCrc = queryOriginal.FilenameCrc, queryOriginal.RootKey, QueryVariables = queryOriginal.Variables.Count, Result = Receipt(bound), ResolvedPaths = paths, PaletteUnion = paletteUnion.OrderBy(p => p.Key).Select(p => new { Path = p.Key, Count = p.Value.Entries.Count, Source = creature.Sources[p.Key] }), resources.ExternalReads, resources.SourceBytes, ArchiveClosed = true, AuthoredNotPlayerState = true });
        Console.WriteLine("PASS: selected ACST/CIDM/palette context, exact authored defaults, atomic callback/cancel/budget cases and original eight-key owned palette binding.");
    }
    private sealed class FinalCancelPaletteEntries(CancellationTokenSource cancel) : IReadOnlyList<PaletteEntry>
    {
        private int enumerations;
        public int Count => 2;
        public PaletteEntry this[int index] => new(1, 2, 3, 4);
        public IEnumerator<PaletteEntry> GetEnumerator()
        {
            int current = ++enumerations;
            for (int i = 0; i < 2; i++) yield return this[i];
            if (current == 3) cancel.Cancel();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
    private sealed class EndlessPaletteEntries : IReadOnlyList<PaletteEntry>
    {
        public int Count => 1;
        public PaletteEntry this[int index] => new(1, 2, 3, 4);
        public IEnumerator<PaletteEntry> GetEnumerator() { while (true) yield return new(1, 2, 3, 4); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
    private sealed record Key(string Name, bool Private);
    private sealed record Description(string Kind, string FullName, string? PalettePath, int Minimum, int Maximum, int Default);
    private sealed record Synthetic(string Name, string Dictionary, Description[] Descriptions, Key[] Keys, Dictionary<string, int> Palettes, bool Reject);
}
