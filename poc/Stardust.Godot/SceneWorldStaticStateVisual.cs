using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
#if !STARDUST_PROJECTION_ONLY
using Godot;
#endif
using Stardust.Core.Protocol;
using Stardust.Core.Session;

/// <summary>Explicit supplied model; not decoded TANO state.</summary>
public sealed record SceneStaticPresentationProjection(SceneObjectIncarnation Incarnation,ulong Revision,CreatureView3NameState Names,byte Visibility)
{
    public bool Same(SceneStaticPresentationProjection other)=>Incarnation==other.Incarnation&&Revision==other.Revision&&Visibility==other.Visibility&&ReferenceEquals(Names,other.Names);
    public void Validate(SceneObjectIncarnation expected,SceneStaticPresentationProjection? previous)
    {
        if(Names is null||Incarnation!=expected||Revision==0)throw new InvalidDataException("Current supplied static projection required.");
        if(previous is not null&&(Revision<previous.Revision||Revision==previous.Revision&&!Same(previous)))throw new InvalidDataException("Stale or changed reused static revision.");
    }
}

#if !STARDUST_PROJECTION_ONLY

/// <summary>Optional supplied static state; owns inner/label, borrows world/base and pure callbacks.</summary>
public sealed class SceneWorldStaticStateVisual:ISceneWorldObjectPresentation,ISceneWorldObjectPresentationDrain
{
    private SceneWorldCoordinator? world;
    private SceneWorldObjectVisual? basis;
    private ISceneWorldObjectPresentation? inner;
    private Func<SceneWorldObjectSnapshot,SceneStaticPresentationProjection?>? source;
    private Func<CreatureView3NameState,string>? resolver;
    private SceneStaticPresentationProjection? observed;
    private Label3D? label;
    private string text=string.Empty;
    private readonly SceneObjectIncarnation incarnation;
    private readonly int owner=System.Environment.CurrentManagedThreadId,maximumTextUnits;
    private bool busy,disposed,withLabel;
    private Task finalDrain=Task.CompletedTask;
    [ThreadStatic]private static bool callback;
    [ThreadStatic]private static bool violation;
    public ScenePreparedObjectPublication Publication{get;}
    public bool IsRetired=>disposed;
    public string Text=>text;
    public ulong Revision=>observed?.Revision??0;
    public byte? Visibility=>observed?.Visibility;
    public ulong LabelId=>label?.GetInstanceId()??0;
    public bool LabelVisible=>label is not null&&GodotObject.IsInstanceValid(label)&&label.IsVisibleInTree();
    public Exception? LastFailure{get;private set;}
    public Task DrainCompletion=>disposed?finalDrain:(inner as ISceneWorldObjectPresentationDrain)?.DrainCompletion??Task.CompletedTask;
    public SceneWorldStaticStateVisual(SceneWorldCoordinator world,ISceneWorldObjectPresentation inner,SceneWorldObjectVisual basis,
        Func<SceneWorldObjectSnapshot,SceneStaticPresentationProjection?> source,Func<CreatureView3NameState,string> nameResolver,
        int maximumTextUnits=256,bool createLabel=true)
    {
        Owner();ArgumentNullException.ThrowIfNull(world);ArgumentNullException.ThrowIfNull(inner);ArgumentNullException.ThrowIfNull(basis);ArgumentNullException.ThrowIfNull(source);ArgumentNullException.ThrowIfNull(nameResolver);
        if(maximumTextUnits is <1 or >1024)throw new ArgumentOutOfRangeException(nameof(maximumTextUnits));
        if(!ReferenceEquals(inner.Publication,basis.Publication)||basis.StaticVisual is null||basis.CreatureVisual is not null)throw new ArgumentException("Exact static base/inner publication required.");
        this.world=world;this.basis=basis;this.source=source;resolver=nameResolver;this.maximumTextUnits=maximumTextUnits;withLabel=createLabel;Publication=basis.Publication;
        var snapshot=world.Snapshots().SingleOrDefault(s=>s.Object.Handle==Publication.Ticket.Handle);
        if(snapshot is null||snapshot.Replication is not null||!Live()||!SceneWorldObjectVisual.Matches(world,Publication,snapshot))throw new ArgumentException("Current opaque static publication required.");
        incarnation=snapshot.Object.Incarnation;var value=Read(snapshot);string display=Resolve(snapshot,value.Names);
        Label3D? candidate=null;
        try
        {
            if(createLabel){candidate=new(){Text=display,Position=basis.StaticVisual.Root.ToLocal(new Vector3(basis.StaticVisual.Bounds.GetCenter().X,basis.StaticVisual.Bounds.End.Y+.35f,basis.StaticVisual.Bounds.GetCenter().Z)),FontSize=32,PixelSize=.006f,OutlineSize=6,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,NoDepthTest=true,Modulate=new Color("#ffd166")};basis.StaticVisual.Root.AddChild(candidate);}
            Stable(snapshot,value);
            if(!basis.StaticVisual.SetVisible(value.Visibility!=0)||Current() is null)throw new InvalidOperationException("Static lifetime changed during publication.");
            Stable(snapshot,value);
            if(candidate is not null&&(!GodotObject.IsInstanceValid(candidate)||candidate.GetParent()!=basis.StaticVisual.Root))throw new InvalidOperationException("Candidate static label lost or moved.");
            label=candidate;candidate=null;observed=value;text=display;this.inner=inner;
        }
        finally{if(candidate is not null&&GodotObject.IsInstanceValid(candidate))candidate.Free();}
    }
    private void Owner(){if(System.Environment.CurrentManagedThreadId!=owner||OS.GetThreadCallerId()!=OS.GetMainThreadId())throw new InvalidOperationException("Static state requires its Godot main thread.");}
    private bool Live()=>basis?.StaticVisual is{IsRetired:false,IsPublished:true} visual&&GodotObject.IsInstanceValid(visual.Root)&&visual.Root.IsInsideTree();
    private SceneWorldObjectSnapshot? Current()
    {
        if(disposed||!Live())return null;
        var s=world!.Snapshots().SingleOrDefault(s=>s.Object.Handle==Publication.Ticket.Handle);
        return s is not null&&s.Object.Incarnation==incarnation&&s.Replication is null&&SceneWorldObjectVisual.Matches(world,Publication,s)?s:null;
    }
    private T Guard<T>(SceneWorldObjectSnapshot snapshot,Func<T> action)
    {
        if(callback){violation=true;throw new InvalidOperationException("Static callback reentry.");}
        if(!SceneWorldObjectVisual.Matches(world!,Publication,snapshot))throw new InvalidOperationException("Stale callback input.");
        var batch=world!.DrainChanges(world.Generation)??throw new InvalidOperationException("Current world required.");
        var root=basis!.StaticVisual!.Root;var transform=root.Transform;bool visible=root.Visible;
        callback=true;violation=false;T result;try{result=action();}finally{callback=false;}
        var after=world.DrainChanges(world.Generation);
        if(violation||!world.IsCurrent(batch)||after is null||after.Entries.Count!=batch.Entries.Count||
            !after.Entries.Zip(batch.Entries).All(p=>p.First.ObjectId==p.Second.ObjectId&&p.First.Kind==p.Second.Kind)||
            Current() is null||!SceneWorldObjectVisual.Matches(world,Publication,snapshot)||root.Transform!=transform||root.Visible!=visible)
            throw new InvalidOperationException("Static callback changed world or base presentation.");
        return result;
    }
    private SceneStaticPresentationProjection Read(SceneWorldObjectSnapshot snapshot)
    {
        var value=Guard(snapshot,()=>source!(snapshot));
        if(value is null)throw new InvalidDataException("Supplied static projection missing.");
        value.Validate(incarnation,observed);
        return value;
    }
    private static bool Same(SceneStaticPresentationProjection a,SceneStaticPresentationProjection b)=>a.Same(b);
    private void Stable(SceneWorldObjectSnapshot snapshot,SceneStaticPresentationProjection value)
    {if(!Same(value,Read(snapshot)))throw new InvalidOperationException("Supplied static projection changed before publication.");}
    private string Resolve(SceneWorldObjectSnapshot snapshot,CreatureView3NameState names)
    {
        string result=Guard(snapshot,()=>resolver!(names));
        if(result is null||result.Length>maximumTextUnits)throw new InvalidDataException("Bounded static caption required.");
        for(int n=0;n<result.Length;n++){char c=result[n];if(char.IsControl(c)||char.IsLowSurrogate(c))throw new InvalidDataException("Safe single-line caption required.");if(char.IsHighSurrogate(c)&&(++n>=result.Length||!char.IsLowSurrogate(result[n])))throw new InvalidDataException("Paired caption surrogates required.");}
        return result;
    }
    private void LabelOwned(){if(withLabel&&(label is null||!GodotObject.IsInstanceValid(label)||label.GetParent()!=basis!.StaticVisual!.Root))throw new InvalidOperationException("Owned static label lost or moved.");}
    private bool Synchronize()
    {
        var snapshot=Current();if(snapshot is null){Retire();return false;}LabelOwned();var value=Read(snapshot);
        string display=observed is not null&&ReferenceEquals(observed.Names,value.Names)?text:Resolve(snapshot,value.Names);
        Stable(snapshot,value);LabelOwned();
        if(label is not null)label.Text=display;
        if(!basis!.StaticVisual!.SetVisible(value.Visibility!=0)||Current() is null)throw new InvalidOperationException("Static lifetime changed during refresh.");
        Stable(snapshot,value);LabelOwned();observed=value;text=display;return true;
    }
    private void Enter(){Owner();if(busy){violation=true;throw new InvalidOperationException("Static state reentry.");}busy=true;}
    public bool Tick(double seconds)
    {Enter();try{if(disposed)return false;if(!inner!.Tick(seconds)){Retire();return false;}return Synchronize();}catch(Exception error){LastFailure=error;Retire();throw;}finally{busy=false;}}
    public bool Apply(SceneWorldObjectSnapshot snapshot)
    {Enter();try{if(disposed)return false;ArgumentNullException.ThrowIfNull(snapshot);if(!SceneWorldObjectVisual.Matches(world!,Publication,snapshot)||snapshot.Object.Incarnation!=incarnation)throw new InvalidDataException("Stale supplied world snapshot.");if(!inner!.Apply(snapshot)){Retire();return false;}return Synchronize();}catch(Exception error){LastFailure=error;Retire();throw;}finally{busy=false;}}
    private void Retire()
    {
        if(disposed)return;disposed=true;if(label is not null&&GodotObject.IsInstanceValid(label))label.Free();label=null;var owned=inner;inner=null;
        try{owned?.Dispose();}finally{finalDrain=(owned as ISceneWorldObjectPresentationDrain)?.DrainCompletion??Task.CompletedTask;world=null;basis=null;source=null;resolver=null;observed=null;text=string.Empty;}
    }
    public void Dispose(){Owner();if(busy){violation=true;throw new InvalidOperationException("Static disposal reentry.");}Retire();}
}

#endif
