using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class InputTimelineScenarios
{
    private static readonly (long Time, float Speed, bool? Allowed)[] Changes =
    {
        (137_000, 1.6f, true), (1_213_000, -1.6f, true), (2_419_000, 1.6f, false),
        (2_817_000, -1.6f, false), (3_007_000, 1.6f, true), (4_113_000, 0, null)
    };
    internal static void Run()
    {
        var bundle = InteriorDoorScenarios.Fixture(); CellFloorLocator start = new(0, 0, new(-.8f, 0, 0));
        PortalDoorActorState? expected = null;
        foreach (int[] partitions in new[] { new[] { 10_000 }, new[] { 50_000 }, new[] { 7_000, 11_000, 23_000, 41_000 } })
        {
            using var actor = new PortalDoorActorSimulation(bundle, start);
            var timeline = new PortalActorInputTimeline(new(actor), 0, new(Vector3.Zero, true));
            foreach (var change in Changes) timeline.Submit(new(change.Time, new(new(change.Speed, 0, 0), change.Allowed)));
            long now = 0; int render = 0;
            while (now < 4_400_000) timeline.AdvanceTo(now = Math.Min(4_400_000, now + partitions[render++ % partitions.Length]));
            Require(timeline.State.Actor.Frame == 220 && timeline.PendingTransitions == 0, "Between-render timeline accounting");
            expected ??= actor.State; Require(actor.State == expected, "Timestamped transition render invariance");
        }
        // Independent direct tick input selection, without the host or timeline.
        using var oracle = new PortalDoorActorSimulation(bundle, start);
        for (long startTime = 0; startTime < 4_400_000; startTime += 20_000)
        {
            var change = Changes.LastOrDefault(c => c.Time <= startTime);
            oracle.Tick(.02f, new(change.Speed * .02f, 0, 0), change.Time == 0 ? true : change.Allowed);
        }
        Require(oracle.State == expected, "Tick-start selection matches direct independently indexed input");
        using var gateActor = new PortalDoorActorSimulation(bundle, start);
        var gate = new PortalActorInputTimeline(new(gateActor), 0, new(Vector3.Zero, true));
        var moving = new PortalActorInput(new(1.6f, 0, 0), true);
        gate.Submit(new(10_000, moving)); gate.AdvanceTo(20_000);
        Require(gateActor.State.WorldPosition == start.LocalPosition, "Mid-tick event cannot backdate movement");
        gate.AdvanceTo(40_000); Require(gateActor.State.WorldPosition.X > start.LocalPosition.X, "Next tick sees event");
        var before = gate.State; int queued = gate.PendingTransitions;
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(() => gate.Submit(new(39_999, moving)));
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(() => gate.Submit(new(60_040_001, moving)));
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(() => gate.Submit(new(40_000, moving with { WorldVelocity = new(float.NaN, 0, 0) })));
        Require(gate.State == before && gate.PendingTransitions == queued, "Invalid submit is nonmutating");
        gate.Submit(new(50_000, moving)); gate.Submit(new(50_000, new(Vector3.Zero, false)));
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(() => gate.Submit(new(49_999, moving)));
        gate.AdvanceTo(80_000); Require(gate.CurrentInput == new PortalActorInput(Vector3.Zero, false), "Same-time latest transition wins");
        gate.Reset(0, new(Vector3.Zero, true)); gate.Submit(new(1_000_000, moving)); gate.AdvanceTo(2_000_007);
        Require(gate.State.Actor.Frame == 8 && gate.CurrentInput == moving && gate.PendingTransitions == 0, "Skipped event applies after dropped interval without replaying skipped movement");
        gate.AdvanceTo(2_020_000); Require(gate.State.Actor.Frame == 9 && gate.State.Actor.WorldPosition.X > start.LocalPosition.X, "Fresh tick adopts skipped interval latest input");
        gate.Reset(0, moving); gate.Submit(new(100_000, new(Vector3.Zero, false))); gate.AdvanceTo(200_000, false); gate.AdvanceTo(500_000);
        Require(gate.State.Actor.Frame == 0 && gate.CurrentInput.WorldVelocity == Vector3.Zero && gate.State.PendingMicroseconds == 0, "Suspended transition consumed without movement or resume catch-up");
        gate.Reset(0, moving);
        for (int i = 0; i < PortalActorInputTimeline.MaximumPendingTransitions; i++) gate.Submit(new(i, moving));
        HostAccountingScenarios.Reject<InvalidOperationException>(() => gate.Submit(new(1024, moving)));
        Require(gate.PendingTransitions == 1024, "Queue saturation bounded and nonmutating");
        gate.Reset(10, new(Vector3.Zero, true)); Require(gate.PendingTransitions == 0 && gate.State.Actor.Frame == 0, "Explicit reset clears timeline and actor epoch");
        Console.WriteLine("PASS: timestamped between-render inputs, tick-start quantization, nonlate ordering/capacity, skipped/pause history and reset");
    }
    private static void Require(bool value, string label) { if (!value) throw new Exception("Input timeline: " + label); }
    internal static void ProbeOptional(string[] args)
    {
        if (args.Length != 16 || args[2] != "--configuration" || args[4] != "--member" || args[6] != "--expected-sha256" || args[8] != "--cell" || args[10] != "--portal" || args[12] != "--table-sha256" || args[14] != "--leaf-sha256") throw new ArgumentException("Input timeline probe requires pinned doorway.");
        using StreamReader cfg = File.OpenText(args[3]); using ConfiguredTreOverlay overlay = ConfiguredTreOverlay.Open(args[1], cfg);
        var bundle = PortalSingleLeafDoorBundleReader.Load(overlay.Lookup, args[5], args[7], int.Parse(args[9]), int.Parse(args[11]), args[13], args[15]); var approach = bundle.InspectionApproach();
        Vector3 direction = approach.End - approach.Start.LocalPosition; direction.Y = 0; direction = Vector3.Normalize(direction);
        foreach (var source in bundle.Sources.Values) Console.WriteLine("timeline_source=" + JsonSerializer.Serialize(source));
        foreach (string pattern in new[] { "100hz", "20hz", "jitter" })
        foreach (bool transformed in new[] { false, true })
        {
            AppearanceTransform owner = transformed ? new(new(0, 0, 1), Vector3.UnitY, new(-1, 0, 0), new(30, 4, -11)) : AppearanceTransform.Identity;
            using var actor = new PortalDoorActorSimulation(bundle, approach.Start, owner); var timeline = new PortalActorInputTimeline(new(actor), 0, new(Vector3.Zero, true));
            foreach (var change in Changes) timeline.Submit(new(change.Time, new(owner.TransformDirection(direction * change.Speed), change.Allowed)));
            int[] partition = pattern == "100hz" ? new[] { 10_000 } : pattern == "20hz" ? new[] { 50_000 } : new[] { 7_000, 11_000, 23_000, 41_000 }; long now = 0; int render = 0;
            while (now < 4_400_000)
            {
                now = Math.Min(4_400_000, now + partition[render++ % partition.Length]); var s = timeline.AdvanceTo(now); var a = s.Actor;
                Console.WriteLine("timeline_update=" + JsonSerializer.Serialize(new { pattern, transformed, now, steps = s.StepsThisUpdate, pending = s.PendingMicroseconds, queued = timeline.PendingTransitions, frame = a.Frame, transfers = a.TotalTransfers, cell = a.Locator.CellIndex, local = InteriorDoorScenarios.V(a.Locator.LocalPosition), world = InteriorDoorScenarios.V(a.WorldPosition), fraction = a.Door.Controller.Fraction, curve = a.Door.DisplacementFraction, retained = a.Door.RetainedActors, status = a.Movement?.Movement.Status.ToString(), kind = a.Movement?.ContactKind?.ToString() }));
            }
        }
    }
}
