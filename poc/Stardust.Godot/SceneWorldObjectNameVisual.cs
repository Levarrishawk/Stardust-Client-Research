using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

/// <summary>Optional diagnostic caption; owns label/inner, borrows world/base and a pure bounded resolver.</summary>
public sealed class SceneWorldObjectNameVisual : ISceneWorldObjectPresentation,ISceneWorldObjectPresentationDrain
{
    private SceneWorldCoordinator? world;
    private SceneWorldObjectVisual? basis;
    private ISceneWorldObjectPresentation? inner;
    private Func<CreatureView3NameState,string>? resolver;
    private CreatureView3NameState? observed;
    private Label3D? label;
    private readonly SceneObjectIncarnation incarnation;
    private readonly int owner=System.Environment.CurrentManagedThreadId,maximumTextUnits;
    private bool busy,disposed;
    private Task finalDrain=Task.CompletedTask;
    [ThreadStatic]private static bool resolving;
    [ThreadStatic]private static bool resolutionViolation;
    public ScenePreparedObjectPublication Publication {get;}
    public bool IsRetired=>disposed;
    public string Text=>label?.Text??string.Empty;
    public bool IsVisibleInTree=>label is not null&&GodotObject.IsInstanceValid(label)&&label.IsVisibleInTree();
    public ulong LabelId=>label?.GetInstanceId()??0;
    public Exception? LastFailure {get;private set;}
    public Task DrainCompletion=>disposed?finalDrain:(inner as ISceneWorldObjectPresentationDrain)?.DrainCompletion??Task.CompletedTask;

