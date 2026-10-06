using System;
using System.Text.Json;
using Godot;
using Stardust.Core.Session;
using NativeVector2=System.Numerics.Vector2;
using NativeVector3=System.Numerics.Vector3;

public partial class ScenePumpedWorldPreview
{
    private bool pointCameraRelativeHosted;
    private TerrainCameraOrbitHost? cameraOrbitHost;
    private Camera3D? orbitCamera;
    private void PrepareCameraOrbitHost(Camera3D camera)
    {
        if(!pointCameraRelativeHosted)return;
        var p=pointProfile!.RootElement;var pivot=p.GetProperty("OrbitPivot");
        Check(p.GetProperty("OrbitAtStep").GetInt32()==24&&p.GetProperty("OrbitHeldAtStep").GetInt32()==26&&p.GetProperty("OrbitWheelAtStep").GetInt32()==33
            &&pivot.GetArrayLength()==3&&p.GetProperty("OrbitRelative").GetArrayLength()==2,"Explicit supplied orbit schedule");
        camera.Projection=Camera3D.ProjectionType.Perspective;orbitCamera=camera;
        cameraOrbitHost=new TerrainCameraOrbitHost{Name="OwnedCameraOrbitHost"};AddChild(cameraOrbitHost);
        cameraOrbitHost.Configure(camera,new Vector3(pivot[0].GetSingle(),pivot[1].GetSingle(),pivot[2].GetSingle()),
            p.GetProperty("OrbitYaw").GetDouble(),p.GetProperty("OrbitPitch").GetDouble(),p.GetProperty("OrbitDistance").GetDouble(),
            new TerrainCameraOrbitPolicy(p.GetProperty("OrbitMinimumPitch").GetDouble(),p.GetProperty("OrbitMaximumPitch").GetDouble(),
                p.GetProperty("OrbitMinimumDistance").GetDouble(),p.GetProperty("OrbitMaximumDistance").GetDouble(),
                p.GetProperty("OrbitRadiansPerPixel").GetDouble(),p.GetProperty("OrbitWheelStep").GetDouble()));
        PrintCameraOrbitState(-1);
    }
    private void PrintCameraOrbitState(int step)
    {
        var h=cameraOrbitHost!;var c=orbitCamera!;var position=c.GlobalPosition;var forward=-c.GlobalBasis.Z;
        GD.Print("camera_orbit_state="+JsonSerializer.Serialize(new{AtStep=step,h.Yaw,h.Pitch,h.Distance,
            EnginePosition=new[]{position.X,position.Y,position.Z},EngineForward=new[]{forward.X,forward.Y,forward.Z},
            Perspective=c.Projection==Camera3D.ProjectionType.Perspective,ActualBorrowedCamera=ReferenceEquals(h.Camera,c),h.AcceptedCount,h.HandledCount}));
    }
    private NativeVector2 MapCameraRelativeInput(NativeVector2 keys)
    {
        if(!pointCameraRelativeHosted)return keys;
        Check(GodotObject.IsInstanceValid(orbitCamera)&&orbitCamera!.IsInsideTree()&&GetViewport().GetCamera3D()==orbitCamera,"Live exact active camera-relative source");
        var engine=-orbitCamera!.GlobalBasis.Z;
        return TerrainCameraRelativeMovement.Project(keys,new NativeVector3(engine.X,engine.Y,-engine.Z));
    }
    private void PublishCameraRelativeManual(NativeVector2 keys,long clock)
    {
        var direction=MapCameraRelativeInput(keys);pointDriver!.SetManualInput(direction,clock);
        if(!pointCameraRelativeHosted)return;
        var engine=-orbitCamera!.GlobalBasis.Z;
        GD.Print("camera_relative_input="+JsonSerializer.Serialize(new{AtStep=pointIndex,RawKeys=new[]{keys.X,keys.Y},
            EngineForward=new[]{engine.X,engine.Y,engine.Z},NativeForward=new[]{engine.X,engine.Y,-engine.Z},
            NativeDirection=new[]{direction.X,direction.Y},Clock=clock,EngineZReflectedOnce=true,OriginalCameraParity=false}));
    }
    private void AdvanceCameraOrbitSchedule()
    {
        if(!pointCameraRelativeHosted)return;
        if(pointIndex is not (24 or 26 or 33))return;
        int before=cameraOrbitHost!.HandledCount;
        if(pointIndex is 24 or 26){var r=pointProfile!.RootElement.GetProperty(pointIndex==24?"OrbitRelative":"OrbitHeldRelative");
            using var input=new InputEventMouseMotion{Relative=new Vector2(r[0].GetSingle(),r[1].GetSingle()),ButtonMask=MouseButtonMask.Right,Position=GetViewport().GetVisibleRect().Size/2};
            GetViewport().PushInput(input,true);
        }else{using var input=new InputEventMouseButton{ButtonIndex=MouseButton.WheelUp,Pressed=true,Position=GetViewport().GetVisibleRect().Size/2};GetViewport().PushInput(input,true);}
        Check(cameraOrbitHost.LastFailure is null&&cameraOrbitHost.HandledCount==before+1&&cameraOrbitHost.LastDiagnostic?.FromGodotEvent==true,"Actual orbit viewport input completes");
        PrintCameraOrbitState(pointIndex);
    }
    private void RefreshCameraRelativeManual()
    {
        if(!pointCameraRelativeHosted||manualEventHost is null||!manualEventHost.IsEnabled||manualEventHost.CurrentInput==NativeVector2.Zero)return;
        var direction=MapCameraRelativeInput(manualEventHost.CurrentInput);
        if(direction==pointDriver!.ManualInput||pointCameraFollowHosted&&NativeVector2.DistanceSquared(direction,pointDriver.ManualInput)<=1e-12f)return;
        var before=pointDriver.State;pointDriver.SetManualInput(direction,localClock);
        Check(ReferenceEquals(before.DriverResult,pointDriver.State.DriverResult)&&ReferenceEquals(before.Target,pointDriver.Target),"Camera turn remaps held keys without driver or target effects");
        GD.Print("camera_relative_refresh="+JsonSerializer.Serialize(new{AtStep=pointIndex,RawKeys=new[]{manualEventHost.CurrentInput.X,manualEventHost.CurrentInput.Y},
            NativeDirection=new[]{direction.X,direction.Y},Clock=localClock,DriverStatePreserved=true,KeyCountsPreserved=true,
            manualEventHost.ClockReadCount,manualEventHost.AcceptedCount,manualEventHost.HandledCount}));
    }
    private void CloseCameraOrbitHost()
    {
        var h=cameraOrbitHost;cameraOrbitHost=null;if(h is null)return;
        var camera=orbitCamera!;var before=camera.GlobalTransform;h.Close();h.Close();
        Check(h.IsClosed&&GodotObject.IsInstanceValid(camera)&&camera.GlobalTransform==before,"Orbit close preserves borrowed camera and external pose");
        GD.Print("camera_orbit_cleanup="+JsonSerializer.Serialize(new{Closed=true,h.IsFaulted,h.AcceptedCount,h.HandledCount,BorrowedCameraPreserved=true,ExternalPosePreserved=true}));
        if(GodotObject.IsInstanceValid(h)){if(h.GetParent() is Node parent)parent.RemoveChild(h);h.Free();}orbitCamera=null;
    }
}
