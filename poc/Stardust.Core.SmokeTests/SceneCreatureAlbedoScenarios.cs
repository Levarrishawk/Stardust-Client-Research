using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

internal static class SceneCreatureAlbedoScenarios
{
    private static string Hash(ReadOnlySpan<byte> bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private static string Factors(SkeletalAlbedoFactorBundle bundle) => Hash(JsonSerializer.SerializeToUtf8Bytes(bundle.Materials.OrderBy(p => p.Key, StringComparer.Ordinal)));
    internal static void Run(string[] args)
    {
        if (args.Length != 4) throw new ArgumentException("Explicit original root, pinned configuration, catalog and animation-bank profiles required.");
        if (Hash(File.ReadAllBytes(args[1])) != RetainedSkeletalDetailBundleReader.ConfigurationPin) throw new InvalidDataException("Configuration pin mismatch.");
        var catalog = ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(args[2])); var entry = catalog.Require(2937962913);
        using var bankFile = File.OpenRead(args[3]); var bank = SkeletalAnimationBankProfile.Load(bankFile);
        var idle = new LogicalAnimationClipSelection("appearance/lat/all_m.lat", "emt_standing_idle_ambient_1_ag", "798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        var create = SceneObjectLifecycleCodec.DecodeCreate(SceneObjectLifecycleCodec.EncodeCreate(new(0xFEDCBA9876543210, -0f, .25f, -.5f, 2, 123.5f, -45.25f, float.Epsilon, entry.TemplateCrc, 255)));
        using var configuration = File.OpenText(args[1]); using var overlay = ConfiguredTreOverlay.Open(args[0], configuration);
        SceneCreatureResources Load(Func<string, TreLookupResult> resolve, bool factors = true, int resources = 64, long bytes = 16777216, ObjectTemplateCatalog? recipes = null, CancellationToken token = default)
            => SceneCreatureResourceReader.Load(create, recipes ?? catalog, idle, bank, resolve, token, resources, bytes, factors);
        var defaultCalls = new Dictionary<string, int>();
        TreLookupResult DefaultRead(string path) { defaultCalls[path] = defaultCalls.GetValueOrDefault(path) + 1; return overlay.Lookup(path); }
        // Omit the new parameter to exercise the retained production default.
        var defaults = SceneCreatureResourceReader.Load(create, catalog, idle, bank, DefaultRead);
        if (defaults.Creature.MaterialBundle!.AlbedoFactors is not null || defaults.ExternalReads != 37 || defaults.SourceBytes != 4683848 || defaultCalls.Values.Any(n => n != 1)) throw new Exception("Default resource closure changed.");
        var explicitFalse = Load(overlay.Lookup, false);
        if (explicitFalse.Creature.MaterialBundle!.AlbedoFactors is not null || !defaults.Creature.Sources.SequenceEqual(explicitFalse.Creature.Sources)) throw new Exception("Explicit false differs from default.");
        var counts = new Dictionary<string, int>(); var borrowed = new List<byte[]>();
        TreLookupResult Read(string path) { counts[path] = counts.GetValueOrDefault(path) + 1; var result = overlay.Lookup(path); if (result.Payload is not null) borrowed.Add(result.Payload); return result; }
        var loaded = Load(Read); var materials = loaded.Creature.MaterialBundle!;
        var albedo = materials.AlbedoFactors ?? throw new Exception("Opt-in albedo missing.");
        if (loaded.ExternalReads != 39 || loaded.SourceBytes != 4686371 || albedo.Materials.Count != 4 || albedo.Programs.Count != 2 || counts.Values.Any(n => n != 1) || counts.Count != loaded.Creature.Sources.Count || loaded.Create != create || ReferenceEquals(loaded.Create, create) || loaded.Recipe != entry || materials.Textures.Count != 10 || !loaded.Creature.Sources.Keys.ToHashSet().SetEquals(defaults.Creature.Sources.Keys.Concat(albedo.Programs.Keys))) throw new Exception("Opt-in closure/identity changed.");
        string factorsBefore = Factors(albedo);
        var programOwned = albedo.Programs.ToDictionary(p => p.Key, p => Hash(p.Value.Metadata.Payload.Span));
        var pixels = materials.Textures.ToDictionary(p => p.Key, p => p.Value.Mips.Select(m => Hash(m.Rgba.Span)).ToArray());
        foreach (byte[] bytes in borrowed) Array.Clear(bytes);
        if (Factors(albedo) != factorsBefore || albedo.Programs.Any(p => programOwned[p.Key] != Hash(p.Value.Metadata.Payload.Span)) || materials.Textures.Any(p => !pixels[p.Key].SequenceEqual(p.Value.Mips.Select(m => Hash(m.Rgba.Span))))) throw new Exception("Albedo/program/pixels borrowed source arrays.");
        if (!defaults.Creature.MaterialBundle!.Textures.All(p => pixels[p.Key].SequenceEqual(p.Value.Mips.Select(m => Hash(m.Rgba.Span))))) throw new Exception("Loading factors changed retained source pixels.");
        Console.WriteLine("creature_albedo_default=" + JsonSerializer.Serialize(new { defaults.ExternalReads, defaults.SourceBytes, counts = defaultCalls, sources = defaults.Creature.Sources.Values }));
        Console.WriteLine("creature_albedo_create=" + JsonSerializer.Serialize(new { wire = Convert.ToHexString(SceneObjectLifecycleCodec.EncodeCreate(loaded.Create)), loaded.Recipe, loaded.ExternalReads, loaded.SourceBytes, counts, root = loaded.Creature.Profile.RootPath, appearance = loaded.Creature.Profile.AppearancePath }));
        foreach (var source in loaded.Creature.Sources.Values) Console.WriteLine("creature_albedo_source=" + JsonSerializer.Serialize(source));
        foreach (var pair in albedo.Materials) Console.WriteLine("creature_albedo_material=" + JsonSerializer.Serialize(new { path = pair.Key, factors = pair.Value }));
        foreach (var pair in albedo.Programs) Console.WriteLine("creature_albedo_program=" + JsonSerializer.Serialize(new { path = pair.Key, pair.Value.UsesAlphaHueMask, bytecodeSha256 = Hash(pair.Value.Metadata.Children[0].Children[1].Payload.Span) }));
        foreach (var pair in materials.Textures) Console.WriteLine("creature_albedo_texture=" + JsonSerializer.Serialize(new { path = pair.Key, mips = pixels[pair.Key] }));
        int changed = 0;
        foreach (var pair in albedo.Materials)
        {
            string main = materials.Materials[pair.Key].References.Textures.Single(t => t.Tag == 0x4D41494E).AssetPath!;
            ReadOnlySpan<byte> rgba = materials.Textures[main].Mips[0].Rgba.Span; int selected = 0; Vector4 sample = default; Vector3 color = default;
            for (int offset = 0; offset < rgba.Length; offset += 4)
            {
                sample = new(rgba[offset] / 255f, rgba[offset + 1] / 255f, rgba[offset + 2] / 255f, rgba[offset + 3] / 255f);
                color = SkeletalAlbedoEvaluator.Evaluate(sample, pair.Value); selected = offset;
                if (Vector3.Distance(color, new(sample.X, sample.Y, sample.Z)) > 1e-5) break;
            }
            bool differs = Vector3.Distance(color, new(sample.X, sample.Y, sample.Z)) > 1e-5; if (differs) changed++;
            Console.WriteLine("creature_albedo_color=" + JsonSerializer.Serialize(new { shader = pair.Key, texture = main, pixel = selected / 4, rgba = Convert.ToHexString(rgba.Slice(selected, 4)), main = new[] { sample.X, sample.Y, sample.Z }, color = new[] { color.X, color.Y, color.Z }, differs }));
        }
        if (changed == 0) throw new Exception("Original factor-derived color did not differ from MAIN-only.");

        int rejects = 0;
        void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { rejects++; return; } throw new Exception("Expected " + typeof(T).Name); }
        foreach (string program in albedo.Programs.Keys)
        {
            foreach (var kind in new[] { TreLookupKind.Missing, TreLookupKind.Masked }) Reject<InvalidDataException>(() => Load(p => p == program ? new(kind) : overlay.Lookup(p)));
            Reject<InvalidDataException>(() => Load(p => { var r = overlay.Lookup(p); if (p == program) r.Payload![^4] ^= 1; return r; }));
            Reject<InvalidDataException>(() => Load(p => { var r = overlay.Lookup(p); return p == program ? r with { Payload = r.Payload![..^1] } : r; }));
            var noProgram = Load(p => p == program ? new(TreLookupKind.Masked) : overlay.Lookup(p), false);
            if (noProgram.ExternalReads != 37 || noProgram.SourceBytes != 4683848 || noProgram.Creature.MaterialBundle!.AlbedoFactors is not null) throw new Exception("Default gained a program dependency.");
        }
        foreach (var wrong in new[] { entry with { ObjectSha256 = new string('0', 64) }, entry with { AppearanceSha256 = new string('0', 64) } }) Reject<InvalidDataException>(() => Load(overlay.Lookup, recipes: new(new[] { wrong })));
        foreach (string pinned in new[] { entry.ObjectPath, loaded.Creature.Profile.AppearancePath }) Reject<InvalidDataException>(() => Load(p => { var r = overlay.Lookup(p); if (p == pinned) r.Payload![^1] ^= 1; return r; }));
        Reject<AppearanceGraphBudgetException>(() => Load(overlay.Lookup, resources: 38)); Reject<AppearanceGraphBudgetException>(() => Load(overlay.Lookup, bytes: 4686370));
        var exact = Load(overlay.Lookup, resources: 39, bytes: 4686371);
        if (Factors(exact.Creature.MaterialBundle!.AlbedoFactors!) != factorsBefore) throw new Exception("Failure contaminated fresh exact-budget albedo.");
        using (var canceled = new CancellationTokenSource()) { canceled.Cancel(); Reject<OperationCanceledException>(() => Load(_ => throw new Exception("Canceled before lookup performed IO."), token: canceled.Token)); }
        int cancellationReads = 0;
        using (var canceled = new CancellationTokenSource()) { Reject<OperationCanceledException>(() => Load(p => { cancellationReads++; var r = overlay.Lookup(p); canceled.Cancel(); return r; }, token: canceled.Token)); }
        if (cancellationReads != 1) throw new Exception("Cancellation continued external reads.");

        // Accepted initializer sees the larger exact source union without an initializer/API change.
        var registry = new SceneObjectRegistry(1, 12); var request = new SceneCreatureResourceRequest(registry.Create(12, create)!, catalog, idle, bank, 39, 4686371);
        int archivesClosed = 0, workerThread = 0, ownerThread = Environment.CurrentManagedThreadId;
        Task<SceneCreatureResources>? worker = null;
        using var initializer = new SceneCreatureResourceInitializer(registry, (q, token) => worker = Task.Run(() =>
        {
            workerThread = Environment.CurrentManagedThreadId;
            try
            {
                using var config = File.OpenText(args[1]); using var mounts = ConfiguredTreOverlay.Open(args[0], config);
                return SceneCreatureResourceReader.Load(q.Snapshot.Create, q.Catalog, q.Idle, q.Movement, mounts.Lookup, token, q.MaximumResources, q.MaximumSourceBytes, loadAlbedoFactors: true);
            }
            finally { Interlocked.Exchange(ref archivesClosed, 1); }
        }));
        var ticket = initializer.Start(request); worker!.GetAwaiter().GetResult(); var completion = initializer.Poll().Single();
        if (completion.Kind != SceneCreatureResourceCompletionKind.Prepared || completion.Ticket != ticket || completion.Resources!.ExternalReads != 39 || completion.Resources.SourceBytes != 4686371 || Factors(completion.Resources.Creature.MaterialBundle!.AlbedoFactors!) != factorsBefore || archivesClosed != 1 || workerThread == ownerThread || initializer.OutstandingJobs != 0 || initializer.Poll().Count != 0) throw new Exception("Initializer albedo transfer/accounting changed.");
        RejectsInitializerBudget(38, 4686371); RejectsInitializerBudget(39, 4686370);
        void RejectsInitializerBudget(int count, long bytes)
        {
            var small = new SceneCreatureResourceRequest(request.Snapshot, catalog, idle, bank, count, bytes);
            using var owner = new SceneCreatureResourceInitializer(registry, (_, _) => Task.FromResult(loaded)); owner.Start(small);
            if (owner.Poll().Single() is not { Kind: SceneCreatureResourceCompletionKind.Faulted, Resources: null } || owner.OutstandingJobs != 0) throw new Exception("Initializer accounting budget accepted enlarged closure.");
        }
        initializer.Dispose(); initializer.DrainCompletion.GetAwaiter().GetResult(); registry.Close();
        Console.WriteLine("creature_albedo_validation=" + JsonSerializer.Serialize(new { rejects, changedColors = changed, sourceCloneChecks = borrowed.Count, programOwnershipChecks = programOwned.Count, textureOwnershipChecks = pixels.Count, cancellationReads, initializerPrepared = true, initializerBudgetFailures = 2, archivesClosedBeforeTransfer = archivesClosed, defaultSourceCount = 37, albedoSourceCount = 39, configSha256 = RetainedSkeletalDetailBundleReader.ConfigurationPin, gpu = false, fullLighting = false }));
        Console.WriteLine("PASS R10-ASSETS-01: optional owned original albedo factors, unchanged default and accepted initializer accounting.");
    }
}
