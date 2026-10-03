using Stardust.Core.Assets;
namespace Stardust.Core.Session;

/// <summary>Modern immutable native pose; raw create placement remains unchanged.</summary>
public sealed record SceneWorldObjectMotionSnapshot(
    SceneObjectIncarnation Incarnation, ulong Revision, AppearanceTransform Transform,
    float Speed, uint? MovementCounter, bool Authoritative);

/// <summary>Borrowed latest pose provider for an exact current incarnation.</summary>
public interface ISceneWorldObjectMotionSource
{
    SceneWorldObjectMotionSnapshot? Find(SceneWorldObjectSnapshot snapshot);
}
