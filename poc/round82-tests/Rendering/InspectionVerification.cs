using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

/// <summary>Synthetic status/control sources only; no original resource/full scene claims.</summary>
public partial class InspectionVerification : Node
{
    private int checks,factories,unhandled;
    private string output="",mode="",closeMode="";
    private PersistentOfflinePlayground? shell;
    private Window? window;
    private readonly List<object> rows=new();
    private string? caption;
    private bool completed;
    private void Check(bool good,string why){checks++;if(!good)throw new InvalidDataException(why);}
    public override async void _Ready(){try{await Run();}catch(Exception error){GD.PushError(error.ToString());GetTree().Quit(2);}}
    public override void _UnhandledInput(InputEvent input){if(input is InputEventMouseButton)unhandled++;}
    private async Task Frames(){await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
    private async Task Click(Vector2 point)
    {
        using(var motion=new InputEventMouseMotion{Position=point,GlobalPosition=point,Relative=Vector2.One})GetViewport().PushInput(motion,true);
        using(var down=new InputEventMouseButton{Position=point,GlobalPosition=point,ButtonIndex=MouseButton.Left,ButtonMask=MouseButtonMask.Left,Pressed=true})GetViewport().PushInput(down,true);
        using(var up=new InputEventMouseButton{Position=point,GlobalPosition=point,ButtonIndex=MouseButton.Left,Pressed=false})GetViewport().PushInput(up,true);
        await Frames();
    }
    private async Task Run()
    {
        var args=OS.GetCmdlineUserArgs();string Arg(string key)=>args[Array.IndexOf(args,key)+1];output=Arg("--out");mode=Arg("--state");closeMode=Arg("--close");string profile=Arg("--profile");
        GetViewport().NotifyMouseEntered();window=GetWindow();GetTree().AutoAcceptQuit=false;
        shell=new();shell.Configure(_=>new[]{"--terrain-point-target-profile",profile},run=>{factories++;if(mode=="reject"&&run==2)throw new IOException("authored candidate rejection");return new InspectionInertPreview();},mode=="cap"?1:2);AddChild(shell);await Frames();
        if(mode is "active" or "teardown")((InspectionInertPreview)shell.CurrentPreview!).FixtureReady();
        if(mode is "idle" or "reject" or "cap" or "terminal")
        {
            var report=mode=="terminal"?new OfflinePreviewCompletionReport("Faulted",3,2,3,1,0,0,0,true,true,false,1,0,0,1,3,2,"SyntheticFailure","SyntheticCleanupFailure",false):
                new OfflinePreviewCompletionReport("BudgetCompleted",3,3,3,1,0,0,0,true,true,true,0,0,0,0,3,3,null,"",false);
            ((InspectionInertPreview)shell.CurrentPreview!).FixtureComplete(report);
        }
        await Frames();var state=shell.InspectionStatus;caption=shell.InspectionCaption;
        Check(state.WindowCloseConnected&&state.CloseEnabled,"owned window and Close availability");Check(caption.Contains("Remaining: "+state.RemainingRuns)&&caption.Contains("Last updates:"),"visible primitive status");
        var layer=shell.GetNode<CanvasLayer>("OwnedOfflineInspection");var close=layer.GetNode<Button>("OfflineCloseButton");var label=layer.GetNode<Panel>("OfflineInspectionPanel").GetNode<Label>("OfflineInspectionStatus");
        Check(close.Position==new Vector2(272,484)&&close.Size==new Vector2(240,40)&&close.FocusMode==Control.FocusModeEnum.None&&close.MouseFilter==Control.MouseFilterEnum.Stop&&label.MouseFilter==Control.MouseFilterEnum.Ignore,"inspection control geometry/input");
        int priorUnhandled=unhandled;await Click(new(700,500));Check(unhandled==priorUnhandled+2,"inspection panel mouse pass-through");
        rows.Add(new{Phase="initial",Status=state,Caption=caption,Factories=factories,RestartRequests=shell.RestartHud?.RequestCount??0,RestartAccepted=shell.RestartHud?.AcceptedRestartCount??0});
        if(mode is "idle" or "reject" or "cap" or "terminal")
        {
            var hud=shell.RestartHud!;int prior=factories;await Click(new(30,500));
            if(mode is "cap" or "terminal")Check(factories==prior&&hud.RequestCount==0&&!hud.IsRestartEnabled&&!shell.RequestRestart(),"cap/terminal actual GUI restart disabled without request");
            else if(mode=="reject")Check(factories==prior+1&&hud.RequestCount==1&&hud.AcceptedRestartCount==0&&shell.OutcomeCaption==hud.Caption&&shell.InspectionStatus.RestartEnabled,"rejected GUI factory preserves outcome and retry");
            else
            {
                Check(factories==prior+1&&hud.AcceptedRestartCount==1&&shell.OutcomeCaption==hud.Caption,"accepted GUI candidate retains old outcome while loading");
                ((InspectionInertPreview)shell.CurrentPreview!).FixtureReady();await Frames();Check(shell.RestartHud is null&&hud.IsRetired&&shell.InspectionStatus.CurrentReady,"actual shell polling retires prior outcome at fixture readiness");
            }
            rows.Add(new{Phase="after-restart",Status=shell.InspectionStatus,Caption=shell.InspectionCaption,Factories=factories,RestartRequests=hud.RequestCount,RestartAccepted=hud.AcceptedRestartCount});
        }
        bool gpu=DisplayServer.GetName()!="headless";
        if(gpu){await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=GetViewport().GetTexture().GetImage();Check(image.SavePng(Path.ChangeExtension(output,"png"))==Error.Ok,"private inspection GPU capture");}
        if(mode=="teardown")
        {
            shell.Free();Check(!shell.InspectionStatus.WindowCloseConnected,"disconnect native exit");window.EmitSignal(Window.SignalName.CloseRequested);Check(shell.InspectionStatus.CloseRequests==0,"late synthetic signal has no retired callback");completed=true;Write(gpu);GetTree().Quit();return;
        }
        // Close occurs after the next process signal; capture primitive final status from teardown.
        completed=true;
        if(closeMode=="signal"){window.EmitSignal(Window.SignalName.CloseRequested);window.EmitSignal(Window.SignalName.CloseRequested);shell.Notification((int)NotificationWMCloseRequest);}
        else
        {
            using(var motion=new InputEventMouseMotion{Position=new(286,500),GlobalPosition=new(286,500),Relative=Vector2.One})GetViewport().PushInput(motion,true);
            using(var down=new InputEventMouseButton{Position=new(286,500),GlobalPosition=new(286,500),ButtonIndex=MouseButton.Left,ButtonMask=MouseButtonMask.Left,Pressed=true})GetViewport().PushInput(down,true);
            using(var up=new InputEventMouseButton{Position=new(286,500),GlobalPosition=new(286,500),ButtonIndex=MouseButton.Left,Pressed=false})GetViewport().PushInput(up,true);
            Check(shell.InspectionStatus.CloseRequests==1&&shell.InspectionStatus.Closing,"actual GUI Close admitted before duplicate direct intent");
            shell.RequestClose(); // Duplicate same intent is idempotent; positive GUI path must already admit once.
        }
        Check(shell.InspectionStatus.CloseRequests==1&&shell.InspectionStatus.Closing,"one close intent across routes");
    }
    private void Write(bool gpu)=>File.WriteAllText(output,JsonSerializer.Serialize(new{Passed=true,Checks=checks,Gpu=gpu,Mode=mode,CloseMode=closeMode,Rows=rows,Final=shell!.InspectionStatus,Factories=factories,SyntheticCloseSignal=closeMode=="signal"||mode=="teardown",PhysicalOsClose=false,Source="SyntheticAuthoredCompletionAndReadyFixtures",Caption=caption}));
    public override void _ExitTree(){if(completed&&shell is not null){try{Write(DisplayServer.GetName()!="headless");}catch(Exception error){GD.PushError("Inspection receipt: "+error);}}}
}
public partial class InspectionInertPreview : ScenePumpedWorldPreview
{ public override void _Ready(){} }
public partial class ScenePumpedWorldPreview
{
    public void FixtureReady()=>offlineRestartReachedReady=true;
    public void FixtureComplete(OfflinePreviewCompletionReport fixture)
    {
        offlineRestartReachedReady=fixture.ReadyReached;offlineRestartSnapshot=fixture;offlineRestartDelivered=true;
        var callback=offlineRestartCompleted;offlineRestartCompleted=null;callback?.Invoke(fixture);
    }
}
