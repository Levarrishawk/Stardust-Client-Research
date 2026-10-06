using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public partial class TerrainOfflineRestartHudVerification : Node3D
{
    private int checks;
    private void Check(bool value,string why){checks++;if(!value)throw new InvalidDataException(why);}
    private void Reject(Action call){bool rejected=false;try{call();}catch(Exception e) when(e is InvalidOperationException or ArgumentException or OperationCanceledException or IOException or InvalidDataException){rejected=true;}Check(rejected,"Expected restart rejection");}
    public override async void _Ready(){try{await Run();GetTree().Quit();}catch(Exception e){GD.PushError("Restart HUD: "+e);GetTree().Quit(1);}}
    private async Task Run()
    {
        var args=OS.GetCmdlineUserArgs();string Arg(string key){int i=Array.IndexOf(args,key);if(i<0)throw new ArgumentException(key);return args[i+1];}
        string output=Arg("--out");using var doc=JsonDocument.Parse(File.ReadAllText(Arg("--fixtures")));var states=doc.RootElement.Deserialize<TerrainOfflineOutcomeHudState[]>()!;
        // Standalone SubViewport keeps injected mouse coordinates independent of OS hover.
        GetViewport().NotifyMouseEntered();var probe=new TerrainRestartInputProbe();AddChild(probe);var rows=new List<object>();var owners=new List<object>();
        async Task Click(Vector2 at)
        {
            using(var motion=new InputEventMouseMotion{Position=at,GlobalPosition=at,Relative=new Vector2(1,1)})GetViewport().PushInput(motion,true);
            using(var e=new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Left,ButtonMask=MouseButtonMask.Left,Position=at,GlobalPosition=at})GetViewport().PushInput(e,true);
            using(var e=new InputEventMouseButton{Pressed=false,ButtonIndex=MouseButton.Left,Position=at,GlobalPosition=at})GetViewport().PushInput(e,true);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        }
        for(int run=0;run<2;run++)
        {
            var owner=new Node3D();AddChild(owner);var hud=new TerrainOfflineRestartHud();int callbacks=0;hud.Configure(owner,states[run],()=>++callbacks==2);
            var button=(Button)owner.GetChild(1).GetChild(0);var label=(Label)owner.GetChild(0).GetChild(0).GetChild(0);
            Check(button.Text=="Restart offline preview"&&button.Size.X>=240&&button.Position==new Vector2(16,484)&&button.FocusMode==Control.FocusModeEnum.None&&button.MouseFilter==Control.MouseFilterEnum.Stop&&label.Text==hud.Caption,"actual frozen outcome/button layout");
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);int before=probe.Events;await Click(new(700,500));Check(probe.Events-before==2,"outside button mouse pass-through");
            for(int i=0;i<3;i++)
            {
                before=probe.Events;await Click(new(30,500));if(i<2)Check(probe.Events==before,"GUI button captures press/release");
                rows.Add(new{Run=run,Index=i,Caption=hud.Caption,Actual=label.Text,Requests=hud.RequestCount,Accepted=hud.AcceptedRestartCount,Callbacks=callbacks,Retired=hud.IsRetired,Disabled=button.Disabled,Children=owner.GetChildCount()});
                Check(!hud.IsRetired&&owner.GetChildCount()==2,"persistent outcome after rejected/accepted/duplicate click");
            }
            Check(!hud.RequestRestart()&&callbacks==2&&hud.RequestCount==2&&hud.AcceptedRestartCount==1,"accepted suppresses direct/late requests");button.EmitSignal(BaseButton.SignalName.Pressed);Check(callbacks==2,"late signal after acceptance ignored");
            string caption=hud.Caption;((IDisposable)hud).Dispose();hud.Dispose();Check(hud.Caption==caption&&hud.RetirementCount==1&&owner.GetChildCount()==0&&!hud.RequestRestart(),"caller replacement disposes both displays once");
            owners.Add(new{Run=run,Caption=caption,Requests=hud.RequestCount,Accepted=hud.AcceptedRestartCount,Retirements=hud.RetirementCount,Children=owner.GetChildCount()});owner.Free();
        }
        var failures=new List<object>();
        foreach(string kind in new[]{"callback-throw","callback-reentry","callback-dispose-reentry","callback-cancel-false","callback-cancel-true","initial-cancel","owner-exit","callback-owner-free","layer-free","layer-queued","off-thread","configure-cancel","invalid-outcome","second-configure","gui-throw","outcome-label-free","outcome-panel-free","callback-label-free","callback-panel-free","button-mouse","button-focus","button-layout"})
        {
            var owner=new Node3D();AddChild(owner);var hud=new TerrainOfflineRestartHud();int callbacks=0;using var cancel=new CancellationTokenSource();
            bool Restart()
            {
                callbacks++;if(kind is "callback-throw" or "gui-throw")throw new IOException("callback");
                if(kind=="callback-reentry"){try{hud.RequestRestart();}catch(InvalidOperationException){}}if(kind=="callback-dispose-reentry"){try{hud.Dispose();}catch(InvalidOperationException){}}
                if(kind.StartsWith("callback-cancel"))cancel.Cancel();if(kind=="callback-owner-free")owner.Free();if(kind=="callback-label-free")owner.GetChild(0).GetChild(0).GetChild(0).Free();if(kind=="callback-panel-free")owner.GetChild(0).GetChild(0).Free();return kind!="callback-cancel-false";
            }
            if(kind=="configure-cancel")Reject(()=>hud.Configure(owner,states[0],Restart,new CancellationToken(true)));
            else if(kind=="invalid-outcome")Reject(()=>hud.Configure(owner,states[0] with{Reason="wrong"},Restart));
            else
            {
                hud.Configure(owner,states[0],Restart,cancel.Token);var button=(Button)owner.GetChild(1).GetChild(0);
                if(kind=="outcome-label-free")owner.GetChild(0).GetChild(0).GetChild(0).Free();if(kind=="outcome-panel-free")owner.GetChild(0).GetChild(0).Free();if(kind=="button-mouse")button.MouseFilter=Control.MouseFilterEnum.Ignore;if(kind=="button-focus")button.FocusMode=Control.FocusModeEnum.All;if(kind=="button-layout")button.Position+=Vector2.One;
                if(kind=="initial-cancel")cancel.Cancel();if(kind=="owner-exit")RemoveChild(owner);if(kind=="layer-free")owner.GetChild(1).Free();if(kind=="layer-queued")owner.GetChild(1).QueueFree();
                if(kind=="off-thread"){Check(await Task.Run(()=>{try{hud.RequestRestart();return false;}catch(InvalidOperationException){return true;}}),"off-thread before callback");Check(!hud.IsRetired&&callbacks==0,"off-thread preserves outcome");hud.Dispose();}
                else if(kind is "owner-exit" or "layer-free")Check(!hud.RequestRestart(),"native retirement suppresses request");
                else if(kind=="second-configure")Reject(()=>hud.Configure(owner,states[0],Restart));
                else if(kind=="gui-throw"){button.EmitSignal(BaseButton.SignalName.Pressed);Check(hud.IsRetired,"GUI boundary retires failure");}
                else Reject(()=>hud.RequestRestart());
                if(GodotObject.IsInstanceValid(button)){int old=callbacks;button.EmitSignal(BaseButton.SignalName.Pressed);Check(callbacks==old,"late retired button has no callback");}
            }
            Check(hud.IsRetired&&hud.RetirementCount==1,"single retirement "+kind);
            failures.Add(new{Kind=kind,Caption=hud.Caption,Requests=hud.RequestCount,Accepted=hud.AcceptedRestartCount,Callbacks=callbacks,Retirements=hud.RetirementCount,Diagnostic=hud.LastDiagnostic});
            if(kind is "callback-throw" or "callback-reentry" or "callback-cancel-false" or "callback-cancel-true" or "gui-throw" or "second-configure")Check(owner.GetChild(0).GetChild(0).GetChild(0) is Label {Text:var text}&&text==hud.Caption,"callback failure keeps outcome until caller Dispose");
            hud.Dispose();hud.Dispose();if(GodotObject.IsInstanceValid(owner))owner.Free();
        }
        var teardown=new List<object>();
        foreach(string mode in new[]{"parent-free","parent-queue-free","layer-detached-free","layer-queue-free","interface-dispose"})
        {
            var owner=new Node3D();AddChild(owner);var hud=new TerrainOfflineRestartHud();int callbacks=0;hud.Configure(owner,states[0],()=>{callbacks++;return true;});var owned=owner.GetChild(1);
            if(mode=="parent-free")owner.Free();else if(mode=="parent-queue-free"){owner.QueueFree();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}else if(mode=="layer-detached-free"){owner.RemoveChild(owned);owned.Free();}else if(mode=="layer-queue-free"){owned.QueueFree();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}else((IDisposable)hud).Dispose();
            hud.Dispose();hud.Dispose();bool alive=GodotObject.IsInstanceValid(owner);Check(hud.IsRetired&&hud.RetirementCount==1&&callbacks==0&&(!alive||owner.GetChildCount()==0),"native/deferred/interface retirement "+mode);
            teardown.Add(new{Kind=mode,OwnerAlive=alive,Caption=hud.Caption,Requests=hud.RequestCount,Accepted=hud.AcceptedRestartCount,Callbacks=callbacks,Retirements=hud.RetirementCount});if(alive)owner.Free();
        }
        probe.Free();var displayOwner=new Node3D();AddChild(displayOwner);var display=new TerrainOfflineRestartHud();display.Configure(displayOwner,states[0],()=>false);bool gpu=DisplayServer.GetName()!="headless";
        if(gpu){await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=GetViewport().GetTexture().GetImage();Check(image.SavePng(Path.ChangeExtension(output,"png"))==Error.Ok,"private restart GPU capture");}
        display.Dispose();displayOwner.Free();Check(GetChildCount()==0,"all native nodes released");
        File.WriteAllText(output,JsonSerializer.Serialize(new{Passed=true,Checks=checks,Gpu=gpu,Rows=rows,Owners=owners,Failures=failures,Teardown=teardown,ActualGuiButton=true,OutsidePassThrough=true,BorrowedSurvival=true}));GD.Print("PASS offline restart HUD");
    }
}
public partial class TerrainRestartInputProbe : Node
{public int Events;public override void _UnhandledInput(InputEvent input){if(input is InputEventMouseButton)Events++;}}
