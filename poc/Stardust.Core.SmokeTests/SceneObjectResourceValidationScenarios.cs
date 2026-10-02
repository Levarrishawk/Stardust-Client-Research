using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;

internal static class SceneObjectResourceValidationScenarios
{
    private static string Hash(ReadOnlySpan<byte> b) => Convert.ToHexString(SHA256.HashData(b));
    private static void Emit(string tag, object value) => Console.WriteLine(tag + "=" + JsonSerializer.Serialize(value));
    // Test-only use of accepted internal typed constructors. No production factory or mutation hook is added.
    private static SceneObjectResources Wrap<T>(T payload) => (SceneObjectResources)typeof(SceneObjectResources)
        .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(T) }, null)!.Invoke(new object?[] { payload });
    internal static void Run(string[] args)
    {
        var catalog = ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(args[2])); using var bankStream = File.OpenRead(args[3]); var bank = SkeletalAnimationBankProfile.Load(bankStream);
        if (Hash(File.ReadAllBytes(args[1])) != RetainedSkeletalDetailBundleReader.ConfigurationPin) throw new InvalidDataException("Configuration pin mismatch.");
        var idle = new LogicalAnimationClipSelection("appearance/lat/all_m.lat", "emt_standing_idle_ambient_1_ag", "798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        var creatureOptions = new SceneCreatureResourceOptions(idle, bank, true);
        var frame = new SwitchTextureFrameSelection("shader/anim_mission_terminal.sht", "B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62", 0);
        var staticOptions = new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete, frame);
        SceneObjectResources? last = null; int failures = 0, reads = 0, successes = 0; var cases = new List<object>();
        void Bad(string name, SceneObjectResources result, SceneCreateObjectByCrcMessage create, SceneObjectResourceOptions options, ObjectTemplateCatalog? recipes = null)
        {
            try { SceneObjectResourceValidation.Validate(result, create, recipes ?? catalog, options); }
            catch (InvalidDataException) { failures++; cases.Add(new { name, rejected = true }); return; }
            throw new Exception("Bad completion accepted: " + name);
        }
        foreach (var options in new SceneObjectResourceOptions[] { creatureOptions, staticOptions })
        {
            uint crc = options is SceneCreatureResourceOptions ? 2937962913u : 2907007746u;
            var create = SceneObjectLifecycleCodec.DecodeCreate(SceneObjectLifecycleCodec.EncodeCreate(new(0xFEDCBA9876543210, -0f, .25f, -.5f, 2, 123.5f, -45.25f, float.Epsilon, crc, 255)));
            SceneObjectResources result; int routeReads = 0; var raw = new List<byte[]>();
            using (var config = File.OpenText(args[1])) using (var overlay = ConfiguredTreOverlay.Open(args[0], config))
            {
                result = SceneObjectResourceRouter.Load(create, catalog, options, p => { reads++; routeReads++; var r = overlay.Lookup(p); if (r.Payload is not null) raw.Add(r.Payload); return r; });
                foreach (var b in raw) Array.Clear(b);
            }
            last = result;
            string before = Hash(JsonSerializer.SerializeToUtf8Bytes(result)); int beforeReads = reads;
            SceneObjectResourceValidation.Validate(result, create, catalog, options); successes++;
            Task.Factory.StartNew(() => SceneObjectResourceValidation.Validate(result, create, catalog, options), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).GetAwaiter().GetResult(); successes++;
            foreach (var wrong in new[] { create with { ObjectId = 1 }, create with { QuaternionX = 0f }, create with { QuaternionY = 0f }, create with { QuaternionZ = 0f }, create with { QuaternionW = 1f }, create with { PositionX = 0f }, create with { PositionZ = 0f }, create with { PositionY = 0f }, create with { TemplateCrc = 0 }, create with { RawHyperspaceFlag = 0 }, create with { PositionY = float.NaN }, create with { QuaternionW = float.PositiveInfinity } })
                Bad("raw-create-" + failures, result, wrong, options);
            Bad("swapped-options", result, create, options is SceneCreatureResourceOptions ? staticOptions : creatureOptions);
            Bad("empty-catalog", result, create, options, new ObjectTemplateCatalog(Array.Empty<ObjectTemplateCatalogEntry>()));
            foreach (var entry in new[] { result.Recipe with { ObjectSha256 = new string('0', 64) }, result.Recipe with { AppearanceSha256 = new string('0', 64) }, result.Recipe with { Recipe = result.Kind == SceneObjectResourceRecipe.CreatureDetail0 ? SceneObjectResourceRecipe.StaticAppearance : SceneObjectResourceRecipe.CreatureDetail0 } })
                Bad("changed-catalog-" + failures, result, create, options, new ObjectTemplateCatalog(new[] { entry }));
            var sources = result.Sources.ToDictionary(p => p.Key, p => p.Value);
            foreach (string variant in new[] { "missing", "bytes", "hash", "path", "null", "extra" })
            {
                var broken = sources.ToDictionary(p => p.Key, p => p.Value); string root = result.Recipe.ObjectPath;
                switch (variant)
                {
                    case "missing": broken.Remove(root); break;
                    case "bytes": broken[root] = broken[root] with { Bytes = 0 }; break;
                    case "hash": broken[root] = broken[root] with { Sha256 = "bad" }; break;
                    case "path": broken[root] = broken[root] with { Path = "object/wrong.iff" }; break;
                    case "null": broken[root] = null!; break;
                    case "extra": broken.Add("object/synthetic/extra.iff", new("object/synthetic/extra.iff", 0, 1, new string('0', 64))); break;
                }
                var faulty = result.Creature is { } c ? Wrap(c with { Creature = c.Creature with { Sources = broken } }) : Wrap(result.Static! with { Sources = broken });
                Bad("source-" + variant, faulty, create, options);
            }
            if (result.Creature is { } creature)
            {
                var c = creature.Creature; var materials = c.MaterialBundle!;
                foreach (var altered in new[] { creature with { ExternalReads = 0 }, creature with { SourceBytes = 0 }, creature with { Recipe = creature.Recipe with { Recipe = SceneObjectResourceRecipe.StaticAppearance } },
                    creature with { Creature = c with { Profile = c.Profile with { AppearancePath = "appearance/wrong.sat" } } },
                    creature with { Creature = c with { Geometries = null } }, creature with { Creature = c with { PreparedAnimation = null } },
                    creature with { Creature = c with { AdditionalAnimations = new Dictionary<string, PreparedSkeletalAnimation>() } },
                    creature with { Creature = c with { MaterialBundle = materials with { AlbedoFactors = null } } },
                    creature with { Creature = c with { MaterialBundle = materials with { Textures = new Dictionary<string, DdsImage>() } } },
                    creature with { Creature = c with { CustomizationValues = new Dictionary<ShaderCustomizationKey, int>() } } })
                    Bad("creature-fault-" + failures, Wrap(altered), create, options);
                var factors = materials.AlbedoFactors!.Materials.ToDictionary(p => p.Key, p => p.Value); string factorKey = factors.Keys.First(); factors[factorKey] = factors[factorKey] with { FirstArgb = 0 };
                Bad("creature-albedo-color", Wrap(creature with { Creature = c with { MaterialBundle = materials with { AlbedoFactors = materials.AlbedoFactors with { Materials = factors } } } }), create, options);
                Bad("wrong-idle", result, create, new SceneCreatureResourceOptions(idle with { LogicalName = "different" }, bank, true));
                Bad("wrong-walk-route", result, create, new SceneCreatureResourceOptions(idle, bank.ToDictionary(p => p.Key, p => p.Key == "walk" ? p.Value with { ExplicitChildRoute = new[] { 4095 } } : p.Value), true));
                Bad("wrong-walk-pin", result, create, new SceneCreatureResourceOptions(idle, bank.ToDictionary(p => p.Key, p => p.Key == "walk" ? p.Value with { ClipSha256 = new string('0', 64) } : p.Value), true));
                Bad("albedo-option", result, create, new SceneCreatureResourceOptions(idle, bank, false));
                Bad("source-count-limit", result, create, new SceneCreatureResourceOptions(idle, bank, true, 38));
                Bad("source-byte-limit", result, create, new SceneCreatureResourceOptions(idle, bank, true, 39, result.SourceBytes - 1));
                SceneObjectResourceValidation.Validate(result, create, catalog, new SceneCreatureResourceOptions(idle, bank, true, 39, result.SourceBytes)); successes++;
                var images = materials.Textures.ToDictionary(p => p.Key, p => p.Value); string path = images.Keys.First(); var image = images[path];
                images[path] = image with { Mips = new[] { image.Mips[0] with { Rgba = new byte[4] } } };
                Bad("creature-mip-bytes", Wrap(creature with { Creature = c with { MaterialBundle = materials with { Textures = images } } }), create, options);
            }
            else
            {
                var s = result.Static!; var graph = s.Appearance;
                foreach (var altered in new[] { s with { ExternalReads = 0 }, s with { SourceBytes = 0 }, s with { MaterialPolicy = SceneStaticMaterialPolicy.AllowNeutral },
                    s with { Profile = s.Profile with { AppearancePath = "appearance/wrong.apt" } },
                    s with { Appearance = graph with { Meshes = new Dictionary<string, TexturedStaticMesh>() } },
                    s with { Appearance = graph with { Diagnostics = new[] { "synthetic-fault" } } },
                    s with { Appearance = graph with { Geometry = graph.Geometry with { Root = graph.Geometry.Root with { Children = Array.Empty<AppearanceGeometryChild>() } } } } })
                    Bad("static-fault-" + failures, Wrap(altered), create, options);
                Bad("wrong-frame", result, create, new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete, frame with { FrameIndex = 1 }));
                Bad("wrong-frame-pin", result, create, new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete, frame with { ShaderSha256 = new string('0', 64) }));
                Bad("absent-frame-option", result, create, new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete, null));
                Bad("wrong-material-policy", result, create, new SceneStaticResourceOptions(SceneStaticMaterialPolicy.AllowNeutral, frame));
                foreach (var limits in new[] { new AppearanceGraphLimits(MaximumResources: 26), new(MaximumSourceBytes: result.SourceBytes - 1), new(MaximumDepth: 1), new(MaximumEdges: 1) })
                    Bad("static-limit-" + failures, result, create, new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete, frame, limits));
                Bad("static-decoded-limit", result, create, new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete, frame, maximumDecodedBytes: 4));
                SceneObjectResourceValidation.Validate(result, create, catalog, new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete, frame, new(MaximumResources: 27, MaximumSourceBytes: result.SourceBytes))); successes++;
                var lowerFrame = frame with { ShaderSha256 = frame.ShaderSha256.ToLowerInvariant() };
                var lowerMeshes = graph.Meshes.ToDictionary(p => p.Key, p => p.Value with { Materials = p.Value.Materials.ToDictionary(m => m.Key, m => m.Value.SwitchTexture is { } lowerSwitch ? m.Value with { SwitchTexture = lowerSwitch with { Selection = lowerFrame } } : m.Value) });
                SceneObjectResourceValidation.Validate(Wrap(s with { Appearance = graph with { Meshes = lowerMeshes } }), create, catalog, new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete, lowerFrame)); successes++;
                var children = new List<AppearanceGeometryChild>(); var cycle = graph.Geometry.Root with { Children = children }; children.Add(new(0, AppearanceTransform.Identity, cycle));
                Bad("static-cycle", Wrap(s with { Appearance = graph with { Geometry = graph.Geometry with { Root = cycle } } }), create, options);
                var badEdges = graph.Geometry.Root.Children.Select((edge, i) => i == 0 ? edge with { LocalTransform = AppearanceTransform.Identity with { Translation = new(float.NaN, 0, 0) } } : edge).ToArray();
                Bad("static-nonfinite-edge", Wrap(s with { Appearance = graph with { Geometry = graph.Geometry with { Root = graph.Geometry.Root with { Children = badEdges } } } }), create, options);
                var switchedPair = graph.Meshes.First(p => p.Value.Materials.Values.Any(m => m.SwitchTexture is not null)); var switchedMaterial = switchedPair.Value.Materials.First(p => p.Value.SwitchTexture is not null);
                var wrongMaterials = switchedPair.Value.Materials.ToDictionary(p => p.Key, p => p.Value); var sw = switchedMaterial.Value.SwitchTexture!;
                wrongMaterials[switchedMaterial.Key] = switchedMaterial.Value with { SwitchTexture = sw with { Selection = sw.Selection with { FrameIndex = 1 } } };
                var wrongMeshes = graph.Meshes.ToDictionary(p => p.Key, p => p.Value); wrongMeshes[switchedPair.Key] = switchedPair.Value with { Materials = wrongMaterials };
                Bad("static-corrupt-selected-frame", Wrap(s with { Appearance = graph with { Meshes = wrongMeshes } }), create, options);
                var wrongSources = switchedPair.Value.Sources.ToDictionary(p => p.Key, p => p.Value); string sourceKey = wrongSources.Keys.First(); wrongSources[sourceKey] = wrongSources[sourceKey] with { Sha256 = new string('0', 64) };
                wrongMeshes[switchedPair.Key] = switchedPair.Value with { Sources = wrongSources };
                Bad("static-nested-source-hash", Wrap(s with { Appearance = graph with { Meshes = wrongMeshes } }), create, options);
                var meshMaps = graph.Meshes.ToDictionary(p => p.Key, p => p.Value); string key = meshMaps.Keys.First(); var mesh = meshMaps[key];
                meshMaps[key] = mesh with { Materials = new Dictionary<string, StaticMeshMaterial>() };
                Bad("static-missing-materials", Wrap(s with { Appearance = graph with { Meshes = meshMaps } }), create, options);
            }
            if (reads != beforeReads || Hash(JsonSerializer.SerializeToUtf8Bytes(result)) != before) throw new Exception("Validation did I/O or mutated the valid result.");
            var ownedTextures = result.Creature?.Creature.MaterialBundle!.Textures ?? result.Static!.Appearance.Meshes.Values.SelectMany(m => m.Textures).GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.First().Value);
            Emit("validation_route", new { kind = result.Kind.ToString(), wire = Convert.ToHexString(SceneObjectLifecycleCodec.EncodeCreate(create)), result.Recipe, result.ExternalReads, result.SourceBytes,
                sources = result.Sources.Values, textures = ownedTextures.Select(p => new { path = p.Key, mips = p.Value.Mips.Select(m => Hash(m.Rgba.Span)).ToArray() }).ToArray(), noReads = true, archiveClosed = true, unchanged = true, rawBuffersCleared = raw.Count });
            if (routeReads != result.ExternalReads) throw new Exception("Original route read count differs.");
        }
        var final = last!;
        foreach (var named in new (string Name, Action Call)[] {
            ("null-result", () => SceneObjectResourceValidation.Validate(null!, final.Create, catalog, staticOptions)),
            ("null-create", () => SceneObjectResourceValidation.Validate(final, null!, catalog, staticOptions)),
            ("null-catalog", () => SceneObjectResourceValidation.Validate(final, final.Create, null!, staticOptions)),
            ("null-options", () => SceneObjectResourceValidation.Validate(final, final.Create, catalog, null!)) })
        {
            try { named.Call(); } catch (ArgumentNullException) { failures++; cases.Add(new { name = named.Name, rejected = true }); continue; }
            throw new Exception("Null caller argument accepted.");
        }
        Emit("validation_cases", cases); Emit("validation_summary", new { failures, successes, reads, noSourceIO = true, archiveClosed = true, nativeThreadIndependent = true, gpu = false, registry = false });
        Console.WriteLine("PASS R14-ASSETS-01: no-I/O two-recipe completion validation, raw identity, source/option/descriptor gates and unchanged owned results.");
    }
}
