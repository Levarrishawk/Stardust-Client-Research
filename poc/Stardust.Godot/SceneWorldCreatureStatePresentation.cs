using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Session;

/// <summary>World-owned resource and state freshness remain separate; no duplicate registry/store.</summary>
public sealed class SceneWorldCreatureStatePresentation : IDisposable
{
    private SceneWorldCoordinator? world;
    private ScenePreparedCreaturePublication? publication;
    private readonly SceneObjectIncarnation incarnation;
    private readonly Transform3D baseTransform;
    private IReadOnlyDictionary<string,SkeletalAlbedoFactors>? paletteFactors;
    public SceneCreatureVisual Visual { get; }
    public ulong AppliedRevision { get; private set; }
    public static bool Matches(SceneWorldCoordinator world, ScenePreparedCreaturePublication resource, SceneWorldReplicationSnapshot state)
    {
        ArgumentNullException.ThrowIfNull(world);ArgumentNullException.ThrowIfNull(resource);ArgumentNullException.ThrowIfNull(state);
        var handle=resource.Ticket.Handle;
        return world.IsCurrent(resource) && world.IsCurrent(state) && resource.Generation==state.Generation &&
            handle.RegistryId==state.Incarnation.RegistryId && handle.SceneGeneration==state.Incarnation.SceneGeneration &&
            handle.ObjectId==state.Incarnation.ObjectId && state.Baseline.ObjectId==handle.ObjectId &&
            SceneCreatureVisual.MatchesWorld(world,resource);
    }
    private SceneWorldCreatureStatePresentation(SceneWorldCoordinator world,ScenePreparedCreaturePublication publication,
        SceneObjectIncarnation incarnation,Transform3D baseTransform,SceneCreatureVisual visual,ulong revision)
    {this.world=world;this.publication=publication;this.incarnation=incarnation;this.baseTransform=baseTransform;Visual=visual;AppliedRevision=revision;}
    public static SceneWorldCreatureStatePresentation? TryPublish(SceneWorldCoordinator world,ScenePreparedCreaturePublication publication,
        SceneWorldReplicationSnapshot state,Transform3D baseTransform,Node parent,Action? beforePublication=null)
    {
        if(!Matches(world,publication,state))return null;
        var transform=SceneCreatureStatePresentation.DeriveTransform(baseTransform,state.Scalars.Height);
        var token=state.Incarnation;long generation=state.Generation;SceneWorldReplicationSnapshot? stagedState=state;
        bool Guard()=>world.FindReplication(generation,token.ObjectId) is { } current && current.Incarnation==token &&
            (stagedState is null || world.IsCurrent(stagedState));
        var visual=SceneCreatureVisual.TryPublishWorld(world,publication,transform,parent,state.Baseline.Visibility!=0,beforePublication,Guard);
        if(visual is null)return null;
        stagedState=null; // The lifetime closure must not retain an obsolete full baseline.
        return new(world,publication,token,baseTransform,visual,state.Revision);
    }
    public bool Apply(SceneWorldReplicationSnapshot state,Action? beforeApplication=null)
    {
        if(!Visual.Tick(0)){ReleaseReferences();return false;}
        var owner=world!;var resource=publication!;
        if(state.Incarnation!=incarnation || !Matches(owner,resource,state))return false;
        var transform=SceneCreatureStatePresentation.DeriveTransform(baseTransform,state.Scalars.Height);
        beforeApplication?.Invoke();
        if(!Matches(owner,resource,state)){if(!Visual.Tick(0))ReleaseReferences();return false;}
        if(!Visual.SetEngineTransform(transform) || !Visual.SetVisible(state.Baseline.Visibility!=0))return false;
        AppliedRevision=state.Revision;return true;
    }
    public bool Tick(double seconds){bool current=Visual.Tick(seconds);if(!current)ReleaseReferences();return current;}
    public bool MatchesResource(ScenePreparedCreaturePublication candidate)=>ReferenceEquals(publication,candidate) && !Visual.IsRetired;
    private List<(ShaderMaterial Material,string Shader)> Bindings()
    {
        var source=publication!.Resources.Creature;var result=new List<(ShaderMaterial,string)>();int index=0;
        foreach(string generator in source.Appearance.MeshGenerators)
        {
            string path=source.LodMeshes[generator].ExposedPaths()[0];var node=(MeshInstance3D)Visual.Root.GetChild(0).GetChild(index++);
            if(node.Mesh.GetSurfaceCount()!=source.Geometries![path].Shaders.Count)throw new InvalidDataException("Palette surface shape differs.");
            for(int surface=0;surface<node.Mesh.GetSurfaceCount();surface++)result.Add(((ShaderMaterial)node.Mesh.SurfaceGetMaterial(surface),source.Geometries[path].Shaders[surface].ShaderPath));
        }
        return result;
    }
    /// <summary>Applies an off-thread prepared palette-only view over the exact shared source closure.</summary>
    public bool ApplyPalette(SceneCreatureResources colorView,SceneWorldReplicationSnapshot state)
    {
        ArgumentNullException.ThrowIfNull(colorView);
        if(!Tick(0) || state.Incarnation!=incarnation || !Matches(world!,publication!,state))return false;
        var original=publication!.Resources;var before=paletteFactors??original.Creature.MaterialBundle!.AlbedoFactors!.Materials;
        var old=original.Creature;var next=colorView.Creature;var material=next.MaterialBundle;var oldMaterial=old.MaterialBundle!;
        if(!ReferenceEquals(original.Create,colorView.Create) || !ReferenceEquals(original.Recipe,colorView.Recipe) ||
            original.ExternalReads!=colorView.ExternalReads || original.SourceBytes!=colorView.SourceBytes ||
            !ReferenceEquals(old.Sources,next.Sources) || !ReferenceEquals(old.Geometries,next.Geometries) || !ReferenceEquals(old.Skeletons,next.Skeletons) ||
            !ReferenceEquals(old.SkinSources,next.SkinSources) || !ReferenceEquals(old.LogicalTables,next.LogicalTables) ||
            !ReferenceEquals(old.PreparedAnimation,next.PreparedAnimation) || !ReferenceEquals(old.AdditionalAnimations,next.AdditionalAnimations) ||
            !ReferenceEquals(old.Profile,next.Profile) || !ReferenceEquals(old.Appearance,next.Appearance) || !ReferenceEquals(old.LodMeshes,next.LodMeshes) ||
            !ReferenceEquals(old.FaceResources,next.FaceResources) || !ReferenceEquals(old.FaceBlueprints,next.FaceBlueprints) || !ReferenceEquals(old.GeneratedFaces,next.GeneratedFaces) ||
            material?.AlbedoFactors is null || !ReferenceEquals(oldMaterial.Materials,material.Materials) || !ReferenceEquals(oldMaterial.Textures,material.Textures) ||
            !ReferenceEquals(oldMaterial.Effects,material.Effects) || !ReferenceEquals(oldMaterial.AlbedoFactors!.Programs,material.AlbedoFactors.Programs) ||
            !ReferenceEquals(oldMaterial.CompressedNormals,material.CompressedNormals) || !ReferenceEquals(oldMaterial.VertexLitSpecular,material.VertexLitSpecular) ||
            !ReferenceEquals(oldMaterial.TangentLighting,material.TangentLighting) || !ReferenceEquals(oldMaterial.TextureSelection,material.TextureSelection) ||
            material.AlbedoFactors.Materials.Count!=before.Count)throw new InvalidDataException("Palette revision changed the owned resource closure.");
        var factors=new Dictionary<string,SkeletalAlbedoFactors>(StringComparer.Ordinal);
        foreach(var pair in before)
        {
            if(!material.AlbedoFactors.Materials.TryGetValue(pair.Key,out var factor) || factor with{FirstArgb=pair.Value.FirstArgb,SecondArgb=pair.Value.SecondArgb}!=pair.Value)
                throw new InvalidDataException("Palette revision changed program/tag/mask identity.");
            factors.Add(pair.Key,factor with{});
        }
        var bindings=Bindings(); // Resolve and validate all bindings before the first setter.
        if(!Matches(world!,publication!,state))return false;
        try{foreach(var binding in bindings)SkeletalAlbedoPreview.Apply(binding.Material,factors[binding.Shader]);}
        catch{foreach(var binding in bindings)if(GodotObject.IsInstanceValid(binding.Material))SkeletalAlbedoPreview.Apply(binding.Material,before[binding.Shader]);throw;}
        paletteFactors=new ReadOnlyDictionary<string,SkeletalAlbedoFactors>(factors);return true;
    }
    public bool Verify(bool gpu)
    {
        if(!Tick(0))return false;
        if(paletteFactors is null)return Visual.Verify(gpu);
        if(!Visual.Verify(false))return false;
        var source=publication!.Resources.Creature.MaterialBundle!;
        foreach(var binding in Bindings())
        {
            SkeletalAlbedoPreview.Verify(binding.Material,paletteFactors[binding.Shader]);
            if(!gpu)continue;
            string path=source.Materials[binding.Shader].References.Textures.Single(t=>t.Tag==0x4D41494E).AssetPath!;var original=source.Textures[path];
            if(binding.Material.GetShaderParameter("albedo_texture").AsGodotObject() is not ImageTexture texture)throw new InvalidDataException("Palette texture binding lost.");
            using var image=texture.GetImage();
            if(image.GetWidth()!=original.Width || image.GetHeight()!=original.Height || image.GetFormat()!=Image.Format.Rgba8 || image.GetMipmapCount()!=original.Mips.Count-1 ||
                !image.GetData().AsSpan().SequenceEqual(original.Mips.SelectMany(m=>m.Rgba.ToArray()).ToArray()))throw new InvalidDataException("Palette texture/mip bytes changed.");
        }
        return true;
    }
    private void ReleaseReferences(){publication=null;world=null;paletteFactors=null;}
    public void Dispose(){Visual.Dispose();ReleaseReferences();}
}

