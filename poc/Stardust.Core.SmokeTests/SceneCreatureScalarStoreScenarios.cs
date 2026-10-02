using System.Buffers.Binary;
using System.Text.Json;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

internal static class SceneCreatureScalarStoreScenarios
{
    private const ulong Oid = 0xF122334455667788;
    private static readonly CreatureView3ScalarState Initial = new(1, 2, 3, 4.5f, 5, 6);
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
    private static object State(CreatureView3ScalarState state) => new { state.Posture, state.Rank, state.LinkId, HeightBits = BitConverter.SingleToUInt32Bits(state.Height), state.Shock, state.States };

    internal static void Run(IReadOnlyDictionary<string, string> peer, string? exportPath = null)
    {
        byte[] Bytes(string name) => Convert.FromHexString(peer[name]);
        var create = SceneObjectLifecycleCodec.DecodeCreate(Bytes("create"));
        byte[] raw = Bytes("delta"); var plan = CreatureView3ScalarDeltaCodec.Decode(raw, 1024, 16); Array.Clear(raw);
        var registry = new SceneObjectRegistry(4, 7); using var store = new SceneCreatureScalarStore(registry, 2);
        var handle = registry.Create(7, create)!.Handle;
        Reject<InvalidOperationException>(() => store.Apply(handle, plan));
        var seeded = store.Seed(handle, Initial)!; var oldArray = store.Snapshots();
        var applied = store.Apply(handle, plan)!;
        Check(applied.State.Posture == 7 && applied.State.Rank == 254 && applied.State.LinkId == 0xFE02030405060708 &&
            BitConverter.SingleToUInt32Bits(applied.State.Height) == 0x80000000 && applied.State.Shock == 0xF1020304 && applied.State.States == 0xFEDCBA9876543210,
            "All six ordered raw scalar values");
        Check(applied.Revision == seeded.Revision + 1 && seeded.State == Initial && oldArray.Single() == seeded && !store.IsCurrent(seeded) && store.IsCurrent(applied), "Immutable observations and monotonic state revision");
        Reject<NotSupportedException>(() => ((IList<SceneCreatureScalarSnapshot>)oldArray)[0] = applied);
        Reject<InvalidOperationException>(() => store.Seed(handle, Initial));
        var wrong = CreatureView3ScalarDeltaCodec.Decode(Bytes("wrongObject"), 1024, 16);
        Reject<InvalidDataException>(() => store.Apply(handle, wrong));
        Check(store.Find(handle) == applied, "Wrong object rollback");
        var foreign = new SceneObjectRegistry(1, 7); var foreignHandle = foreign.Create(7, create)!.Handle;
        Check(store.Apply(foreignHandle, plan) is null && store.Seed(foreignHandle, Initial) is null && store.Find(handle) == applied, "Foreign registry rejects without affecting owned state");
        using (var other = new SceneCreatureScalarStore(registry)) { other.Seed(handle, Initial); Check(!other.IsCurrent(applied), "Foreign store publication rejected"); }

        // Accepted codec must completely reject unsupported/truncated/wrong-schema messages before store application.
        byte[] malformed = Bytes("delta"); Array.Resize(ref malformed, malformed.Length + 3);
        BinaryPrimitives.WriteUInt32LittleEndian(malformed.AsSpan(19), checked((uint)malformed.Length - 23));
        BinaryPrimitives.WriteUInt16LittleEndian(malformed.AsSpan(malformed.Length - 3), 17);
        Reject<InvalidDataException>(() => store.Apply(handle, CreatureView3ScalarDeltaCodec.Decode(malformed, 1024, 16)));
        foreach (int offset in new[] { 14, 18 }) { var bad = Bytes("delta"); bad[offset] ^= 1; Reject<InvalidDataException>(() => CreatureView3ScalarDeltaCodec.Decode(bad, 1024, 16)); }
        Reject<InvalidDataException>(() => CreatureView3ScalarDeltaCodec.Decode(Bytes("delta")[..^1], 1024, 16));
        Check(store.Find(handle) == applied, "Malformed plan leaves existing scalar snapshot untouched");

        registry.Contain(7, SceneObjectLifecycleCodec.DecodeContainment(Bytes("contain")));
        var contained = registry.Find(Oid)!.Handle;
        Check(store.Find(handle) is null && store.Apply(handle, plan) is null && !store.IsCurrent(applied) && store.Snapshots().Count == 0, "Containment retires exact revision");
        Reject<InvalidOperationException>(() => store.Apply(contained, plan));
        var reseeded = store.Seed(contained, Initial)!;
        registry.EndBaselines(7, SceneObjectLifecycleCodec.DecodeEndBaselines(Bytes("end")));
        var finalized = registry.Find(Oid)!.Handle;
        Check(store.Apply(contained, plan) is null && !store.IsCurrent(reseeded), "End-baselines invalidates pending scalar binding");
        Reject<InvalidOperationException>(() => store.Apply(finalized, plan));
        var finalSeed = store.Seed(finalized, Initial)!;
        registry.EndBaselines(7, SceneObjectLifecycleCodec.DecodeEndBaselines(Bytes("end")));
        Check(store.IsCurrent(finalSeed), "Idempotent end-baselines keeps exact handle");
        var finalizedApplied = store.Apply(finalized, plan)!; Check(finalizedApplied.State == applied.State, "All six fields after finalized lifecycle/reseed");
        registry.Replace(7, create with { PositionX = 9 }); var replacement = registry.Find(Oid)!.Handle;
        Check(store.Apply(finalized, plan) is null && store.Find(replacement) is null, "Replacement cannot inherit state");
        var replacementSeed = store.Seed(replacement, Initial)!;
        registry.Destroy(7, SceneObjectLifecycleCodec.DecodeDestroy(Bytes("destroy")));
        Check(!store.IsCurrent(replacementSeed) && store.Snapshots().Count == 0, "Destroy retires state");
        var recreated = registry.Create(7, SceneObjectLifecycleCodec.DecodeCreate(Bytes("create")))!.Handle;
        var recreatedSeed = store.Seed(recreated, Initial)!;
        Check(store.Apply(replacement, plan) is null && store.IsCurrent(recreatedSeed), "Old same-ID handle cannot affect recreated state");
        registry.Reset(8); var nextGeneration = registry.Create(8, create)!.Handle;
        Check(store.Apply(recreated, plan) is null && store.Seed(recreated, Initial) is null && store.Find(nextGeneration) is null, "Old generation cannot resurrect state");
        var nextSeed = store.Seed(nextGeneration, Initial)!;
        Check(nextSeed.Revision > replacementSeed.Revision && applied.State.Posture == 7 && seeded.State == Initial, "Revision never rewinds and old states remain immutable");
        registry.Close(); Check(store.Snapshots().Count == 0 && store.Apply(nextGeneration, plan) is null, "Registry close retires state");
        store.Close(); store.Close(); Check(store.Find(nextGeneration) is null && !store.IsCurrent(nextSeed) && store.Snapshots().Count == 0, "Store close idempotent and observations empty");
        Reject<ObjectDisposedException>(() => store.Seed(nextGeneration, Initial)); Reject<ObjectDisposedException>(() => store.Apply(nextGeneration, plan));

        var boundedRegistry = new SceneObjectRegistry(2, 1); var first = boundedRegistry.Create(1, create)!.Handle; var second = boundedRegistry.Create(1, create with { ObjectId = Oid + 1 })!.Handle;
        using (var bounded = new SceneCreatureScalarStore(boundedRegistry, 1))
        {
            var initial = bounded.Seed(first, Initial with { Height = -float.MaxValue })!;
            Check(initial.State.Height == -float.MaxValue, "Finite raw height is not clamped");
            Reject<InvalidDataException>(() => bounded.Seed(second, Initial)); Check(bounded.Find(first) == initial && bounded.Snapshots().Count == 1, "Capacity rollback");
            boundedRegistry.Destroy(1, new(Oid, 0)); var secondSeed = bounded.Seed(second, Initial)!; Check(secondSeed.Revision == initial.Revision + 1, "Retirement frees capacity without revision rewind");
        }
        using (var invalid = new SceneCreatureScalarStore(boundedRegistry))
        {
            foreach (float value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity }) Reject<InvalidDataException>(() => invalid.Seed(second, Initial with { Height = value }));
            Check(invalid.Snapshots().Count == 0 && invalid.Seed(second, Initial)!.Revision == 1, "Invalid seed rollback includes local counter");
        }
        using (var exhausted = new SceneCreatureScalarStore(boundedRegistry, 1, ulong.MaxValue))
        { Reject<OverflowException>(() => exhausted.Seed(second, Initial)); Check(exhausted.Snapshots().Count == 0, "Seed overflow atomic"); }
        var again = boundedRegistry.Create(1, create)!.Handle;
        using (var overflow = new SceneCreatureScalarStore(boundedRegistry, 1, ulong.MaxValue - 1))
        {
            var last = overflow.Seed(again, Initial)!; Reject<OverflowException>(() => overflow.Apply(again, plan));
            Check(overflow.Find(again) == last && overflow.IsCurrent(last) && last.Revision == ulong.MaxValue, "Apply overflow leaves prior owned state current");
            overflow.Close(); Check(overflow.Snapshots().Count == 0, "Close does not increment exhausted revision");
        }
        using (var owner = new SceneCreatureScalarStore(boundedRegistry))
        {
            Exception? error = null; var thread = new Thread(() => { try { owner.Snapshots(); } catch (Exception e) { error = e; } }); thread.Start(); thread.Join();
            Check(error is InvalidOperationException && owner.Snapshots().Count == 0, "Dedicated foreign thread rejected");
        }
        Reject<ArgumentOutOfRangeException>(() => new SceneCreatureScalarStore(boundedRegistry, 0)); Reject<ArgumentOutOfRangeException>(() => new SceneCreatureScalarStore(boundedRegistry, 4097));
        if (exportPath is not null)
        {
            var export = new { ObjectId = Oid, SeedRevision = seeded.Revision, AppliedRevision = applied.Revision, Initial = State(seeded.State), Applied = State(applied.State),
                SeedHandle = seeded.Handle, AppliedHandle = applied.Handle, FinalizedHandle = finalizedApplied.Handle, FinalizedApplied = State(finalizedApplied.State), FinalizedAppliedRevision = finalizedApplied.Revision,
                EncodedDelta = Convert.ToHexString(CreatureView3ScalarDeltaCodec.Encode(plan, 1024, 16)), SourcePlacementX = create.PositionX,
                LifecycleRetirements = new[] { "contain", "end", "replace", "destroy", "reset", "registry-close", "store-close" },
                FinalCount = store.Snapshots().Count };
            File.WriteAllText(exportPath, JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        }
        Console.WriteLine("PASS: six-scalar store actual lifecycle, exact revision/caller token, immutable order/bits, foreign ownership, reseed, rollback/budgets/overflow and close");
    }
}
