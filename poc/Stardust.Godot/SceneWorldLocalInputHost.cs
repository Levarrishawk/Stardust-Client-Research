using System;
using System.Collections.Generic;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Session;

/// <summary>Timestamped planar key adapter; caller advances driver before presentation Step.</summary>
public partial class SceneWorldLocalInputHost : Node, IDisposable
{
    private readonly int owner = System.Environment.CurrentManagedThreadId;
    private readonly Func<long> clock;
    private readonly HashSet<Key> held = new();
    private ISceneWorldLocalMotionDriver? driver;
    private bool configured, closed, busy, focused=true, paused;
    private long timestamp;
    private ActorViewOrientation orientation = new();
    public Exception? Failure { get; private set; }
    public SceneWorldLocalMotionDriverState? LastState { get; private set; }
    public SceneWorldLocalInputHost() : this(()=>checked((long)Time.GetTicksUsec())) { }
    public SceneWorldLocalInputHost(Func<long> clock) { ArgumentNullException.ThrowIfNull(clock); this.clock=clock; }
    private void OwnerThread(){if(System.Environment.CurrentManagedThreadId!=owner || OS.GetThreadCallerId()!=OS.GetMainThreadId())throw new InvalidOperationException("Input owner/main thread required.");}
    private void Enter(){OwnerThread();if(busy)throw new InvalidOperationException("Input callback reentry.");if(closed)throw new ObjectDisposedException(nameof(SceneWorldLocalInputHost));busy=true;}
    private long Timestamp(long value){if(value<0 || value<timestamp)throw new ArgumentOutOfRangeException(nameof(value),"Monotonic nonnegative microseconds required.");return value;}
    public void Configure(ISceneWorldLocalMotionDriver driver)
    {
        Enter();try
        {
            ArgumentNullException.ThrowIfNull(driver);if(configured || !IsInsideTree() || GetParent() is not { } parent || !GodotObject.IsInstanceValid(parent) || !parent.IsInsideTree())throw new InvalidOperationException("Configure once with live parent.");
            var state=driver.State??throw new ArgumentException("Driver state required.");long now=clock();if(state.TimestampMicroseconds<0 || now<state.TimestampMicroseconds)throw new ArgumentException("Clock precedes driver state.");
            SetProcessUnhandledInput(true);this.driver=driver;timestamp=state.TimestampMicroseconds;LastState=state;configured=true;
        }finally{busy=false;}
    }
    private PortalActorInput Input()=>PlanarActorInputMapping.Create(orientation.PlanarForward(System.Numerics.Vector3.UnitZ),
        (held.Contains(Key.W)?1:0)-(held.Contains(Key.S)?1:0),(held.Contains(Key.D)?1:0)-(held.Contains(Key.A)?1:0),held.Contains(Key.Shift)?6:2,null);
    private void Submit(long now){driver!.Submit(now,Input());timestamp=now;}
    private void RequireDriver(){if(!configured || driver is null)throw new InvalidOperationException("Configure driver first.");}
    private void Release(long now){if(held.Count==0)return;held.Clear();Submit(now);}
    public bool HandleInput(InputEvent input)
    {
        Enter();try
        {
            if(!configured || input is not InputEventKey key || key.Echo || (key.Pressed && (key.AltPressed || key.CtrlPressed || key.MetaPressed)))return false;
            Key code=key.PhysicalKeycode!=Key.None?key.PhysicalKeycode:key.Keycode;
            if(code is not (Key.W or Key.A or Key.S or Key.D or Key.Shift) || paused || !focused)return false;
            bool changed=key.Pressed?held.Add(code):held.Remove(code);if(!changed)return false;
            Submit(Timestamp(clock()));return true;
        }catch(Exception error){Fail(error);throw;}finally{busy=false;}
    }
    public SceneWorldLocalMotionDriverState Advance(long timestampMicroseconds,bool active=true)
    {
        Enter();try
        {
            if(!configured)throw new InvalidOperationException("Driver required.");long now=Timestamp(timestampMicroseconds);bool enabled=active && focused && !paused;
            if(!enabled)Release(now);
            LastState=driver!.Advance(now,enabled);timestamp=now;return LastState;
        }catch(Exception error){Fail(error);throw;}finally{busy=false;}
    }
    public void SetPaused(bool value)=>Suspend(value,focused);
    public void SetFocused(bool value)=>Suspend(paused,value);
    private void Suspend(bool pause,bool focus)
    {
        Enter();try{RequireDriver();if(paused==pause && focused==focus)return;long now=Timestamp(clock());Release(now);LastState=driver!.Advance(now,false);timestamp=now;paused=pause;focused=focus;}
        catch(Exception error){Fail(error);throw;}finally{busy=false;}
    }
    public void SetOrientation(ActorViewOrientation value)
    {
        Enter();try{RequireDriver();ArgumentNullException.ThrowIfNull(value);orientation=value;Submit(Timestamp(clock()));}
        catch(Exception error){Fail(error);throw;}finally{busy=false;}
    }
    public void Reset(long timestampMicroseconds)
    {
        Enter();try{RequireDriver();long now=Timestamp(timestampMicroseconds);held.Clear();orientation=new();driver!.Reset(now);timestamp=now;LastState=driver.State;}
        catch(Exception error){Fail(error);throw;}finally{busy=false;}
    }
    private void Fail(Exception error){Failure=error;try{CloseOwned();}catch(Exception close){Failure=new AggregateException(error,close);}}
    private void CloseOwned()
    {
        if(closed)return;closed=true;SetProcessUnhandledInput(false);var owned=driver;driver=null;held.Clear();
        if(owned is not null)
        {
            try{long now=Timestamp(clock());owned.Submit(now,new(System.Numerics.Vector3.Zero,null));LastState=owned.Advance(now,false);timestamp=now;}
            finally{owned.Dispose();}
        }
    }
    public void Close(){OwnerThread();if(busy)throw new InvalidOperationException("Input callback reentry.");CloseOwned();}
    public new void Dispose(){Close();base.Dispose();}
    public override void _EnterTree(){OwnerThread();SetProcessUnhandledInput(configured && !closed);}
    public override void _UnhandledInput(InputEvent input){try{HandleInput(input);}catch(Exception error){Failure??=error;}}
    public override void _Notification(int what)
    {
        if(!configured || closed)return;
        try{if(what==NotificationWMWindowFocusOut)SetFocused(false);else if(what==NotificationWMWindowFocusIn)SetFocused(true);else if(what==NotificationPaused)SetPaused(true);else if(what==NotificationUnpaused)SetPaused(false);}
        catch(Exception error){Failure??=error;}
    }
    public override void _ExitTree(){OwnerThread();try{CloseOwned();}catch(Exception error){Failure??=error;}}
}
