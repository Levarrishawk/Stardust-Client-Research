using System;
using System.IO;
using System.Threading;
using Godot;

public sealed record TerrainCameraOrbitPolicy(double MinimumPitchRadians=.05,double MaximumPitchRadians=1.45,
    double MinimumDistance=.25,double MaximumDistance=1000,double RadiansPerPixel=.005,double WheelDistanceStep=1);
public sealed record TerrainCameraOrbitDiagnostic(string Kind,double Yaw,double Pitch,double Distance,Vector3 RequestedPosition,
    bool PositionMayHaveChanged,bool PositionReturned,bool LookAtMayHaveRun,bool LookAtReturned,bool StateCommitted,
    string? FailureType,bool FromGodotEvent);

/// <summary>Bounded modern orbit around supplied engine-world pivot; borrows camera without capture or projection changes.</summary>
public partial class TerrainCameraOrbitHost : Node
{
    private readonly int ownerThread=System.Environment.CurrentManagedThreadId;
    private Camera3D? camera;
    private Node? hostParent,cameraParent;
    private Vector3 pivot;
    private TerrainCameraOrbitPolicy? policy;
    private TerrainCameraOrbitDiagnostic? diagnostic;
    private bool configured,enabled,closed,faulted,busy,reentered;
    private double yaw,pitch,distance;
    private int attempts,accepted,handled,revision;
    private void EnsureOwner(){if(System.Environment.CurrentManagedThreadId!=ownerThread||OS.GetThreadCallerId()!=OS.GetMainThreadId())throw new InvalidOperationException("Orbit owner/Godot main thread required.");}
    private void Enter(){EnsureOwner();if(busy){reentered=true;throw new InvalidOperationException("Orbit reentry.");}busy=true;reentered=false;}
    private static bool Finite(Vector3 value)=>float.IsFinite(value.X)&&float.IsFinite(value.Y)&&float.IsFinite(value.Z);
    private static bool LiveNode(Node? value)=>GodotObject.IsInstanceValid(value)&&value!.IsInsideTree()&&!value.IsQueuedForDeletion();
    private void Live()
    {
        if(!LiveNode(this)||!LiveNode(hostParent)||GetParent()!=hostParent||!LiveNode(camera)||!LiveNode(cameraParent)||camera!.GetParent()!=cameraParent||
            GetViewport()!=hostParent!.GetViewport()||GetViewport()!=camera.GetViewport()||GetViewport()!=cameraParent!.GetViewport()||
            GetViewport().GetCamera3D()!=camera||camera.Projection!=Camera3D.ProjectionType.Perspective)
            throw new InvalidOperationException("Exact live parent/camera parent, same viewport, active perspective camera required.");
    }
    private void Guard(){if(reentered||closed||faulted)throw new InvalidOperationException("Callback invalidated orbit mutation.");Live();}
    private static void Validate(TerrainCameraOrbitPolicy p)
    {
        if(!double.IsFinite(p.MinimumPitchRadians)||!double.IsFinite(p.MaximumPitchRadians)||p.MinimumPitchRadians<=0||p.MaximumPitchRadians>=Math.PI/2||p.MinimumPitchRadians>=p.MaximumPitchRadians||
            !double.IsFinite(p.MinimumDistance)||!double.IsFinite(p.MaximumDistance)||p.MinimumDistance<.001||p.MaximumDistance>1e6||p.MinimumDistance>p.MaximumDistance||
            !double.IsFinite(p.RadiansPerPixel)||p.RadiansPerPixel<1e-9||p.RadiansPerPixel>1||!double.IsFinite(p.WheelDistanceStep)||p.WheelDistanceStep<=0||p.WheelDistanceStep>1e6)
            throw new ArgumentOutOfRangeException(nameof(p));
    }
    private static double Normalize(double value)=>Math.IEEERemainder(value,2*Math.PI);
    private Vector3 Position(double y,double p,double d,Vector3? candidatePivot=null)
    {
        var target=candidatePivot??pivot;
        var result=new Vector3((float)(target.X+d*Math.Sin(y)*Math.Cos(p)),(float)(target.Y+d*Math.Sin(p)),(float)(target.Z+d*Math.Cos(y)*Math.Cos(p)));
        var delta=target-result;
        if(!Finite(result)||Math.Abs(result.X)>2e6||Math.Abs(result.Y)>2e6||Math.Abs(result.Z)>2e6||delta.LengthSquared()==0||delta.Normalized().Cross(Vector3.Up).LengthSquared()<1e-8f)
            throw new InvalidDataException("Finite distinct non-pole binary32 camera pose required before effects.");
        return result;
    }
    public Camera3D? Camera {get{EnsureOwner();return camera;}}
    public Vector3 Pivot {get{EnsureOwner();return pivot;}}
    public double Yaw {get{EnsureOwner();return yaw;}}
    public double Pitch {get{EnsureOwner();return pitch;}}
    public double Distance {get{EnsureOwner();return distance;}}
    public int PoseAttemptCount {get{EnsureOwner();return attempts;}}
    public int Revision {get{EnsureOwner();return revision;}}
    public int AcceptedCount {get{EnsureOwner();return accepted;}}
    public int HandledCount {get{EnsureOwner();return handled;}}
    public bool IsConfigured {get{EnsureOwner();return configured;}}
    public bool IsEnabled {get{EnsureOwner();return enabled&&!closed&&!faulted;}}
    public bool IsClosed {get{EnsureOwner();return closed;}}
    public bool IsFaulted {get{EnsureOwner();return faulted;}}
    public TerrainCameraOrbitDiagnostic? LastDiagnostic {get{EnsureOwner();return diagnostic;}}
    public string? LastFailure {get{EnsureOwner();return diagnostic?.FailureType;}}
    public override void _Ready(){EnsureOwner();SetProcessUnhandledInput(configured&&IsEnabled);}
    private void Apply(double y,double p,double d,string kind,CancellationToken cancellation,bool input,Vector3? candidatePivot=null)
    {
        Vector3 target=candidatePivot??pivot;Vector3 position=Position(y,p,d,target);bool positionStarted=false,positionReturned=false,lookStarted=false,lookReturned=false;
        try
        {
            Guard();cancellation.ThrowIfCancellationRequested();int nextRevision=checked(revision+1),nextAccepted=input?checked(accepted+1):accepted;
            attempts=checked(attempts+1);positionStarted=true;camera!.GlobalPosition=position;positionReturned=true;Guard();cancellation.ThrowIfCancellationRequested();
            lookStarted=true;camera.LookAt(target,Vector3.Up);lookReturned=true;Guard();cancellation.ThrowIfCancellationRequested();
            pivot=target;yaw=y;pitch=p;distance=d;revision=nextRevision;accepted=nextAccepted;diagnostic=new(kind,y,p,d,position,true,true,true,true,true,null,false);
        }
        catch(Exception failure)
        {
            faulted=true;diagnostic=new(kind,y,p,d,position,positionStarted,positionReturned,lookStarted,lookReturned,false,failure.GetType().FullName,false);
            if(GodotObject.IsInstanceValid(this))SetProcessUnhandledInput(false);throw;
        }
    }
    public void Configure(Camera3D camera,Vector3 pivot,double yawRadians,double pitchRadians,double distance,TerrainCameraOrbitPolicy? policy=null,CancellationToken cancellation=default)
    {
        Enter();try
        {
            if(configured||closed||faulted)throw new InvalidOperationException("Configuration accepted once before closure.");
            ArgumentNullException.ThrowIfNull(camera);var supplied=policy??new();Validate(supplied);
            if(!Finite(pivot)||Math.Abs(pivot.X)>1e6||Math.Abs(pivot.Y)>1e6||Math.Abs(pivot.Z)>1e6||!double.IsFinite(yawRadians)||Math.Abs(yawRadians)>1e12||!double.IsFinite(pitchRadians)||!double.IsFinite(distance))throw new ArgumentOutOfRangeException(nameof(pivot));
            cancellation.ThrowIfCancellationRequested();this.camera=camera;this.pivot=pivot;this.policy=supplied;hostParent=GetParent();cameraParent=camera.GetParent();
            double y=Normalize(yawRadians),p=Math.Clamp(pitchRadians,supplied.MinimumPitchRadians,supplied.MaximumPitchRadians),d=Math.Clamp(distance,supplied.MinimumDistance,supplied.MaximumDistance);
            Live();Apply(y,p,d,"Configure",cancellation,false);configured=true;enabled=true;SetProcessUnhandledInput(true);
        }catch{if(!configured&&!faulted){this.camera=null;hostParent=null;cameraParent=null;this.policy=null;}throw;}finally{busy=false;}
    }
    private void Open(){if(!configured||closed||faulted)throw new InvalidOperationException("Configured open healthy orbit required.");}
    public bool Dispatch(InputEvent input,CancellationToken cancellation=default)
    {
        Enter();try
        {
            Open();ArgumentNullException.ThrowIfNull(input);if(!enabled)return false;double y=yaw,p=pitch,d=distance;string kind;
            if(input is InputEventMouseMotion motion&&(motion.ButtonMask&MouseButtonMask.Right)!=0)
            {
                if(!float.IsFinite(motion.Relative.X)||!float.IsFinite(motion.Relative.Y)||Math.Abs(motion.Relative.X)>1e6||Math.Abs(motion.Relative.Y)>1e6)throw new ArgumentOutOfRangeException(nameof(motion.Relative));
                y=Normalize(y-motion.Relative.X*policy!.RadiansPerPixel);p=Math.Clamp(p+motion.Relative.Y*policy.RadiansPerPixel,policy.MinimumPitchRadians,policy.MaximumPitchRadians);kind="Motion";
            }
            else if(input is InputEventMouseButton button&&button.Pressed&&button.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
            {d=Math.Clamp(d+(button.ButtonIndex==MouseButton.WheelUp?-policy!.WheelDistanceStep:policy!.WheelDistanceStep),policy.MinimumDistance,policy.MaximumDistance);kind="Wheel";}
            else return false;
            if(y==yaw&&p==pitch&&d==distance)return false;
            Guard();Apply(y,p,d,kind,cancellation,true);return true;
        }finally{busy=false;}
    }
    public bool SetPivot(Vector3 pivot,CancellationToken cancellation=default)
    {
        Enter();try
        {
            Open();
            if(!Finite(pivot)||Math.Abs(pivot.X)>1e6||Math.Abs(pivot.Y)>1e6||Math.Abs(pivot.Z)>1e6)throw new ArgumentOutOfRangeException(nameof(pivot));
            Guard();cancellation.ThrowIfCancellationRequested();
            if(pivot==this.pivot)return false;
            if(Position(yaw,pitch,distance,pivot).IsEqualApprox(pivot))throw new InvalidDataException("Rounded camera and pivot must also differ under Godot LookAt approximate equality.");
            Apply(yaw,pitch,distance,"Pivot",cancellation,false,pivot);return true;
        }finally{busy=false;}
    }
    public bool SetEnabled(bool enabled)
    {Enter();try{Open();if(this.enabled==enabled)return false;Guard();this.enabled=enabled;SetProcessUnhandledInput(enabled);return true;}finally{busy=false;}}
    public override void _UnhandledInput(InputEvent input)
    {
        EnsureOwner();if(!configured||!IsEnabled)return;
        try{if(!Dispatch(input))return;GetViewport().SetInputAsHandled();handled=checked(handled+1);diagnostic=diagnostic! with{FromGodotEvent=true};}
        catch(Exception failure)
        {
            if(!faulted){faulted=true;diagnostic=new("Input",yaw,pitch,distance,default,false,false,false,false,false,failure.GetType().FullName,true);SetProcessUnhandledInput(false);}
            else if(diagnostic is not null)diagnostic=diagnostic with{FromGodotEvent=true};
        }
    }
    public override void _Notification(int what)
    {
        if(what!=(int)NotificationWMWindowFocusOut)return;EnsureOwner();if(!configured||!IsEnabled)return;
        try{SetEnabled(false);}catch(Exception failure){faulted=true;diagnostic=new("FocusLost",yaw,pitch,distance,default,false,false,false,false,false,failure.GetType().FullName,true);if(GodotObject.IsInstanceValid(this))SetProcessUnhandledInput(false);}
    }
    private void CloseCore(){closed=true;enabled=false;camera=null;hostParent=null;cameraParent=null;policy=null;if(GodotObject.IsInstanceValid(this))SetProcessUnhandledInput(false);}
    public void Close(){Enter();try{if(!closed)CloseCore();}finally{busy=false;}}
    public override void _ExitTree(){EnsureOwner();if(busy){reentered=true;CloseCore();return;}Close();}
}
