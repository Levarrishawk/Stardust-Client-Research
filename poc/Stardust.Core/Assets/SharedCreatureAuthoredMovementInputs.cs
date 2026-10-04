using System.Collections.ObjectModel;
using System.Numerics;

namespace Stardust.Core.Assets;

public enum AuthoredMovementSlot { Speed0, Speed1, SlopeAngle, SlopePercent, WaterPercent }
public sealed record AuthoredMovementSnapshotLimits(int MaximumSources = 16, int MaximumEvidence = 80,
    int MaximumRawBytes = 1048576, long MaximumSnapshotBytes = 4194304, long MaximumWork = 4194304);
public sealed record AuthoredMovementSelectionLimits(int MaximumRequestedSlots = 5, int MaximumOutputBytes = 512, int MaximumWork = 5);
public sealed record AuthoredMovementEligibilityPolicy(string Name, bool RequireNonNegative = false, float MaximumAbsoluteValue = float.MaxValue)
{
    public static AuthoredMovementEligibilityPolicy FiniteLiteral { get; } = new("Finite authored literal");
}
public sealed class AuthoredMovementEvidence
{
    private readonly byte[]? raw;
    internal AuthoredMovementEvidence(SharedCreatureMovementVisit visit, SharedCreatureMovementParameter? parameter)
    {
        SourcePath = visit.SourcePath; Version = visit.Version; ParentPath = visit.ParentPath;
        Mode = parameter?.Mode; RelativeByte = parameter?.RelativeByte; LiteralBits = parameter?.LiteralBits;
        raw = parameter?.CopyEncoding();
    }
    public string SourcePath { get; }
    public string? Version { get; }
    public string? ParentPath { get; }
    public byte? Mode { get; }
    public byte? RelativeByte { get; }
    public uint? LiteralBits { get; }
    public byte[]? CopyEncoding() => raw?.ToArray();
}
public sealed record AuthoredMovementInput(AuthoredMovementSlot Slot, SharedCreatureMovementInputKind Kind,
    float? Value, uint? Bits, ResolvedTemplateField? Field, IReadOnlyList<AuthoredMovementEvidence> Evidence);
public sealed class AuthoredMovementSelection
{
    internal AuthoredMovementSelection(string policy, AuthoredMovementSlot[] requested, Dictionary<AuthoredMovementSlot, AuthoredMovementInput> inputs)
    { PolicyName = policy; Requested = Array.AsReadOnly(requested); Inputs = new ReadOnlyDictionary<AuthoredMovementSlot, AuthoredMovementInput>(inputs); }
    public string PolicyName { get; }
    public IReadOnlyList<AuthoredMovementSlot> Requested { get; }
    public IReadOnlyDictionary<AuthoredMovementSlot, AuthoredMovementInput> Inputs { get; }
}

