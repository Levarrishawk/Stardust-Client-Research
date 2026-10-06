using System;
using System.Threading;
using Godot;

public sealed record TerrainPointTargetInputHostDiagnostic(TerrainPointTargetInputResult? Result,
    string? FailureType,long? Clock,bool FromGodotEvent);

/// <summary>One configured owner/main-thread event host; owns no adapter or simulation resources.</summary>
public partial class TerrainPointTargetInputHost : Node
{
    private readonly int ownerThread=System.Environment.CurrentManagedThreadId;
    private TerrainPointTargetInputAdapter? adapter;
    private Func<long>? clock;
    private Node? configuredParent;
    private TerrainPointTargetInputHostDiagnostic? diagnostic;
    private double maximumT;
    private bool configured,closed,faulted,busy,reentered;
    private int clockReads,accepted,handled;
    private void EnsureOwner(){if(System.Environment.CurrentManagedThreadId!=ownerThread||OS.GetThreadCallerId()!=OS.GetMainThreadId())throw new InvalidOperationException("Input host owner/Godot main thread required.");}
    private void Enter(){EnsureOwner();if(busy){reentered=true;throw new InvalidOperationException("Input host reentry.");}busy=true;reentered=false;}
    private static bool LiveNode(Node? node)=>GodotObject.IsInstanceValid(node)&&node!.IsInsideTree()&&!node.IsQueuedForDeletion();
    private void Live(Node? parent)
    {if(!LiveNode(this)||!LiveNode(parent)||GetParent()!=parent||GetViewport()!=parent!.GetViewport())throw new InvalidOperationException("Exact live same-viewport host parent required.");}
    private void Guard(){if(reentered||closed||faulted)throw new InvalidOperationException("Host callback invalidated dispatch.");Live(configuredParent);}
    public bool IsConfigured {get{EnsureOwner();return configured;}}
    public bool IsClosed {get{EnsureOwner();return closed;}}
    public bool IsFaulted {get{EnsureOwner();return faulted;}}
    public int ClockReadCount {get{EnsureOwner();return clockReads;}}
    public int AcceptedCount {get{EnsureOwner();return accepted;}}
    public int HandledCount {get{EnsureOwner();return handled;}}
    public TerrainPointTargetInputHostDiagnostic? LastDiagnostic {get{EnsureOwner();return diagnostic;}}
    public TerrainPointTargetInputResult? LastResult {get{EnsureOwner();return diagnostic?.Result;}}
    public string? LastFailure {get{EnsureOwner();return diagnostic?.FailureType;}}
    public override void _Ready(){EnsureOwner();SetProcessUnhandledInput(configured&&!closed&&!faulted);}
    public void Configure(TerrainPointTargetInputAdapter adapter,Func<long> clock,double maximumT=1000)
    {
        Enter();try
        {
            if(closed||faulted||configured)throw new InvalidOperationException("Host configuration is accepted once before terminal closure.");
            ArgumentNullException.ThrowIfNull(adapter);ArgumentNullException.ThrowIfNull(clock);
            if(!double.IsFinite(maximumT)||maximumT<0||maximumT>1e9)throw new ArgumentOutOfRangeException(nameof(maximumT));
            var parent=GetParent();Live(parent);
            if(adapter.IsDisposed)throw new ObjectDisposedException(nameof(adapter));
            this.adapter=adapter;this.clock=clock;this.maximumT=maximumT;configuredParent=parent;configured=true;SetProcessUnhandledInput(true);
        }finally{busy=false;}
    }
    private void Fail(Exception failure,TerrainPointTargetInputAdapter? child,long? readClock,bool fromEvent)
    {
        faulted=true;diagnostic=new(closed?null:child?.LastResult,failure.GetType().FullName,readClock,fromEvent);
        if(GodotObject.IsInstanceValid(this))SetProcessUnhandledInput(false);
    }
    public bool Dispatch(InputEvent input,CancellationToken cancellation=default)
    {
        Enter();try
        {
            if(closed||faulted||!configured)throw new InvalidOperationException("Configured open healthy host required.");
            ArgumentNullException.ThrowIfNull(input);
            if(input is not InputEventMouseButton button||button.ButtonIndex!=MouseButton.Left||!button.Pressed)return false;
            var child=adapter!;long? readClock=null;
            try
            {
                Guard();cancellation.ThrowIfCancellationRequested();
                if(child.DeclineWhenExhausted)
                {
                    ObjectDisposedException.ThrowIf(child.IsDisposed,child);
                    if(child.IsExhausted)return false;
                }
                clockReads=checked(clockReads+1);
                readClock=clock!();Guard();cancellation.ThrowIfCancellationRequested();
                bool admitted=child.HandleInput(input,readClock.Value,maximumT,out var result,cancellation);
                if(!admitted||result is null)throw new InvalidOperationException("Admitted left press unexpectedly ignored.");
                Guard();accepted=checked(accepted+1);diagnostic=new(result,null,readClock,false);return true;
            }
            catch(Exception failure){Fail(failure,child,readClock,false);throw;}
        }finally{busy=false;}
    }
    public override void _UnhandledInput(InputEvent input)
    {
        EnsureOwner();if(closed||faulted||!configured)return;
        try
        {
            if(!Dispatch(input))return;
            GetViewport().SetInputAsHandled();handled=checked(handled+1);
            diagnostic=diagnostic! with{FromGodotEvent=true};
        }
        catch(Exception failure)
        {
            // Engine callback boundary records one failure and disables processing; direct Dispatch rethrows.
            if(!faulted)Fail(failure,adapter,diagnostic?.Clock,true);
            else if(diagnostic is not null)diagnostic=diagnostic with{FromGodotEvent=true};
        }
    }
    private void CloseCore()
    {
        closed=true;adapter=null;clock=null;configuredParent=null;
        if(diagnostic is not null)diagnostic=diagnostic with{Result=null};
        if(GodotObject.IsInstanceValid(this))SetProcessUnhandledInput(false);
    }
    public void Close(){Enter();try{if(!closed)CloseCore();}finally{busy=false;}}
    public override void _ExitTree()
    {
        EnsureOwner();if(busy){reentered=true;CloseCore();return;}Close();
    }
}
