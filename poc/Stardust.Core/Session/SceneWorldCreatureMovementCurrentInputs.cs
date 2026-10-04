using Stardust.Core.Assets;
using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

public enum SceneCreatureMovementConstructorScope { Invalid = 0, CallerVerifiedConcreteClientCreatureRawInputs = 1 }
public enum SceneCreatureMovementCurrentOrigin { Unknown, ConstructorLiteralOne, ConstructorAuthoredRaw, Received }
public enum SceneCreatureMovementCurrentEligibility { Unknown, NonFinite, PendingPhase, MissingTypedState, FiniteCurrent }
public sealed record SceneWorldCreatureMovementCurrentPolicy(
    SceneReceivedMovementInspectionPhase InspectionPhase = SceneReceivedMovementInspectionPhase.PendingOrFinalized,
    int MaximumOutputBytes = 1632, int MaximumSlots = 10);

public sealed class SceneCreatureMovementCurrentField
{
    internal SceneCreatureMovementCurrentField(ushort index, uint? bits, SceneCreatureMovementCurrentOrigin origin,
        AuthoredMovementInput? authored, SceneReceivedCreatureMovementField? received, bool finalized, bool typed)
    {
        Index = index; RawBits = bits; Origin = origin; AuthoredInput = authored; ReceivedInput = received;
        Eligibility = bits is null ? SceneCreatureMovementCurrentEligibility.Unknown :
            (bits.Value & 0x7F800000U) == 0x7F800000U ? SceneCreatureMovementCurrentEligibility.NonFinite :
            !finalized ? SceneCreatureMovementCurrentEligibility.PendingPhase :
            !typed ? SceneCreatureMovementCurrentEligibility.MissingTypedState : SceneCreatureMovementCurrentEligibility.FiniteCurrent;
    }
    public ushort Index { get; }
    public uint? RawBits { get; }
    public SceneCreatureMovementCurrentOrigin Origin { get; }
    public SceneCreatureMovementCurrentEligibility Eligibility { get; }
    public AuthoredMovementInput? AuthoredInput { get; }
    public SceneReceivedCreatureMovementField? ReceivedInput { get; }
    public uint? ConstructorLiteralBits => ConstructorLiteralStoreVA is null ? null : 0x3F800000;
    public uint? ConstructorLiteralStoreVA => Index switch
    { 0 => 0x0042F14C, 1 => 0x0042F122, 4 => 0x0042F093, 5 => 0x0042F071, 10 => 0x0042F176, _ => null };
}

/// <summary>Immutable raw starting inputs under an explicit concrete-constructor policy, not final movement.</summary>
public sealed class SceneWorldCreatureMovementCurrentReceipt
{
    internal SceneWorldCreatureMovementCurrentReceipt(object owner, long generation, SceneWorldObjectSnapshot target,
        SharedCreatureAuthoredMovementInputs authored, string pin, SceneWorldReceivedCreatureMovementReceipt? received,
        SceneCreatureMovementCurrentField[] fields, bool typed)
    { Owner = owner; Generation = generation; Target = target; AuthoredRoot = authored; RootSha256 = pin;
        Received = received; Fields = Array.AsReadOnly(fields); HasCurrentTypedState = typed; }
    internal object Owner { get; }
    public long Generation { get; }
    public SceneWorldObjectSnapshot Target { get; }
    public SceneObjectIncarnation Incarnation => Target.Object.Incarnation;
    public SceneObjectPhase Phase => Target.Object.Phase;
    public bool HasCurrentTypedState { get; }
    public bool IsOperationalContext => Phase == SceneObjectPhase.Finalized && HasCurrentTypedState;
    public SharedCreatureAuthoredMovementInputs AuthoredRoot { get; }
    public string RootSha256 { get; }
    public SceneCreatureMovementConstructorScope ConstructorScope => SceneCreatureMovementConstructorScope.CallerVerifiedConcreteClientCreatureRawInputs;
    public string ConstructorExecutableSha256 => "E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150";
    public SceneWorldReceivedCreatureMovementReceipt? Received { get; }
    public IReadOnlyList<SceneCreatureMovementCurrentField> Fields { get; }
    public int OutputBytes => SceneWorldCreatureMovementCurrentInputs.OutputBytes;
    public SceneCreatureMovementCurrentField Get(ushort index) => Fields[SceneWorldCreatureMovementCurrentInputs.Slot(index)];
}

