using Stardust.Core.Assets;

internal static class BackgroundAssetLoadScenarios
{
    internal static async Task RunAsync()
    {
        int caller=Environment.CurrentManagedThreadId,workerThread=caller;
        using(var initialStarted=new ManualResetEventSlim())using(var job=new OwnedBackgroundAssetLoad<string>(_=>{workerThread=Environment.CurrentManagedThreadId;initialStarted.Set();return "owned";}))
        {
            if(!initialStarted.Wait(TimeSpan.FromSeconds(5)))throw new Exception("Initial asset worker did not start.");
            if(await job.TakeAsync()!="owned"||workerThread==caller)throw new Exception("Asset worker affinity/transfer failed.");
            HostAccountingScenarios.Reject<InvalidOperationException>(()=>job.TakeAsync());job.Cancel();job.Cancel();
        }
        using(var started=new ManualResetEventSlim())using(var release=new ManualResetEventSlim())
        {
            var job=new OwnedBackgroundAssetLoad<string>(_=>{started.Set();if(!release.Wait(TimeSpan.FromSeconds(5)))throw new Exception("Controlled worker release timed out.");return "stale";});
            Task<string> take=job.TakeAsync();if(!started.Wait(TimeSpan.FromSeconds(5)))throw new Exception("Controlled asset worker did not start.");
            job.Dispose();job.Dispose();release.Set();await RejectCanceled(take);
            HostAccountingScenarios.Reject<ObjectDisposedException>(()=>job.TakeAsync());await job.WorkerCompletion;
        }
        using(var started=new ManualResetEventSlim())
        {
            int cleaned=0;
            using var job=new OwnedBackgroundAssetLoad<string>(token=>{try{started.Set();token.WaitHandle.WaitOne();token.ThrowIfCancellationRequested();return "unexpected";}finally{Interlocked.Exchange(ref cleaned,1);}});
            Task<string> take=job.TakeAsync();if(!started.Wait(TimeSpan.FromSeconds(5)))throw new Exception("Cancelable asset worker did not start.");
            job.Cancel();await RejectCanceled(take);if(cleaned!=1||!job.IsCancellationRequested)throw new Exception("Asset cancellation cleanup failed.");
        }
        using(var job=new OwnedBackgroundAssetLoad<string>(_=>throw new InvalidDataException("synthetic asset fault")))
        {
            try{await job.TakeAsync();throw new Exception("Asset worker fault was hidden.");}catch(InvalidDataException){}
        }
        using(var job=new OwnedBackgroundAssetLoad<string>(_=>null!))
        {
            try{await job.TakeAsync();throw new Exception("Null asset result accepted.");}catch(InvalidDataException){}
        }
        Console.WriteLine("PASS: owned asset worker affinity, single transfer, cancellation/cleanup, closed-owner stale result, repeated disposal and original fault type");
    }
    private static async Task RejectCanceled(Task<string> task)
    {try{await task;throw new Exception("Canceled/closed asset result published.");}catch(OperationCanceledException){}}
}
