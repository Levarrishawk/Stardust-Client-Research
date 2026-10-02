using Stardust.Core.Protocol;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;
public static class SceneObjectRegistryScenarios
{
    // Independently pinned R4 synthetic packets, already verified by its Python oracle.
    private const string CreateHex = "0500EADD89FE88776655443322F10000803E000000BF0000403F0000A03F000020C10000A8410000F4C1443322F1A5";
    private const string ContainHex = "04009EDECB5688776655443322F108070605040302FEFEFFFFFF";
    public static void Run()
    {
        byte[] input = Convert.FromHexString(CreateHex);
        var create = SceneObjectLifecycleCodec.DecodeCreate(input); Array.Fill(input, (byte)0);
        var containment = SceneObjectLifecycleCodec.DecodeContainment(Convert.FromHexString(ContainHex));
        var end = SceneObjectLifecycleCodec.DecodeEndBaselines(Convert.FromHexString("02003760432C88776655443322F1"));
        var destroy = SceneObjectLifecycleCodec.DecodeDestroy(Convert.FromHexString("030004D5454D88776655443322F1FF"));
        SceneObjectRegistry r = new(3, 17);
        var initial = r.Create(17, create)!; Check(r.IsCurrent(initial.Handle) && initial.Phase == SceneObjectPhase.PendingBaselines);
        Check(ReferenceEquals(r.Create(17, create), initial));
        SceneObjectRegistry foreignOwner = new(1, 17); var foreignHandle = foreignOwner.Create(17, create)!.Handle; Check(!r.IsCurrent(foreignHandle));
        Reject(() => r.Create(17, create with { PositionX = 9 })); Check(r.Find(create.ObjectId) == initial);
        Reject(() => r.Contain(17, containment)); Check(r.Find(create.ObjectId) == initial);
        var parent = r.Create(17, create with { ObjectId = containment.ContainerId })!;
        Check(r.Contain(17, containment)); var contained = r.Find(create.ObjectId)!;
        Check(contained.ContainerId == parent.Handle.ObjectId && contained.ArrangementBits == 0xFFFFFFFE && !r.IsCurrent(initial.Handle));
        Check(r.Contain(17, containment) && r.Find(create.ObjectId)!.Handle == contained.Handle);
        Reject(() => r.Contain(17, new(parent.Handle.ObjectId, create.ObjectId, 1)));
        Reject(() => r.Contain(17, new(create.ObjectId, create.ObjectId, 1)));
        Check(r.Find(create.ObjectId) == contained && r.Find(parent.Handle.ObjectId) == parent);
        Check(r.EndBaselines(17, end)); var final = r.Find(create.ObjectId)!;
        Check(final.Phase == SceneObjectPhase.Finalized && r.Count == 2 && !r.IsCurrent(contained.Handle));
        Check(r.EndBaselines(17, end) && r.Find(create.ObjectId)!.Handle == final.Handle);
        var oldSnapshots = r.Snapshots();
        Check(r.Destroy(17, new(parent.Handle.ObjectId, 0xA5)));
        var detached = r.Find(create.ObjectId)!;
        Check(detached.ContainerId == 0 && detached.ArrangementBits == contained.ArrangementBits && detached.Phase == SceneObjectPhase.Finalized && !r.IsCurrent(final.Handle));
        Check(oldSnapshots.Count == 2 && oldSnapshots.Any(v => v.Handle == final.Handle && v.ContainerId == parent.Handle.ObjectId));
        Check(!r.Destroy(17, new(parent.Handle.ObjectId, 255)));
        Check(r.Destroy(17, destroy) && r.Count == 0 && !r.IsCurrent(detached.Handle));
        Reject(() => r.EndBaselines(17, end)); Reject(() => r.Contain(17, containment));
        var recreated = r.Create(17, create)!; Check(recreated.Handle.Revision > detached.Handle.Revision);
        var replaced = r.Replace(17, create with { TemplateCrc = 0xFFFFFFFF })!;
        Check(!r.IsCurrent(recreated.Handle) && replaced.Phase == SceneObjectPhase.PendingBaselines && replaced.ContainerId == 0);
        Check(replaced.Create.QuaternionW == 1.25f && replaced.Create.PositionZ == 21f && replaced.Create.RawHyperspaceFlag == 0xA5);
        Reject(() => r.Replace(17, create with { ObjectId = 7 }));
        Check(r.Create(16, null!) is null && r.Replace(18, null!) is null && !r.Contain(16, null!) && !r.EndBaselines(16, null!) && !r.Destroy(18, null!));
        Check(r.Find(create.ObjectId) == replaced);
        r.Reset(18); Check(r.Count == 0 && !r.IsCurrent(replaced.Handle));
        var resetCreate = r.Create(18, create)!; Check(resetCreate.Handle.Revision > replaced.Handle.Revision);
        var snapshot = r.Snapshots(); r.Close(); r.Close();
        Check(r.IsClosed && r.Count == 0 && !r.IsCurrent(resetCreate.Handle) && snapshot.Count == 1 && snapshot[0] == resetCreate);
        Throws<InvalidOperationException>(() => r.Create(18, create)); Throws<InvalidOperationException>(() => r.Reset(19));
        Check(r.Create(17, create) is null);

        SceneObjectRegistry limited = new(1, 1); var one = limited.Create(1, create)!;
        Reject(() => limited.Create(1, create with { ObjectId = 2 })); Check(limited.Count == 1 && limited.Find(create.ObjectId) == one);
        Reject(() => limited.Create(1, create with { ObjectId = 0 }));
        Reject(() => limited.Replace(1, create with { QuaternionX = float.NaN })); Check(limited.Find(create.ObjectId) == one);
        Throws<ArgumentOutOfRangeException>(() => limited.Reset(1)); Check(limited.Find(create.ObjectId) == one);
        Reject(() => new SceneObjectRegistry(0, 1).Create(1, create));
        Throws<ArgumentOutOfRangeException>(() => new SceneObjectRegistry(-1, 1));
        // Negative zero differs at wire level: record numeric equality must not hide conflicting create.
        SceneObjectRegistry bits = new(1, 1); bits.Create(1, create with { QuaternionX = 0f });
        Reject(() => bits.Create(1, create with { QuaternionX = -0.0f }));
        SceneObjectRegistry sceneLimit = new(1, ulong.MaxValue); var maxScene = sceneLimit.Create(ulong.MaxValue, create)!;
        Throws<OverflowException>(() => sceneLimit.Reset()); Check(sceneLimit.IsCurrent(maxScene.Handle)); sceneLimit.Close(); Check(!sceneLimit.IsCurrent(maxScene.Handle));
        SceneObjectRegistry revisions = new(2, 1, ulong.MaxValue); Throws<OverflowException>(() => revisions.Create(1, create)); Check(revisions.Count == 0);
        // Multi-child detachment must roll back even if revision exhaustion occurs halfway.
        SceneObjectRegistry atomic = new(3, 1, ulong.MaxValue - 6);
        atomic.Create(1, create with { ObjectId = 3 }); atomic.Create(1, create with { ObjectId = 1 }); atomic.Create(1, create with { ObjectId = 2 });
        atomic.Contain(1, new(1, 3, 7)); atomic.Contain(1, new(2, 3, 8));
        var before = atomic.Snapshots();
        Throws<OverflowException>(() => atomic.Destroy(1, new(3, 0))); Check(before.SequenceEqual(atomic.Snapshots()));
        Throws<OverflowException>(() => atomic.Replace(1, create with { ObjectId = 3 })); Check(before.SequenceEqual(atomic.Snapshots()));
        atomic.EndBaselines(1, new(3));
        Throws<OverflowException>(() => atomic.EndBaselines(1, new(1))); Check(atomic.Find(1)!.Phase == SceneObjectPhase.PendingBaselines);
        // Replacement detaches only direct children in deterministic ID order; grandchildren remain attached.
        SceneObjectRegistry graph = new(4, 1);
        foreach (ulong id in new ulong[] { 3, 2, 1, 4 }) graph.Create(1, create with { ObjectId = id });
        graph.Contain(1, new(1, 3, 10)); graph.Contain(1, new(2, 3, 20)); graph.Contain(1, new(4, 1, 40));
        Reject(() => graph.Contain(1, new(3, 4, 0)));
        graph.Replace(1, create with { ObjectId = 3 });
        Check(graph.Find(1)!.ContainerId == 0 && graph.Find(2)!.ContainerId == 0 && graph.Find(4)!.ContainerId == 1);
        Check(graph.Find(1)!.Handle.Revision < graph.Find(2)!.Handle.Revision && graph.Find(2)!.Handle.Revision < graph.Find(3)!.Handle.Revision);
        Console.WriteLine("R5-NET-01 pinned DTO lifecycle, capacity, cycles, revisions, stale/ownership and exhaustion rollback scenarios passed.");
    }
    private static void Check(bool value) { if (!value) throw new Exception("Registry assertion failed."); }
    private static void Reject(Action action) => Throws<InvalidDataException>(action);
    private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception($"Expected {typeof(T).Name}"); }
}

