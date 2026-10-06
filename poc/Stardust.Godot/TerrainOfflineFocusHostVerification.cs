using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public partial class TerrainOfflineFocusHostVerification : Node3D
{
    private int checks;
    private void Check(bool value,string why){checks++;if(!value)throw new InvalidDataException(why);}
    private void Reject(Action call){bool rejected=false;try{call();}catch(Exception e) when(e is InvalidOperationException or ArgumentException or OperationCanceledException or IOException){rejected=true;}Check(rejected,"Expected focus rejection");}
    public override async void _Ready(){try{await Run();GetTree().Quit();}catch(Exception e){GD.PushError("Focus host: "+e);GetTree().Quit(1);}}
    private async Task Run()
    {
        var args=OS.GetCmdlineUserArgs();string Arg(string key){int i=Array.IndexOf(args,key);if(i<0)throw new ArgumentException(key);return args[i+1];}
        string output=Arg("--out");using var doc=JsonDocument.Parse(File.ReadAllText(Arg("--fixtures")));var window=GetWindow();
        var rows=new List<object>();var trace=new List<string>();
        foreach(bool initialPaused in new[]{false,true})
        {
            var owner=new Node3D();AddChild(owner);var manual=new TerrainManualInputHost();var run=new TerrainRunInputHost();owner.AddChild(manual);owner.AddChild(run);
            manual.Configure(()=>5000,(v,t)=>trace.Add($"manual:{v.X},{v.Y}"));run.Configure(()=>5000,(v,t)=>trace.Add("run:"+(v?1:0)));
            bool paused=initialPaused;var host=new TerrainOfflineFocusHost();owner.AddChild(host);int externalExit=0,externalEnter=0;
            void OutsideExit()=>externalExit++;void OutsideEnter()=>externalEnter++;window.FocusExited+=OutsideExit;window.FocusEntered+=OutsideEnter;
            host.Configure(owner,()=>{trace.Add("read:"+(paused?1:0));return paused;},v=>{trace.Add("pause:"+(v?1:0));paused=v;},v=>{trace.Add("game:"+(v?1:0));manual.SetEnabled(v);run.SetEnabled(v);});
            foreach(var action in doc.RootElement.GetProperty("Actions").EnumerateArray())
            {
                string kind=action.GetString()!;
                if(kind is "Hold" or "FreshHold")
                {
                    using var w=new InputEventKey{Pressed=true,PhysicalKeycode=Key.W};GetViewport().PushInput(w,true);
                    using var shift=new InputEventKey{Pressed=true,PhysicalKeycode=Key.Shift};GetViewport().PushInput(shift,true);
                }
                else if(kind=="Loss")window.EmitSignal(Window.SignalName.FocusExited);
                else if(kind=="Gain")window.EmitSignal(Window.SignalName.FocusEntered);
                else host.Close();
                rows.Add(new{InitialPaused=initialPaused,Kind=kind,Paused=paused,ManualEnabled=manual.IsEnabled,RunEnabled=run.IsEnabled,ManualHeld=manual.HeldKeyMask,RunHeld=run.HeldKeyMask,PriorPaused=host.PriorPaused,Suspended=host.IsSuspended,Reads=host.StateReadCount,GameplayCalls=host.GameplayCallCount,PauseCalls=host.PauseCallCount,Transitions=host.TransitionCount,OwnConnections=host.OwnConnectionCount,ExternalExit=externalExit,ExternalEnter=externalEnter});
                Check(!host.IsFaulted,"healthy signal sequence");
            }
            Check(manual.IsEnabled&&run.IsEnabled&&manual.HeldKeyMask==0&&run.HeldKeyMask==0&&paused==initialPaused,"fresh gameplay with captured prior pause");
            host.Dispose();host.Dispose();Check(GodotObject.IsInstanceValid(owner)&&owner.GetChildCount()==2,"only focus child disposed");
            window.FocusExited-=OutsideExit;window.FocusEntered-=OutsideEnter;manual.Close();run.Close();owner.Free();
        }
        var failures=new List<object>();
        foreach(string kind in new[]{"source-throw","game-throw","pause-throw","gain-game-throw","gain-pause-throw","source-reentry","source-cancel","game-reentry","pause-reentry","source-dispose-reentry","game-cancel","pause-cancel","initial-cancel","lifetime-cancel","owner-exit","callback-owner-free","callback-window-free","off-thread","configure-cancel","wrong-owner","window-exit","signal-source-throw"})
        {
            var borrowedWindow=new Window();AddChild(borrowedWindow);var owner=new Node3D();borrowedWindow.AddChild(owner);var host=new TerrainOfflineFocusHost();owner.AddChild(host);var other=new Node3D();borrowedWindow.AddChild(other);
            int reads=0,games=0,pauses=0;bool ready=false;using var cancellation=new CancellationTokenSource();
            void Nested(){try{host.HandleFocus(false);}catch(InvalidOperationException){}}
            bool Source(){reads++;if(ready&&kind is "source-throw" or "signal-source-throw")throw new IOException("source");if(ready&&kind=="source-reentry")Nested();if(ready&&kind=="source-cancel")cancellation.Cancel();if(ready&&kind=="source-dispose-reentry"){try{host.Dispose();}catch(InvalidOperationException){}}return false;}
            void Game(bool value){games++;if(ready&&kind is "game-throw" or "gain-game-throw")throw new IOException("game");if(ready&&kind=="game-reentry")Nested();if(ready&&kind=="game-cancel")cancellation.Cancel();if(ready&&kind=="callback-owner-free")owner.Free();if(ready&&kind=="callback-window-free")borrowedWindow.Free();}
            void Pause(bool value){pauses++;if(ready&&kind is "pause-throw" or "gain-pause-throw")throw new IOException("pause");if(ready&&kind=="pause-reentry")Nested();if(ready&&kind=="pause-cancel")cancellation.Cancel();}
            if(kind is "configure-cancel" or "wrong-owner")
            {
                Reject(()=>host.Configure(kind=="wrong-owner"?other:owner,Source,Pause,Game,new CancellationToken(kind=="configure-cancel")));
                Check(!host.IsConfigured&&!host.IsClosed&&!host.IsFaulted&&host.OwnConnectionCount==0,"rejected configure retains no connection");host.Configure(owner,Source,Pause,Game);host.Close();
            }
            else
            {
                host.Configure(owner,Source,Pause,Game,kind=="lifetime-cancel"?cancellation.Token:default);
                if(kind.StartsWith("gain")){host.HandleFocus(false);reads=games=pauses=0;}
                ready=true;if(kind is "initial-cancel" or "lifetime-cancel")cancellation.Cancel();
                if(kind=="owner-exit")borrowedWindow.RemoveChild(owner);if(kind=="window-exit")RemoveChild(borrowedWindow);
                if(kind=="off-thread")
                {
                    Check(await Task.Run(()=>{try{host.HandleFocus(false);return false;}catch(InvalidOperationException){return true;}}),"off-thread before callback");Check(reads+games+pauses==0&&!host.IsClosed,"off-thread leaves host open");host.Close();
                }
                else if(kind=="signal-source-throw")borrowedWindow.EmitSignal(Window.SignalName.FocusExited);
                else Reject(()=>host.HandleFocus(kind.StartsWith("gain"),cancellation.Token));
            }
            bool failed=kind is not ("owner-exit" or "window-exit" or "off-thread" or "configure-cancel" or "wrong-owner");
            Check(host.IsClosed&&host.IsFaulted==failed&&host.OwnConnectionCount==0,"failure terminal and disconnected "+kind);
            failures.Add(new{Kind=kind,Reads=reads,GameplayCalls=games,PauseCalls=pauses,Suspended=host.IsSuspended,Transitions=host.TransitionCount,Faulted=host.IsFaulted,Diagnostic=host.LastDiagnostic});
            host.Dispose();if(GodotObject.IsInstanceValid(owner))owner.Free();if(GodotObject.IsInstanceValid(other))other.Free();if(GodotObject.IsInstanceValid(borrowedWindow))borrowedWindow.Free();
        }
        var teardown=new List<object>();
        foreach(string mode in new[]{"detached-free","attached-free","parent-free","parent-queue-free","interface-dispose"})
        {
            var owner=new Node3D();AddChild(owner);var host=new TerrainOfflineFocusHost();owner.AddChild(host);int callbacks=0,external=0;void Neighbor()=>external++;window.FocusExited+=Neighbor;
            host.Configure(owner,()=>{callbacks++;return false;},_=>callbacks++,_=>callbacks++);
            if(mode=="detached-free"){owner.RemoveChild(host);host.Free();}else if(mode=="attached-free")host.Free();else if(mode=="parent-free")owner.Free();else if(mode=="interface-dispose")((IDisposable)host).Dispose();
            else{owner.QueueFree();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
            host.Dispose();host.Dispose();window.EmitSignal(Window.SignalName.FocusExited);window.FocusExited-=Neighbor;bool ownerAlive=GodotObject.IsInstanceValid(owner);
            teardown.Add(new{Kind=mode,OwnerAlive=ownerAlive,HostAlive=GodotObject.IsInstanceValid(host),Callbacks=callbacks,External=external,Closed=host.IsClosed,OwnConnections=host.OwnConnectionCount});
            Check(!GodotObject.IsInstanceValid(host)&&host.IsClosed&&host.OwnConnectionCount==0&&callbacks==0&&external==1,"safe native retirement and neighbor retained "+mode);
            if(ownerAlive){Check(owner.GetChildCount()==0,"borrowed owner intact");owner.Free();}
        }
        bool gpu=DisplayServer.GetName()!="headless";if(gpu){await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);Check(true,"actual GPU frame completed");}
        Check(GetChildCount()==0,"all focus fixture nodes released");
        File.WriteAllText(output,JsonSerializer.Serialize(new{Passed=true,Checks=checks,Gpu=gpu,SyntheticWindowSignals=true,Rows=rows,Trace=trace,Failures=failures,Teardown=teardown}));GD.Print("PASS offline focus host");
    }
}
