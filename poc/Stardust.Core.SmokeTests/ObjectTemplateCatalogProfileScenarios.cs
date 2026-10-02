using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;

internal static class ObjectTemplateCatalogProfileScenarios
{
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    internal static void Run(string[] args)
    {
        if (args.Length != 5) throw new ArgumentException("Explicit root, configuration, catalog profile, animation bank and private canonical output required.");
        byte[] input = File.ReadAllBytes(args[2]); var catalog = ObjectTemplateCatalogProfile.Decode(input); byte[] canonical = ObjectTemplateCatalogProfile.Encode(catalog);
        if (catalog.Entries.Count != 2 || !canonical.SequenceEqual(ObjectTemplateCatalogProfile.Encode(ObjectTemplateCatalogProfile.Decode(canonical)))) throw new Exception("Profile round trip changed.");
        var human = catalog.Require(2937962913); var terminal = catalog.Require(2907007746);
        if (human.ObjectPath != RetainedSkeletalDetailBundleReader.ObjectPath || human.ObjectSha256 != RetainedSkeletalDetailBundleReader.ObjectPin || human.AppearanceSha256 != RetainedSkeletalDetailBundleReader.AppearancePin || human.Recipe != SceneObjectResourceRecipe.CreatureDetail0 || terminal.ObjectPath != "object/tangible/terminal/shared_terminal_mission.iff" || terminal.Recipe != SceneObjectResourceRecipe.StaticAppearance) throw new Exception("Public profile recipes changed.");
        int rejects = 0, accepted = 0, optionRejects = 0, encoderRejects = 0;
        void Case(string label, byte[] bytes, bool accept = false, ObjectTemplateCatalogProfileLimits? limits = null)
        {
            ObjectTemplateCatalog? result = null;
            try { result = ObjectTemplateCatalogProfile.Decode(bytes, limits); }
            catch (InvalidDataException) { if (accept) throw; rejects++; }
            if ((result is not null) != accept) throw new Exception("Unexpected profile outcome: " + label);
            if (accept) accepted++;
            Console.WriteLine("catalog_profile_case=" + JsonSerializer.Serialize(new { label, hex = Convert.ToHexString(bytes), accept, limits = limits ?? new(), canonical = result is null ? null : Convert.ToHexString(ObjectTemplateCatalogProfile.Encode(result)) }));
        }
        string Entry(ObjectTemplateCatalogEntry e, string? path = null, string? recipe = null)
            => JsonSerializer.Serialize(new { templateCrc = e.TemplateCrc, objectPath = path ?? e.ObjectPath, objectSha256 = e.ObjectSha256, appearanceSha256 = e.AppearanceSha256, recipe = recipe ?? (e.Recipe == SceneObjectResourceRecipe.CreatureDetail0 ? "creatureDetail0" : "staticAppearance") });
        string Profile(params string[] entries) => "{\"version\":1,\"entries\":[" + string.Join(',', entries) + "]}";
        string one = Profile(Entry(human));
        void JsonCase(string label, string json, bool accept = false, ObjectTemplateCatalogProfileLimits? limits = null) => Case(label, Encoding.UTF8.GetBytes(json), accept, limits);
        Case("public", input, true); Case("canonical", canonical, true);
        JsonCase("empty", Profile(), true); JsonCase("one", one, true);
        JsonCase("reversed", Profile(Entry(terminal), Entry(human)), true);
        JsonCase("alias", Profile(Entry(human with { ObjectSha256 = human.ObjectSha256.ToLowerInvariant(), AppearanceSha256 = human.AppearanceSha256.ToLowerInvariant() }, "/OBJECT\\CREATURE//PLAYER/SHARED_HUMAN_MALE.IFF"), Entry(terminal)), true);
        JsonCase("escaped-property", one.Replace("objectPath", "object\\u0050ath"), true);
        const string printablePath = "object/synthetic/a+<&'\".iff";
        JsonCase("printable-ascii-json", Profile(Entry(human with { ObjectPath = printablePath, TemplateCrc = LegacyAssetPath.Checksum(printablePath) })), true);
        JsonCase("trailing-path-separator", Profile(Entry(human, human.ObjectPath + "/")));
        foreach (string bad in new[] { "null", "[]", "0", "true", "\"text\"", "{}", "{\"version\":1}", "{\"entries\":[]}", "{\"version\":1,\"entries\":[],\"extra\":0}", "{\"Version\":1,\"entries\":[]}", "{\"version\":1,\"version\":1,\"entries\":[]}", "{\"version\":1,\"\\u0076ersion\":1,\"entries\":[]}", "{\"version\":1,\"entries\":[],\"entries\":[]}", "{\"version\":1,\"entries\":[],}", "{\"version\":1,/* comment */\"entries\":[]}", one + "{}" }) JsonCase("root-" + rejects, bad);
        foreach (string value in new[] { "0", "2", "-0", "-1", "1.0", "1e0", "4294967296", "\"1\"", "true", "null", "[]", "{}" }) JsonCase("version-" + value, one.Replace("\"version\":1", "\"version\":" + value));
        foreach (string value in new[] { "null", "{}", "true", "0", "\"x\"", "[null]", "[[]]", "[0]" }) JsonCase("entries-" + value, "{\"version\":1,\"entries\":" + value + "}");
        foreach (string property in new[] { "templateCrc", "objectPath", "objectSha256", "appearanceSha256", "recipe" })
        {
            using var doc = JsonDocument.Parse(Entry(human));
            var fields = doc.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
            fields.Remove(property); JsonCase("missing-" + property, Profile(JsonSerializer.Serialize(fields)));
            string entry = Entry(human); JsonCase("duplicate-" + property, Profile(entry[..^1] + ",\"" + property + "\":" + doc.RootElement.GetProperty(property).GetRawText() + "}"));
            fields = doc.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone()); fields[property] = JsonSerializer.SerializeToElement((object?)null);
            JsonCase("null-" + property, Profile(JsonSerializer.Serialize(fields)));
        }
        JsonCase("unknown-entry", Profile(Entry(human)[..^1] + ",\"extra\":0}"));
        foreach (string property in new[] { "objectPath", "objectSha256", "appearanceSha256", "recipe" })
            foreach (string value in new[] { "0", "true", "[]", "{}" })
            {
                using var doc = JsonDocument.Parse(Entry(human)); var fields = doc.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
                using var wrongType = JsonDocument.Parse(value); fields[property] = wrongType.RootElement.Clone();
                JsonCase("type-" + property + "-" + value, Profile(JsonSerializer.Serialize(fields)));
            }
        JsonCase("escaped-duplicate-entry", Profile(Entry(human)[..^1] + ",\"object\\u0050ath\":\"" + human.ObjectPath + "\"}"));
        foreach (string value in new[] { "-0", "-1", "2937962913.0", "2937962913e0", "4294967296", "\"2937962913\"", "true", "[]", "{}", "0" }) JsonCase("crc-" + value, one.Replace("2937962913", value));
        foreach (string recipe in new[] { "CreatureDetail0", "static", "", "0", "unknown" }) JsonCase("recipe-" + recipe, Profile(Entry(human, recipe: recipe)));
        JsonCase("numeric-recipe", one.Replace("\"creatureDetail0\"", "0"));
        foreach (string path in new[] { "./" + human.ObjectPath, "../" + human.ObjectPath, "object/./x.iff", "object/a/../x.iff", "object\\..\\x.iff", "appearance/x.iff", "object/.iff", "object/x.sat", "", "object/é.iff", "object/x\0.iff", "object/" + new string('a', 1020) + ".iff" }) JsonCase("path-" + rejects, Profile(Entry(human, path)));
        JsonCase("escaped-dot", one.Replace("object/creature", "object/\\u002e/creature"));
        foreach (string property in new[] { "objectSha256", "appearanceSha256" })
            foreach (string pin in new[] { "", "bad", new string('G', 64), new string('A', 65) })
            {
                using var doc = JsonDocument.Parse(Entry(human)); var fields = doc.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
                fields[property] = JsonSerializer.SerializeToElement(pin); JsonCase("pin-" + property + "-" + rejects, Profile(JsonSerializer.Serialize(fields)));
            }
        JsonCase("duplicate-entry", Profile(Entry(human), Entry(human)));
        JsonCase("alias-duplicate", Profile(Entry(human), Entry(human, "/OBJECT\\CREATURE//PLAYER/SHARED_HUMAN_MALE.IFF")));
        JsonCase("conflicting-entry", Profile(Entry(human), Entry(human with { AppearanceSha256 = new string('0', 64) })));
        const string collisionA = "object/synthetic/pppppppppppppppppppp.iff", collisionB = "object/synthetic/qqqpprrpqqprrssspppp.iff";
        JsonCase("crc-collision", Profile(Entry(human with { TemplateCrc = 0xBFCB749D, ObjectPath = collisionA }), Entry(human with { TemplateCrc = 0xBFCB749D, ObjectPath = collisionB })));
        foreach (byte[] bad in new[] { new byte[] { 0xFF }, new byte[] { 0xC0, 0xAF }, new byte[] { 0xE2, 0x82 }, Encoding.UTF8.GetPreamble().Concat(input).ToArray(), Encoding.UTF8.GetBytes(one.Replace("creatureDetail0", "\uD800")) }) Case("utf8-" + rejects, bad);
        JsonCase("escaped-surrogate", one.Replace("creatureDetail0", "\\uD800"));
        foreach (int n in new[] { 0, 1, input.Length / 2, input.Length - 2 }) Case("truncated-" + n, input[..n]);
        int characters = catalog.Entries.Values.Sum(e => e.ObjectPath.Length);
        Case("exact-bytes", input, true, new(MaximumBytes: input.Length)); Case("over-bytes", input, limits: new(MaximumBytes: input.Length - 1));
        Case("exact-entries", input, true, new(MaximumEntries: 2)); Case("over-entries", input, limits: new(MaximumEntries: 1));
        Case("exact-paths", input, true, new(MaximumPathCharacters: characters)); Case("over-paths", input, limits: new(MaximumPathCharacters: characters - 1));
        Case("exact-depth", input, true, new(MaximumDepth: 3)); Case("over-depth", input, limits: new(MaximumDepth: 2));
        JsonCase("nested-depth", "{\"version\":1,\"entries\":[],\"extra\":[[[[[0]]]]]}");
        foreach (var bad in new[] { new ObjectTemplateCatalogProfileLimits(MaximumBytes: 0), new(MaximumBytes: 16777217), new(MaximumDepth: 0), new(MaximumDepth: 17), new(MaximumEntries: 0), new(MaximumEntries: 4097), new(MaximumPathCharacters: 0), new(MaximumPathCharacters: 4194305) })
        {
            try { _ = ObjectTemplateCatalogProfile.Decode(input, bad); throw new Exception("Bad decoder limits accepted."); } catch (ArgumentOutOfRangeException) { optionRejects++; }
            try { _ = ObjectTemplateCatalogProfile.Encode(catalog, bad); throw new Exception("Bad encoder limits accepted."); } catch (ArgumentOutOfRangeException) { optionRejects++; }
        }
        foreach (var bad in new[] { new ObjectTemplateCatalogProfileLimits(MaximumBytes: canonical.Length - 1), new(MaximumDepth: 2), new(MaximumEntries: 1), new(MaximumPathCharacters: characters - 1) })
        { try { _ = ObjectTemplateCatalogProfile.Encode(catalog, bad); throw new Exception("Encoder budget accepted."); } catch (InvalidDataException) { encoderRejects++; } }
        if (!canonical.SequenceEqual(ObjectTemplateCatalogProfile.Encode(catalog, new(MaximumBytes: canonical.Length, MaximumDepth: 3, MaximumEntries: 2, MaximumPathCharacters: characters)))) throw new Exception("Exact encoder limits changed output.");
        var reversed = new ObjectTemplateCatalog(catalog.Entries.Values.Reverse()); if (!canonical.SequenceEqual(ObjectTemplateCatalogProfile.Encode(reversed))) throw new Exception("Insertion order affects canonical output.");
        byte[] copied = input.ToArray(); var owned = ObjectTemplateCatalogProfile.Decode(copied); Array.Clear(copied);
        byte[] published = ObjectTemplateCatalogProfile.Encode(owned); Array.Clear(published);
        if (!canonical.SequenceEqual(ObjectTemplateCatalogProfile.Encode(owned))) throw new Exception("Profile borrows input/output bytes.");
        try { ((IDictionary<uint, ObjectTemplateCatalogEntry>)owned.Entries).Clear(); throw new Exception("Mutable profile catalog."); } catch (NotSupportedException) { }
        try { _ = owned.Require(0); throw new Exception("Unknown CRC has fallback."); } catch (InvalidDataException) { }

