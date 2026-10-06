using System.Numerics;

namespace Stardust.Core.Session;

public sealed record TerrainOfflineSessionState(int CompletedUpdates, bool Paused, bool StopRequested,
    bool Stopped, bool IsFaulted, string? FaultType, SceneWorldTerrainPointTargetState DriverResult);

/// <summary>Modern pause/stop owner borrowing the actual sole-writer point driver; never refunds a tick.</summary>
public sealed class TerrainOfflineSessionController : IDisposable
{
    private readonly int ownerThread = Environment.CurrentManagedThreadId, maximumUpdates;
    private readonly long stepMicroseconds;
    private SceneWorldTerrainPointTargetDriver? driver;
    private TerrainOfflineSessionState state;
    private bool busy, disposed;
    public TerrainOfflineSessionController(SceneWorldTerrainPointTargetDriver driver, int maximumUpdates, long stepMicroseconds = 10000)
    {
        ArgumentNullException.ThrowIfNull(driver);
        if (maximumUpdates is < 1 or > 6000 || stepMicroseconds is < 1000 or > 100000) throw new ArgumentOutOfRangeException(nameof(maximumUpdates));
        var actual=driver.State;
        if (actual.CompletedUpdates!=0 || actual.AttemptedUpdates!=0) throw new InvalidDataException("Initial unadvanced point driver required.");
        Healthy(driver,actual); this.driver=driver; this.maximumUpdates=maximumUpdates; this.stepMicroseconds=stepMicroseconds;
        state=new(0,driver.IsPaused,false,false,false,null,actual);
    }
    private void Enter()
    {
        if(Environment.CurrentManagedThreadId!=ownerThread)throw new InvalidOperationException("Offline session owner thread required.");
        if(busy)throw new InvalidOperationException("Offline session reentry.");busy=true;
    }
    public TerrainOfflineSessionState State { get { Enter();try{return state;}finally{busy=false;} } }
    private static void Healthy(SceneWorldTerrainPointTargetDriver driver,SceneWorldTerrainPointTargetState actual)
    {
        var inner=actual.DriverResult;
        if(actual.Mode is SceneWorldTerrainPointTargetMode.Faulted or SceneWorldTerrainPointTargetMode.Unavailable or SceneWorldTerrainPointTargetMode.Disposed ||
            !inner.Active || inner.FaultType is not null || inner.TimestampMicroseconds<0 || inner.DroppedMicroseconds!=0 || inner.SuspendedMicroseconds!=0 ||
            !ReferenceEquals(driver.Driver?.State,inner))throw new InvalidDataException("Healthy actual point/terrain result required.");
    }
    private void Admit()
    {
        ObjectDisposedException.ThrowIf(disposed,this);
        if(state.Stopped || state.IsFaulted)throw new InvalidOperationException("Offline session terminal.");
        var actual=driver!.State;
        if(actual.CompletedUpdates!=state.CompletedUpdates || actual.AttemptedUpdates!=state.CompletedUpdates ||
            actual.DriverResult.TimestampMicroseconds!=state.DriverResult.DriverResult.TimestampMicroseconds)
        {Fault("ExternalDriverProgress",actual);throw new InvalidOperationException("External driver advance detected.");}
        try { Healthy(driver,actual); } catch { Fault("ExternalDriverUnhealthy",actual); throw; }
    }
    private void Fault(string why,SceneWorldTerrainPointTargetState actual)
    { state=state with {CompletedUpdates=actual.CompletedUpdates,DriverResult=actual,IsFaulted=true,FaultType=why,Stopped=false}; }
    public void SetPaused(bool paused)
    { Enter();try{Admit();if(state.Paused!=paused)state=state with {Paused=paused};}finally{busy=false;} }
    public void RequestStop()
    { Enter();try{Admit();if(!state.StopRequested)state=state with {StopRequested=true};}finally{busy=false;} }
    public TerrainOfflineSessionState AdvanceNext(long currentClock,long nextClock,CancellationToken token=default)
    {
        Enter();try
        {
            token.ThrowIfCancellationRequested();Admit();
            if(state.CompletedUpdates>=maximumUpdates || currentClock!=state.DriverResult.DriverResult.TimestampMicroseconds ||
                currentClock>long.MaxValue-stepMicroseconds || nextClock!=currentClock+stepMicroseconds)
                throw new ArgumentOutOfRangeException(nameof(nextClock));
            bool stopping=state.StopRequested || state.CompletedUpdates==maximumUpdates-1;
            token.ThrowIfCancellationRequested();
            try
            {
                // Mutation boundary: all later failures preserve the actual borrowed prefix and latch terminal state.
                driver!.SetPaused(stopping?false:state.Paused,currentClock,token);
                if(stopping)driver.ClearTarget(currentClock,token);
                var actual=driver.AdvanceNext(currentClock,nextClock,token);
                Healthy(driver,actual);
                if(actual.CompletedUpdates!=state.CompletedUpdates+1 || actual.AttemptedUpdates!=actual.CompletedUpdates ||
                    actual.DriverResult.TimestampMicroseconds!=nextClock || actual.DriverResult.StepsThisUpdate!=1)
                    throw new InvalidDataException("One successful actual grounded step required.");
                bool stopped=stopping && actual.Mode==SceneWorldTerrainPointTargetMode.Idle && actual.Target is null &&
                    driver.Target is null && driver.ManualInput==Vector2.Zero && actual.DriverResult.Speed==0;
                if(stopping&&!stopped)throw new InvalidDataException("Final actual zero-speed idle stop required.");
                state=new(actual.CompletedUpdates,stopping?false:state.Paused,stopping||state.StopRequested,stopped,false,null,actual);
                return state;
            }
            catch(Exception e){Fault(e.GetType().Name,driver!.State);throw;}
        }finally{busy=false;}
    }
    public void Dispose()
    {Enter();try{if(disposed)return;disposed=true;driver=null;}finally{busy=false;}}
}
