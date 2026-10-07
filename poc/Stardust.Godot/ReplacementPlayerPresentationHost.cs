using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Session;

/// <summary>Bounded selected-creature adapter. Owns accepted pump/world close through the existing host.</summary>
public partial class ReplacementPlayerPresentationHost : Node
{
    private readonly int ownerThread = System.Environment.CurrentManagedThreadId;
    private readonly TaskCompletionSource<bool> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private SceneWorldPresentationHost? host;
    private SceneWorldPresentationPump? pump;
    private SceneWorldCoordinator? world;
    private Action<double>? update;
    private Node? fixedParent;
    private Viewport? fixedViewport;
    private long generation;
    private ulong selected, frames;
    private bool configured, configuring, inUpdate, reentered, closing, nativeExit;
    private Exception? failure;

    private void AssertOwner()
    {
        if (System.Environment.CurrentManagedThreadId != ownerThread || OS.GetThreadCallerId() != OS.GetMainThreadId())
            throw new InvalidOperationException("Selected-player adapter requires its creating Godot main thread.");
    }
    public SceneWorldPresentationPump Pump { get { AssertOwner();return pump ?? throw new InvalidOperationException("Adapter not configured."); } }
    public Task Completion => completion.Task;
    public ulong CompletedFrames { get { AssertOwner();return host?.CompletedFrames ?? frames; } }
    public Exception? Failure { get { AssertOwner();return host?.Failure ?? failure; } }
    public bool PlayerPublished
    {
        get
        {
            AssertOwner();
            if (closing || nativeExit || host?.IsClosing != false || world?.Generation != generation ||
                world.State is ZoneSceneTransitionState.Closed or ZoneSceneTransitionState.Failed) return false;
            // Borrow only during this observation; retain no actor, publication or resource reference.
            return (pump!.Consumer.Find(selected) as SceneWorldObjectVisual)?.CreatureVisual?.IsPublished == true;
        }
    }
    private void Live()
    {
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || IsQueuedForDeletion() ||
            !GodotObject.IsInstanceValid(fixedParent) || !fixedParent!.IsInsideTree() || fixedParent.IsQueuedForDeletion() ||
            GetParent() != fixedParent || !GodotObject.IsInstanceValid(fixedViewport) || GetViewport() != fixedViewport ||
            fixedParent.GetViewport() != fixedViewport)
            throw new InvalidOperationException("Exact live selected-player parent and viewport required.");
    }
    private void Boundary()
    {
        Live();
        if (reentered) throw new InvalidOperationException("Selected-player callback attempted configuration reentry.");
        if (world!.Generation != generation || world.State is ZoneSceneTransitionState.Closed or ZoneSceneTransitionState.Failed ||
            world.Scene is { } scene && scene.ObjectId != selected)
            throw new InvalidDataException("Selected-player generation or identity changed; create a fresh adapter.");
    }
    public void Configure(SceneWorldCoordinator world, ulong selectedObjectId, Action<double> updateSession)
    {
        AssertOwner();
        if (configuring || inUpdate) { reentered = true;throw new InvalidOperationException("Selected-player configuration reentry."); }
        if (configured || closing || nativeExit) throw new InvalidOperationException("Selected-player configuration accepted once.");
        ArgumentNullException.ThrowIfNull(world);ArgumentNullException.ThrowIfNull(updateSession);
        if (selectedObjectId == 0) throw new ArgumentOutOfRangeException(nameof(selectedObjectId));
        _ = world.Snapshots(); // Owner affinity before acquiring world close ownership.
        if (world.State is ZoneSceneTransitionState.Closed or ZoneSceneTransitionState.Failed ||
            world.Scene is { } scene && scene.ObjectId != selectedObjectId ||
            world.Snapshots().Any(s => s.Object.Handle.ObjectId != selectedObjectId))
            throw new ArgumentException("Open world with matching selected identity required.");
        fixedParent = GetParent();fixedViewport = GetViewport();
        try { Live(); } catch { fixedParent = null;fixedViewport = null;throw; }
        configuring = true;
        SceneWorldPresentationHost? candidate = null;
        SceneWorldPresentationPump? accepted = null;
        try
        {
            candidate = new SceneWorldPresentationHost { Name = "SelectedPlayerProcessHost", DeferNativeExitClose = true };AddChild(candidate);Live();
            var visualFactory = new SceneWorldObjectVisualFactory(world, this);
            accepted = new SceneWorldPresentationPump(world, (publication, snapshot) =>
            {
                Boundary();
                if (publication.Ticket.Handle.ObjectId != selected || snapshot.Object.Handle.ObjectId != selected ||
                    publication.Resources.Kind != SceneObjectResourceRecipe.CreatureDetail0)
                    throw new InvalidDataException("Bounded selected-creature adapter refuses unrelated publication.");
                return visualFactory.Create(publication, snapshot);
            }, new SceneWorldPresentationPumpPolicy(MaximumActors: 1));
            this.world = world;selected = selectedObjectId;generation = world.Generation;update = updateSession;
            candidate.Configure(accepted, BeforeStep);
            pump = accepted;host = candidate;configured = true;
            _ = Observe(candidate);
        }
        catch (Exception error)
        {
            update = null;fixedParent = null;fixedViewport = null;
            if (accepted is not null)
            {
                // Construction has acquired pump/world ownership. Close this admitted prefix on engine failure.
                pump = accepted;closing = true;configured = true;failure = error;
                try { accepted.Close(); } catch (Exception cleanup) { failure = new AggregateException(error, cleanup); }
                _ = DrainRejected(accepted);
            }
            if (candidate is not null && GodotObject.IsInstanceValid(candidate)) candidate.Free();
            throw;
        }
        finally { configuring = false; }
    }
    private void BeforeStep(double seconds)
    {
        AssertOwner();Boundary();if (closing) return;
        if (inUpdate) { reentered = true;throw new InvalidOperationException("Selected-player update reentry."); }
        inUpdate = true;
        try { update!(seconds);if (!closing) Boundary(); }
        finally { inUpdate = false; }
    }
    public void RequestClose()
    {
        AssertOwner();if (closing) return;closing = true;update = null;
        if (host is not null) _ = host.CloseAsync();
        else completion.TrySetResult(true);
    }
    private async Task Observe(SceneWorldPresentationHost accepted)
    {
        try { await accepted.Completion; }
        catch (Exception error) { failure = accepted.Failure ?? error; }
        finally
        {
            // Managed state only: continuation may run after native exit while the Godot loop still drains.
            frames = accepted.CompletedFrames;closing = true;update = null;world = null;fixedParent = null;fixedViewport = null;host = null;
            if (failure is null) completion.TrySetResult(true);else completion.TrySetException(failure);
        }
    }
    private async Task DrainRejected(SceneWorldPresentationPump accepted)
    {
        try { await accepted.DrainCompletion;accepted.Dispose(); }
        catch (Exception error) { failure = new AggregateException(failure!, error); }
        finally { world = null;completion.TrySetException(failure!); }
    }
    public override void _ExitTree()
    {
        nativeExit = true;RequestClose();update = null;fixedParent = null;fixedViewport = null;
    }
}
