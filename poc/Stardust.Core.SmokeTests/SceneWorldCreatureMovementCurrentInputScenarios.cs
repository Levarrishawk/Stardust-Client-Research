using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;
public static class SceneWorldCreatureMovementCurrentInputScenarios
{
    private const SceneCreatureMovementConstructorScope Scope = SceneCreatureMovementConstructorScope.CallerVerifiedConcreteClientCreatureRawInputs;
    private static int checks;
    private static readonly string path = "object/root.iff", zero = new('0', 64);
    private static readonly uint crc = LegacyAssetPath.Checksum(path);
    private static void Check(bool value, string why) { checks++; if (!value) throw new InvalidDataException(why); }
    private static void Reject(Action action)
    { try { action(); } catch (Exception e) when (e is InvalidDataException or InvalidOperationException or ArgumentException or NotSupportedException or ObjectDisposedException) { checks++; return; } throw new Exception("Expected rejection"); }
    private static SceneWorldCoordinator World(string pin, uint? rootCrc = null) => new(
        new ObjectTemplateCatalog([new(rootCrc ?? crc, rootCrc is null ? path : "object/wrong.iff", pin, zero, SceneObjectResourceRecipe.CreatureDetail0)]),
        new Dictionary<SceneObjectResourceRecipe, SceneObjectResourceOptions> { [SceneObjectResourceRecipe.CreatureDetail0] = new SceneCreatureResourceOptions(
            new("appearance/lat/synthetic.lat", "idle", zero), new Dictionary<string, LogicalAnimationClipSelection> {
                ["walk"] = new("appearance/lat/synthetic.lat", "idle", zero), ["run"] = new("appearance/lat/synthetic.lat", "idle", zero) }, false) },
        (_, _) => throw new InvalidDataException("No resource loads"), SceneCreateZeroQuaternionPolicy.Reject, 4, 2, 32, 4194304, 32, 4194304, 1048576,
        replicationPolicy: new(SceneWorldReplicationMode.CreatureView3MixedComponents, new(1024, 64, 64, 16, 512), 16, 1024,
            new(1024, 32, 24, 64, 64, 16), CreatureWoundsApplicationPolicy.LegacyBounded, new(1024, 32, 64, 64, 16), new(16, 16)),
        creatureView4PartialPolicy: new(new(MessageLimits: new(MaximumPayloadBytes: 1048576)), new()));
    private static byte[] Start()
    { using var s = new MemoryStream(); using var w = new BinaryWriter(s); w.Write((ushort)9); w.Write(ZoneSceneProtocolCodec.CmdStartSceneOpcode);
        w.Write((byte)0); w.Write(42UL); w.Write((ushort)0); w.Write(0f); w.Write(0f); w.Write(0f); w.Write((ushort)0); w.Write(0UL); return s.ToArray(); }
    private static byte[] Create(uint? rootCrc = null) => SceneObjectLifecycleCodec.EncodeCreate(new(42, 0, 0, 0, 1, 0, 0, 0, rootCrc ?? crc, 0));
    private static long Stage(SceneWorldCoordinator w, byte[] baseline, bool typed, bool final, uint? rootCrc = null)
    { long g = w.BeginSelection(42).Generation; Check(w.Accept(g, Start()), "start"); Check(w.Accept(g, Create(rootCrc)), "create");
        if (typed) Check(w.Accept(g, baseline), "typed3"); if (final) Check(w.Accept(g, SceneObjectLifecycleCodec.EncodeEndBaselines(new(42))), "end");
        Check(w.CompleteMinimumSceneBarrier(g, true) is not null, "barrier"); return g; }
    private static SharedCreatureAuthoredMovementInputs Profile(JsonElement p)
    { var resources = p.GetProperty("Resources").EnumerateArray().ToDictionary(r => r.GetProperty("Path").GetString()!, r => new TreLookupResult(TreLookupKind.Present, Convert.FromHexString(r.GetProperty("Hex").GetString()!), 77));
        return SharedCreatureAuthoredMovementInputs.FromProfile(SharedCreatureMovementInputProfile.Load(q => resources.GetValueOrDefault(q, new(TreLookupKind.Missing)), path, p.GetProperty("RootSha256").GetString()!)); }
    private static object Row(SceneWorldCreatureMovementCurrentReceipt r) => new {
        r.Generation, r.Incarnation.CreationRevision, r.Phase, r.HasCurrentTypedState, r.IsOperationalContext, r.OutputBytes,
        HasReceived = r.Received is not null, Fields = r.Fields.Select(f => new { f.Index, f.RawBits, Origin = f.Origin.ToString(), Eligibility = f.Eligibility.ToString(),
            AuthoredKind = f.AuthoredInput?.Kind.ToString(), AuthoredBits = f.AuthoredInput?.Bits,
            AuthoredPaths = f.AuthoredInput?.Evidence.Select(e => e.SourcePath), f.ConstructorLiteralBits, f.ConstructorLiteralStoreVA,
            ReceivedBits = f.ReceivedInput?.RawBits, ReceivedProvenance = f.ReceivedInput?.Provenance }) };
    public static void Run(string fixtures, string output)
    {
        using var json = JsonDocument.Parse(File.ReadAllBytes(fixtures)); byte[] baseline = Convert.FromHexString(json.RootElement.GetProperty("Baseline").GetString()!);
        var profiles = json.RootElement.GetProperty("Profiles").EnumerateArray().Select(Profile).ToArray(); var rows = new List<object>();
        foreach (var c in json.RootElement.GetProperty("Cases").EnumerateArray())
        {
            var authored = profiles[c.GetProperty("Profile").GetInt32()]; string pin = authored.Sources[path].Sha256;
            using var world = World(pin); long g = Stage(world, baseline, c.GetProperty("Typed").GetBoolean(), c.GetProperty("Finalized").GetBoolean());
            using var view = new SceneWorldCreatureMovementCurrentInputs(world, 42, authored, pin, Scope, true);
            var before = view.Read(g)!; Check(view.IsCurrent(before), "initial context");
            if (c.TryGetProperty("Wire", out var wire)) Check(world.Accept(g, Convert.FromHexString(wire.GetString()!)), "received literal wire");
            long storedBeforeRead = world.RetainedCreatureView4PartialBytes;
            var r = view.Read(g)!; Check(view.IsCurrent(r) && r.Fields.Count == 10 && r.OutputBytes == 1632, "current fixed receipt");
            Check(world.RetainedCreatureView4PartialBytes == storedBeforeRead, "derived read does not seed or mutate received storage");
            if (c.TryGetProperty("Wire", out _)) Check(!view.IsCurrent(before), "late publication invalidates absence");
            var retained = JsonSerializer.Serialize(Row(r)); Check(view.Read(g - 1) is null && retained == JsonSerializer.Serialize(Row(r)), "foreign generation immutable");
            Check(world.RetainedCreatureView4PartialBytes == (r.Received is null ? 0 : world.RetainedCreatureView4PartialBytes), "no bootstrap storage seed");
            using var foreign = new SceneWorldCreatureMovementCurrentInputs(world, 42, authored, pin, Scope, true); Check(!foreign.IsCurrent(r), "foreign consumer");
            rows.Add(Row(r));
        }
        Negatives(profiles[0], baseline);
        File.WriteAllText(output, JsonSerializer.Serialize(new { Cases = rows, Checks = checks })); Console.WriteLine($"R49_CURRENT_INPUTS_PASS cases={rows.Count} checks={checks}");
    }
    private static void Negatives(SharedCreatureAuthoredMovementInputs p, byte[] baseline)
    {
        string pin = p.Sources[path].Sha256; using var w = World(pin); long g = Stage(w, baseline, false, false);
        Reject(() => new SceneWorldCreatureMovementCurrentInputs(w, 42, p, pin, SceneCreatureMovementConstructorScope.Invalid, true));
        Reject(() => new SceneWorldCreatureMovementCurrentInputs(w, 42, p, pin, Scope, false));
        Reject(() => new SceneWorldCreatureMovementCurrentInputs(w, 42, p, zero, Scope, true));
        foreach (int cap in new[] { 0, 1631 }) Reject(() => new SceneWorldCreatureMovementCurrentInputs(w, 42, p, pin, Scope, true, new(MaximumOutputBytes: cap)));
        Reject(() => new SceneWorldCreatureMovementCurrentInputs(w, 42, p, pin, Scope, true, new(MaximumSlots: 9)));
        Reject(() => new SceneWorldCreatureMovementCurrentInputs(w, 42, p, pin, Scope, true, new(MaximumOutputBytes: 65537)));
        using var view = new SceneWorldCreatureMovementCurrentInputs(w, 42, p, pin, Scope, true, new(MaximumOutputBytes: 1632));
        using var final = new SceneWorldCreatureMovementCurrentInputs(w, 42, p, pin, Scope, true, new(SceneReceivedMovementInspectionPhase.FinalizedOnly));
        var absence = view.Read(g)!; Check(final.Read(g) is null && !absence.HasCurrentTypedState && absence.Received is null, "pending no typed/no received");
        Check(w.Accept(g, baseline), "late typed3"); Check(!view.IsCurrent(absence), "late typed state invalidates absence receipt");
        var typed = view.Read(g)!; Check(typed.HasCurrentTypedState && !typed.IsOperationalContext, "pending typed inspection");
        Check(w.Accept(g, SceneObjectLifecycleCodec.EncodeEndBaselines(new(42))), "finalization"); Check(!view.IsCurrent(typed) && final.Read(g) is not null, "phase invalidation");
        var current = view.Read(g)!; Check(current.IsOperationalContext, "final typed current");
        foreach (ushort index in new ushort[] { 2, 3, 6, 13, ushort.MaxValue }) Reject(() => current.Get(index));
        Reject(() => ((IList<SceneCreatureMovementCurrentField>)current.Fields)[0] = current.Fields[1]);
        Task.Run(() => Reject(() => view.Read(g))).GetAwaiter().GetResult(); Task.Run(() => Reject(() => view.Dispose())).GetAwaiter().GetResult();
        Task.Run(() => Reject(() => new SceneWorldCreatureMovementCurrentInputs(w, 42, p, pin, Scope, true))).GetAwaiter().GetResult(); Check(view.IsCurrent(current), "owner recovery");
        Check(w.ApplyCreatureView4PartialDelta(g, null, new(42, 0, [new(6, 123)])), "listen-only first publication");
        Check(!view.IsCurrent(current), "nonmovement first publication still stales"); var listened = view.Read(g)!;
        Check(listened.Received is not null && listened.Fields.All(f => f.Origin != SceneCreatureMovementCurrentOrigin.Received), "missing movement remains bootstrap");
        Check(w.ApplyCreatureView4PartialDelta(g, listened.Received!.Source, new(42, 0, [new(7, 0x7F800001), new(11, 0x80000000)])), "raw override");
        var raw = view.Read(g)!; Check(raw.Get(7).RawBits == 0x7F800001 && raw.Get(7).Origin == SceneCreatureMovementCurrentOrigin.Received && raw.Get(7).Eligibility == SceneCreatureMovementCurrentEligibility.NonFinite, "nan override no fallback");
        Check(raw.Get(11).RawBits == 0x80000000 && raw.Get(11).Eligibility == SceneCreatureMovementCurrentEligibility.FiniteCurrent, "signed zero override");
        Check(w.Accept(g, SceneObjectLifecycleCodec.EncodeContainment(new(42, 0, 1))), "contain"); Check(!view.IsCurrent(raw), "registry context stale");
        var latest = view.Read(g)!; Check(w.Accept(g, SceneObjectLifecycleCodec.EncodeDestroy(new(42, 0))), "destroy");
        Check(view.Read(g) is null && !view.IsCurrent(latest), "destroy stale"); Check(w.Accept(g, Create()), "recreate"); Check(view.Read(g) is null, "bound old incarnation");
        using var replacement = new SceneWorldCreatureMovementCurrentInputs(w, 42, p, pin, Scope, true); var recreated = replacement.Read(g)!;
        Check(recreated.Received is null, "new incarnation received absence"); long next = Stage(w, baseline, true, true); Check(replacement.Read(next) is null && !replacement.IsCurrent(recreated), "reset stale");
        using var after = new SceneWorldCreatureMovementCurrentInputs(w, 42, p, pin, Scope, true); var historical = after.Read(next)!;
        using (var other = World(pin)) { long og = Stage(other, baseline, true, true); using var foreign = new SceneWorldCreatureMovementCurrentInputs(other, 42, p, pin, Scope, true); Check(!foreign.IsCurrent(historical) && !after.IsCurrent(foreign.Read(og)!), "foreign worlds"); }
        after.Dispose(); after.Dispose(); Check(!after.IsCurrent(historical), "dispose invalidation"); Reject(() => after.Read(next));
        using var closing = new SceneWorldCreatureMovementCurrentInputs(w, 42, p, pin, Scope, true); var beforeClose = closing.Read(next)!; w.Close(); Check(!closing.IsCurrent(beforeClose) && closing.Read(next) is null, "world close");
        uint wrongCrc = LegacyAssetPath.Checksum("object/wrong.iff");
        using var wrong = World(pin, wrongCrc); Stage(wrong, baseline, false, false, wrongCrc); Reject(() => new SceneWorldCreatureMovementCurrentInputs(wrong, 42, p, pin, Scope, true));
    }
}
