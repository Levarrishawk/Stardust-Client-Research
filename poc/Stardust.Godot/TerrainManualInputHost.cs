using System;
using System.Threading;
using Godot;
using NativeVector2=System.Numerics.Vector2;

public sealed record TerrainManualInputDiagnostic(NativeVector2 Direction,byte ProposedHeldMask,
    long? TimestampMicroseconds,string Kind,bool SinkInvoked,bool SinkReturned,bool StateCommitted,
    string? FailureType,bool FromGodotEvent);

/// <summary>Modern unhandled physical WASD intent; borrows a clock reader and native intent sink.</summary>
public partial class TerrainManualInputHost : Node
{
    private readonly int ownerThread=System.Environment.CurrentManagedThreadId;
    private Func<long>? clock;
    private Action<NativeVector2,long>? sink;
    private Node? configuredParent;
    private TerrainManualInputDiagnostic? diagnostic;
    private byte held;
    private bool configured,enabled,closed,faulted,busy,reentered;
    private int accepted,handled,clockReads,sinkCalls;
    private void EnsureOwner(){if(System.Environment.CurrentManagedThreadId!=ownerThread||OS.GetThreadCallerId()!=OS.GetMainThreadId())throw new InvalidOperationException("Manual host owner/Godot main thread required.");}
    private void Enter(){EnsureOwner();if(busy){reentered=true;throw new InvalidOperationException("Manual input reentry.");}busy=true;reentered=false;}
    private static NativeVector2 Direction(byte keys)=>new(((keys&8)!=0?1:0)-((keys&4)!=0?1:0),((keys&2)!=0?1:0)-((keys&1)!=0?1:0));
    private static bool LiveNode(Node? node)=>GodotObject.IsInstanceValid(node)&&node!.IsInsideTree()&&!node.IsQueuedForDeletion();
    private void Live(Node? parent){if(!LiveNode(this)||!LiveNode(parent)||GetParent()!=parent||GetViewport()!=parent!.GetViewport())throw new InvalidOperationException("Exact live same-viewport host parent required.");}
    private void Guard(){if(reentered||closed||faulted)throw new InvalidOperationException("Callback invalidated manual transition.");Live(configuredParent);}
    private void Open(){if(!configured||closed||faulted)throw new InvalidOperationException("Configured open healthy manual host required.");}
    public NativeVector2 CurrentInput {get{EnsureOwner();return Direction(held);}}
    public byte HeldKeyMask {get{EnsureOwner();return held;}}
    public bool IsEnabled {get{EnsureOwner();return enabled&&!closed&&!faulted;}}
    public bool IsConfigured {get{EnsureOwner();return configured;}}
    public bool IsClosed {get{EnsureOwner();return closed;}}
    public bool IsFaulted {get{EnsureOwner();return faulted;}}
    public int AcceptedCount {get{EnsureOwner();return accepted;}}
    public int HandledCount {get{EnsureOwner();return handled;}}
    public int ClockReadCount {get{EnsureOwner();return clockReads;}}
    public int SinkCallCount {get{EnsureOwner();return sinkCalls;}}
    public TerrainManualInputDiagnostic? LastDiagnostic {get{EnsureOwner();return diagnostic;}}
    public string? LastFailure {get{EnsureOwner();return diagnostic?.FailureType;}}
    public override void _Ready(){EnsureOwner();SetProcessUnhandledKeyInput(configured&&IsEnabled);}
    public void Configure(Func<long> clock,Action<NativeVector2,long> sink)
    {
        Enter();try
        {
            if(configured||closed||faulted)throw new InvalidOperationException("Configuration accepted once before closure.");
            ArgumentNullException.ThrowIfNull(clock);ArgumentNullException.ThrowIfNull(sink);var parent=GetParent();Live(parent);
            this.clock=clock;this.sink=sink;configuredParent=parent;configured=true;enabled=true;SetProcessUnhandledKeyInput(true);
        }finally{busy=false;}
    }
    private void Fault(Exception failure,NativeVector2 direction,byte proposed,long? timestamp,string kind,bool invoked,bool returned,bool fromEvent)
    {
        faulted=true;diagnostic=new(direction,proposed,timestamp,kind,invoked,returned,false,failure.GetType().FullName,fromEvent);
        if(GodotObject.IsInstanceValid(this))SetProcessUnhandledKeyInput(false);
    }
    private void Submit(byte proposed,string kind,CancellationToken cancellation)
    {
        NativeVector2 direction=Direction(proposed);long? timestamp=null;bool invoked=false,returned=false;
        try
        {
            Guard();cancellation.ThrowIfCancellationRequested();int nextAccepted=checked(accepted+1);
            clockReads=checked(clockReads+1);timestamp=clock!();Guard();cancellation.ThrowIfCancellationRequested();
            sinkCalls=checked(sinkCalls+1);invoked=true;sink!(direction,timestamp.Value);returned=true;
            Guard();cancellation.ThrowIfCancellationRequested();
            held=proposed;accepted=nextAccepted;diagnostic=new(direction,proposed,timestamp,kind,true,true,true,null,false);
        }
        catch(Exception failure){Fault(failure,direction,proposed,timestamp,kind,invoked,returned,false);throw;}
    }
    public bool Dispatch(InputEvent input,CancellationToken cancellation=default)
    {
        Enter();try
        {
            Open();ArgumentNullException.ThrowIfNull(input);
            if(!enabled||input is not InputEventKey key||key.Echo)return false;
            Key code=key.PhysicalKeycode==Key.None?key.Keycode:key.PhysicalKeycode;
            byte bit=code switch{Key.W=>1,Key.S=>2,Key.A=>4,Key.D=>8,_=>0};if(bit==0)return false;
            byte proposed=key.Pressed?(byte)(held|bit):(byte)(held&~bit);if(proposed==held)return false;
            Submit(proposed,"Key",cancellation);return true;
        }finally{busy=false;}
    }
    private bool EnableCore(bool value,string kind,CancellationToken cancellation)
    {
        Open();if(enabled==value)return false;Guard();cancellation.ThrowIfCancellationRequested();
        if(!value&&Direction(held)!=NativeVector2.Zero)Submit(0,kind,cancellation);
        held=0;enabled=value;SetProcessUnhandledKeyInput(value);return true;
    }
    public bool SetEnabled(bool enabled,CancellationToken cancellation=default)
    {Enter();try
      {Open();if(this.enabled==enabled)return false;
       try{return EnableCore(enabled,"Disabled",cancellation);}
       catch(Exception failure){if(!faulted)Fault(failure,NativeVector2.Zero,0,null,"Disabled",false,false,false);throw;}}
     finally{busy=false;}}
    public override void _UnhandledKeyInput(InputEvent input)
    {
        EnsureOwner();if(!IsEnabled||!configured)return;
        try
        {
            if(!Dispatch(input))return;GetViewport().SetInputAsHandled();handled=checked(handled+1);
            diagnostic=diagnostic! with{FromGodotEvent=true};
        }
        catch(Exception failure)
        {
            if(!faulted)Fault(failure,Direction(held),held,null,"KeyEvent",false,false,true);
            else if(diagnostic is not null)diagnostic=diagnostic with{FromGodotEvent=true};
        }
    }
    public override void _Notification(int what)
    {
        if(what!=(int)NotificationWMWindowFocusOut)return;
        EnsureOwner();if(!configured||closed||faulted||!enabled)return;
        try{Enter();try{EnableCore(false,"FocusLost",default);}finally{busy=false;}}
        catch(Exception failure)
        {
            if(!faulted)Fault(failure,Direction(held),held,null,"FocusLost",false,false,true);
            else if(diagnostic is not null)diagnostic=diagnostic with{FromGodotEvent=true};
        }
    }
    private void CloseCore(){closed=true;enabled=false;held=0;clock=null;sink=null;configuredParent=null;if(GodotObject.IsInstanceValid(this))SetProcessUnhandledKeyInput(false);}
    public void Close(){Enter();try{if(!closed)CloseCore();}finally{busy=false;}}
    public override void _ExitTree(){EnsureOwner();if(busy){reentered=true;CloseCore();return;}Close();}
}
