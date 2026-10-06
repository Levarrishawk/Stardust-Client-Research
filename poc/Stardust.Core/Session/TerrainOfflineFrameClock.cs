namespace Stardust.Core.Session;

public sealed record TerrainOfflineFrameReceipt(int Steps, long StepMicroseconds, int TotalReservedSteps,
    long RemainderMicroseconds, long DroppedMicroseconds, long TotalDroppedMicroseconds, bool Exhausted);

/// <summary>Owned integer scheduling slots only; caller counts completed core work separately.</summary>
public sealed class TerrainOfflineFrameClock : IDisposable
{
    private readonly int ownerThread = Environment.CurrentManagedThreadId, maxStepsPerFrame, maximumReservedSteps;
    private readonly long stepMicroseconds, maximumFrameMicroseconds;
    private TerrainOfflineFrameReceipt state;
    private bool busy, disposed;
    public TerrainOfflineFrameClock(long stepMicroseconds = 10000, int maxStepsPerFrame = 8,
        int maximumReservedSteps = 2000, long maximumFrameMicroseconds = 250000, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (stepMicroseconds is < 1000 or > 100000 || maxStepsPerFrame is < 1 or > 32 ||
            maximumReservedSteps is < 1 or > 6000 || maximumFrameMicroseconds is < 1 or > 250000)
            throw new ArgumentOutOfRangeException(nameof(stepMicroseconds));
        this.stepMicroseconds = stepMicroseconds; this.maxStepsPerFrame = maxStepsPerFrame;
        this.maximumReservedSteps = maximumReservedSteps; this.maximumFrameMicroseconds = maximumFrameMicroseconds;
        state = new(0,stepMicroseconds,0,0,0,0,false);
    }
    private void Enter()
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Offline frame-clock owner thread required.");
        if (busy) throw new InvalidOperationException("Offline frame-clock reentry."); busy = true;
    }
    public TerrainOfflineFrameReceipt State { get { Enter(); try { return state; } finally { busy=false; } } }
    public bool Exhausted { get { Enter(); try { return state.Exhausted; } finally { busy=false; } } }
    public TerrainOfflineFrameReceipt AdvanceFrame(long elapsedMicroseconds, CancellationToken token = default)
    {
        Enter(); try
        {
            ObjectDisposedException.ThrowIf(disposed,this); token.ThrowIfCancellationRequested();
            if (elapsedMicroseconds < 0 || elapsedMicroseconds > maximumFrameMicroseconds)
                throw new ArgumentOutOfRangeException(nameof(elapsedMicroseconds));
            long available = checked(state.RemainderMicroseconds+elapsedMicroseconds);
            long due = available/stepMicroseconds;
            int steps = (int)Math.Min(due,Math.Min(maxStepsPerFrame,maximumReservedSteps-state.TotalReservedSteps));
            long dropped = checked((due-steps)*stepMicroseconds);
            int total = checked(state.TotalReservedSteps+steps);
            var candidate = new TerrainOfflineFrameReceipt(steps,stepMicroseconds,total,available%stepMicroseconds,
                dropped,checked(state.TotalDroppedMicroseconds+dropped),total==maximumReservedSteps);
            token.ThrowIfCancellationRequested(); state=candidate; return state;
        } finally { busy=false; }
    }
    public void Dispose()
    { Enter(); try { if(disposed)return; disposed=true; } finally { busy=false; } }
}
