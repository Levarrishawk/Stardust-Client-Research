namespace Stardust.Core.Assets;

/// <summary>One cooperative worker and one result transfer. Caller owns request snapshots and transferred data.</summary>
public sealed class OwnedBackgroundAssetLoad<T>:IDisposable where T:class
{
    private readonly object gate=new();
    private readonly CancellationTokenSource cancellation=new();
    private readonly CancellationToken token;
    private readonly Task<T> worker;
    private bool canceled,disposed,taken;
    public Task WorkerCompletion=>worker;
    public bool IsCancellationRequested{get{lock(gate)return canceled;}}
    public OwnedBackgroundAssetLoad(Func<CancellationToken,T> load)
    {
        ArgumentNullException.ThrowIfNull(load);token=cancellation.Token;worker=Task.Run(()=>load(token),token);
    }
    public Task<T> TakeAsync()
    {
        lock(gate){ObjectDisposedException.ThrowIf(disposed,this);if(taken)throw new InvalidOperationException("Asset load result already requested.");taken=true;}
        return TransferAsync();
    }
    private async Task<T> TransferAsync()
    {
        T result;
        try{result=await worker.ConfigureAwait(false);}
        catch{lock(gate){if(canceled||disposed)throw new OperationCanceledException(token);}throw;}
        lock(gate){if(canceled||disposed)throw new OperationCanceledException(token);}
        return result??throw new InvalidDataException("Asset worker returned no owned result.");
    }
    public void Cancel()
    {
        lock(gate){if(disposed||canceled)return;canceled=true;cancellation.Cancel();}
    }
    public void Dispose()
    {
        lock(gate)
        {
            if(disposed)return;disposed=true;canceled=true;
            try{cancellation.Cancel();}
            finally
            {
                _=worker.ContinueWith(completed=>{_ = completed.Exception;cancellation.Dispose();},CancellationToken.None,TaskContinuationOptions.ExecuteSynchronously,TaskScheduler.Default);
            }
        }
    }
}
