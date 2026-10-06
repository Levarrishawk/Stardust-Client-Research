using System;
using System.Text.Json;
using Godot;

public partial class ScenePumpedWorldPreview
{
    private TerrainCameraPicker? pointPicker;
    private Camera3D? pointCamera;
    private Node3D? pointMarkerParent;
    private TerrainCameraPickReceipt? pointPick;
    private TerrainPointTargetInputAdapter? pointInputAdapter;
    private TerrainPointTargetInputResult? pointInputResult;
    private void SelectAdaptedPointTarget(float x,float z)
    {
        var ground=suppliedTerrainSetQuery!.Query(x,z)??throw new InvalidOperationException("Supplied target outside terrain");
        Vector2 pixel=pointCamera!.UnprojectPosition(new Vector3(x,(float)ground.Hit.Height,-z));
        var prior=pointInputResult;
        using var button=new InputEventMouseButton{Position=pixel,ButtonIndex=MouseButton.Left,Pressed=true};
        bool handled;
        if(pointViewportHosted)handled=PushPointViewportInput(button);
        else handled=pointInputAdapter!.HandleInput(button,localClock,1000,out pointInputResult);
        Check(handled&&pointInputResult?.Target is not null
            &&pointInputAdapter!.IsCurrent(pointInputResult),"Actual admitted input pick and target");
        pointPick=pointInputResult!.Pick;
        Check(pointPick?.Hit is not null&&ReferenceEquals(pointInputResult.Target!.Hit,pointPick.Hit)
            &&ReferenceEquals(pointInputResult.Target,pointDriver!.Target),"Exact actual pick and controller receipt identities");
        PrintPointCameraPick();
        var r=pointInputResult!;
        GD.Print("point_input_target="+JsonSerializer.Serialize(new{AtStep=pointIndex,r.Attempt,r.TimestampMicroseconds,
            r.PickerAttemptsBefore,r.PickerAttemptsAfter,Target=new[]{r.Target!.X,r.Target.Z},TargetRevision=r.Target.Revision,
            ActualPickTarget=true,Current=true,PriorCurrent=prior is not null&&pointInputAdapter!.IsCurrent(prior),
            Paused=pointDriver!.IsPaused,Failure=r.FailureType,OriginalMovementParity=false}));
    }
    private void ClosePointInputAdapter()
    {
        ClosePointInputHost();
        if(pointInputAdapter is null)return;
        var owned=pointInputAdapter;pointInputAdapter=null;owned.Dispose();owned.Dispose();
        Check(owned.IsDisposed,"Owned input adapter drains references");
        GD.Print("point_input_cleanup="+JsonSerializer.Serialize(new{Disposed=true,owned.Attempts,OwnsBorrowers=false,
            BorrowedPickerPreserved=pointPicker is {IsDisposed:false},
            BorrowedControllerPreserved=pointDriver is not null&&pointDriver.State.Mode!=Stardust.Core.Session.SceneWorldTerrainPointTargetMode.Disposed}));
        pointInputResult=null;
    }
    private void PreparePointPicker(Camera3D camera)
    {
        if(!pointRequested)return;
        Check(suppliedTerrainRayQuery is not null,"Point picking borrows retained actual ray query");
        pointCamera=camera;
        pointMarkerParent=new Node3D{Name="OwnedPointMarkerParent"};AddChild(pointMarkerParent);
        pointPicker=new(camera,pointMarkerParent,suppliedTerrainRayQuery!,pointConfigured?OfflineMaximumClicks:256);
    }
    private void SelectPointTarget(float x,float z)
    {
        var ground=suppliedTerrainSetQuery!.Query(x,z)??throw new InvalidOperationException("Supplied point outside terrain");
        Vector2 pixel=pointCamera!.UnprojectPosition(new Vector3(x,(float)ground.Hit.Height,-z));
        using var button=new InputEventMouseButton{Position=pixel,ButtonIndex=MouseButton.Left,Pressed=true};
        Check(pointPicker!.HandleInput(button,1000,out pointPick)&&pointPick?.Hit is not null
            &&pointPicker.IsCurrent(pointPick)&&ReferenceEquals(pointPick.Query,suppliedTerrainRayQuery),"Actual left pressed camera terrain pick");
        PrintPointCameraPick();
    }
    private void PrintPointCameraPick()
    {
        var r=pointPick!;
        var picker=pointPicker!;
        GD.Print("point_camera_pick="+JsonSerializer.Serialize(new{Pixel=new[]{r.Pixel.X,r.Pixel.Y},Viewport=new[]{r.ViewportSize.X,r.ViewportSize.Y},
            EngineOrigin=new[]{r.EngineOrigin.X,r.EngineOrigin.Y,r.EngineOrigin.Z},EngineDirection=new[]{r.EngineDirection.X,r.EngineDirection.Y,r.EngineDirection.Z},
            Native=RayObservation(r.Ray,r.Hit),r.Revision,r.Attempt,picker.MarkerBuilds,Owned=picker.OwnedResourceCount,
            ActualInputEvent=true,ActualReferences=ReferenceEquals(r.Query,suppliedTerrainRayQuery),OriginalPickingParity=false}));
    }
    private void ClosePointPicker()
    {
        CloseCameraOrbitHost();
        if(pointPicker is null){ClosePointMarkerParent();return;}
        var owned=pointPicker;pointPicker=null;
        owned.Dispose();owned.Dispose();
        Check(owned.OwnedResourceCount==0&&owned.IsDisposed,"Point marker owner drained");
        GD.Print("point_camera_cleanup="+JsonSerializer.Serialize(new{Disposed=true,Owned=0,
            BorrowedCameraPreserved=GodotObject.IsInstanceValid(pointCamera),BorrowedTerrainPreserved=suppliedTerrainRayQuery is not null}));
        pointPick=null;pointCamera=null;
        ClosePointMarkerParent();
    }
    private void ClosePointMarkerParent(){if(GodotObject.IsInstanceValid(pointMarkerParent)){RemoveChild(pointMarkerParent!);pointMarkerParent!.Free();}pointMarkerParent=null;}
}
