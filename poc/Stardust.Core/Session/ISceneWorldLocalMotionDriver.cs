using Stardust.Core.Assets;
namespace Stardust.Core.Session;

/// <summary>Modern planar local movement diagnostics; not recovered prediction or floor physics.</summary>
public sealed record SceneWorldLocalMotionDriverState(
    ulong ObjectId, AppearanceTransform Transform, float Speed, long TimestampMicroseconds,
    int StepsThisUpdate, long DroppedMicroseconds, long SuspendedMicroseconds,
    bool Active, string? FaultType);

/// <summary>Owner-thread input adapter boundary over integer-clock native motion.</summary>
public interface ISceneWorldLocalMotionDriver : IDisposable
{
    SceneWorldLocalMotionDriverState State { get; }
    void Submit(long timestampMicroseconds, PortalActorInput input);
    SceneWorldLocalMotionDriverState Advance(long timestampMicroseconds, bool active = true);
    void Reset(long timestampMicroseconds);
}
