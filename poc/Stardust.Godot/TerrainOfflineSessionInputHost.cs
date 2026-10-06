using System;
using System.Threading;
using Godot;

public sealed record TerrainOfflineSessionInputDiagnostic(string Kind,bool? RequestedPause,bool SinkMayHaveRun,
    bool SinkReturned,bool StateCommitted,string? FailureType,bool FromGodotEvent);

/// <summary>Modern keyboard session actions over borrowed state/sinks; owns no world or driver.</summary>
public partial class TerrainOfflineSessionInputHost : Node, IDisposable
{
    private readonly int ownerThread=System.Environment.CurrentManagedThreadId;
    private Node? owner,parent;
    private Viewport? viewport;
    private Func<bool>? paused;
    private Action<bool>? setPaused;
    private Action? requestStop;
    private bool configured,closed,faulted,busy,reentered,finishRequested;
    private int stateReads,accepted,handled;
    private TerrainOfflineSessionInputDiagnostic? diagnostic;
    private void EnsureOwner(){if(System.Environment.CurrentManagedThreadId!=ownerThread||OS.GetThreadCallerId()!=OS.GetMainThreadId())throw new InvalidOperationException("Session input owner/Godot main thread required.");}
    private void Enter(){EnsureOwner();if(busy){reentered=true;throw new InvalidOperationException("Session input reentry.");}busy=true;reentered=false;}
    private static bool Live(Node? node)=>GodotObject.IsInstanceValid(node)&&node!.IsInsideTree()&&!node.IsQueuedForDeletion();
    private void Guard(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if(reentered||closed||faulted||!Live(this)||!Live(owner)||!Live(parent)||owner!.GetParent()!=parent||GetParent()!=owner||
            !Live(viewport)||GetViewport()!=viewport||owner.GetViewport()!=viewport||parent!.GetViewport()!=viewport)
            throw new InvalidOperationException("Exact live owner/parent/viewport and healthy input host required.");
    }
    public bool IsConfigured {get{EnsureOwner();return configured;}}
    public bool IsClosed {get{EnsureOwner();return closed;}}
    public bool IsFaulted {get{EnsureOwner();return faulted;}}
    public bool FinishRequested {get{EnsureOwner();return finishRequested;}}
    public int StateReadCount {get{EnsureOwner();return stateReads;}}
    public int AcceptedCount {get{EnsureOwner();return accepted;}}
    public int HandledCount {get{EnsureOwner();return handled;}}
    public TerrainOfflineSessionInputDiagnostic? LastDiagnostic {get{EnsureOwner();return diagnostic;}}
    public override void _Ready(){EnsureOwner();SetProcessUnhandledInput(configured&&!closed&&!faulted&&!finishRequested);}
    public void Configure(Node owner,Func<bool> paused,Action<bool> setPaused,Action requestStop,CancellationToken cancellation=default)
    {
        Enter();try
        {
            if(configured||closed||faulted)throw new InvalidOperationException("Session input configuration accepted once.");
            ArgumentNullException.ThrowIfNull(owner);ArgumentNullException.ThrowIfNull(paused);ArgumentNullException.ThrowIfNull(setPaused);ArgumentNullException.ThrowIfNull(requestStop);
            this.owner=owner;parent=owner.GetParent();viewport=owner.GetViewport();Guard(cancellation);
            this.paused=paused;this.setPaused=setPaused;this.requestStop=requestStop;configured=true;SetProcessUnhandledInput(true);
        }catch{if(!configured){this.owner=null;parent=null;viewport=null;this.paused=null;this.setPaused=null;this.requestStop=null;}throw;}finally{busy=false;}
    }
    public bool Dispatch(InputEvent input,CancellationToken cancellation=default)
    {
        Enter();try
        {
            if(!configured||closed||faulted)throw new InvalidOperationException("Configured open session input required.");
            ArgumentNullException.ThrowIfNull(input);
            if(input is not InputEventKey key||!key.Pressed||key.Echo||key.Keycode is not (Key.P or Key.Escape)||finishRequested)return false;
            string kind=key.Keycode==Key.P?"Pause":"Finish";bool? target=null;bool started=false,returned=false;
            try
            {
                Guard(cancellation);int next=checked(accepted+1);
                if(kind=="Pause")
                {
                    stateReads=checked(stateReads+1);target=!paused!();Guard(cancellation);
                    started=true;setPaused!(target.Value);returned=true;Guard(cancellation);
                }
                else
                {
                    finishRequested=true;started=true;requestStop!();returned=true;Guard(cancellation);
                }
                accepted=next;diagnostic=new(kind,target,started,returned,true,null,false);
                if(finishRequested)SetProcessUnhandledInput(false);return true;
            }
            catch(Exception error)
            {
                faulted=true;diagnostic=new(kind,target,started,returned,false,error.GetType().FullName,false);
                CloseCore();throw;
            }
        }finally{busy=false;}
    }
    public override void _UnhandledInput(InputEvent input)
    {
        EnsureOwner();if(!configured||closed||faulted||finishRequested)return;
        try{if(!Dispatch(input))return;GetViewport().SetInputAsHandled();handled=checked(handled+1);diagnostic=diagnostic! with{FromGodotEvent=true};}
        catch(Exception error)
        {
            if(!faulted){faulted=true;diagnostic=new("Input",null,false,false,false,error.GetType().FullName,true);CloseCore();}
            else if(diagnostic is not null)diagnostic=diagnostic with{FromGodotEvent=true};
        }
    }
    private void CloseCore(bool native=true)
    {
        closed=true;paused=null;setPaused=null;requestStop=null;owner=null;parent=null;viewport=null;
        if(native&&GodotObject.IsInstanceValid(this))SetProcessUnhandledInput(false);
    }
    public void Close(){Enter();try{if(!closed)CloseCore();}finally{busy=false;}}
    public override void _ExitTree(){EnsureOwner();if(busy){reentered=true;CloseCore();return;}Close();}
    public new void Dispose()
    {
        Enter();try{if(!closed)CloseCore();}finally{busy=false;}
        // Free owns native node destruction. Its protected disposal callback must not Free again.
        if(GodotObject.IsInstanceValid(this))Free();
        base.Dispose();
    }
    void IDisposable.Dispose()=>Dispose();
    protected override void Dispose(bool disposing)
    {
        if(disposing)
        {
            EnsureOwner();
            if(busy&&!closed){reentered=true;throw new InvalidOperationException("Session input disposal reentry.");}
            if(busy)reentered=true;
        }
        CloseCore(false);
        base.Dispose(disposing);
    }
}
