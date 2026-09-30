using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class FixedStepActorScenarios
{
    internal static void Run()
    {
        var bundle = InteriorDoorScenarios.Fixture();CellFloorLocator start = new(0, 0, new(-.8f, 0, 0));
        PortalDoorActorState? expected = null;
        foreach (int[] partitions in new[] { new[] { 10_000 }, new[] { 50_000 }, new[] { 7_000, 11_000, 23_000, 41_000 } })
        {
            using var actor = new PortalDoorActorSimulation(bundle, start);var host = new FixedStepPortalActorHost(actor);host.AdvanceTo(0, new(Vector3.Zero, true));long now = 0;int render = 0;
            foreach (var phase in new (long End, float Velocity, bool? Allow)[] { (End: 200_000L, Velocity: 1.6f, Allow: (bool?)true), (400_000L, 0f, (bool?)false), (800_000L, -1.6f, (bool?)true), (1_400_000L, 1.6f, (bool?)true) })
                while (now < phase.End)
                {
                    now = Math.Min(phase.End, now + partitions[render++ % partitions.Length]);host.AdvanceTo(now, new(new(phase.Velocity, 0, 0), phase.Allow));
                }
            Require(host.State.Actor.Frame == 70 && host.State.PendingMicroseconds == 0 && host.State.DroppedMicroseconds == 0 && host.State.FaultType is null, "Exact fixed tick accounting");
            expected ??= host.State.Actor;Require(host.State.Actor == expected, "Render partition invariant actor/controller/ownership");
        }
        using var clockActor = new PortalDoorActorSimulation(bundle, start);var clock = new FixedStepPortalActorHost(clockActor);var input = new PortalActorInput(new(1.6f, 0, 0), true);
        clock.AdvanceTo(0, input);clock.AdvanceTo(2_000_007, input);
        Require(clock.State.StepsThisUpdate == 8 && clock.State.Actor.Frame == 8 && clock.State.PendingMicroseconds == 7 && clock.State.DroppedMicroseconds == 1_840_000, "Bounded stall catch-up and whole tick discard");
        var before = clock.State;
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(() => clock.AdvanceTo(2_000_006, input));
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(() => clock.AdvanceTo(2_000_008, input with { WorldVelocity = new(float.NaN, 0, 0) }));
        Require(clock.State == before, "Invalid time/input leaves clock and actor unchanged");
        clock.Reset(0);clock.AdvanceTo(10_000, input);clock.AdvanceTo(20_000, input, false);clock.AdvanceTo(1_000_000, input, false);clock.AdvanceTo(1_020_000, input, true);
        Require(clock.State.Actor.Frame == 0 && clock.State.PendingMicroseconds == 0 && clock.State.SuspendedMicroseconds == 1_020_000, "Pause clears residual and resume discards suspended interval");
        clock.AdvanceTo(1_040_000, input);Require(clock.State.Actor.Frame == 1 && clock.State.StepsThisUpdate == 1, "Resume advances only fresh interval");
        clockActor.Dispose();clock.AdvanceTo(1_060_000, input);Require(clock.State.FaultType == nameof(ObjectDisposedException) && clock.State.StepsThisUpdate == 0, "Disposed borrowed actor faults once");
        clock.AdvanceTo(1_080_000, input);Require(clock.State.FaultType == nameof(ObjectDisposedException) && clock.State.Actor.Frame == 1, "Fault does not retry actor");
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(() => new FixedStepPortalActorHost(clockActor, 0));
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(() => new FixedStepPortalActorHost(clockActor, maximumStepsPerUpdate: 33));
        Console.WriteLine("PASS: fixed-step render partition invariance, input boundaries, stall accounting, pause/focus resume, invalid time/input and fault latch");
    }
    private static void Require(bool value, string label) { if (!value) throw new Exception("Fixed actor: " + label); }
    internal static void ProbeOptional(string[] args)
    {
        if (args.Length != 16 || args[2] != "--configuration" || args[4] != "--member" || args[6] != "--expected-sha256" || args[8] != "--cell" || args[10] != "--portal" || args[12] != "--table-sha256" || args[14] != "--leaf-sha256") throw new ArgumentException("Fixed actor probe requires pinned doorway.");
        using StreamReader cfg = File.OpenText(args[3]);using ConfiguredTreOverlay overlay = ConfiguredTreOverlay.Open(args[1], cfg);
        var bundle = PortalSingleLeafDoorBundleReader.Load(overlay.Lookup, args[5], args[7], int.Parse(args[9]), int.Parse(args[11]), args[13], args[15]);var approach = bundle.InspectionApproach();
        Vector3 direction = approach.End - approach.Start.LocalPosition;direction.Y = 0;direction = Vector3.Normalize(direction);
        foreach (var source in bundle.Sources.Values) Console.WriteLine("fixed_actor_source=" + JsonSerializer.Serialize(source));
        foreach (string pattern in new[] { "100hz", "20hz", "jitter" })
        foreach (bool transformed in new[] { false, true })
        {
            AppearanceTransform owner = transformed ? new(new(0, 0, 1), Vector3.UnitY, new(-1, 0, 0), new(30, 4, -11)) : AppearanceTransform.Identity;
            using var actor = new PortalDoorActorSimulation(bundle, approach.Start, owner);var host = new FixedStepPortalActorHost(actor);int[] partitions = pattern == "100hz" ? new[] { 10_000 } : pattern == "20hz" ? new[] { 50_000 } : new[] { 7_000, 11_000, 23_000, 41_000 };
            long now = 0;int render = 0;host.AdvanceTo(now, new(Vector3.Zero, true));
            foreach (var phase in new (string Name, long End, float Speed, bool? Allowed)[] { (Name: "forward", End: 1_200_000L, Speed: 1.6f, Allowed: (bool?)true), ("reverse", 2_400_000L, -1.6f, (bool?)true), ("denied", 2_800_000L, 1.6f, (bool?)false), ("retreat", 3_000_000L, -1.6f, (bool?)false), ("recover", 4_200_000L, 1.6f, (bool?)true) })
            {
                while (now < phase.End)
                {
                    now = Math.Min(phase.End, now + partitions[render++ % partitions.Length]);var state = host.AdvanceTo(now, new(owner.TransformDirection(direction * phase.Speed), phase.Allowed));
                    Console.WriteLine("fixed_actor_clock=" + JsonSerializer.Serialize(new { pattern, transformed, phase = phase.Name, now, steps = state.StepsThisUpdate, frame = state.Actor.Frame, pending = state.PendingMicroseconds, dropped = state.DroppedMicroseconds, suspended = state.SuspendedMicroseconds, fault = state.FaultType }));
                }
                var a = host.State.Actor;
                Console.WriteLine("fixed_actor_phase=" + JsonSerializer.Serialize(new { pattern, transformed, phase = phase.Name, now, frame = a.Frame, transfers = a.TotalTransfers, cell = a.Locator.CellIndex, local = InteriorDoorScenarios.V(a.Locator.LocalPosition), world = InteriorDoorScenarios.V(a.WorldPosition), fraction = a.Door.Controller.Fraction, curve = a.Door.DisplacementFraction, retained = a.Door.RetainedActors, status = a.Movement?.Movement.Status.ToString(), kind = a.Movement?.ContactKind?.ToString() }));
            }
        }
    }
}
