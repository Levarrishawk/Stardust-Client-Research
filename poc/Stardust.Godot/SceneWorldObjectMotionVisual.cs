using System;
using System.IO;
using System.Linq;
using Godot;
using Stardust.Core.Session;

/// <summary>Owned visual over borrowed exact-incarnation native motion; no loading or interpolation.</summary>
public sealed class SceneWorldObjectMotionVisual : ISceneWorldObjectPresentation
{
    private SceneWorldCoordinator? world;
    private ISceneWorldObjectMotionSource? motionSource;
    private readonly SceneObjectIncarnation incarnation;
    private readonly int ownerThread = System.Environment.CurrentManagedThreadId;
    private SceneWorldObjectMotionSnapshot? latestMotion;
    private bool busy, disposed;
    public SceneWorldObjectVisual Inner { get; }
    public ScenePreparedObjectPublication Publication => Inner.Publication;
    public bool IsRetired => disposed;

    public SceneWorldObjectMotionVisual(SceneWorldCoordinator world, SceneWorldObjectVisual inner, ISceneWorldObjectMotionSource motionSource)
    {
        OwnerThread(); ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(inner); ArgumentNullException.ThrowIfNull(motionSource);
        var snapshot = world.Snapshots().SingleOrDefault(s => s.Object.Handle == inner.Publication.Ticket.Handle);
        if (snapshot is null || !SceneWorldObjectVisual.Matches(world, inner.Publication, snapshot) || !Live(inner))
            throw new ArgumentException("Exact current live inner visual required.", nameof(inner));
        var motion = motionSource.Find(snapshot);
        _ = Derive(inner, snapshot, Select(motion, snapshot.Object.Incarnation, null));
        if (!SceneWorldObjectVisual.Matches(world, inner.Publication, snapshot) || !Live(inner)) throw new ArgumentException("World or visual changed during motion acquisition.", nameof(inner));
        // No setter, Tick or disposal is invoked on inner before every initial check succeeds.
        Inner = inner; this.world = world; this.motionSource = motionSource; incarnation = snapshot.Object.Incarnation;
        latestMotion = Select(motion, incarnation, null);
    }
    private static bool Live(SceneWorldObjectVisual inner)
    {
        var root = inner.CreatureVisual?.Root ?? inner.StaticVisual!.Root;
        return !(inner.CreatureVisual?.IsRetired ?? inner.StaticVisual!.IsRetired) &&
            (inner.CreatureVisual?.IsPublished ?? inner.StaticVisual!.IsPublished) && GodotObject.IsInstanceValid(root) && root.IsInsideTree();
    }
    private void OwnerThread()
    {
        if (System.Environment.CurrentManagedThreadId != ownerThread || OS.GetThreadCallerId() != OS.GetMainThreadId())
            throw new InvalidOperationException("Motion visual requires its creating Godot main thread.");
    }
    private void Enter()
    {
        OwnerThread(); if (busy) throw new InvalidOperationException("Motion presentation reentrancy."); busy = true;
    }
    private static SceneWorldObjectMotionSnapshot? Select(SceneWorldObjectMotionSnapshot? motion, SceneObjectIncarnation incarnation, SceneWorldObjectMotionSnapshot? previous)
    {
        if (motion is null || motion.Incarnation != incarnation || (previous is not null && motion.Revision < previous.Revision)) return null;
        if (previous is not null && motion.Revision == previous.Revision && motion != previous)
            throw new InvalidDataException("A motion revision changed its immutable value.");
        if (!float.IsFinite(motion.Speed) || motion.Speed < 0) throw new ArgumentException("Finite nonnegative motion speed required.");
        return motion;
    }
    private static Transform3D Derive(SceneWorldObjectVisual inner, SceneWorldObjectSnapshot snapshot, SceneWorldObjectMotionSnapshot? motion)
    {
        var basis = GodotAppearanceTransform.ToGodot(motion?.Transform ?? inner.Publication.Placement.Transform);
        SceneCreatureVisual.ValidateEngineTransform(basis);
        if (motion is not null && (Math.Abs(basis.Basis.X.LengthSquared()-1)>1e-4f || Math.Abs(basis.Basis.Y.LengthSquared()-1)>1e-4f ||
            Math.Abs(basis.Basis.Z.LengthSquared()-1)>1e-4f || Math.Abs(basis.Basis.Determinant()-1)>1e-4f))
            throw new ArgumentException("Native motion must be a proper rigid pose; Height owns creature scale.");
        return inner.CreatureVisual is null ? basis : SceneCreatureStatePresentation.DeriveTransform(basis, snapshot.Replication!.Scalars.Height);
    }
    private SceneWorldObjectSnapshot? Current()
    {
        if (!world!.IsCurrent(Publication)) return null;
        var snapshot = world.Snapshots().SingleOrDefault(s=>s.Object.Handle==Publication.Ticket.Handle);
        return snapshot is not null && snapshot.Object.Incarnation==incarnation && SceneWorldObjectVisual.Matches(world,Publication,snapshot) ? snapshot : null;
    }
    private bool Present(SceneWorldObjectSnapshot snapshot)
    {
        var selected = Select(motionSource!.Find(snapshot), incarnation, latestMotion);
        var transform = Derive(Inner, snapshot, selected);
        if (!SceneWorldObjectVisual.Matches(world!,Publication,snapshot)) { Retire(); return false; }
        if (!Inner.Apply(snapshot)) { Retire(); return false; }
        bool applied = Inner.CreatureVisual?.SetEngineTransform(transform) ?? Inner.StaticVisual!.SetEngineTransform(transform);
        if (!applied || !SceneWorldObjectVisual.Matches(world!,Publication,snapshot)) { Retire(); return false; }
        if (selected is not null) latestMotion = selected; // Missing/stale motion resets pose but does not rewind the revision guard.
        return true;
    }
    public bool Tick(double seconds)
    {
        Enter(); try
        {
            if (disposed) return false;
            if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            var snapshot = Current();
            if (snapshot is null || !Inner.Tick(seconds)) { Retire(); return false; }
            return Present(snapshot);
        }
        catch { Retire(); throw; }
        finally { busy = false; }
    }
    public bool Apply(SceneWorldObjectSnapshot snapshot)
    {
        Enter(); try
        {
            ArgumentNullException.ThrowIfNull(snapshot); if (disposed) return false;
            if (Current() is null) { Retire(); return false; }
            if (snapshot.Object.Incarnation!=incarnation || !SceneWorldObjectVisual.Matches(world!,Publication,snapshot)) return false;
            return Present(snapshot);
        }
        catch { Retire(); throw; }
        finally { busy = false; }
    }
    private void Retire()
    {
        if (disposed) return; disposed = true;
        try { Inner.Dispose(); } finally { world=null; motionSource=null; latestMotion=null; }
    }
    public void Dispose() { Enter(); try { Retire(); } finally { busy=false; } }
}
