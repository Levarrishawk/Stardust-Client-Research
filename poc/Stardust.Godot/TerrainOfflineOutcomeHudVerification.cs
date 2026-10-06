using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public partial class TerrainOfflineOutcomeHudVerification : Node3D
{
    private int checks;
    private void Check(bool value,string why){checks++;if(!value)throw new InvalidDataException(why);}
    private void Reject(Action call){bool rejected=false;try{call();}catch(Exception e) when(e is InvalidOperationException or ArgumentException or OperationCanceledException or IOException or InvalidDataException){rejected=true;}Check(rejected,"Expected outcome HUD rejection");}
    public override async void _Ready(){try{await Run();GetTree().Quit();}catch(Exception e){GD.PushError("Outcome HUD: "+e);GetTree().Quit(1);}}
    private async Task Run()
    {
        var args=OS.GetCmdlineUserArgs();string Arg(string key){int i=Array.IndexOf(args,key);if(i<0)throw new ArgumentException(key);return args[i+1];}
        string output=Arg("--out");using var doc=JsonDocument.Parse(File.ReadAllText(Arg("--fixtures")));var states=doc.RootElement.Deserialize<TerrainOfflineOutcomeHudState[]>()!;
        var probe=new TerrainOutcomeInputProbe();AddChild(probe);var owner=new Node3D();AddChild(owner);var hud=new TerrainOfflineOutcomeHud();int reads=0;var state=states[0];hud.Configure(owner,()=>{reads++;return state;});var rows=new List<object>();
        for(int i=0;i<states.Length;i++)
        {
            state=states[i];if(i>0)Check(hud.Refresh(),"actual fresh display");var layer=(CanvasLayer)owner.GetChild(0);var panel=(Panel)layer.GetChild(0);var label=(Label)panel.GetChild(0);
            Check(label.Text==hud.Caption&&panel.Position==new Vector2(16,328)&&panel.Size==new Vector2(608,148)&&panel.MouseFilter==Control.MouseFilterEnum.Ignore&&panel.FocusMode==Control.FocusModeEnum.None&&label.Position==new Vector2(12,8)&&label.GetThemeFontSize("font_size")==16&&label.MouseFilter==Control.MouseFilterEnum.Ignore&&label.FocusMode==Control.FocusModeEnum.None,"actual caption/layout/input policy");
            rows.Add(new{Index=i,Caption=hud.Caption,Actual=label.Text,RefreshCount=hud.RefreshCount,Reads=reads});
        }
        using(var key=new InputEventKey{Pressed=true,Keycode=Key.W})GetViewport().PushInput(key,true);
        using(var mouse=new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Left,Position=new(40,345),GlobalPosition=new(40,345)})GetViewport().PushInput(mouse,true);
        Check(probe.Events==2&&GetViewport().GuiGetFocusOwner() is null,"actual input crosses noninteractive visible limits label");
        string historical=hud.Caption;hud.Dispose();hud.Dispose();Check(hud.IsRetired&&hud.RetirementCount==1&&hud.Caption==historical&&hud.RefreshCount==states.Length&&owner.GetChildCount()==0&&!hud.Refresh(new CancellationToken(true))&&reads==states.Length,"single retirement/history/no source after disposal");owner.Free();probe.Free();
        var failures=new List<object>();
        foreach(string kind in new[]{"updates-zero","updates-large","completed-negative","completed-large","frames-large","queue-negative","queue-large","pending-negative","pending-large","retained-negative","retained-large","reason-unknown","reason-null","null-state","source-throw","source-reentry","source-dispose-reentry","source-cancel","initial-cancel","owner-exit","source-owner-free","layer-free","panel-free","label-free","panel-captures","label-captures","second-configure","unconfigured-refresh","off-thread","detached-configure"})
        {
            owner=new();if(kind!="detached-configure")AddChild(owner);hud=new();int calls=0;bool ready=false;using var cancel=new CancellationTokenSource();
            TerrainOfflineOutcomeHudState Source()
            {
                calls++;if(!ready)return states[0];
                if(kind=="source-throw")throw new IOException("source");if(kind=="source-reentry"){try{hud.Refresh();}catch(InvalidOperationException){}}
                if(kind=="source-dispose-reentry"){try{hud.Dispose();}catch(InvalidOperationException){}}if(kind=="source-cancel")cancel.Cancel();if(kind=="source-owner-free")owner.Free();
                return kind switch{"updates-zero"=>states[0] with{MaximumUpdates=0},"updates-large"=>states[0] with{MaximumUpdates=6001},"completed-negative"=>states[0] with{CompletedUpdates=-1},"completed-large"=>states[0] with{CompletedUpdates=1001},"frames-large"=>states[0] with{RootFrames=6001},"queue-negative"=>states[0] with{QueueCount=-1},"queue-large"=>states[0] with{QueueCount=9},"pending-negative"=>states[0] with{PendingMessages=-1},"pending-large"=>states[0] with{PendingMessages=9},"retained-negative"=>states[0] with{RetainedPackets=-1},"retained-large"=>states[0] with{RetainedPackets=6001},"reason-unknown"=>states[0] with{Reason="Escape"},"reason-null"=>states[0] with{Reason=null!},"null-state"=>null!,_=>states[0]};
            }
            if(kind=="detached-configure")Reject(()=>hud.Configure(owner,Source));
            else if(kind=="unconfigured-refresh")Reject(()=>hud.Refresh());
            else
            {
                hud.Configure(owner,Source);ready=true;
                if(kind=="owner-exit")RemoveChild(owner);
                if(kind=="layer-free")owner.GetChild(0).Free();
                if(kind=="panel-free")owner.GetChild(0).GetChild(0).Free();if(kind=="label-free")owner.GetChild(0).GetChild(0).GetChild(0).Free();
                if(kind=="panel-captures")((Panel)owner.GetChild(0).GetChild(0)).MouseFilter=Control.MouseFilterEnum.Stop;
                if(kind=="label-captures")((Label)owner.GetChild(0).GetChild(0).GetChild(0)).MouseFilter=Control.MouseFilterEnum.Stop;
                if(kind=="initial-cancel")cancel.Cancel();
                if(kind=="off-thread")
                {
                    Check(await Task.Run(()=>{try{hud.Refresh();return false;}catch(InvalidOperationException){return true;}}),"off-thread before source");Check(!hud.IsRetired&&calls==1,"off-thread preserves owner");hud.Dispose();
                }
                else if(kind is "owner-exit" or "layer-free")Check(!hud.Refresh(),"native retirement before another read");
                else if(kind=="second-configure")Reject(()=>hud.Configure(owner,Source));
                else Reject(()=>hud.Refresh(cancel.Token));
            }
            Check(hud.IsRetired&&hud.RetirementCount==1,"one retirement "+kind);
            failures.Add(new{Kind=kind,Reads=calls,RefreshCount=hud.RefreshCount,RetirementCount=hud.RetirementCount,Caption=hud.Caption});
            ((IDisposable)hud).Dispose();hud.Dispose();if(GodotObject.IsInstanceValid(owner))owner.Free();
        }
        var teardown=new List<object>();
        foreach(string mode in new[]{"parent-free","parent-queue-free","layer-detached-free","layer-queue-free","interface-dispose"})
        {
            owner=new();AddChild(owner);hud=new();int calls=0;hud.Configure(owner,()=>{calls++;return states[0];});var owned=owner.GetChild(0);
            if(mode=="parent-free")owner.Free();else if(mode=="parent-queue-free"){owner.QueueFree();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
            else if(mode=="layer-detached-free"){owner.RemoveChild(owned);owned.Free();}else if(mode=="layer-queue-free"){owned.QueueFree();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}else((IDisposable)hud).Dispose();
            hud.Dispose();hud.Dispose();bool alive=GodotObject.IsInstanceValid(owner);Check(hud.IsRetired&&hud.RetirementCount==1&&calls==1&&(!alive||owner.GetChildCount()==0),"safe native/interface/deferred deletion "+mode);
            teardown.Add(new{Kind=mode,OwnerAlive=alive,Reads=calls,RefreshCount=hud.RefreshCount,RetirementCount=hud.RetirementCount,Caption=hud.Caption});if(alive)owner.Free();
        }
        owner=new();AddChild(owner);var existing=new TerrainOfflinePlaygroundHud();existing.Configure(owner,()=>new("Idle",0,0,120,120,false,true));string legacyCaption=existing.Caption;Check(existing.RefreshCount==1,"frozen default HUD admission");existing.Dispose();
        existing=new();existing.Configure(owner,()=>new("Idle",0,0,1000,1000,false,true),default,true);
        var limits=new TerrainOfflineLimitsHud();limits.Configure(owner,()=>new(1000,32,32,true,false));string limitsCaption=limits.Caption;
        hud=new();hud.Configure(owner,()=>new("BudgetCompleted",1000,1000,90,0,0,0));bool gpu=DisplayServer.GetName()!="headless";
        if(gpu){await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=GetViewport().GetTexture().GetImage();Check(image.SavePng(Path.ChangeExtension(output,"png"))==Error.Ok,"private GPU outcome HUD capture");}
        hud.Dispose();limits.Dispose();existing.Dispose();owner.Free();Check(GetChildCount()==0,"all HUD fixture nodes released");
        File.WriteAllText(output,JsonSerializer.Serialize(new{Passed=true,Checks=checks,Gpu=gpu,Rows=rows,Failures=failures,Teardown=teardown,LegacyCaption=legacyCaption,LimitsCaption=limitsCaption,InputPassThrough=true,BorrowedSurvival=true}));GD.Print("PASS offline outcome HUD");
    }
}
public partial class TerrainOutcomeInputProbe : Node
{public int Events;public override void _UnhandledInput(InputEvent input){Events++;}}
