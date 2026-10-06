using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public partial class TerrainCameraOrbitHostVerification : Node3D
{
    private static double[] V(Vector3 v)=>new[]{(double)v.X,v.Y,v.Z};
    private static double[][] B(Basis b)=>new[]{V(b.X),V(b.Y),V(b.Z)};
    private static void Need(bool value,string why){if(!value)throw new InvalidOperationException(why);}
    public override async void _Ready(){try{await Run();GetTree().Quit();}catch(Exception e){GD.PushError("Orbit: "+e);GetTree().Quit(1);}}
    private async Task Run()
    {
        string[] args=OS.GetCmdlineUserArgs();string Arg(string n){int i=Array.IndexOf(args,n);Need(i>=0,"Explicit argument "+n);return args[i+1];}
        string output=Path.GetFullPath(Arg("--out")),repository=Path.GetFullPath(Arg("--repository"));Need(output.StartsWith(Path.Combine(repository,"local-private")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)&&!File.Exists(output),"Fresh private receipt");
        using var doc=JsonDocument.Parse(File.ReadAllText(Arg("--fixtures")));var f=doc.RootElement;var pv=f.GetProperty("Pivot");var pivot=new Vector3(pv[0].GetSingle(),pv[1].GetSingle(),pv[2].GetSingle());var policy=f.GetProperty("Policy").Deserialize<TerrainCameraOrbitPolicy>()!;
        int checks=0;void Check(bool v,string why){Need(v,why);checks++;}void Reject(Action call){try{call();}catch(Exception e) when(e is InvalidOperationException or ArgumentException or OperationCanceledException or InvalidDataException){checks++;return;}throw new InvalidOperationException("Expected rejection");}
        var probe=new TerrainOrbitPropagationProbe();AddChild(probe);var parent=new Node();AddChild(parent);var camera=new TerrainOrbitCallbackCamera{Current=true,Fov=50,Near=.05f,Far=200};AddChild(camera);camera.SetNotifyLocalTransform(true);
        var host=new TerrainCameraOrbitHost();parent.AddChild(host);var gui=new TerrainOrbitGuiConsumer{FocusMode=Control.FocusModeEnum.All,MouseFilter=Control.MouseFilterEnum.Ignore,Size=GetViewport().GetVisibleRect().Size};AddChild(gui);
        var mesh=new SphereMesh{Radius=.35f,Height=.7f};var material=new StandardMaterial3D{ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,AlbedoColor=new(0,.8f,.9f)};var target=new MeshInstance3D{Mesh=mesh,MaterialOverride=material,Position=pivot};AddChild(target);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);Vector2 center=GetViewport().GetVisibleRect().Size/2;
        InputEventMouseMotion Motion(float x,float y,bool right=true)=>new(){Position=center,GlobalPosition=center,Relative=new(x,y),ButtonMask=right?MouseButtonMask.Right:MouseButtonMask.Left};
        InputEventMouseButton Wheel(bool up=true,bool pressed=true)=>new(){Position=center,GlobalPosition=center,ButtonIndex=up?MouseButton.WheelUp:MouseButton.WheelDown,Pressed=pressed};
        bool Push(InputEvent e){int before=probe.Count;GetViewport().PushInput(e,true);return probe.Count==before;}
        host.Configure(camera,pivot,f.GetProperty("Yaw").GetDouble(),f.GetProperty("Pitch").GetDouble(),f.GetProperty("Distance").GetDouble(),policy);
        Check(host.IsConfigured&&host.IsEnabled&&host.PoseAttemptCount==1&&host.Revision==1&&host.AcceptedCount==0&&host.HandledCount==0&&ReferenceEquals(host.Camera,camera),"Actual configured pose, counters and borrowed identity.");Reject(()=>host.Configure(camera,pivot,0,.4,8,policy));
        var rows=new List<object>();bool guiConsumed=false,focusDisabled=false;int index=0;
        foreach(var a in f.GetProperty("Actions").EnumerateArray())
        {
            bool? consumed=null;string type=a.GetProperty("Type").GetString()!;int before=host.AcceptedCount;
            switch(type)
            {
                case "Motion":using(var e=Motion(a.GetProperty("X").GetSingle(),a.GetProperty("Y").GetSingle(),a.GetProperty("Right").GetBoolean()))consumed=Push(e);break;
                case "Wheel":using(var e=Wheel(a.GetProperty("Up").GetBoolean(),a.GetProperty("Pressed").GetBoolean()))consumed=Push(e);break;
                case "Gui":gui.Capture=a.GetProperty("Enabled").GetBoolean();gui.MouseFilter=gui.Capture?Control.MouseFilterEnum.Stop:Control.MouseFilterEnum.Ignore;if(gui.Capture){gui.GrabFocus();Check(GetViewport().GuiGetFocusOwner()==gui,"Actual focused GUI Control.");}else gui.ReleaseFocus();break;
                case "Enable":host.SetEnabled(a.GetProperty("Enabled").GetBoolean());break;
                case "FocusOut":GetWindow().PropagateNotification((int)NotificationWMWindowFocusOut);focusDisabled|=!host.IsEnabled;break;
                case "FocusIn":GetWindow().PropagateNotification((int)NotificationWMWindowFocusIn);break;
                default:throw new InvalidDataException("Unknown action");
            }
            if(gui.Capture&&consumed is true)guiConsumed|=before==host.AcceptedCount;
            Check(!host.IsFaulted&&camera.Projection==Camera3D.ProjectionType.Perspective&&camera.Fov==50&&camera.Near==.05f&&camera.Far==200,"Healthy pose and untouched borrowed camera projection settings.");
            if(host.AcceptedCount!=before)Check(host.LastDiagnostic is {PositionReturned:true,LookAtReturned:true,StateCommitted:true,FailureType:null,FromGodotEvent:true},"Actual completed camera mutations before stable commit.");
            Vector2 viewport=GetViewport().GetVisibleRect().Size,pixel=new(viewport.X*.6f,viewport.Y*.4f);
            rows.Add(new{Index=index,Yaw=host.Yaw,Pitch=host.Pitch,Distance=host.Distance,Enabled=host.IsEnabled,Accepted=host.AcceptedCount,Handled=host.HandledCount,Attempts=host.PoseAttemptCount,Revision=host.Revision,GuiCaptured=gui.Captured,Consumed=consumed,Position=V(camera.GlobalPosition),Basis=B(camera.GlobalBasis),Viewport=new[]{viewport.X,viewport.Y},Pixel=new[]{pixel.X,pixel.Y},RayOrigin=V(camera.ProjectRayOrigin(pixel)),RayDirection=V(camera.ProjectRayNormal(pixel))});index++;
        }
        Check(guiConsumed&&focusDisabled&&!probe.SawHandled,"Actual GUI consumption, focus disable and later unhandled observer.");
        bool gpu=DisplayServer.GetName()!="headless";if(gpu){await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=GetViewport().GetTexture().GetImage();var pixel=image.GetPixel((int)center.X,(int)center.Y);Check(pixel.G>.6f&&pixel.B>.7f&&pixel.R<.2f,"Actual orbit looks at supplied cyan target.");Need(image.SavePng(Path.ChangeExtension(output,".png"))==Error.Ok,"Own capture");}
        Vector3 finalPosition=camera.GlobalPosition;Basis finalBasis=camera.GlobalBasis;host.Close();host.Close();bool survives=host.Camera is null&&GodotObject.IsInstanceValid(camera)&&camera.GlobalPosition==finalPosition&&camera.GlobalBasis==finalBasis;Check(survives,"Close drops borrowed reference without reset/free.");parent.Free();
        var detached=new TerrainCameraOrbitHost();Reject(()=>detached.Configure(camera,pivot,0,.4,8,policy));detached.Close();detached.Free();
        foreach(var bad in new[]{policy with{MinimumPitchRadians=0},policy with{MaximumPitchRadians=Math.PI/2},policy with{MinimumPitchRadians=1.3},policy with{MinimumDistance=0},policy with{MaximumDistance=double.MaxValue},policy with{RadiansPerPixel=double.NaN},policy with{WheelDistanceStep=0}})
        {var p=new Node();AddChild(p);var h=new TerrainCameraOrbitHost();p.AddChild(h);var old=camera.GlobalTransform;Reject(()=>h.Configure(camera,pivot,0,.4,8,bad));Check(camera.GlobalTransform==old&&h.PoseAttemptCount==0,"Policy rejected before camera effects.");h.Close();p.Free();}
        var boundsParent=new Node();AddChild(boundsParent);var bounds=new TerrainCameraOrbitHost();boundsParent.AddChild(bounds);Reject(()=>bounds.Configure(camera,new(float.MaxValue,0,0),0,.4,8,policy));Reject(()=>bounds.Configure(camera,pivot,double.MaxValue,.4,8,policy));Reject(()=>bounds.Configure(camera,pivot,0,double.NaN,8,policy));Reject(()=>bounds.Configure(camera,pivot,0,.4,double.PositiveInfinity,policy));Reject(()=>bounds.Configure(camera,pivot,0,.4,8,policy,new CancellationToken(true)));var boundsPose=camera.GlobalTransform;
        Reject(()=>bounds.Configure(camera,Vector3.Zero,0,Math.BitDecrement(Math.PI/2),1e6,policy with{MaximumPitchRadians=Math.BitDecrement(Math.PI/2),MaximumDistance=1e6}));
        Reject(()=>bounds.Configure(camera,Vector3.One*1e6f,0,.4,.001,policy with{MinimumDistance=.001,MaximumDistance=1}));
        Check(camera.GlobalTransform==boundsPose&&bounds.PoseAttemptCount==0&&bounds.Camera is null,"Near-pole and rounded-collapse candidates reject before effects and tentative references clear.");bounds.Close();boundsParent.Free();
        // Actual local-transform notification catches nested host Close after position assignment.
        var prefixParent=new Node();AddChild(prefixParent);var prefix=new TerrainCameraOrbitHost();prefixParent.AddChild(prefix);prefix.Configure(camera,pivot,.2,.4,8,policy);var oldPosition=camera.GlobalPosition;var oldBasis=camera.GlobalBasis;double oldYaw=prefix.Yaw;int nested=0;
        camera.Hook=()=>{camera.Hook=null;try{prefix.Close();}catch(InvalidOperationException){nested++;}};using(var e=Motion(10,0)){Check(!Push(e),"Mutation-prefix event unhandled.");}
        bool mutationPrefix=nested==1&&prefix.IsFaulted&&prefix.Yaw==oldYaw&&prefix.AcceptedCount==0&&prefix.HandledCount==0&&camera.GlobalPosition!=oldPosition&&camera.GlobalBasis==oldBasis&&prefix.LastDiagnostic is {PositionMayHaveChanged:true,PositionReturned:true,LookAtMayHaveRun:false,StateCommitted:false};Check(mutationPrefix,"Completed Godot position prefix retained without scalar commit or rollback.");prefix.Close();prefixParent.Free();
        var suffixParent=new Node();AddChild(suffixParent);var suffix=new TerrainCameraOrbitHost();suffixParent.AddChild(suffix);suffix.Configure(camera,pivot,.2,.4,8,policy);var suffixBasis=camera.GlobalBasis;int notifications=0,suffixNested=0;
        camera.Hook=()=>{if(++notifications!=2)return;camera.Hook=null;try{suffix.Close();}catch(InvalidOperationException){suffixNested++;}};
        using(var e=Motion(10,0)){Check(!Push(e),"Completed look-at prefix event unhandled.");}camera.Hook=null;
        Check(suffixNested==1&&suffix.IsFaulted&&suffix.Yaw==.2&&suffix.Revision==1&&suffix.AcceptedCount==0&&suffix.HandledCount==0&&camera.GlobalBasis!=suffixBasis&&suffix.LastDiagnostic is {PositionReturned:true,LookAtMayHaveRun:true,LookAtReturned:true,StateCommitted:false},"Both actual camera mutations can finish before caught reentry prevents stable commit.");suffix.Close();suffixParent.Free();
        foreach(string failure in new[]{"Token","BadRelative","Orthographic","Inactive","Reparent"})
        {
            var p=new Node();AddChild(p);var h=new TerrainCameraOrbitHost();p.AddChild(h);h.Configure(camera,pivot,.2,.4,8,policy);var old=camera.GlobalTransform;var other=new Node3D();AddChild(other);
            if(failure=="Orthographic")camera.Projection=Camera3D.ProjectionType.Orthogonal;if(failure=="Inactive")camera.Current=false;if(failure=="Reparent")camera.Reparent(other);
            using var e=Motion(failure=="BadRelative"?float.MaxValue:10,0);
            if(failure=="Token")Reject(()=>h.Dispatch(e,new CancellationToken(true)));else Check(!Push(e),"Actual negative event remains unhandled.");
            Check(h.AcceptedCount==0&&h.PoseAttemptCount==1&&camera.GlobalTransform==old,"Negative rejection before further camera mutation.");
            if(failure=="Reparent")camera.Reparent(this);camera.Projection=Camera3D.ProjectionType.Perspective;camera.Current=true;h.Close();p.Free();other.Free();
        }
        var threadParent=new Node();AddChild(threadParent);var thread=new TerrainCameraOrbitHost();threadParent.AddChild(thread);thread.Configure(camera,pivot,.2,.4,8,policy);using(var e=Motion(10,0)){await Task.Run(()=>{foreach(Action call in new Action[]{()=>thread.Dispatch(e),()=>thread.SetEnabled(false),()=>thread.Close()}){try{call();throw new Exception("Off-thread accepted");}catch(InvalidOperationException){}}});}Check(!thread.IsClosed&&thread.AcceptedCount==0,"Wrong owner thread rejected.");thread.Close();threadParent.Free();
        var freeParent=new Node();AddChild(freeParent);var freeHost=new TerrainCameraOrbitHost();freeParent.AddChild(freeHost);freeHost.Configure(camera,pivot,.2,.4,8,policy);camera.Free();using(var e=Motion(10,0)){Check(!Push(e),"Freed camera event unhandled.");}Check(freeHost.IsFaulted&&freeHost.AcceptedCount==0&&freeHost.PoseAttemptCount==1,"Freed borrowed camera rejected before effects.");freeParent.Free();Check(freeHost.IsClosed,"Actual host ExitTree closes.");
        target.Free();mesh.Dispose();material.Dispose();gui.Free();probe.Free();Check(GetChildCount()==0,"All owned scene nodes/resources drained.");
        File.WriteAllText(output,JsonSerializer.Serialize(new{Gpu=gpu,Rows=rows,NativeChecks=checks,BorrowedSurvival=survives,MutationPrefix=mutationPrefix,GuiConsumed=guiConsumed,FocusDisabled=focusDisabled,Cleanup=true}),new System.Text.UTF8Encoding(false));GD.Print("PASS terrain camera orbit host");
    }
}
public partial class TerrainOrbitPropagationProbe : Node
{
    public int Count {get;private set;}public bool SawHandled {get;private set;}
    public override void _UnhandledInput(InputEvent input){Count++;SawHandled|=GetViewport().IsInputHandled();}
}
public partial class TerrainOrbitGuiConsumer : Control
{
    public bool Capture {get;set;}public int Captured {get;private set;}
    public override void _GuiInput(InputEvent input){if(Capture){Captured++;AcceptEvent();}}
}
public partial class TerrainOrbitCallbackCamera : Camera3D
{
    public Action? Hook {get;set;}
    public override void _Notification(int what){if(what==(int)NotificationLocalTransformChanged)Hook?.Invoke();}
}
