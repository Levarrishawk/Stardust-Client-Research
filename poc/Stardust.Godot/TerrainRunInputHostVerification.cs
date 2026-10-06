using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;

/// <summary>Actual viewport/GUI/native-notification test over independent authored key timeline.</summary>
public partial class TerrainRunInputHostVerification : Node
{
    private static void Need(bool value,string why){if(!value)throw new InvalidOperationException(why);}
    public override async void _Ready(){try{await Run();GetTree().Quit();}catch(Exception e){GD.PushError("Manual host: "+e);GetTree().Quit(1);}}
    private async Task Run()
    {
        string[] args=OS.GetCmdlineUserArgs();string Arg(string name){int i=Array.IndexOf(args,name);Need(i>=0,"Explicit argument "+name);return args[i+1];}
        string output=Path.GetFullPath(Arg("--out")),repository=Path.GetFullPath(Arg("--repository"));Need(output.StartsWith(Path.Combine(repository,"local-private")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)&&!File.Exists(output),"Fresh private receipt");
        using var doc=JsonDocument.Parse(File.ReadAllText(Arg("--fixtures")));int checks=0;void Check(bool value,string why){Need(value,why);checks++;}
        void Reject(Action action){try{action();}catch(Exception e) when(e is InvalidOperationException or ArgumentException or OperationCanceledException or InvalidDataException){checks++;return;}throw new InvalidOperationException("Expected rejection");}
        InputEventKey KeyEvent(Key physical,bool pressed=true,Key? logical=null,bool echo=false,KeyLocation location=KeyLocation.Left)=>new(){PhysicalKeycode=physical,Keycode=logical??physical,Pressed=pressed,Echo=echo,Location=location};
        var probe=new TerrainRunPropagationProbe();AddChild(probe);var parent=new Node();AddChild(parent);var host=new TerrainRunInputHost();parent.AddChild(host);
        var gui=new TerrainRunGuiConsumer{FocusMode=Control.FocusModeEnum.All,MouseFilter=Control.MouseFilterEnum.Ignore,Position=new(8,8),Size=new(80,30)};AddChild(gui);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        long clock=0;string kind="Key";var rows=new List<object>();var sinks=new List<object>();bool externalIntent=false;
        host.Configure(()=>clock,(direction,time)=>{externalIntent=direction;sinks.Add(new{Running=direction,Clock=time,Kind=kind});});
        Check(host.IsConfigured&&host.IsEnabled&&!host.IsRunning&&host.ClockReadCount==0&&host.SinkCallCount==0,"Configure invokes neither clock nor sink.");
        Reject(()=>host.Configure(()=>0,(_,_)=>{}));
        bool Push(InputEvent input){int before=probe.Count;GetViewport().PushInput(input,true);return probe.Count==before;}
        int index=0;bool guiConsumed=false,focusStopped=false;
        foreach(var a in doc.RootElement.GetProperty("Actions").EnumerateArray())
        {
            clock=(index+1)*1000;string type=a.GetProperty("Type").GetString()!;kind=type=="FocusOut"?"FocusLost":type=="Enable"?"Disabled":"Key";bool? consumed=null;int priorSink=host.SinkCallCount,priorAccepted=host.AcceptedCount;
            switch(type)
            {
                case "Key":
                    using(var e=KeyEvent(Enum.Parse<Key>(a.GetProperty("Physical").GetString()!),a.GetProperty("Pressed").GetBoolean(),Enum.Parse<Key>(a.GetProperty("Logical").GetString()!),a.GetProperty("Echo").GetBoolean(),Enum.Parse<KeyLocation>(a.GetProperty("Location").GetString()!)))
                    {consumed=Push(e);if(gui.Capture)guiConsumed|=consumed.Value&&host.SinkCallCount==priorSink;}
                    break;
                case "Gui":gui.Capture=a.GetProperty("Enabled").GetBoolean();if(gui.Capture){gui.GrabFocus();Check(GetViewport().GuiGetFocusOwner()==gui,"Actual Control keyboard focus.");}else gui.ReleaseFocus();break;
                case "Enable":host.SetEnabled(a.GetProperty("Enabled").GetBoolean());break;
                case "FocusOut":GetWindow().PropagateNotification((int)NotificationWMWindowFocusOut);focusStopped|=!host.IsEnabled&&!host.IsRunning;break;
                case "FocusIn":GetWindow().PropagateNotification((int)NotificationWMWindowFocusIn);break;
                case "Motion":using(var e=new InputEventMouseMotion{Position=new(200,100)})consumed=Push(e);break;
                default:throw new InvalidDataException("Unknown authored action");
            }
            Check(!host.IsFaulted,"Healthy timeline remains open.");
            if(host.AcceptedCount!=priorAccepted)
            {
                Check(host.LastDiagnostic is {StateCommitted:true,FailureType:null}&&host.LastDiagnostic.Kind==kind&&host.LastDiagnostic.Running==host.IsRunning,"Actual committed mask diagnostic.");
                if(host.SinkCallCount!=priorSink)Check(host.LastDiagnostic is {SinkInvoked:true,SinkReturned:true}&&host.LastDiagnostic.TimestampMicroseconds==clock,"Actual aggregate change invokes sink.");
                else Check(host.LastDiagnostic is {SinkInvoked:false,SinkReturned:false,TimestampMicroseconds:null},"Mask-only change has no clock or sink.");
            }
            rows.Add(new{Index=index,HeldMask=host.HeldKeyMask,Running=host.IsRunning,Enabled=host.IsEnabled,Accepted=host.AcceptedCount,Handled=host.HandledCount,ClockReads=host.ClockReadCount,SinkCalls=host.SinkCallCount,GuiCaptured=gui.Captured,Consumed=consumed});index++;
        }
        Check(guiConsumed&&focusStopped&&!probe.SawHandled,"Actual GUI and focus event behavior and unhandled propagation.");
        Check(!externalIntent&&!host.IsEnabled&&!host.IsRunning,"Explicit final disable stopped external intent.");
        int sinkBeforeClose=sinks.Count;host.Close();host.Close();Check(host.IsClosed&&sinks.Count==sinkBeforeClose&&!host.IsProcessingUnhandledKeyInput(),"Close adds no implicit sink operation.");parent.Free();
        bool borrowedSurvival=true,clockPrevented=true,sinkPrefix=true;
        // Close without prior disable preserves the caller's already published nonzero intent.
        var closeParent=new Node();AddChild(closeParent);var closeHost=new TerrainRunInputHost();closeParent.AddChild(closeHost);bool retained=false;int calls=0;closeHost.Configure(()=>7,(d,_)=>{retained=d;calls++;});
        using(var e=KeyEvent(Key.Shift)){Check(closeHost.Dispatch(e),"Direct direction setup.");}closeHost.Close();borrowedSurvival=retained&&calls==1&&!closeHost.IsRunning;Check(borrowedSurvival,"Close retains external sink effect, clears only host state.");closeParent.Free();
        var detached=new TerrainRunInputHost();Reject(()=>detached.Configure(()=>0,(_,_)=>{}));detached.Free();
        // Clock callback mutation reentry is caught by the caller yet poisons before sink.
        foreach(string operation in new[]{"Dispatch","Enable","Configure","Close"})
        {
            var p=new Node();AddChild(p);var h=new TerrainRunInputHost();p.AddChild(h);int nested=0,invocations=0;using var e=KeyEvent(Key.Shift);
            h.Configure(()=>{try{if(operation=="Dispatch")h.Dispatch(e);else if(operation=="Enable")h.SetEnabled(false);else if(operation=="Configure")h.Configure(()=>0,(_,_)=>{});else h.Close();}catch(InvalidOperationException){nested++;}return 11;},(_,_)=>invocations++);
            Check(!Push(e),"Clock-reentry event unhandled.");clockPrevented&=nested==1&&h.IsFaulted&&h.ClockReadCount==1&&h.SinkCallCount==0&&invocations==0&&h.HeldKeyMask==0&&h.AcceptedCount==0&&h.HandledCount==0&&h.LastDiagnostic is {SinkInvoked:false,SinkReturned:false,StateCommitted:false,FromGodotEvent:true};
            Check(clockPrevented&&!h.IsProcessingUnhandledKeyInput(),"Caught clock reentry prevents sink and disables.");Check(!Push(e)&&h.ClockReadCount==1,"Faulted input disabled.");h.Close();p.Free();
        }
        // Sink return followed by caught host reentry leaves an honest completed external prefix.
        foreach(string operation in new[]{"Dispatch","Enable","Close"})
        {
            var p=new Node();AddChild(p);var h=new TerrainRunInputHost();p.AddChild(h);bool committed=false;int nested=0;using var e=KeyEvent(Key.Shift);
            h.Configure(()=>12,(d,_)=>{committed=d;try{if(operation=="Dispatch")h.Dispatch(e);else if(operation=="Enable")h.SetEnabled(false);else h.Close();}catch(InvalidOperationException){nested++;}});
            Check(!Push(e),"Sink-reentry event unhandled.");sinkPrefix&=nested==1&&committed&&h.IsFaulted&&h.HeldKeyMask==0&&h.AcceptedCount==0&&h.HandledCount==0&&h.ClockReadCount==1&&h.SinkCallCount==1&&h.LastDiagnostic is {SinkInvoked:true,SinkReturned:true,StateCommitted:false};
            Check(sinkPrefix,"Actual completed sink prefix preserved without held commit.");h.Close();p.Free();
        }
        foreach(string failure in new[]{"ClockThrows","InvalidClock","SinkThrows","Token","CallbackCancel"})
        {
            var p=new Node();AddChild(p);var h=new TerrainRunInputHost();p.AddChild(h);int invocations=0;using var token=new CancellationTokenSource();
            h.Configure(()=>{if(failure=="ClockThrows")throw new InvalidDataException("Synthetic reader error");if(failure=="CallbackCancel")token.Cancel();return failure=="InvalidClock"?-1:13;},(_,time)=>{invocations++;if(time<0||failure=="SinkThrows")throw new InvalidDataException("Synthetic sink rejection");});
            using var e=KeyEvent(Key.Shift);
            if(failure=="Token")Reject(()=>h.Dispatch(e,new CancellationToken(true)));
            else if(failure=="CallbackCancel")Reject(()=>h.Dispatch(e,token.Token));
            else Check(!Push(e),"Throwing reader/sink event unhandled.");
            Check(h.IsFaulted&&h.HeldKeyMask==0&&h.AcceptedCount==0&&h.HandledCount==0&&h.LastFailure is not null,"Failure never commits held state.");
            if(failure is "ClockThrows" or "CallbackCancel" or "Token")Check(invocations==0&&h.SinkCallCount==0,"Clock/token failure before sink.");
            else Check(invocations==1&&h.LastDiagnostic is {SinkInvoked:true,SinkReturned:false},"Invoked throwing sink records possible external prefix.");h.Close();p.Free();
        }
        var maskParent=new Node();AddChild(maskParent);var maskHost=new TerrainRunInputHost();maskParent.AddChild(maskHost);int maskCallbacks=0;maskHost.Configure(()=>14,(_,_)=>maskCallbacks++);
        using(var left=KeyEvent(Key.Shift,location:KeyLocation.Left)){maskHost.Dispatch(left);}
        using(var right=KeyEvent(Key.Shift,location:KeyLocation.Right)){Reject(()=>maskHost.Dispatch(right,new CancellationToken(true)));}
        Check(maskHost.IsFaulted&&maskHost.HeldKeyMask==1&&maskHost.IsRunning&&maskHost.AcceptedCount==1&&maskHost.ClockReadCount==1&&maskHost.SinkCallCount==1&&maskCallbacks==1&&maskHost.LastDiagnostic is {ProposedHeldMask:3,TimestampMicroseconds:null,SinkInvoked:false,SinkReturned:false,StateCommitted:false},"Cancelled mask-only transition retains prior left ownership without callbacks.");maskHost.Close();maskParent.Free();
        // A failed stop keeps the last committed held state; it may have already stopped externally.
        var stopParent=new Node();AddChild(stopParent);var stop=new TerrainRunInputHost();stopParent.AddChild(stop);bool stopExternal=false;int stopCalls=0;stop.Configure(()=>14,(d,_)=>{stopExternal=d;if(++stopCalls==2)throw new InvalidDataException("Stop suffix rejected");});
        using(var e=KeyEvent(Key.Shift)){stop.Dispatch(e);}Reject(()=>stop.SetEnabled(false));Check(stop.IsFaulted&&stop.IsRunning&&!stopExternal&&stop.AcceptedCount==1&&stop.SinkCallCount==2,"Failed zero sink may stop external intent without committing held transition.");stop.Close();stopParent.Free();
        var liveParent=new Node();AddChild(liveParent);var live=new TerrainRunInputHost();liveParent.AddChild(live);live.Configure(()=>15,(_,_)=>{});using(var left=KeyEvent(Key.Shift)){live.Dispatch(left);}liveParent.QueueFree();using(var e=KeyEvent(Key.Shift,location:KeyLocation.Right)){Check(!Push(e),"Queued parent event unhandled.");}Check(live.IsFaulted&&live.ClockReadCount==1&&live.SinkCallCount==1&&live.AcceptedCount==1&&live.HeldKeyMask==1,"Parent liveness before mask-only commit or callbacks.");await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);Check(live.IsClosed,"Actual ExitTree closes queued parent.");
        var exitParent=new Node();AddChild(exitParent);var exit=new TerrainRunInputHost();exitParent.AddChild(exit);int exitSinks=0;exit.Configure(()=>{RemoveChild(exitParent);return 16;},(_,_)=>exitSinks++);using(var e=KeyEvent(Key.Shift)){Reject(()=>exit.Dispatch(e));}Check(exit.IsClosed&&exitSinks==0&&exit.HeldKeyMask==0,"Exit during reader closes before sink.");exitParent.Free();
        var threadParent=new Node();AddChild(threadParent);var thread=new TerrainRunInputHost();threadParent.AddChild(thread);thread.Configure(()=>17,(_,_)=>{});using(var e=KeyEvent(Key.Shift)){await Task.Run(()=>{foreach(Action call in new Action[]{()=>thread.Dispatch(e),()=>thread.SetEnabled(false),()=>thread.Close()}){try{call();throw new Exception("Off-thread accepted");}catch(InvalidOperationException){}}});}Check(!thread.IsClosed&&thread.IsEnabled&&thread.ClockReadCount==0&&thread.SinkCallCount==0,"Wrong thread rejected before mutation.");thread.Close();threadParent.Free();
        bool gpu=DisplayServer.GetName()!="headless";
        if(gpu)
        {
            var panel=new ColorRect{Color=new Color(.1f,.7f,.2f),Position=new(20,20),Size=new(600,120),MouseFilter=Control.MouseFilterEnum.Ignore};AddChild(panel);
            var label=new Label{Text="Shift run host: locations + GUI + focus\n"+index+" authored events; "+sinks.Count+" exact native sink calls\nCaller owns run speed and clock",Position=new(32,28),MouseFilter=Control.MouseFilterEnum.Ignore};AddChild(label);
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=GetViewport().GetTexture().GetImage();var pixel=image.GetPixel(25,25);Check(pixel.G>.6f&&pixel.R<.3f,"Own actual GPU diagnostic panel readback.");Need(image.SavePng(Path.ChangeExtension(output,".png"))==Error.Ok,"Own capture");label.Free();panel.Free();
        }
        gui.Free();probe.Free();Check(GetChildCount()==0,"All owned Godot nodes drained.");
        File.WriteAllText(output,JsonSerializer.Serialize(new{Gpu=gpu,Rows=rows,Sinks=sinks,NativeChecks=checks,BorrowedSurvival=borrowedSurvival,ClockPhasePrevented=clockPrevented,SinkPrefixPreserved=sinkPrefix,GuiConsumed=guiConsumed,FocusStopped=focusStopped,Cleanup=true}),new System.Text.UTF8Encoding(false));GD.Print("PASS terrain run input host");
    }
}
public partial class TerrainRunPropagationProbe : Node
{
    public int Count {get;private set;}
    public bool SawHandled {get;private set;}
    public override void _UnhandledInput(InputEvent input){Count++;SawHandled|=GetViewport().IsInputHandled();}
}
public partial class TerrainRunGuiConsumer : Control
{
    public bool Capture {get;set;}
    public int Captured {get;private set;}
    public override void _GuiInput(InputEvent input){if(Capture&&input is InputEventKey){Captured++;AcceptEvent();}}
}
