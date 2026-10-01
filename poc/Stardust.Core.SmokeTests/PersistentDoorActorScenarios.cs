using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class PersistentDoorActorScenarios
{
    internal static void Run()
    {
        var bundle = InteriorDoorScenarios.Fixture(); CellFloorLocator initial = new(0, 0, new(-.8f, 0, 0));
        foreach (var pose in new[] { AppearanceTransform.Identity, new AppearanceTransform(new(0, 0, 1), Vector3.UnitY, new(-1, 0, 0), new(20, 4, -9)) })
        {
            using PortalDoorActorSimulation actor = new(bundle, initial, pose);
            PortalDoorActorState before = actor.State;
            HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(() => actor.Tick(float.NaN, Vector3.Zero, true));
            HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(() => actor.Tick(.1f, new(float.NaN, 0, 0), true));
            Require(actor.State == before, "Invalid input leaves actor/controller untouched");
            for (int i = 0; i < 20; i++) actor.Tick(.05f, pose.TransformDirection(new(.08f, 0, 0)), true);
            Require(actor.State.Locator.CellIndex == 1 && actor.State.TotalTransfers == 1 && actor.State.Door.RetainedActors == 1, "Actor crosses and contact survives new owner");
            for (int i = 0; i < 20; i++) actor.Tick(.05f, pose.TransformDirection(new(-.08f, 0, 0)), true);
            Require(actor.State.Locator.CellIndex == 0 && actor.State.TotalTransfers == 2, "Same leaf supports reverse direction");
            for (int trip = 0; trip < 30; trip++)
            {
                for (int i = 0; i < 20; i++) actor.Tick(.05f, pose.TransformDirection(new(.08f, 0, 0)), true);
                for (int i = 0; i < 20; i++) actor.Tick(.05f, pose.TransformDirection(new(-.08f, 0, 0)), true);
            }
            Require(actor.State.TotalTransfers == 62 && actor.State.Locator.CellIndex == 0 && actor.State.Door.RetainedActors == 1, "Repeated deterministic ownership without duplicate contacts");
            actor.Reset();Require(actor.State.Locator == before.Locator && actor.State.Frame == 0 && actor.State.TotalTransfers == 0 && actor.State.Door.RetainedActors == 0, "Reset actor locator and contact lifetime");
            for (int i = 0; i < 8; i++) actor.Tick(.05f, pose.TransformDirection(new(.1f, 0, 0)), false);
            Require(actor.State.Locator.CellIndex == 0 && actor.State.TotalTransfers == 0, "Denied actor stays source");
            Vector3 blocked = actor.State.WorldPosition;
            actor.Tick(.05f, pose.TransformDirection(new(-.1f, 0, 0)), false);
            Require(Vector3.Distance(actor.State.WorldPosition, blocked) > .09f, "Actor retreats from touching closed leaf");
            actor.Tick(.05f, Vector3.Zero, null);Require(actor.State.TotalTransfers == 0, "Unknown permission does not transfer");
            actor.Dispose();Require(actor.State.Door.RetainedActors == 0, "Dispose clears shared contact");
            HostAccountingScenarios.Reject<ObjectDisposedException>(() => actor.Tick(0, Vector3.Zero, true));
        }
        HostAccountingScenarios.Reject<InvalidDataException>(() => new SingleLeafDoorRuntime(bundle.Leaf.Selection.Style, bundle.Leaf.Extent, bundle.DoorToCell, 0, 0));
        HostAccountingScenarios.Reject<InvalidDataException>(() => new PortalDoorActorSimulation(bundle, initial with { CellIndex = 7 }));
        HostAccountingScenarios.Reject<InvalidDataException>(() => new PortalDoorActorSimulation(bundle, initial, actorHandle: 0));
        var reverse = bundle.Layout.Cells[1].Portals[0];
        var mismatch = bundle.Layout with { Cells = new[] { bundle.Layout.Cells[0], bundle.Layout.Cells[1] with { Portals = new[] { reverse with { DoorTransform = AppearanceTransform.Identity } } } } };
        HostAccountingScenarios.Reject<InvalidDataException>(() => new PortalDoorActorSimulation(bundle with { Layout = mismatch }, initial));
        var box = new PortalBarrierCollider(new(-1, 0, -.1f), new(1, 2, .1f), AppearanceTransform.Identity);
        Require(box.Sweep(new(0, 1, -.35f), new(0, 1, -.8f), new(.25f,.9f,.25f), false, true).Status == PortalBarrierContactStatus.Clear, "Separating face contact");
        Require(box.Sweep(new(0, 1, 0), new(0, 1, -.8f), new(.25f,.9f,.25f), false, true).Status == PortalBarrierContactStatus.InitialOverlap, "Genuine overlap remains explicit");
        Console.WriteLine("PASS: persistent shared-leaf actor ownership, 124 repeated transfers, retreat/reset/dispose and invalid pair/input gates");
    }
    private static void Require(bool value, string label) { if (!value) throw new Exception("Persistent actor: " + label); }
    internal static void ProbeOptional(string[] args,bool precise=false,bool stepPolicy=false,bool localRay=false,bool connected=false)
    {
        if (args.Length != 16 || args[2] != "--configuration" || args[4] != "--member" || args[6] != "--expected-sha256" || args[8] != "--cell" || args[10] != "--portal" || args[12] != "--table-sha256" || args[14] != "--leaf-sha256") throw new ArgumentException("Persistent door actor requires the explicit pinned doorway arguments.");
        using StreamReader cfg = File.OpenText(args[3]);using ConfiguredTreOverlay overlay = ConfiguredTreOverlay.Open(args[1], cfg);
        var bundle = PortalSingleLeafDoorBundleReader.Load(overlay.Lookup, args[5], args[7], int.Parse(args[9]), int.Parse(args[11]), args[13], args[15]);
        var approach = connected?bundle.ConnectedApproach(stepPolicy:new(UseLocalRayCoverage:localRay)):bundle.InspectionApproach();Vector3 direction = approach.End - approach.Start.LocalPosition;direction.Y = 0;direction = Vector3.Normalize(direction);
        foreach (var source in bundle.Sources.Values) Console.WriteLine("persistent_source=" + JsonSerializer.Serialize(source));
        foreach (bool transformed in new[] { false, true })
        {
            AppearanceTransform pose = transformed ? new(new(0, 0, 1), Vector3.UnitY, new(-1, 0, 0), new(30, 4, -11)) : AppearanceTransform.Identity;
            using PortalDoorActorSimulation actor = new(bundle, approach.Start, pose,usePreciseMovement:precise,stepPolicy:stepPolicy?new(UseLocalRayCoverage:localRay):null);
            int sequence = 0;
            void Step(string phase, float step, bool? allowed, bool reset = false)
            {
                if (reset) actor.Reset();
                Vector3 delta = pose.TransformDirection(direction * step);var state = actor.Tick(.05f, delta, allowed);
                Console.WriteLine("persistent_actor=" + JsonSerializer.Serialize(new { sequence = sequence++, phase, transformed, reset, seconds = .05f, delta = InteriorDoorScenarios.V(delta), allowed, frame = state.Frame, total = state.TotalTransfers, cell = state.Locator.CellIndex, triangle = state.Locator.TriangleIndex, local = InteriorDoorScenarios.V(state.Locator.LocalPosition), world = InteriorDoorScenarios.V(state.WorldPosition), fraction = state.Door.Controller.Fraction, curve = state.Door.DisplacementFraction, retained = state.Door.RetainedActors, transition = (int)state.Door.Controller.Transition, status = state.Movement!.Movement.Status.ToString(), kind = state.Movement.ContactKind?.ToString(), diagnostic = state.Movement.Diagnostic, transfers = state.Movement.Movement.Transfers, parameter = state.Movement.Movement.Parameter,steps=state.Movement.Steps }));
            }
            for (int trip = 0; trip < 10; trip++)
            {
                for (int i = 0; i < 20; i++) Step("forward", .08f, true);
                for (int i = 0; i < 20; i++) Step("reverse", -.08f, true);
            }
            Step("deny-reset", 0, false, true);
            for (int i = 0; i < 12; i++) Step("deny", .08f, false);
            for (int i = 0; i < 8; i++) Step("retreat", -.04f, false);
            for (int i = 0; i < 12; i++) Step("unknown", .04f, null);
            for (int i = 0; i < 20; i++) Step("recover", .08f, true);
            for (int i = 0; i < 20; i++) Step("return", -.08f, true);
            Step("final-reset", 0, true, true);
        }
    }
}
