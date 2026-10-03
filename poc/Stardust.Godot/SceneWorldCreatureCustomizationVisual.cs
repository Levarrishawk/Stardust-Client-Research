using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Session;

/// <summary>Latest supplied color/style revision over an owned locomotion visual and borrowed world/source.</summary>
public sealed class SceneWorldCreatureCustomizationVisual : ISceneWorldObjectPresentation, ISceneWorldObjectPresentationDrain
{
    private SceneWorldCoordinator? world;
    private ISceneWorldCreatureCustomizationSource? source;
    private CreatureAppearanceDescriptorBundle? basis;
    private readonly SceneObjectIncarnation incarnation;
    private readonly HashSet<ShaderCustomizationKey> required;
    private Func<CreatureAppearanceDescriptorBundle,IReadOnlyDictionary<ShaderCustomizationKey,int>,CancellationToken,CreatureAppearanceDescriptorBundle>? compose;
    private readonly int owner=System.Environment.CurrentManagedThreadId;
    private Task retiring=Task.CompletedTask;
    private SkeletalCustomizationRevisions? revisions;
    private SceneWorldCreatureCustomizationSnapshot? requested;
    private SceneWorldCreatureCustomizationSnapshot? observed;
    private long requestNumber;
    private ulong observedRevision;
    private bool disposed,busy;
    private Task? finalDrain;
    public SceneWorldLocomotionVisual Inner {get;}
    public ScenePreparedObjectPublication Publication=>Inner.Publication;
    public bool IsRetired=>disposed;
    public ulong LastAppliedRevision {get;private set;}
    public Exception? LastFailure {get;private set;}
    /// <summary>Single generated face's accepted CPU RGBA digest; null for a multi-output appearance.</summary>
    public string? GeneratedFaceSha256 {get;private set;}
    public Task DrainCompletion
    {
        get {if(finalDrain is not null)return finalDrain;Owner();return revisions?.WorkerCompletion??retiring;}
    }

