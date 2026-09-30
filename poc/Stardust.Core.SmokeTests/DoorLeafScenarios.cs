using System.Numerics;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class DoorLeafScenarios
{
    private static readonly string[] Columns = "doorStyleName frameAppearance doorAppearance doorAppearance2 doorFlip2 doorMoveX doorMoveY doorMoveZ openTime closeTime springiness smoothness triggerRadius isForceField openBeginEffect openEndEffect closeBeginEffect closeEndEffect".Split(' ');
    internal static byte[] Fixture(bool duplicate = false, bool badType = false, bool nonfinite = false)
    {
        byte[] Payload(Action<BinaryWriter> action) { using MemoryStream s = new(); using BinaryWriter w = new(s); action(w); return s.ToArray(); }
        void S(BinaryWriter w, string value) { w.Write(Encoding.ASCII.GetBytes(value)); w.Write((byte)0); }
        byte[] cols = Payload(w => { w.Write(18); foreach (string c in Columns) S(w, c); });
        byte[] types = Payload(w => { foreach (string t in new[] { "s", "s", "s", "s", "b", "f", "f", "f", "f", "f", "f", "f", "f", "b", "s", "s", "s", "s" }) S(w, badType && t == "b" ? "i" : t); });
        byte[] rows = Payload(w => { w.Write(duplicate ? 2 : 1); for (int i = 0; i < (duplicate ? 2 : 1); i++) { S(w, "synthetic"); S(w, ""); S(w, "appearance/test.apt"); S(w, ""); w.Write(0); foreach (float f in new[] { -2f, 0, 0, nonfinite ? float.NaN : 1, 2, 1, 1, 2 }) w.Write(f); w.Write(0); foreach (string effect in new[] { "open-begin", "open-end", "close-begin", "close-end" }) S(w, effect); } });
        return MeshScenarios.Form("DTII", MeshScenarios.Form("0001", MeshScenarios.Chunk("COLS", cols), MeshScenarios.Chunk("TYPE", types), MeshScenarios.Chunk("ROWS", rows)));
    }
    internal static void Run()
    {
        byte[] fixture = Fixture(); DoorStyleTable table = DoorStyleTableDecoder.Decode(fixture); DoorStyle style = table.Resolve("synthetic").Style;
        Require(table.Rows.Count == 1 && style.Movement == new Vector3(-2, 0, 0) && style.CloseSeconds == 2 && !table.Resolve("synthetic").UsedFallback && table.Resolve("missing").UsedFallback, "Table values and explicit row-zero fallback");
        for (int n = 0; n < fixture.Length; n++) HostAccountingScenarios.Reject<InvalidDataException>(() => DoorStyleTableDecoder.Decode(fixture.AsSpan(0, n)));
        HostAccountingScenarios.Reject<InvalidDataException>(() => DoorStyleTableDecoder.Decode(Fixture(duplicate: true)));
        HostAccountingScenarios.Reject<InvalidDataException>(() => DoorStyleTableDecoder.Decode(Fixture(badType: true)));
        HostAccountingScenarios.Reject<InvalidDataException>(() => DoorStyleTableDecoder.Decode(Fixture(nonfinite: true)));
        HostAccountingScenarios.Reject<InvalidDataException>(() => DoorStyleTableDecoder.Decode(fixture.Concat(new byte[1]).ToArray()));
        HostAccountingScenarios.Reject<ArgumentException>(() => table.Resolve(""));
        AppearanceExtent box = new("EXBX", "0001", new(0, 1, 0), 2, new(-1, 0, -.1f), new(1, 2, .1f));
        using SingleLeafDoorRuntime door = new(style, box, AppearanceTransform.Identity, 7);
        Vector3 start = new(0, 1, -2), end = new(0, 1, 2), half = new(.25f, .5f, .25f);
        Require(door.SweepLeaf(start, end, half).Status == PortalBarrierContactStatus.Blocked, "Closed leaf clearance");
        Require(door.NotifyContact(1, 8, 8, new(0, 1, 0), true, 0) == DoorContactResult.WrongCell, "Owner filter");
        Require(door.NotifyContact(1, 7, 8, new(0, 1, 0), false, 0) == DoorContactResult.EntryDenied && door.State.RetainedActors == 0, "Denied normal leaf does not start");
        Require(door.NotifyContact(1, 7, 8, new(0, 1, 0), null, 0) == DoorContactResult.PermissionUnknown, "Unknown permission");
        Require(door.NotifyContact(1, 7, 8, new(5, 1, 0), true, 0) == DoorContactResult.OutsideTrigger, "Trigger sphere radius");
        Require(door.NotifyContact(1, 7, 8, new(0, 1, 0), true, 0) == DoorContactResult.Retained, "Contact starts normal controller");
        Require(door.NotifyContact(1, 7, 8, new(0, 1, 0), true, 0) == DoorContactResult.Duplicate, "Duplicate handle");
        DoorLeafState state = door.Advance(0, .25f, _ => Vector3.Zero, true);
        float curve = (float)((1 - Math.Cos(Math.PI * .25)) / 2);
        Require(Math.Abs(state.DisplacementFraction - curve) < 1e-7 && state.Effect == "open-begin" && state.LeafPose.Translation.X == -2 * curve, "Authored curve, motion and transition effect");
        Require(door.SweepLeaf(start, end, half).Status == PortalBarrierContactStatus.Blocked, "Partial opening remains blocked by actual bounds");
        state = door.Advance(1, .75f, _ => Vector3.Zero, true);
        Require(state.Controller.Fraction == 1 && state.Effect == "open-end" && door.SweepLeaf(start, end, half).Status == PortalBarrierContactStatus.Clear, "Fully displaced leaf clears actor");
        state = door.Advance(2, 0, _ => new(3, 0, 0), true);
        Require(state.RetainedActors == 0, "Equality at 1.5-radius retention boundary prunes");
        state = door.Advance(3, 1, _ => null, true); Require(state.Controller.Fraction == .5f && state.Effect == "close-begin", "Pruned contact permits closing");
        state = door.Advance(4, 1, _ => null, true); Require(state.Controller.Fraction == 0 && state.Effect == "close-end" && !state.Controller.Active, "Deterministic close cleanup");
        door.Reset();
        for (ulong i = 1; i <= 25; i++) Require(door.NotifyContact(i, 7, 7, new(0, 1, 0), true, 0) == DoorContactResult.Retained, "Retained contact capacity");
        Require(door.NotifyContact(26, 7, 7, new(0, 1, 0), true, 0) == DoorContactResult.Capacity, "Cap at 25");
        DoorLeafState before = door.State;
        HostAccountingScenarios.Reject<InvalidOperationException>(() => door.Advance(0, .1f, _ => throw new InvalidOperationException("Synthetic lookup failure"), true));
        Require(door.State == before, "Lookup failure leaves update uncommitted");
        door.Advance(0, .1f, _ => null, false); Require(door.State.RetainedActors == 0, "Destroyed handles released");
        door.Reset(); Require(door.State.Controller.Fraction == 0 && door.State.RetainedActors == 0, "Reset clears retained actors");
        door.NotifyContact(1, 7, 7, new(0, 1, 0), true, 0);
        state = door.Advance(0, 1, _ => new(2.5f, 0, 0), true);
        Require(state.RetainedActors == 1, "Contact retained outside trigger but inside prune radius");
        state = door.Advance(1, 0, _ => new(2.5f, 0, 0), false);
        state = door.Advance(2, 0, _ => new(2.5f, 0, 0), false);
        state = door.Advance(3, 2, _ => new(2.5f, 0, 0), false);
        Require(state.Controller.Fraction == 0 && state.RetainedActors == 1, "Revoked permission closes after pulse without deleting retained actor");
        DoorLeafState unchanged = door.State;
        HostAccountingScenarios.Reject<InvalidDataException>(() => door.Advance(4, .1f, _ => new(float.NaN, 0, 0), true));
        Require(door.State == unchanged, "Nonfinite callback leaves state unchanged");
        HostAccountingScenarios.Reject<InvalidDataException>(() => new SingleLeafDoorRuntime(style with { Movement = new(float.MaxValue, 0, 0) }, box, AppearanceTransform.Identity with { Translation = new(float.MaxValue, 0, 0) }, 0));
        door.Reset();
        HostAccountingScenarios.Reject<InvalidDataException>(() => new SingleLeafDoorRuntime(style with { ForceFieldRaw = 1 }, box, AppearanceTransform.Identity, 0));
        HostAccountingScenarios.Reject<InvalidDataException>(() => new SingleLeafDoorRuntime(style with { SecondaryAppearance = "appearance/second.apt" }, box, AppearanceTransform.Identity, 0));
        AppearanceTransform pose = new(new(0, 0, 1), Vector3.UnitY, new(-1, 0, 0), new(10, 4, 20));
        using SingleLeafDoorRuntime transformed = new(style, box, pose, 7);
        transformed.NotifyContact(1, 7, 7, pose.TransformPoint(new(0, 1, 0)), true, 0);
        state = transformed.Advance(0, 1, _ => pose.Translation, true);
        Require(state.LeafPose.Translation == pose.TransformPoint(new(-2, 0, 0)) && transformed.SweepLeaf(pose.TransformPoint(start), pose.TransformPoint(end), half).Status == PortalBarrierContactStatus.Clear, "Asymmetric door owner pose");
        door.Dispose(); Require(door.State.RetainedActors == 0, "Dispose releases actor state");
        HostAccountingScenarios.Reject<ObjectDisposedException>(() => door.Advance(0, 0, _ => null, true));
        Console.WriteLine("PASS: strict door DTII table and malformed bytes; contact-triggered single leaf, authored curve, bounds clearance, retained actor cap/pruning and cleanup");
    }
    private static void Require(bool value, string label) { if (!value) throw new Exception("Door leaf: " + label); }

    internal static object Row(DoorStyle s) => new { name = s.Name, frame = s.FrameAppearance, primary = s.PrimaryAppearance, secondary = s.SecondaryAppearance, flip = s.FlipSecondaryRaw, move = V(s.Movement), open = s.OpenSeconds, close = s.CloseSeconds, spring = s.Springiness, smooth = s.Smoothness, radius = s.TriggerRadius, force = s.ForceFieldRaw, effects = new[] { s.OpenBeginEffect, s.OpenEndEffect, s.CloseBeginEffect, s.CloseEndEffect } };
    private static float[] V(Vector3 v) => new[] { v.X, v.Y, v.Z };
    internal static void ProbeOptional(string[] args)
    {
        if (args.Length != 10 || args[2] != "--configuration" || args[4] != "--style" || args[6] != "--table-sha256" || args[8] != "--leaf-sha256") throw new ArgumentException("Door probe needs pinned table/leaf and configuration.");
        using StreamReader cfg = File.OpenText(args[3]); using ConfiguredTreOverlay overlay = ConfiguredTreOverlay.Open(args[1], cfg);
        TreLookupResult tableSource = overlay.Lookup(SingleLeafDoorAssetReader.TablePath);
        if (tableSource.Payload is null) throw new InvalidDataException("Door table missing.");
        DoorStyleTable table = DoorStyleTableDecoder.Decode(tableSource.Payload);
        SingleLeafDoorAsset asset = SingleLeafDoorAssetReader.Load(overlay.Lookup, args[5], args[7], args[9]);
        Console.WriteLine("door_table=" + JsonSerializer.Serialize(table.Rows.Select(Row)));
        Console.WriteLine("door_asset=" + JsonSerializer.Serialize(new { style = Row(asset.Selection.Style), fallback = asset.Selection.UsedFallback, sources = asset.Sources.Values, min = V(asset.Extent.Minimum!.Value), max = V(asset.Extent.Maximum!.Value) }));
        using SingleLeafDoorRuntime door = new(asset.Selection.Style, asset.Extent, AppearanceTransform.Identity, 0);
        door.NotifyContact(1, 0, 1, new(0, 1, 0), true, 0);
        for (int frame = 0; frame < 100; frame++)
        {
            DoorLeafState state = door.Advance(frame, .025f, _ => frame < 40 ? Vector3.Zero : null, true);
            PortalBarrierContact hit = door.SweepLeaf(new(0, 1, -2), new(0, 1, 2), new(.25f, .9f, .25f));
            Console.WriteLine("door_frame=" + JsonSerializer.Serialize(new { frame, fraction = state.Controller.Fraction, active = state.Controller.Active, opening = state.Controller.Opening, transition = (int)state.Controller.Transition, curve = state.DisplacementFraction, translation = V(state.LeafPose.Translation), effect = state.Effect, retained = state.RetainedActors, contact = new { status = hit.Status.ToString(), parameter = hit.Parameter, center = V(hit.Center) } }));
        }
    }
}
