using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class InteriorDoorScenarios
{
    internal static PortalSingleLeafDoorBundle Fixture()
    {
        PortalLayoutDescription basis = PortalLayoutDecoder.Decode(PortalLayoutScenarios.Fixture());
        DoorStyle style = DoorStyleTableDecoder.Decode(DoorLeafScenarios.Fixture()).Resolve("synthetic").Style;
        AppearanceTransform doorPose = new(new(0, 0, 1), Vector3.UnitY, new(-1, 0, 0), Vector3.Zero);
        PortalGeometry geometry = new(new Vector3[] { new(0, 0, -1), new(0, 0, 1), new(0, 2, 1), new(0, 2, -1) }, new uint[] { 0, 1, 2, 0, 2, 3 }, true);
        PortalReference reference = basis.Cells[0].Portals[0] with { LeadingByte = 0, SolidByte = 1, GeometryIndex = 0, DoorName = "synthetic", TransformFlag = 1, DoorTransform = doorPose };
        PortalLayoutDescription layout = basis with { Geometry = new[] { geometry }, Cells = new[] { basis.Cells[0] with { Portals = new[] { reference with { TargetCell = 1 } } }, basis.Cells[1] with { Portals = new[] { reference with { TargetCell = 0 } } } } };
        FloorMeshDescription floor = FloorMeshDecoder.Decode(FloorMeshScenarios.Fixture());
        FloorTriangle T(int[] v, int[] n, int[] p) => new(v, 0, n, Vector3.UnitY, new byte[3], 0, 0, p);
        Dictionary<int, FloorMeshDescription> floors = new()
        {
            [0] = floor with { Vertices = new Vector3[] { new(-2, 0, -2), new(0, 0, -2), new(0, 0, 2), new(-2, 0, 2) }, Triangles = new[] { T(new[] { 0, 1, 2 }, new[] { -1, -1, 1 }, new[] { -1, 0, -1 }), T(new[] { 0, 2, 3 }, new[] { 0, -1, -1 }, new[] { -1, -1, -1 }) } },
            [1] = floor with { Vertices = new Vector3[] { new(0, 0, -2), new(2, 0, -2), new(2, 0, 2), new(0, 0, 2) }, Triangles = new[] { T(new[] { 0, 1, 2 }, new[] { -1, -1, 1 }, new[] { -1, -1, -1 }), T(new[] { 0, 2, 3 }, new[] { 0, -1, -1 }, new[] { -1, -1, 0 }) } }
        };
        AppearanceExtent box = new("EXBX", "0001", new(0, 1, 0), 2, new(-1, 0, -.1f), new(1, 2, .1f));
        var sources = new Dictionary<string, AssetSourceIdentity>();
        var graph = new AppearanceGeometryGraph(new("synthetic", null, null, null, Array.Empty<AppearanceGeometryChild>(), Array.Empty<string>()), sources);
        return new(layout, 0, 0, 1, new(new(style, false), graph, box, sources), doorPose, floors, sources);
    }
    internal static void Run()
    {
        var fixture = Fixture();
        var connected = fixture.ConnectedApproach();
        Require(Vector3.Distance(connected.Start.LocalPosition, new(-.8f, 0, 0)) < 1e-5 && Vector3.Distance(connected.End, new(.8f, 0, 0)) < 1e-5, "Connected approach follows paired edge normal");
        var stackedFloors = fixture.Floors.ToDictionary(p => p.Key, p => p.Value with
        {
            Vertices = p.Value.Vertices.Concat(p.Value.Vertices.Select(v => v + Vector3.UnitY * 2)).ToArray(),
            Triangles = p.Value.Triangles.Concat(p.Value.Triangles.Select(t => t with
            { VertexIndices = t.VertexIndices.Select(i => i + p.Value.Vertices.Count).ToArray(), Neighbors = new[] { -1, -1, -1 }, PortalIds = new[] { -1, -1, -1 } })).ToArray()
        });
        var stacked = fixture with { Floors = stackedFloors };
        Require(stacked.InspectionApproach().Start.LocalPosition.Y == 2 && stacked.ConnectedApproach().Start.LocalPosition.Y == 0 && stacked.ConnectedApproach().End.Y == 0, "Connected approach retains selected lower sheet under stacked floor");
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(() => fixture.ConnectedApproach(float.NaN));
        HostAccountingScenarios.Reject<InvalidDataException>(() => fixture.ConnectedApproach(3));
        var layout = fixture.Layout; var floors = fixture.Floors; var style = fixture.Leaf.Selection.Style; var box = fixture.Leaf.Extent; var doorPose = fixture.DoorToCell;
        foreach (AppearanceTransform owner in new[] { AppearanceTransform.Identity, new AppearanceTransform(new(0, 0, 1), Vector3.UnitY, new(-1, 0, 0), new(10, 3, 7)) })
        {
            using SingleLeafDoorRuntime leaf = new(style, box, owner.Compose(doorPose), 0);
            var poses = new Dictionary<int, AppearanceTransform> { [0] = owner, [1] = owner };
            PortalBarrierFloorWalker walker = new(layout, floors, poses, new Dictionary<(int, int), SingleLeafDoorRuntime> { [(0, 0)] = leaf });
            Vector3 start = new(-.8f, 0, 0), end = owner.TransformPoint(new(.8f, 0, 0)), half = new(.25f, .9f, .25f);
            CellFloorLocator locator = new(0, 0, start);
            PortalBarrierFloorResult closed = walker.Walk(locator, end, half, _ => new(true, true));
            Require(closed.ContactKind == PortalActorContactKind.DoorLeaf && closed.Movement.Status == CellFloorWalkStatus.PortalBlocked && closed.Movement.Transfers == 0 && closed.Movement.Locator.CellIndex == 0 && Math.Abs(closed.Movement.Parameter - .28125) < 1e-5, "Closed leaf retains source at actor contact");
            var overlap = walker.Walk(new(0, 0, new(-.1f, 0, 0)), end, half, _ => new(true, true));
            Require(overlap.Contact?.Status == PortalBarrierContactStatus.InitialOverlap && overlap.Movement.Parameter == 0 && overlap.Movement.Transfers == 0, "Initial leaf overlap retains source without depenetration");
            leaf.NotifyContact(1, 0, 1, owner.TransformPoint(start + new Vector3(0, .9f, 0)), true, 0);
            leaf.Advance(0, .25f, _ => owner.TransformPoint(start), true);
            Require(walker.Walk(locator, end, half, _ => new(true, true)).Movement.Status == CellFloorWalkStatus.PortalBlocked, "Partial displacement blocks");
            leaf.Advance(1, .75f, _ => owner.TransformPoint(start), true);
            PortalBarrierFloorResult open = walker.Walk(locator, end, half, _ => new(true, true));
            Require(open.Movement.Status == CellFloorWalkStatus.Accepted && open.Movement.Transfers == 1 && open.Movement.Locator.CellIndex == 1 && Vector3.Distance(open.Movement.WorldPosition, end) < 1e-5 && open.Contact is null, "Clear leaf permits owner transfer and remaining motion");
            int calls = 0;
            var denied = walker.Walk(locator, end, half, _ => { calls++; return new(false, true); });
            Require(denied.Movement.Status == CellFloorWalkStatus.EntryDenied && denied.ContactKind == PortalActorContactKind.PermissionBarrier && calls == 1, "Current denial sampled once per link");
            Require(walker.Walk(locator, end, half, _ => new(null, true)).Movement.Status == CellFloorWalkStatus.PermissionUnknown, "Unknown permission retains owner");
            var snapshot = walker.Walk(locator, end, half, _ => { leaf.Reset(); return new(true, true); });
            Require(snapshot.Movement.Status == CellFloorWalkStatus.Accepted && walker.Walk(locator, end, half, _ => new(true, true)).Movement.Status == CellFloorWalkStatus.PortalBlocked, "Leaf pose captured before callback; next query sees reset");
            HostAccountingScenarios.Reject<InvalidOperationException>(() => walker.Walk(locator, end, half, _ => throw new InvalidOperationException("Synthetic permission failure")));
            Require(leaf.State.Controller.Fraction == 0, "Failed query does not advance door");
            var nonlevel = walker.Walk(locator, end + Vector3.UnitY, half, _ => new(true, true));
            Require(nonlevel.Diagnostic == "UnsupportedNonlevelBarrierSweep" && nonlevel.Movement.Locator == locator, "Nonlevel retains source");
            Require(Vector3.Distance(nonlevel.Movement.WorldPosition, owner.TransformPoint(start)) < 1e-5, "Nonlevel stop reports world owner frame");
            HostAccountingScenarios.Reject<InvalidDataException>(() => new PortalBarrierFloorWalker(layout, floors, poses, new Dictionary<(int, int), SingleLeafDoorRuntime> { [(1, 0)] = leaf }));
            using SingleLeafDoorRuntime beyond = new(style, box, owner.Compose(doorPose with { Translation = new(.5f, 0, 0) }), 0);
            var shifted = new PortalBarrierFloorWalker(layout, floors, poses, new Dictionary<(int, int), SingleLeafDoorRuntime> { [(0, 0)] = beyond }).Walk(locator, end, half, _ => new(true, true));
            Require(shifted.Diagnostic == "UnsupportedSourceContactLanding" && shifted.Movement.Locator == locator && shifted.Movement.Transfers == 0, "Contact after boundary retains source without inventing a landing");
        }
        Require(new PortalBarrierFloorWalker(layout, floors, new Dictionary<int, AppearanceTransform> { [0] = AppearanceTransform.Identity }).Walk(new(0, 0, new(-.8f, 0, 0)), new(.8f, 0, 0), new(.25f,.9f,.25f), _ => new(true,true)).Movement.Status == CellFloorWalkStatus.Accepted, "Missing owner pose defaults to identity consistently");
        Console.WriteLine("PASS: integrated leaf contact/permission/owner transfer, current pose snapshots, transformed owners and failure retention");
    }
    private static void Require(bool value, string text) { if (!value) throw new Exception("Interior door: " + text); }
    internal static float[] V(Vector3 p) => new[] { p.X, p.Y, p.Z };
    internal static void ProbeOptional(string[] args)
    {
        if (args.Length != 16 || args[2] != "--configuration" || args[4] != "--member" || args[6] != "--expected-sha256" || args[8] != "--cell" || args[10] != "--portal" || args[12] != "--table-sha256" || args[14] != "--leaf-sha256") throw new ArgumentException("Interior door requires pinned layout/table/leaf and explicit reference.");
        using StreamReader cfg = File.OpenText(args[3]); using ConfiguredTreOverlay overlay = ConfiguredTreOverlay.Open(args[1], cfg);
        PortalSingleLeafDoorBundle bundle = PortalSingleLeafDoorBundleReader.Load(overlay.Lookup, args[5], args[7], int.Parse(args[9]), int.Parse(args[11]), args[13], args[15]);
        var approach = bundle.InspectionApproach(); Vector3 half = new(.25f, .9f, .25f);
        HostAccountingScenarios.Reject<InvalidDataException>(() => PortalSingleLeafDoorBundleReader.Load(overlay.Lookup, args[5], new string('0', 64), bundle.Cell, bundle.Portal, args[13], args[15]));
        HostAccountingScenarios.Reject<InvalidDataException>(() => PortalSingleLeafDoorBundleReader.Load(overlay.Lookup, args[5], args[7], -1, bundle.Portal, args[13], args[15]));
        HostAccountingScenarios.Reject<InvalidDataException>(() => PortalSingleLeafDoorBundleReader.Load(overlay.Lookup, args[5], args[7], bundle.Cell, bundle.Portal, args[13], new string('0', 64)));
        string floorPath = bundle.Layout.Cells[bundle.TargetCell].FloorPath!;
        TreLookupResult MissingFloor(string path) => path == floorPath ? overlay.Lookup("appearance/__synthetic_missing_floor__.flr") : overlay.Lookup(path);
        HostAccountingScenarios.Reject<InvalidDataException>(() => PortalSingleLeafDoorBundleReader.Load(MissingFloor, args[5], args[7], bundle.Cell, bundle.Portal, args[13], args[15]));
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(() => bundle.InspectionApproach(float.NaN));
        foreach (var source in bundle.Sources.Values) Console.WriteLine("interior_door_source=" + JsonSerializer.Serialize(source));
        foreach (bool transformed in new[] { false, true })
        {
            AppearanceTransform owner = transformed ? new(new(0, 0, 1), Vector3.UnitY, new(-1, 0, 0), new(30, 4, -11)) : AppearanceTransform.Identity;
            using SingleLeafDoorRuntime leaf = bundle.CreateRuntime(owner);
            var poses = bundle.Floors.Keys.ToDictionary(c => c, _ => owner);
            PortalBarrierFloorWalker walker = new(bundle.Layout, bundle.Floors, poses, new Dictionary<(int, int), SingleLeafDoorRuntime> { [(bundle.Cell, bundle.Portal)] = leaf });
            foreach (string stage in new[] { "closed", "partial", "open", "deny", "unknown", "reset" })
            {
                if (stage == "partial") { leaf.NotifyContact(1, bundle.Cell, bundle.TargetCell, owner.TransformPoint(approach.Start.LocalPosition + Vector3.UnitY * .9f), true, 0); leaf.Advance(0, .125f, _ => owner.TransformPoint(approach.Start.LocalPosition), true); }
                if (stage == "open") leaf.Advance(1, .375f, _ => owner.TransformPoint(approach.Start.LocalPosition), true);
                if (stage == "reset") leaf.Reset();
                var result = walker.Walk(approach.Start, owner.TransformPoint(approach.End), half, _ => new(stage == "unknown" ? null : stage != "deny", true));
                Console.WriteLine("interior_door=" + JsonSerializer.Serialize(new { stage, transformed, cell = bundle.Cell, portal = bundle.Portal, target = bundle.TargetCell, startTriangle = approach.Start.TriangleIndex, start = V(approach.Start.LocalPosition), end = V(approach.End), doorPose = new[] { V(bundle.DoorToCell.RowX), V(bundle.DoorToCell.RowY), V(bundle.DoorToCell.RowZ), V(bundle.DoorToCell.Translation) }, fraction = leaf.State.Controller.Fraction, curve = leaf.State.DisplacementFraction, status = result.Movement.Status.ToString(), owner = result.Movement.Locator.CellIndex, transfers = result.Movement.Transfers, parameter = result.Movement.Parameter, position = V(result.Movement.WorldPosition), localPosition = V(result.Movement.Locator.LocalPosition), kind = result.ContactKind?.ToString(), diagnostic = result.Diagnostic, contact = result.Contact is null ? null : new { parameter = result.Contact.Parameter, center = V(result.Contact.Center), normal = V(result.Contact.Normal) } }));
            }
        }
    }
}
