using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class PortalBarrierScenarios
{
    internal static void Run()
    {
        PortalDoorController door = new(2, 4);
        door.RequestOpen(0);
        Require(door.Advance(0, .5f) == new DoorControllerState(.25f, true, true, DoorTransitionEvent.OpenBegin), "Begin opening");
        Require(door.Advance(1, .5f).Fraction == .5f, "Previous-frame request");
        Require(door.Advance(2, 1).Fraction == .25f && !door.State.Opening, "Expired pulse closes");
        Require(door.Advance(3, 1) == new DoorControllerState(0, false, false, DoorTransitionEvent.CloseEnd), "Closed cleanup");
        door.RequestOpen(4); Require(door.Advance(4, 2).Transition == DoorTransitionEvent.OpenBegin && door.State.Fraction == 1, "Large delta reports first transition");
        door.RequestOpen(5); Require(door.Advance(5, 0).Transition == DoorTransitionEvent.None, "No duplicate endpoint event");
        Require(door.Advance(7, 1).Transition == DoorTransitionEvent.CloseBegin, "Begin closing");
        door.RequestOpen(8); Require(door.Advance(8, .5f).Transition == DoorTransitionEvent.OpenEnd, "Mid-close reversal reaches open endpoint");
        Require(!door.PermissionBarrierActive(true) && door.PermissionBarrierActive(false), "Permission barrier independent of leaf fraction");
        Require(door.CandidateBarrierActive(false) && !door.CandidateBarrierActive(true), "Candidate active/permission predicate");
        Require(!door.ModeBarrierActive(false, 1) && door.ModeBarrierActive(false, 0), "Raw collision mode distinction");
        door.Reset(); Require(door.ModeBarrierActive(true, 1) && !door.CandidateBarrierActive(false), "Inactive mode predicate/reset");
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(() => new PortalDoorController(0, 1));
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(() => door.Advance(0, float.NaN));
        door.Advance(0, 0); HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(() => door.Advance(0, 0));
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(() => door.RequestOpen(-1));

        Vector3[] polygon = { new(-1, 0, 0), new(1, 0, 0), new(1, 2, 0), new(-1, 2, 0) };
        PortalBarrierCollider collider = new(polygon, AppearanceTransform.Identity);
        Vector3 start = new(0, 1, -2), end = new(0, 1, 2), body = new(.25f, .5f, .25f);
        PortalBarrierContact contact = collider.Sweep(start, end, body, false);
        Require(contact.Status == PortalBarrierContactStatus.Blocked && Math.Abs(contact.Parameter - .425) < 1e-7 && Vector3.Distance(contact.Center, new(0, 1, -.3f)) < 1e-6 && contact.Normal == -Vector3.UnitZ, "Body stops before portal plane");
        Require(collider.Sweep(end, start, body, false).Normal == Vector3.UnitZ, "Reverse normal");
        Require(collider.Sweep(start, end, body, true).Status == PortalBarrierContactStatus.Clear, "Permission changes without rebuilding collider");
        Require(collider.Sweep(start, end, body, null).Status == PortalBarrierContactStatus.PermissionUnknown, "Unknown permission");
        Require(collider.Sweep(new(3, 1, -2), new(3, 1, 2), body, false).Status == PortalBarrierContactStatus.Clear, "Off-polygon box miss");
        Require(collider.Sweep(new(0, 4, -2), new(0, 4, 2), body, false).Status == PortalBarrierContactStatus.Clear, "Actor height miss");
        Require(collider.Sweep(new(0, 1, 0), end, body, false).Status == PortalBarrierContactStatus.InitialOverlap, "Initial overlap is explicit, no depenetration");
        Require(collider.Sweep(start, start, body, false).Status == PortalBarrierContactStatus.Clear, "Stationary outside");
        Require(collider.Sweep(start, new(0, 1, -.3f), body, false).Status == PortalBarrierContactStatus.Blocked, "Closed endpoint contact");
        AppearanceTransform pose = new(new(0, 0, 1), Vector3.UnitY, new(-1, 0, 0), new(7, 3, 11));
        PortalBarrierCollider rotated = new(polygon.Select(pose.TransformPoint).ToArray(), pose);
        contact = rotated.Sweep(pose.TransformPoint(start), pose.TransformPoint(end), body, false);
        Require(Math.Abs(contact.Parameter - .425) < 1e-6 && Vector3.Distance(contact.Normal, pose.TransformDirection(-Vector3.UnitZ)) < 1e-6, "Translated rotated actor contact");
        Array.Clear(polygon); Require(collider.Sweep(start, end, body, false).Status == PortalBarrierContactStatus.Blocked, "Prepared geometry ownership");
        HostAccountingScenarios.Reject<InvalidDataException>(() => collider.Sweep(start, end, new(-1, 0, 0), false));
        HostAccountingScenarios.Reject<InvalidDataException>(() => collider.Sweep(new(float.NaN, 0, 0), end, body, true));
        HostAccountingScenarios.Reject<InvalidDataException>(() => new PortalBarrierCollider(new[] { start, end, body }, pose with { RowX = pose.RowX * 2 }));

        PortalLayoutDescription basis = PortalLayoutDecoder.Decode(PortalLayoutScenarios.Fixture());
        PortalReference reference = basis.Cells[0].Portals[0];
        Require(!PortalDoorFlags.FromReference("0004", reference).CanCreateDoor, "Leading flag exclusion");
        Require(PortalDoorFlags.FromReference("0004", reference with { LeadingByte = 0, SolidByte = 2 }).CanCreateDoor, "Nonzero supported door byte");
        Require(!PortalDoorFlags.FromReference("0004", reference with { LeadingByte = 0, SolidByte = 0 }).CanCreateDoor, "No inversion on supported version");
        HostAccountingScenarios.Reject<InvalidDataException>(() => PortalDoorFlags.FromReference("0002", reference));

        FloorMeshDescription f = FloorMeshDecoder.Decode(FloorMeshScenarios.Fixture());
        FloorTriangle t = new(new[] { 0, 1, 2 }, 1, new[] { -1, -1, -1 }, Vector3.UnitY, new byte[3], 0, 0, new[] { -1, 0, -1 });
        FloorMeshDescription a = f with { Vertices = new Vector3[] { new(0, 0, 0), new(2, 0, 0), new(0, 0, 2) }, Triangles = new[] { t } };
        FloorMeshDescription b = f with { Vertices = new Vector3[] { new(2, 0, 0), new(2, 0, 2), new(0, 0, 2) }, Triangles = new[] { t with { PortalIds = new[] { -1, -1, 0 } } } };
        PortalGeometry gate = new(new Vector3[] { new(2, 0, 0), new(0, 0, 2), new(0, 2, 2), new(2, 2, 0) }, new uint[] { 0, 1, 2, 0, 2, 3 }, false);
        PortalLayoutDescription layout = basis with { Geometry = new[] { gate }, Cells = basis.Cells.Select(c => c with { Portals = c.Portals.Select(p => p with { LeadingByte = 0, SolidByte = 1 }).ToArray() }).ToArray() };
        PortalBarrierFloorWalker movement = new(layout, new Dictionary<int, FloorMeshDescription> { [0] = a, [1] = b });
        CellFloorLocator locator = new(0, 0, new(.25f, 0, .25f)); Vector3 desired = new(1.75f, 0, 1.75f); int calls = 0;
        PortalBarrierFloorResult result = movement.Walk(locator, desired, body, _ => { calls++; return new(false, true); });
        Require(result.Contact?.Status == PortalBarrierContactStatus.Blocked && result.Movement.Status == CellFloorWalkStatus.EntryDenied && result.Movement.Locator.CellIndex == 0 && result.Movement.WorldPosition.X < 1 && calls == 1, "Source contact before transfer, one policy snapshot");
        Require(movement.Walk(locator, desired, body, _ => new(true, true)).Movement.Transfers == 1, "Open permission resumes transfer");
        Require(movement.Walk(locator, new(.75f, 0, .75f), body, _ => new(false, true)).Contact is not null, "Body may contact barrier without foot crossing portal");
        result = movement.Walk(locator, desired + Vector3.UnitY, body, _ => new(true, true));
        Require(result.Diagnostic == "UnsupportedNonlevelBarrierSweep" && result.Movement.Locator == locator && result.Movement.Transfers == 0, "Unsupported motion retains source owner");
        Console.WriteLine("PASS: door frame pulses/events and permission predicates; padded barrier actor sweep, transformed bounds, current policy and pre-transfer owner retention");
    }
    private static void Require(bool value, string message) { if (!value) throw new Exception("Portal barrier: " + message); }

    internal static void ProbeOptional(string[] args)
    {
        if (args.Length != 8 || args[2] != "--configuration" || args[4] != "--members" || args[6] != "--expected-members-sha256") throw new ArgumentException("Barrier probe needs pinned manifest/configuration.");
        if (new FileInfo(args[5]).Length > 1_048_576) throw new InvalidDataException("Barrier manifest budget.");
        byte[] manifest = File.ReadAllBytes(args[5]);
        if (!Convert.ToHexString(SHA256.HashData(manifest)).Equals(args[7], StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Barrier manifest SHA mismatch.");
        string[] members = File.ReadAllLines(args[5]);
        if (members.Length is 0 or > 1000 || members.Distinct(StringComparer.Ordinal).Count() != members.Length) throw new InvalidDataException("Barrier manifest count/duplicates.");
        using StreamReader config = File.OpenText(args[3]); using ConfiguredTreOverlay overlay = ConfiguredTreOverlay.Open(args[1], config);
        float[] V(Vector3 v) => new[] { v.X, v.Y, v.Z };
        foreach (string member in members)
        {
            TreLookupResult source = overlay.Lookup(member); if (source.Kind != TreLookupKind.Present || source.Payload is null) throw new InvalidDataException("Barrier source missing/masked.");
            PortalLayoutDescription layout = PortalLayoutDecoder.Decode(source.Payload); List<object> rows = new();
            for (int cell = 0; cell < layout.Cells.Count; cell++) for (int reference = 0; reference < layout.Cells[cell].Portals.Count; reference++)
            {
                PortalReference r = layout.Cells[cell].Portals[reference]; PortalDoorFlags flags = PortalDoorFlags.FromReference(layout.Version, r);
                if (!flags.CanCreateDoor) { rows.Add(new { cell, reference, eligible = false }); continue; }
                PortalBarrierCollider box = PortalBarrierCollider.FromReference(layout, r); AppearanceTransform p = box.Pose;
                Vector3 center = (box.Minimum + box.Maximum) / 2, half = (box.Maximum - box.Minimum) / 2;
                int axis = half.X <= half.Y && half.X <= half.Z ? 0 : half.Y <= half.Z ? 1 : 2;
                Vector3 direction = axis == 0 ? Vector3.UnitX : axis == 1 ? Vector3.UnitY : Vector3.UnitZ;
                float h = axis == 0 ? half.X : axis == 1 ? half.Y : half.Z;
                Vector3 start = p.TransformPoint(center - direction * (h + 2)), end = p.TransformPoint(center + direction * (h + 2)), body = new(.25f);
                rows.Add(new { cell, reference, eligible = true, min = V(box.Minimum), max = V(box.Maximum), pose = new[] { V(p.RowX), V(p.RowY), V(p.RowZ), V(p.Translation) }, start = V(start), end = V(end), body = V(body), contacts = new bool?[] { false, true, null }.Select(allowed => { var hit = box.Sweep(start, end, body, allowed); return new { allowed, status = hit.Status.ToString(), parameter = hit.Parameter, center = V(hit.Center), normal = V(hit.Normal) }; }).ToArray() });
            }
            Console.WriteLine("barriers=" + JsonSerializer.Serialize(new { member, sha256 = Convert.ToHexString(SHA256.HashData(source.Payload)), rows }));
        }
    }
}
