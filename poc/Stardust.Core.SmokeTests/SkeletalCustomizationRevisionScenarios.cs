using Stardust.Core.Assets;

internal static class SkeletalCustomizationRevisionScenarios
{
    internal static async Task RunAsync()
    {
        var bundle=SkeletalCustomizationScenarios.Fixture();Dictionary<ShaderCustomizationKey,int> Values(int value)=>new(){{new("skin",false),value}};
        using(var started=new ManualResetEventSlim())using(var release=new ManualResetEventSlim())
        {
            int calls=0,active=0,maximum=0;
            using var queue=new SkeletalCustomizationRevisions(bundle,(source,values,token)=>
            {
                int concurrency=Interlocked.Increment(ref active);maximum=Math.Max(maximum,concurrency);int call=Interlocked.Increment(ref calls);
                try{if(call==1){started.Set();if(!release.Wait(TimeSpan.FromSeconds(5)))throw new Exception("Controlled composer release timeout.");}return SkeletalAppearanceCustomization.Apply(source,values);}
                finally{Interlocked.Decrement(ref active);}
            });
            queue.Submit(Values(0));if(!started.Wait(TimeSpan.FromSeconds(5)))throw new Exception("Customization composer did not start.");
            queue.Submit(Values(0));queue.Submit(Values(0));var supplied=Values(1);long newest=queue.Submit(supplied);supplied.Clear();release.Set();await queue.WorkerCompletion;
            if(calls!=2||maximum!=1||!queue.TryTake(out var result)||result!.Number!=newest||result.Failure is not null||result.Appearance!.CustomizationValues![new("skin",false)]!=1||queue.TryTake(out _))throw new Exception("Latest revision/coalescing/snapshot/single transfer failed.");
            queue.Submit(new Dictionary<ShaderCustomizationKey,int>());await queue.WorkerCompletion;
            if(!queue.TryTake(out result)||result!.Appearance is not null||result.Failure is not InvalidDataException)throw new Exception("Current failure did not transfer explicitly.");
            queue.Submit(Values(0));await queue.WorkerCompletion;if(!queue.TryTake(out result)||result!.Failure is not null)throw new Exception("Revision queue did not recover after failure.");
            queue.Submit(Values(1));await queue.WorkerCompletion;queue.Submit(Values(0));await queue.WorkerCompletion;
            if(!queue.TryTake(out result)||result!.Appearance!.CustomizationValues![new("skin",false)]!=0)throw new Exception("Completed stale revision was retained.");
        }
        using(var started=new ManualResetEventSlim())using(var release=new ManualResetEventSlim())
        {
            using var queue=new SkeletalCustomizationRevisions(bundle,(source,values,token)=>{started.Set();if(!release.Wait(TimeSpan.FromSeconds(5)))throw new Exception("Disposal fixture timeout.");return SkeletalAppearanceCustomization.Apply(source,values);});
            queue.Submit(Values(0));if(!started.Wait(TimeSpan.FromSeconds(5)))throw new Exception("Disposal composer did not start.");queue.Dispose();queue.Dispose();release.Set();await queue.WorkerCompletion;
            HostAccountingScenarios.Reject<ObjectDisposedException>(()=>queue.TryTake(out _));HostAccountingScenarios.Reject<ObjectDisposedException>(()=>queue.Submit(Values(0)));
        }
        Console.WriteLine("PASS: one customization CPU composer/latest pending snapshot, stale completion rejection, single transfer, owned values, explicit failure/recovery and closed-owner disposal");
    }
}
