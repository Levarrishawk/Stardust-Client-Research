using System.Buffers.Binary;
using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

/// <summary>Optional modern one-player admission policy, not a recovered dispatch table.</summary>
public sealed class ReplacementSelectedPlayerIngress : IDisposable
{
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private long generation;
    private ulong selectedObjectId;
    private bool bound, created, finalized, destroyed, failed, disposed, busy;
    private byte baselineMask;
    private int deferred;
    public ReplacementSelectedPlayerIngress() { }
    public bool RequiredBaselinesReceived { get { Owner(); return bound && !disposed && !failed && baselineMask == 7; } }
    public int DeferredApplications { get { Owner(); return deferred; } }
    public void Bind(long generation, ulong selectedObjectId)
    {
        Enter();
        try
        {
            Open(); if (bound) throw new InvalidOperationException("Ingress already bound.");
            if (generation <= 0 || selectedObjectId == 0) throw new ArgumentOutOfRangeException(nameof(generation));
            this.generation = generation; this.selectedObjectId = selectedObjectId; bound = true;
        }
        finally { busy = false; }
    }
    public bool Route(long generation, ReadOnlySpan<byte> application)
    {
        Enter();
        try
        {
            Open(); if (!bound || generation != this.generation) throw new InvalidOperationException("Current bound ingress generation required.");
            if (application.Length is < 6 or > SoeClientTransport.MaximumMessageSize) throw Invalid();
            uint opcode = BinaryPrimitives.ReadUInt32LittleEndian(application[2..]);
            switch (opcode)
            {
                case SceneObjectLifecycleCodec.CreateOpcode:
                    var create = SceneObjectLifecycleCodec.DecodeCreate(application);
                    if (create.ObjectId != selectedObjectId) return Defer();
                    Live(); if (created) throw Invalid(); created = true; return true;
                case ReplicationEnvelopeCodec.BaselineOpcode:
                case ReplicationEnvelopeCodec.DeltaOpcode:
                    var envelope = opcode == ReplicationEnvelopeCodec.BaselineOpcode
                        ? ReplicationEnvelopeCodec.DecodeBaseline(application, SoeClientTransport.MaximumMessageSize)
                        : ReplicationEnvelopeCodec.DecodeDelta(application, SoeClientTransport.MaximumMessageSize);
                    if (envelope.ObjectId != selectedObjectId) return Defer();
                    Live(); if (!created) throw Invalid();
                    if (envelope.Kind == ReplicationEnvelopeKind.Delta && !finalized) throw Invalid();
                    if (envelope.RawTag != 0x4352454F || envelope.View is not (3 or 4 or 6)) return Defer();
                    if (envelope.Kind == ReplicationEnvelopeKind.Delta)
                    { return true; }
                    byte bit = envelope.View switch { 3 => (byte)1, 4 => (byte)2, _ => (byte)4 };
                    if (finalized || (baselineMask & bit) != 0) throw Invalid();
                    baselineMask |= bit; return true;
                case SceneObjectLifecycleCodec.EndBaselinesOpcode:
                    var end = SceneObjectLifecycleCodec.DecodeEndBaselines(application);
                    if (end.ObjectId != selectedObjectId) return Defer();
                    Live(); if (!created || finalized || baselineMask != 7) throw Invalid(); finalized = true; return true;
                case SceneObjectLifecycleCodec.ContainmentOpcode:
                    var containment = SceneObjectLifecycleCodec.DecodeContainment(application);
                    if (containment.ObjectId != selectedObjectId) return Defer();
                    Live(); if (!created) throw Invalid(); return true;
                case SceneObjectLifecycleCodec.DestroyOpcode:
                    var destroy = SceneObjectLifecycleCodec.DecodeDestroy(application);
                    if (destroy.ObjectId != selectedObjectId) return Defer();
                    Live(); if (!created) throw Invalid(); destroyed = true; return true;
                case ZoneSceneProtocolCodec.CmdStartSceneOpcode:
                    var scene = ZoneSceneProtocolCodec.DecodeCmdStartScene(application);
                    if (scene.ObjectId == selectedObjectId) Live(); return true;
                case SceneReadyProtocolCodec.Opcode:
                    SceneReadyProtocolCodec.Decode(application); return true;
                default: return true; // Existing bridge returns nonworld messages explicitly to its caller.
            }
        }
        catch { failed = true; throw; }
        finally { busy = false; }
    }
    public void Dispose() { Enter(); try { disposed = true; } finally { busy = false; } }
    private bool Defer() { if (deferred == int.MaxValue) throw Invalid(); deferred++; return false; }
    private void Live() { if (destroyed) throw Invalid(); }
    private void Open() { if (disposed || failed) throw new InvalidOperationException("Ingress retired."); }
    private void Owner() { if (Environment.CurrentManagedThreadId != ownerThread || busy) throw new InvalidOperationException("Ingress owner thread without reentry required."); }
    private void Enter() { Owner(); busy = true; }
    private static InvalidDataException Invalid() => new("Invalid selected-player lifecycle ingress.");
}
