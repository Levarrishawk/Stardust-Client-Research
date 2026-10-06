using System;
using System.Threading;
using Godot;

public sealed record TerrainOfflineFocusDiagnostic(string Kind,bool? PriorPaused,bool GameplayMayHaveRun,
    bool GameplayReturned,bool PauseMayHaveRun,bool PauseReturned,bool StateCommitted,string? FailureType,bool FromWindowSignal);

/// <summary>Modern focus pause/input policy over borrowed callbacks; owns only its connections/node.</summary>
public partial class TerrainOfflineFocusHost : Node, IDisposable
{
    private readonly int ownerThread=System.Environment.CurrentManagedThreadId;
    private Node? owner,parent;
    private Viewport? viewport;
    private Window? window;
    private Func<bool>? pausedSource;
    private Action<bool>? pauseSink,gameplaySink;
    private CancellationToken lifetime;
    private bool configured,closed,faulted,busy,reentered,suspended,enteredConnected,exitedConnected;
    private bool? priorPaused;
    private int reads,gameplayCalls,pauseCalls,transitions;
    private TerrainOfflineFocusDiagnostic? diagnostic;
    private void EnsureOwner(){if(System.Environment.CurrentManagedThreadId!=ownerThread||OS.GetThreadCallerId()!=OS.GetMainThreadId())throw new InvalidOperationException("Focus owner/Godot main thread required.");}
    private void Enter(){EnsureOwner();if(busy){reentered=true;throw new InvalidOperationException("Focus host reentry.");}busy=true;reentered=false;}
    private static bool Live(Node? node)=>GodotObject.IsInstanceValid(node)&&node!.IsInsideTree()&&!node.IsQueuedForDeletion();
    private void Guard(CancellationToken token)
    {
        lifetime.ThrowIfCancellationRequested();token.ThrowIfCancellationRequested();
        if(reentered||closed||faulted||!Live(this)||!Live(owner)||!Live(parent)||!Live(viewport)||!Live(window)||
            GetParent()!=owner||owner!.GetParent()!=parent||GetViewport()!=viewport||owner.GetViewport()!=viewport||parent!.GetViewport()!=viewport||
            GetWindow()!=window||owner.GetWindow()!=window||parent.GetWindow()!=window)
            throw new InvalidOperationException("Exact live owner/parent/viewport/window and healthy focus host required.");
    }
    public bool IsConfigured {get{EnsureOwner();return configured;}}
    public bool IsClosed {get{EnsureOwner();return closed;}}
    public bool IsFaulted {get{EnsureOwner();return faulted;}}
    public bool IsSuspended {get{EnsureOwner();return suspended;}}
    public bool? PriorPaused {get{EnsureOwner();return priorPaused;}}
    public int StateReadCount {get{EnsureOwner();return reads;}}
    public int GameplayCallCount {get{EnsureOwner();return gameplayCalls;}}
    public int PauseCallCount {get{EnsureOwner();return pauseCalls;}}
    public int TransitionCount {get{EnsureOwner();return transitions;}}
    public int OwnConnectionCount {get{EnsureOwner();return (enteredConnected?1:0)+(exitedConnected?1:0);}}
    public TerrainOfflineFocusDiagnostic? LastDiagnostic {get{EnsureOwner();return diagnostic;}}
    public void Configure(Node owner,Func<bool> pausedSource,Action<bool> pauseSink,Action<bool> gameplayEnabledSink,CancellationToken cancellation=default)
    {
        Enter();try
        {
            if(configured||closed||faulted)throw new InvalidOperationException("Focus configuration accepted once.");
            ArgumentNullException.ThrowIfNull(owner);ArgumentNullException.ThrowIfNull(pausedSource);ArgumentNullException.ThrowIfNull(pauseSink);ArgumentNullException.ThrowIfNull(gameplayEnabledSink);
            this.owner=owner;parent=owner.GetParent();viewport=owner.GetViewport();window=owner.GetWindow();lifetime=cancellation;Guard(default);
            this.pausedSource=pausedSource;this.pauseSink=pauseSink;gameplaySink=gameplayEnabledSink;
            window!.FocusExited+=OnFocusExited;exitedConnected=true;window.FocusEntered+=OnFocusEntered;enteredConnected=true;configured=true;
        }catch{if(!configured)Release(true);throw;}finally{busy=false;}
    }
    public bool HandleFocus(bool focused,CancellationToken cancellation=default)
    {
        Enter();try
        {
            if(!configured||closed||faulted)throw new InvalidOperationException("Configured open focus host required.");
            if(focused==!suspended)return false;
            string kind=focused?"Gain":"Loss";bool? prior=priorPaused;bool gameStarted=false,gameReturned=false,pauseStarted=false,pauseReturned=false;
            try
            {
                Guard(cancellation);int next=checked(transitions+1);
                if(!focused){reads=checked(reads+1);prior=pausedSource!();Guard(cancellation);priorPaused=prior;}
                gameplayCalls=checked(gameplayCalls+1);gameStarted=true;gameplaySink!(focused);gameReturned=true;Guard(cancellation);
                pauseCalls=checked(pauseCalls+1);pauseStarted=true;pauseSink!(focused?prior!.Value:true);pauseReturned=true;Guard(cancellation);
                suspended=!focused;transitions=next;diagnostic=new(kind,prior,gameStarted,gameReturned,pauseStarted,pauseReturned,true,null,false);return true;
            }
            catch(Exception error){faulted=true;diagnostic=new(kind,prior,gameStarted,gameReturned,pauseStarted,pauseReturned,false,error.GetType().FullName,false);CloseCore(true);throw;}
        }finally{busy=false;}
    }
    private void OnFocusExited()=>Signal(false);
    private void OnFocusEntered()=>Signal(true);
    private void Signal(bool focused)
    {
        if(closed||faulted||!configured)return;
        try{if(HandleFocus(focused))diagnostic=diagnostic! with{FromWindowSignal=true};}
        catch(Exception error)
        {
            if(!faulted){faulted=true;diagnostic=new(focused?"Gain":"Loss",priorPaused,false,false,false,false,false,error.GetType().FullName,true);CloseCore(true);}
            else if(diagnostic is not null)diagnostic=diagnostic with{FromWindowSignal=true};
        }
    }
    private void Release(bool native)
    {
        if(native&&GodotObject.IsInstanceValid(window))
        {
            if(exitedConnected)window!.FocusExited-=OnFocusExited;
            if(enteredConnected)window!.FocusEntered-=OnFocusEntered;
        }
        enteredConnected=exitedConnected=false;owner=parent=null;viewport=null;window=null;pausedSource=null;pauseSink=gameplaySink=null;lifetime=default;
    }
    private void CloseCore(bool native){closed=true;Release(native);}
    public void Close(){Enter();try{if(!closed)CloseCore(true);}finally{busy=false;}}
    public override void _ExitTree(){EnsureOwner();if(busy){reentered=true;CloseCore(true);return;}Close();}
    public new void Dispose(){Enter();try{if(!closed)CloseCore(true);}finally{busy=false;}if(GodotObject.IsInstanceValid(this))Free();base.Dispose();}
    void IDisposable.Dispose()=>Dispose();
    protected override void Dispose(bool disposing)
    {
        if(disposing){EnsureOwner();if(busy&&!closed){reentered=true;throw new InvalidOperationException("Focus disposal reentry.");}if(busy)reentered=true;}
        CloseCore(false);base.Dispose(disposing);
    }
}
