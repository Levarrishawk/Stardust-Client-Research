namespace Stardust.Core.Assets;

public sealed record CameraDistanceState(double Distance,float SafeDistance,bool Recovering);

/// <summary>Explicit modern policy: collision retraction is immediate; release approaches the new safe boom exponentially.</summary>
public sealed class ChaseCameraDistanceClock
{
    public double RecoverySeconds{get;}
    public CameraDistanceState? State{get;private set;}
    public ChaseCameraDistanceClock(double recoverySeconds=.2)
    {
        if(!double.IsFinite(recoverySeconds)||recoverySeconds<=0||recoverySeconds>10)throw new InvalidDataException("Bounded camera recovery time required.");
        RecoverySeconds=recoverySeconds;
    }
    public void Reset()=>State=null;
    public CameraDistanceState Advance(float safeDistance,double seconds,bool active=true)
    {
        if(!float.IsFinite(safeDistance)||safeDistance<0||safeDistance>100||!double.IsFinite(seconds)||seconds<0||seconds>.25)
            throw new InvalidDataException("Bounded safe camera distance/tick required.");
        double distance=State is null?safeDistance:Math.Min(State.Distance,safeDistance);
        if(active&&seconds>0&&distance<safeDistance)distance=safeDistance-(safeDistance-distance)*Math.Exp(-seconds/RecoverySeconds);
        State=new(distance,safeDistance,distance<safeDistance);return State;
    }
}
