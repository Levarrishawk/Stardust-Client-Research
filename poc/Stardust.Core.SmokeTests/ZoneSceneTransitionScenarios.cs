using Stardust.Core.Protocol;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;

public static class ZoneSceneTransitionScenarios
{
    private static readonly byte[] GoldenReady = Convert.FromHexString("0100221CFD43");
    private static CmdStartSceneMessage Scene(ulong id = 0xF122334455667788) => new(0xA5, id, "terrain/Raw\\Path.trn", 1.5f, -2.25f, 3.75f, "Object/Raw.iff", 0xFE02030405060708);
    public static void Run()
    {
        Check(SceneReadyProtocolCodec.Encode().SequenceEqual(GoldenReady));
        SceneReadyProtocolCodec.Decode(GoldenReady);
        for (int length = 0; length < 6; length++) Invalid(() => SceneReadyProtocolCodec.Decode(GoldenReady[..length]));
        Invalid(() => SceneReadyProtocolCodec.Decode([..GoldenReady, 0]));
        for (int offset = 0; offset < 6; offset++)
        { byte[] bad = (byte[])GoldenReady.Clone(); bad[offset] ^= 1; Invalid(() => SceneReadyProtocolCodec.Decode(bad)); }
        byte[] encoded = SceneReadyProtocolCodec.Encode(); encoded[0] = 0;
        Check(SceneReadyProtocolCodec.Encode().SequenceEqual(GoldenReady));
        Throws<ArgumentOutOfRangeException>(() => new ZoneSceneTransition(-1, 0));
        Throws<ArgumentOutOfRangeException>(() => new ZoneSceneTransition(0, -1));
        ZoneSceneTransition coordinator = new(2, 3);
        Check(coordinator.State == ZoneSceneTransitionState.Idle);
        long generation = coordinator.BeginSelection(Scene().ObjectId);
        Throws<InvalidOperationException>(() => coordinator.AcceptServerReady(generation, GoldenReady));
        Invalid(() => coordinator.AcceptStart(generation, Scene(7)));
        Check(coordinator.State == ZoneSceneTransitionState.Selecting && coordinator.Scene is null);
        Check(coordinator.AcceptStart(generation, Scene()));
        Throws<InvalidOperationException>(() => coordinator.AcceptStart(generation, Scene()));
        Throws<InvalidOperationException>(() => coordinator.AcceptServerReady(generation, GoldenReady));
        byte[] first = [1, 2]; Check(coordinator.Stage(generation, first)); first[0] = 9;
        Invalid(() => coordinator.Stage(generation, [3, 4]));
        Check(coordinator.StagedCount == 1 && coordinator.StagedBytes == 2);
        Check(coordinator.Stage(generation, [3]));
        Invalid(() => coordinator.Stage(generation, [])); // count cap even for zero-byte messages
        Check(coordinator.StagedCount == 2 && coordinator.StagedBytes == 3);
        ZoneScenePublication published = coordinator.CompleteMinimumResources(generation)!;
        Check(published.Generation == generation && published.CopyReadyMessage().SequenceEqual(GoldenReady));
        Check(published.CopyStagedPayloads().SelectMany(p => p).SequenceEqual(new byte[] { 1, 2, 3 }));
        byte[] mutatedReady = published.CopyReadyMessage(); mutatedReady[0] = 0;
        IReadOnlyList<byte[]> mutatedPayloads = published.CopyStagedPayloads(); mutatedPayloads[0][0] = 9;
        Check(published.CopyReadyMessage().SequenceEqual(GoldenReady) && published.CopyStagedPayloads()[0][0] == 1);
        Check(coordinator.StagedCount == 0 && coordinator.StagedBytes == 0);
        Check(coordinator.CompleteMinimumResources(generation) is null);
        Throws<InvalidOperationException>(() => coordinator.Stage(generation, [4]));
        Invalid(() => coordinator.AcceptServerReady(generation, [..GoldenReady, 0]));
        Check(coordinator.State == ZoneSceneTransitionState.AwaitingServerReady);
        Check(coordinator.AcceptServerReady(generation, GoldenReady));
        Check(!coordinator.AcceptServerReady(generation, GoldenReady));
        Check(coordinator.State == ZoneSceneTransitionState.Ready);

        // Explicit replacement invalidates old queued work and acknowledgement routing tokens.
        long old = coordinator.BeginSelection(7); coordinator.AcceptStart(old, Scene(7)); coordinator.Stage(old, [8]);
        long current = coordinator.BeginSelection(8);
        Check(current > old && coordinator.StagedCount == 0 && coordinator.Scene is null);
        Check(coordinator.CompleteMinimumResources(old) is null);
        Check(!coordinator.AcceptStart(old, Scene(7)) && !coordinator.Stage(old, [9]));
        Check(!coordinator.AcceptServerReady(old, GoldenReady) && !coordinator.Fail(old));
        Check(coordinator.State == ZoneSceneTransitionState.Selecting);
        coordinator.AcceptStart(current, Scene(8)); coordinator.Stage(current, [4]);
        Check(coordinator.Fail(current));
        Check(coordinator.State == ZoneSceneTransitionState.Failed && coordinator.StagedBytes == 0 && coordinator.Scene is null && coordinator.SelectedObjectId is null);
        Check(coordinator.CompleteMinimumResources(current) is null && !coordinator.Fail(current));
        Throws<InvalidOperationException>(() => coordinator.AcceptServerReady(current, GoldenReady));
        current = coordinator.BeginSelection(9); coordinator.AcceptStart(current, Scene(9)); coordinator.Stage(current, [5]);
        coordinator.Reset();
        Check(coordinator.State == ZoneSceneTransitionState.Idle && coordinator.Generation > current && coordinator.StagedCount == 0);
        Check(coordinator.CompleteMinimumResources(current) is null && !coordinator.AcceptServerReady(current, GoldenReady));
        current = coordinator.BeginSelection(10); coordinator.AcceptStart(current, Scene(10)); coordinator.Stage(current, [6]);
        coordinator.Close(); long closed = coordinator.Generation; coordinator.Close();
        Check(coordinator.Generation == closed && coordinator.State == ZoneSceneTransitionState.Closed && coordinator.StagedCount == 0 && coordinator.Scene is null);
        Check(coordinator.CompleteMinimumResources(current) is null && !coordinator.AcceptServerReady(current, GoldenReady));
        Throws<InvalidOperationException>(() => coordinator.BeginSelection(11));
        Throws<InvalidOperationException>(() => coordinator.Reset());
        // Already handed-off data belongs to caller and survives reset/close.
        Check(published.CopyStagedPayloads()[1][0] == 3);

        ZoneSceneTransition zero = new(0, 0); long zeroGeneration = zero.BeginSelection(0);
        Check(zero.AcceptStart(zeroGeneration, Scene(0)));
        Invalid(() => zero.Stage(zeroGeneration, []));
        Check(zero.CompleteMinimumResources(zeroGeneration)!.CopyStagedPayloads().Count == 0);
        ZoneSceneTransition byteBudget = new(5, 0); long b = byteBudget.BeginSelection(1); byteBudget.AcceptStart(b, Scene(1));
        Check(byteBudget.Stage(b, [])); Invalid(() => byteBudget.Stage(b, [1]));
        Check(byteBudget.StagedCount == 1 && byteBudget.StagedBytes == 0);
        byteBudget.Reset(); b = byteBudget.BeginSelection(1);
        Invalid(() => byteBudget.AcceptStart(b, Scene(1) with { X = float.NaN }));
        Invalid(() => byteBudget.AcceptStart(b, Scene(1) with { Terrain = "non-ASCII-\u0080" }));
        Invalid(() => byteBudget.AcceptStart(b, Scene(1) with { Template = new string('A', 65536) }));
        Throws<InvalidOperationException>(() => byteBudget.Stage(b, [1]));
        Check(byteBudget.CompleteMinimumResources(b) is null);
        Check(byteBudget.State == ZoneSceneTransitionState.Selecting);
        Console.WriteLine("R2-NET-01 codec, ordered barrier, budgets, generations, ownership and cleanup scenarios passed.");
    }
    private static void Check(bool condition) { if (!condition) throw new Exception("Scene transition assertion failed."); }
    private static void Invalid(Action action) => Throws<InvalidDataException>(action);
    private static void Throws<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new Exception($"Expected {typeof(T).Name}."); }
}

