using System;
using System.IO;
using System.Linq;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

/// <summary>Godot recipe selection over borrowed world publications; no loading or scheduling.</summary>
public sealed class SceneWorldObjectVisualFactory
{
    private readonly SceneWorldCoordinator world;
    private readonly Node parent;
    private readonly bool terminalFrame0Inspection;
    private readonly int ownerThread = System.Environment.CurrentManagedThreadId;

    public SceneWorldObjectVisualFactory(SceneWorldCoordinator world, Node parent, bool terminalFrame0Inspection = false)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(parent);
        this.world = world; this.parent = parent; this.terminalFrame0Inspection = terminalFrame0Inspection;
    }

    public ISceneWorldObjectPresentation? Create(ScenePreparedObjectPublication publication, SceneWorldObjectSnapshot snapshot)
    {
        if (System.Environment.CurrentManagedThreadId != ownerThread || OS.GetThreadCallerId() != OS.GetMainThreadId())
            throw new InvalidOperationException("Godot main thread required.");
        ArgumentNullException.ThrowIfNull(publication); ArgumentNullException.ThrowIfNull(snapshot);
        if (!GodotObject.IsInstanceValid(parent) || !parent.IsInsideTree()) throw new ArgumentException("Live parent required.");
        if (!SceneWorldObjectVisual.Matches(world, publication, snapshot)) return null;
        var baseTransform = GodotAppearanceTransform.ToGodot(publication.Placement.Transform);
        var incarnation = snapshot.Object.Incarnation;
        SceneWorldObjectSnapshot? staged = snapshot;
        bool Current() => SceneWorldObjectVisual.Current(world, publication, incarnation) &&
            (staged is null || SceneWorldObjectVisual.Matches(world, publication, staged));
        SceneCreatureVisual? creature = null;
        SceneStaticVisual? geometry = null;
        try
        {
            switch (publication.Resources.Kind)
            {
                case SceneObjectResourceRecipe.CreatureDetail0:
                    var state = snapshot.Replication!;
                    var transform = SceneCreatureStatePresentation.DeriveTransform(baseTransform, state.Scalars.Height);
                    creature = SceneCreatureVisual.TryPublishCurrent(publication.Resources.Creature!, transform, parent,
                        Current, SceneCreatureTangibleStatePresentation.IsVisible(state.TangibleScalars));
                    if (creature is null) return null;
                    break;
                case SceneObjectResourceRecipe.StaticAppearance:
                    if (!terminalFrame0Inspection && publication.Resources.Static!.Appearance.Meshes.Values
                        .SelectMany(m => m.Materials.Values).Any(m => m.SwitchTexture is not null))
                        throw new InvalidDataException("Switch-texture inspection requires explicit terminal frame0 policy.");
                    SceneStaticVisual.ValidateDescriptor(publication.Resources.Static!, terminalFrame0Inspection);
                    geometry = SceneStaticVisual.TryPublish(publication.Resources.Static!, baseTransform, parent, Current,
                        terminalFrame0Inspection: terminalFrame0Inspection);
                    if (geometry is null) return null;
                    if (snapshot.TangibleReplication is { } tangible && !geometry.SetVisible(tangible.TangibleScalars.Visibility != 0))
                    { geometry.Dispose(); return null; }
                    break;
                default: throw new InvalidDataException("Unsupported visual recipe.");
            }
            if (!Current()) { creature?.Dispose(); geometry?.Dispose(); return null; }
            staged = null; // Lifetime checks must not retain obsolete replication snapshots.
            return new SceneWorldObjectVisual(world, publication, incarnation, baseTransform, creature, geometry);
        }
        catch { creature?.Dispose(); geometry?.Dispose(); throw; }
    }
}

/// <summary>Owns one recipe visual, while resource attempt and state incarnation remain separate.</summary>
public sealed class SceneWorldObjectVisual : ISceneWorldObjectPresentation
{
    private SceneWorldCoordinator? world;
    private readonly SceneObjectIncarnation incarnation;
    private readonly Transform3D baseTransform;
    private readonly int ownerThread = System.Environment.CurrentManagedThreadId;
    private bool disposed;
    public ScenePreparedObjectPublication Publication { get; }
    public SceneCreatureVisual? CreatureVisual { get; }
    public SceneStaticVisual? StaticVisual { get; }

    internal SceneWorldObjectVisual(SceneWorldCoordinator world, ScenePreparedObjectPublication publication,
        SceneObjectIncarnation incarnation, Transform3D baseTransform, SceneCreatureVisual? creature, SceneStaticVisual? geometry)
    { this.world = world; Publication = publication; this.incarnation = incarnation; this.baseTransform = baseTransform; CreatureVisual = creature; StaticVisual = geometry; }

