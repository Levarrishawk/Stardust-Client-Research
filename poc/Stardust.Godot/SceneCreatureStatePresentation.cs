using System;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Session;

/// <summary>Modern bounded scalar presentation; immutable raw replication state remains in Core.</summary>
public sealed class SceneCreatureStatePresentation : IDisposable
{
    public const float MinimumHeight = .25f, MaximumHeight = 2f;
    private readonly SceneObjectRegistry registry;
    private readonly SceneCreatureReplicationStore store;
    private readonly SceneObjectHandle handle;
    private readonly SceneObjectIncarnation incarnation;
    private readonly Transform3D baseTransform;
    public SceneCreatureVisual Visual { get; }
    public ulong AppliedRevision { get; private set; }

    public static Transform3D DeriveTransform(Transform3D basis, float height)
    {
        SceneCreatureVisual.ValidateEngineTransform(basis);
        if (!float.IsFinite(height) || height < MinimumHeight || height > MaximumHeight)
            throw new ArgumentOutOfRangeException(nameof(height), "Modern uniform height multiplier must be in [0.25, 2].");
        var result = new Transform3D(new Basis(basis.Basis.X * height, basis.Basis.Y * height, basis.Basis.Z * height), basis.Origin);
        SceneCreatureVisual.ValidateEngineTransform(result);
        return result;
    }
    public static bool Matches(SceneObjectRegistry registry, SceneObjectHandle handle,
        SceneCreatureReplicationStore store, SceneCreatureReplicationSnapshot state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var resource = registry.Find(handle.ObjectId);
        return registry.IsCurrent(handle) && resource is not null && resource.Incarnation == state.Incarnation &&
            state.Baseline.ObjectId == handle.ObjectId && store.IsCurrent(state);
    }
    private SceneCreatureStatePresentation(SceneObjectRegistry registry, SceneObjectHandle handle,
        SceneCreatureReplicationStore store, SceneCreatureReplicationSnapshot state, Transform3D basis, SceneCreatureVisual visual)
    {
        this.registry = registry; this.handle = handle; this.store = store; incarnation = state.Incarnation;
        baseTransform = basis; Visual = visual; AppliedRevision = state.Revision;
    }
    public static SceneCreatureStatePresentation? TryPublish(SceneObjectRegistry registry, SceneObjectHandle handle,
        SceneCreatureResources source, SceneCreatureReplicationStore store, SceneCreatureReplicationSnapshot state,
        Transform3D baseTransform, Node parent, Action? beforePublication = null)
    {
        if (!Matches(registry, handle, store, state) || !SceneCreatureVisual.Matches(registry, handle, source)) return null;
        var transform = DeriveTransform(baseTransform, state.Scalars.Height);
        var token = state.Incarnation;
        SceneCreatureReplicationSnapshot? stagedState = state;
        // During allocation/attachment the supplied state must stay current. After publication,
        // newer scalar revisions keep the same resource incarnation alive.
        bool Guard() => registry.IsCurrent(token) && store.Find(token) is not null &&
            (stagedState is null || store.IsCurrent(stagedState));
        var visual = SceneCreatureVisual.TryPublish(registry, handle, source, transform, parent,
            state.Baseline.Visibility != 0, beforePublication, Guard);
        if (visual is null) return null;
        stagedState = null; // Do not retain the obsolete baseline through the visual's lifetime closure.
        return new(registry, handle, store, state, baseTransform, visual);
    }
    public bool Apply(SceneCreatureReplicationSnapshot state, Action? beforeApplication = null)
    {
        if (!Visual.Tick(0)) return false;
        if (state.Incarnation != incarnation || !Matches(registry, handle, store, state)) return false;
        var transform = DeriveTransform(baseTransform, state.Scalars.Height); // Validate before any GPU mutation.
        beforeApplication?.Invoke();
        if (!Matches(registry, handle, store, state)) { Visual.Tick(0); return false; }
        if (!Visual.SetEngineTransform(transform) || !Visual.SetVisible(state.Baseline.Visibility != 0)) return false;
        AppliedRevision = state.Revision; return true;
    }
    public bool Tick(double seconds) => Visual.Tick(seconds);
    public void Dispose() => Visual.Dispose();
}
