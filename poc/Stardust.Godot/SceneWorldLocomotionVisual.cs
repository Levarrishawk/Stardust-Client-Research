using System;
using System.IO;
using System.Linq;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Session;

/// <summary>Explicit idle/walk/run preview clock over accepted native motion and Height.</summary>
public sealed class SceneWorldLocomotionVisual : ISceneWorldObjectPresentation
{
    private SceneWorldCoordinator? world;
    private ISceneWorldObjectMotionSource? motionSource;
    private readonly int owner=System.Environment.CurrentManagedThreadId;
    private readonly float runThreshold;
    private AnimationPreviewClock clock;
    private bool disposed,busy,focused=true,playing=true;
    private ulong? motionRevision;
    public SceneWorldObjectMotionVisual Inner {get;}
    public ScenePreparedObjectPublication Publication=>Inner.Publication;
    public string SelectedClip {get;private set;}="idle";
    public double Frame=>clock.Frame;
    public bool IsRetired=>disposed;
    public SceneWorldLocomotionVisual(SceneWorldCoordinator world,SceneWorldObjectMotionVisual inner,ISceneWorldObjectMotionSource motionSource,float runThreshold=4)
    {
        Owner();ArgumentNullException.ThrowIfNull(world);ArgumentNullException.ThrowIfNull(inner);ArgumentNullException.ThrowIfNull(motionSource);
        if(!float.IsFinite(runThreshold) || runThreshold<=0 || runThreshold>10)throw new ArgumentOutOfRangeException(nameof(runThreshold));
        var snapshot=world.Snapshots().SingleOrDefault(s=>s.Object.Handle==inner.Publication.Ticket.Handle);
        if(inner.IsRetired || snapshot is null || !SceneWorldObjectVisual.Matches(world,inner.Publication,snapshot) || inner.Inner.CreatureVisual is null ||
            inner.Inner.CreatureVisual.IsRetired || !GodotObject.IsInstanceValid(inner.Inner.CreatureVisual.Root) || !inner.Inner.CreatureVisual.Root.IsInsideTree())throw new ArgumentException("Current creature inner required.");
        var resource=inner.Publication.Resources.Creature!.Creature;
        if(resource.PreparedAnimation is null || resource.AdditionalAnimations is null || !resource.AdditionalAnimations.ContainsKey("walk") || !resource.AdditionalAnimations.ContainsKey("run"))throw new InvalidDataException("Explicit loaded idle/walk/run required.");
        clock=Clock(resource.PreparedAnimation);_=Clock(resource.AdditionalAnimations["walk"]);_=Clock(resource.AdditionalAnimations["run"]);
        var initial=motionSource.Find(snapshot);
        if(initial is not null && initial.Incarnation==snapshot.Object.Incarnation && (!float.IsFinite(initial.Speed) || initial.Speed<0))throw new ArgumentException("Invalid initial speed.");
        if(!SceneWorldObjectVisual.Matches(world,inner.Publication,snapshot) || inner.IsRetired)throw new ArgumentException("Inner changed during initial source checks.");
        Inner=inner;this.world=world;this.motionSource=motionSource;this.runThreshold=runThreshold;
    }
    private static AnimationPreviewClock Clock(PreparedSkeletalAnimation clip){var result=new AnimationPreviewClock(clip.FrameRate,clip.EndFrame,clip.ProxyTimeScale);result.SetPlaying(true);return result;}
    private void Owner(){if(System.Environment.CurrentManagedThreadId!=owner || OS.GetThreadCallerId()!=OS.GetMainThreadId())throw new InvalidOperationException("Locomotion owner/main thread required.");}
    private void Enter(){Owner();if(busy)throw new InvalidOperationException("Locomotion reentry.");busy=true;}
    private bool Present(double seconds)
    {
        var snapshot=world!.Snapshots().SingleOrDefault(s=>s.Object.Handle==Publication.Ticket.Handle);
        if(snapshot is null || !SceneWorldObjectVisual.Matches(world,Publication,snapshot)){Retire();return false;}
        var motion=motionSource!.Find(snapshot);float speed=0;
        if(motion is not null && motion.Incarnation==snapshot.Object.Incarnation && (motionRevision is null || motion.Revision>=motionRevision))
        {if(!float.IsFinite(motion.Speed) || motion.Speed<0)throw new ArgumentException("Finite nonnegative speed required.");speed=motion.Speed;motionRevision=motion.Revision;}
        string key=speed==0?"idle":speed>=runThreshold?"run":"walk";
        var resource=Publication.Resources.Creature!.Creature;
        if(key!=SelectedClip){SelectedClip=key;clock=Clock(key=="idle"?resource.PreparedAnimation!:resource.AdditionalAnimations![key]);}
        clock.SetFocused(focused);clock.SetPlaying(playing);clock.Advance(Math.Min(seconds,.25));
        if(!SceneWorldObjectVisual.Matches(world,Publication,snapshot)){Retire();return false;}
        var visual=Inner.Inner.CreatureVisual!;
        // Diagnostic instant selection: finish the accepted 0.15s pose blend without advancing clip phase.
        bool result=SelectedClip=="idle"?visual.SeekIdle(clock.Frame):visual.SetMovement(SelectedClip,clock.Frame,.25);
        if(!result){Retire();return false;}return true;
    }
    public bool Tick(double seconds)
    {
        Enter();try{if(disposed)return false;if(!double.IsFinite(seconds)||seconds<0||seconds>60)throw new ArgumentOutOfRangeException(nameof(seconds));if(!Inner.Tick(0)){Retire();return false;}return Present(seconds);}
        catch{Retire();throw;}finally{busy=false;}
    }
    public bool Apply(SceneWorldObjectSnapshot snapshot)
    {Enter();try{if(disposed)return false;if(!Inner.Apply(snapshot)){if(Inner.IsRetired)Retire();return false;}return Present(0);}catch{Retire();throw;}finally{busy=false;}}
    public void SetFocused(bool value){Enter();try{ObjectDisposedException.ThrowIf(disposed,this);focused=value;clock.SetFocused(value);}finally{busy=false;}}
    public void SetPlaying(bool value){Enter();try{ObjectDisposedException.ThrowIf(disposed,this);playing=value;clock.SetPlaying(value);}finally{busy=false;}}
    public void Reset(){Enter();try{ObjectDisposedException.ThrowIf(disposed,this);clock.Seek(0);if(!Inner.Tick(0) || !Present(0))Retire();}catch{Retire();throw;}finally{busy=false;}}
    private void Retire(){if(disposed)return;disposed=true;try{Inner.Dispose();}finally{world=null;motionSource=null;}}
    public void Dispose(){Enter();try{Retire();}finally{busy=false;}}
}
