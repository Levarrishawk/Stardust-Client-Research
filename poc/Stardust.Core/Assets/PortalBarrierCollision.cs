using System.Numerics;

namespace Stardust.Core.Assets;

public readonly record struct PortalDoorFlags(bool Excluded, bool SupportsDoor)
{
    // Supported PRTO/0001, /0003 and /0004 do not run the PRTO/0002 inversion pass.
    public static PortalDoorFlags FromReference(string layoutVersion, PortalReference reference)
    {
        if (layoutVersion is not ("0001" or "0003" or "0004")) throw new InvalidDataException("Unsupported runtime portal flag version.");
        return new(reference.LeadingByte.GetValueOrDefault() != 0, reference.SolidByte != 0);
    }
    public bool CanCreateDoor => !Excluded && SupportsDoor;
}

public enum PortalBarrierContactStatus { Clear, Blocked, PermissionUnknown, InitialOverlap }
public sealed record PortalBarrierContact(PortalBarrierContactStatus Status, double Parameter, Vector3 Center, Vector3 Normal);

/// <summary>Owned swept world-axis actor box against a rigid box: padded portal geometry or unpadded authored bounds. No sliding.</summary>
public sealed class PortalBarrierCollider
{
    public const float Padding = .05f;
    public AppearanceTransform Pose { get; }
    public Vector3 Minimum { get; }
    public Vector3 Maximum { get; }

    public PortalBarrierCollider(Vector3 minimum, Vector3 maximum, AppearanceTransform boxToOwner)
    {
        ValidatePose(boxToOwner);
        if (!Finite(minimum) || !Finite(maximum) || minimum.X > maximum.X || minimum.Y > maximum.Y || minimum.Z > maximum.Z) throw new InvalidDataException("Invalid authored collision box.");
        Pose = boxToOwner; Minimum = minimum; Maximum = maximum;
    }

    public PortalBarrierCollider(IReadOnlyList<Vector3> ownerVertices, AppearanceTransform portalToOwner)
    {
        ArgumentNullException.ThrowIfNull(ownerVertices);
        if (ownerVertices.Count is < 3 or > 100_000) throw new InvalidDataException("Portal barrier vertex budget.");
        ValidatePose(portalToOwner); Pose = portalToOwner;
        Vector3 min = new(float.PositiveInfinity), max = new(float.NegativeInfinity);
        foreach (Vector3 vertex in ownerVertices)
        {
            if (!Finite(vertex)) throw new InvalidDataException("Nonfinite barrier vertex.");
            Vector3 local = Local(vertex);
            if (!Finite(local)) throw new InvalidDataException("Nonfinite local barrier vertex.");
            min = Vector3.Min(min, local); max = Vector3.Max(max, local);
        }
        Minimum = min - new Vector3(Padding); Maximum = max + new Vector3(Padding);
        if (!Finite(Minimum) || !Finite(Maximum)) throw new InvalidDataException("Nonfinite barrier bounds.");
    }

    public static PortalBarrierCollider FromReference(PortalLayoutDescription layout, PortalReference reference)
    {
        PortalGeometry geometry = layout.Geometry[reference.GeometryIndex];
        AppearanceTransform pose;
        if (reference.TransformFlag.GetValueOrDefault() != 0 && reference.DoorTransform.HasValue) pose = reference.DoorTransform.Value;
        else
        {
            // Explicit modern frame derivation for untransformed inspection geometry.
            // The legacy fallback frame's full winding/vertical-surface behavior remains separate.
            Vector3 normal = Vector3.Zero;
            for (int i = 0; i + 2 < geometry.Indices.Count; i += 3)
            {
                normal = Vector3.Cross(geometry.Vertices[(int)geometry.Indices[i + 1]] - geometry.Vertices[(int)geometry.Indices[i]], geometry.Vertices[(int)geometry.Indices[i + 2]] - geometry.Vertices[(int)geometry.Indices[i]]);
                if (Finite(normal) && normal.LengthSquared() > 1e-12f) break;
            }
            if (!Finite(normal) || normal.LengthSquared() <= 1e-12f) throw new InvalidDataException("Degenerate barrier polygon.");
            Vector3 z = Vector3.Normalize(normal), helper = Math.Abs(z.Y) < .99f ? Vector3.UnitY : Vector3.UnitX;
            Vector3 x = Vector3.Normalize(Vector3.Cross(helper, z)), y = Vector3.Cross(z, x);
            pose = new(new(x.X, y.X, z.X), new(x.Y, y.Y, z.Y), new(x.Z, y.Z, z.Z), geometry.Vertices[0]);
        }
        return new(geometry.Vertices, pose);
    }

