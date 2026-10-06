using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

/// <summary>Serialized bounded synthetic generations; owns active root owner, never borrowed scene/FIFO.</summary>
public sealed class OfflineSoeRootMotionRestartOwner : IDisposable
{
    private readonly int maximumRuns,owner=Environment.CurrentManagedThreadId;
    private OfflineSoeRootMotionSessionOwner? active;
    private OfflineSoeRootMotionSessionOwner? failedRetirement;
    private OfflineSoeRootMotionCompletion? completion;
    private string? cleanupFaultType;
    private int generation;
    private bool disposed,busy,reentered;
    public OfflineSoeRootMotionRestartOwner(int maximumRuns=8)
    {
        if(maximumRuns is < 1 or > 32)throw new ArgumentOutOfRangeException(nameof(maximumRuns));
        this.maximumRuns=maximumRuns;
    }
    private void Enter()
    {
        if(Environment.CurrentManagedThreadId!=owner)throw new InvalidOperationException("Restart owner thread required.");
        if(busy){reentered=true;throw new InvalidOperationException("Restart owner reentry.");}
        busy=true;reentered=false;
    }
    public OfflineSoeRootMotionSessionOwner? Active{get{Enter();try{return active;}finally{busy=false;}}}
    public int Generation{get{Enter();try{return generation;}finally{busy=false;}}}
    public OfflineSoeRootMotionCompletion? LastCompletion{get{Enter();try{return completion;}finally{busy=false;}}}
    public bool IsDisposed{get{Enter();try{return disposed;}finally{busy=false;}}}
    public string? CleanupFaultType{get{Enter();try{return cleanupFaultType;}finally{busy=false;}}}
    private void CheckCleanupFault()
    {if(cleanupFaultType is not null)throw new InvalidOperationException("Owned retirement failed; restart owner is terminally faulted.");}
    private OfflineSoeRootMotionSessionOwner Admit(Func<OfflineSoeRootMotionSessionOwner> factory,bool restart)
    {
        ObjectDisposedException.ThrowIf(disposed,this);CheckCleanupFault();ArgumentNullException.ThrowIfNull(factory);
        if(active is not null||(!restart&&generation!=0)||(restart&&(generation==0||completion is null)))
            throw new InvalidOperationException("Initial Start or retired Complete followed by Restart required.");
        if(generation==maximumRuns)throw new InvalidOperationException("Successful generation budget exhausted.");
        OfflineSoeRootMotionSessionOwner? candidate=null;
        try
        {
            candidate=factory()??throw new InvalidDataException("Factory returned no root owner.");
            if(reentered)throw new InvalidOperationException("Factory caught reentry; admission poisoned.");
            if(candidate.IsDisposed)throw new InvalidDataException("Disposed root owner cannot be admitted.");
            var state=candidate.Session.State;var transport=candidate.Transport;
            if(state.Samples!=0||state.Queued!=0||state.Drained!=0||state.NextSequence!=0||state.IsFaulted||
                state.RelativeElapsedMicroseconds!=0||state.LastNowMilliseconds!=-1||state.LastReceipt is not null||
                transport.State!=SoeClientTransportState.Established||transport.PendingMessageCount!=0||transport.RetainedOutboundPackets!=0)
                throw new InvalidDataException("Fresh zero-sample/sequence established clean root owner required.");
            active=candidate;generation++;return candidate;
        }
        catch(Exception error)
        {
            if(candidate is not null)
            {
                try{candidate.Dispose();}
                catch(Exception cleanup)
                {
                    failedRetirement=candidate;cleanupFaultType=cleanup.GetType().Name;
                    throw new AggregateException("Factory admission and returned-prefix cleanup failed.",error,cleanup);
                }
            }
            throw;
        }
    }
    public OfflineSoeRootMotionSessionOwner Start(Func<OfflineSoeRootMotionSessionOwner> factory)
    {Enter();try{return Admit(factory,false);}finally{busy=false;}}
    public OfflineSoeRootMotionSessionOwner Restart(Func<OfflineSoeRootMotionSessionOwner> factory)
    {Enter();try{return Admit(factory,true);}finally{busy=false;}}
    public OfflineSoeRootMotionCompletion Complete(SceneWorldClientRootMotionQueue queue)
    {
        Enter();try
        {
            ObjectDisposedException.ThrowIf(disposed,this);CheckCleanupFault();ArgumentNullException.ThrowIfNull(queue);
            var current=active??throw new InvalidOperationException("No active generation.");
            if(current.IsDisposed)throw new InvalidOperationException("Borrowed active owner was externally disposed.");
            var report=OfflineSoeRootMotionCompletion.Capture(current.Session,current.Transport,queue);
            if(!report.SessionFaulted&&(report.QueueCount!=0||report.QueueBytes!=0||report.PendingMessages!=0||report.RetainedPackets!=0))
                throw new InvalidOperationException("Healthy generation still has outstanding buffers.");
            completion=report; // Accepted honest prefix is retained even if owned retirement fails.
            try{current.Dispose();}
            catch(Exception cleanup){failedRetirement=current;cleanupFaultType=cleanup.GetType().Name;throw;}
            finally{active=null;}
            return report;
        }finally{busy=false;}
    }
    public void Dispose()
    {
        Enter();try
        {
            if(disposed)return;disposed=true;
            // A failed owned prefix remains retained; never silently retry its retirement.
            if(failedRetirement is not null)return;
            try{active?.Dispose();}
            catch(Exception cleanup){failedRetirement=active;cleanupFaultType=cleanup.GetType().Name;throw;}
            finally{active=null;}
        }finally{busy=false;}
    }
}
