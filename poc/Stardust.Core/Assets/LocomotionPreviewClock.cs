using System.Numerics;

namespace Stardust.Core.Assets;

public enum PreviewLocomotionMode{Idle,Walk,Run}
public readonly record struct PreviewLocomotionClip(float FrameRate,ushort EndFrame,double IdleTimeScale,float AuthoredSpeed);
public sealed record LocomotionPreviewState(PreviewLocomotionMode Mode,double Frame,double NormalizedPhase,double AcceptedDistance,long Switches,Vector3 Heading);

/// <summary>Explicit modern presentation policy: accepted planar displacement controls gait/phase; it never moves an actor.</summary>
public sealed class LocomotionPreviewClock
{
    private readonly PreviewLocomotionClip idle,walk,run;
    private readonly float runThreshold;
    public LocomotionPreviewState State{get;private set;}=new(PreviewLocomotionMode.Idle,0,0,0,0,Vector3.UnitZ);
    public LocomotionPreviewClock(PreviewLocomotionClip idle,PreviewLocomotionClip walk,PreviewLocomotionClip run,float runThreshold)
    {
        Validate(idle,false);Validate(walk,true);Validate(run,true);
        if(!float.IsFinite(runThreshold)||runThreshold<=0||runThreshold>1000)throw new InvalidDataException("Invalid explicit run threshold.");
        this.idle=idle;this.walk=walk;this.run=run;this.runThreshold=runThreshold;
    }
    public static PreviewLocomotionClip Describe(PreparedSkeletalAnimation clip)=>new(clip.FrameRate,clip.EndFrame,clip.ProxyTimeScale,clip.Locomotion?.AuthoredSpeed??0);
    public void Reset(Vector3 heading)
    {
        heading=Planar(heading);if(heading.LengthSquared()<1e-12f)throw new InvalidDataException("Locomotion heading required.");
        State=new(PreviewLocomotionMode.Idle,0,0,0,0,Vector3.Normalize(heading));
    }
    public bool Advance(double seconds,Vector3 acceptedWorldDelta,bool active=true)
    {
        if(!double.IsFinite(seconds)||seconds<0||seconds>.25)throw new InvalidDataException("Bounded locomotion tick time required.");
        Vector3 planar=Planar(acceptedWorldDelta);double distance=planar.Length();
        if(distance>100||seconds==0&&distance!=0)throw new InvalidDataException("Bounded accepted locomotion displacement required.");
        if(!active||seconds==0)return false;
        var mode=distance<=1e-6?PreviewLocomotionMode.Idle:distance/seconds>=runThreshold?PreviewLocomotionMode.Run:PreviewLocomotionMode.Walk;
        var clip=mode==PreviewLocomotionMode.Idle?idle:mode==PreviewLocomotionMode.Walk?walk:run;
        double increment=mode==PreviewLocomotionMode.Idle?seconds*clip.FrameRate*clip.IdleTimeScale/clip.EndFrame:distance*clip.FrameRate/(clip.AuthoredSpeed*clip.EndFrame);
        double phase=(State.NormalizedPhase+increment)%1;
        State=new(mode,phase*clip.EndFrame,phase,State.AcceptedDistance+distance,checked(State.Switches+(State.Mode==mode?0:1)),distance>1e-6?Vector3.Normalize(planar):State.Heading);
        return true;
    }
    private static Vector3 Planar(Vector3 value)
    {
        if(!float.IsFinite(value.X)||!float.IsFinite(value.Y)||!float.IsFinite(value.Z)||Math.Abs(value.X)>1e6||Math.Abs(value.Y)>1e6||Math.Abs(value.Z)>1e6)throw new InvalidDataException("Finite locomotion vector required.");
        return new(value.X,0,value.Z);
    }
    private static void Validate(PreviewLocomotionClip clip,bool movement)
    {
        if(!float.IsFinite(clip.FrameRate)||clip.FrameRate<=0||clip.FrameRate>1000||clip.EndFrame==0||!double.IsFinite(clip.IdleTimeScale)||clip.IdleTimeScale<=0||clip.IdleTimeScale>1e6||!float.IsFinite(clip.AuthoredSpeed)||clip.AuthoredSpeed<0||clip.AuthoredSpeed>1e6||movement&&clip.AuthoredSpeed<=0)throw new InvalidDataException("Invalid locomotion clip descriptor.");
    }
}