    public SceneWorldObjectNameVisual(SceneWorldCoordinator world,ISceneWorldObjectPresentation inner,
        SceneWorldObjectVisual basis,Func<CreatureView3NameState,string> nameResolver,int maximumTextUnits=256)
    {
        Owner();ArgumentNullException.ThrowIfNull(world);ArgumentNullException.ThrowIfNull(inner);ArgumentNullException.ThrowIfNull(basis);ArgumentNullException.ThrowIfNull(nameResolver);
        if(maximumTextUnits is <1 or >1024)throw new ArgumentOutOfRangeException(nameof(maximumTextUnits));
        if(!ReferenceEquals(inner.Publication,basis.Publication))throw new ArgumentException("Exact base/inner publication required.");
        this.world=world;this.basis=basis;resolver=nameResolver;this.maximumTextUnits=maximumTextUnits;Publication=basis.Publication;
        var snapshot=world.Snapshots().SingleOrDefault(s=>s.Object.Handle==Publication.Ticket.Handle);
        if(snapshot?.Replication is null||!Live()||!SceneWorldObjectVisual.Matches(world,Publication,snapshot))throw new ArgumentException("Current typed creature required.");
        incarnation=snapshot.Object.Incarnation;
        string text=Resolve(snapshot);
        Label3D? candidate=null;
        try
        {
            candidate=new(){Text=text,Position=new Vector3(0,2.05f,0),FontSize=32,PixelSize=.006f,OutlineSize=6,
                Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,NoDepthTest=true,Modulate=new Color("#ffd166")};
            basis.CreatureVisual!.Root.AddChild(candidate);
            if(Current() is not { } current||!ReferenceEquals(current.Replication!.Names,snapshot.Replication.Names))throw new InvalidOperationException("World changed before caption ownership transfer.");
            label=candidate;observed=snapshot.Replication.Names;this.inner=inner;candidate=null;
        }
        finally{candidate?.Free();}
    }
    private void Owner(){if(System.Environment.CurrentManagedThreadId!=owner||OS.GetThreadCallerId()!=OS.GetMainThreadId())throw new InvalidOperationException("Name presentation requires its Godot main thread.");}
    private bool Live()=>basis?.CreatureVisual is {IsRetired:false,IsPublished:true} visual&&GodotObject.IsInstanceValid(visual.Root)&&visual.Root.IsInsideTree();
    private SceneWorldObjectSnapshot? Current()
    {
        if(disposed||!Live())return null;
        var snapshot=world!.Snapshots().SingleOrDefault(s=>s.Object.Handle==Publication.Ticket.Handle);
        return snapshot?.Replication is not null&&snapshot.Object.Incarnation==incarnation&&SceneWorldObjectVisual.Matches(world,Publication,snapshot)?snapshot:null;
    }
    private string Resolve(SceneWorldObjectSnapshot snapshot)
    {
        if(resolving){resolutionViolation=true;throw new InvalidOperationException("Name resolver reentry.");}
        var guard=world!.DrainChanges(world.Generation)??throw new InvalidOperationException("Current world required.");
        var root=basis!.CreatureVisual!.Root;var transform=root.Transform;bool visible=root.Visible;
        resolving=true;resolutionViolation=false;string text;
        try{text=resolver!(snapshot.Replication!.Names);}
        finally{resolving=false;}
        var after=world.DrainChanges(world.Generation);
        if(resolutionViolation||!world.IsCurrent(guard)||after is null||after.Entries.Count!=guard.Entries.Count||
            !after.Entries.Zip(guard.Entries).All(p=>p.First.ObjectId==p.Second.ObjectId&&p.First.Kind==p.Second.Kind)||
            Current() is not { } current||!ReferenceEquals(current.Replication!.Names,snapshot.Replication!.Names)||
            root.Transform!=transform||root.Visible!=visible)
            throw new InvalidOperationException("Name resolver changed world or presentation state.");
        if(text is null||text.Length>maximumTextUnits)throw new InvalidDataException("Bounded name display text required.");
        for(int n=0;n<text.Length;n++)
        {
            char c=text[n];if(char.IsControl(c)||char.IsLowSurrogate(c))throw new InvalidDataException("Safe single-line name text required.");
            if(char.IsHighSurrogate(c)){if(++n>=text.Length||!char.IsLowSurrogate(text[n]))throw new InvalidDataException("Paired name surrogates required.");}
        }
        return text;
    }
    private void Enter(){Owner();if(busy){resolutionViolation=true;throw new InvalidOperationException("Name presentation reentry.");}busy=true;}
    private bool Synchronize()
    {
        var snapshot=Current();if(snapshot is null){Retire();return false;}
        if(label is null||!GodotObject.IsInstanceValid(label)||label.GetParent()!=basis!.CreatureVisual!.Root)throw new InvalidOperationException("Owned caption lost.");
        if(ReferenceEquals(observed,snapshot.Replication!.Names))return true;
        string text=Resolve(snapshot);
        if(!GodotObject.IsInstanceValid(label)||label.GetParent()!=basis!.CreatureVisual!.Root)throw new InvalidOperationException("Resolver moved or destroyed the owned caption.");
        label.Text=text;observed=snapshot.Replication.Names;return true;
    }
    public bool Tick(double seconds)
    {
        Enter();try{if(disposed)return false;if(!inner!.Tick(seconds)){Retire();return false;}return Synchronize();}
        catch(Exception error){LastFailure=error;Retire();throw;}finally{busy=false;}
    }
    public bool Apply(SceneWorldObjectSnapshot snapshot)
    {
        Enter();try{if(disposed)return false;ArgumentNullException.ThrowIfNull(snapshot);
            if(!SceneWorldObjectVisual.Matches(world!,Publication,snapshot)||snapshot.Object.Incarnation!=incarnation)return false;
            if(!inner!.Apply(snapshot)){Retire();return false;}return Synchronize();}
        catch(Exception error){LastFailure=error;Retire();throw;}finally{busy=false;}
    }
    private void Retire()
    {
        if(disposed)return;disposed=true;
        if(label is not null&&GodotObject.IsInstanceValid(label))label.Free();label=null;
        var owned=inner;inner=null;
        try{owned?.Dispose();}finally{finalDrain=(owned as ISceneWorldObjectPresentationDrain)?.DrainCompletion??Task.CompletedTask;world=null;basis=null;resolver=null;observed=null;}
    }
    public void Dispose(){Owner();if(busy){resolutionViolation=true;throw new InvalidOperationException("Name disposal reentry.");}Retire();}
}