/// <summary>One serialized acknowledged consumer; resource transfer and scene boundaries stay explicit.</summary>
public sealed class SceneWorldPresentationConsumer : IDisposable
{
    private SceneWorldCoordinator? world;
    private Node? parent;
    private readonly Dictionary<ulong,SceneWorldCreatureStatePresentation> actors=new();
    private readonly int maximumActors;
    private readonly int ownerThread=System.Environment.CurrentManagedThreadId;
    private long generation,appliedGeneration;
    private ulong appliedRevision;
    private bool applied,disposed;
    public uint? RejectedHeightBits{get;private set;}
    public int Count{get{OwnerThread();return actors.Count;}}
    public SceneWorldPresentationConsumer(SceneWorldCoordinator world,Node parent,int maximumActors=64)
    {ArgumentNullException.ThrowIfNull(world);ArgumentNullException.ThrowIfNull(parent);if(maximumActors is <1 or >4096)throw new ArgumentOutOfRangeException(nameof(maximumActors));this.world=world;this.parent=parent;this.maximumActors=maximumActors;generation=world.Generation;}
    private void OwnerThread(){if(System.Environment.CurrentManagedThreadId!=ownerThread)throw new InvalidOperationException("Serialized presentation consumer owner thread required.");}
    public SceneWorldCreatureStatePresentation? Find(ulong id){OwnerThread();return actors.GetValueOrDefault(id);}
    private bool Boundary()
    {
        OwnerThread();if(disposed)return false;
        bool active=world!.State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready;
        if(generation!=world.Generation || !active){RetireAll();generation=world.Generation;}
        foreach(var pair in actors.ToArray())if(!pair.Value.Tick(0)){pair.Value.Dispose();actors.Remove(pair.Key);}
        return active;
    }
    public bool AcceptResource(ScenePreparedCreaturePublication resource)
    {
        if(!Boundary() || !world!.IsCurrent(resource))return false;
        ulong id=resource.Ticket.Handle.ObjectId;var state=world.FindReplication(resource.Generation,id);
        if(state is null || !SceneWorldCreatureStatePresentation.Matches(world,resource,state))return false;
        if(actors.TryGetValue(id,out var existing) && existing.MatchesResource(resource))return existing.Apply(state);
        if(!actors.ContainsKey(id) && actors.Count>=maximumActors)throw new InvalidDataException("Presentation actor capacity.");
        var candidate=SceneWorldCreatureStatePresentation.TryPublish(world,resource,state,GodotAppearanceTransform.ToGodot(resource.Placement.Transform),parent!);
        if(candidate is null)return false;
        if(actors.Remove(id,out var old))old.Dispose();actors.Add(id,candidate);return true;
    }
    public bool ApplyBatch(SceneWorldChangeBatch batch)
    {
        OwnerThread();ArgumentNullException.ThrowIfNull(batch);
        applied=false;RejectedHeightBits=null;
        if(!Boundary() || !world!.IsCurrent(batch))return false;
        foreach(var entry in batch.Entries)
        {
            if(!world.IsCurrent(batch))return false;
            if(entry.Current is null){if(actors.Remove(entry.ObjectId,out var removed))removed.Dispose();continue;}
            if(entry.Current.Object.Handle.ObjectId!=entry.ObjectId)throw new InvalidDataException("Observer object identity differs.");
            var state=entry.Current.Replication;
            if(state is not null && (state.Generation!=batch.Generation || state.Incarnation!=entry.Current.Object.Incarnation || !world.IsCurrent(state)))return false;
            if(!actors.TryGetValue(entry.ObjectId,out var actor))continue; // Later resource transfer resolves latest state itself.
            if(state is null){actor.Dispose();actors.Remove(entry.ObjectId);continue;}
            try{if(!actor.Apply(state)){if(!actor.Tick(0)){actor.Dispose();actors.Remove(entry.ObjectId);}else return false;}}
            catch(ArgumentOutOfRangeException){RejectedHeightBits=BitConverter.SingleToUInt32Bits(state.Scalars.Height);return false;}
        }
        if(!world.IsCurrent(batch))return false;
        appliedGeneration=batch.Generation;appliedRevision=batch.MutationRevision;applied=true;return true;
    }
    public bool AcknowledgeAppliedBatch(SceneWorldChangeBatch batch)
    {OwnerThread();return !disposed && applied && batch.Generation==appliedGeneration && batch.MutationRevision==appliedRevision && world!.AcknowledgeChanges(batch);}
    public bool ConsumePending(int maximumAttempts=4)
    {
        if(maximumAttempts is <1 or >16)throw new ArgumentOutOfRangeException(nameof(maximumAttempts));
        if(!Boundary())return false;
        for(int attempt=0;attempt<maximumAttempts;attempt++)
        {
            var batch=world!.DrainChanges(generation);if(batch is null)return false;
            if(!ApplyBatch(batch)){if(RejectedHeightBits is not null)return false;continue;}
            if(AcknowledgeAppliedBatch(batch))return true; // Stale ack retains every pending ID; redrain latest observations.
        }
        return false;
    }
    public void RetireAll(){OwnerThread();foreach(var actor in actors.Values)actor.Dispose();actors.Clear();applied=false;RejectedHeightBits=null;}
    public void Dispose(){OwnerThread();if(disposed)return;RetireAll();disposed=true;parent=null;world=null;}
}
