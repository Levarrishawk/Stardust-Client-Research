using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;

internal static class LocalizedObjectNameScenarios
{
    private sealed record Fixture(string Name, string FileHex, string KeyHex, ushort[] Units, int Maximum, string Primary, string? Fallback, string Mode);
    private sealed record Input(string TableHex, Fixture[] Cases);
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static object Receipt(LocalizedObjectNameResult r) => new { r.DisplayText, Origin = r.Origin.ToString(), r.Truncated, r.TablePath, r.MatchedId };
    private static void Emit(string tag, object value) => Console.WriteLine(tag + "=" + JsonSerializer.Serialize(value));
    private static CreatureView3NameState Model(byte[] file, byte[] key, ushort[] units)
    {
        // Independently authored complete baseline, permitting the frozen baseline's larger historical caps.
        using var stream = new MemoryStream(); using var w = new BinaryWriter(stream);
        void Narrow(byte[] bytes) { if (bytes.Length >= 65535) { w.Write((ushort)65535); w.Write((uint)bytes.Length); } else w.Write((ushort)bytes.Length); w.Write(bytes); }
        w.Write(0f); Narrow(file); w.Write(0xDEADBEEFu); Narrow(key); w.Write((uint)units.Length); foreach (ushort u in units) w.Write(u);
        w.Write(0u); Narrow([]); w.Write(0u); w.Write(0u);
        for (int i = 0; i < 4; i++) w.Write(0u);
        w.Write((byte)1); w.Write((byte)0); w.Write((byte)0); w.Write(0UL); w.Write(1f); w.Write(0u); w.Write(0UL); w.Write(0u); w.Write(0u); w.Flush();
        byte[] fields = stream.ToArray(); int maximum = fields.Length + 2;
        var envelope = new ReplicationEnvelope(ReplicationEnvelopeKind.Baseline, 7, CreatureView3BaselineCodec.RawTag, 3, 18, fields, maximum);
        return CreatureView3NameState.FromBaseline(CreatureView3BaselineCodec.Decode(ReplicationEnvelopeCodec.EncodeBaseline(envelope, maximum), new(maximum, 100000, 100000, 0, 1000000)));
    }
    internal static void Run(string[] args)
    {
        Check(args.Length == 3, "root/config/name fixtures required");
        var input = JsonSerializer.Deserialize<Input>(File.ReadAllBytes(args[2]))!;
        byte[] tableBytes = Convert.FromHexString(input.TableHex); var table = LocalizedStringTable.Load(tableBytes); Array.Clear(tableBytes);
        var cases = new List<object>();
        foreach (var f in input.Cases)
        {
            byte[] file = Convert.FromHexString(f.FileHex), key = Convert.FromHexString(f.KeyHex); var model = Model(file, key, f.Units); Array.Clear(file); Array.Clear(key);
            var calls = new List<string>();
            LocalizedStringTable? Lookup(string path) { calls.Add(path); return f.Mode == "primary" && path.StartsWith(f.Primary + "/", StringComparison.Ordinal) || f.Mode == "fallback" && path.StartsWith(f.Fallback + "/", StringComparison.Ordinal) ? table : null; }
            try { var result = LocalizedObjectNameResolver.Resolve(model, Lookup, f.Primary, f.Fallback, f.Maximum); cases.Add(new { f.Name, Rejected = false, Calls = calls, Result = Receipt(result) }); }
            catch (ArgumentException) { cases.Add(new { f.Name, Rejected = true, Calls = calls }); }
            Check(model.NameSpacer == 0xDEADBEEF && Convert.ToHexString(model.NameFile.CopyBytes()) == f.FileHex && Convert.ToHexString(model.NameKey.CopyBytes()) == f.KeyHex && model.CustomNameUnits.SequenceEqual(f.Units), "Raw model preserved");
        }
        var normal = Model("obj_n"u8.ToArray(), "key"u8.ToArray(), []); int checks = 0;
        void Assert(bool value, string message) { Check(value, message); checks++; }
        void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { checks++; return; } throw new Exception("Expected " + typeof(T).Name); }
        var failure = new IOException("controlled");
        bool exact = false; try { LocalizedObjectNameResolver.Resolve(normal, _ => throw failure); } catch (IOException e) { exact = ReferenceEquals(e, failure); } Assert(exact, "Callback exception identity");
        Throws<InvalidOperationException>(() => LocalizedObjectNameResolver.Resolve(normal, _ => { LocalizedObjectNameResolver.Resolve(normal, _ => table); return table; }));
        Throws<InvalidOperationException>(() => LocalizedObjectNameResolver.Resolve(normal, _ => { try { LocalizedObjectNameResolver.Resolve(normal, _ => table); } catch (InvalidOperationException) { } return table; }));
        var custom = normal.Apply(CreatureView3NameUpdate.FromCustomName(new ushort[] { 65 }));
        Throws<InvalidOperationException>(() => LocalizedObjectNameResolver.Resolve(normal, _ => { try { LocalizedObjectNameResolver.Resolve(custom, _ => table); } catch (InvalidOperationException) { } return table; }));
        var prior = LocalizedObjectNameResolver.Resolve(normal, _ => table); Assert(prior.Origin == LocalizedObjectNameOrigin.PrimaryTable, "Guard recovery");
        var huge = Model([], [], Enumerable.Repeat((ushort)65, 70000).ToArray());
        Assert(LocalizedObjectNameResolver.Resolve(huge, _ => throw new Exception("No lookup"), maximumDisplayUnits: 3) == new LocalizedObjectNameResult("AAA", LocalizedObjectNameOrigin.Custom, true), "Large baseline custom prefix");
        Assert(LocalizedObjectNameResolver.Resolve(Model(new byte[70000], [], []), _ => throw new Exception("No lookup")).Origin == LocalizedObjectNameOrigin.InvalidStringId, "Large baseline file rejected before lookup");
        Assert(LocalizedObjectNameResolver.Resolve(Model("obj_n"u8.ToArray(), new byte[70000], []), _ => throw new Exception("No lookup")).Origin == LocalizedObjectNameOrigin.InvalidStringId, "Large baseline key rejected before lookup");
        ushort[] customInput = [65, 66]; var update = CreatureView3NameUpdate.FromCustomName(customInput); var owned = normal.Apply(update); Array.Clear(customInput);
        Assert(LocalizedObjectNameResolver.Resolve(owned, _ => throw new Exception("No lookup")).DisplayText == "AB" && normal.CustomNameUnits.Count == 0, "Owned update and original state unchanged");
        Assert(prior == LocalizedObjectNameResolver.Resolve(normal, _ => table), "Prior result/table preserved after failures");
        Assert(LocalizedObjectNameResolver.Resolve(normal.Apply(CreatureView3NameUpdate.FromCustomName(new ushort[] { 0xD83D, 0xDE00 })), _ => table, maximumDisplayUnits: 1).DisplayText == "", "No split surrogate pair");
        Assert(LocalizedObjectNameResolver.Resolve(normal, _ => { var other = Task.Run(() => LocalizedObjectNameResolver.Resolve(custom, _ => table)).GetAwaiter().GetResult(); Check(other.DisplayText == "A", "Other thread allowed"); return table; }).MatchedId == 1, "Independent callback thread");
        Throws<InvalidDataException>(() => LocalizedObjectNameResolver.Resolve(normal, _ => LocalizedStringTable.Load(new byte[13])));
        int attempted = 0; exact = false; try { LocalizedObjectNameResolver.Resolve(normal, _ => ++attempted == 1 ? null : throw failure, fallbackDirectory: "string/fr"); } catch (IOException e) { exact = ReferenceEquals(e, failure); } Assert(exact && attempted == 2, "Fallback exception unchanged");
        Check(checks == 14, "Controlled check accounting");
        Parallel.For(0, 32, _ => Check(LocalizedObjectNameResolver.Resolve(normal, _ => table) == prior, "Independent concurrent calls"));
        var originals = new List<object>(); var resources = new List<(string Path, int? Priority, byte[] Bytes)>();
        using (var config = File.OpenText(args[1])) using (var overlay = ConfiguredTreOverlay.Open(args[0], config))
            foreach (string path in new[] { "string/en/obj_n.stf", "string/en/terminal_name.stf" }) { var found = overlay.Lookup(path); Check(found.Kind == TreLookupKind.Present && found.Payload is not null, "Pinned original present"); resources.Add((path, found.Priority, found.Payload!)); }
        foreach (var resource in resources)
        {
            int size = resource.Bytes.Length; string pin = Convert.ToHexString(SHA256.HashData(resource.Bytes));
            Check(resource.Priority == 53 && (resource.Path.EndsWith("obj_n.stf", StringComparison.Ordinal) ? size == 2527 && pin == "3EEA0D2B5E785F593B9C9180877665A495CD67B68BF3D6DAF74F7820FCAFB054" : size == 4610 && pin == "CDEDCD3ECE3E5FE6C7455B70760DDDB047B2F16281D740A1034D465F081FF603"), "Pinned table identity");
            var originalTable = LocalizedStringTable.Load(resource.Bytes); Array.Clear(resource.Bytes); var record = originalTable.Records[0]; byte[] rawKey = record.Key.CopyBytes();
            string identifier = Path.GetFileNameWithoutExtension(resource.Path); var model = Model(System.Text.Encoding.ASCII.GetBytes(identifier), rawKey, []); Array.Clear(rawKey); var calls = new List<string>();
            var result = LocalizedObjectNameResolver.Resolve(model, path => { calls.Add(path); Check(path == resource.Path, "Exact original table path"); return originalTable; });
            Check(result.MatchedId == record.Id && result.Origin == LocalizedObjectNameOrigin.PrimaryTable, "Original selected lookup");
            originals.Add(new { resource.Path, resource.Priority, Bytes = size, Sha256 = pin, KeyHex = Convert.ToHexString(record.Key.CopyBytes()), Units = record.Utf16Units, Calls = calls, Result = Receipt(result) });
        }
        Emit("name_synthetic", new { Cases = cases, ControlledChecks = checks, ConcurrentCalls = 32, RawPreserved = true });
        Emit("name_original", new { Lookups = originals, ArchivesClosed = true });
        Console.WriteLine("PASS: stateless bounded object-name precedence, exact keys, table-only fallback, callback recovery and two original lookups.");
    }
}
