using System;
using System.Threading;
using System.IO;
using Godot;
using Stardust.Core.Geometry;

public sealed record TerrainCameraPickReceipt(TerrainCameraPicker Owner,Camera3D Camera,ulong CameraInstanceId,
    TerrainModernPatchSetRayQuery Query,Vector2 Pixel,Vector2 ViewportSize,Vector3 EngineOrigin,Vector3 EngineDirection,
    TerrainModernPatchSetRay Ray,TerrainModernPatchSetRayHit? Hit,ulong Revision,int Attempt);

/// <summary>Main-thread modern camera picker. Borrowed camera/parent/query; one owned lazy diagnostic marker.</summary>
public sealed class TerrainCameraPicker : IDisposable
{
    private readonly int ownerThread=System.Environment.CurrentManagedThreadId;
    private readonly Camera3D camera;
    private readonly Node3D parent;
    private readonly int maximumAttempts;
    private MeshInstance3D? marker;
    private SphereMesh? mesh;
    private StandardMaterial3D? material;
    private bool disposed,busy,reentered;
    public TerrainModernPatchSetRayQuery Query {get;}
    public TerrainCameraPickReceipt? Current {get;private set;}
    public TerrainModernPatchSetRayHit? SelectedHit=>Current?.Hit;
    public int Attempts {get;private set;}
    public ulong Revision {get;private set;}
    public bool IsDisposed=>disposed;
    public MeshInstance3D? Marker=>marker;
    public int MarkerBuilds {get;private set;}
    public int OwnedResourceCount=>(GodotObject.IsInstanceValid(marker)?1:0)+(GodotObject.IsInstanceValid(mesh)?1:0)+(GodotObject.IsInstanceValid(material)?1:0);
    public bool OriginalPickingParity=>false;
    public TerrainCameraPicker(Camera3D camera,Node3D parent,TerrainModernPatchSetRayQuery query,int maximumAttempts=256,ulong revisionSeed=0)
    {
        Owner();ArgumentNullException.ThrowIfNull(camera);ArgumentNullException.ThrowIfNull(parent);ArgumentNullException.ThrowIfNull(query);
        if(maximumAttempts is <1 or >256)throw new ArgumentOutOfRangeException(nameof(maximumAttempts));
        this.camera=camera;this.parent=parent;Query=query;this.maximumAttempts=maximumAttempts;Revision=revisionSeed;Live();
    }
    private void Owner(){if(System.Environment.CurrentManagedThreadId!=ownerThread||OS.GetThreadCallerId()!=OS.GetMainThreadId())throw new InvalidOperationException("Picker owner/Godot main thread required.");}
    private static bool Finite(Vector3 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z);
    private static bool Finite(Vector2 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y);
    private bool LiveObjects()=>GodotObject.IsInstanceValid(camera)&&GodotObject.IsInstanceValid(parent)&&camera.IsInsideTree()&&parent.IsInsideTree()&&!camera.IsQueuedForDeletion()&&!parent.IsQueuedForDeletion();
    private void Live()
    {
        if(!LiveObjects()||camera.GetViewport()!=parent.GetViewport()||camera.GetViewport().GetCamera3D()!=camera||camera.Projection is not (Camera3D.ProjectionType.Perspective or Camera3D.ProjectionType.Orthogonal))throw new InvalidOperationException("Exact live active camera and same-viewport live parent required.");
        if(marker is not null&&(!GodotObject.IsInstanceValid(marker)||marker.GetParent()!=parent||marker.IsQueuedForDeletion()||!GodotObject.IsInstanceValid(mesh)||!GodotObject.IsInstanceValid(material)||marker.Mesh!=mesh||marker.MaterialOverride!=material))throw new InvalidOperationException("Owned marker was externally invalidated.");
    }
    public bool IsCurrent(TerrainCameraPickReceipt? receipt)
    {Owner();return receipt is not null&&!disposed&&LiveObjects()&&camera.GetViewport()==parent.GetViewport()&&camera.GetViewport().GetCamera3D()==camera&&camera.Projection is (Camera3D.ProjectionType.Perspective or Camera3D.ProjectionType.Orthogonal)&&(marker is null||(GodotObject.IsInstanceValid(marker)&&marker.GetParent()==parent&&!marker.IsQueuedForDeletion()&&GodotObject.IsInstanceValid(mesh)&&GodotObject.IsInstanceValid(material)&&marker.Mesh==mesh&&marker.MaterialOverride==material))&&ReferenceEquals(receipt,Current)&&ReferenceEquals(receipt.Owner,this)&&ReferenceEquals(receipt.Camera,camera)&&ReferenceEquals(receipt.Query,Query);}
    private void Enter()
    {Owner();if(busy){reentered=true;throw new InvalidOperationException("Reentrant picker mutation rejected.");}busy=true;reentered=false;}
    private void Guard(){Live();if(reentered)throw new InvalidOperationException("Caught picker reentry invalidates outer publication.");}
    /// <summary>Non-mutating admission; does not charge, query, allocate or publish.</summary>
    public void ValidateAdmission(Vector2 pixel,double maximumT,CancellationToken cancellation=default)
    {Enter();try{ValidateAdmissionCore(pixel,maximumT,cancellation);}finally{busy=false;}}
    private void ValidateAdmissionCore(Vector2 pixel,double maximumT,CancellationToken cancellation)
    {
        Owner();if(disposed)throw new ObjectDisposedException(nameof(TerrainCameraPicker));Live();cancellation.ThrowIfCancellationRequested();
        Vector2 size=camera.GetViewport().GetVisibleRect().Size;
        if(!Finite(size)||size.X<=0||size.Y<=0||!Finite(pixel)||pixel.X<0||pixel.Y<0||pixel.X>=size.X||pixel.Y>=size.Y)throw new ArgumentOutOfRangeException(nameof(pixel));
        if(!double.IsFinite(maximumT)||maximumT<0||maximumT>1e9)throw new ArgumentOutOfRangeException(nameof(maximumT));
        if(Attempts>=maximumAttempts)throw new InvalidOperationException("Picker attempt cap before query/output allocation.");
        _=checked(Revision+1);cancellation.ThrowIfCancellationRequested();
    }
    public TerrainCameraPickReceipt Pick(Vector2 pixel,double maximumT,CancellationToken cancellation=default)
    {Enter();try{return PickCore(pixel,maximumT,cancellation);}finally{busy=false;}}
    private TerrainCameraPickReceipt PickCore(Vector2 pixel,double maximumT,CancellationToken cancellation)
    {
        ValidateAdmissionCore(pixel,maximumT,cancellation);
        Vector2 size=camera.GetViewport().GetVisibleRect().Size;
        ulong next=checked(Revision+1);var origin=camera.ProjectRayOrigin(pixel);var direction=camera.ProjectRayNormal(pixel);
        if(!Finite(origin)||!Finite(direction)||direction==Vector3.Zero)throw new InvalidDataException("Finite actual camera ray required.");
        var ray=new TerrainModernPatchSetRay(new(origin.X,origin.Y,-(double)origin.Z),new(direction.X,direction.Y,-(double)direction.Z),0,maximumT);
        cancellation.ThrowIfCancellationRequested();Attempts=checked(Attempts+1);
        var hit=Query.Query(ray,cancellation);cancellation.ThrowIfCancellationRequested();Guard();
        var receipt=new TerrainCameraPickReceipt(this,camera,camera.GetInstanceId(),Query,pixel,size,origin,direction,ray,hit,next,Attempts);
        MeshInstance3D? stagedNode=null;SphereMesh? stagedMesh=null;StandardMaterial3D? stagedMaterial=null;
        try
        {
            if(hit is not null&&marker is null)
            {
                stagedMesh=new SphereMesh{Radius=.08f,Height=.16f};stagedMaterial=new StandardMaterial3D{ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,AlbedoColor=Colors.Magenta};
                stagedNode=new MeshInstance3D{Name="OwnedTerrainPickMarker",Mesh=stagedMesh,MaterialOverride=stagedMaterial,TopLevel=true};
            }
            cancellation.ThrowIfCancellationRequested();Guard();
            if(stagedNode is not null)
            {
                parent.AddChild(stagedNode);cancellation.ThrowIfCancellationRequested();Guard();
                if(!GodotObject.IsInstanceValid(stagedNode)||stagedNode.GetParent()!=parent||stagedNode.IsQueuedForDeletion())throw new InvalidOperationException("Staged marker invalidated by callback.");
            }
            var target=stagedNode??marker;
            if(target is not null)
            {
                if(hit is { } h){target.GlobalPosition=new((float)h.Position.X,(float)h.Position.Y,-(float)h.Position.Z);Guard();target.Visible=true;}
                else target.Visible=false;
            }
            // Property setters may signal. Current/revision remain unchanged on detected callback reentry;
            // a reused marker's mutation prefix cannot be claimed as rolled back.
            Guard();
            if(stagedNode is not null)
            {
                if(!GodotObject.IsInstanceValid(stagedNode)||stagedNode.GetParent()!=parent||stagedNode.IsQueuedForDeletion()
                    ||!GodotObject.IsInstanceValid(stagedMesh)||!GodotObject.IsInstanceValid(stagedMaterial)||stagedNode.Mesh!=stagedMesh||stagedNode.MaterialOverride!=stagedMaterial)
                    throw new InvalidOperationException("Staged resources invalidated before transfer.");
                marker=stagedNode;mesh=stagedMesh;material=stagedMaterial;stagedNode=null;stagedMesh=null;stagedMaterial=null;MarkerBuilds++;
            }
            Revision=next;Current=receipt;return receipt;
        }
        finally{Release(stagedNode,stagedMesh,stagedMaterial);}
    }
    public bool HandleInput(InputEvent input,double maximumT,out TerrainCameraPickReceipt? receipt,CancellationToken cancellation=default)
    {
        Owner();if(disposed)throw new ObjectDisposedException(nameof(TerrainCameraPicker));ArgumentNullException.ThrowIfNull(input);receipt=null;
        if(input is not InputEventMouseButton button||button.ButtonIndex!=MouseButton.Left||!button.Pressed)return false;
        receipt=Pick(button.Position,maximumT,cancellation);return true;
    }
    private static void Release(MeshInstance3D? node,SphereMesh? mesh,StandardMaterial3D? material)
    {
        if(GodotObject.IsInstanceValid(node)){node!.Mesh=null;node.MaterialOverride=null;if(node.GetParent() is Node p)p.RemoveChild(node);node.Free();}
        if(GodotObject.IsInstanceValid(mesh))mesh!.Dispose();if(GodotObject.IsInstanceValid(material))material!.Dispose();
    }
    public void Dispose()
    {
        Enter();try
        {
            if(disposed)return;disposed=true;Current=null;
            try{Release(marker,mesh,material);}finally{marker=null;mesh=null;material=null;}
            if(reentered)throw new InvalidOperationException("Picker closed after rejected disposal reentry.");
        }
        finally{busy=false;}
    }
}