    private void OwnerThread()
    {
        if (System.Environment.CurrentManagedThreadId != ownerThread || OS.GetThreadCallerId() != OS.GetMainThreadId())
            throw new InvalidOperationException("Godot main thread required.");
    }
    private static bool SameCreate(SceneCreateObjectByCrcMessage a, SceneCreateObjectByCrcMessage b) =>
        SceneObjectLifecycleCodec.EncodeCreate(a).AsSpan().SequenceEqual(SceneObjectLifecycleCodec.EncodeCreate(b));
    internal static bool Current(SceneWorldCoordinator world, ScenePreparedObjectPublication publication, SceneObjectIncarnation incarnation) =>
        world.IsCurrent(publication) && world.Snapshots().Any(s => s.Object.Handle == publication.Ticket.Handle &&
            s.Object.Incarnation == incarnation && s.Object.Phase == SceneObjectPhase.Finalized);
    internal static bool Matches(SceneWorldCoordinator world, ScenePreparedObjectPublication publication, SceneWorldObjectSnapshot snapshot)
    {
        if (!world.IsCurrent(publication)) return false;
        var current = world.Snapshots().SingleOrDefault(s => s.Object.Handle.ObjectId == publication.Ticket.Handle.ObjectId);
        if (current is null || snapshot.Object != current.Object || snapshot.Object.Handle != publication.Ticket.Handle ||
            snapshot.Object.Phase != SceneObjectPhase.Finalized || snapshot.Placement != current.Placement ||
            snapshot.Placement != publication.Placement || !SameCreate(snapshot.Object.Create, publication.Resources.Create) ||
            !SameCreate(snapshot.Placement.RawCreate, publication.Resources.Create)) return false;
        // Snapshot lists are immutable world-owned envelope observations; no opaque payload semantics are inferred.
        if (snapshot.Envelopes.Count != current.Envelopes.Count ||
            !snapshot.Envelopes.Zip(current.Envelopes).All(p => ReferenceEquals(p.First, p.Second))) return false;
        return publication.Resources.Kind switch
        {
            SceneObjectResourceRecipe.CreatureDetail0 => publication.Resources.Creature is not null && publication.Resources.Static is null &&
                snapshot.Replication is { } state && (world.ReplicationMode switch
                {
                    SceneWorldReplicationMode.CreatureView3Scalars => true,
                    SceneWorldReplicationMode.CreatureView3Mixed => state.Wounds is not null,
                    SceneWorldReplicationMode.CreatureView3MixedComponents => state.Wounds is not null && state.Components is not null,
                    _ => false
                }) &&
                world.IsCurrent(state) && state.Generation == publication.Generation && state.Incarnation == snapshot.Object.Incarnation &&
                state.Baseline.ObjectId == snapshot.Object.Handle.ObjectId,
            SceneObjectResourceRecipe.StaticAppearance => publication.Resources.Static is not null && publication.Resources.Creature is null && snapshot.Replication is null &&
                (world.TangibleReplicationEnabled
                    ? snapshot.TangibleReplication is { } tangible && world.IsCurrent(tangible) &&
                        tangible.Generation == publication.Generation && tangible.Incarnation == snapshot.Object.Incarnation &&
                        tangible.Baseline.ObjectId == snapshot.Object.Handle.ObjectId && tangible.Revision == current.TangibleReplication?.Revision
                    : snapshot.TangibleReplication is null),
            _ => false
        };
    }
    public bool Tick(double seconds)
    {
        OwnerThread(); if (disposed) return false;
        if (!Current(world!, Publication, incarnation)) { Dispose(); return false; }
        bool alive = CreatureVisual?.Tick(seconds) ?? StaticVisual!.Tick();
        if (!alive) Dispose();
        return alive;
    }
    public bool Apply(SceneWorldObjectSnapshot snapshot)
    {
        OwnerThread(); ArgumentNullException.ThrowIfNull(snapshot);
        if (!Tick(0) || snapshot.Object.Incarnation != incarnation || !Matches(world!, Publication, snapshot)) return false;
        if (CreatureVisual is null)
            return snapshot.TangibleReplication is not { } tangible || StaticVisual!.SetVisible(tangible.TangibleScalars.Visibility != 0);
        try
        {
            var state = snapshot.Replication!;
            var transform = SceneCreatureStatePresentation.DeriveTransform(baseTransform, state.Scalars.Height);
            if (!Matches(world!, Publication, snapshot)) return false;
            return CreatureVisual.SetEngineTransform(transform) && CreatureVisual.SetVisible(SceneCreatureTangibleStatePresentation.IsVisible(state.TangibleScalars));
        }
        catch { Dispose(); throw; } // An invalid current presentation state cannot leave a live stale visual.
    }
    public void Dispose()
    {
        OwnerThread(); if (disposed) return;
        disposed = true; CreatureVisual?.Dispose(); StaticVisual?.Dispose(); world = null;
    }
}
