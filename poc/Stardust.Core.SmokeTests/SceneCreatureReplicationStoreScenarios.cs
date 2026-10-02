using System.Text.Json;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

internal static class SceneCreatureReplicationStoreScenarios
{
    private const ulong Oid = 0xF122334455667788;
    private static readonly CreatureView3BaselineLimits Limits = new(1024, 64, 64, 16, 512);
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
    private static object State(CreatureView3ScalarState value) => new { value.Posture, value.Rank, value.LinkId, HeightBits = BitConverter.SingleToUInt32Bits(value.Height), value.Shock, value.States };
    internal static void Run(IReadOnlyDictionary<string, string> peer, string? exportPath = null)
    {
        byte[] Bytes(string name) => Convert.FromHexString(peer[name]);
        byte[] raw = Bytes("baseline"); var baseline = CreatureView3BaselineCodec.Decode(raw, Limits); Array.Clear(raw);
        var plan = CreatureView3ScalarDeltaCodec.Decode(Bytes("delta"), 1024, 16);
        var wrongBaseline = CreatureView3BaselineCodec.Decode(Bytes("wrongBaseline"), Limits);
        var wrongPlan = CreatureView3ScalarDeltaCodec.Decode(Bytes("wrongDelta"), 1024, 16);
        var big = CreatureView3BaselineCodec.Decode(Bytes("bigBaseline"), Limits);
        var create = SceneObjectLifecycleCodec.DecodeCreate(Bytes("create"));
        var registry = new SceneObjectRegistry(4, 17); using var store = new SceneCreatureReplicationStore(registry, 2, 240);
        var created = registry.Create(17, create)!; var incarnation = created.Incarnation;
        Check(incarnation.CreationRevision == created.Handle.Revision && registry.IsCurrent(incarnation), "Creation revision supplies unique incarnation");
        var compatible = new SceneObjectSnapshot(created.Handle, created.Create, created.Phase, created.ContainerId, created.ArrangementBits);
        Check(registry.IsCurrent(compatible.Handle) && !registry.IsCurrent(compatible.Incarnation), "Existing snapshot constructor remains compatible without fabricating incarnation");
        Check(registry.Create(17, create)!.Incarnation == incarnation, "Duplicate create preserves incarnation");
        Reject<InvalidOperationException>(() => store.Apply(incarnation, plan));
        var seed = store.Seed(incarnation, baseline)!; var oldList = store.Snapshots();
        var applied = store.Apply(incarnation, plan)!;
        Check(applied.Scalars.Posture == 7 && applied.Scalars.Rank == 254 && applied.Scalars.LinkId == 0xFE02030405060708 &&
            BitConverter.SingleToUInt32Bits(applied.Scalars.Height) == 0x80000000 && applied.Scalars.Shock == 0xF1020304 && applied.Scalars.States == 0xFEDCBA9876543210, "Ordered six scalar overlay raw bits");
        Check(seed.Scalars == baseline.Scalars && applied.Baseline == seed.Baseline && applied.Revision == seed.Revision + 1 && !store.IsCurrent(seed) && store.IsCurrent(applied), "Immutable baseline retained separately from current overlay");
        Check(CreatureView3BaselineCodec.Encode(applied.Baseline, Limits).SequenceEqual(Bytes("baseline")), "All fixed baseline bytes preserved");
        byte[] name = applied.Baseline.NameFile.CopyBytes(); Array.Clear(name); Check(applied.Baseline.NameFile.CopyBytes()[0] == 102, "Owned byte string copy");
        Reject<NotSupportedException>(() => ((IList<int>)applied.Baseline.Components.Values)[0] = 0);
        Reject<NotSupportedException>(() => ((IList<SceneCreatureReplicationSnapshot>)oldList)[0] = applied);
        Reject<InvalidDataException>(() => store.Seed(incarnation, wrongBaseline)); Reject<InvalidDataException>(() => store.Apply(incarnation, wrongPlan));
        Check(store.Find(incarnation) == applied && store.RetainedDecodedBytes == 119, "Wrong-object rollback and one baseline charge");
        var foreign = new SceneObjectRegistry(1, 17).Create(17, create)!.Incarnation;
        Check(store.Seed(foreign, baseline) is null && store.Apply(foreign, plan) is null && store.Find(incarnation) == applied, "Foreign registry does not mutate owned entry");
        using (var other = new SceneCreatureReplicationStore(registry)) { other.Seed(incarnation, baseline); Check(!other.IsCurrent(applied), "Foreign store snapshot rejected"); }

        registry.Contain(17, SceneObjectLifecycleCodec.DecodeContainment(Bytes("contain"))); var contained = registry.Find(Oid)!;
        Check(!registry.IsCurrent(created.Handle) && registry.IsCurrent(incarnation) && contained.Incarnation == incarnation && store.IsCurrent(applied), "Contain changes handle but retains incarnation state");
        registry.EndBaselines(17, SceneObjectLifecycleCodec.DecodeEndBaselines(Bytes("end"))); var ended = registry.Find(Oid)!;
        Check(!registry.IsCurrent(contained.Handle) && ended.Incarnation == incarnation && store.Find(incarnation) == applied, "End-baselines retains overlay while exact handles retire");
        var afterLifecycle = store.Apply(incarnation, plan)!;
        var reseed = store.Seed(incarnation, baseline)!;
        Check(reseed.Scalars == baseline.Scalars && reseed.Revision == afterLifecycle.Revision + 1 && store.RetainedDecodedBytes == 119 && !store.IsCurrent(afterLifecycle), "Deterministic baseline replacement resets overlay without duplicate charge");
        registry.Contain(17, new(Oid, 0, 0xFFFFFFFE)); registry.EndBaselines(17, new(Oid));
        Check(registry.Find(Oid)!.Incarnation == incarnation && store.IsCurrent(reseed), "Idempotent lifecycle preserves incarnation");

        // Detaching children changes their handle only; parent replacement must receive a different incarnation.
        var child = registry.Create(17, create with { ObjectId = Oid + 1 })!; registry.Contain(17, new(Oid + 1, Oid, 0)); var childBefore = registry.Find(Oid + 1)!;
        var childSeed = store.Seed(childBefore.Incarnation, wrongBaseline)!;
        var replaced = registry.Replace(17, create)!; var childAfter = registry.Find(Oid + 1)!;
        Check(replaced.Incarnation != incarnation && replaced.Incarnation.CreationRevision == replaced.Handle.Revision && !registry.IsCurrent(incarnation), "Explicit replace issues new unique incarnation");
        Check(childAfter.Incarnation == childBefore.Incarnation && !registry.IsCurrent(childBefore.Handle) && store.IsCurrent(childSeed), "Detached child retains incarnation and store state");
        Check(store.Apply(incarnation, plan) is null && store.Find(replaced.Incarnation) is null, "Replacement cannot inherit old baseline");
        var replacementSeed = store.Seed(replaced.Incarnation, baseline)!;
        Check(store.Seed(incarnation, baseline) is null && store.IsCurrent(replacementSeed), "Old token cannot erase replacement seed");
        registry.Destroy(17, SceneObjectLifecycleCodec.DecodeDestroy(Bytes("destroy")));
        Check(!store.IsCurrent(replacementSeed) && !registry.IsCurrent(replaced.Incarnation), "Destroy retires incarnation");
        var recreated = registry.Create(17, create)!; var recreatedSeed = store.Seed(recreated.Incarnation, baseline)!;
        Check(recreated.Incarnation.CreationRevision > replaced.Incarnation.CreationRevision && store.Apply(replaced.Incarnation, plan) is null && store.IsCurrent(recreatedSeed), "Same-ID recreation cannot be affected by old incarnation");
        registry.Reset(18); var next = registry.Create(18, create)!;
        Check(store.Apply(recreated.Incarnation, plan) is null && store.Seed(recreated.Incarnation, baseline) is null && store.RetainedDecodedBytes == 0, "Scene reset retires all incarnations and accounting");
        var nextSeed = store.Seed(next.Incarnation, baseline)!; Check(nextSeed.Revision > recreatedSeed.Revision, "Local revision never rewinds");
        registry.Close(); Check(store.Snapshots().Count == 0 && store.RetainedDecodedBytes == 0 && store.Apply(next.Incarnation, plan) is null, "Registry close cleanup");
        store.Close(); store.Close(); Reject<ObjectDisposedException>(() => store.Seed(next.Incarnation, baseline)); Reject<ObjectDisposedException>(() => store.Apply(next.Incarnation, plan));
        Check(!store.IsCurrent(nextSeed) && store.Snapshots().Count == 0, "Store close retires publications");

        var boundedRegistry = new SceneObjectRegistry(2, 1); var first = boundedRegistry.Create(1, create)!; var second = boundedRegistry.Create(1, create with { ObjectId = Oid + 1 })!;
        using (var bounded = new SceneCreatureReplicationStore(boundedRegistry, 1, 120))
        {
            var current = bounded.Seed(first.Incarnation, baseline)!;
            Reject<InvalidDataException>(() => bounded.Seed(second.Incarnation, wrongBaseline)); Reject<InvalidDataException>(() => bounded.Seed(first.Incarnation, big));
            Check(bounded.Find(first.Incarnation) == current && bounded.RetainedDecodedBytes == 119 && bounded.Snapshots().Count == 1, "Capacity and replacement byte-budget rollback");
            Check(bounded.Seed(first.Incarnation, baseline)!.Revision == current.Revision + 1, "Failed seeds consume no revision");
            boundedRegistry.Destroy(1, new(Oid, 0)); Check(bounded.Seed(second.Incarnation, wrongBaseline) is not null && bounded.RetainedDecodedBytes == 119, "Retirement frees capacity and bytes");
        }
        using (var bytes = new SceneCreatureReplicationStore(boundedRegistry, 2, 200))
        {
            bytes.Seed(second.Incarnation, wrongBaseline); var again = boundedRegistry.Create(1, create)!;
            Reject<InvalidDataException>(() => bytes.Seed(again.Incarnation, baseline)); Check(bytes.Snapshots().Count == 1 && bytes.RetainedDecodedBytes == 119, "Aggregate bytes rollback independent of capacity");
        }
        var active = boundedRegistry.Find(Oid)!;
        using (var overflow = new SceneCreatureReplicationStore(boundedRegistry, 1, 240, ulong.MaxValue - 1))
        {
            var last = overflow.Seed(active.Incarnation, baseline)!;
            Reject<OverflowException>(() => overflow.Apply(active.Incarnation, plan)); Reject<OverflowException>(() => overflow.Seed(active.Incarnation, big));
            Check(overflow.Find(active.Incarnation) == last && overflow.IsCurrent(last) && overflow.RetainedDecodedBytes == 119, "Apply and baseline-replacement overflow rollback"); overflow.Close(); Check(overflow.RetainedDecodedBytes == 0, "Close at maximum counter");
        }
        using (var overflow = new SceneCreatureReplicationStore(boundedRegistry, 1, 240, ulong.MaxValue))
        { Reject<OverflowException>(() => overflow.Seed(active.Incarnation, baseline)); Check(overflow.Snapshots().Count == 0 && overflow.RetainedDecodedBytes == 0, "Initial seed overflow rollback"); }
        var registryOverflow = new SceneObjectRegistry(1, 1, ulong.MaxValue); Reject<OverflowException>(() => registryOverflow.Create(1, create)); Check(registryOverflow.Count == 0, "Registry incarnation creation overflow atomic");
        var replaceOverflow = new SceneObjectRegistry(1, 1, ulong.MaxValue - 1); var lastObject = replaceOverflow.Create(1, create)!;
        Reject<OverflowException>(() => replaceOverflow.Replace(1, create)); Check(replaceOverflow.IsCurrent(lastObject.Handle) && replaceOverflow.IsCurrent(lastObject.Incarnation), "Registry replacement overflow preserves both identities");
        using (var owner = new SceneCreatureReplicationStore(boundedRegistry))
        {
            Exception? error = null; var thread = new Thread(() => { try { owner.Snapshots(); } catch (Exception e) { error = e; } }); thread.Start(); thread.Join(); Check(error is InvalidOperationException, "Dedicated foreign thread rejected");
        }
        Reject<ArgumentOutOfRangeException>(() => new SceneCreatureReplicationStore(boundedRegistry, 0)); Reject<ArgumentOutOfRangeException>(() => new SceneCreatureReplicationStore(boundedRegistry, 4097));
        Reject<ArgumentOutOfRangeException>(() => new SceneCreatureReplicationStore(boundedRegistry, maximumDecodedBytes: 80));
        if (exportPath is not null)
        {
            var export = new { SeedIncarnation = seed.Incarnation, CreatedHandle = created.Handle, ContainedHandle = contained.Handle, EndedHandle = ended.Handle,
                ReplacementIncarnation = replaced.Incarnation, RecreatedIncarnation = recreated.Incarnation, NextIncarnation = next.Incarnation,
                Initial = State(seed.Scalars), Applied = State(applied.Scalars), AfterLifecycle = State(afterLifecycle.Scalars), Reseeded = State(reseed.Scalars),
                SeedRevision = seed.Revision, AppliedRevision = applied.Revision, AfterLifecycleRevision = afterLifecycle.Revision, ReseedRevision = reseed.Revision,
                BaselineHex = Convert.ToHexString(CreatureView3BaselineCodec.Encode(applied.Baseline, Limits)), DeltaHex = Convert.ToHexString(CreatureView3ScalarDeltaCodec.Encode(plan, 1024, 16)),
                BaselineDecodedBytes = applied.Baseline.DecodedBytes, FinalRetainedBytes = store.RetainedDecodedBytes, FinalCount = store.Snapshots().Count };
            File.WriteAllText(exportPath, JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        }
        Console.WriteLine("PASS: incarnation/full baseline/six-scalar overlay, lifecycle retention, retirement/foreign tokens, immutable ownership, bounded replacement and overflow rollback");
    }
}
