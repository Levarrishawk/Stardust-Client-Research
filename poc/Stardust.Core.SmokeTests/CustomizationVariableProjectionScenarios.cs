using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;

internal static class CustomizationVariableProjectionScenarios
{
    internal sealed record Fixture(string Name, string Wire);
    private static readonly ShaderCustomizationKey Signed = new("signed", true), Palette = new("palette", false);
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static object Values(IReadOnlyDictionary<ShaderCustomizationKey, int> values) => values.OrderBy(p => p.Key.VariableName, StringComparer.Ordinal).ThenBy(p => p.Key.Private).Select(p => new { p.Key.VariableName, p.Key.Private, Value = p.Value }).ToArray();
    private static object Diagnostic(CustomizationProjectionDiagnostic d) => new { d.RecordIndex, d.Id, d.FullName, VariableName = d.Key?.VariableName, Private = d.Key?.Private, d.AttemptedValue, d.PreviousValue, d.ResultValue, Decision = d.Decision.ToString() };
    private static void Emit(string name, object result) => Console.WriteLine(name + "=" + JsonSerializer.Serialize(result));
    internal static void Run(string[] args)
    {
        Check(args.Length == 7, "root/config/catalog/bank/face-profile/full-profile/fixtures required");
        // Independently authored synthetic IFF, kept textual and small; no original name table copied.
        var names = new[] { "/private/signed", "/shared_owner/palette", "private/anomaly", "/private/absent", "/other/unknown" };
        var body = new List<byte>(); for (int i = 0; i < names.Length; i++) { body.Add((byte)(i + 1)); body.Add(0); body.AddRange(System.Text.Encoding.ASCII.GetBytes(names[i])); body.Add(0); }
        byte[] Chunk(string tag, byte[] bytes) { var result = new byte[bytes.Length + 8]; System.Text.Encoding.ASCII.GetBytes(tag).CopyTo(result, 0); System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(4), (uint)bytes.Length); bytes.CopyTo(result, 8); return result; }
        byte[] Form(string tag, byte[] child) => Chunk("FORM", System.Text.Encoding.ASCII.GetBytes(tag).Concat(child).ToArray());
        var dictionary = CustomizationIdDictionary.Load(Form("CIDM", Form("0001", Chunk("DATA", body.ToArray()))));
        var mutableRanges = new Dictionary<ShaderCustomizationKey, CustomizationVariableRange> { [Signed] = new(-100, 300), [Palette] = new(0, 4) };
        var ranges = new CustomizationVariableRangeContext(mutableRanges); mutableRanges.Clear();
        var seed = new Dictionary<ShaderCustomizationKey, int> { [Signed] = 17, [Palette] = 2 };
        var rows = new List<object>(); int failures = 0;
        void Reject(Action action) { try { action(); } catch (InvalidDataException) { failures++; return; } throw new Exception("Expected projection rejection"); }
        foreach (var fixture in JsonSerializer.Deserialize<Fixture[]>(File.ReadAllBytes(args[6]))!)
        {
            byte[] raw = Convert.FromHexString(fixture.Wire); var packed = PackedCustomizationCodec.Decode(raw); Array.Clear(raw);
            try
            {
                var result = CustomizationVariableProjection.Apply(dictionary, ranges, seed, packed);
                Check(result.Values.Count == 2 && seed[Signed] == 17 && seed[Palette] == 2, "Full output and untouched seed");
                rows.Add(new { fixture.Name, Failed = false, result.IsOuterEmpty, Values = Values(result.Values), Diagnostics = result.Diagnostics.Select(Diagnostic) });
            }
            catch (InvalidDataException)
            {
                rows.Add(new { fixture.Name, Failed = true }); Check(seed[Signed] == 17 && seed[Palette] == 2, "Atomic unknown ID preserves seed");
            }
        }
        var empty = new PackedCustomizationSnapshot(true, Array.Empty<PackedCustomizationRecord>());
        var preserved = CustomizationVariableProjection.Apply(dictionary, ranges, seed, empty);
        Check(CustomizationVariableProjection.Apply(dictionary, ranges.Ranges, seed, empty).Values[Signed] == 17, "Convenience overload exact full seed");
        seed[Signed] = 99; Check(preserved.Values[Signed] == 17 && ranges.Ranges.Count == 2, "Owned range/current snapshots"); seed[Signed] = 17;
        bool readOnly = false; try { ((IDictionary<ShaderCustomizationKey, int>)preserved.Values)[Signed] = 8; } catch (NotSupportedException) { readOnly = true; } Check(readOnly, "Result read-only");
        bool rangeReadOnly = false; try { ((IDictionary<ShaderCustomizationKey, CustomizationVariableRange>)ranges.Ranges)[Signed] = new(0, 1); } catch (NotSupportedException) { rangeReadOnly = true; } Check(rangeReadOnly, "Range context read-only");
        var diagnosticResult = CustomizationVariableProjection.Apply(dictionary, ranges, seed, new(false, new[] { new PackedCustomizationRecord(1, new byte[] { 42 }) }));
        bool diagnosticReadOnly = false; try { ((IList<CustomizationProjectionDiagnostic>)diagnosticResult.Diagnostics).Clear(); } catch (NotSupportedException) { diagnosticReadOnly = true; } Check(diagnosticReadOnly, "Diagnostics read-only");
        var extremeKey = new ShaderCustomizationKey(new string('x', 256), false);
        var extremes = new CustomizationVariableRangeContext(new Dictionary<ShaderCustomizationKey, CustomizationVariableRange> { [extremeKey] = new(int.MinValue, int.MaxValue) });
        foreach (int value in new[] { int.MinValue, int.MaxValue - 1 }) Check(CustomizationVariableProjection.Apply(dictionary, extremes, new Dictionary<ShaderCustomizationKey, int> { [extremeKey] = value }, empty).Values[extremeKey] == value, "Full Int32 seed edges and key length256");
        Reject(() => new CustomizationVariableRangeContext(Array.Empty<KeyValuePair<ShaderCustomizationKey, CustomizationVariableRange>>()));
        Reject(() => new CustomizationVariableRangeContext(new[] { new KeyValuePair<ShaderCustomizationKey, CustomizationVariableRange>(Signed, new(1, 1)) }));
        Reject(() => new CustomizationVariableRangeContext(new[] { new KeyValuePair<ShaderCustomizationKey, CustomizationVariableRange>(Signed, new(2, 1)) }));
        Reject(() => new CustomizationVariableRangeContext(new[] { new KeyValuePair<ShaderCustomizationKey, CustomizationVariableRange>(Signed, null!) }));
        Reject(() => new CustomizationVariableRangeContext(new[] { new KeyValuePair<ShaderCustomizationKey, CustomizationVariableRange>(new("", true), new(0, 1)) }));
        Reject(() => new CustomizationVariableRangeContext(new[] { new KeyValuePair<ShaderCustomizationKey, CustomizationVariableRange>(new("bad name", true), new(0, 1)) }));
        Reject(() => new CustomizationVariableRangeContext(new[] { new KeyValuePair<ShaderCustomizationKey, CustomizationVariableRange>(Signed, new(0, 1)), new KeyValuePair<ShaderCustomizationKey, CustomizationVariableRange>(Signed, new(0, 2)) }));
        var many = Enumerable.Range(0, 4096).ToDictionary(i => new ShaderCustomizationKey("key" + i, true), _ => new CustomizationVariableRange(int.MinValue, int.MaxValue));
        var full = new CustomizationVariableRangeContext(many); var fullSeed = many.ToDictionary(p => p.Key, _ => 0);
        Check(CustomizationVariableProjection.Apply(dictionary, full, fullSeed, empty).Values.Count == 4096, "Maximum full context accepted");
        many.Add(new("extra", true), new(0, 1)); Reject(() => new CustomizationVariableRangeContext(many));
        Reject(() => CustomizationVariableProjection.Apply(dictionary, ranges, new Dictionary<ShaderCustomizationKey, int> { [Signed] = 17 }, empty));
        Reject(() => CustomizationVariableProjection.Apply(dictionary, ranges, new Dictionary<ShaderCustomizationKey, int> { [Signed] = -101, [Palette] = 2 }, empty));
        Reject(() => CustomizationVariableProjection.Apply(dictionary, ranges, new Dictionary<ShaderCustomizationKey, int> { [Signed] = 300, [Palette] = 2 }, empty));
        Reject(() => CustomizationVariableProjection.Apply(dictionary, ranges, new Dictionary<ShaderCustomizationKey, int> { [Signed] = 17, [new("palette", true)] = 2 }, empty));
        var accepted = new PackedCustomizationSnapshot(false, new[] { new PackedCustomizationRecord(1, new byte[] { 42 }) });
        var current = CustomizationVariableProjection.Apply(dictionary, ranges, seed, accepted);
        var invalid = new PackedCustomizationSnapshot(false, new[] { new PackedCustomizationRecord(1, new byte[] { 0, 128 }) });
        Check(CustomizationVariableProjection.Apply(dictionary, ranges, current.Values, invalid).Values[Signed] == 42, "Sequential valid then invalid preserves earlier write");
        Reject(() => CustomizationVariableProjection.Apply(dictionary, ranges, current.Values, new(false, new[] { new PackedCustomizationRecord(1, new byte[] { 7 }), new PackedCustomizationRecord(6, new byte[] { 2 }) })));
        Check(current.Values[Signed] == 42, "Atomic late failure preserves previously published result");
        Parallel.For(0, 32, _ => Check(CustomizationVariableProjection.Apply(dictionary, ranges, seed, accepted).Values[Signed] == 42, "Pure concurrent/repeated application"));
        // Original resources supply consumer bounds for this fixture only, not ACST/live variable ranges.
        using var profile = File.OpenRead(args[5]); var originalSeed = SkeletalAppearanceCustomization.LoadProfile(profile);
        using var faceProfile = File.OpenRead(args[4]); var faceValues = BlueprintFaceCustomizationProfile.Load(faceProfile);
        var catalog = ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(args[2])); using var bankFile = File.OpenRead(args[3]); var bank = SkeletalAnimationBankProfile.Load(bankFile);
        var idle = new LogicalAnimationClipSelection("appearance/lat/all_m.lat", "emt_standing_idle_ambient_1_ag", "798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        SceneObjectResources resource; CustomizationIdDictionary originalDictionary; var borrowed = new List<byte[]>();
        using (var config = File.OpenText(args[1]))
        using (var overlay = ConfiguredTreOverlay.Open(args[0], config))
        {
            var raw = overlay.Lookup("customization/customization_id_manager.iff"); Check(raw.Priority == 53 && raw.Payload?.Length == 3697 && Convert.ToHexString(SHA256.HashData(raw.Payload!)) == CustomizationIdDictionaryScenarios.OriginalPin, "Original CIDM pin");
            originalDictionary = CustomizationIdDictionary.Load(raw.Payload!); borrowed.Add(raw.Payload!);
            resource = SceneObjectResourceRouter.Load(new(101, 0, 0, 0, 1, 10, 20, 30, 2937962913, 0), catalog,
                new SceneCreatureResourceOptions(idle, bank, true, 128, 33554432, new(faceValues)), path => { var found = overlay.Lookup(path); if (found.Payload is not null) borrowed.Add(found.Payload); return found; });
        }
        foreach (var bytes in borrowed) Array.Clear(bytes);
        var controls = SkeletalAppearanceCustomization.DescribeControls(resource.Creature!.Creature);
        Check(controls.Count == originalSeed.Count && originalSeed.Count == 8, "Original eight-key supplied context");
        var originalRanges = new CustomizationVariableRangeContext(controls.Select(c => new KeyValuePair<ShaderCustomizationKey, CustomizationVariableRange>(c.Key, new(0, checked(c.MaximumValue + 1)))));
        var mappings = controls.OrderBy(c => c.Key.VariableName).ThenBy(c => c.Key.Private).Select(c =>
        {
            string fullName = (c.Key.Private ? "/private/" : "/shared_owner/") + c.Key.VariableName;
            var entry = originalDictionary.ByName[fullName]; return new { entry.Id, entry.FullName, c.Key.VariableName, c.Key.Private, MinimumInclusive = 0, MaximumExclusive = checked(c.MaximumValue + 1), Current = originalSeed[c.Key] };
        }).ToArray();
        var originalRecords = mappings.SelectMany(m => new[] { new PackedCustomizationRecord((byte)m.Id, new byte[] { 1 }), new PackedCustomizationRecord((byte)m.Id, new byte[] { 255, 255 }) }).Append(new PackedCustomizationRecord(118, new byte[] { 4 })).ToArray();
        var originalPacked = new PackedCustomizationSnapshot(false, originalRecords);
        var originalResult = CustomizationVariableProjection.Apply(originalDictionary, originalRanges, originalSeed, originalPacked);
        Check(originalResult.Values.Values.All(v => v == 1), "Each valid1 followed by invalid minus1 retains1");
        Check(originalResult.Diagnostics.Last() is { FullName: "private/alternate_shader_blade", Decision: CustomizationProjectionDecision.SkippedUnsupportedNamespace, Key: null }, "Original ID118 explicit skip");
        Emit("projection", new { Fixtures = rows, ContextNegatives = failures, Ownership = true, ConcurrentIndependent = true, Capacity = 4096 });
        Emit("original_projection", new { DictionarySha256 = CustomizationIdDictionaryScenarios.OriginalPin, ProfileSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[5]))), BoundsPolicy = "supplied-consumer-DescribeControls-min0-maxPlus1-not-ACST", Mappings = mappings, Wire = Convert.ToHexString(PackedCustomizationCodec.Encode(originalPacked)), Values = Values(originalResult.Values), Diagnostics = originalResult.Diagnostics.Select(Diagnostic), resource.ExternalReads, resource.SourceBytes, ArchiveClosed = true, Owned = true });
        Console.WriteLine("PASS: ordered owned declared-range projection, independent-ready wire/value fixtures and pinned eight-key original consumer mapping.");
    }
}
