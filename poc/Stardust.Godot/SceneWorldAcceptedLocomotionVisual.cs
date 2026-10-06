using System;
using System.IO;
using System.Linq;
using System.Threading;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Session;

/// <summary>Owns a caller-associated presentation chain and displays supplied accepted locomotion without advancing it.</summary>
public sealed class SceneWorldAcceptedLocomotionVisual : ISceneWorldObjectPresentation
{
    private readonly int owner = System.Environment.CurrentManagedThreadId;
    private SceneWorldCoordinator? world;
    private ISceneWorldObjectPresentation? presentation;
    private Func<LocomotionPreviewState>? source;
    private Func<bool>? enabled;
    private bool busy, reentered, disposed;
    private SceneWorldObjectSnapshot? observed;
    public SceneWorldObjectMotionVisual Inner { get; }
    public ScenePreparedObjectPublication Publication => Inner.Publication;
    public bool IsRetired => disposed;
    public string SelectedClip { get; private set; } = "idle";
    public double Frame { get; private set; }
    public long SourceReads { get; private set; }
    public long EnableReads { get; private set; }
    public long Presentations { get; private set; }
    public LocomotionPreviewState? LastState { get; private set; }

    // The caller guarantees that ownedPresentation contains inner exactly once and
    // that source belongs to that actor/publication, with the same prepared clip metadata.
    public SceneWorldAcceptedLocomotionVisual(SceneWorldCoordinator world,
        SceneWorldObjectMotionVisual inner, Func<LocomotionPreviewState> source,
        ISceneWorldObjectPresentation? ownedPresentation = null, Func<bool>? enabled = null)
    {
        Owner(); ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(source);
        var snapshot = world.Snapshots().SingleOrDefault(s => s.Object.Handle == inner.Publication.Ticket.Handle);
        if (snapshot is null || !SceneWorldObjectVisual.Matches(world, inner.Publication, snapshot) || !Live(inner))
            throw new ArgumentException("Current live creature motion visual required.", nameof(inner));
        if (ownedPresentation is not null && !ReferenceEquals(ownedPresentation.Publication, inner.Publication))
            throw new ArgumentException("Owned chain must expose the exact inner publication.", nameof(ownedPresentation));
        var resource = inner.Publication.Resources.Creature!.Creature;
        if (resource.PreparedAnimation is null || resource.AdditionalAnimations is null ||
            !resource.AdditionalAnimations.ContainsKey("walk") || !resource.AdditionalAnimations.ContainsKey("run"))
            throw new InvalidDataException("Prepared idle/walk/run resources required.");
        if (!SceneWorldObjectVisual.Matches(world, inner.Publication, snapshot) || !Live(inner))
            throw new ArgumentException("World or inner changed during constructor checks.", nameof(inner));
        Inner = inner; this.world = world; this.source = source;
        presentation = ownedPresentation ?? inner; this.enabled = enabled;
    }
    private void Owner()
    {
        if (System.Environment.CurrentManagedThreadId != owner || OS.GetThreadCallerId() != OS.GetMainThreadId())
            throw new InvalidOperationException("Accepted locomotion requires its creating Godot main thread.");
    }
    private void Enter()
    {
        Owner();
        if (busy) { reentered = true; throw new InvalidOperationException("Accepted locomotion reentry."); }
        busy = true; reentered = false; observed = null;
    }
    private static bool Live(SceneWorldObjectMotionVisual inner)
    {
        var visual = inner.Inner.CreatureVisual;
        return !inner.IsRetired && visual is not null && !visual.IsRetired && visual.IsPublished &&
            GodotObject.IsInstanceValid(visual.Root) && !visual.Root.IsQueuedForDeletion() && visual.Root.IsInsideTree();
    }
    private SceneWorldObjectSnapshot? Current()
    {
        if (!Live(Inner) || !world!.IsCurrent(Publication)) return null;
        var snapshot = world.Snapshots().SingleOrDefault(s => s.Object.Handle == Publication.Ticket.Handle);
        return snapshot is not null && SceneWorldObjectVisual.Matches(world, Publication, snapshot) ? snapshot : null;
    }
    private void Guard(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (reentered) throw new InvalidOperationException("A callback attempted accepted locomotion reentry.");
    }
    private bool Enable(CancellationToken token)
    {
        bool result = true;
        if (enabled is not null) { EnableReads++; result = enabled(); }
        Guard(token); return result;
    }
    private bool CheckCurrent(CancellationToken token)
    {
        Guard(token); var snapshot = Current();
        if (snapshot is not null && (observed is null || SceneWorldObjectVisual.Matches(world!, Publication, observed)))
        { observed ??= snapshot; return true; }
        Retire(); return false;
    }
    private bool Present(CancellationToken token)
    {
        if (!CheckCurrent(token)) return false;
        SourceReads++; var state = source!(); Guard(token);
        if (!CheckCurrent(token)) return false;
        if (state is null) throw new InvalidDataException("Locomotion source returned null.");
        string key = state.Mode switch
        {
            PreviewLocomotionMode.Idle => "idle", PreviewLocomotionMode.Walk => "walk",
            PreviewLocomotionMode.Run => "run", _ => throw new InvalidDataException("Unknown locomotion mode.")
        };
        var resource = Publication.Resources.Creature!.Creature;
        PreparedSkeletalAnimation clip = key == "idle" ? resource.PreparedAnimation! : resource.AdditionalAnimations![key];
        var heading = state.Heading;
        if (!double.IsFinite(state.Frame) || state.Frame < 0 || state.Frame > clip.EndFrame ||
            !double.IsFinite(state.NormalizedPhase) || state.NormalizedPhase < 0 || state.NormalizedPhase >= 1 ||
            !double.IsFinite(state.AcceptedDistance) || state.AcceptedDistance < 0 || state.Switches < 0 ||
            !float.IsFinite(heading.X) || !float.IsFinite(heading.Y) || !float.IsFinite(heading.Z) ||
            Math.Abs(heading.Y) > 1e-5f || Math.Abs(heading.LengthSquared() - 1) > 1e-4f)
            throw new InvalidDataException("Finite clip-bounded frame, phase, distance and planar unit heading required.");
        bool applied = key == "idle" ? Inner.Inner.CreatureVisual!.SeekIdle(state.Frame) :
            Inner.Inner.CreatureVisual!.SetMovement(key, state.Frame, .25);
        if (!applied || !CheckCurrent(token)) { Retire(); return false; }
        SelectedClip = key; Frame = state.Frame; LastState = state; Presentations++; return true;
    }
    public bool Tick(double seconds) => Tick(seconds, default);
    public bool Tick(double seconds, CancellationToken token)
    {
        Enter(); try
        {
            if (disposed) return false;
            if (!double.IsFinite(seconds) || seconds < 0 || seconds > 60) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (!CheckCurrent(token)) return false;
            bool active = Enable(token); if (!CheckCurrent(token)) return false;
            bool result = presentation!.Tick(active ? 0 : seconds);
            if (!result || !CheckCurrent(token)) { Retire(); return false; }
            return !active || Present(token);
        }
        catch { Retire(); throw; } finally { busy = false; observed = null; }
    }
    public bool Apply(SceneWorldObjectSnapshot snapshot) => Apply(snapshot, default);
    public bool Apply(SceneWorldObjectSnapshot snapshot, CancellationToken token)
    {
        Enter(); try
        {
            ArgumentNullException.ThrowIfNull(snapshot); if (disposed) return false;
            if (!CheckCurrent(token)) return false;
            if (!SceneWorldObjectVisual.Matches(world!, Publication, snapshot)) return false;
            bool active = Enable(token); if (!CheckCurrent(token)) return false;
            bool result = presentation!.Apply(snapshot);
            if (!result || !CheckCurrent(token)) { Retire(); return false; }
            return !active || Present(token);
        }
        catch { Retire(); throw; } finally { busy = false; observed = null; }
    }
    private void Retire()
    {
        if (disposed) return; disposed = true;
        try { presentation!.Dispose(); }
        finally { presentation = null; world = null; source = null; enabled = null; LastState = null; }
    }
    public void Dispose() { Enter(); try { Retire(); } finally { busy = false; } }
}
