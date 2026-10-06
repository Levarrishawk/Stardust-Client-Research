using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public partial class TerrainOfflinePlaygroundHudVerification : Node3D
{
    private int checks;
    private void Check(bool value,string why){checks++;if(!value)throw new InvalidOperationException(why);}
    private void Reject(Action call){bool rejected=false;try{call();}catch(Exception e) when(e is InvalidOperationException or ArgumentException or OperationCanceledException or InvalidDataException or IOException){rejected=true;}Check(rejected,"Expected HUD rejection");}
    private string Actual(Node owner)=>string.Join("\n",new[]{"OfflinePlaygroundTitle","OfflinePlaygroundStatus","OfflinePlaygroundControls"}.Select(n=>((Label)owner.FindChild(n,true,false)).Text));
    private static IEnumerable<Node> Descendants(Node node){foreach(Node child in node.GetChildren()){yield return child;foreach(var nested in Descendants(child))yield return nested;}}
    public override async void _Ready(){try{await Run();GetTree().Quit();}catch(Exception error){GD.PushError("HUD: "+error);GetTree().Quit(1);}}
    private async Task Run()
    {
        var args=OS.GetCmdlineUserArgs();string Arg(string key){int i=Array.IndexOf(args,key);if(i<0)throw new ArgumentException(key);return args[i+1];}
        string output=Arg("--out");var states=JsonSerializer.Deserialize<TerrainOfflinePlaygroundHudState[]>(File.ReadAllText(Arg("--fixtures")))!;
        var owner=new Node3D();AddChild(owner);TerrainOfflinePlaygroundHudState state=states[0];int reads=0;
        var hud=new TerrainOfflinePlaygroundHud();hud.Configure(owner,()=>{reads++;return state;});
        Check(hud.IsConfigured&&!hud.IsRetired&&reads==1&&hud.RefreshCount==1,"configure single callback and display");
        var rows=new List<object>();
        for(int i=0;i<states.Length;i++)
        {
            state=states[i];int before=reads;if(i>0)Check(hud.Refresh(),"state refresh");
            Check(reads==before+(i>0?1:0)&&Actual(owner)==hud.Caption,"actual labels equal caption and exact callback count");
            rows.Add(new{Index=i,Caption=hud.Caption,Actual=Actual(owner),RefreshCount=hud.RefreshCount});
        }
        var controls=Descendants(owner).OfType<Control>().ToArray();Check(controls.Length==4&&controls.All(c=>c.MouseFilter==Control.MouseFilterEnum.Ignore&&c.FocusMode==Control.FocusModeEnum.None),"every owned control noninteractive");
        var probe=new TerrainOfflineHudInputProbe();AddChild(probe);var camera=new Camera3D{Current=true};AddChild(camera);var orbit=new TerrainCameraOrbitHost();AddChild(orbit);orbit.Configure(camera,Vector3.Zero,.2,.4,8);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        using(var key=new InputEventKey{Pressed=true,Keycode=Key.W})GetViewport().PushInput(key,true);
        using(var click=new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Left,Position=new(32,32),GlobalPosition=new(32,32)})GetViewport().PushInput(click,true);
        int accepted=orbit.AcceptedCount;
        using(var drag=new InputEventMouseMotion{Relative=new(10,2),ButtonMask=MouseButtonMask.Right,Position=new(32,32),GlobalPosition=new(32,32)})GetViewport().PushInput(drag,true);
        Check(probe.Keys==1&&probe.LeftClicks==1&&orbit.AcceptedCount==accepted+1&&orbit.HandledCount==1,"actual key/click reaches world and right-drag reaches existing orbit through HUD");
        Check(GetViewport().GuiGetFocusOwner() is null,"HUD never acquires GUI focus");orbit.Close();orbit.Free();camera.Free();probe.Free();
        int heldReads=reads;string heldCaption=hud.Caption;
        await Task.Run(()=>{foreach(Action call in new Action[]{()=>hud.Refresh(),()=>hud.Dispose(),()=>{_ = hud.Caption;},()=>hud.Configure(owner,()=>state)}){try{call();throw new Exception("Off-thread accepted");}catch(InvalidOperationException){}}});
        Check(reads==heldReads&&!hud.IsRetired&&hud.Caption==heldCaption,"off-thread rejection preserves caller/HUD");
        hud.Dispose();hud.Dispose();Check(hud.RetirementCount==1&&owner.GetChildCount()==0&&GodotObject.IsInstanceValid(owner),"idempotent retirement borrows owner");
        Check(!hud.Refresh()&&reads==heldReads,"retired no callback");owner.Free();
        var failures=new List<string>();
        foreach(string kind in new[]{"mode","speed-nan","speed-negative","speed-large","updates","maximum","remaining","online","null","source-throw","refresh-reentry","dispose-reentry","callback-cancel","initial-cancel","freed-root","freed-owner","reparent-owner","detached-owner","callback-parent-loss","configure-again","unconfigured"})
        {
            var parent=new Node3D();AddChild(parent);var other=new Node3D();AddChild(other);owner=new();parent.AddChild(owner);bool fail=false;int sourceReads=0;using var cancel=new CancellationTokenSource();hud=new();
            TerrainOfflinePlaygroundHudState Source()
            {
                sourceReads++;if(!fail)return states[0];
                if(kind=="source-throw")throw new IOException("Synthetic HUD source");
                if(kind=="refresh-reentry"){try{hud.Refresh();}catch(InvalidOperationException){}}
                if(kind=="dispose-reentry"){try{hud.Dispose();}catch(InvalidOperationException){}}
                if(kind=="callback-cancel")cancel.Cancel();
                if(kind=="callback-parent-loss")owner.Reparent(other);
                return kind switch {
                    "mode"=>states[0] with{Mode="Unknown"},"speed-nan"=>states[0] with{Speed=double.NaN},
                    "speed-negative"=>states[0] with{Speed=-1},"speed-large"=>states[0] with{Speed=11},
                    "updates"=>states[0] with{Updates=6001},"maximum"=>states[0] with{MaximumUpdates=0},
                    "remaining"=>states[0] with{RemainingUpdates=-1},"online"=>states[0] with{Offline=false},"null"=>null!,_=>states[0]};
            }
            if(kind!="unconfigured")hud.Configure(owner,Source);
            fail=true;int before=sourceReads;
            if(kind=="initial-cancel")cancel.Cancel();
            if(kind=="freed-root")owner.GetChild(0).Free();
            if(kind=="freed-owner")owner.Free();
            if(kind=="reparent-owner")owner.Reparent(other);
            if(kind=="detached-owner")parent.RemoveChild(owner);
            if(kind=="configure-again")Reject(()=>hud.Configure(owner,Source));else Reject(()=>hud.Refresh(cancel.Token));
            Check(hud.IsRetired&&hud.RetirementCount==1&&!hud.Refresh()&&sourceReads<=before+1,"failure single bounded source/retirement "+kind);
            hud.Dispose();Check(hud.RetirementCount==1,"failure retirement idempotent "+kind);
            if(GodotObject.IsInstanceValid(owner)){Check(!Descendants(owner).OfType<CanvasLayer>().Any(),"owned display removed "+kind);if(kind=="detached-owner")owner.Free();}
            parent.Free();other.Free();failures.Add(kind);
        }
        var detached=new Node3D();hud=new();Reject(()=>hud.Configure(detached,()=>states[0]));Check(hud.IsRetired&&hud.RetirementCount==1&&GodotObject.IsInstanceValid(detached),"detached Configure owns nothing");hud.Dispose();detached.Free();
        foreach(string kind in new[]{"configure-source-reentry","configure-cancel","attach-reentry","freed-label"})
        {
            owner=new();AddChild(owner);hud=new();int calls=0;using var token=new CancellationTokenSource();
            TerrainOfflinePlaygroundHudState Source(){calls++;if(kind=="configure-source-reentry"){try{hud.Refresh();}catch(InvalidOperationException){}}if(kind=="configure-cancel")token.Cancel();return states[0];}
            void Attached(Node child){if(child.Name=="OfflinePlaygroundHud"){try{hud.Dispose();}catch(InvalidOperationException){}}}
            if(kind=="attach-reentry")owner.ChildEnteredTree+=Attached;
            if(kind=="freed-label"){hud.Configure(owner,Source);owner.FindChild("OfflinePlaygroundTitle",true,false).Free();Reject(()=>hud.Refresh());}
            else Reject(()=>hud.Configure(owner,Source,token.Token));
            Check(hud.IsRetired&&hud.RetirementCount==1&&owner.GetChildCount()==0&&calls==(kind=="attach-reentry"?0:1),"configure/child callback guard "+kind);
            if(kind=="attach-reentry")owner.ChildEnteredTree-=Attached;hud.Dispose();owner.Free();
        }
        owner=new();AddChild(owner);hud=new();hud.Configure(owner,()=>states[3]);bool gpu=DisplayServer.GetName()!="headless";
        if(gpu){await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=GetViewport().GetTexture().GetImage();Check(image.SavePng(Path.ChangeExtension(output,"png"))==Error.Ok,"private GPU HUD capture");}
        hud.Dispose();owner.Free();Check(GetChildCount()==0,"all owned verification nodes drained");
        File.WriteAllText(output,JsonSerializer.Serialize(new{Passed=true,Checks=checks,Gpu=gpu,Rows=rows,Failures=failures,InputPassThrough=true,BorrowedSurvival=true}));GD.Print("PASS offline playground HUD");
    }
}
public partial class TerrainOfflineHudInputProbe : Node
{
    public int Keys,LeftClicks;
    public override void _UnhandledInput(InputEvent input){if(input is InputEventKey{Pressed:true,Keycode:Key.W})Keys++;if(input is InputEventMouseButton{Pressed:true,ButtonIndex:MouseButton.Left})LeftClicks++;}
}
