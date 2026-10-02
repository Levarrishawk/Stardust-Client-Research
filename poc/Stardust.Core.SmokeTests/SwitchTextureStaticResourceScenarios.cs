using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Proof;

internal static class SwitchTextureStaticResourceScenarios
{
    private const string Shader = "shader/anim_mission_terminal.sht";
    private const string Pin = "B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62";
    private static string Hash(ReadOnlySpan<byte> bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private static byte[] Name(string s) => Encoding.ASCII.GetBytes(s + "\0");
    private static byte[] Fixture(string version = "0000", string dtst = "0000", string text = "TEXT", byte[]? name = null, byte[]? state = null, byte[]? texture = null)
        => MeshScenarios.Form("SWTS", MeshScenarios.Form(version,
            MeshScenarios.Chunk("NAME", name ?? Name("shader/synthetic.sht")),
            MeshScenarios.Form("DTST", MeshScenarios.Chunk(dtst, state ?? new byte[8])),
            MeshScenarios.Chunk(text, BitConverter.GetBytes(0x4D41494Eu).Concat(texture ?? Name("texture/synthetic.dds")).ToArray())));
    internal static void Run(string[] args)
    {
        if (args.Length != 2) throw new ArgumentException("Explicit original root and pinned configuration required.");
        int rejects = 0;
        void Reject<T>(Action action) where T : Exception
        { try { action(); } catch (T) { rejects++; return; } throw new Exception("Expected " + typeof(T).Name); }
        byte[] fixture = Fixture(); var decoded = SwitchTextureShaderDecoder.Decode(fixture);
        if (decoded.Frames.Count != 1 || decoded.RawDtstWord0 != 0 || decoded.RawDtstWord1 != 0) throw new Exception("Opaque DTST changed.");
        string metadata = Hash(decoded.Metadata.Payload.Span); Array.Clear(fixture);
        if (metadata != Hash(decoded.Metadata.Payload.Span)) throw new Exception("SWTS metadata borrowed source.");
        foreach (var bad in new[] { Fixture(version: "0001"), Fixture(dtst: "0001"), Fixture(text: "NAME"), Fixture(name: Name("../shader/x.sht")), Fixture(name: Name("texture/x.dds")), Fixture(name: new byte[] { 65 }), Fixture(name: new byte[] { 65, 0, 0 }), Fixture(name: new byte[] { 0xE9, 0 }), Fixture(state: new byte[7]), Fixture(state: new byte[9]) })
            Reject<InvalidDataException>(() => SwitchTextureShaderDecoder.Decode(bad));
        foreach (var badPath in new[] { "shader/./x.sht", "shader/a/../x.sht", "shader\\.\\x.sht" }) Reject<InvalidDataException>(() => SwitchTextureShaderDecoder.Decode(Fixture(name: Name(badPath))));
        foreach (var badPath in new[] { "texture/./x.dds", "texture/a/../x.dds", "texture\\..\\x.dds", "shader/x.sht" }) Reject<InvalidDataException>(() => SwitchTextureShaderDecoder.Decode(Fixture(texture: Name(badPath))));
        foreach (byte[] badName in new[] { Array.Empty<byte>(), new byte[] { 65 }, new byte[] { 65, 0, 0 }, new byte[] { 0xE9, 0 } }) Reject<InvalidDataException>(() => SwitchTextureShaderDecoder.Decode(Fixture(texture: badName)));
        byte[] valid = Fixture(); for (int n = 0; n < valid.Length; n++) Reject<InvalidDataException>(() => SwitchTextureShaderDecoder.Decode(valid.AsSpan(0, n)));
        Reject<InvalidDataException>(() => SwitchTextureShaderDecoder.Decode(valid.Concat(new byte[1]).ToArray()));
        Reject<InvalidDataException>(() => SwitchTextureShaderDecoder.Decode(MeshScenarios.Form("SWTS", MeshScenarios.Form("0000", MeshScenarios.Chunk("NAME", Name("shader/x.sht")), MeshScenarios.Form("DTST", MeshScenarios.Chunk("0000", new byte[8]))))));
        var baseShader = StaticShaderReferences.Decode(MaterialScenarios.Shader());
        Reject<InvalidDataException>(() => SwitchTextureShaderDecoder.Select(decoded, baseShader, -1));
        Reject<InvalidDataException>(() => SwitchTextureShaderDecoder.Select(decoded, baseShader, 1));
        Reject<InvalidDataException>(() => SwitchTextureShaderDecoder.Select(decoded, baseShader with { Textures = Array.Empty<ShaderTextureReference>() }, 0));
        Reject<InvalidDataException>(() => SwitchTextureShaderDecoder.Select(decoded, baseShader with { Textures = baseShader.Textures.Concat(baseShader.Textures).ToArray() }, 0));
        Reject<InvalidDataException>(() => SwitchTextureShaderDecoder.Select(decoded, baseShader with { Textures = baseShader.Textures.Select(t => t with { SourceFlag = 1 }).ToArray() }, 0));
        var entry = new ObjectTemplateCatalogEntry(2907007746, "object/tangible/terminal/shared_terminal_mission.iff",
            "E9FE68A85ED45CA33AC2715BD7275C01AFB1CFC837E5CEB61E8E3AB567586281", "74316F0F9E28961C3AB983D5DF51DA313A3A4E8AD96358FECC04F747801B6021", SceneObjectResourceRecipe.StaticAppearance);
        var catalog = new ObjectTemplateCatalog(new[] { entry });
        var create = SceneObjectLifecycleCodec.DecodeCreate(SceneObjectLifecycleCodec.EncodeCreate(new(0xFEDCBA9876543210, -0f, .25f, -.5f, 2, 123.5f, -45.25f, float.Epsilon, entry.TemplateCrc, 255)));
        int preflight = 0; TreLookupResult Never(string _) { preflight++; throw new Exception("Preflight performed lookup."); }
        foreach (var invalid in new[] { new SwitchTextureFrameSelection(Shader, "bad", 0), new(Shader, Pin, -1), new(Shader, Pin, 256), new("../shader/x.sht", Pin, 0), new("shader/./x.sht", Pin, 0), new("shader/a/../x.sht", Pin, 0), new("texture/x.dds", Pin, 0) })
            Reject<ArgumentException>(() => SceneStaticResourceReader.Load(create, catalog, Never, SceneStaticMaterialPolicy.RequireComplete, switchTextureFrame: invalid));
        if (preflight != 0) throw new Exception("Bad selection read source.");
        if (Hash(File.ReadAllBytes(args[1])) != RetainedSkeletalDetailBundleReader.ConfigurationPin) throw new InvalidDataException("Configuration identity mismatch.");
        using var configuration = File.OpenText(args[1]); using var overlay = ConfiguredTreOverlay.Open(args[0], configuration);
        SceneStaticResources Load(Func<string, TreLookupResult> resolve, SwitchTextureFrameSelection? selection, SceneStaticMaterialPolicy policy = SceneStaticMaterialPolicy.RequireComplete,
            AppearanceGraphLimits? limits = null, long budget = 134217728, CancellationToken token = default)
            => SceneStaticResourceReader.Load(create, catalog, resolve, policy, token, limits, budget, selection);
        var originals = new List<SceneStaticResources>(); var firstPixels = new List<string>();
        for (int frame = 0; frame < 2; frame++)
        {
            var selection = new SwitchTextureFrameSelection(Shader, Pin, frame); var counts = new Dictionary<string, int>(); var borrowed = new List<byte[]>();
            TreLookupResult Read(string p) { counts[p] = counts.GetValueOrDefault(p) + 1; var r = overlay.Lookup(p); if (r.Payload is not null) borrowed.Add(r.Payload); return r; }
            var result = Load(Read, selection); originals.Add(result);
            var meshes = result.Appearance.Meshes; var materials = meshes.Values.SelectMany(m => m.Materials).GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.First().Value);
            var textures = meshes.Values.SelectMany(m => m.Textures).GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.First().Value);
            var switched = materials[Shader].SwitchTexture ?? throw new Exception("Missing switch metadata."); string selectedPath = switched.Description.Frames[frame].TexturePath;
            if (meshes.Count != 4 || result.Appearance.Diagnostics.Count != 0 || counts.Values.Any(n => n != 1) || counts.Count != result.ExternalReads || !counts.Keys.ToHashSet().SetEquals(result.Sources.Keys) || result.Create != create || ReferenceEquals(result.Create, create) || switched.Description.Frames.Count != 8 || switched.Description.RawDtstWord0 != 8 || switched.Description.RawDtstWord1 != 0x3DCCCCCD || switched.Selection != selection || switched.BaseShaderSource != result.Sources[switched.Description.BaseShaderPath]) throw new Exception("Original strict closure/identity changed.");
            if (switched.Description.Frames.Where((_, i) => i != frame).Any(f => counts.ContainsKey(f.TexturePath)) || counts.ContainsKey("texture/defaulttexture.dds")) throw new Exception("Unselected texture loaded.");
            var geometry = meshes.ToDictionary(p => p.Key, p => StaticMeshVerification.ComputeGeometrySha256(p.Value.Mesh));
            var pixels = textures.ToDictionary(p => p.Key, p => p.Value.Mips.Select(m => Hash(m.Rgba.Span)).ToArray()); firstPixels.Add(pixels[selectedPath][0]);
            string owned = Hash(switched.Description.Metadata.Payload.Span); foreach (byte[] b in borrowed) Array.Clear(b);
            if (owned != Hash(switched.Description.Metadata.Payload.Span) || meshes.Any(p => geometry[p.Key] != StaticMeshVerification.ComputeGeometrySha256(p.Value.Mesh)) || textures.Any(p => !pixels[p.Key].SequenceEqual(p.Value.Mips.Select(m => Hash(m.Rgba.Span))))) throw new Exception("Output borrowed resolver memory.");
            Console.WriteLine("switch_frame=" + JsonSerializer.Serialize(new { frame, result.ExternalReads, result.SourceBytes, counts, sources = result.Sources.Values, wire = Convert.ToHexString(SceneObjectLifecycleCodec.EncodeCreate(result.Create)),
                baseSource = switched.BaseShaderSource, selectedPath, raw0 = switched.Description.RawDtstWord0, raw1 = switched.Description.RawDtstWord1, frames = switched.Description.Frames,
                meshes = meshes.Select(p => new { path = p.Key, vertices = p.Value.Mesh.VertexCount, triangles = p.Value.Mesh.TriangleCount, hash = geometry[p.Key], shaders = p.Value.Materials.Keys, textures = p.Value.Textures.Keys, effects = p.Value.Effects.Keys }),
                materials = materials.Select(p => new { path = p.Key, effect = p.Value.References.EffectPath, textures = p.Value.References.Textures.Select(t => new { t.Tag, t.SourceFlag, t.AssetPath }) }),
                textures = pixels.Select(p => new { path = p.Key, mips = p.Value }) }));
        }
        if (firstPixels[0] == firstPixels[1] || originals[0].Sources.Keys.ToHashSet().SetEquals(originals[1].Sources.Keys)) throw new Exception("Authored frames did not differ.");
        var good = new SwitchTextureFrameSelection(Shader, Pin, 0);
        Reject<InvalidDataException>(() => Load(overlay.Lookup, null));
        var defaultNeutral = Load(overlay.Lookup, null, SceneStaticMaterialPolicy.AllowNeutral);
        if (defaultNeutral.Appearance.Meshes.Count != 2 || defaultNeutral.Appearance.Diagnostics.Count != 2) throw new Exception("Default SWTS rejection changed.");
        foreach (var bad in new[] { good with { ShaderSha256 = new string('0', 64) }, good with { FrameIndex = 8 }, good with { ShaderPath = "shader/unselected.sht" } }) Reject<InvalidDataException>(() => Load(overlay.Lookup, bad));
        // Synthetic resolver variant: two logical shader paths have identical original bytes in one shared-cache load.
        const string alias = "shader/anim_mission_terminax.sht";
        TreLookupResult Aliased(string path)
        {
            if (path == alias) return overlay.Lookup(Shader);
            var r = overlay.Lookup(path);
            if (path == "appearance/mesh/ksk_all_mission_l0.msh")
            {
                byte[] ownedBytes = r.Payload!.ToArray(); int offset = ownedBytes.AsSpan().IndexOf(Encoding.ASCII.GetBytes(Shader + "\0"));
                if (offset < 0 || alias.Length != Shader.Length) throw new Exception("Alias fixture mismatch.");
                Encoding.ASCII.GetBytes(alias).CopyTo(ownedBytes, offset); return r with { Payload = ownedBytes };
            }
            return r;
        }
        Reject<InvalidDataException>(() => Load(Aliased, good));
        var aliased = Load(Aliased, good, SceneStaticMaterialPolicy.AllowNeutral);
        if (aliased.Appearance.Meshes.Count != 3 || aliased.Appearance.Diagnostics.Count != 1 || aliased.Sources[alias].Sha256 != Pin) throw new Exception("Unselected identical-byte alias inherited pinned selection.");
        foreach (string dependency in new[] { Shader, "shader/defaultemis.sht", originals[0].Appearance.Meshes.Values.SelectMany(m => m.Materials).First(p => p.Key == Shader).Value.References.Textures.Single().AssetPath! })
            foreach (var kind in new[] { TreLookupKind.Missing, TreLookupKind.Masked })
            {
                TreLookupResult Missing(string p) => p == dependency ? new(kind) : overlay.Lookup(p);
                Reject<InvalidDataException>(() => Load(Missing, good)); var neutral = Load(Missing, good, SceneStaticMaterialPolicy.AllowNeutral);
                if (neutral.Appearance.Meshes.Count != 2 || neutral.Appearance.Diagnostics.Count != 2) throw new Exception("Selected failure did not retain neutral geometry.");
            }
        foreach (byte[] badBase in new[] { Fixture(), Array.Empty<byte>() }) Reject<InvalidDataException>(() => Load(p => p == "shader/defaultemis.sht" ? new(TreLookupKind.Present, badBase) : overlay.Lookup(p), good));
        foreach (var limits in new[] { new AppearanceGraphLimits(MaximumResources: originals[0].ExternalReads - 1), new AppearanceGraphLimits(MaximumSourceBytes: originals[0].SourceBytes - 1) }) Reject<AppearanceGraphBudgetException>(() => Load(overlay.Lookup, good, limits: limits));
        Reject<AppearanceGraphBudgetException>(() => Load(overlay.Lookup, good, budget: 4));
        var exact = Load(overlay.Lookup, good, limits: new(MaximumResources: originals[0].ExternalReads, MaximumSourceBytes: originals[0].SourceBytes));
        if (exact.SourceBytes != originals[0].SourceBytes) throw new Exception("Failed load contaminated fresh exact-budget result.");
        using (var canceled = new CancellationTokenSource()) { canceled.Cancel(); Reject<OperationCanceledException>(() => Load(Never, good, token: canceled.Token)); }
        using (var canceled = new CancellationTokenSource()) { int reads = 0; Reject<OperationCanceledException>(() => Load(p => { reads++; var r = overlay.Lookup(p); canceled.Cancel(); return r; }, good, token: canceled.Token)); if (reads != 1) throw new Exception("Cancellation continued reads."); }
        Console.WriteLine("switch_validation=" + JsonSerializer.Serialize(new { rejects, preflightReads = preflight, originalFrames = 2, completeMeshesPerFrame = 4, distinctPixels = true, gpu = false, configSha256 = RetainedSkeletalDetailBundleReader.ConfigurationPin }));
        Console.WriteLine("PASS R8-ASSETS-01: explicit pinned SWTS frame inspection, strict terminal materials, owned CPU data; no runtime switcher.");
    }
}
