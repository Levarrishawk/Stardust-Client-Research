using Stardust.Core.Protocol;
namespace Stardust.Core.Session;

public enum SceneReceivedMovementValueKind { Missing, Finite, NonFinite }
public enum SceneReceivedMovementEligibility { Missing, NonFinite, PendingPhase, FiniteReceived }
public enum SceneReceivedMovementInspectionPhase { PendingOrFinalized, FinalizedOnly }
public sealed record SceneWorldReceivedCreatureMovementPolicy(
    SceneReceivedMovementInspectionPhase InspectionPhase = SceneReceivedMovementInspectionPhase.PendingOrFinalized,
    int MaximumOutputBytes = 544);

/// <summary>One received scalar, including zero and nonfinite words; no numeric conversion or speed selection.</summary>
public sealed class SceneReceivedCreatureMovementField
{
    internal SceneReceivedCreatureMovementField(ushort index, uint? rawBits,
        CreatureView4PartialProvenance? provenance, SceneObjectPhase phase)
    {
        Index = index; RawBits = rawBits; Provenance = provenance;
        Kind = rawBits is null ? SceneReceivedMovementValueKind.Missing :
            (rawBits.Value & 0x7F800000U) == 0x7F800000U ? SceneReceivedMovementValueKind.NonFinite : SceneReceivedMovementValueKind.Finite;
        Eligibility = Kind == SceneReceivedMovementValueKind.Missing ? SceneReceivedMovementEligibility.Missing :
            Kind == SceneReceivedMovementValueKind.NonFinite ? SceneReceivedMovementEligibility.NonFinite :
            phase != SceneObjectPhase.Finalized ? SceneReceivedMovementEligibility.PendingPhase : SceneReceivedMovementEligibility.FiniteReceived;
    }
    public ushort Index { get; }
    public uint? RawBits { get; }
    public CreatureView4PartialProvenance? Provenance { get; }
    public SceneReceivedMovementValueKind Kind { get; }
    public SceneReceivedMovementEligibility Eligibility { get; }
}

/// <summary>Historical immutable observations; use consumer IsCurrent before treating them as current inputs.</summary>
public sealed class SceneWorldReceivedCreatureMovementReceipt
{
    internal SceneWorldReceivedCreatureMovementReceipt(object owner, SceneWorldCreatureView4PartialSnapshot source,
        SceneObjectSnapshot target, SceneReceivedCreatureMovementField[] fields)
    { Owner = owner; Source = source; Object = target; Fields = Array.AsReadOnly(fields); }
    internal object Owner { get; }
    public SceneWorldCreatureView4PartialSnapshot Source { get; }
    public SceneObjectSnapshot Object { get; }
    public long Generation => Source.Generation;
    public SceneObjectIncarnation Incarnation => Source.Incarnation;
    public ulong PublicationRevision => Source.Revision;
    public ulong StateRevision => Source.State.Revision;
    public SceneObjectPhase Phase => Object.Phase;
    public bool IsOperationalPhase => Phase == SceneObjectPhase.Finalized;
    public int DecodedBytes => SceneWorldReceivedCreatureMovementInputs.OutputBytes;
    public IReadOnlyList<SceneReceivedCreatureMovementField> Fields { get; }
    public SceneReceivedCreatureMovementField Get(ushort index) => Fields[SceneWorldReceivedCreatureMovementInputs.Slot(index)];
}