/// <summary>Owner-thread derived inspection over a borrowed partial world and immutable caller-trusted authored root.</summary>
public sealed class SceneWorldCreatureMovementCurrentInputs : IDisposable
{
    public const int OutputBytes = 128 + 10 * 96 + SceneWorldReceivedCreatureMovementInputs.OutputBytes;
    private static readonly ushort[] indices = [0, 1, 4, 5, 7, 8, 9, 10, 11, 12];
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private readonly object identity = new();
    private readonly SceneWorldCreatureMovementCurrentPolicy policy;
    private readonly SceneObjectIncarnation incarnation;
    private readonly long generation;
    private readonly string pin;
    private SceneWorldCoordinator? world;
    private SharedCreatureAuthoredMovementInputs? authored;
    private SceneWorldReceivedCreatureMovementInputs? received;
    private bool busy, closed;

    public SceneWorldCreatureMovementCurrentInputs(SceneWorldCoordinator world, ulong objectId,
        SharedCreatureAuthoredMovementInputs authored, string expectedRootSha256,
        SceneCreatureMovementConstructorScope constructorScope, bool callerCatalogTrusted,
        SceneWorldCreatureMovementCurrentPolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(authored);
        ArgumentNullException.ThrowIfNull(expectedRootSha256); var limits = policy ?? new();
        if (constructorScope != SceneCreatureMovementConstructorScope.CallerVerifiedConcreteClientCreatureRawInputs || !callerCatalogTrusted)
            throw new ArgumentException("Explicit concrete constructor scope and caller catalog trust required.");
        if (limits.InspectionPhase is not (SceneReceivedMovementInspectionPhase.PendingOrFinalized or SceneReceivedMovementInspectionPhase.FinalizedOnly) ||
            limits.MaximumOutputBytes is < 0 or > 65536 || limits.MaximumSlots is < 0 or > 10) throw new ArgumentOutOfRangeException(nameof(policy));
        Reserve(limits); // Before borrowed world snapshots, received-reader construction, and any output candidates.
        if (!world.CreatureView4PartialEnabled) throw new ArgumentException("Partial received-only world required.", nameof(world));
        string path = LegacyAssetPath.Normalize(authored.RootPath);
        if (path != authored.RootPath || expectedRootSha256.Length != 64 || !expectedRootSha256.All(Uri.IsHexDigit) ||
            !authored.Sources.TryGetValue(path, out var source) || !string.Equals(source.Sha256, expectedRootSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Normalized authored root and independent SHA pin required.");
        _ = world.CaptureTangibleIngress(); // Frozen world owner/open guard; no private catalog access.
        var target = world.Snapshots().SingleOrDefault(s => s.Object.Handle.ObjectId == objectId);
        if (world.State is not (ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready) ||
            target is null || target.Object.Create.TemplateCrc != LegacyAssetPath.Checksum(path))
            throw new InvalidDataException("Current object and matching authored root CRC required.");
        this.world = world; this.authored = authored; this.policy = limits with { };
        incarnation = target.Object.Incarnation; generation = world.Generation; pin = expectedRootSha256.ToUpperInvariant();
        received = new(world);
    }
    private static void Reserve(SceneWorldCreatureMovementCurrentPolicy p)
    { if (p.MaximumSlots < 10 || p.MaximumOutputBytes < OutputBytes) throw new InvalidDataException("Current movement fixed output reservation exhausted."); }
    internal static int Slot(ushort index) => index switch
    { 0 => 0, 1 => 1, 4 => 2, 5 => 3, 7 => 4, 8 => 5, 9 => 6, 10 => 7, 11 => 8, 12 => 9,
        _ => throw new InvalidDataException("Not a current movement F32 slot.") };
    private void Enter()
    { if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Current movement owner thread required.");
        if (busy) throw new InvalidOperationException("Current movement reentry."); busy = true; }
    private SceneWorldObjectSnapshot? Current()
    {
        if (world is null || generation != world.Generation || world.State is not (ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready)) return null;
        var target = world.Snapshots().SingleOrDefault(s => s.Object.Incarnation == incarnation);
        return target is null || (policy.InspectionPhase == SceneReceivedMovementInspectionPhase.FinalizedOnly && target.Object.Phase != SceneObjectPhase.Finalized) ? null : target;
    }
    private bool SameContext(SceneWorldCreatureMovementCurrentReceipt r, SceneWorldObjectSnapshot target)
    {
        if (!ReferenceEquals(r.Target.Object, target.Object) || (r.Target.Replication is null) != (target.Replication is null) ||
            (r.Target.Replication is not null && !world!.IsCurrent(r.Target.Replication))) return false;
        if (r.Received is not null) return received!.IsCurrent(r.Received) && world!.IsCurrent(r.Received.Source);
        return world!.FindCreatureView4Partial(generation, incarnation.ObjectId) is null; // Late first publication stales absence-based inputs.
    }
    public SceneWorldCreatureMovementCurrentReceipt? Read(long requestedGeneration)
    {
        Enter(); try
        {
            ObjectDisposedException.ThrowIf(closed, this); Reserve(policy);
            if (requestedGeneration != generation) return null;
            var target = Current(); if (target is null) return null;
            var observations = received!.Read(generation, incarnation.ObjectId);
            bool typed = target.Replication is not null && world!.IsCurrent(target.Replication);
            var fields = new SceneCreatureMovementCurrentField[10];
            for (int i = 0; i < indices.Length; i++)
            {
                ushort index = indices[i]; AuthoredMovementInput? input = index switch
                { 7 => authored!.Speed0, 11 => authored!.Speed1, 8 => authored!.SlopeAngle, 9 => authored!.SlopePercent, 12 => authored!.WaterPercent, _ => null };
                var actual = observations?.Get(index); uint? bits; SceneCreatureMovementCurrentOrigin origin;
                if (actual?.RawBits is uint raw) { bits = raw; origin = SceneCreatureMovementCurrentOrigin.Received; }
                else if (input is null) { bits = 0x3F800000; origin = SceneCreatureMovementCurrentOrigin.ConstructorLiteralOne; }
                else if (input.Kind == SharedCreatureMovementInputKind.Literal && input.Bits is uint literal)
                { bits = literal; origin = SceneCreatureMovementCurrentOrigin.ConstructorAuthoredRaw; }
                else { bits = null; origin = SceneCreatureMovementCurrentOrigin.Unknown; }
                fields[i] = new(index, bits, origin, input, actual, target.Object.Phase == SceneObjectPhase.Finalized, typed);
            }
            var candidate = new SceneWorldCreatureMovementCurrentReceipt(identity, generation, target, authored!, pin, observations, fields, typed);
            var now = Current(); return now is not null && SameContext(candidate, now) ? candidate : null;
        } finally { busy = false; }
    }
    public bool IsCurrent(SceneWorldCreatureMovementCurrentReceipt receipt)
    {
        Enter(); try
        { ArgumentNullException.ThrowIfNull(receipt); if (closed || !ReferenceEquals(receipt.Owner, identity)) return false;
            var target = Current(); return target is not null && receipt.Generation == generation && SameContext(receipt, target); }
        finally { busy = false; }
    }
    public void Dispose()
    { Enter(); try { if (closed) return; received!.Dispose(); received = null; world = null; authored = null; closed = true; } finally { busy = false; } }
}
