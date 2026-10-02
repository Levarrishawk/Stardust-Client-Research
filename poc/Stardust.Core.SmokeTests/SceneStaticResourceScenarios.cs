using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Proof;

internal static class SceneStaticResourceScenarios
{
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private static byte[] StringMode(string value) => new byte[] { 1 }.Concat(Encoding.ASCII.GetBytes(value + "\0")).ToArray();
    private static byte[] Template(string? parent, byte[]? appearance, string version = "0008")
    {
        var parts = new List<byte[]>();
        if (parent is not null) parts.Add(MeshScenarios.Form("DERV", MeshScenarios.Chunk("XXXX", Encoding.ASCII.GetBytes(parent + "\0"))));
        var fields = new List<byte[]> { MeshScenarios.Chunk("PCNT", BitConverter.GetBytes(appearance is null ? 0 : 1)) };
        if (appearance is not null) fields.Add(MeshScenarios.Chunk("XXXX", Encoding.ASCII.GetBytes("appearanceFilename\0").Concat(appearance).ToArray()));
        parts.Add(MeshScenarios.Form(version, fields.ToArray())); return MeshScenarios.Form("SHOT", parts.ToArray());
    }
    private static List<AppearanceGeometryTemplate> Geometry(SceneStaticResources result)
    {
        var nodes = new Dictionary<string, AppearanceGeometryTemplate>(StringComparer.Ordinal);
        void Walk(AppearanceGeometryTemplate t) { if (!nodes.TryAdd(t.Path, t)) return; foreach (var child in t.Children) Walk(child.Template); }
        Walk(result.Appearance.Geometry.Root); return nodes.Values.Where(n => n.Mesh is not null).ToList();
    }
    private static string ProfileHash(SharedObjectAppearanceProfile p) => Hash(p.Inheritance.SelectMany(i => i.Layers).SelectMany(l => l.Parameters.OrderBy(k => k.Key, StringComparer.Ordinal)).SelectMany(k => k.Value.ToArray()).ToArray());
    internal static void Run(string[] args)
    {
        if (args.Length != 2) throw new ArgumentException("Explicit original root and pinned configuration required.");
        int rejects = 0, syntheticProfiles = 0;
        void Reject<T>(Action a) where T : Exception { try { a(); } catch (T) { rejects++; return; } throw new Exception("Expected " + typeof(T).Name); }
        const string tiny = "object/synthetic/root.iff", ancestor = "object/synthetic/base.iff";
        Dictionary<string, byte[]> Fixture(byte[]? rootValue, byte[]? baseValue, string? parent = ancestor, string version = "0008")
            => new(StringComparer.Ordinal) { [tiny] = Template(parent, rootValue, version), [ancestor] = Template(null, baseValue, "0010") };
        SharedObjectAppearanceProfile Profile(Dictionary<string, byte[]> fixture, int depth = 16, long bytes = 1048576)
            => SharedObjectAppearanceProfileReader.Load(p => fixture.TryGetValue(p, out var data) ? new(TreLookupKind.Present, data, 1) : new(TreLookupKind.Missing), tiny, Hash(fixture[tiny]), depth, bytes);
        foreach (var rootValue in new byte[]?[] { StringMode("appearance/near.apt"), new byte[] { 0 }, null })
        {
            var data = Fixture(rootValue, StringMode("appearance/far.apt")); var p = Profile(data); syntheticProfiles++;
            string expected = rootValue is { Length: > 1 } ? "appearance/near.apt" : "appearance/far.apt";
            if (p.AppearancePath != expected || p.AppearanceField.SourcePath != (expected.Contains("near") ? tiny : ancestor) || p.Inheritance.Count != 2) throw new Exception("Nearest deterministic provenance changed.");
            string before = ProfileHash(p); foreach (byte[] b in data.Values) Array.Clear(b); if (before != ProfileHash(p)) throw new Exception("Synthetic profile borrowed bytes.");
        }
        var valid = Fixture(StringMode("appearance/near.apt"), StringMode("")); _ = Profile(valid); syntheticProfiles++;
        foreach (byte[] bad in new[] { Array.Empty<byte>(), new byte[] { 0, 0 }, new byte[] { 2 }, new byte[] { 3, 1 }, new byte[] { 1 }, new byte[] { 1, (byte)'a' }, new byte[] { 1, 0xE9, 0 }, StringMode(""), StringMode("object/wrong.iff"), StringMode("../appearance/near.apt"), StringMode("appearance/a/../near.apt") })
            Reject<InvalidDataException>(() => Profile(Fixture(bad, StringMode("appearance/far.apt"))));
        Reject<InvalidDataException>(() => Profile(Fixture(StringMode("appearance/near.apt"), new byte[] { 3 })));
        Reject<InvalidDataException>(() => Profile(Fixture(new byte[] { 0 }, new byte[] { 0 })));
        foreach (string badParent in new[] { tiny, "../object/synthetic/base.iff", "object/synthetic/../base.iff", "appearance/base.apt" })
            Reject<InvalidDataException>(() => Profile(Fixture(StringMode("appearance/near.apt"), null, badParent)));
        foreach (string version in new[] { "0006", "0011" }) Reject<InvalidDataException>(() => Profile(Fixture(StringMode("appearance/near.apt"), null, version: version)));
        Reject<AppearanceGraphBudgetException>(() => Profile(valid, depth: 1));
        Reject<AppearanceGraphBudgetException>(() => Profile(valid, bytes: 1));
        Reject<ArgumentOutOfRangeException>(() => Profile(valid, depth: 0));
        Reject<ArgumentOutOfRangeException>(() => Profile(valid, bytes: 0));
        foreach (var kind in new[] { TreLookupKind.Missing, TreLookupKind.Masked })
            Reject<InvalidDataException>(() => SharedObjectAppearanceProfileReader.Load(p => p == tiny ? new(TreLookupKind.Present, valid[tiny]) : new(kind), tiny, Hash(valid[tiny])));
        Reject<ArgumentException>(() => SharedObjectAppearanceProfileReader.Load(_ => throw new Exception("Bad pin lookup"), tiny, "bad"));
        Reject<InvalidDataException>(() => SharedObjectAppearanceProfileReader.Load(_ => new(TreLookupKind.Present, valid[tiny]), tiny, new string('0', 64)));

        const string rootPath = "object/tangible/terminal/shared_terminal_mission.iff", rootPin = "E9FE68A85ED45CA33AC2715BD7275C01AFB1CFC837E5CEB61E8E3AB567586281",
            aptPath = "appearance/ksk_all_mission.apt", aptPin = "74316F0F9E28961C3AB983D5DF51DA313A3A4E8AD96358FECC04F747801B6021";
        var entry = new ObjectTemplateCatalogEntry(LegacyAssetPath.Checksum(rootPath), rootPath, rootPin, aptPin, SceneObjectResourceRecipe.StaticAppearance);
        var catalog = new ObjectTemplateCatalog(new[] { entry });
        var message = SceneObjectLifecycleCodec.DecodeCreate(SceneObjectLifecycleCodec.EncodeCreate(new(0xFEDCBA9876543210, -0f, .25f, -.5f, 2, 123.5f, -45.25f, float.Epsilon, entry.TemplateCrc, 255)));
        int preflightReads = 0; TreLookupResult Never(string _) { preflightReads++; throw new Exception("Invalid request performed lookup."); }
        SceneStaticResources Preflight(SceneCreateObjectByCrcMessage? m = null, ObjectTemplateCatalog? c = null, SceneStaticMaterialPolicy policy = SceneStaticMaterialPolicy.RequireComplete, AppearanceGraphLimits? limits = null, long decoded = 134217728, CancellationToken token = default)
            => SceneStaticResourceReader.Load(m ?? message, c ?? catalog, Never, policy, token, limits, decoded);
        Reject<InvalidDataException>(() => Preflight(message with { TemplateCrc = 0 }));
        Reject<InvalidDataException>(() => Preflight(c: new ObjectTemplateCatalog(new[] { entry with { Recipe = SceneObjectResourceRecipe.CreatureDetail0 } })));
        Reject<InvalidDataException>(() => Preflight(message with { PositionY = float.NaN }));
        Reject<ArgumentOutOfRangeException>(() => Preflight(policy: (SceneStaticMaterialPolicy)255));
        foreach (var bad in new[] { new AppearanceGraphLimits(MaximumResources: 0), new AppearanceGraphLimits(MaximumResources: 4097), new AppearanceGraphLimits(MaximumSourceBytes: 0), new AppearanceGraphLimits(MaximumSourceBytes: 134217729), new AppearanceGraphLimits(MaximumDepth: 0), new AppearanceGraphLimits(MaximumEdges: 0) })
            Reject<ArgumentOutOfRangeException>(() => Preflight(limits: bad));
        foreach (long bad in new[] { 3L, 134217729L }) Reject<ArgumentOutOfRangeException>(() => Preflight(decoded: bad));
        using (var canceled = new CancellationTokenSource()) { canceled.Cancel(); Reject<OperationCanceledException>(() => Preflight(token: canceled.Token)); }
        var hash = new string('0', 64); var fakeIdle = new LogicalAnimationClipSelection("appearance/lat/test.lat", "idle", hash);
        var fakeBank = new Dictionary<string, LogicalAnimationClipSelection> { ["walk"] = fakeIdle, ["run"] = fakeIdle };
        Reject<InvalidDataException>(() => SceneCreatureResourceReader.Load(message, catalog, fakeIdle, fakeBank, Never));
        if (preflightReads != 0) throw new Exception("Preflight/wrong recipe reached resolver.");
        if (Hash(File.ReadAllBytes(args[1])) != RetainedSkeletalDetailBundleReader.ConfigurationPin) throw new InvalidDataException("Configuration identity mismatch.");
        using var config = File.OpenText(args[1]); using var overlay = ConfiguredTreOverlay.Open(args[0], config);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal); var borrowed = new List<byte[]>();
        TreLookupResult Read(string p) { counts[p] = counts.GetValueOrDefault(p) + 1; var r = overlay.Lookup(p); if (r.Payload is not null) borrowed.Add(r.Payload); return r; }
        var result = SceneStaticResourceReader.Load(message, catalog, Read, SceneStaticMaterialPolicy.AllowNeutral);
        var geometry = Geometry(result); var materials = result.Appearance.Meshes.Values.SelectMany(m => m.Materials).GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.First().Value);
        var textures = result.Appearance.Meshes.Values.SelectMany(m => m.Textures).GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.First().Value);
        var effects = result.Appearance.Meshes.Values.SelectMany(m => m.Effects).Select(p => p.Key).Distinct().ToArray();
        if (counts.Values.Any(n => n != 1) || counts.Count != result.ExternalReads || !counts.Keys.ToHashSet().SetEquals(result.Sources.Keys) || result.Profile.Inheritance.Count != 4 || result.Profile.AppearanceField != new ResolvedTemplateField(rootPath, "SHOT", "0008") || result.Profile.AppearancePath != aptPath || result.Create != message || ReferenceEquals(result.Create, message) || geometry.Count != 4 || result.Appearance.Meshes.Count != 2 || result.Appearance.Diagnostics.Count != 2) throw new Exception("Original static geometry/partial-material identity/read-once changed.");
        var geometryHashes = geometry.ToDictionary(n => n.Path, n => StaticMeshVerification.ComputeGeometrySha256(n.Mesh!));
        var pixelHashes = textures.ToDictionary(p => p.Key, p => p.Value.Mips.Select(m => Convert.ToHexString(SHA256.HashData(m.Rgba.Span))).ToArray());
        string profileHash = ProfileHash(result.Profile); foreach (byte[] b in borrowed) Array.Clear(b);
        if (profileHash != ProfileHash(result.Profile)) throw new Exception("Original profile borrowed source.");
        foreach (var node in geometry) if (geometryHashes[node.Path] != StaticMeshVerification.ComputeGeometrySha256(node.Mesh!)) throw new Exception("Original geometry borrowed source.");
        foreach (var pair in textures) if (!pixelHashes[pair.Key].SequenceEqual(pair.Value.Mips.Select(m => Convert.ToHexString(SHA256.HashData(m.Rgba.Span))))) throw new Exception("Original pixels borrowed source.");
        Console.WriteLine("static_catalog=" + JsonSerializer.Serialize(entry));
        Console.WriteLine("static_create=" + JsonSerializer.Serialize(new { wire = Convert.ToHexString(SceneObjectLifecycleCodec.EncodeCreate(result.Create)), result.Create, result.ExternalReads, result.SourceBytes, counts,
            profile = new { result.Profile.RootPath, result.Profile.AppearancePath, result.Profile.AppearanceField }, rootGeometry = result.Appearance.Geometry.Root.Path, geometryMeshes = geometry.Count, materialMeshes = result.Appearance.Meshes.Count, shaders = materials.Count, textures = textures.Count, effects = effects.Length, diagnostics = result.Appearance.Diagnostics, materialPolicy = (int)result.MaterialPolicy }));
        foreach (var source in result.Sources.Values) Console.WriteLine("static_source=" + JsonSerializer.Serialize(source));
        foreach (var item in result.Profile.Inheritance) Console.WriteLine("static_inheritance=" + JsonSerializer.Serialize(new { item.SourcePath, layers = item.Layers.Select(l => new { l.Family, l.Version, l.ParentPath, appearance = l.Parameters.TryGetValue("appearanceFilename", out var data) ? Convert.ToHexString(data.Span) : null }) }));
        foreach (var node in geometry) Console.WriteLine("static_geometry=" + JsonSerializer.Serialize(new { path = node.Path, source = node.MeshSourcePath, vertices = node.Mesh!.VertexCount, triangles = node.Mesh.TriangleCount, hash = geometryHashes[node.Path], shaders = node.Mesh.Primitives.Select(p => p.ShaderPath).Distinct() }));
        foreach (var pair in result.Appearance.Meshes) Console.WriteLine("static_material_mesh=" + JsonSerializer.Serialize(new { path = pair.Key, shaders = pair.Value.Materials.Keys, textures = pair.Value.Textures.Keys, effects = pair.Value.Effects.Keys }));
        foreach (var pair in materials) Console.WriteLine("static_material=" + JsonSerializer.Serialize(new { path = pair.Key, effect = pair.Value.References.EffectPath, textures = pair.Value.References.Textures.Select(t => new { t.Tag, t.SourceFlag, t.AssetPath }) }));
        foreach (var pair in textures) Console.WriteLine("static_texture=" + JsonSerializer.Serialize(new { path = pair.Key, mips = pixelHashes[pair.Key] }));

        SceneStaticResources Load(Func<string, TreLookupResult> resolver, SceneStaticMaterialPolicy policy = SceneStaticMaterialPolicy.RequireComplete, ObjectTemplateCatalog? c = null, AppearanceGraphLimits? limits = null, long decoded = 134217728, CancellationToken token = default)
            => SceneStaticResourceReader.Load(message, c ?? catalog, resolver, policy, token, limits, decoded);
        Reject<InvalidDataException>(() => Load(overlay.Lookup)); // Original l1/l0 unsupported shaders make completeness explicitly unavailable.
        foreach (var wrong in new[] { entry with { ObjectSha256 = hash }, entry with { AppearanceSha256 = hash } }) Reject<InvalidDataException>(() => Load(overlay.Lookup, c: new ObjectTemplateCatalog(new[] { wrong })));
        foreach (string path in result.Profile.Inheritance.Select(i => i.SourcePath).Concat(new[] { aptPath, "appearance/lod/ksk_all_mission.lod" }).Concat(geometry.Select(g => g.MeshSourcePath!)))
            foreach (var kind in new[] { TreLookupKind.Missing, TreLookupKind.Masked }) Reject<InvalidDataException>(() => Load(p => p == path ? new(kind) : overlay.Lookup(p), SceneStaticMaterialPolicy.AllowNeutral));
        foreach (string pinned in new[] { rootPath, aptPath }) Reject<InvalidDataException>(() => Load(p => { var r = overlay.Lookup(p); if (p == pinned) r.Payload![^1] ^= 1; return r; }));
        string missingMaterial = materials.Keys.First(); int neutralCases = 0;
        foreach (var kind in new[] { TreLookupKind.Missing, TreLookupKind.Masked })
        {
            Reject<InvalidDataException>(() => Load(p => p == missingMaterial ? new(kind) : overlay.Lookup(p)));
            var calls = new Dictionary<string, int>(StringComparer.Ordinal);
            var neutral = Load(p => { calls[p] = calls.GetValueOrDefault(p) + 1; return p == missingMaterial ? new(kind) : overlay.Lookup(p); }, SceneStaticMaterialPolicy.AllowNeutral);
            if (Geometry(neutral).Count != 4 || neutral.Appearance.Diagnostics.Count <= result.Appearance.Diagnostics.Count || calls.Values.Any(n => n != 1) || neutral.Appearance.Meshes.Count >= result.Appearance.Meshes.Count) throw new Exception("Explicit material-neutral diagnostic policy changed.");
            Console.WriteLine("static_neutral=" + JsonSerializer.Serialize(new { kind = kind.ToString(), missingMaterial, geometryMeshes = Geometry(neutral).Count, materialMeshes = neutral.Appearance.Meshes.Count, diagnostics = neutral.Appearance.Diagnostics, reads = neutral.ExternalReads })); neutralCases++;
        }
        foreach (var budget in new[] { new AppearanceGraphLimits(MaximumResources: 1), new AppearanceGraphLimits(MaximumSourceBytes: 1), new AppearanceGraphLimits(MaximumResources: result.ExternalReads - 1), new AppearanceGraphLimits(MaximumSourceBytes: result.SourceBytes - 1), new AppearanceGraphLimits(MaximumDepth: 1), new AppearanceGraphLimits(MaximumEdges: 1) })
            Reject<AppearanceGraphBudgetException>(() => Load(overlay.Lookup, limits: budget));
        Reject<AppearanceGraphBudgetException>(() => Load(overlay.Lookup, decoded: 4));
        using (var cancel = new CancellationTokenSource())
        { int calls = 0; Reject<OperationCanceledException>(() => Load(p => { calls++; var r = overlay.Lookup(p); cancel.Cancel(); return r; }, token: cancel.Token)); if (calls != 1) throw new Exception("Canceled resolver continued."); }
        // CMP part omission must reject even when the caller permits neutral MATERIALS.
        byte[] cmp = MeshScenarios.Form("CMPA", MeshScenarios.Form("0003", MeshScenarios.Form("APPR", MeshScenarios.Form("0003")), MeshScenarios.Chunk("PART", Encoding.ASCII.GetBytes("mesh/synthetic.msh\0").Concat(new float[] { 1,0,0,0,0,1,0,0,0,0,1,0 }.SelectMany(BitConverter.GetBytes)).ToArray())));
        var tinyData = new Dictionary<string, byte[]> { [tiny] = Template(null, StringMode("appearance/synthetic.cmp")), ["appearance/synthetic.cmp"] = cmp, ["appearance/mesh/synthetic.msh"] = MeshScenarios.Mesh("0001") };
        var tinyEntry = new ObjectTemplateCatalogEntry(LegacyAssetPath.Checksum(tiny), tiny, Hash(tinyData[tiny]), Hash(cmp), SceneObjectResourceRecipe.StaticAppearance);
        var tinyCatalog = new ObjectTemplateCatalog(new[] { tinyEntry }); var tinyMessage = message with { TemplateCrc = tinyEntry.TemplateCrc };
        TreLookupResult TinyRead(string p) => tinyData.TryGetValue(p, out var b) ? new(TreLookupKind.Present, b) : new(TreLookupKind.Missing);
        var tinyResult = SceneStaticResourceReader.Load(tinyMessage, tinyCatalog, TinyRead, SceneStaticMaterialPolicy.AllowNeutral);
        if (Geometry(tinyResult).Count != 1 || tinyResult.Appearance.Diagnostics.Count == 0) throw new Exception("Tiny neutral fixture failed.");
        tinyData["shader/synthetic.sht"] = MaterialScenarios.Shader(); tinyData["effect/synthetic.eft"] = MaterialScenarios.Effect();
        tinyData["texture/synthetic.dds"] = DdsScenarios.Fixture("DXT1", 4, 4, 1, new byte[8]);
        var tinyComplete = SceneStaticResourceReader.Load(tinyMessage, tinyCatalog, TinyRead, SceneStaticMaterialPolicy.RequireComplete);
        if (tinyComplete.Appearance.Meshes.Count != 1 || tinyComplete.Appearance.Diagnostics.Count != 0) throw new Exception("Strict complete synthetic materials failed.");
        foreach (string missing in new[] { "shader/synthetic.sht", "effect/synthetic.eft", "texture/synthetic.dds" })
            foreach (var kind in new[] { TreLookupKind.Missing, TreLookupKind.Masked }) Reject<InvalidDataException>(() => SceneStaticResourceReader.Load(tinyMessage, tinyCatalog, p => p == missing ? new(kind) : TinyRead(p), SceneStaticMaterialPolicy.RequireComplete));
        foreach (var kind in new[] { TreLookupKind.Missing, TreLookupKind.Masked }) Reject<InvalidDataException>(() => SceneStaticResourceReader.Load(tinyMessage, tinyCatalog, p => p == "appearance/mesh/synthetic.msh" ? new(kind) : TinyRead(p), SceneStaticMaterialPolicy.AllowNeutral));
        var fresh = Load(overlay.Lookup, SceneStaticMaterialPolicy.AllowNeutral, limits: new(MaximumResources: result.ExternalReads, MaximumSourceBytes: result.SourceBytes));
        if (fresh.SourceBytes != result.SourceBytes || fresh.ExternalReads != result.ExternalReads) throw new Exception("Failure contaminated fresh load/exact budget boundary.");
        Console.WriteLine("static_validation=" + JsonSerializer.Serialize(new { rejects, syntheticProfiles, neutralCases, preflightReads, canceledResolverCalls = 1, sourceCloneChecks = borrowed.Count, geometryOwnershipChecks = geometryHashes.Count, textureOwnershipChecks = pixelHashes.Count, configSha256 = RetainedSkeletalDetailBundleReader.ConfigurationPin, gpu = false }));
        Console.WriteLine("PASS R7-ASSETS-01: explicit static recipe, original SHOT0008/terminal inheritance and owned textured graph; modern strict/neutral policy, no engine/world readiness.");
    }
}