/// <summary>Serialized fixed-slot read over a borrowed opt-in partial world; no callbacks, defaults, or motion arithmetic.</summary>
public sealed class SceneWorldReceivedCreatureMovementInputs : IDisposable
{
    public const int OutputBytes = 64 + 10 * 48;
    private static readonly ushort[] indices = [0, 1, 4, 5, 7, 8, 9, 10, 11, 12];
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private readonly object identity = new();
    private readonly SceneWorldReceivedCreatureMovementPolicy policy;
    private SceneWorldCoordinator? world;
    private bool busy, closed;
    public SceneWorldReceivedCreatureMovementInputs(SceneWorldCoordinator world, SceneWorldReceivedCreatureMovementPolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(world); var limits = policy ?? new();
        if (limits.InspectionPhase is not (SceneReceivedMovementInspectionPhase.PendingOrFinalized or SceneReceivedMovementInspectionPhase.FinalizedOnly) ||
            limits.MaximumOutputBytes is < 0 or > 65536) throw new ArgumentOutOfRangeException(nameof(policy));
        if (!world.CreatureView4PartialEnabled) throw new ArgumentException("Received-only partial CREO4 world required.", nameof(world));
        _ = world.CaptureTangibleIngress(); // Validate borrowed world owner affinity/open state before retaining it.
        this.world = world; this.policy = limits with { };
    }
    internal static int Slot(ushort index) => index switch
    { 0 => 0, 1 => 1, 4 => 2, 5 => 3, 7 => 4, 8 => 5, 9 => 6, 10 => 7, 11 => 8, 12 => 9,
        _ => throw new InvalidDataException("Not a received movement F32 slot; listen6 and collections are excluded.") };
    private void Enter()
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Received movement consumer owner thread required.");
        if (busy) throw new InvalidOperationException("Received movement consumer reentry."); busy = true;
    }
    private void Open() => ObjectDisposedException.ThrowIf(closed, this);
    private SceneObjectSnapshot? CurrentObject(SceneWorldCreatureView4PartialSnapshot source)
    {
        if (world is null || !world.IsCurrent(source)) return null;
        var target = world.Snapshots().SingleOrDefault(o => o.Object.Incarnation == source.Incarnation)?.Object;
        if (target is null || (policy.InspectionPhase == SceneReceivedMovementInspectionPhase.FinalizedOnly && target.Phase != SceneObjectPhase.Finalized)) return null;
        return target;
    }
    private SceneWorldReceivedCreatureMovementReceipt? ReadCore(long generation, SceneWorldCreatureView4PartialSnapshot source)
    {
        if (generation != world!.Generation || source.Generation != generation) return null;
        var target = CurrentObject(source); if (target is null) return null;
        if (policy.MaximumOutputBytes < OutputBytes) throw new InvalidDataException("Received movement fixed output reservation exhausted.");
        // Full fixed reservation and exact source/phase checks precede all receipt/slot candidates.
        var fields = new SceneReceivedCreatureMovementField[indices.Length];
        for (int i = 0; i < indices.Length; i++)
        {
            source.State.TryGetField(indices[i], out var field);
            fields[i] = new(indices[i], field is null ? null : checked((uint)field.Bits), field?.Provenance, target.Phase);
        }
        if (!ReferenceEquals(CurrentObject(source), target)) return null;
        return new(identity, source, target, fields);
    }
    public SceneWorldReceivedCreatureMovementReceipt? Read(long generation, ulong objectId)
    {
        Enter(); try { Open(); var source = world!.FindCreatureView4Partial(generation, objectId); return source is null ? null : ReadCore(generation, source); }
        finally { busy = false; }
    }
    public SceneWorldReceivedCreatureMovementReceipt? Read(long generation, SceneWorldCreatureView4PartialSnapshot source)
    { Enter(); try { Open(); ArgumentNullException.ThrowIfNull(source); return ReadCore(generation, source); } finally { busy = false; } }
    public bool IsCurrent(SceneWorldReceivedCreatureMovementReceipt receipt)
    {
        Enter(); try { ArgumentNullException.ThrowIfNull(receipt); return !closed && ReferenceEquals(receipt.Owner, identity) &&
            receipt.Generation == world!.Generation && ReferenceEquals(CurrentObject(receipt.Source), receipt.Object); }
        finally { busy = false; }
    }
    public void Dispose() { Enter(); try { world = null; closed = true; } finally { busy = false; } }
}
