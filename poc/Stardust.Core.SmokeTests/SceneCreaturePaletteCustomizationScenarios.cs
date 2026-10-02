using System.Collections.ObjectModel;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

internal static class SceneCreaturePaletteCustomizationScenarios
{
    private static string Hash(ReadOnlySpan<byte> b) => Convert.ToHexString(SHA256.HashData(b));
    private static string Fingerprint(SceneCreatureResources r) => Hash(JsonSerializer.SerializeToUtf8Bytes(r.Creature.MaterialBundle!.AlbedoFactors!.Materials.OrderBy(p => p.Key)));
    internal static void Run(string[] args)
    {
        if (args.Length != 5) throw new ArgumentException("Explicit original root, configuration, catalog, animation bank and color profile required.");
        if (Hash(File.ReadAllBytes(args[1])) != RetainedSkeletalDetailBundleReader.ConfigurationPin) throw new InvalidDataException("Configuration pin mismatch.");
        var catalog = ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(args[2])); var entry = catalog.Require(2937962913);
        using var bankFile = File.OpenRead(args[3]); var bank = SkeletalAnimationBankProfile.Load(bankFile);
        var idle = new LogicalAnimationClipSelection("appearance/lat/all_m.lat", "emt_standing_idle_ambient_1_ag", "798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        var create = SceneObjectLifecycleCodec.DecodeCreate(SceneObjectLifecycleCodec.EncodeCreate(new(0xFEDCBA9876543210, -0f, .25f, -.5f, 2, 123.5f, -45.25f, float.Epsilon, entry.TemplateCrc, 255)));
        using var configuration = File.OpenText(args[1]); using var overlay = ConfiguredTreOverlay.Open(args[0], configuration);
        var reads = new Dictionary<string, int>(); var borrowed = new List<byte[]>();
        TreLookupResult Read(string p) { reads[p] = reads.GetValueOrDefault(p) + 1; var r = overlay.Lookup(p); if (r.Payload is not null) borrowed.Add(r.Payload); return r; }
        var original = SceneCreatureResourceReader.Load(create, catalog, idle, bank, Read, loadAlbedoFactors: true);
        if (original.ExternalReads != 39 || original.SourceBytes != 4686371 || original.Creature.FaceResources is not null || original.Creature.FaceBlueprints is not null || original.Creature.GeneratedFaces is not null || reads.Values.Any(n => n != 1)) throw new Exception("Original palette-only input changed.");
        var controls = SceneCreaturePaletteCustomization.DescribeControls(original);
        if (controls.Count != 3 || controls.Single(c => c.Key == new ShaderCustomizationKey("index_color_skin", false)).MaximumValue != 255 || controls.Single(c => c.Key == new ShaderCustomizationKey("index_color_2", true)).MaximumValue != 255 || controls.Single(c => c.Key == new ShaderCustomizationKey("index_color_3", true)).MaximumValue != 1) throw new Exception("Original palette controls changed.");
        byte[] profileBytes = File.ReadAllBytes(args[4]); IReadOnlyDictionary<ShaderCustomizationKey, int> profile;
        using (var stream = new MemoryStream(profileBytes)) { profile = SceneCreaturePaletteCustomization.LoadProfile(stream); if (!stream.CanRead) throw new Exception("Profile loader closed caller stream."); }
        var values = profile.ToDictionary(p => p.Key, p => p.Value); string before = Fingerprint(original);
        var zeroValues = controls.ToDictionary(c => c.Key, _ => 0); var zero = SceneCreaturePaletteCustomization.Apply(original, zeroValues);
        var varied = SceneCreaturePaletteCustomization.Apply(original, values);
        var fingerprints = new[] { Fingerprint(zero), Fingerprint(varied) };
        if (fingerprints[0] == fingerprints[1] || Fingerprint(original) != before || original.Creature.CustomizationValues is not null) throw new Exception("Color choices/input mutation changed.");
        Array.Clear(profileBytes); foreach (var key in values.Keys.ToArray()) values[key] = -1;
        foreach (byte[] b in borrowed) Array.Clear(b);
        if (Fingerprint(zero) != fingerprints[0] || Fingerprint(varied) != fingerprints[1] || Fingerprint(original) != before) throw new Exception("Selection borrowed caller bytes/values.");
        int sharingChecks = 0;
        void Shared(object? a, object? b) { sharingChecks++; if (!ReferenceEquals(a, b)) throw new Exception("Unchanged owned resource not shared."); }
        foreach (var result in new[] { zero, varied })
        {
            if (ReferenceEquals(result, original) || ReferenceEquals(result.Creature, original.Creature) || ReferenceEquals(result.Creature.MaterialBundle, original.Creature.MaterialBundle) || result.ExternalReads != original.ExternalReads || result.SourceBytes != original.SourceBytes || result.Create != create || result.Recipe != entry || result.Creature.GeneratedFaces is not null || result.Creature.FaceResources is not null) throw new Exception("Customized closure identity changed.");
            Shared(result.Create, original.Create); Shared(result.Recipe, original.Recipe); Shared(result.Creature.Sources, original.Creature.Sources);
            Shared(result.Creature.Geometries, original.Creature.Geometries); Shared(result.Creature.Skeletons, original.Creature.Skeletons); Shared(result.Creature.SkinSources, original.Creature.SkinSources);
            Shared(result.Creature.LogicalTables, original.Creature.LogicalTables); Shared(result.Creature.PreparedAnimation, original.Creature.PreparedAnimation); Shared(result.Creature.AdditionalAnimations, original.Creature.AdditionalAnimations);
            var mat = result.Creature.MaterialBundle!; var old = original.Creature.MaterialBundle!;
            Shared(mat.Materials, old.Materials); Shared(mat.Textures, old.Textures); Shared(mat.Effects, old.Effects); Shared(mat.AlbedoFactors!.Programs, old.AlbedoFactors!.Programs);
            foreach (var pair in mat.PaletteDefaults!)
            {
                Shared(pair.Value.Palettes, old.PaletteDefaults![pair.Key].Palettes); Shared(pair.Value.Shader, old.PaletteDefaults[pair.Key].Shader); Shared(pair.Value.Sources, old.PaletteDefaults[pair.Key].Sources);
                if (ReferenceEquals(pair.Value, old.PaletteDefaults[pair.Key]) || pair.Value.AuthoredFactors.Any(f => f.UsedAuthoredDefault || f.Color!.SelectedIndex != f.RequestedIndex)) throw new Exception("Explicit palette selection changed.");
            }
        }
        Console.WriteLine("scene_colors_controls=" + JsonSerializer.Serialize(controls));
        Console.WriteLine("scene_colors_input=" + JsonSerializer.Serialize(new { original.ExternalReads, original.SourceBytes, reads, sources = original.Creature.Sources.Values, wire = Convert.ToHexString(SceneObjectLifecycleCodec.EncodeCreate(original.Create)), recipe = original.Recipe }));
        var mats = original.Creature.MaterialBundle!;
        foreach (var p in mats.Textures) Console.WriteLine("scene_colors_texture=" + JsonSerializer.Serialize(new { path = p.Key, mips = p.Value.Mips.Select(m => Hash(m.Rgba.Span)) }));
        foreach (var p in mats.AlbedoFactors!.Programs) Console.WriteLine("scene_colors_program=" + JsonSerializer.Serialize(new { path = p.Key, p.Value.UsesAlphaHueMask, bytecodeSha256 = Hash(p.Value.Metadata.Children[0].Children[1].Payload.Span) }));
        foreach (var (name, result) in new[] { ("zero", zero), ("profile", varied) })
        {
            var material = result.Creature.MaterialBundle!;
            Console.WriteLine("scene_colors_choice=" + JsonSerializer.Serialize(new { name, values = result.Creature.CustomizationValues!.Select(p => new { p.Key.VariableName, p.Key.Private, value = p.Value }), factors = material.AlbedoFactors!.Materials.Select(p => new { path = p.Key, factors = p.Value }), selected = material.PaletteDefaults!.SelectMany(p => p.Value.AuthoredFactors.Select(f => new { shader = p.Key, f.VariableName, f.Private, f.PalettePath, f.RequestedIndex, f.UsedAuthoredDefault, f.MissingPalette, f.Color })) }));
            foreach (var pair in material.AlbedoFactors.Materials)
            {
                string path = material.Materials[pair.Key].References.Textures.Single(t => t.Tag == 0x4D41494E).AssetPath!;
                ReadOnlySpan<byte> rgba = material.Textures[path].Mips[0].Rgba.Span; var sample = new Vector4(rgba[0] / 255f, rgba[1] / 255f, rgba[2] / 255f, rgba[3] / 255f);
                var color = SkeletalAlbedoEvaluator.Evaluate(sample, pair.Value);
                Console.WriteLine("scene_colors_pixel=" + JsonSerializer.Serialize(new { name, shader = pair.Key, texture = path, pixel = 0, rgba = Convert.ToHexString(rgba[..4]), color = new[] { color.X, color.Y, color.Z } }));
            }
        }
        int rejects = 0;
        void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { rejects++; return; } throw new Exception("Expected " + typeof(T).Name); }
        foreach (var control in controls)
        {
            Reject<InvalidDataException>(() => SceneCreaturePaletteCustomization.Apply(original, zeroValues.Where(p => p.Key != control.Key)));
            foreach (int bad in new[] { -1, control.MaximumValue + 1, int.MinValue, int.MaxValue }) { var changed = zeroValues.ToDictionary(p => p.Key, p => p.Value); changed[control.Key] = bad; Reject<InvalidDataException>(() => SceneCreaturePaletteCustomization.Apply(original, changed)); }
        }
        Reject<InvalidDataException>(() => SceneCreaturePaletteCustomization.Apply(original, zeroValues.Concat(zeroValues.Take(1))));
        Reject<InvalidDataException>(() => SceneCreaturePaletteCustomization.Apply(original, zeroValues.Append(new(new("index_color_skin", true), 0))));
        Reject<InvalidDataException>(() => SceneCreaturePaletteCustomization.Apply(original, zeroValues.Select(p => new KeyValuePair<ShaderCustomizationKey, int>(new(p.Key.VariableName.ToUpperInvariant(), p.Key.Private), p.Value))));
        Reject<InvalidDataException>(() => SkeletalAppearanceCustomization.Apply(original.Creature, zeroValues));
        Reject<InvalidDataException>(() => SceneCreaturePaletteCustomization.DescribeControls(original with { Creature = original.Creature with { MaterialBundle = mats with { AlbedoFactors = null } } }));
        using (var cancel = new CancellationTokenSource()) { cancel.Cancel(); Reject<OperationCanceledException>(() => SceneCreaturePaletteCustomization.Apply(original, zeroValues, cancel.Token)); }
        using (var cancel = new CancellationTokenSource())
        {
            IEnumerable<KeyValuePair<ShaderCustomizationKey, int>> CancelValues() { foreach (var p in zeroValues) { cancel.Cancel(); yield return p; } }
            Reject<OperationCanceledException>(() => SceneCreaturePaletteCustomization.Apply(original, CancelValues(), cancel.Token));
        }
        int enumerated = 0, enumerationDisposed = 0;
        IEnumerable<KeyValuePair<ShaderCustomizationKey, int>> Endless() { try { for (int i = 0; ; i++) { enumerated++; yield return new(new("synthetic_" + i, false), 0); } } finally { enumerationDisposed++; } }
        Reject<InvalidDataException>(() => SceneCreaturePaletteCustomization.Apply(original, Endless())); if (enumerated != 4097 || enumerationDisposed != 1) throw new Exception("Value enumeration unbounded or not disposed.");
        // Independently authored descriptor colors/counts exercise intersection, not original-source grammar.
        var syntheticPalettes = mats.PaletteDefaults!.ToDictionary(p => p.Key, p => p.Value);
        foreach (var (shader, count) in new[] { ("shader/hum_m_body.sht", 2), ("shader/hum_m_head.sht", 3) })
        {
            var p = syntheticPalettes[shader]; var owned = p.Palettes.ToDictionary(x => x.Key, x => x.Value);
            owned["palette/pc_skin_hum.pal"] = new(0x0300, Array.AsReadOnly(Enumerable.Range(0, count).Select(i => new PaletteEntry((byte)(11 + i), 22, 33, 0)).ToArray()), ReadOnlyMemory<byte>.Empty, Array.Empty<string>());
            syntheticPalettes[shader] = p with { Palettes = new ReadOnlyDictionary<string, PaletteDescription>(owned) };
        }
        var synthetic = original with { Creature = original.Creature with { MaterialBundle = mats with { PaletteDefaults = new ReadOnlyDictionary<string, CustomizableShaderPaletteBundle>(syntheticPalettes) } } };
        if (SceneCreaturePaletteCustomization.DescribeControls(synthetic).Single(c => c.Key == new ShaderCustomizationKey("index_color_skin", false)).MaximumValue != 1) throw new Exception("Palette ranges not intersected.");
        var outOfIntersection = zeroValues.ToDictionary(p => p.Key, p => p.Value); outOfIntersection[new("index_color_skin", false)] = 2;
        Reject<InvalidDataException>(() => SceneCreaturePaletteCustomization.Apply(synthetic, outOfIntersection)); _ = SceneCreaturePaletteCustomization.Apply(synthetic, zeroValues);
        foreach (int count in new[] { 0, 1025 })
        {
            var p = syntheticPalettes["shader/hum_m_body.sht"]; var dict = p.Palettes.ToDictionary(x => x.Key, x => x.Value);
            dict["palette/pc_skin_hum.pal"] = new(0x0300, Array.AsReadOnly(new PaletteEntry[count]), ReadOnlyMemory<byte>.Empty, Array.Empty<string>());
            var changedPalettes = syntheticPalettes.ToDictionary(x => x.Key, x => x.Value); changedPalettes["shader/hum_m_body.sht"] = p with { Palettes = new ReadOnlyDictionary<string, PaletteDescription>(dict) };
            var invalid = original with { Creature = original.Creature with { MaterialBundle = mats with { PaletteDefaults = new ReadOnlyDictionary<string, CustomizableShaderPaletteBundle>(changedPalettes) } } };
            Reject<InvalidDataException>(() => SceneCreaturePaletteCustomization.DescribeControls(invalid));
        }