        // Reuse accepted readers once through the profile; their closed byte/geometry audits are not duplicated here.
        if (Hash(File.ReadAllBytes(args[1])) != RetainedSkeletalDetailBundleReader.ConfigurationPin) throw new InvalidDataException("Configuration pin mismatch.");
        using var configuration = File.OpenText(args[1]); using var overlay = ConfiguredTreOverlay.Open(args[0], configuration);
        using var bankFile = File.OpenRead(args[3]); var bank = SkeletalAnimationBankProfile.Load(bankFile);
        var idle = new LogicalAnimationClipSelection("appearance/lat/all_m.lat", "emt_standing_idle_ambient_1_ag", "798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        SceneCreateObjectByCrcMessage Create(uint crc) => SceneObjectLifecycleCodec.DecodeCreate(SceneObjectLifecycleCodec.EncodeCreate(new(0xFEDCBA9876543210, -0f, .25f, -.5f, 2, 123.5f, -45.25f, float.Epsilon, crc, 255)));
        var creatureCounts = new Dictionary<string, int>(); var staticCounts = new Dictionary<string, int>();
        TreLookupResult Read(string path, Dictionary<string, int> counts) { counts[path] = counts.GetValueOrDefault(path) + 1; return overlay.Lookup(path); }
        var creature = SceneCreatureResourceReader.Load(Create(human.TemplateCrc), owned, idle, bank, p => Read(p, creatureCounts));
        var stat = SceneStaticResourceReader.Load(Create(terminal.TemplateCrc), owned, p => Read(p, staticCounts), SceneStaticMaterialPolicy.RequireComplete,
            switchTextureFrame: new("shader/anim_mission_terminal.sht", "B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62", 0));
        if (creature.Recipe != human || stat.Recipe != terminal || creature.ExternalReads != 37 || creature.SourceBytes != 4683848 || stat.ExternalReads != 27 || stat.SourceBytes != 1401794 || stat.Appearance.Meshes.Count != 4 || creatureCounts.Values.Any(n => n != 1) || staticCounts.Values.Any(n => n != 1)) throw new Exception("Profile reader composition changed.");
        Console.WriteLine("catalog_profile_readers=" + JsonSerializer.Serialize(new { creature = new { creature.ExternalReads, creature.SourceBytes, recipe = creature.Recipe, counts = creatureCounts }, stat = new { stat.ExternalReads, stat.SourceBytes, recipe = stat.Recipe, counts = staticCounts, meshes = stat.Appearance.Meshes.Count }, gpu = false }));
        string destination = Path.GetFullPath(args[4]); string privateRoot = Path.GetFullPath("local-private") + Path.DirectorySeparatorChar;
        if (!destination.StartsWith(privateRoot, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Canonical evidence output must be private.");
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.WriteAllBytes(destination, canonical);
        Console.WriteLine("catalog_profile_validation=" + JsonSerializer.Serialize(new { rejects, accepted, optionRejects, encoderRejects, entries = catalog.Entries.Count, inputSha256 = Hash(input), canonicalSha256 = Hash(canonical), canonicalBytes = canonical.Length, pathCharacters = characters, owned = true, deterministic = true, configSha256 = RetainedSkeletalDetailBundleReader.ConfigurationPin }));
        Console.WriteLine("PASS R9-ASSETS-01: bounded caller JSON catalog profile, immutable canonical recipes and existing reader composition.");
    }
}
