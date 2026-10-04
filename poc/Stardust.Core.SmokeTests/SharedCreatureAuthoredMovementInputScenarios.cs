using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class SharedCreatureAuthoredMovementInputScenarios
{
    private sealed record Resource(string Path, string Kind, string? Hex);
    private sealed record Fixture(string Name, string RootPath, string RootSha256, Resource[] Resources);
    private static readonly AuthoredMovementSlot[] All = Enum.GetValues<AuthoredMovementSlot>();
    private static readonly AuthoredMovementEligibilityPolicy Finite = AuthoredMovementEligibilityPolicy.FiniteLiteral;
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    internal static object Export(SharedCreatureAuthoredMovementInputs p) => new {
        p.RootPath, p.SnapshotBytes, p.Work, p.Sources,
        Inputs = All.Select(s => new { Slot = s.ToString(), Kind = p[s].Kind.ToString(), p[s].Bits, p[s].Field,
            Evidence = p[s].Evidence.Select(e => new { e.SourcePath, e.Version, e.ParentPath, e.Mode, e.RelativeByte, e.LiteralBits,
                Hex = e.CopyEncoding() is byte[] raw ? Convert.ToHexString(raw) : null }) }),
        Selections = new[] { All, new[] { AuthoredMovementSlot.Speed0 }, new[] { AuthoredMovementSlot.Speed1, AuthoredMovementSlot.SlopeAngle } }
            .Select(request => new { Requested = request.Select(s => s.ToString()), Eligible = p.TrySelect(request, Finite, out _),
                NonNegative = p.TrySelect(request, new("nonnegative", true), out _), Bounded = p.TrySelect(request, new("absolute two", false, 2), out _) })
    };
    internal static object Run(string path)
    {
        var fixtures = JsonSerializer.Deserialize<Fixture[]>(File.ReadAllBytes(path))!;
        var records = new List<object>(); int ownership = 0, boundaries = 0, checks = 0;
        void Reject(Action action, Type type) { try { action(); } catch (Exception e) when (type.IsInstanceOfType(e)) { checks++; return; } throw new Exception("Expected " + type.Name); }
        foreach (var f in fixtures)
        {
            var resources = f.Resources.ToDictionary(r => r.Path, r => new TreLookupResult(Enum.Parse<TreLookupKind>(r.Kind), r.Hex is null ? null : Convert.FromHexString(r.Hex), 77));
            var profile = SharedCreatureMovementInputProfile.Load(p => resources.GetValueOrDefault(p, new(TreLookupKind.Missing)), f.RootPath, f.RootSha256);
            var p = SharedCreatureAuthoredMovementInputs.FromProfile(profile); string before = JsonSerializer.Serialize(Export(p));
            foreach (var s in All) foreach (var e in p[s].Evidence) if (e.CopyEncoding() is byte[] raw) Array.Clear(raw);
            foreach (var r in resources.Values) if (r.Payload is byte[] raw) Array.Clear(raw);
            Check(before == JsonSerializer.Serialize(Export(p)), "Owned snapshot " + f.Name); ownership++;
            Check(SharedCreatureAuthoredMovementInputs.FromProfile(profile, new(MaximumSnapshotBytes: p.SnapshotBytes, MaximumWork: p.Work)).SnapshotBytes == p.SnapshotBytes, "Exact snapshot budget"); boundaries++;
            Reject(() => SharedCreatureAuthoredMovementInputs.FromProfile(profile, new(MaximumSnapshotBytes: p.SnapshotBytes - 1)), typeof(AppearanceGraphBudgetException)); boundaries++;
            Reject(() => SharedCreatureAuthoredMovementInputs.FromProfile(profile, new(MaximumWork: p.Work - 1)), typeof(AppearanceGraphBudgetException)); boundaries++;
            using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
            Reject(() => SharedCreatureAuthoredMovementInputs.FromProfile(profile, cancellationToken: cancelled.Token), typeof(OperationCanceledException));
            Reject(() => p.TrySelect([AuthoredMovementSlot.Speed0, AuthoredMovementSlot.Speed0], Finite, out _), typeof(ArgumentException));
            Reject(() => p.TrySelect([], Finite, out _), typeof(ArgumentException));
            Reject(() => p.TrySelect([(AuthoredMovementSlot)99], Finite, out _), typeof(ArgumentException));
            Reject(() => p.TrySelect(All, new("\n"), out _), typeof(ArgumentException));
            Reject(() => p.TrySelect(All, Finite, out _, new(MaximumOutputBytes: 303)), typeof(AppearanceGraphBudgetException));
            Reject(() => p.TrySelect(All, Finite, out _, new(MaximumWork: 4)), typeof(AppearanceGraphBudgetException));
            bool eligible = p.TrySelect(All, Finite, out var selection, new(MaximumOutputBytes: 304));
            Check(eligible == (selection is not null), "Atomic selection");
            if (selection is not null) Check(selection.Inputs.Count == 5 && selection.PolicyName == Finite.Name, "Complete named selection");
            foreach (var direction in new[] { Vector2.Zero, new Vector2(2, 0), new Vector2(float.NaN, 0), new Vector2(float.PositiveInfinity, 0) })
                Check(!p.TryPlanarVelocity(AuthoredMovementSlot.Speed0, direction, Finite, out var invalid) && invalid is null, "Direction guard");
            Reject(() => p.TryPlanarVelocity(AuthoredMovementSlot.SlopeAngle, Vector2.UnitX, Finite, out _), typeof(ArgumentException));
            Reject(() => p.TryPlanarVelocity(AuthoredMovementSlot.Speed0, Vector2.UnitX, Finite, out _, double.NaN), typeof(ArgumentOutOfRangeException));
            var velocities = new List<object>();
            foreach (var slot in new[] { AuthoredMovementSlot.Speed0, AuthoredMovementSlot.Speed1 })
                foreach (var direction in new[] { Vector2.UnitX, Vector2.UnitY, new Vector2(-1, 0), new Vector2(.6f, .8f), new Vector2(1.0000001f, 0) })
                {
                    bool ok = p.TryPlanarVelocity(slot, direction, Finite, out var v);
                    Check(ok == (v is not null), "Atomic planar output");
                    velocities.Add(new { Slot = slot.ToString(), XBits = BitConverter.SingleToUInt32Bits(direction.X), YBits = BitConverter.SingleToUInt32Bits(direction.Y), Eligible = ok,
                        OutputXBits = v is Vector2 value ? (uint?)BitConverter.SingleToUInt32Bits(value.X) : null,
                        OutputYBits = v is Vector2 value2 ? (uint?)BitConverter.SingleToUInt32Bits(value2.Y) : null });
                }
            records.Add(new { f.Name, Snapshot = Export(p), Velocities = velocities });
        }
        return new { Records = records, Ownership = ownership, Boundaries = boundaries, Rejections = checks };
    }
}
