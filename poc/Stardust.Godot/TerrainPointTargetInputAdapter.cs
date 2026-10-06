using System;
using System.Threading;
using Godot;
using Stardust.Core.Session;

public sealed record TerrainPointTargetInputResult(TerrainPointTargetInputAdapter Owner,int Attempt,
    long TimestampMicroseconds,int PickerAttemptsBefore,int PickerAttemptsAfter,
    TerrainCameraPickReceipt? Pick,SceneWorldTerrainPointTargetReceipt? Target,
    SceneWorldTerrainPointTargetReceipt? PriorTarget,string? FailureType);

/// <summary>Owner-thread two-stage input adapter; owns diagnostics only, never its borrowers.</summary>
public sealed class TerrainPointTargetInputAdapter : IDisposable
{
    private readonly int ownerThread=System.Environment.CurrentManagedThreadId;
    private readonly int maximumAttempts;
    private readonly bool declineWhenExhausted;
    private TerrainCameraPicker? picker;
    private SceneWorldTerrainPointTargetDriver? controller;
    private bool busy,reentered,disposed;
    public int Attempts {get;private set;}
    public TerrainPointTargetInputResult? LastResult {get;private set;}
    public bool IsDisposed=>disposed;
    public int MaximumAttempts {get{Owner();return maximumAttempts;}}
    public int RemainingAttempts {get{Owner();return maximumAttempts-Attempts;}}
    public bool IsExhausted {get{Owner();return Attempts>=maximumAttempts;}}
    public bool DeclineWhenExhausted {get{Owner();return declineWhenExhausted;}}
    public TerrainPointTargetInputAdapter(TerrainCameraPicker picker,SceneWorldTerrainPointTargetDriver controller,int maximumAttempts=16,TerrainOfflineSessionBudget? budget=null,bool declineWhenExhausted=false)
    {
        Owner();ArgumentNullException.ThrowIfNull(picker);ArgumentNullException.ThrowIfNull(controller);
        if(maximumAttempts<1||maximumAttempts>(budget?.MaximumTargets??16))throw new ArgumentOutOfRangeException(nameof(maximumAttempts));
        if(picker.IsDisposed||!ReferenceEquals(picker.Query,controller.Query))throw new ArgumentException("Exact live borrower query binding required.");
        controller.ValidateTargetAdmission(controller.State.DriverResult.TimestampMicroseconds);
        picker.ValidateAdmission(Vector2.Zero,0);
        this.picker=picker;this.controller=controller;this.maximumAttempts=maximumAttempts;this.declineWhenExhausted=declineWhenExhausted;
    }
    private void Owner(){if(System.Environment.CurrentManagedThreadId!=ownerThread||OS.GetThreadCallerId()!=OS.GetMainThreadId())throw new InvalidOperationException("Adapter owner/Godot main thread required.");}
    private void Enter(){Owner();if(busy){reentered=true;throw new InvalidOperationException("Adapter reentry.");}busy=true;reentered=false;}
    private void Guard(){if(reentered)throw new InvalidOperationException("Caught adapter reentry invalidates outer target submission.");}
    public bool HandleInput(InputEvent input,long timestampMicroseconds,double maximumT,out TerrainPointTargetInputResult? result,CancellationToken cancellation=default)
    {
        Enter();try
        {
            ObjectDisposedException.ThrowIf(disposed,this);ArgumentNullException.ThrowIfNull(input);result=null;
            if(input is not InputEventMouseButton button||button.ButtonIndex!=MouseButton.Left||!button.Pressed)return false;
            cancellation.ThrowIfCancellationRequested();
            if(Attempts>=maximumAttempts){if(declineWhenExhausted)return false;throw new InvalidOperationException("Adapter attempt cap.");}
            var p=picker!;var c=controller!;
            if(!ReferenceEquals(p.Query,c.Query))throw new InvalidOperationException("Borrower query binding retired.");
            c.ValidateTargetAdmission(timestampMicroseconds,cancellation);
            p.ValidateAdmission(button.Position,maximumT,cancellation);Guard();
            var prior=c.Target;int before=p.Attempts;int attempt=checked(++Attempts);
            TerrainCameraPickReceipt? pick=null;SceneWorldTerrainPointTargetReceipt? target=null;
            try
            {
                if(!p.HandleInput(input,maximumT,out pick,cancellation))throw new InvalidOperationException("Admitted left press unexpectedly ignored.");
                Guard();cancellation.ThrowIfCancellationRequested();
                if(!p.IsCurrent(pick))throw new InvalidOperationException("Issued pick invalidated before target submission.");
                c.ValidateTargetAdmission(timestampMicroseconds,cancellation);Guard();
                if(pick!.Hit is { } hit)target=c.SubmitTarget(hit,timestampMicroseconds,cancellation);
                result=LastResult=new(this,attempt,timestampMicroseconds,before,p.Attempts,pick,target,prior,null);return true;
            }
            catch(Exception e)
            {
                // Preserve the exact committed picker prefix even if its return was interrupted.
                if(pick is null&&p.Current is { } current&&current.Attempt>before)pick=current;
                result=LastResult=new(this,attempt,timestampMicroseconds,before,p.Attempts,pick,target,prior,e.GetType().FullName);throw;
            }
        }finally{busy=false;}
    }
    public bool IsCurrent(TerrainPointTargetInputResult? result)
    {
        Owner();if(disposed||result is null||!ReferenceEquals(result,LastResult)||!ReferenceEquals(result.Owner,this)||result.FailureType is not null)return false;
        if(!picker!.IsCurrent(result.Pick))return false;
        if(controller!.State.Mode is SceneWorldTerrainPointTargetMode.Disposed or SceneWorldTerrainPointTargetMode.Faulted or SceneWorldTerrainPointTargetMode.Unavailable)return false;
        var selected=result.Target??result.PriorTarget;
        if(selected is null)
        {try{controller.ValidateTargetAdmission(controller.State.DriverResult.TimestampMicroseconds);}
         catch(Exception e) when(e is InvalidOperationException or ArgumentException){return false;}}
        return selected is null?controller.Target is null:controller.IsCurrent(selected);
    }
    public void Dispose(){Enter();try{if(disposed)return;disposed=true;LastResult=null;picker=null;controller=null;}finally{busy=false;}}
}