    public PortalBarrierContact Sweep(Vector3 startCenter, Vector3 endCenter, Vector3 worldHalfExtents, bool? passageAllowed, bool allowSeparatingContact = false)
    {
        if (!Finite(startCenter) || !Finite(endCenter) || !Finite(worldHalfExtents) || worldHalfExtents.X < 0 || worldHalfExtents.Y < 0 || worldHalfExtents.Z < 0)
            throw new InvalidDataException("Invalid barrier sweep bounds.");
        if (passageAllowed == true) return new(PortalBarrierContactStatus.Clear, 1, endCenter, Vector3.Zero);
        Vector3 start = Local(startCenter), end = Local(endCenter);
        Vector3 extent = new(
            Math.Abs(Pose.RowX.X) * worldHalfExtents.X + Math.Abs(Pose.RowY.X) * worldHalfExtents.Y + Math.Abs(Pose.RowZ.X) * worldHalfExtents.Z,
            Math.Abs(Pose.RowX.Y) * worldHalfExtents.X + Math.Abs(Pose.RowY.Y) * worldHalfExtents.Y + Math.Abs(Pose.RowZ.Y) * worldHalfExtents.Z,
            Math.Abs(Pose.RowX.Z) * worldHalfExtents.X + Math.Abs(Pose.RowY.Z) * worldHalfExtents.Y + Math.Abs(Pose.RowZ.Z) * worldHalfExtents.Z);
        Vector3 min = Minimum - extent, max = Maximum + extent;
        if (!Finite(start) || !Finite(end) || !Finite(min) || !Finite(max)) throw new InvalidDataException("Barrier sweep overflow.");
        bool inside = start.X >= min.X && start.X <= max.X && start.Y >= min.Y && start.Y <= max.Y && start.Z >= min.Z && start.Z <= max.Z;
        // Owned locomotion policy: a touching face may be left outward. Genuine interior overlap still stops.
        // Tolerance only recognizes a rounded contact face; it does not enlarge passage clearance.
        if (allowSeparatingContact)
        {
            for (int axis = 0; axis < 3; axis++)
            {
                float s = Component(start, axis), e = Component(end, axis), lo = Component(min, axis), hi = Component(max, axis);
                if (Math.Abs(s - lo) <= 1e-5f && e < s || Math.Abs(s - hi) <= 1e-5f && e > s)
                    return new(PortalBarrierContactStatus.Clear, 1, endCenter, Vector3.Zero);
            }
        }
        double enter = 0, exit = 1; Vector3 localNormal = Vector3.Zero;
        for (int axis = 0; axis < 3; axis++)
        {
            double s = Component(start, axis), delta = (double)Component(end, axis) - s;
            double lo = Component(min, axis), hi = Component(max, axis);
            if (delta == 0) { if (s < lo || s > hi) return new(PortalBarrierContactStatus.Clear, 1, endCenter, Vector3.Zero); continue; }
            double near = (lo - s) / delta, far = (hi - s) / delta;
            float sign = -1;
            if (near > far) { (near, far) = (far, near); sign = 1; }
            if (near > enter) { enter = near; localNormal = axis switch { 0 => new(sign, 0, 0), 1 => new(0, sign, 0), _ => new(0, 0, sign) }; }
            exit = Math.Min(exit, far);
            if (enter > exit) return new(PortalBarrierContactStatus.Clear, 1, endCenter, Vector3.Zero);
        }
        if (exit < 0 || enter > 1) return new(PortalBarrierContactStatus.Clear, 1, endCenter, Vector3.Zero);
        double parameter = Math.Clamp(enter, 0, 1);
        Vector3 center = new((float)(startCenter.X + ((double)endCenter.X - startCenter.X) * parameter), (float)(startCenter.Y + ((double)endCenter.Y - startCenter.Y) * parameter), (float)(startCenter.Z + ((double)endCenter.Z - startCenter.Z) * parameter));
        return new(passageAllowed is null ? PortalBarrierContactStatus.PermissionUnknown : inside ? PortalBarrierContactStatus.InitialOverlap : PortalBarrierContactStatus.Blocked,
            parameter, center, Pose.TransformDirection(localNormal));
    }

    private Vector3 Local(Vector3 point)
    {
        Vector3 v = point - Pose.Translation;
        return new(Pose.RowX.X * v.X + Pose.RowY.X * v.Y + Pose.RowZ.X * v.Z, Pose.RowX.Y * v.X + Pose.RowY.Y * v.Y + Pose.RowZ.Y * v.Z, Pose.RowX.Z * v.X + Pose.RowY.Z * v.Y + Pose.RowZ.Z * v.Z);
    }
    private static float Component(Vector3 v, int axis) => axis == 0 ? v.X : axis == 1 ? v.Y : v.Z;
    private static bool Finite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
    private static void ValidatePose(AppearanceTransform p)
    {
        if (!Finite(p.RowX) || !Finite(p.RowY) || !Finite(p.RowZ) || !Finite(p.Translation) ||
            Math.Abs(p.RowX.LengthSquared() - 1) > 1e-4 || Math.Abs(p.RowY.LengthSquared() - 1) > 1e-4 || Math.Abs(p.RowZ.LengthSquared() - 1) > 1e-4 ||
            Math.Abs(Vector3.Dot(p.RowX, p.RowY)) > 1e-4 || Math.Abs(Vector3.Dot(p.RowX, p.RowZ)) > 1e-4 || Math.Abs(Vector3.Dot(p.RowY, p.RowZ)) > 1e-4 ||
            Vector3.Dot(Vector3.Cross(p.RowX, p.RowY), p.RowZ) < .9999f) throw new InvalidDataException("Barrier pose must be finite, rigid and right-handed.");
    }
}
