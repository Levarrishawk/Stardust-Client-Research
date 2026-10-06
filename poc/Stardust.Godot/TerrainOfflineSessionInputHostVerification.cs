using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public partial class TerrainOfflineSessionInputHostVerification : Node3D
{
    private int checks;
    private void Check(bool value,string why){checks++;if(!value)throw new InvalidDataException(why);}
    private void Reject(Action call){bool rejected=false;try{call();}catch(Exception e) when(e is InvalidOperationException or ArgumentException or OperationCanceledException or IOException){rejected=true;}Check(rejected,"Expected session input rejection");}
    public override async void _Ready(){try{await Run();GetTree().Quit();}catch(Exception e){GD.PushError("Session input: "+e);GetTree().Quit(1);}}
    private async Task Run()
    {
        var args=OS.GetCmdlineUserArgs();string Arg(string key){int i=Array.IndexOf(args,key);if(i<0)throw new ArgumentException(key);return args[i+1];}
        string output=Arg("--out");using var doc=JsonDocument.Parse(File.ReadAllText(Arg("--fixtures")));
        var probe=new TerrainSessionInputProbe();AddChild(probe);
        var owner=new Node3D();AddChild(owner);var host=new TerrainOfflineSessionInputHost();owner.AddChild(host);bool paused=false;int pauseCalls=0,finishCalls=0;
        host.Configure(owner,()=>paused,value=>{paused=value;pauseCalls++;},()=>finishCalls++);
        var hud=new TerrainOfflinePlaygroundHud();var hudState=new TerrainOfflinePlaygroundHudState("Moving",4,20,1000,980,true,true);
        hud.Configure(owner,()=>hudState,default,true);Check(hud.Caption.Contains("Movement controls")&&hud.Caption.Contains("P pause  |  Escape finish"),"interactive plain controls");
        var gui=new TerrainSessionInputGui{FocusMode=Control.FocusModeEnum.All,MouseFilter=Control.MouseFilterEnum.Ignore,Size=GetViewport().GetVisibleRect().Size};AddChild(gui);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        InputEventKey KeyEvent(string key,bool pressed=true,bool echo=false)=>new(){Keycode=key=="P"?Key.P:key=="Escape"?Key.Escape:Key.W,Pressed=pressed,Echo=echo};
        var rows=new List<object>();int index=0;
        foreach(var action in doc.RootElement.GetProperty("Actions").EnumerateArray())
        {
            string type=action.GetProperty("Type").GetString()!;int before=probe.Events;bool? consumed=null;
            if(type=="Gui")
            {
                gui.Capture=action.GetProperty("Enabled").GetBoolean();gui.MouseFilter=gui.Capture?Control.MouseFilterEnum.Stop:Control.MouseFilterEnum.Ignore;
                if(gui.Capture){gui.GrabFocus();Check(GetViewport().GuiGetFocusOwner()==gui,"actual focused consuming GUI");}else gui.ReleaseFocus();
            }
            else if(type=="Mouse") {using var e=new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Left,Position=new(32,32),GlobalPosition=new(32,32)};GetViewport().PushInput(e,true);consumed=probe.Events==before;}
            else{using var e=KeyEvent(action.GetProperty("Key").GetString()!,action.GetProperty("Pressed").GetBoolean(),action.GetProperty("Echo").GetBoolean());GetViewport().PushInput(e,true);consumed=probe.Events==before;}
            rows.Add(new{Index=index++,Paused=paused,PauseCalls=pauseCalls,FinishCalls=finishCalls,StateReads=host.StateReadCount,Accepted=host.AcceptedCount,Handled=host.HandledCount,FinishRequested=host.FinishRequested,GuiCaptured=gui.Captured,Consumed=consumed});
            Check(!host.IsFaulted,"healthy synthetic viewport sequence");
        }
        Check(finishCalls==1&&pauseCalls==2&&!paused&&host.AcceptedCount==3&&host.HandledCount==3&&host.StateReadCount==2,"P toggles and Escape finish exactly once");
        Check(host.LastDiagnostic is {Kind:"Finish",SinkMayHaveRun:true,SinkReturned:true,StateCommitted:true,FromGodotEvent:true},"actual completed callback prefix");
        host.Dispose();Check(GodotObject.IsInstanceValid(owner)&&owner.GetChildCount()==1,"Dispose releases input node only, HUD/owner survive");hud.Dispose();owner.Free();gui.Free();probe.Free();
        var failures=new List<object>();
        foreach(string kind in new[]{"source-throw","pause-throw","finish-throw","source-reentry","pause-reentry","finish-reentry","source-dispose-reentry","pause-cancel","initial-cancel","owner-exit","host-exit","callback-owner-exit","callback-owner-free","off-thread","configure-cancel","wrong-owner"})
        {
            owner=new();AddChild(owner);host=new();owner.AddChild(host);var other=new Node3D();AddChild(other);int reads=0,sinks=0,stops=0;using var cancellation=new CancellationTokenSource();
            void Nested(){using var nested=KeyEvent("P");try{host.Dispatch(nested);}catch(InvalidOperationException){}}
            bool Source(){reads++;if(kind=="source-throw")throw new IOException("source");if(kind=="source-reentry")Nested();if(kind=="source-dispose-reentry"){try{host.Dispose();}catch(InvalidOperationException){}}return false;}
            void Pause(bool value){sinks++;if(kind=="pause-throw")throw new IOException("pause");if(kind=="pause-reentry")Nested();if(kind=="pause-cancel")cancellation.Cancel();if(kind=="callback-owner-exit")RemoveChild(owner);if(kind=="callback-owner-free")owner.Free();}
            void Finish(){stops++;if(kind=="finish-throw")throw new IOException("finish");if(kind=="finish-reentry")Nested();}
            if(kind=="configure-cancel"||kind=="wrong-owner")
            {
                Reject(()=>host.Configure(kind=="wrong-owner"?other:owner,Source,Pause,Finish,new CancellationToken(kind=="configure-cancel")));
                Check(!host.IsConfigured&&!host.IsClosed&&!host.IsFaulted&&reads+sinks+stops==0,"failed Configure borrows nothing and can retry");host.Configure(owner,Source,Pause,Finish);host.Close();
            }
            else
            {
                host.Configure(owner,Source,Pause,Finish);
                if(kind=="initial-cancel")cancellation.Cancel();if(kind=="owner-exit")RemoveChild(owner);if(kind=="host-exit")owner.RemoveChild(host);
                using var e=KeyEvent(kind.StartsWith("finish")?"Escape":"P");
                if(kind=="off-thread")
                {
                    Check(await Task.Run(()=>{try{host.Dispatch(e);return false;}catch(InvalidOperationException){return true;}}),"off-thread dispatch before callback");
                    Check(reads+sinks+stops==0&&!host.IsClosed&&!host.IsFaulted,"off-thread preserves node");host.Close();
                }
                else Reject(()=>host.Dispatch(e,cancellation.Token));
            }
            bool failed=kind is not ("owner-exit" or "host-exit" or "off-thread" or "configure-cancel" or "wrong-owner");
            Check(host.IsClosed&&host.IsFaulted==failed,"terminal outcome "+kind);
            failures.Add(new{Kind=kind,Reads=reads,PauseCalls=sinks,FinishCalls=stops,Faulted=host.IsFaulted,FinishRequested=host.FinishRequested,Accepted=host.AcceptedCount,Diagnostic=host.LastDiagnostic});
            host.Dispose();if(GodotObject.IsInstanceValid(owner))owner.Free();other.Free();
        }
        owner=new();AddChild(owner);host=new();owner.AddChild(host);int engineReads=0;
        host.Configure(owner,()=>{engineReads++;throw new IOException("engine source");},_=>{},()=>{});
        using(var eventKey=KeyEvent("P"))GetViewport().PushInput(eventKey,true);
        Check(engineReads==1&&host.IsClosed&&host.IsFaulted&&host.LastDiagnostic is {Kind:"Pause",FromGodotEvent:true,SinkMayHaveRun:false,StateCommitted:false},"engine boundary catches source failure and records prefix");
        host.Dispose();Check(GodotObject.IsInstanceValid(owner)&&owner.GetChildCount()==0,"engine failure owner survives");owner.Free();
        owner=new();AddChild(owner);host=new();owner.AddChild(host);host.Configure(owner,()=>false,_=>{},()=>{});host.Dispose();host.Dispose();Check(GodotObject.IsInstanceValid(owner)&&owner.GetChildCount()==0,"idempotent input disposal borrowed owner intact");owner.Free();
        var teardown=new List<object>();
        foreach(string mode in new[]{"detached-free","attached-free","parent-free","parent-queue-free","interface-dispose"})
        {
            owner=new();AddChild(owner);host=new();owner.AddChild(host);int reads=0,sinks=0,stops=0;
            host.Configure(owner,()=>{reads++;return false;},_=>sinks++,()=>stops++);
            GD.Print("teardown begin "+mode);
            if(mode=="detached-free"){owner.RemoveChild(host);host.Free();}
            else if(mode=="attached-free")host.Free();
            else if(mode=="parent-free")owner.Free();
            else if(mode=="interface-dispose")((IDisposable)host).Dispose();
            else{owner.QueueFree();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
            Check(!GodotObject.IsInstanceValid(host),"own native input freed "+mode);
            bool ownerAlive=GodotObject.IsInstanceValid(owner);int? children=ownerAlive?owner.GetChildCount():null;
            host.Dispose();host.Dispose();
            var row=new{Kind=mode,OwnerAlive=ownerAlive,HostAlive=GodotObject.IsInstanceValid(host),OwnerChildren=children,Closed=host.IsClosed,Faulted=host.IsFaulted,Reads=reads,PauseCalls=sinks,FinishCalls=stops,Accepted=host.AcceptedCount};
            teardown.Add(row);Check(host.IsClosed&&!host.IsFaulted&&reads+sinks+stops==0&&host.AcceptedCount==0,"no callbacks/repeated Dispose stable "+mode);
            Check(ownerAlive==(mode is "detached-free" or "attached-free" or "interface-dispose")&&(!ownerAlive||children==0),"raw node free preserves borrowed owner; parent deletion removes own child "+mode);
            if(ownerAlive)owner.Free();GD.Print("teardown end "+mode);
        }
        owner=new();AddChild(owner);hud=new();hud.Configure(owner,()=>hudState,default,true);bool gpu=DisplayServer.GetName()!="headless";
        if(gpu){await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=GetViewport().GetTexture().GetImage();Check(image.SavePng(Path.ChangeExtension(output,"png"))==Error.Ok,"private interactive HUD capture");}
        string interactiveCaption=hud.Caption;hud.Dispose();owner.Free();Check(GetChildCount()==0,"all verification nodes released");
        File.WriteAllText(output,JsonSerializer.Serialize(new{Passed=true,Checks=checks,Gpu=gpu,Rows=rows,Failures=failures,Teardown=teardown,InteractiveCaption=interactiveCaption,BorrowedSurvival=true}));GD.Print("PASS offline session input");
    }
}
public partial class TerrainSessionInputProbe : Node
{public int Events;public override void _UnhandledInput(InputEvent input){Events++;}}
public partial class TerrainSessionInputGui : Control
{public bool Capture;public int Captured;public override void _GuiInput(InputEvent input){if(Capture){Captured++;AcceptEvent();}}}
