using System.Numerics;

namespace Stardust.Core.Session;

public sealed record TerrainCameraPivotFollowState(Vector3 Pivot, Vector3 Target, long TimestampMicroseconds, int Updates, int Rebases);

/// <summary>Modern native pivot follow only; caller supplies accepted-position provenance.</summary>
public sealed class TerrainCameraPivotFollowClock : IDisposable
{
    private readonly int ownerThread = Environment.CurrentManagedThreadId, maximumUpdates, maximumRebases;
    private readonly Vector3 offset;
    private readonly long stepMicroseconds;
    private readonly double factor;
    private TerrainCameraPivotFollowState state;
    private bool busy, disposed;
    public TerrainCameraPivotFollowClock(Vector3 initialAcceptedPosition, Vector3 nativeOffset,
        long initialTimestampMicroseconds = 0, long stepMicroseconds = 10000, double responseSeconds = .1,
        int maximumUpdates = 128, int maximumRebases = 128, CancellationToken token = default, TerrainOfflineSessionBudget? budget = null)
    {
        token.ThrowIfCancellationRequested();
        if (stepMicroseconds is < 1000 or > 100000 || maximumUpdates < 1 || maximumUpdates > (budget?.MaximumUpdates ?? 256) || maximumRebases is < 1 or > 256 ||
            initialTimestampMicroseconds < 0 || initialTimestampMicroseconds > long.MaxValue-checked((long)maximumUpdates*stepMicroseconds))
            throw new ArgumentOutOfRangeException(nameof(initialTimestampMicroseconds));
        if (!double.IsFinite(responseSeconds) || responseSeconds < 1e-6 || responseSeconds > 10)
            throw new ArgumentOutOfRangeException(nameof(responseSeconds));
        Position(nativeOffset); offset = nativeOffset;
        var target = Target(initialAcceptedPosition); var pivot = Publish(target.X,target.Y,target.Z);
        factor = Math.Exp(-(stepMicroseconds/1_000_000.0)/responseSeconds);
        this.stepMicroseconds = stepMicroseconds; this.maximumUpdates = maximumUpdates; this.maximumRebases = maximumRebases;
        var candidate = new TerrainCameraPivotFollowState(pivot,pivot,initialTimestampMicroseconds,0,0);
        token.ThrowIfCancellationRequested(); state = candidate;
    }
    private void Enter()
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Pivot follow owner thread required.");
        if (busy) throw new InvalidOperationException("Pivot follow reentry."); busy = true;
    }
    public TerrainCameraPivotFollowState State { get { Enter(); try { return state; } finally { busy = false; } } }
    private static void Position(Vector3 p)
    {
        if (!float.IsFinite(p.X) || !float.IsFinite(p.Y) || !float.IsFinite(p.Z) || Math.Abs(p.X)>1e6 || Math.Abs(p.Y)>1e6 || Math.Abs(p.Z)>1e6)
            throw new ArgumentOutOfRangeException(nameof(p));
    }
    private (double X,double Y,double Z) Target(Vector3 acceptedPosition)
    {
        Position(acceptedPosition);
        var target = ((double)acceptedPosition.X+offset.X,(double)acceptedPosition.Y+offset.Y,(double)acceptedPosition.Z+offset.Z);
        Validate(target.Item1,target.Item2,target.Item3); return target;
    }
    private static void Validate(double x,double y,double z)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(z) || Math.Abs(x)>1e6 || Math.Abs(y)>1e6 || Math.Abs(z)>1e6)
            throw new InvalidDataException("Finite bounded pivot/target required before binary32 publication.");
    }
    private static Vector3 Publish(double x,double y,double z)
    { Validate(x,y,z); return new((float)x,(float)y,(float)z); }
    private void Admit(CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(disposed,this); token.ThrowIfCancellationRequested();
        if (state.Updates >= maximumUpdates) throw new InvalidOperationException("Pivot follow update capacity.");
    }
    public TerrainCameraPivotFollowState Advance(Vector3 acceptedPosition, long exactNextClock, CancellationToken token = default)
    {
        Enter(); try
        {
            Admit(token);
            if (state.TimestampMicroseconds > long.MaxValue-stepMicroseconds || exactNextClock != state.TimestampMicroseconds+stepMicroseconds)
                throw new ArgumentOutOfRangeException(nameof(exactNextClock));
            var target = Target(acceptedPosition); var old = state.Pivot;
            var pivot = Publish(target.X+((double)old.X-target.X)*factor,target.Y+((double)old.Y-target.Y)*factor,target.Z+((double)old.Z-target.Z)*factor);
            var candidate = new TerrainCameraPivotFollowState(pivot,Publish(target.X,target.Y,target.Z),exactNextClock,state.Updates+1,state.Rebases);
            token.ThrowIfCancellationRequested(); state = candidate; return state;
        } finally { busy = false; }
    }
    public TerrainCameraPivotFollowState Rebase(Vector3 authoritativePosition, long exactUnchangedClock, CancellationToken token = default)
    {
        Enter(); try
        {
            Admit(token);
            if (exactUnchangedClock != state.TimestampMicroseconds || state.Rebases >= maximumRebases)
                throw new ArgumentOutOfRangeException(nameof(exactUnchangedClock));
            var target = Target(authoritativePosition); var pivot = Publish(target.X,target.Y,target.Z);
            var candidate = new TerrainCameraPivotFollowState(pivot,pivot,state.TimestampMicroseconds,state.Updates,state.Rebases+1);
            token.ThrowIfCancellationRequested(); state = candidate; return state;
        } finally { busy = false; }
    }
    public void Dispose()
    { Enter(); try { if(disposed)return; disposed=true; } finally { busy=false; } }
}
