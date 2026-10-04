using System.Numerics;
using Stardust.Core.Assets;

namespace Stardust.Core.Session;

public enum SceneWorldMovementSpeedSource { Invalid = 0, AuthoredSpeed0 = 1, AuthoredSpeed1 = 2, ReceivedField7 = 3, ReceivedField11 = 4, CurrentField7 = 5, CurrentField11 = 6 }

/// <summary>Captured modern-policy input. Historical identity does not make an old source current.</summary>
public sealed class SceneWorldExplicitMovementReceipt
{
    internal SceneWorldExplicitMovementReceipt(object owner, SceneWorldMovementSpeedSource source,
        SceneWorldObjectSnapshot target, long timestamp, Vector2 direction, uint bits, Vector3 velocity,
        SharedCreatureAuthoredMovementInputs rootBinding, string rootSha256, AuthoredMovementSelection? authored,
        SceneWorldReceivedCreatureMovementReceipt? received, SceneWorldCreatureMovementCurrentReceipt? current = null)
    {
        Owner = owner; SpeedSource = source; Target = target; TimestampMicroseconds = timestamp;
        NativeXZDirection = direction; RawSpeedBits = bits; WorldVelocity = velocity;
        RootBinding = rootBinding; RootSha256 = rootSha256; AuthoredSelection = authored; ReceivedSource = received; CurrentSource = current;
    }
    internal object Owner { get; }
    public SceneWorldMovementSpeedSource SpeedSource { get; }
    public SceneWorldObjectSnapshot Target { get; }
    public SceneObjectIncarnation Incarnation => Target.Object.Incarnation;
    public long Generation => Target.Replication!.Generation;
    public long TimestampMicroseconds { get; }
    public Vector2 NativeXZDirection { get; }
    public uint RawSpeedBits { get; }
    public float Speed => BitConverter.UInt32BitsToSingle(RawSpeedBits);
    public Vector3 WorldVelocity { get; }
    public SharedCreatureAuthoredMovementInputs RootBinding { get; }
    public string RootSha256 { get; }
    public AuthoredMovementSelection? AuthoredSelection { get; }
    public SceneWorldReceivedCreatureMovementReceipt? ReceivedSource { get; }
    public SceneWorldCreatureMovementCurrentReceipt? CurrentSource { get; }
}

/// <summary>Explicit modern planar admission into an owned real local driver; no legacy speed arbitration.</summary>
public sealed class SceneWorldExplicitMovementAdmission : IDisposable
{
    private static readonly AuthoredMovementEligibilityPolicy eligibility = new("Explicit nonnegative modern planar speed", true);
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private readonly object identity = new();
    private readonly SceneWorldMovementSpeedSource speedSource;
    private readonly SceneObjectIncarnation incarnation;
    private readonly string rootSha256;
    private SceneWorldCoordinator? world;
    private SharedCreatureAuthoredMovementInputs? rootBinding;
    private SceneWorldReceivedCreatureMovementInputs? receivedReader;
    private SceneWorldCreatureMovementCurrentInputs? currentReader;
    private SceneWorldLocalMotionDriver? driver;
    private bool busy, closed;