        byte[] validProfile = File.ReadAllBytes(args[4]); string text = Encoding.UTF8.GetString(validProfile);
        var badProfiles = new List<byte[]> { Array.Empty<byte>(), validProfile[..^2], new byte[] { 0xFF }, Encoding.UTF8.GetBytes("{}"), Encoding.UTF8.GetBytes(text.Replace("\"version\": 1", "\"version\": 2")), Encoding.UTF8.GetBytes(text.Replace("\"factoryFlag\": true", "\"factoryFlag\": 1")), Encoding.UTF8.GetBytes(text.Replace("\"value\": 75", "\"value\": 75.5")), Encoding.UTF8.GetBytes(text.Replace("\"values\":", "\"extra\": 0, \"values\":")), Encoding.UTF8.GetBytes(text.Replace("\"variableName\": \"index_color_skin\"", "\"variableName\": \"index_color_skin\", \"variableName\": \"index_color_skin\"")), new byte[65537] };
        badProfiles.Add(Encoding.UTF8.GetBytes("{\"version\":1,\"values\":[{\"variableName\":\"index_color_skin\",\"factoryFlag\":false,\"value\":0},{\"variableName\":\"index_color_skin\",\"factoryFlag\":false,\"value\":1}]}"));
        foreach (byte[] bad in badProfiles) { Reject<InvalidDataException>(() => { using var s = new MemoryStream(bad); _ = SceneCreaturePaletteCustomization.LoadProfile(s); }); Console.WriteLine("scene_colors_bad_profile=" + JsonSerializer.Serialize(new { hex = Convert.ToHexString(bad) })); }
        Reject<NotSupportedException>(() => ((IDictionary<ShaderCustomizationKey, int>)varied.Creature.CustomizationValues!).Clear());
        var registry = new SceneObjectRegistry(1, 12); var request = new SceneCreatureResourceRequest(registry.Create(12, create)!, catalog, idle, bank, 39, 4686371);
        using var owner = new SceneCreatureResourceInitializer(registry, (_, _) => Task.FromResult(varied)); owner.Start(request); var completion = owner.Poll().Single();
        if (completion.Kind != SceneCreatureResourceCompletionKind.Prepared || !ReferenceEquals(completion.Resources, varied) || owner.OutstandingJobs != 0 || owner.Poll().Count != 0) throw new Exception("Initializer rejected customized owned closure.");
        owner.Dispose(); owner.DrainCompletion.GetAwaiter().GetResult(); registry.Close();
        if (reads.Count != 39 || reads.Values.Any(n => n != 1) || Fingerprint(original) != before) throw new Exception("Selection reread or mutated source closure.");
        Console.WriteLine("scene_colors_validation=" + JsonSerializer.Serialize(new { rejects, controls = controls.Count, choices = 2, sharingChecks, noExtraReads = true, originalUnchanged = true, callerValuesOwned = true, sourceCloneChecks = borrowed.Count, boundedEnumeration = enumerated, enumerationDisposed, intersectionMaximum = 1, initializerPrepared = true, faceGenerated = false, gpu = false }));
        Console.WriteLine("PASS R11-ASSETS-01: explicit palette-only owned customization, strict ranges/namespaces, no face generation or source reload.");
    }
}
