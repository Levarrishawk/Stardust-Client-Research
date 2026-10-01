using System.Numerics;

namespace Stardust.Core.Assets;

/// <summary>Modern immutable camera heading/pitch; locomotion uses horizontal heading only.</summary>
public sealed record ActorViewOrientation
{
    public const float MaximumPitchRadians=85*MathF.PI/180;
    public float HeadingRadians {get;}
    public float PitchRadians {get;}
    public ActorViewOrientation(float headingRadians=0,float pitchRadians=0)
    {
        Validate(headingRadians);Validate(pitchRadians);
        HeadingRadians=(float)Math.IEEERemainder(headingRadians,2*Math.PI);PitchRadians=Math.Clamp(pitchRadians,-MaximumPitchRadians,MaximumPitchRadians);
    }
    public ActorViewOrientation Rotate(float headingDelta,float pitchDelta)
    {
        Validate(headingDelta);Validate(pitchDelta);return new((float)Math.IEEERemainder((double)HeadingRadians+headingDelta,2*Math.PI),Math.Clamp(PitchRadians+pitchDelta,-MaximumPitchRadians,MaximumPitchRadians));
    }
    public Vector3 PlanarForward(Vector3 referenceForward)
    {
        if(!float.IsFinite(referenceForward.X)||!float.IsFinite(referenceForward.Y)||!float.IsFinite(referenceForward.Z)||MathF.Abs(referenceForward.Y)>1e-6f||referenceForward.LengthSquared()<1e-12f||referenceForward.LengthSquared()>1e12f)throw new ArgumentException("Finite nonzero planar view reference required.");
        return Vector3.Transform(Vector3.Normalize(referenceForward),Matrix4x4.CreateRotationY(HeadingRadians));
    }
    public Vector3 LookForward(Vector3 referenceForward)=>PlanarForward(referenceForward)*MathF.Cos(PitchRadians)+Vector3.UnitY*MathF.Sin(PitchRadians);
    private static void Validate(float angle){if(!float.IsFinite(angle)||MathF.Abs(angle)>1e6f)throw new ArgumentOutOfRangeException(nameof(angle));}
}
