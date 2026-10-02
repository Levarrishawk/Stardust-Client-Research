using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;

internal static class SceneObjectResourceRouterScenarios
{
    private static string Hash(ReadOnlySpan<byte> b) => Convert.ToHexString(SHA256.HashData(b));
    private static void Emit(string kind, object value) => Console.WriteLine(kind + "=" + JsonSerializer.Serialize(value));
    private static SceneCreateObjectByCrcMessage Create(uint crc) => SceneObjectLifecycleCodec.DecodeCreate(SceneObjectLifecycleCodec.EncodeCreate(
        new(0xFEDCBA9876543210, -0f, .25f, -.5f, 2, 123.5f, -45.25f, float.Epsilon, crc, 255)));
    private static IReadOnlyDictionary<string, DdsImage> Images(SceneObjectResources r) => r.Creature?.Creature.MaterialBundle!.Textures ??
        r.Static!.Appearance.Meshes.Values.SelectMany(m => m.Textures).GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.First().Value);
    private static string Pixels(SceneObjectResources r)
    {
        var images = Images(r);
        return Hash(images.OrderBy(p => p.Key, StringComparer.Ordinal).SelectMany(p => p.Value.Mips).SelectMany(m => m.Rgba.ToArray()).ToArray());
    }
    private static string Owned(SceneObjectResources r)
    {
        object payload = r.Creature is not null ? (object)r.Creature : r.Static!;
        return Hash(JsonSerializer.SerializeToUtf8Bytes(payload));
    }
    internal static void Run(string[] args)
    {
        if (args.Length != 4) throw new ArgumentException("Explicit original root/config/catalog/animation-bank required.");
        if (Hash(File.ReadAllBytes(args[1])) != RetainedSkeletalDetailBundleReader.ConfigurationPin) throw new InvalidDataException("Configuration pin mismatch.");
        byte[] catalogBytes = File.ReadAllBytes(args[2]); var catalog = ObjectTemplateCatalogProfile.Decode(catalogBytes); Array.Clear(catalogBytes);
        using var bankFile = File.OpenRead(args[3]); var movement = SkeletalAnimationBankProfile.Load(bankFile).ToDictionary(p => p.Key, p => p.Value);
        var idle = new LogicalAnimationClipSelection("appearance/lat/all_m.lat", "emt_standing_idle_ambient_1_ag", "798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        var c = new SceneCreatureResourceOptions(idle, movement, true); movement.Clear();
        var frame = new SwitchTextureFrameSelection("shader/anim_mission_terminal.sht", "B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62", 0);
        var s = new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete, frame);
        var human = Create(2937962913); var terminal = Create(2907007746);
        int rejects = 0, preflightReads = 0; var callerThread = Environment.CurrentManagedThreadId;
        void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { rejects++; return; } throw new Exception("Expected " + typeof(T).Name); }
        TreLookupResult Never(string _) { preflightReads++; throw new Exception("Rejected route read a source."); }
        SceneObjectResources Preflight(SceneCreateObjectByCrcMessage message, SceneObjectResourceOptions options, ObjectTemplateCatalog? recipes = null, CancellationToken token = default)
            => SceneObjectResourceRouter.Load(message, recipes ?? catalog, options, Never, token);
        Reject<InvalidDataException>(() => Preflight(human with { TemplateCrc = 0 }, c));
        Reject<InvalidDataException>(() => Preflight(terminal with { TemplateCrc = 0 }, s));
        Reject<InvalidDataException>(() => Preflight(human, s)); Reject<InvalidDataException>(() => Preflight(terminal, c));
        Reject<InvalidDataException>(() => Preflight(human, c, new ObjectTemplateCatalog(new[] { catalog.Require(human.TemplateCrc) with { Recipe = SceneObjectResourceRecipe.StaticAppearance } })));
        Reject<InvalidDataException>(() => Preflight(terminal, s, new ObjectTemplateCatalog(new[] { catalog.Require(terminal.TemplateCrc) with { Recipe = SceneObjectResourceRecipe.CreatureDetail0 } })));
        foreach (var pair in new[] { (human, (SceneObjectResourceOptions)c), (terminal, (SceneObjectResourceOptions)s) })
        {
            Reject<InvalidDataException>(() => Preflight(pair.Item1 with { PositionY = float.NaN }, pair.Item2));
            Reject<InvalidDataException>(() => Preflight(pair.Item1 with { QuaternionW = float.PositiveInfinity }, pair.Item2));
            using var canceled = new CancellationTokenSource(); canceled.Cancel(); Reject<OperationCanceledException>(() => Preflight(pair.Item1, pair.Item2, token: canceled.Token));
        }
        Reject<ArgumentNullException>(() => SceneObjectResourceRouter.Load(null!, catalog, c, Never));
        Reject<ArgumentNullException>(() => SceneObjectResourceRouter.Load(human, null!, c, Never));
        Reject<ArgumentNullException>(() => SceneObjectResourceRouter.Load(human, catalog, null!, Never));
        Reject<ArgumentNullException>(() => SceneObjectResourceRouter.Load(human, catalog, c, null!));
        foreach (int n in new[] { 0, 4097 }) Reject<ArgumentOutOfRangeException>(() => new SceneCreatureResourceOptions(idle, c.Movement, true, maximumResources: n));
        foreach (long n in new[] { 0L, 134217729L }) Reject<ArgumentOutOfRangeException>(() => new SceneCreatureResourceOptions(idle, c.Movement, true, maximumSourceBytes: n));
        Reject<ArgumentException>(() => new SceneCreatureResourceOptions(idle, new Dictionary<string, LogicalAnimationClipSelection>(), true));
        foreach (var bad in new[] { idle with { ClipSha256 = "bad" }, idle with { TablePath = "appearance/a/../x.lat" }, idle with { TablePath = "object/x.iff" }, idle with { ExplicitChildRoute = new[] { -1 } }, idle with { ExplicitChildRoute = Enumerable.Repeat(0, 9).ToArray() } })
            Reject<ArgumentException>(() => new SceneCreatureResourceOptions(bad, c.Movement, true));
        Reject<InvalidDataException>(() => new SceneCreatureResourceOptions(idle with { LogicalName = "\n" }, c.Movement, true));
        Reject<ArgumentOutOfRangeException>(() => new SceneStaticResourceOptions((SceneStaticMaterialPolicy)255, frame));
        foreach (var bad in new[] { frame with { ShaderSha256 = "bad" }, frame with { ShaderPath = "../shader/x.sht" }, frame with { FrameIndex = -1 }, frame with { FrameIndex = 256 } })
            Reject<ArgumentException>(() => new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete, bad));
        foreach (var limits in new[] { new AppearanceGraphLimits(MaximumResources: 0), new(MaximumResources: 4097), new(MaximumSourceBytes: 0), new(MaximumSourceBytes: 134217729), new(MaximumDepth: 0), new(MaximumDepth: 33), new(MaximumEdges: 0), new(MaximumEdges: 4097) })
            Reject<ArgumentOutOfRangeException>(() => new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete, frame, limits));
        foreach (long n in new[] { 3L, 134217729L }) Reject<ArgumentOutOfRangeException>(() => new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete, frame, maximumDecodedBytes: n));
        int[] route = { 1, 2 }; var snap = new SceneCreatureResourceOptions(idle with { ExplicitChildRoute = route }, c.Movement, true); Array.Clear(route);
        if (!snap.Idle.ExplicitChildRoute!.SequenceEqual(new[] { 1, 2 }) || c.Movement.Count != 2) throw new Exception("Caller options not snapshotted.");
        Reject<NotSupportedException>(() => ((IDictionary<string, LogicalAnimationClipSelection>)c.Movement).Clear());
        if (preflightReads != 0) throw new Exception("Preflight reads occurred.");
        var results = new List<SceneObjectResources>(); int borrowedChecks = 0, cancellationCalls = 0;
        foreach (var pair in new[] { (human, (SceneObjectResourceOptions)c), (terminal, (SceneObjectResourceOptions)s) })
        {
            var message = pair.Item1; var options = pair.Item2;
            var result = Task.Factory.StartNew(() =>
            {
                int worker = Environment.CurrentManagedThreadId;
                if (worker == callerThread) throw new Exception("Worker load executed on caller thread.");
                var borrowed = new List<byte[]>(); var reads = new Dictionary<string, int>(); SceneObjectResources loaded; string pixels, owned;
                using (var config = File.OpenText(args[1]))
                using (var overlay = ConfiguredTreOverlay.Open(args[0], config))
                {
                    TreLookupResult Read(string path) { if (Environment.CurrentManagedThreadId != worker) throw new Exception("Resolver escaped worker."); reads[path] = reads.GetValueOrDefault(path) + 1; var b = overlay.Lookup(path); if (b.Payload is not null) borrowed.Add(b.Payload); return b; }
                    loaded = SceneObjectResourceRouter.Load(message, catalog, options, Read);
                    pixels = Pixels(loaded); owned = Owned(loaded);
                    foreach (byte[] b in borrowed) { Array.Clear(b); borrowedChecks++; }
                    if (Pixels(loaded) != pixels || Owned(loaded) != owned) throw new Exception("Router payload borrowed raw sources.");
                }
                if (Pixels(loaded) != pixels || Owned(loaded) != owned) throw new Exception("Owned payload requires open archive.");
                if (reads.Values.Any(n => n != 1) || loaded.ExternalReads != reads.Count || loaded.SourceBytes != loaded.Sources.Values.Sum(b => (long)b.Bytes)) throw new Exception("Router accounting changed.");
                if (!SceneObjectLifecycleCodec.EncodeCreate(loaded.Create).SequenceEqual(SceneObjectLifecycleCodec.EncodeCreate(message))) throw new Exception("Raw create changed.");
                if (loaded.Kind != options.Recipe || (loaded.Creature is null) == (loaded.Static is null)) throw new Exception("Route discriminator/payload mismatch.");
                if (!ReferenceEquals(loaded.Create, loaded.Creature?.Create ?? loaded.Static!.Create) || !ReferenceEquals(loaded.Recipe, loaded.Creature?.Recipe ?? loaded.Static!.Recipe) || !ReferenceEquals(loaded.Sources, loaded.Creature?.Creature.Sources ?? loaded.Static!.Sources)) throw new Exception("Common metadata diverged from payload.");
                Emit("router_route", new { kind = loaded.Kind.ToString(), wire = Convert.ToHexString(SceneObjectLifecycleCodec.EncodeCreate(loaded.Create)), loaded.Recipe, loaded.ExternalReads, loaded.SourceBytes,
                    sources = loaded.Sources.Values.OrderBy(v => v.Path).ToArray(), counts = reads, workerThread = worker, callerThread,
                    albedo = loaded.Creature?.Creature.MaterialBundle!.AlbedoFactors is not null, frame = loaded.Static is null ? (int?)null : 0,
                    pixels, textures = Images(loaded).Select(p => new { path = p.Key, mips = p.Value.Mips.Select(m => Hash(m.Rgba.Span)).ToArray() }).ToArray(), archiveDisposed = true, ownedPayload = true, exactlyOnePayload = true });
                return loaded;
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).GetAwaiter().GetResult();
            results.Add(result);
            if (result.ExternalReads != (options is SceneCreatureResourceOptions ? 39 : 27) || result.SourceBytes != (options is SceneCreatureResourceOptions ? 4686371 : 1401794)) throw new Exception("Original recipe totals differ.");
            Reject<NotSupportedException>(() => ((IDictionary<string, AssetSourceIdentity>)result.Sources).Clear());
            using var config = File.OpenText(args[1]); using var overlay = ConfiguredTreOverlay.Open(args[0], config);
            SceneObjectResources? rejected = null;
            foreach (TreLookupKind missing in new[] { TreLookupKind.Missing, TreLookupKind.Masked })
                Reject<InvalidDataException>(() => rejected = SceneObjectResourceRouter.Load(message, catalog, options, p => p == result.Recipe.ObjectPath ? new(missing) : overlay.Lookup(p)));
            foreach (bool appearance in new[] { false, true })
            {
                var bad = appearance ? result.Recipe with { AppearanceSha256 = new string('0', 64) } : result.Recipe with { ObjectSha256 = new string('0', 64) };
                Reject<InvalidDataException>(() => rejected = SceneObjectResourceRouter.Load(message, new ObjectTemplateCatalog(new[] { bad }), options, overlay.Lookup));
            }
            using (var canceled = new CancellationTokenSource())
            {
                int calls = 0; Reject<OperationCanceledException>(() => rejected = SceneObjectResourceRouter.Load(message, catalog, options, p => { calls++; var b = overlay.Lookup(p); canceled.Cancel(); return b; }, canceled.Token));
                if (calls != 1) throw new Exception("Cancellation read beyond first source."); cancellationCalls += calls;
            }
            Reject<IOException>(() => rejected = SceneObjectResourceRouter.Load(message, catalog, options, _ => throw new IOException("Synthetic resolver failure")));
            SceneObjectResourceOptions Exact(int count, long bytes) => options is SceneCreatureResourceOptions ? new SceneCreatureResourceOptions(c.Idle, c.Movement, true, count, bytes) : new SceneStaticResourceOptions(s.MaterialPolicy, s.SwitchTextureFrame, new(MaximumResources: count, MaximumSourceBytes: bytes));
            foreach (var bounded in new[] { Exact(result.ExternalReads - 1, result.SourceBytes), Exact(result.ExternalReads, result.SourceBytes - 1) })
                Reject<AppearanceGraphBudgetException>(() => rejected = SceneObjectResourceRouter.Load(message, catalog, bounded, overlay.Lookup));
            var exact = SceneObjectResourceRouter.Load(message, catalog, Exact(result.ExternalReads, result.SourceBytes), overlay.Lookup);
            if (exact.ExternalReads != result.ExternalReads || exact.SourceBytes != result.SourceBytes || rejected is not null) throw new Exception("Budget/rejection published partial result.");
            if (options is SceneStaticResourceOptions)
            {
                Reject<InvalidDataException>(() => SceneObjectResourceRouter.Load(message, catalog, new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete, null), overlay.Lookup));
                Reject<InvalidDataException>(() => SceneObjectResourceRouter.Load(message, catalog, new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete, frame with { ShaderSha256 = new string('0', 64) }), overlay.Lookup));
                Reject<InvalidDataException>(() => SceneObjectResourceRouter.Load(message, catalog, new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete, frame with { FrameIndex = 8 }), overlay.Lookup));
                Reject<AppearanceGraphBudgetException>(() => SceneObjectResourceRouter.Load(message, catalog, new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete, frame, maximumDecodedBytes: 4), overlay.Lookup));
            }
        }
        Emit("router_validation", new { rejects, preflightReads, routes = results.Count, borrowedChecks, cancellationCalls, optionSnapshot = true, offThread = true, rawCreate = true, noPublication = true, gpu = false });
        Console.WriteLine("PASS R13-ASSETS-01: two explicit owned resource routes, raw metadata, source accounting, worker ownership and bounded failures.");
    }
}