    public SceneWorldExplicitMovementAdmission(SceneWorldCoordinator world, SceneWorldObjectMotionStore motions,
        ulong objectId, SceneWorldMovementSpeedSource speedSource, SharedCreatureAuthoredMovementInputs rootBinding,
        string expectedRootSha256, SceneWorldReceivedCreatureMovementInputs? receivedReader = null,
        long initialTimestampMicroseconds = 0, long stepMicroseconds = 16667, int maximumStepsPerUpdate = 8,
        SceneWorldCreatureMovementCurrentInputs? currentInputs = null)
    {
        ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(motions);
        ArgumentNullException.ThrowIfNull(rootBinding); ArgumentNullException.ThrowIfNull(expectedRootSha256);
        if (speedSource is < SceneWorldMovementSpeedSource.AuthoredSpeed0 or > SceneWorldMovementSpeedSource.CurrentField11)
            throw new ArgumentOutOfRangeException(nameof(speedSource));
        bool received = speedSource is SceneWorldMovementSpeedSource.ReceivedField7 or SceneWorldMovementSpeedSource.ReceivedField11;
        if (received && receivedReader is null) throw new ArgumentException("Explicit received reader required.", nameof(receivedReader));
        bool useCurrent = speedSource is SceneWorldMovementSpeedSource.CurrentField7 or SceneWorldMovementSpeedSource.CurrentField11;
        if (useCurrent && (currentInputs is null || receivedReader is not null))
            throw new ArgumentException("Current source requires its current reader without a conflicting direct received reader.");
        if (!useCurrent && currentInputs is not null) throw new ArgumentException("Unused current reader supplied.", nameof(currentInputs));
        if (expectedRootSha256.Length != 64 || !expectedRootSha256.All(Uri.IsHexDigit))
            throw new ArgumentException("Independent SHA-256 pin required.", nameof(expectedRootSha256));
        string path = LegacyAssetPath.Normalize(rootBinding.RootPath);
        if (path != rootBinding.RootPath || !rootBinding.Sources.TryGetValue(path, out var source) ||
            !string.Equals(source.Sha256, expectedRootSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Authored root identity does not match the independent pin.");
        var target = world.Snapshots().SingleOrDefault(s => s.Object.Handle.ObjectId == objectId);
        if (world.State is not (ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready) ||
            target is not { Object.Phase: SceneObjectPhase.Finalized, Replication: not null } ||
            !world.IsCurrent(target.Replication) || target.Object.Create.TemplateCrc != LegacyAssetPath.Checksum(path))
            throw new InvalidDataException("Current finalized typed CREO3 and matching root path CRC required.");
        if (useCurrent)
        {
            var initial = currentInputs!.Read(world.Generation);
            if (initial is null || !CurrentBinding(world, currentInputs, rootBinding, expectedRootSha256, target, initial))
                throw new InvalidDataException("Current reader requires the exact world/root/typed incarnation binding.");
        }
        // All adapter checks precede construction, which intentionally publishes the driver's initial local pose.
        var owned = new SceneWorldLocalMotionDriver(world, motions, objectId, initialTimestampMicroseconds, stepMicroseconds, maximumStepsPerUpdate);
        this.world = world; this.rootBinding = rootBinding; this.receivedReader = receivedReader;
        this.speedSource = speedSource; incarnation = target.Object.Incarnation; currentReader = currentInputs;
        rootSha256 = expectedRootSha256.ToUpperInvariant(); driver = owned;
    }
    private void Enter()
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Movement admission owner thread required.");
        if (busy) throw new InvalidOperationException("Movement admission reentry."); busy = true;
    }
    private void Open() => ObjectDisposedException.ThrowIf(closed, this);
    private SceneWorldObjectSnapshot? Current()
    {
        if (world!.State is not (ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready)) return null;
        var target = world.Snapshots().SingleOrDefault(s => s.Object.Incarnation == incarnation);
        return target is { Object.Phase: SceneObjectPhase.Finalized, Replication: not null } && world.IsCurrent(target.Replication) ? target : null;
    }
    private bool CurrentReceived(SceneWorldReceivedCreatureMovementReceipt receipt, SceneWorldObjectSnapshot target) =>
        receivedReader!.IsCurrent(receipt) && world!.IsCurrent(receipt.Source) && receipt.Generation == world.Generation &&
        receipt.Incarnation == incarnation && ReferenceEquals(receipt.Object, target.Object) && receipt.IsOperationalPhase;

    private static bool CurrentBinding(SceneWorldCoordinator world, SceneWorldCreatureMovementCurrentInputs reader,
        SharedCreatureAuthoredMovementInputs root, string pin, SceneWorldObjectSnapshot target,
        SceneWorldCreatureMovementCurrentReceipt source) =>
        ReferenceEquals(source.AuthoredRoot, root) && string.Equals(source.RootSha256, pin, StringComparison.OrdinalIgnoreCase) &&
        source.Generation == world.Generation && source.Incarnation == target.Object.Incarnation && source.IsOperationalContext &&
        ReferenceEquals(source.Target.Object, target.Object) && source.Target.Replication is not null &&
        world.IsCurrent(source.Target.Replication) && reader.IsCurrent(source);

    public SceneWorldMovementSpeedSource SpeedSource => speedSource;

    /// <summary>Explicit zero velocity, without sampling or attributing a speed source.</summary>
    public bool SubmitStop(long timestampMicroseconds)
    {
        Enter(); try
        {
            Open(); if (Current() is null) return false;
            driver!.Submit(timestampMicroseconds, new(Vector3.Zero, null));
            return true;
        } finally { busy = false; }
    }

    public SceneWorldExplicitMovementReceipt? Submit(long timestampMicroseconds, Vector2 normalizedNativeXZ,
        SceneWorldReceivedCreatureMovementReceipt? received = null)
    {
        Enter(); try
        {
            Open(); var target = Current(); if (target is null) return null;
            if (!float.IsFinite(normalizedNativeXZ.X) || !float.IsFinite(normalizedNativeXZ.Y)) return null;
            double squared = (double)normalizedNativeXZ.X * normalizedNativeXZ.X + (double)normalizedNativeXZ.Y * normalizedNativeXZ.Y;
            if (Math.Abs(squared - 1) > .000001) return null;
            AuthoredMovementSelection? authored = null; SceneWorldCreatureMovementCurrentReceipt? currentSource = null; uint bits;
            if (speedSource is SceneWorldMovementSpeedSource.AuthoredSpeed0 or SceneWorldMovementSpeedSource.AuthoredSpeed1)
            {
                if (received is not null) throw new ArgumentException("Received receipt supplied for an authored-only source.", nameof(received));
                var slot = speedSource == SceneWorldMovementSpeedSource.AuthoredSpeed0 ? AuthoredMovementSlot.Speed0 : AuthoredMovementSlot.Speed1;
                if (!rootBinding!.TrySelect([slot], eligibility, out authored)) return null;
                bits = authored!.Inputs[slot].Bits!.Value;
            }
            else if (speedSource is SceneWorldMovementSpeedSource.ReceivedField7 or SceneWorldMovementSpeedSource.ReceivedField11)
            {
                if (received is null || !CurrentReceived(received, target)) return null;
                ushort index = speedSource == SceneWorldMovementSpeedSource.ReceivedField7 ? (ushort)7 : (ushort)11;
                var field = received.Get(index);
                if (field.Eligibility != SceneReceivedMovementEligibility.FiniteReceived || field.RawBits is not uint raw) return null;
                bits = raw;
            }
            else
            {
                if (received is not null) throw new ArgumentException("Direct received receipt conflicts with a current source.", nameof(received));
                currentSource = currentReader!.Read(world!.Generation);
                if (currentSource is null || !CurrentBinding(world, currentReader, rootBinding!, rootSha256, target, currentSource)) return null;
                ushort index = speedSource == SceneWorldMovementSpeedSource.CurrentField7 ? (ushort)7 : (ushort)11;
                var field = currentSource.Get(index);
                if (field.Eligibility != SceneCreatureMovementCurrentEligibility.FiniteCurrent || field.RawBits is not uint raw) return null;
                bits = raw;
            }
            float speed = BitConverter.UInt32BitsToSingle(bits);
            if (!float.IsFinite(speed) || speed < 0) return null;
            Vector2 planar = normalizedNativeXZ * speed;
            if (!float.IsFinite(planar.X) || !float.IsFinite(planar.Y) || MathF.Abs(planar.X) > 10 || MathF.Abs(planar.Y) > 10) return null;
            Vector3 velocity = new(planar.X, 0, planar.Y);
            var candidate = new SceneWorldExplicitMovementReceipt(identity, speedSource, target, timestampMicroseconds,
                normalizedNativeXZ, bits, velocity, rootBinding!, rootSha256, authored, received, currentSource);
            var current = Current();
            if (current is null || !ReferenceEquals(current.Object, target.Object) ||
                !world!.IsCurrent(target.Replication!) || (received is not null && !CurrentReceived(received, current)) ||
                (currentSource is not null && !CurrentBinding(world, currentReader!, rootBinding!, rootSha256, current, currentSource))) return null;
            driver!.Submit(timestampMicroseconds, new(velocity, null)); // Existing atomic time/order/capacity admission; exactly one transition.
            return candidate;
        } finally { busy = false; }
    }
    public bool IsSourceCurrent(SceneWorldExplicitMovementReceipt receipt)
    {
        Enter(); try
        {
            ArgumentNullException.ThrowIfNull(receipt);
            if (closed || !ReferenceEquals(receipt.Owner, identity)) return false;
            var target = Current();
            return target is not null && ReferenceEquals(target.Object, receipt.Target.Object) &&
                world!.IsCurrent(receipt.Target.Replication!) &&
                (receipt.ReceivedSource is null || CurrentReceived(receipt.ReceivedSource, target)) &&
                (receipt.CurrentSource is null || CurrentBinding(world, currentReader!, rootBinding!, rootSha256, target, receipt.CurrentSource));
        } finally { busy = false; }
    }
    public SceneWorldLocalMotionDriverState State { get { Enter(); try { Open(); return driver!.State; } finally { busy = false; } } }
    public SceneWorldLocalMotionDriverState Advance(long timestampMicroseconds, bool active = true)
    { Enter(); try { Open(); return driver!.Advance(timestampMicroseconds, active); } finally { busy = false; } }
    public void Reset(long timestampMicroseconds)
    { Enter(); try { Open(); driver!.Reset(timestampMicroseconds); } finally { busy = false; } }
    public void Dispose()
    {
        Enter(); try
        {
            if (closed) return; driver!.Dispose(); closed = true;
            driver = null; world = null; rootBinding = null; receivedReader = null; currentReader = null;
        } finally { busy = false; }
    }
}
