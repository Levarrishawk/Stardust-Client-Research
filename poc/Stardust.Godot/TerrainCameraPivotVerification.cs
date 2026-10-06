using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public partial class TerrainCameraPivotVerification : Node3D
{
    private int checks;
    private void Check(bool v,string why){checks++;if(!v)throw new InvalidOperationException(why);}
    private void Reject(Action call){bool rejected=false;try{call();}catch(Exception e) when(e is InvalidOperationException or ArgumentException or OperationCanceledException or InvalidDataException){rejected=true;}Check(rejected,"Expected rejection");}
    private static double[] V(Vector3 v)=>new[]{(double)v.X,v.Y,v.Z};
    private static double[][] B(Basis b)=>new[]{V(b.X),V(b.Y),V(b.Z)};
    public override async void _Ready(){try{await Run();GetTree().Quit();}catch(Exception e){GD.PushError("Pivot: "+e);GetTree().Quit(1);}}
    private async Task Run()
    {
        var args=OS.GetCmdlineUserArgs();string Arg(string key){int i=Array.IndexOf(args,key);if(i<0)throw new ArgumentException(key);return args[i+1];}
        string output=Arg("--out");using var doc=JsonDocument.Parse(File.ReadAllText(Arg("--fixtures")));
        var camera=new TerrainOrbitCallbackCamera{Current=true,Fov=50,Near=.05f,Far=200};AddChild(camera);camera.SetNotifyLocalTransform(true);
        var rows=new List<object>();TerrainCameraOrbitHost? h=null;Vector3 finalPivot=default;
        foreach(var group in doc.RootElement.GetProperty("Groups").EnumerateArray())
        {
            h?.Close();h?.Free();h=new();AddChild(h);h.Configure(camera,new(0,1,0),group.GetProperty("Yaw").GetDouble(),group.GetProperty("Pitch").GetDouble(),group.GetProperty("Distance").GetDouble());
            using(var motion=new InputEventMouseMotion{ButtonMask=MouseButtonMask.Right,Relative=new(10,3)})Check(h.Dispatch(motion),"prior input counter");
            h.SetEnabled(false);
            foreach(var p in group.GetProperty("Pivots").EnumerateArray())
            {
                var pivot=new Vector3(p[0].GetSingle(),p[1].GetSingle(),p[2].GetSingle());int attempts=h.PoseAttemptCount,revision=h.Revision,accepted=h.AcceptedCount,handled=h.HandledCount;
                double yaw=h.Yaw,pitch=h.Pitch,distance=h.Distance;
                Check(h.SetPivot(pivot)&&h.Pivot==pivot&&h.Revision==revision+1&&h.PoseAttemptCount==attempts+1,"committed pivot counters");
                Check(!h.IsEnabled&&h.Yaw==yaw&&h.Pitch==pitch&&h.Distance==distance&&h.AcceptedCount==accepted&&h.HandledCount==handled,"disabled explicit follow preserves input and orbit state");
                Check(h.LastDiagnostic is {Kind:"Pivot",PositionReturned:true,LookAtReturned:true,StateCommitted:true,FromGodotEvent:false},"honest complete effects");
                Check(!h.SetPivot(pivot)&&h.Revision==revision+1&&h.PoseAttemptCount==attempts+1,"equal pivot no mutation");
                Vector2 viewport=GetViewport().GetVisibleRect().Size,pixel=new(viewport.X*.6f,viewport.Y*.4f);
                rows.Add(new{Position=V(camera.GlobalPosition),Basis=B(camera.GlobalBasis),Pivot=V(h.Pivot),Yaw=h.Yaw,Pitch=h.Pitch,Distance=h.Distance,Attempts=h.PoseAttemptCount,Revision=h.Revision,Accepted=h.AcceptedCount,Handled=h.HandledCount,Viewport=new[]{viewport.X,viewport.Y},Pixel=new[]{pixel.X,pixel.Y},RayOrigin=V(camera.ProjectRayOrigin(pixel)),RayDirection=V(camera.ProjectRayNormal(pixel))});finalPivot=pivot;
            }
        }
        var healthy=h!;var old=camera.GlobalTransform;var oldPivot=healthy.Pivot;int oldAttempts=healthy.PoseAttemptCount,oldRevision=healthy.Revision;var diagnostic=healthy.LastDiagnostic;
        foreach(var bad in new[]{new Vector3(float.NaN,0,0),new Vector3(0,float.PositiveInfinity,0),new Vector3(1000001,0,0)})Reject(()=>healthy.SetPivot(bad));
        Reject(()=>healthy.SetPivot(new(2,3,4),new CancellationToken(true)));
        Check(camera.GlobalTransform==old&&healthy.Pivot==oldPivot&&!healthy.IsFaulted&&healthy.PoseAttemptCount==oldAttempts&&healthy.Revision==oldRevision&&ReferenceEquals(diagnostic,healthy.LastDiagnostic),"malformed/cancel preflight unchanged");
        await Task.Run(()=>{try{healthy.SetPivot(new(2,3,4));throw new Exception("Worker accepted");}catch(InvalidOperationException){}});
        Check(!healthy.IsFaulted&&camera.GlobalTransform==old,"off-thread before effects");
        healthy.Close();Check(GodotObject.IsInstanceValid(camera)&&camera.GlobalTransform==old,"borrowed camera survives close");Reject(()=>healthy.SetPivot(new(2,3,4)));healthy.Free();
        foreach(double tinyDistance in new[]{.001,.25})
        {
        var precision=new TerrainCameraOrbitHost();AddChild(precision);precision.Configure(camera,Vector3.Zero,.3,.1,tinyDistance,new(MinimumDistance:tinyDistance,MaximumDistance:tinyDistance));
        var precisionPose=camera.GlobalTransform;var precisionDiagnostic=precision.LastDiagnostic;
        Reject(()=>precision.SetPivot(new(1e6f,1e6f,1e6f)));
        Check(!precision.IsFaulted&&precision.Pivot==Vector3.Zero&&precision.Revision==1&&precision.PoseAttemptCount==1&&camera.GlobalTransform==precisionPose&&ReferenceEquals(precisionDiagnostic,precision.LastDiagnostic),"rounded binary32 collapse rejected before effects");
        precision.Close();precision.Free();
        }
        foreach(string kind in new[]{"reentry","cancel-after-position","projection-after-position","reparent-after-position","freed","queued","unconfigured"})
        {
            var host=new TerrainCameraOrbitHost();AddChild(host);var parent=new Node3D();AddChild(parent);using var token=new CancellationTokenSource();
            if(kind!="unconfigured")host.Configure(camera,new(0,1,0),.3,.5,8);
            var priorPivot=host.Pivot;int priorRevision=host.Revision,priorAttempts=host.PoseAttemptCount;bool called=false;
            if(kind=="freed")camera.Free();
            if(kind=="queued")camera.QueueFree();
            if(kind is "reentry" or "cancel-after-position" or "projection-after-position" or "reparent-after-position")camera.Hook=()=>
            {
                if(called)return;called=true;
                if(kind=="reentry"){try{host.SetPivot(new(3,4,5));}catch(InvalidOperationException){}}
                if(kind=="cancel-after-position")token.Cancel();
                if(kind=="projection-after-position")camera.Projection=Camera3D.ProjectionType.Orthogonal;
                if(kind=="reparent-after-position")camera.Reparent(parent);
            };
            Reject(()=>host.SetPivot(new(9,8,7),token.Token));
            bool effect=kind is "reentry" or "cancel-after-position" or "projection-after-position" or "reparent-after-position";
            Check(host.Pivot==priorPivot&&host.Revision==priorRevision&&host.AcceptedCount==0&&host.HandledCount==0,"failed update stable state");
            Check(host.IsFaulted==effect&&host.PoseAttemptCount==priorAttempts+(effect?1:0),"post-effect terminal versus preflight");
            if(effect)Check(called&&host.LastDiagnostic is {PositionMayHaveChanged:true,PositionReturned:true,StateCommitted:false}&&camera.GlobalPosition!=new Vector3(0,0,0),"honest position prefix");
            if(effect){int held=host.PoseAttemptCount;Reject(()=>host.SetPivot(new(1,2,3)));Check(host.PoseAttemptCount==held,"faulted host rejects further effects");}
            if(GodotObject.IsInstanceValid(camera)){camera.Hook=null;if(kind=="reparent-after-position")camera.Reparent(this);camera.Projection=Camera3D.ProjectionType.Perspective;}
            host.Close();host.Free();parent.Free();
            if(kind is "freed" or "queued")
            {if(GodotObject.IsInstanceValid(camera))camera.Free();camera=new(){Current=true,Fov=50,Near=.05f,Far=200};AddChild(camera);camera.SetNotifyLocalTransform(true);}
        }
        h=new();AddChild(h);h.Configure(camera,new(0,1,0),.2,.4,8);Check(h.SetPivot(finalPivot),"final actual follow capture pose");var mesh=new SphereMesh{Radius=.35f,Height=.7f};var material=new StandardMaterial3D{ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,AlbedoColor=new(0,.8f,.9f)};var target=new MeshInstance3D{Mesh=mesh,MaterialOverride=material,Position=finalPivot};AddChild(target);
        bool gpu=DisplayServer.GetName()!="headless";
        if(gpu){await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=GetViewport().GetTexture().GetImage();var center=image.GetPixel(image.GetWidth()/2,image.GetHeight()/2);Check(center.G>.6f&&center.B>.7f&&center.R<.2f,"GPU camera faces pivot target");Check(image.SavePng(Path.ChangeExtension(output,"png"))==Error.Ok,"GPU private PNG");}
        h.Close();h.Free();camera.Free();target.Free();mesh.Dispose();material.Dispose();Check(GetChildCount()==0,"owned scene cleanup");
        File.WriteAllText(output,JsonSerializer.Serialize(new{Passed=true,Checks=checks,Gpu=gpu,Rows=rows,FailureCases=7,BorrowedSurvival=true,NoRuntimeDependency=true}));GD.Print("PASS terrain camera pivot");
    }
}