    public SceneWorldCreatureCustomizationVisual(SceneWorldCoordinator world,SceneWorldLocomotionVisual inner,
        ISceneWorldCreatureCustomizationSource source,
        Func<CreatureAppearanceDescriptorBundle,IReadOnlyDictionary<ShaderCustomizationKey,int>,CancellationToken,CreatureAppearanceDescriptorBundle>? compose=null)
    {
        Owner();ArgumentNullException.ThrowIfNull(world);ArgumentNullException.ThrowIfNull(inner);ArgumentNullException.ThrowIfNull(source);
        var snapshot=world.Snapshots().SingleOrDefault(s=>s.Object.Handle==inner.Publication.Ticket.Handle);
        var visual=inner.Inner.Inner.CreatureVisual;
        if(inner.IsRetired || snapshot is null || !SceneWorldObjectVisual.Matches(world,inner.Publication,snapshot) || visual is null ||
            visual.IsRetired || !visual.IsPublished || !GodotObject.IsInstanceValid(visual.Root) || !visual.Root.IsInsideTree())
            throw new ArgumentException("Exact current live creature locomotion inner required.");
        basis=inner.Publication.Resources.Creature!.Creature;
        if(basis.GeneratedFaces is null || basis.MaterialBundle?.AlbedoFactors is null)throw new InvalidDataException("Owned generated face/material basis required.");
        required=SkeletalAppearanceCustomization.DescribeControls(basis).Select(c=>c.Key).ToHashSet();
        GeneratedFaceSha256=FaceHash(basis);
        if(!SceneWorldObjectVisual.Matches(world,inner.Publication,snapshot))throw new ArgumentException("World changed before ownership transfer.");
        Inner=inner;this.world=world;this.source=source;this.compose=compose;incarnation=snapshot.Object.Incarnation;
    }
    private void Owner(){if(System.Environment.CurrentManagedThreadId!=owner || OS.GetThreadCallerId()!=OS.GetMainThreadId())throw new InvalidOperationException("Customization owner/main thread required.");}
    private void Enter(){Owner();if(busy)throw new InvalidOperationException("Customization presentation reentry.");busy=true;}
    private SceneWorldObjectSnapshot? Current()
    {
        var snapshot=world!.Snapshots().SingleOrDefault(s=>s.Object.Handle==Publication.Ticket.Handle);
        return snapshot is not null && snapshot.Object.Incarnation==incarnation && SceneWorldObjectVisual.Matches(world,Publication,snapshot)?snapshot:null;
    }
    private static bool Same(SceneWorldCreatureCustomizationSnapshot a,SceneWorldCreatureCustomizationSnapshot b)=>
        a.Incarnation==b.Incarnation && a.Revision==b.Revision && a.Values.Count==b.Values.Count && a.Values.All(p=>b.Values.TryGetValue(p.Key,out var value)&&value==p.Value);
    private static string? FaceHash(CreatureAppearanceDescriptorBundle appearance)=>appearance.GeneratedFaces!.Outputs.Count==1?
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(appearance.GeneratedFaces.Outputs.Single().Value.Image.Mips[0].Rgba.Span)):null;
    private void Invalidate()
    {
        if(revisions is not null){revisions.Dispose();retiring=revisions.WorkerCompletion;revisions=null;}
        requested=null;
    }
    private bool Present()
    {
        var snapshot=Current();if(snapshot is null){Retire();return false;}
        var value=source!.Find(snapshot);
        if(Current() is null){Retire();return false;}
        if(value is null || value.Incarnation!=incarnation || value.Revision<observedRevision){Invalidate();return true;}
        if(value.Revision>observedRevision)
        {
            observedRevision=value.Revision;observed=value;requested=value;requestNumber=0;LastFailure=null;
            if(!required.SetEquals(value.Values.Keys)){Invalidate();LastFailure=new InvalidDataException("Exactly the full face/material namespace union is required.");return true;}
            if(revisions is not null)requestNumber=revisions.Submit(value.Values);
        }
        else if(!Same(value,observed!)){Invalidate();LastFailure=new InvalidDataException("A customization revision changed immutable values.");return true;}
        if(requested is null)return true; // Missing/failed/stale revisions never restart themselves.
        if(revisions is null)
        {
            if(!retiring.IsCompleted)return true; // One retiring worker, one replaceable pending snapshot.
            if(retiring.IsFaulted)LastFailure=retiring.Exception!.GetBaseException(); // Observe and release fault metadata.
            retiring=Task.CompletedTask;revisions=new(basis!,compose);requestNumber=revisions.Submit(requested.Values);
        }
        if(revisions is null || !revisions.TryTake(out var ready))return true;
        if(ready!.Number!=requestNumber)return true;
        var currentSnapshot=Current();if(currentSnapshot is null){Retire();return false;}
        var fresh=source.Find(currentSnapshot);
        if(Current() is null){Retire();return false;}
        if(fresh is null || !Same(fresh,requested!)){Invalidate();return true;}
        if(ready.Failure is not null){LastFailure=ready.Failure;requested=null;return true;}
        try
        {
            if(!Inner.Inner.Inner.CreatureVisual!.ApplyCustomization(ready.Appearance!)){Retire();return false;}
            LastAppliedRevision=fresh.Revision;GeneratedFaceSha256=FaceHash(ready.Appearance!);LastFailure=null;requested=null;
        }
        catch(Exception failure){LastFailure=failure;requested=null;} // Existing GPU transaction retains the accepted display.
        return true;
    }
    public bool Tick(double seconds)
    {Enter();try{if(disposed)return false;if(!Inner.Tick(seconds)){Retire();return false;}return Present();}catch{Retire();throw;}finally{busy=false;}}
    public bool Apply(SceneWorldObjectSnapshot snapshot)
    {Enter();try{if(disposed)return false;if(!Inner.Apply(snapshot)){if(Inner.IsRetired)Retire();return false;}return Present();}catch{Retire();throw;}finally{busy=false;}}
    private void Retire()
    {
        if(disposed)return;disposed=true;
        try{Invalidate();finalDrain=retiring;Inner.Dispose();}
        finally{world=null;source=null;requested=null;observed=null;basis=null;compose=null;required.Clear();}
    }
    public void Dispose(){Enter();try{Retire();}finally{busy=false;}}
}
