using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class CustomizationIdDictionaryScenarios
{
    internal const string OriginalPin = "FD3A7C9FFB9E2AF4234C214F803E20CE9A2F06091F15F4E91578C6B4FBF6BCF2";
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static byte[] Chunk(string tag, byte[] body)
    {
        byte[] result = new byte[8 + body.Length]; Encoding.ASCII.GetBytes(tag).CopyTo(result, 0);
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(4), (uint)body.Length); body.CopyTo(result, 8); return result;
    }
    private static byte[] Form(string tag, params byte[][] children) => Chunk("FORM", Encoding.ASCII.GetBytes(tag).Concat(children.SelectMany(c => c)).ToArray());
    private static byte[] Table(byte[] data) => Form("CIDM", Form("0001", Chunk("DATA", data)));
    private static byte[] Records(params (ushort Id, string Name)[] entries)
    {
        using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
        foreach (var e in entries) { writer.Write(e.Id); writer.Write(Encoding.ASCII.GetBytes(e.Name)); writer.Write((byte)0); }
        return stream.ToArray();
    }
    private static object Row(CustomizationIdEntry entry)
    {
        var p = CustomizationIdDictionary.ProjectName(entry.FullName);
        return new { entry.Id, entry.FullName, Projection = p.Kind.ToString(), VariableName = p.Key?.VariableName, FactoryFlag = p.Key?.FactoryFlag };
    }
    internal static void Run(string[] args)
    {
        Check(args.Length == 2, "original archive root/config required");
        var source = Table(Records((1, "/private/MixedCase"), (2, "/shared_owner/index_color_skin"), (3, "private/alternate_shader_blade"), (4, "/other/name")));
        var owned = CustomizationIdDictionary.Load(source); Array.Clear(source);
        Check(owned.Entries.Count == 4 && owned.ById[1].FullName == "/private/MixedCase", "Input snapshot survives mutation");
        Check(owned.ByName.ContainsKey("/private/MixedCase") && !owned.ByName.ContainsKey("/private/mixedcase"), "Ordinal exact names");
        Check(CustomizationIdDictionary.ProjectName(owned.ById[1].FullName).Key == new BlueprintCustomizationKey("MixedCase", true), "Private projection");
        Check(CustomizationIdDictionary.ProjectName(owned.ById[2].FullName).Key == new BlueprintCustomizationKey("index_color_skin", false), "Shared-owner projection");
        foreach (var name in new[] { owned.ById[3].FullName, "/Private/name", "/private/", "/shared_owner/", "/other/name", "", "//private/name" })
            Check(CustomizationIdDictionary.ProjectName(name) is { Kind: CustomizationNameProjectionKind.UnsupportedNamespace, Key: null, FullName: var kept } && kept == name, "Unsupported name preserved");
        bool immutable = false; try { ((IList<CustomizationIdEntry>)owned.Entries).Add(new(5, "x")); } catch (NotSupportedException) { immutable = true; }
        Check(immutable, "Read-only entries");
        foreach (var map in new[] { (object)owned.ById, owned.ByName }) Check(map is not Dictionary<ushort, CustomizationIdEntry> && map is not Dictionary<string, CustomizationIdEntry>, "Maps encapsulated");
        var cases = new List<string>();
        void Reject(string name, Action action, Type? expected = null)
        {
            try { action(); } catch (Exception e) when (e.GetType() == (expected ?? typeof(InvalidDataException))) { cases.Add(name); return; }
            throw new Exception("Expected rejection: " + name);
        }
        var good = Table(Records((1, "/private/a"), (2, "/shared_owner/b")));
        for (int i = 0; i < good.Length; i++) { int length = i; Reject("truncation-" + i, () => CustomizationIdDictionary.Load(good.AsSpan(0, length))); }
        Reject("root", () => CustomizationIdDictionary.Load(Form("ACST", Form("0001", Chunk("DATA", Records((1, "a")))))));
        Reject("version", () => CustomizationIdDictionary.Load(Form("CIDM", Form("0000", Chunk("DATA", Records((1, "a")))))));
        Reject("missing-data", () => CustomizationIdDictionary.Load(Form("CIDM", Form("0001"))));
        Reject("chunk-tag", () => CustomizationIdDictionary.Load(Form("CIDM", Form("0001", Chunk("NAME", Records((1, "a")))))));
        Reject("extra-data", () => CustomizationIdDictionary.Load(Form("CIDM", Form("0001", Chunk("DATA", Records((1, "a"))), Chunk("DATA", Array.Empty<byte>())))));
        Reject("extra-form", () => CustomizationIdDictionary.Load(Form("CIDM", Form("0001", Chunk("DATA", Records((1, "a")))), Form("0001"))));
        Reject("empty", () => CustomizationIdDictionary.Load(Table(Array.Empty<byte>())));
        Reject("partial-id", () => CustomizationIdDictionary.Load(Table(new byte[] { 1 })));
        Reject("no-name", () => CustomizationIdDictionary.Load(Table(new byte[] { 1, 0 })));
        Reject("unterminated", () => CustomizationIdDictionary.Load(Table(new byte[] { 1, 0, 65 })));
        Reject("empty-name", () => CustomizationIdDictionary.Load(Table(Records((1, "")))));
        Reject("non-ascii", () => CustomizationIdDictionary.Load(Table(new byte[] { 1, 0, 128, 0 })));
        Reject("control", () => CustomizationIdDictionary.Load(Table(new byte[] { 1, 0, 31, 0 })));
        Reject("zero-id", () => CustomizationIdDictionary.Load(Table(Records((0, "a")))));
        Reject("skipped-id", () => CustomizationIdDictionary.Load(Table(Records((1, "a"), (3, "b")))));
        Reject("duplicate-id", () => CustomizationIdDictionary.Load(Table(Records((1, "a"), (1, "b")))));
        Reject("wide-id", () => CustomizationIdDictionary.Load(Table(Records((0x8001, "a")))));
        Reject("duplicate-name", () => CustomizationIdDictionary.Load(Table(Records((1, "a"), (2, "a")))));
        Reject("trailing-byte", () => CustomizationIdDictionary.Load(Table(Records((1, "a")).Concat(new byte[] { 9 }).ToArray())));
        Reject("external-padding", () => CustomizationIdDictionary.Load(good.Concat(new byte[] { 0 }).ToArray()));
        Reject("byte-budget", () => CustomizationIdDictionary.Load(good, new(good.Length - 1)));
        Reject("row-budget", () => CustomizationIdDictionary.Load(good, new(MaximumRecords: 1)));
        Reject("name-budget", () => CustomizationIdDictionary.Load(good, new(MaximumNameBytes: 1)));
        Reject("bad-limit", () => CustomizationIdDictionary.Load(good, new(MaximumRecords: 128)), typeof(ArgumentOutOfRangeException));
        using (var cancel = new CancellationTokenSource()) { cancel.Cancel(); Reject("pre-cancel", () => CustomizationIdDictionary.Load(good, token: cancel.Token), typeof(OperationCanceledException)); }
        var exact = CustomizationIdDictionary.Load(good, new(good.Length, 2, 15)); Check(exact.Entries.Count == 2, "Exact limits accepted");
        var largest = Table(Records(Enumerable.Range(1, 127).Select(i => ((ushort)i, "n" + i)).ToArray()));
        Check(CustomizationIdDictionary.Load(largest).Entries.Count == 127, "ID127 positive boundary");
        Reject("id128", () => CustomizationIdDictionary.Load(Table(Records(Enumerable.Range(1, 128).Select(i => ((ushort)i, "n" + i)).ToArray()))));
        Check(CustomizationIdDictionary.Load(Table(Records((1, "a"), (2, "A")))).Entries.Count == 2, "Case-distinct names accepted");
        byte[] original;
        int? priority;
        using (var config = File.OpenText(args[1]))
        using (var overlay = ConfiguredTreOverlay.Open(args[0], config))
        {
            var lookup = overlay.Lookup("customization/customization_id_manager.iff");
            Check(lookup.Kind == TreLookupKind.Present && lookup.Payload is not null, "Original present");
            original = lookup.Payload!; priority = lookup.Priority;
        }
        Check(priority == 53 && original.Length == 3697 && Convert.ToHexString(SHA256.HashData(original)) == OriginalPin, "Original source pin");
        var result = CustomizationIdDictionary.Load(original); Array.Clear(original);
        Check(result.Entries.Count == 121 && result.Entries.Select(e => (int)e.Id).SequenceEqual(Enumerable.Range(1, 121)), "Original exact121 IDs");
        Check(result.ById[118].FullName == "private/alternate_shader_blade" && CustomizationIdDictionary.ProjectName(result.ById[118].FullName).Key is null, "ID118 anomaly preserved");
        Check(owned.ById[1].FullName == "/private/MixedCase", "Failures preserve prior result");
        Console.WriteLine("cidm=" + JsonSerializer.Serialize(new { Path = "customization/customization_id_manager.iff", Priority = priority, Bytes = 3697, Sha256 = OriginalPin, Rows = result.Entries.Select(Row), NegativeCases = cases.Count, Cases = cases, InputOwned = true, ArchiveClosed = true }));
        Console.WriteLine("PASS: owned CIDM records, exact namespace projection, synthetic malformed/budget/cancellation and pinned original table.");
    }
}
