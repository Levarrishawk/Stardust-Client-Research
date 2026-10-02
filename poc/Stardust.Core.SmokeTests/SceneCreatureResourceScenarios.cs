using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;

internal static class SceneCreatureResourceScenarios
{
    internal static void Run(string[] args)
    {
        if (args.Length != 3) throw new ArgumentException("Explicit original root, pinned configuration and animation-bank profile required.");
        int rejections = 0;
        void Reject<T>(Action action) where T : Exception
        { try { action(); } catch (T) { rejections++; return; } throw new Exception("Expected " + typeof(T).Name); }
        const string path = RetainedSkeletalDetailBundleReader.ObjectPath;
        const uint crc = 2937962913;
        var entry = new ObjectTemplateCatalogEntry(crc, path, RetainedSkeletalDetailBundleReader.ObjectPin, RetainedSkeletalDetailBundleReader.AppearancePin);
        var array = new[] { entry with { ObjectPath = "./OBJECT\\CREATURE//PLAYER/SHARED_HUMAN_MALE.IFF", ObjectSha256 = entry.ObjectSha256.ToLowerInvariant() } };
        var catalog = new ObjectTemplateCatalog(array);
        array[0] = entry with { ObjectSha256 = new string('0', 64) };
        if (catalog.Require(crc) != entry) throw new Exception("Catalog snapshot or normalization changed.");
        Reject<NotSupportedException>(() => ((IDictionary<uint, ObjectTemplateCatalogEntry>)catalog.Entries).Clear());
        Reject<InvalidDataException>(() => catalog.Require(0));
        Reject<ArgumentException>(() => new ObjectTemplateCatalog(new[] { entry, entry }));
        Reject<ArgumentException>(() => new ObjectTemplateCatalog(new[] { entry, entry with { AppearanceSha256 = new string('0', 64) } }));
        foreach (string bad in new[] { "appearance/a.iff", "object/.iff", "object/a/.iff", "object/a.sat", "object/a/../b.iff", "", "object/é.iff" })
            Reject<ArgumentException>(() => new ObjectTemplateCatalog(new[] { entry with { ObjectPath = bad, TemplateCrc = bad.Length > 0 && !bad.Contains('é') ? LegacyAssetPath.Checksum(bad) : crc } }));
        foreach (string bad in new[] { "", "bad", new string('Z', 64) })
        {
            Reject<ArgumentException>(() => new ObjectTemplateCatalog(new[] { entry with { ObjectSha256 = bad } }));
            Reject<ArgumentException>(() => new ObjectTemplateCatalog(new[] { entry with { AppearanceSha256 = bad } }));
        }
        Reject<ArgumentException>(() => new ObjectTemplateCatalog(new[] { entry with { TemplateCrc = 0 } }));
        Reject<ArgumentException>(() => new ObjectTemplateCatalog(new[] { entry with { Recipe = (SceneObjectResourceRecipe)255 } }));
        Reject<ArgumentOutOfRangeException>(() => new ObjectTemplateCatalog(new[] { entry }, maximumEntries: 0));
        Reject<ArgumentOutOfRangeException>(() => new ObjectTemplateCatalog(new[] { entry }, maximumEntries: 4097));
        Reject<ArgumentOutOfRangeException>(() => new ObjectTemplateCatalog(new[] { entry }, maximumPathCharacters: 0));
        Reject<ArgumentException>(() => new ObjectTemplateCatalog(new[] { entry }, maximumPathCharacters: path.Length - 1));
        var synthetic = entry with { ObjectPath = "object/synthetic/fixture.iff", TemplateCrc = LegacyAssetPath.Checksum("object/synthetic/fixture.iff") };
        Reject<ArgumentException>(() => new ObjectTemplateCatalog(new[] { entry, synthetic }, maximumEntries: 1));
        const string collisionA = "object/synthetic/pppppppppppppppppppp.iff", collisionB = "object/synthetic/qqqpprrpqqprrssspppp.iff";
        if (LegacyAssetPath.Checksum(collisionA) != 0xBFCB749D || LegacyAssetPath.Checksum(collisionB) != 0xBFCB749D) throw new Exception("Fixed CRC collision fixture changed.");
        Reject<ArgumentException>(() => new ObjectTemplateCatalog(new[] { entry with { ObjectPath = collisionA, TemplateCrc = 0xBFCB749D }, entry with { ObjectPath = collisionB, TemplateCrc = 0xBFCB749D } }));
        Console.WriteLine("scene_catalog=" + JsonSerializer.Serialize(new { kind = "modern supplied manifest", original = entry, synthetic, collisionA, collisionB, collisionCrc = 0xBFCB749Du }));

        using var bankFile = File.OpenRead(args[2]); var loadedBank = SkeletalAnimationBankProfile.Load(bankFile);
        var bank = loadedBank.ToDictionary(p => p.Key, p => p.Value with { ExplicitChildRoute = p.Value.ExplicitChildRoute!.ToArray() }, StringComparer.Ordinal);
        var idle = new LogicalAnimationClipSelection("appearance/lat/all_m.lat", "emt_standing_idle_ambient_1_ag", "798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        var message = SceneObjectLifecycleCodec.DecodeCreate(SceneObjectLifecycleCodec.EncodeCreate(new(0xFEDCBA9876543210, -0f, .25f, -.5f, 2, 123.5f, -45.25f, float.Epsilon, crc, 255)));
        int preflightReads = 0;
        TreLookupResult Never(string _) { preflightReads++; throw new Exception("Invalid recipe reached resolver."); }
        SceneCreatureResources Preflight(SceneCreateObjectByCrcMessage? m = null, ObjectTemplateCatalog? c = null, LogicalAnimationClipSelection? i = null,
            IReadOnlyDictionary<string, LogicalAnimationClipSelection>? b = null, CancellationToken token = default, int count = 64, long bytes = 16777216)
            => SceneCreatureResourceReader.Load(m ?? message, c ?? catalog, i ?? idle, b ?? bank, Never, token, count, bytes);
        Reject<InvalidDataException>(() => Preflight(message with { TemplateCrc = 0 }));
        Reject<InvalidDataException>(() => Preflight(c: new ObjectTemplateCatalog(Array.Empty<ObjectTemplateCatalogEntry>())));
        Reject<InvalidDataException>(() => Preflight(message with { QuaternionW = float.NaN }));
        Reject<ArgumentOutOfRangeException>(() => Preflight(count: 0));
        Reject<ArgumentOutOfRangeException>(() => Preflight(count: 4097));
        Reject<ArgumentOutOfRangeException>(() => Preflight(bytes: 0));
        Reject<ArgumentOutOfRangeException>(() => Preflight(bytes: 134217729));
        Reject<ArgumentException>(() => Preflight(i: idle with { ClipSha256 = "bad" }));
        Reject<ArgumentException>(() => Preflight(i: idle with { TablePath = "object/x.iff" }));
        Reject<ArgumentException>(() => Preflight(i: idle with { ExplicitChildRoute = new[] { -1 } }));
        Reject<ArgumentException>(() => Preflight(i: idle with { ExplicitChildRoute = Enumerable.Repeat(0, 9).ToArray() }));
        Reject<ArgumentException>(() => Preflight(b: new Dictionary<string, LogicalAnimationClipSelection>()));
        var wrongBank = bank.ToDictionary(p => p.Key, p => p.Value);
        wrongBank["run"] = wrongBank["run"] with { ClipSha256 = "bad" };
        Reject<ArgumentException>(() => Preflight(b: wrongBank));
        using (var canceled = new CancellationTokenSource()) { canceled.Cancel(); Reject<OperationCanceledException>(() => Preflight(token: canceled.Token)); }
        if (preflightReads != 0) throw new Exception("Invalid/missing recipe or canceled request performed lookup.");

        if (Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[1]))) != RetainedSkeletalDetailBundleReader.ConfigurationPin) throw new InvalidDataException("Configuration pin mismatch.");
        using var config = File.OpenText(args[1]); using var overlay = ConfiguredTreOverlay.Open(args[0], config);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal); var borrowed = new List<byte[]>();
        TreLookupResult Read(string p)
        {
            counts[p] = counts.GetValueOrDefault(p) + 1; var source = overlay.Lookup(p);
            if (source.Payload is not null) borrowed.Add(source.Payload);
            // Mutate caller-owned configuration during the first read: snapshot must already exist.
            if (p == path) { ((int[])bank["walk"].ExplicitChildRoute!)[1] = 99; bank.Clear(); }
            return source;
        }
        var result = SceneCreatureResourceReader.Load(message, catalog, idle, bank, Read);
        var creature = result.Creature;
        if (result.Create != message || ReferenceEquals(result.Create, message) || result.Recipe != entry || counts.Values.Any(n => n != 1) ||
            result.ExternalReads != counts.Count || !counts.Keys.ToHashSet().SetEquals(creature.Sources.Keys)) throw new Exception("Create identity/read-once boundary changed.");
        if (creature.Geometries!.Count != 2 || creature.Skeletons!.Count != 2 || creature.PreparedAnimation is null || creature.AdditionalAnimations!.Count != 2) throw new Exception("Owned detail0 creature incomplete.");
        foreach (var material in creature.MaterialBundle!.Materials.Values)
            if (!material.References.Textures.Any(t => t.Tag == 0x4D41494E && t.AssetPath is not null && creature.MaterialBundle.Textures.ContainsKey(t.AssetPath))) throw new Exception("Authored MAIN texture absent.");
        var binding = SkeletalJointBindingBuilder.Create(creature.Appearance, creature.Skeletons, creature.Appearance.Skeletons.ToDictionary(r => r.Path, _ => 0));
        var prepared = creature.AdditionalAnimations.ToDictionary(p => p.Key, p => p.Value); prepared.Add("idle", creature.PreparedAnimation);
        var poses = prepared.ToDictionary(p => p.Key, p => p.Value.CreateLocalPose(binding, .5).ToArray());
        var geometryHashes = creature.Geometries.ToDictionary(p => p.Key, p => SkeletalGeometryScenarios.CanonicalHash(p.Value));
        var pixelHashes = creature.MaterialBundle.Textures.ToDictionary(p => p.Key, p => p.Value.Mips.Select(m => Convert.ToHexString(SHA256.HashData(m.Rgba.Span))).ToArray());
        foreach (var payload in borrowed) Array.Clear(payload);
        foreach (var pair in prepared) if (!poses[pair.Key].SequenceEqual(pair.Value.CreateLocalPose(binding, .5))) throw new Exception("Borrowed clip mutation changed owned pose.");
        foreach (var pair in creature.Geometries) if (geometryHashes[pair.Key] != SkeletalGeometryScenarios.CanonicalHash(pair.Value)) throw new Exception("Borrowed geometry mutation changed owned mesh.");
        foreach (var pair in creature.MaterialBundle.Textures) if (!pixelHashes[pair.Key].SequenceEqual(pair.Value.Mips.Select(m => Convert.ToHexString(SHA256.HashData(m.Rgba.Span))))) throw new Exception("Borrowed texture mutation changed owned pixels.");
        var rawBits = new[] { result.Create.QuaternionX, result.Create.QuaternionY, result.Create.QuaternionZ, result.Create.QuaternionW, result.Create.PositionX, result.Create.PositionZ, result.Create.PositionY }.Select(BitConverter.SingleToUInt32Bits).ToArray();
        Console.WriteLine("scene_create=" + JsonSerializer.Serialize(new { wire = Convert.ToHexString(SceneObjectLifecycleCodec.EncodeCreate(result.Create)), result.Create, rawBits, result.ExternalReads, result.SourceBytes, counts, root = creature.Profile.RootPath, appearance = creature.Profile.AppearancePath, detail = 0, joints = binding.Joints.Count, ownership = "borrowed bytes/config mutated; retained poses/meshes/pixels unchanged" }));
        foreach (var source in creature.Sources.Values) Console.WriteLine("scene_source=" + JsonSerializer.Serialize(source));
        foreach (var pair in prepared) Console.WriteLine("scene_clip=" + JsonSerializer.Serialize(new { key = pair.Key, pair.Value.ClipPath, pair.Value.ClipSha256, pair.Value.SkeletonPath, pair.Value.FrameRate, pair.Value.EndFrame }));
        Console.WriteLine("scene_idle=" + JsonSerializer.Serialize(creature.SelectedAnimation!.Selection));
        foreach (var pair in loadedBank)
        {
            var clip = prepared[pair.Key]; var motion = clip.Locomotion!;
            Console.WriteLine("scene_bank=" + JsonSerializer.Serialize(new { key = pair.Key, table = pair.Value.TablePath, name = pair.Value.LogicalName,
                route = pair.Value.ExplicitChildRoute, path = clip.ClipPath, sha256 = clip.ClipSha256, clip.FrameRate, clip.EndFrame,
                keys = motion.Positions.Count, poses = clip.EndFrame * 4 + 1, motion.AuthoredSpeed, clip.UnusedAnimationTransforms,
                clip.MissingSkeletonTransforms, joints = binding.Joints.Count, root_motion_applied = false, event_dispatch = false }));
        }
        foreach (var pair in creature.MaterialBundle.Materials) Console.WriteLine("scene_main=" + JsonSerializer.Serialize(new { shader = pair.Key, bindings = pair.Value.References.Textures.Where(t => t.Tag == 0x4D41494E) }));

        SceneCreatureResources Load(Func<string, TreLookupResult> resolver, ObjectTemplateCatalog? c = null, LogicalAnimationClipSelection? i = null,
            IReadOnlyDictionary<string, LogicalAnimationClipSelection>? b = null, CancellationToken token = default, int count = 64, long bytes = 16777216)
            => SceneCreatureResourceReader.Load(message, c ?? catalog, i ?? idle, b ?? loadedBank, resolver, token, count, bytes);
        Reject<InvalidDataException>(() => Load(overlay.Lookup, new ObjectTemplateCatalog(new[] { entry with { ObjectSha256 = new string('0', 64) } })));
        Reject<InvalidDataException>(() => Load(overlay.Lookup, new ObjectTemplateCatalog(new[] { entry with { AppearanceSha256 = new string('0', 64) } })));
        Reject<InvalidDataException>(() => Load(overlay.Lookup, i: idle with { ClipSha256 = new string('0', 64) }));
        foreach (string key in new[] { "walk", "run" })
        { var wrong = loadedBank.ToDictionary(p => p.Key, p => p.Value); wrong[key] = wrong[key] with { ClipSha256 = new string('0', 64) }; Reject<InvalidDataException>(() => Load(overlay.Lookup, b: wrong)); }
        var required = new[] { path, creature.Profile.AppearancePath, creature.Appearance.MeshGenerators[0], creature.Geometries.Keys.First(), creature.Appearance.Skeletons[0].Path,
            creature.PreparedAnimation.ClipPath, prepared["walk"].ClipPath, prepared["run"].ClipPath, creature.MaterialBundle.Materials.Keys.First(), creature.MaterialBundle.Textures.Keys.First() };
        foreach (string missing in required)
            foreach (var kind in new[] { TreLookupKind.Missing, TreLookupKind.Masked }) Reject<InvalidDataException>(() => Load(p => p == missing ? new(kind) : overlay.Lookup(p)));
        Reject<AppearanceGraphBudgetException>(() => Load(overlay.Lookup, count: 1));
        Reject<AppearanceGraphBudgetException>(() => Load(overlay.Lookup, bytes: 1));
        Reject<AppearanceGraphBudgetException>(() => Load(overlay.Lookup, count: result.ExternalReads - 1));
        Reject<AppearanceGraphBudgetException>(() => Load(overlay.Lookup, bytes: result.SourceBytes - 1));
        using (var cancel = new CancellationTokenSource())
        {
            int calls = 0;
            Reject<OperationCanceledException>(() => Load(p => { calls++; var r = overlay.Lookup(p); cancel.Cancel(); return r; }, token: cancel.Token));
            if (calls != 1) throw new Exception("Canceled resolver continued lookup.");
        }
        int missingReads = 0;
        Reject<InvalidDataException>(() => SceneCreatureResourceReader.Load(message with { TemplateCrc = synthetic.TemplateCrc }, new ObjectTemplateCatalog(new[] { synthetic }), idle, loadedBank, _ => { missingReads++; return new(TreLookupKind.Missing); }));
        if (missingReads != 1) throw new Exception("Known synthetic missing root attempted fallback.");
        foreach (string pinned in new[] { path, creature.Profile.AppearancePath, creature.PreparedAnimation.ClipPath, prepared["walk"].ClipPath, prepared["run"].ClipPath })
            Reject<InvalidDataException>(() => Load(p => { var r = overlay.Lookup(p); if (p == pinned) r.Payload![^1] ^= 1; return r; }));
        // A fresh successful load after all failed attempts confirms no static cache/partial result reuse.
        var fresh = Load(overlay.Lookup, count: result.ExternalReads, bytes: result.SourceBytes);
        if (fresh.SourceBytes != result.SourceBytes || fresh.ExternalReads != result.ExternalReads || fresh.Create != message) throw new Exception("Failed calls contaminated subsequent load.");
        Console.WriteLine("scene_validation=" + JsonSerializer.Serialize(new { rejections, preflightReads, missingReads, cancellationResolverCalls = 1, configSha256 = RetainedSkeletalDetailBundleReader.ConfigurationPin, sourceCloneChecks = borrowed.Count, meshOwnershipChecks = geometryHashes.Count, textureOwnershipChecks = pixelHashes.Count, poseOwnershipChecks = poses.Count }));
        Console.WriteLine("PASS R5-ASSETS-01: bounded modern catalog, original create/detail0 authored MAIN/idle/walk/run resources; no engine transform/root translation/GPU.");
    }
}
