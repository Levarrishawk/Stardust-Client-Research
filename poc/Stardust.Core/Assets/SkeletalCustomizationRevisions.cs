using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

public sealed record SkeletalCustomizationRevision(long Number,CreatureAppearanceDescriptorBundle? Appearance,Exception? Failure);

/// <summary>One CPU composer and one replaceable pending snapshot. Only the newest completed revision can transfer.</summary>
public sealed class SkeletalCustomizationRevisions:IDisposable
{
    private readonly object gate=new();
    private readonly CreatureAppearanceDescriptorBundle source;
    private readonly Func<CreatureAppearanceDescriptorBundle,IReadOnlyDictionary<ShaderCustomizationKey,int>,CancellationToken,CreatureAppearanceDescriptorBundle> compose;
    private (long Number,IReadOnlyDictionary<ShaderCustomizationKey,int> Values)? pending;
    private CancellationTokenSource? current;
    private SkeletalCustomizationRevision? ready;
    private Task completion=Task.CompletedTask;
    private long latest;
    private bool running,disposed;
    public long Latest{get{lock(gate)return latest;}}
    public bool IsBusy{get{lock(gate)return running;}}
    public Task WorkerCompletion{get{lock(gate)return completion;}}

    public SkeletalCustomizationRevisions(CreatureAppearanceDescriptorBundle source,
        Func<CreatureAppearanceDescriptorBundle,IReadOnlyDictionary<ShaderCustomizationKey,int>,CancellationToken,CreatureAppearanceDescriptorBundle>? compose=null)
    {ArgumentNullException.ThrowIfNull(source);this.source=source;this.compose=compose??SkeletalAppearanceCustomization.Apply;}

    public long Submit(IReadOnlyDictionary<ShaderCustomizationKey,int> values)
    {
        ArgumentNullException.ThrowIfNull(values);if(values.Count>4096)throw new InvalidDataException("Customization revision value budget.");
        var snapshot=new ReadOnlyDictionary<ShaderCustomizationKey,int>(values.ToDictionary(p=>p.Key,p=>p.Value));
        CancellationTokenSource? cancel;long number;
        lock(gate)
        {
            ObjectDisposedException.ThrowIf(disposed,this);number=checked(latest+1);latest=number;pending=(number,snapshot);ready=null;cancel=current;
            if(!running){running=true;completion=Task.Run(Drain);}
        }
        Cancel(cancel);return number;
    }
    public bool TryTake(out SkeletalCustomizationRevision? revision)
    {
        lock(gate)
        {
            ObjectDisposedException.ThrowIf(disposed,this);revision=ready;ready=null;
            return revision is not null&&revision.Number==latest;
        }
    }
    private void Drain()
    {
        while(true)
        {
            (long Number,IReadOnlyDictionary<ShaderCustomizationKey,int> Values) request;CancellationTokenSource cancellation;
            lock(gate)
            {
                if(disposed||pending is null){running=false;return;}
                request=pending.Value;pending=null;current=cancellation=new();
            }
            SkeletalCustomizationRevision? result=null;
            try
            {
                var appearance=compose(source,request.Values,cancellation.Token)??throw new InvalidDataException("Customization composer returned no appearance.");
                result=new(request.Number,appearance,null);
            }
            catch(OperationCanceledException)when(cancellation.IsCancellationRequested){}
            catch(Exception failure){result=new(request.Number,null,failure);}
            lock(gate)
            {
                if(!disposed&&latest==request.Number&&!cancellation.IsCancellationRequested)ready=result;
                if(ReferenceEquals(current,cancellation))current=null;
            }
            cancellation.Dispose();
        }
    }
    private static void Cancel(CancellationTokenSource? source)
    {try{source?.Cancel();}catch(ObjectDisposedException){/* Completion won the cancellation race. */}}
    public void Dispose()
    {
        CancellationTokenSource? cancel;
        lock(gate){if(disposed)return;disposed=true;pending=null;ready=null;cancel=current;}
        Cancel(cancel);
    }
}