/// <summary>Immutable authored-input consumer; selection never supplies replicated or runtime defaults.</summary>
public sealed class SharedCreatureAuthoredMovementInputs
{
    private static readonly string[] names = ["speed[0]", "speed[1]", "slopeModAngle", "slopeModPercent", "waterModPercent"];
    private readonly AuthoredMovementInput[] slots;
    private SharedCreatureAuthoredMovementInputs(SharedCreatureMovementInputProfile profile, AuthoredMovementInput[] slots, long bytes, long work)
    {
        this.slots = slots; RootPath = profile.RootPath; SnapshotBytes = bytes; Work = work;
        Sources = new ReadOnlyDictionary<string, AssetSourceIdentity>(profile.Sources.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal));
    }
    public string RootPath { get; }
    public IReadOnlyDictionary<string, AssetSourceIdentity> Sources { get; }
    public long SnapshotBytes { get; }
    public long Work { get; }
    public AuthoredMovementInput Speed0 => slots[0];
    public AuthoredMovementInput Speed1 => slots[1];
    public AuthoredMovementInput SlopeAngle => slots[2];
    public AuthoredMovementInput SlopePercent => slots[3];
    public AuthoredMovementInput WaterPercent => slots[4];
    public AuthoredMovementInput this[AuthoredMovementSlot slot] => Valid(slot) ? slots[(int)slot] : throw new ArgumentOutOfRangeException(nameof(slot));

    public static SharedCreatureAuthoredMovementInputs FromProfile(SharedCreatureMovementInputProfile profile,
        AuthoredMovementSnapshotLimits? limits = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile); limits ??= new();
        if (limits.MaximumSources is < 1 or > 16 || limits.MaximumEvidence is < 1 or > 80 ||
            limits.MaximumRawBytes is < 0 or > 1048576 || limits.MaximumSnapshotBytes is < 1 or > 4194304 || limits.MaximumWork is < 1 or > 4194304)
            throw new ArgumentOutOfRangeException(nameof(limits));
        cancellationToken.ThrowIfCancellationRequested(); long rawBytes = 0, evidenceCount = 0;
        foreach (string name in names)
        {
            var input = profile.Inputs[name];
            foreach (var visit in input.Provenance)
            { cancellationToken.ThrowIfCancellationRequested(); evidenceCount++; rawBytes += visit.Parameters[name]?.Bytes ?? 0; }
        }
        long bytes = checked(profile.Sources.Count * 64L + 5 * 64L + evidenceCount * 64 + rawBytes);
        long work = checked(5 + profile.Sources.Count + evidenceCount + rawBytes);
        if (profile.Sources.Count > limits.MaximumSources || evidenceCount > limits.MaximumEvidence || rawBytes > limits.MaximumRawBytes ||
            bytes > limits.MaximumSnapshotBytes || work > limits.MaximumWork)
            throw new AppearanceGraphBudgetException("Authored movement snapshot aggregate limit.");
        // All retained capacities and raw copies are reserved before any output allocation.
        var slots = new AuthoredMovementInput[5];
        for (int index = 0; index < 5; index++)
        {
            cancellationToken.ThrowIfCancellationRequested(); string name = names[index]; var input = profile.Inputs[name];
            var evidence = new AuthoredMovementEvidence[input.Provenance.Count];
            for (int at = 0; at < evidence.Length; at++)
            { cancellationToken.ThrowIfCancellationRequested(); var visit = input.Provenance[at]; evidence[at] = new(visit, visit.Parameters[name]); }
            slots[index] = new((AuthoredMovementSlot)index, input.Kind, input.Value,
                input.Value is float value ? BitConverter.SingleToUInt32Bits(value) : null, input.Field, Array.AsReadOnly(evidence));
        }
        cancellationToken.ThrowIfCancellationRequested();
        var result = new SharedCreatureAuthoredMovementInputs(profile, slots, bytes, work);
        cancellationToken.ThrowIfCancellationRequested(); return result;
    }

    public bool TrySelect(ReadOnlySpan<AuthoredMovementSlot> requested, AuthoredMovementEligibilityPolicy policy,
        out AuthoredMovementSelection? selection, AuthoredMovementSelectionLimits? limits = null)
    {
        selection = null; ArgumentNullException.ThrowIfNull(policy); limits ??= new();
        if (limits.MaximumRequestedSlots is < 1 or > 5 || limits.MaximumOutputBytes is < 1 or > 512 || limits.MaximumWork is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(limits));
        if (string.IsNullOrWhiteSpace(policy.Name) || policy.Name.Length > 128 || policy.Name.Any(c => c < 32 || c > 126) ||
            !float.IsFinite(policy.MaximumAbsoluteValue) || policy.MaximumAbsoluteValue < 0)
            throw new ArgumentException("Invalid named authored-input policy.", nameof(policy));
        if (requested.Length is < 1 or > 5) throw new ArgumentException("Explicit nonempty requested slots required.", nameof(requested));
        int mask = 0;
        foreach (var slot in requested)
        {
            if (!Valid(slot) || (mask & (1 << (int)slot)) != 0) throw new ArgumentException("Invalid or duplicate requested slot.", nameof(requested));
            mask |= 1 << (int)slot;
        }
        if (requested.Length > limits.MaximumRequestedSlots || requested.Length > limits.MaximumWork || 64 + requested.Length * 48 > limits.MaximumOutputBytes)
            throw new AppearanceGraphBudgetException("Authored movement selection aggregate limit.");
        foreach (var slot in requested)
        {
            var input = this[slot];
            if (input.Kind != SharedCreatureMovementInputKind.Literal || input.Value is not float value || !float.IsFinite(value) ||
                (policy.RequireNonNegative && value < 0) || MathF.Abs(value) > policy.MaximumAbsoluteValue) return false;
        }
        // Failure returns no operational prefix; only a fully eligible request allocates its result.
        var inputs = new Dictionary<AuthoredMovementSlot, AuthoredMovementInput>(requested.Length);
        foreach (var slot in requested) inputs.Add(slot, this[slot]);
        selection = new(policy.Name, requested.ToArray(), inputs); return true;
    }

    public bool TryPlanarVelocity(AuthoredMovementSlot speedSlot, Vector2 normalizedDirection, AuthoredMovementEligibilityPolicy policy,
        out Vector2? velocity, double unitSquaredTolerance = 0.000001)
    {
        velocity = null;
        if (speedSlot is not (AuthoredMovementSlot.Speed0 or AuthoredMovementSlot.Speed1)) throw new ArgumentException("Explicit speed0 or speed1 required.", nameof(speedSlot));
        if (!double.IsFinite(unitSquaredTolerance) || unitSquaredTolerance < 0 || unitSquaredTolerance > .001) throw new ArgumentOutOfRangeException(nameof(unitSquaredTolerance));
        if (!float.IsFinite(normalizedDirection.X) || !float.IsFinite(normalizedDirection.Y)) return false;
        double length = (double)normalizedDirection.X * normalizedDirection.X + (double)normalizedDirection.Y * normalizedDirection.Y;
        if (Math.Abs(length - 1) > unitSquaredTolerance) return false;
        Span<AuthoredMovementSlot> requested = stackalloc AuthoredMovementSlot[1]; requested[0] = speedSlot;
        if (!TrySelect(requested, policy, out var selected)) return false;
        float speed = selected!.Inputs[speedSlot].Value!.Value;
        if (speed < 0) return false;
        var value = normalizedDirection * speed; if (!float.IsFinite(value.X) || !float.IsFinite(value.Y)) return false;
        velocity = value; return true;
    }
    private static bool Valid(AuthoredMovementSlot slot) => slot is >= AuthoredMovementSlot.Speed0 and <= AuthoredMovementSlot.WaterPercent;
}
