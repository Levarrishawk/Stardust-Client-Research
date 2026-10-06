using System.Numerics;
using Stardust.Core.Assets;
namespace Stardust.Core.Session;

public enum SceneWorldCurrentSpeedPlaybackPolicy { SuppliedLocalOnlyStoredSpeedOnWorldZ=1 }
public enum SceneWorldCurrentSpeedPlaybackState { Ready,Complete,Unavailable,Faulted,Disposed }
public sealed record SceneWorldCurrentSpeedPlaybackLimits(int MaximumSteps=128,int MaximumDriverOperations=384,int MaximumOutputBytes=512);
public sealed class SceneWorldCurrentSpeedPlaybackReceipt
{
    internal SceneWorldCurrentSpeedPlaybackReceipt(object owner,SceneWorldSuppliedCurrentSpeedPlanReceipt source,
        CreatureNominalSpeedPlanReceipt step,int index,long submit,long advance,SceneWorldLocalMotionDriverState result)
    { Owner=owner;Source=source;Step=step;Index=index;SubmitTimestampMicroseconds=submit;AdvanceTimestampMicroseconds=advance;DriverResult=result; }
    internal object Owner { get; }
    public SceneWorldSuppliedCurrentSpeedPlanReceipt Source { get; }
    public CreatureNominalSpeedPlanReceipt Step { get; }
    public int Index { get; }
    public uint StoredBits => Step.Transition.StoredBits;
    public long SubmitTimestampMicroseconds { get; }
    public long AdvanceTimestampMicroseconds { get; }
    public SceneWorldLocalMotionDriverState DriverResult { get; }
    public SceneWorldCurrentSpeedPlaybackPolicy Policy => SceneWorldCurrentSpeedPlaybackPolicy.SuppliedLocalOnlyStoredSpeedOnWorldZ;
}
/// <summary>Bounded modern world-Z playback over borrowed current plan and driver; no retry or rollback.</summary>
public sealed class SceneWorldCurrentSpeedPlanPlayback : IDisposable
{
    public const int HardMaximumSteps=128;
    public const int OutputBytes=512;
    private readonly int ownerThread=Environment.CurrentManagedThreadId;
    private readonly object identity=new();
    private readonly long stepMicroseconds;
    private readonly int stepCount;
    private readonly int maximumDriverOperations;
    private SceneWorldSuppliedCurrentSpeedPlan? composition;
    private SceneWorldSuppliedCurrentSpeedPlanReceipt? source;
    private ISceneWorldLocalMotionDriver? driver;
    private bool busy,closed;
    private int nextIndex,stateReads,submitAttempts,advanceAttempts;
    private long lastTimestamp;
    private SceneWorldCurrentSpeedPlaybackState state;
    private string? faultType,unavailableReason;
    private SceneWorldCurrentSpeedPlaybackReceipt? lastSuccessful;
    private SceneWorldLocalMotionDriverState? lastAttemptedDriverResult;
    public SceneWorldCurrentSpeedPlanPlayback(SceneWorldSuppliedCurrentSpeedPlan composition,
        SceneWorldSuppliedCurrentSpeedPlanReceipt receipt,ISceneWorldLocalMotionDriver driver,
        SceneWorldCurrentSpeedPlaybackPolicy policy,long initialTimestampMicroseconds,long stepMicroseconds,
        SceneWorldCurrentSpeedPlaybackLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(composition);ArgumentNullException.ThrowIfNull(receipt);ArgumentNullException.ThrowIfNull(driver);
        if(policy!=SceneWorldCurrentSpeedPlaybackPolicy.SuppliedLocalOnlyStoredSpeedOnWorldZ||receipt.Policy!=CreatureSuppliedCurrentSpeedPolicy.SuppliedLocalOnly)throw new ArgumentOutOfRangeException(nameof(policy));
        if(initialTimestampMicroseconds<0||stepMicroseconds is <1000 or >100000)throw new ArgumentOutOfRangeException(nameof(initialTimestampMicroseconds));
        var p=limits??new();if(p.MaximumSteps is <0 or >128||p.MaximumDriverOperations is <0 or >384||p.MaximumOutputBytes is <0 or >65536)throw new ArgumentOutOfRangeException(nameof(limits));
        if(receipt.Plan.StepCount>p.MaximumSteps||checked(receipt.Plan.StepCount*3)>p.MaximumDriverOperations||p.MaximumOutputBytes<OutputBytes)throw new InvalidDataException("Playback fixed count/output reservation exhausted before any driver operation.");
        this.composition=composition;source=receipt;this.driver=driver;this.stepMicroseconds=stepMicroseconds;lastTimestamp=initialTimestampMicroseconds;
        maximumDriverOperations=p.MaximumDriverOperations;stepCount=receipt.Plan.StepCount;state=stepCount==0?SceneWorldCurrentSpeedPlaybackState.Complete:SceneWorldCurrentSpeedPlaybackState.Ready;
    }
    private void Enter()
    { if(Environment.CurrentManagedThreadId!=ownerThread)throw new InvalidOperationException("Current-plan playback owner thread required.");
      if(busy)throw new InvalidOperationException("Current-plan playback reentry.");busy=true; }
    private T Read<T>(Func<T> value) { Enter();try{return value();}finally{busy=false;} }
    public SceneWorldCurrentSpeedPlaybackState State => Read(()=>state);
    public int NextIndex => Read(()=>nextIndex);
    public int CompletedSteps => Read(()=>nextIndex);
    public int StateReads => Read(()=>stateReads);
    public string? UnavailableReason => Read(()=>unavailableReason);
    public int SubmitAttempts => Read(()=>submitAttempts);
    public int AdvanceAttempts => Read(()=>advanceAttempts);
    public string? FaultType => Read(()=>faultType);
    public SceneWorldCurrentSpeedPlaybackReceipt? LastSuccessful => Read(()=>lastSuccessful);
    public SceneWorldLocalMotionDriverState? LastAttemptedDriverResult => Read(()=>lastAttemptedDriverResult);
    private static bool Finite(Vector3 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z);
    private static bool Healthy(SceneWorldLocalMotionDriverState r,ulong objectId,long time)=>r is not null&&r.ObjectId==objectId&&r.TimestampMicroseconds==time&&r.Active&&r.FaultType is null&&r.StepsThisUpdate==1&&r.DroppedMicroseconds==0&&r.SuspendedMicroseconds==0&&float.IsFinite(r.Speed)&&r.Speed>=0&&r.Speed<=10&&Finite(r.Transform.Translation)&&Finite(r.Transform.RowX)&&Finite(r.Transform.RowY)&&Finite(r.Transform.RowZ);
    private void Latch(string why) { faultType??=why;state=SceneWorldCurrentSpeedPlaybackState.Faulted; }
    public SceneWorldCurrentSpeedPlaybackReceipt? AdvanceNext(long submitTimestampMicroseconds,long advanceTimestampMicroseconds,CancellationToken cancellationToken=default)
    {
        Enter();try
        {
            ObjectDisposedException.ThrowIf(closed,this);cancellationToken.ThrowIfCancellationRequested();
            if(state is SceneWorldCurrentSpeedPlaybackState.Faulted or SceneWorldCurrentSpeedPlaybackState.Unavailable)return null;
            if(!composition!.IsCurrent(source!)){state=SceneWorldCurrentSpeedPlaybackState.Unavailable;unavailableReason="SourceNotCurrent";return null;}
            if(nextIndex==stepCount){state=SceneWorldCurrentSpeedPlaybackState.Complete;return null;}
            if(lastTimestamp>long.MaxValue-stepMicroseconds||submitTimestampMicroseconds!=lastTimestamp||advanceTimestampMicroseconds!=lastTimestamp+stepMicroseconds)throw new ArgumentOutOfRangeException(nameof(advanceTimestampMicroseconds),"Exact nonnegative consecutive one-step timestamps required before driver calls.");
            var step=source!.Plan.Steps[nextIndex];float speed=BitConverter.Int32BitsToSingle(unchecked((int)step.Transition.StoredBits));
            if(!float.IsFinite(speed)||MathF.Abs(speed)>10)throw new InvalidDataException("Stored speed exceeds frozen bounded local-driver input before any calls.");
            var input=new PortalActorInput(new Vector3(0,0,speed),null);cancellationToken.ThrowIfCancellationRequested();
            if(stateReads+submitAttempts+advanceAttempts>maximumDriverOperations-3)throw new InvalidDataException("Playback driver operation budget exhausted before calls.");
            SceneWorldLocalMotionDriverState before;
            try{stateReads++;before=driver!.State;}
            catch(Exception e){state=SceneWorldCurrentSpeedPlaybackState.Unavailable;unavailableReason="StateRead:"+e.GetType().Name;return null;}
            if(before is null||before.ObjectId!=source.Incarnation.ObjectId||before.TimestampMicroseconds!=lastTimestamp||!before.Active||before.FaultType is not null||!float.IsFinite(before.Speed)||before.Speed<0||before.Speed>10||!Finite(before.Transform.Translation)||!Finite(before.Transform.RowX)||!Finite(before.Transform.RowY)||!Finite(before.Transform.RowZ))
            {state=SceneWorldCurrentSpeedPlaybackState.Unavailable;unavailableReason="DriverBindingOrClockUnavailable";return null;}
            cancellationToken.ThrowIfCancellationRequested();if(!composition.IsCurrent(source)){state=SceneWorldCurrentSpeedPlaybackState.Unavailable;unavailableReason="SourceChangedBeforeSubmit";return null;}
            try
            {
                lastAttemptedDriverResult=null;submitAttempts++;driver!.Submit(submitTimestampMicroseconds,input);
                cancellationToken.ThrowIfCancellationRequested();if(!composition.IsCurrent(source)){Latch("SourceChangedAfterSubmit");return null;}
                advanceAttempts++;var actual=driver.Advance(advanceTimestampMicroseconds,true);lastAttemptedDriverResult=actual;
                if(!Healthy(actual,source.Incarnation.ObjectId,advanceTimestampMicroseconds)){Latch("UnhealthyDriverResult");return null;}
                cancellationToken.ThrowIfCancellationRequested();if(!composition.IsCurrent(source)){Latch("SourceChangedAfterAdvance");return null;}
                var result=new SceneWorldCurrentSpeedPlaybackReceipt(identity,source,step,nextIndex,submitTimestampMicroseconds,advanceTimestampMicroseconds,actual);
                cancellationToken.ThrowIfCancellationRequested();nextIndex++;lastTimestamp=advanceTimestampMicroseconds;lastSuccessful=result;
                state=nextIndex==stepCount?SceneWorldCurrentSpeedPlaybackState.Complete:SceneWorldCurrentSpeedPlaybackState.Ready;return result;
            }
            catch(OperationCanceledException){Latch(nameof(OperationCanceledException));throw;}
            catch(Exception e){Latch(e.GetType().Name);return null;}
        }finally{busy=false;}
    }
    public bool IsCurrent(SceneWorldCurrentSpeedPlaybackReceipt receipt)
    {
        Enter();try { ArgumentNullException.ThrowIfNull(receipt);return !closed&&ReferenceEquals(receipt.Owner,identity)&&composition!.IsCurrent(receipt.Source); }
        finally{busy=false;}
    }
    public void Dispose()
    { Enter();try { if(closed)return;closed=true;state=SceneWorldCurrentSpeedPlaybackState.Disposed;driver=null;composition=null;source=null;lastSuccessful=null;lastAttemptedDriverResult=null; }finally{busy=false;} }
}
