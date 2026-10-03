using System.Buffers.Binary;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

public sealed class SceneWorldSelection
{
    private readonly byte[] message;
    internal SceneWorldSelection(long generation, byte[] message) { Generation = generation; this.message = message; }
    public long Generation { get; }
    public byte[] CopySelectMessage() => (byte[])message.Clone();
}
[Flags]
public enum SceneWorldChangeKind { Lifecycle = 1, Replication = 2 }
public sealed record SceneWorldChangePolicy(int MaximumPendingObjects = 4096, ulong MutationRevisionSeed = 0);
public sealed record SceneWorldChangeEntry(ulong ObjectId, SceneWorldChangeKind Kind, SceneWorldObjectSnapshot? Current);
public sealed class SceneWorldChangeBatch
{
    internal SceneWorldChangeBatch(object owner, long generation, ulong revision, SceneWorldChangeEntry[] entries)
    { Owner = owner; Generation = generation; MutationRevision = revision; Entries = Array.AsReadOnly(entries); }
    internal object Owner { get; }
    public long Generation { get; }
    public ulong MutationRevision { get; }
    public IReadOnlyList<SceneWorldChangeEntry> Entries { get; }
}
public enum SceneWorldReplicationMode { Opaque, CreatureView3Scalars, CreatureView3Mixed, CreatureView3MixedComponents }
public sealed record SceneWorldReplicationPolicy(SceneWorldReplicationMode Mode, CreatureView3BaselineLimits BaselineLimits,
    int MaximumScalarUpdates, long MaximumDecodedBytes, CreatureView3MixedDeltaLimits? MixedLimits = null,
    CreatureWoundsApplicationPolicy WoundsPolicy = CreatureWoundsApplicationPolicy.StrictInterval,
    CreatureView3ComponentsDeltaLimits? ComponentsLimits = null, SceneCreatureComponentsReplicationPolicy? ComponentsPolicy = null, ulong RevisionSeed = 0);
public sealed class SceneWorldReplicationSnapshot
{
    internal SceneWorldReplicationSnapshot(object owner, long generation, SceneCreatureReplicationSnapshot state)
    { Owner = owner; Generation = generation; State = state; }
    internal object Owner { get; }
    internal SceneCreatureReplicationSnapshot State { get; }
    public long Generation { get; }
    public SceneObjectIncarnation Incarnation => State.Incarnation;
    public ulong Revision => State.Revision;
    public CreatureView3BaselineSnapshot Baseline => State.Baseline;
    public CreatureView3ScalarState Scalars => State.Scalars;
    public CreatureView3WoundsState? Wounds => State.Wounds;
    public CreatureView3ComponentsState? Components => State.Components;
    public CreatureView3TangibleScalarState TangibleScalars => State.TangibleScalars;
    public CreatureView3NameState Names => State.Names;
}
public sealed record SceneWorldTangibleReplicationPolicy(CreatureView3BaselineLimits BaselineLimits,
    TangibleView3DeltaLimits DeltaLimits,SceneTangibleReplicationLimits StoreLimits,ulong RevisionSeed=0,
    Func<ReplicationEnvelope,TangibleView3BaselineSnapshot>? BaselineDecoder=null,
    Func<ReplicationEnvelope,TangibleView3DeltaPlan>? DeltaDecoder=null);
public sealed class SceneWorldTangibleReplicationSnapshot
{
    internal SceneWorldTangibleReplicationSnapshot(object owner,long generation,SceneTangibleReplicationSnapshot state)
    {Owner=owner;Generation=generation;State=state;}
    internal object Owner {get;}
    internal SceneTangibleReplicationSnapshot State {get;}
    public long Generation {get;}
    public SceneObjectIncarnation Incarnation=>State.Incarnation;
    public ulong Revision=>State.Revision;
    public TangibleView3BaselineSnapshot Baseline=>State.Baseline;
    public CreatureView3NameState Names=>State.Names;
    public CreatureView3TangibleScalarState TangibleScalars=>State.TangibleScalars;
    public ReplicationByteString Customization=>State.Customization;
    public CreatureView3ComponentsState Components=>State.Components;
}
public sealed record SceneWorldObjectSnapshot(SceneObjectSnapshot Object, SceneCreatePlacement Placement, IReadOnlyList<ReplicationEnvelope> Envelopes)
{
    public SceneWorldReplicationSnapshot? Replication { get; init; }
    public SceneWorldTangibleReplicationSnapshot? TangibleReplication {get;init;}
}
public sealed class ScenePreparedCreaturePublication
{
    internal object Owner { get; }
    internal ScenePreparedCreaturePublication(object owner, long generation, SceneCreatureResourceTicket ticket, SceneCreatePlacement placement, SceneCreatureResources resources)
    { Owner = owner; Generation = generation; Ticket = ticket; Placement = placement; Resources = resources; }
    public long Generation { get; }
    public SceneCreatureResourceTicket Ticket { get; }
    public SceneCreatePlacement Placement { get; }
    public SceneCreatureResources Resources { get; }
}
public sealed class ScenePreparedObjectPublication
{
    internal object Owner { get; }
    internal ScenePreparedObjectPublication(object owner, long generation, SceneObjectResourceTicket ticket, SceneCreatePlacement placement, SceneObjectResources resources)
    { Owner = owner; Generation = generation; Ticket = ticket; Placement = placement; Resources = resources; }
    public long Generation { get; }
    public SceneObjectResourceTicket Ticket { get; }
    public SceneCreatePlacement Placement { get; }
    public SceneObjectResources Resources { get; }
}
public sealed record SceneWorldObjectResourcePoll(IReadOnlyList<ScenePreparedObjectPublication> Prepared, IReadOnlyList<SceneObjectResourceCompletion> Retired);
public sealed record SceneWorldResourcePoll(IReadOnlyList<ScenePreparedCreaturePublication> Prepared, IReadOnlyList<SceneCreatureResourceCompletion> Retired);

/// <summary>Modern serialized offline world barrier; optional typed CREO3, no terrain/engine/live application.</summary>
public sealed class SceneWorldCoordinator : IDisposable
{
    private sealed record Pending(SceneObjectHandle Handle, bool Restart);
    private readonly record struct ChangeSlot(SceneWorldChangeKind Kind, ulong FirstRevision);
    private Dictionary<ulong, ChangeSlot> changes = new();
    private readonly int maximumPendingChanges;
    private ulong mutationRevision;
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private readonly object identity = new();
    private readonly ZoneSceneTransition transition;
    private readonly SceneObjectRegistry registry;
    private readonly SceneCreatureResourceInitializer? initializer;
    private readonly SceneObjectResourceInitializer? objectInitializer;
    private readonly ObjectTemplateCatalog catalog;
    private readonly IReadOnlyDictionary<SceneObjectResourceRecipe, SceneObjectResourceOptions>? objectOptions;
    private readonly SceneTangibleReplicationStore? tangible;
    private readonly SceneWorldTangibleReplicationPolicy? tangiblePolicy;
    private bool reentered;
    private ulong callbackVersion;
    private readonly SceneCreatureReplicationStore? replication;
    private readonly SceneWorldReplicationPolicy? replicationPolicy;
    private readonly CreatureView3CompositeDeltaLimits? compositeLimits;
    private readonly SceneCreatureResourceRequest? configuration;
    private readonly SceneCreateZeroQuaternionPolicy zeroPolicy;
    private readonly int maximumOutstandingJobs, maximumEnvelopes, maximumEnvelopeBytes, maximumPayloadBytes;
    private readonly Dictionary<ulong, SceneCreatePlacement> placements = new();
    private readonly Dictionary<ulong, List<ReplicationEnvelope>> envelopes = new();
    private readonly List<Pending> pending = new();
    private readonly HashSet<ulong> started = new();
    private int envelopeCount, envelopeBytes;
    private bool busy, closed;
    public long Generation => transition.Generation;
    public ZoneSceneTransitionState State => transition.State;
    public CmdStartSceneMessage? Scene => transition.Scene;
    public int StagedCount => transition.StagedCount;
    public int OutstandingJobs => objectInitializer?.OutstandingJobs ?? initializer!.OutstandingJobs;
    public Task DrainCompletion => objectInitializer?.DrainCompletion ?? initializer!.DrainCompletion;
    public bool TangibleReplicationEnabled=>tangiblePolicy is not null;
    public SceneWorldReplicationMode ReplicationMode => replicationPolicy?.Mode ?? SceneWorldReplicationMode.Opaque;
    public long RetainedReplicationDecodedBytes
    { get { Enter(); try { return replication?.RetainedDecodedBytes ?? 0; } finally { busy = false; } } }
    public long RetainedTangibleReplicationDecodedBytes {get{Enter();try{return tangible?.RetainedDecodedBytes??0;}finally{busy=false;}}}
    public SceneWorldCoordinator(ObjectTemplateCatalog catalog, LogicalAnimationClipSelection idle,
        IReadOnlyDictionary<string, LogicalAnimationClipSelection> movement,
        Func<SceneCreatureResourceRequest, CancellationToken, Task<SceneCreatureResources>> loader,
        SceneCreateZeroQuaternionPolicy zeroPolicy, int maximumObjects = 64, int maximumOutstandingJobs = 8,
        int maximumStagedMessages = 256, int maximumStagedBytes = 1048576,
        int maximumEnvelopes = 256, int maximumEnvelopeBytes = 1048576, int maximumPayloadBytes = 65536,
        SceneWorldReplicationPolicy? replicationPolicy = null, SceneWorldChangePolicy? changePolicy = null, SceneWorldTangibleReplicationPolicy? tangibleReplicationPolicy = null)
        : this(catalog, zeroPolicy, maximumObjects, maximumOutstandingJobs, maximumStagedMessages, maximumStagedBytes, maximumEnvelopes, maximumEnvelopeBytes, maximumPayloadBytes, replicationPolicy, changePolicy, tangibleReplicationPolicy)
    {
        var recipe = catalog.Entries.Values.First();
        configuration = new(new(default, new(1, 0, 0, 0, 1, 0, 0, 0, recipe.TemplateCrc, 0), SceneObjectPhase.PendingBaselines, 0, 0), catalog, idle, movement);
        initializer = new(registry, loader, maximumOutstandingJobs, maximumObjects);
    }
    public SceneWorldCoordinator(ObjectTemplateCatalog catalog,
        IReadOnlyDictionary<SceneObjectResourceRecipe, SceneObjectResourceOptions> options,
        Func<SceneObjectResourceRequest, CancellationToken, Task<SceneObjectResources>> loader,
        SceneCreateZeroQuaternionPolicy zeroPolicy, int maximumObjects = 64, int maximumOutstandingJobs = 8,
        int maximumStagedMessages = 256, int maximumStagedBytes = 1048576,
        int maximumEnvelopes = 256, int maximumEnvelopeBytes = 1048576, int maximumPayloadBytes = 65536,
        SceneWorldReplicationPolicy? replicationPolicy = null, SceneWorldChangePolicy? changePolicy = null, SceneWorldTangibleReplicationPolicy? tangibleReplicationPolicy = null)
        : this(catalog, zeroPolicy, maximumObjects, maximumOutstandingJobs, maximumStagedMessages, maximumStagedBytes, maximumEnvelopes, maximumEnvelopeBytes, maximumPayloadBytes, replicationPolicy, changePolicy, tangibleReplicationPolicy)
    {
        ArgumentNullException.ThrowIfNull(options); ArgumentNullException.ThrowIfNull(loader);
        var copy = new Dictionary<SceneObjectResourceRecipe, SceneObjectResourceOptions>();
        foreach (var pair in options)
        {
            ArgumentNullException.ThrowIfNull(pair.Value);
            Require(pair.Key == pair.Value.Recipe, "Options key/recipe mismatch."); copy.Add(pair.Key, SceneObjectResourceRequest.CopyOptions(pair.Value));
        }
        foreach (var recipe in catalog.Entries.Values) Require(copy.ContainsKey(recipe.Recipe), "Explicit options required for every catalog recipe.");
        objectOptions = new System.Collections.ObjectModel.ReadOnlyDictionary<SceneObjectResourceRecipe, SceneObjectResourceOptions>(copy);
        objectInitializer = new(registry, loader, maximumOutstandingJobs, maximumObjects);
    }
    private SceneWorldCoordinator(ObjectTemplateCatalog catalog,
        SceneCreateZeroQuaternionPolicy zeroPolicy, int maximumObjects = 64, int maximumOutstandingJobs = 8,
        int maximumStagedMessages = 256, int maximumStagedBytes = 1048576,
        int maximumEnvelopes = 256, int maximumEnvelopeBytes = 1048576, int maximumPayloadBytes = 65536,
        SceneWorldReplicationPolicy? replicationPolicy = null, SceneWorldChangePolicy? changePolicy = null, SceneWorldTangibleReplicationPolicy? tangibleReplicationPolicy = null)
    {
        ArgumentNullException.ThrowIfNull(catalog); this.catalog = catalog;
        if (catalog.Entries.Count == 0) throw new ArgumentException("Explicit nonempty catalog required.", nameof(catalog));
        var delivery = changePolicy ?? new SceneWorldChangePolicy();
        if (delivery.MaximumPendingObjects is < 1 or > 4096) throw new ArgumentOutOfRangeException(nameof(changePolicy));
        maximumPendingChanges = delivery.MaximumPendingObjects; mutationRevision = delivery.MutationRevisionSeed;
        if (zeroPolicy is not (SceneCreateZeroQuaternionPolicy.Reject or SceneCreateZeroQuaternionPolicy.Identity) ||
            maximumObjects is < 1 or > 4096 || maximumOutstandingJobs is < 1 or > 4096 || maximumStagedMessages is < 1 or > 4096 ||
            maximumStagedBytes is < 1 or > 16777216 || maximumEnvelopes is < 1 or > 4096 || maximumEnvelopeBytes is < 25 or > 16777216 || maximumPayloadBytes is < 2 or > 1048576)
            throw new ArgumentOutOfRangeException(nameof(maximumObjects));
        if (replicationPolicy is not null)
        {
            ArgumentNullException.ThrowIfNull(replicationPolicy.BaselineLimits);
            var limits = replicationPolicy.BaselineLimits;
            if (replicationPolicy.Mode is not (SceneWorldReplicationMode.Opaque or SceneWorldReplicationMode.CreatureView3Scalars or SceneWorldReplicationMode.CreatureView3Mixed or SceneWorldReplicationMode.CreatureView3MixedComponents) ||
                limits.MaximumPayloadBytes < 2 || limits.MaximumPayloadBytes > maximumPayloadBytes ||
                limits.MaximumStringBytes is < 0 or > 1048576 || limits.MaximumUnicodeUnits is < 0 or > 524288 ||
                limits.MaximumContainerElements is < 0 or > 262144 || limits.MaximumDecodedBytes is < 0 or > 134217728 ||
                replicationPolicy.MaximumScalarUpdates is < 0 or > 4096 || replicationPolicy.MaximumDecodedBytes is < 81 or > 134217728)
                throw new ArgumentOutOfRangeException(nameof(replicationPolicy));
            if (replicationPolicy.WoundsPolicy is not (CreatureWoundsApplicationPolicy.StrictInterval or CreatureWoundsApplicationPolicy.LegacyBounded))
                throw new ArgumentOutOfRangeException(nameof(replicationPolicy));
            if (replicationPolicy.Mode is SceneWorldReplicationMode.CreatureView3Mixed or SceneWorldReplicationMode.CreatureView3MixedComponents)
            {
                ArgumentNullException.ThrowIfNull(replicationPolicy.MixedLimits); var mixed = replicationPolicy.MixedLimits;
                if (mixed.MaximumPayloadBytes < 2 || mixed.MaximumPayloadBytes > maximumPayloadBytes || mixed.MaximumFields is < 0 or > 4096 ||
                    mixed.MaximumScalarUpdates is < 0 or > 4096 || mixed.MaximumWoundsCommands is < 0 or > 4096 ||
                    mixed.MaximumWoundsValues is < 0 or > 262144 || mixed.MaximumWoundsElements is < 0 or > 262144)
                    throw new ArgumentOutOfRangeException(nameof(replicationPolicy));
            }
            if (replicationPolicy.Mode == SceneWorldReplicationMode.CreatureView3MixedComponents)
            {
                ArgumentNullException.ThrowIfNull(replicationPolicy.ComponentsLimits); ArgumentNullException.ThrowIfNull(replicationPolicy.ComponentsPolicy);
                var c = replicationPolicy.ComponentsLimits; var p = replicationPolicy.ComponentsPolicy;
                if (c.MaximumPayloadBytes < 2 || c.MaximumPayloadBytes > maximumPayloadBytes || c.MaximumFields is < 0 or > 4096 ||
                    c.MaximumCommands is < 0 or > 4096 || c.MaximumKeys is < 0 or > 262144 || c.MaximumElements is < 0 or > 262144 ||
                    p.MaximumRawKeys is < 0 or > 262144 || p.MaximumElements != c.MaximumElements) throw new ArgumentOutOfRangeException(nameof(replicationPolicy));
            }
            this.replicationPolicy = replicationPolicy with { BaselineLimits = limits with { }, MixedLimits = replicationPolicy.MixedLimits is { } ml ? ml with { } : null,
                ComponentsLimits = replicationPolicy.ComponentsLimits is { } cl ? cl with { } : null, ComponentsPolicy = replicationPolicy.ComponentsPolicy is { } cp ? cp with { } : null };
            if (replicationPolicy.Mode == SceneWorldReplicationMode.CreatureView3MixedComponents)
            {
                var mixed = this.replicationPolicy.MixedLimits!; var components = this.replicationPolicy.ComponentsLimits!;
                compositeLimits = new(Math.Min(mixed.MaximumPayloadBytes, components.MaximumPayloadBytes), Math.Min(mixed.MaximumFields, components.MaximumFields),
                    mixed.MaximumScalarUpdates, mixed.MaximumWoundsCommands, mixed.MaximumWoundsValues, mixed.MaximumWoundsElements,
                    components.MaximumCommands, components.MaximumKeys, components.MaximumElements);
            }
        }
        transition = new(maximumStagedMessages, maximumStagedBytes); registry = new(maximumObjects, 0);
        if (ReplicationMode is SceneWorldReplicationMode.CreatureView3Scalars or SceneWorldReplicationMode.CreatureView3Mixed or SceneWorldReplicationMode.CreatureView3MixedComponents)
            replication = new(registry, maximumObjects, this.replicationPolicy!.MaximumDecodedBytes, revisionSeed:this.replicationPolicy.RevisionSeed, mixedPolicy:
                ReplicationMode is SceneWorldReplicationMode.CreatureView3Mixed or SceneWorldReplicationMode.CreatureView3MixedComponents ? new(this.replicationPolicy.WoundsPolicy, this.replicationPolicy.MixedLimits!.MaximumWoundsElements) : null,
                componentsPolicy: ReplicationMode == SceneWorldReplicationMode.CreatureView3MixedComponents ? this.replicationPolicy.ComponentsPolicy : null);
        if(tangibleReplicationPolicy is {} tp)
        {
            ArgumentNullException.ThrowIfNull(tp.BaselineLimits);ArgumentNullException.ThrowIfNull(tp.DeltaLimits);ArgumentNullException.ThrowIfNull(tp.StoreLimits);
            var bl=tp.BaselineLimits;var dl=tp.DeltaLimits;
            if(bl.MaximumPayloadBytes is <2||bl.MaximumPayloadBytes>maximumPayloadBytes||bl.MaximumStringBytes is <0 or >1048576||bl.MaximumUnicodeUnits is <0 or >524288||bl.MaximumContainerElements is <0 or >262144||bl.MaximumDecodedBytes is <0 or >134217728||
                dl.MaximumPayloadBytes is <2||dl.MaximumPayloadBytes>maximumPayloadBytes||dl.MaximumFields is <0 or >4096||dl.MaximumTangibleUpdates is <0 or >4096||dl.MaximumRawUpdates is <0 or >4096||dl.MaximumStringBytes is <0 or >1048576||dl.MaximumDecodedBytes is <0 or >134217728||
                dl.MaximumComponentCommands is <0 or >65536||dl.MaximumComponentKeys is <0 or >65536||dl.MaximumComponentElements is <0 or >65536||dl.MaximumNameUpdates is <0 or >4096||dl.MaximumNameStringBytes is <0 or >65536||dl.MaximumUnicodeUnits is <0 or >65536||dl.MaximumComponentElements!=tp.StoreLimits.MaximumComponentsElements)
                throw new ArgumentOutOfRangeException(nameof(tangibleReplicationPolicy));
            tangiblePolicy=tp with {BaselineLimits=tp.BaselineLimits with {},DeltaLimits=tp.DeltaLimits with {},StoreLimits=tp.StoreLimits with {}};
            tangible=new(registry,tangiblePolicy.StoreLimits,tp.RevisionSeed);
        }
        this.zeroPolicy = zeroPolicy; this.maximumOutstandingJobs = maximumOutstandingJobs; this.maximumEnvelopes = maximumEnvelopes;
        this.maximumEnvelopeBytes = maximumEnvelopeBytes; this.maximumPayloadBytes = maximumPayloadBytes;
    }
    public SceneWorldSelection BeginSelection(ulong objectId)
    {
        Enter(true); try { EnsureOpen(); byte[] select = ZoneSceneProtocolCodec.EncodeSelectCharacter(objectId); long generation = transition.BeginSelection(objectId); ClearWorld(); return new(generation, select); }
        finally { busy = false; }
    }
    public bool Accept(long generation, ReadOnlySpan<byte> application)
    {
        Enter(true); try
        {
            EnsureOpen(); if (generation != Generation) return false;
            bool typedAttempt=tangible is not null&&application.Length>=ReplicationEnvelopeCodec.MinimumMessageLength&&BinaryPrimitives.ReadUInt32LittleEndian(application[14..])==TangibleView3BaselineCodec.RawTag&&application[18]==3&&
                (BinaryPrimitives.ReadUInt32LittleEndian(application[2..]) is ReplicationEnvelopeCodec.BaselineOpcode or ReplicationEnvelopeCodec.DeltaOpcode)&&State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready;
            try
            {
                object value = Decode(application);
                if (value is CmdStartSceneMessage start) return transition.AcceptStart(generation, start);
                if (value is Ready) return transition.AcceptServerReady(generation, application);
                if (State == ZoneSceneTransitionState.Initializing) return transition.Stage(generation, application);
                Require(State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready, "World lifecycle outside active scene.");
                Apply(value); return true;
            }
            catch { if(!typedAttempt)Fail(); throw; }
        } finally { busy = false; }
    }
    /// <summary>Atomic frozen field4/six-scalar application; rejected preparations leave world/source semantics unchanged.</summary>
    public bool ApplyCustomizationScalars(long generation,CreatureView3CustomizationScalarDeltaPlan plan,SceneWorldCustomizationStateStore rawStore)
    {
        Enter(true);SceneCreatureReplicationStore.PreparedScalars? typed=null;
        SceneWorldCustomizationStateStore.PreparedWorldCustomization? raw=null;
        try
        {
            EnsureOpen();if(generation!=Generation)return false;
            ArgumentNullException.ThrowIfNull(plan);ArgumentNullException.ThrowIfNull(rawStore);
            Require(State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready,"Active customization world required.");
            var target=registry.Find(plan.ObjectId);
            var state=target is not null?replication?.InspectCurrent(target.Incarnation):null;
            var latest=target is not null?new SceneWorldObjectSnapshot(target,placements[plan.ObjectId],Array.AsReadOnly(envelopes.GetValueOrDefault(plan.ObjectId)?.ToArray()??Array.Empty<ReplicationEnvelope>()))
                {Replication=state is not null?new(identity,Generation,state):null,TangibleReplication=TangibleFor(plan.ObjectId)}:null;
            Require(latest?.Object.Phase==SceneObjectPhase.Finalized && latest.Replication is not null,"Finalized complete creature target required.");
            RequireCreature(plan.ObjectId);
            var scalars=plan.Updates.Where(u=>u.Scalar is not null).Select(u=>u.Scalar!).ToArray();
            var strings=plan.Updates.Where(u=>u.Customization is not null).Select(u=>u.Customization!).ToArray();
            Require(scalars.Length<=replicationPolicy!.MaximumScalarUpdates,"Scalar update budget exhausted.");
            if(replicationPolicy.MixedLimits is {} limits)Require(plan.Updates.Count<=limits.MaximumFields && scalars.Length<=limits.MaximumScalarUpdates,"Mixed field/scalar budget exhausted.");
            if(replicationPolicy.ComponentsLimits is {} components)Require(plan.Updates.Count<=components.MaximumFields,"Components field budget exhausted.");
            ulong next=checked(mutationRevision+1);
            Require(changes.ContainsKey(plan.ObjectId) || changes.Count<maximumPendingChanges,"Undelivered world change capacity exhausted.");
            var stagedChanges=new Dictionary<ulong,ChangeSlot>(changes);
            stagedChanges[plan.ObjectId]=changes.TryGetValue(plan.ObjectId,out var slot)?slot with {Kind=slot.Kind|SceneWorldChangeKind.Replication}:new(SceneWorldChangeKind.Replication,next);
            var current=registry.Snapshots().Where(o=>o.Phase==SceneObjectPhase.Finalized && replication!.InspectCurrent(o.Incarnation) is not null).Select(o=>o.Incarnation).ToHashSet();
            raw=rawStore.PrepareWorldCustomization(this,latest!,strings,current);
            if(scalars.Length>0)typed=replication!.PrepareScalars(latest!.Replication!.State,scalars);
            // Everything below is owner-held assignment of prebuilt state: no callbacks, parsing, allocations or checked increments.
            if(typed is not null)replication!.CommitScalars(typed);
            rawStore.CommitWorldCustomization(raw);changes=stagedChanges;mutationRevision=next;return true;
        }
        finally
        {
            if(typed is not null)replication!.ReleaseScalars(typed);
            if(raw is not null)rawStore.ReleaseWorldCustomization(raw);
            busy=false;
        }
    }
    /// <summary>Atomic frozen full creature application; rejected preparations leave world/source semantics unchanged.</summary>
    public bool ApplyFullCreatureDelta(long generation,CreatureView3FullDeltaPlan plan,SceneWorldCustomizationStateStore rawStore)
    {
        Enter(true);SceneCreatureReplicationStore.PreparedScalars? typed=null;
        SceneWorldCustomizationStateStore.PreparedWorldCustomization? raw=null;
        try
        {
            EnsureOpen();if(generation!=Generation)return false;
            ArgumentNullException.ThrowIfNull(plan);ArgumentNullException.ThrowIfNull(rawStore);
            Require(State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready,"Active customization world required.");
            var target=registry.Find(plan.ObjectId);
            var state=target is not null?replication?.InspectCurrent(target.Incarnation):null;
            var latest=target is not null?new SceneWorldObjectSnapshot(target,placements[plan.ObjectId],Array.AsReadOnly(envelopes.GetValueOrDefault(plan.ObjectId)?.ToArray()??Array.Empty<ReplicationEnvelope>()))
                {Replication=state is not null?new(identity,Generation,state):null,TangibleReplication=TangibleFor(plan.ObjectId)}:null;
            Require(latest?.Object.Phase==SceneObjectPhase.Finalized && latest.Replication is not null,"Finalized complete creature target required.");
            RequireCreature(plan.ObjectId);
            var scalars=plan.Updates.Where(u=>u.Scalar is not null).Select(u=>u.Scalar!).ToArray();
            var strings=plan.Updates.Where(u=>u.Customization is not null).Select(u=>u.Customization!).ToArray();
            Require(scalars.Length<=replicationPolicy!.MaximumScalarUpdates,"Scalar update budget exhausted.");
            if(replicationPolicy.MixedLimits is {} limits)Require(plan.Updates.Count<=limits.MaximumFields && scalars.Length<=limits.MaximumScalarUpdates,"Mixed field/scalar budget exhausted.");
            if(replicationPolicy.ComponentsLimits is {} components)Require(plan.Updates.Count<=components.MaximumFields,"Components field budget exhausted.");
            var wounds=plan.Updates.Where(u=>u.Wounds is not null).Select(u=>u.Wounds!).ToArray();
            var componentsUpdates=plan.Updates.Where(u=>u.Components is not null).Select(u=>u.Components!).ToArray();
            if(wounds.Length>0)
            {
                Require(replicationPolicy.MixedLimits is not null,"Wounds-enabled world required.");var ml=replicationPolicy.MixedLimits!;
                Require(wounds.Sum(u=>(long)u.CommandCount)<=ml.MaximumWoundsCommands && wounds.Sum(u=>u.Commands.Sum(c=>(long)c.Values.Count))<=ml.MaximumWoundsValues,"Wounds aggregate command/value capacity.");
            }
            if(componentsUpdates.Length>0)
            {
                Require(replicationPolicy.ComponentsLimits is not null,"Components-enabled world required.");var cl=replicationPolicy.ComponentsLimits!;
                Require(componentsUpdates.Sum(u=>(long)u.Commands.Count)<=cl.MaximumCommands && componentsUpdates.Sum(u=>u.Commands.LongCount(c=>c.Kind!=CreatureComponentsCommandKind.Clear))<=cl.MaximumKeys,"Component aggregate command/key capacity.");
            }
            ulong next=checked(mutationRevision+1);
            Require(changes.ContainsKey(plan.ObjectId) || changes.Count<maximumPendingChanges,"Undelivered world change capacity exhausted.");
            var stagedChanges=new Dictionary<ulong,ChangeSlot>(changes);
            stagedChanges[plan.ObjectId]=changes.TryGetValue(plan.ObjectId,out var slot)?slot with {Kind=slot.Kind|SceneWorldChangeKind.Replication}:new(SceneWorldChangeKind.Replication,next);
            var current=registry.Snapshots().Where(o=>o.Phase==SceneObjectPhase.Finalized && replication!.InspectCurrent(o.Incarnation) is not null).Select(o=>o.Incarnation).ToHashSet();
            raw=rawStore.PrepareWorldCustomization(this,latest!,strings,current);
            if(scalars.Length+wounds.Length+componentsUpdates.Length>0)typed=replication!.PrepareFull(latest!.Replication!.State,plan);
            // Everything below is owner-held assignment of prebuilt state: no callbacks, parsing, allocations or checked increments.
            if(typed is not null)replication!.CommitScalars(typed);
            rawStore.CommitWorldCustomization(raw);changes=stagedChanges;mutationRevision=next;return true;
        }
        finally
        {
            if(typed is not null)replication!.ReleaseScalars(typed);
            if(raw is not null)rawStore.ReleaseWorldCustomization(raw);
            busy=false;
        }
    }
    /// <summary>Atomic frozen extended creature application; rejected preparations leave world/source semantics unchanged.</summary>
    public bool ApplyExtendedCreatureDelta(long generation,CreatureView3ExtendedDeltaPlan plan,SceneWorldCustomizationStateStore rawStore)
    {
        Enter(true);SceneCreatureReplicationStore.PreparedScalars? typed=null;
        SceneWorldCustomizationStateStore.PreparedWorldCustomization? raw=null;
        try
        {
            EnsureOpen();if(generation!=Generation)return false;
            ArgumentNullException.ThrowIfNull(plan);ArgumentNullException.ThrowIfNull(rawStore);
            Require(State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready,"Active customization world required.");
            var target=registry.Find(plan.ObjectId);
            var state=target is not null?replication?.InspectCurrent(target.Incarnation):null;
            var latest=target is not null?new SceneWorldObjectSnapshot(target,placements[plan.ObjectId],Array.AsReadOnly(envelopes.GetValueOrDefault(plan.ObjectId)?.ToArray()??Array.Empty<ReplicationEnvelope>()))
                {Replication=state is not null?new(identity,Generation,state):null,TangibleReplication=TangibleFor(plan.ObjectId)}:null;
            Require(latest?.Object.Phase==SceneObjectPhase.Finalized && latest.Replication is not null,"Finalized complete creature target required.");
            RequireCreature(plan.ObjectId);
            var scalars=plan.Updates.Count(u=>u.Tangible is not null || u.Full?.Scalar is not null);
            var strings=plan.Updates.Where(u=>u.Full?.Customization is not null).Select(u=>u.Full!.Customization!).ToArray();
            Require(scalars<=replicationPolicy!.MaximumScalarUpdates,"Scalar update budget exhausted.");
            if(replicationPolicy.MixedLimits is {} limits)Require(plan.Updates.Count<=limits.MaximumFields && scalars<=limits.MaximumScalarUpdates,"Mixed field/scalar budget exhausted.");
            if(replicationPolicy.ComponentsLimits is {} components)Require(plan.Updates.Count<=components.MaximumFields,"Components field budget exhausted.");
            var wounds=plan.Updates.Where(u=>u.Full?.Wounds is not null).Select(u=>u.Full!.Wounds!).ToArray();
            var componentsUpdates=plan.Updates.Where(u=>u.Full?.Components is not null).Select(u=>u.Full!.Components!).ToArray();
            if(wounds.Length>0)
            {
                Require(replicationPolicy.MixedLimits is not null,"Wounds-enabled world required.");var ml=replicationPolicy.MixedLimits!;
                Require(wounds.Sum(u=>(long)u.CommandCount)<=ml.MaximumWoundsCommands && wounds.Sum(u=>u.Commands.Sum(c=>(long)c.Values.Count))<=ml.MaximumWoundsValues,"Wounds aggregate command/value capacity.");
            }
            if(componentsUpdates.Length>0)
            {
                Require(replicationPolicy.ComponentsLimits is not null,"Components-enabled world required.");var cl=replicationPolicy.ComponentsLimits!;
                Require(componentsUpdates.Sum(u=>(long)u.Commands.Count)<=cl.MaximumCommands && componentsUpdates.Sum(u=>u.Commands.LongCount(c=>c.Kind!=CreatureComponentsCommandKind.Clear))<=cl.MaximumKeys,"Component aggregate command/key capacity.");
            }
            ulong next=checked(mutationRevision+1);
            Require(changes.ContainsKey(plan.ObjectId) || changes.Count<maximumPendingChanges,"Undelivered world change capacity exhausted.");
            var stagedChanges=new Dictionary<ulong,ChangeSlot>(changes);
            stagedChanges[plan.ObjectId]=changes.TryGetValue(plan.ObjectId,out var slot)?slot with {Kind=slot.Kind|SceneWorldChangeKind.Replication}:new(SceneWorldChangeKind.Replication,next);
            var current=registry.Snapshots().Where(o=>o.Phase==SceneObjectPhase.Finalized && replication!.InspectCurrent(o.Incarnation) is not null).Select(o=>o.Incarnation).ToHashSet();
            raw=rawStore.PrepareWorldCustomization(this,latest!,strings,current);
            if(scalars+wounds.Length+componentsUpdates.Length>0)typed=replication!.PrepareExtended(latest!.Replication!.State,plan);
            // Everything below is owner-held assignment of prebuilt state: no callbacks, parsing, allocations or checked increments.
            if(typed is not null)replication!.CommitScalars(typed);
            rawStore.CommitWorldCustomization(raw);changes=stagedChanges;mutationRevision=next;return true;
        }
        finally
        {
            if(typed is not null)replication!.ReleaseScalars(typed);
            if(raw is not null)rawStore.ReleaseWorldCustomization(raw);
            busy=false;
        }
    }
    /// <summary>Atomic frozen named creature application; rejected preparations leave world/source semantics unchanged.</summary>
    public bool ApplyNamedCreatureDelta(long generation,CreatureView3NamedDeltaPlan plan,SceneWorldCustomizationStateStore rawStore)
    {
        Enter(true);SceneCreatureReplicationStore.PreparedScalars? typed=null;
        SceneWorldCustomizationStateStore.PreparedWorldCustomization? raw=null;
        try
        {
            EnsureOpen();if(generation!=Generation)return false;
            ArgumentNullException.ThrowIfNull(plan);ArgumentNullException.ThrowIfNull(rawStore);
            Require(State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready,"Active customization world required.");
            var target=registry.Find(plan.ObjectId);
            var state=target is not null?replication?.InspectCurrent(target.Incarnation):null;
            var latest=target is not null?new SceneWorldObjectSnapshot(target,placements[plan.ObjectId],Array.AsReadOnly(envelopes.GetValueOrDefault(plan.ObjectId)?.ToArray()??Array.Empty<ReplicationEnvelope>()))
                {Replication=state is not null?new(identity,Generation,state):null,TangibleReplication=TangibleFor(plan.ObjectId)}:null;
            Require(latest?.Object.Phase==SceneObjectPhase.Finalized && latest.Replication is not null,"Finalized complete creature target required.");
            RequireCreature(plan.ObjectId);
            var scalars=plan.Updates.Count(u=>u.Name is not null || u.Extended?.Tangible is not null || u.Extended?.Full?.Scalar is not null);
            var strings=plan.Updates.Where(u=>u.Extended?.Full?.Customization is not null).Select(u=>u.Extended!.Full!.Customization!).ToArray();
            Require(scalars<=replicationPolicy!.MaximumScalarUpdates,"Scalar update budget exhausted.");
            if(replicationPolicy.MixedLimits is {} limits)Require(plan.Updates.Count<=limits.MaximumFields && scalars<=limits.MaximumScalarUpdates,"Mixed field/scalar budget exhausted.");
            if(replicationPolicy.ComponentsLimits is {} components)Require(plan.Updates.Count<=components.MaximumFields,"Components field budget exhausted.");
            var wounds=plan.Updates.Where(u=>u.Extended?.Full?.Wounds is not null).Select(u=>u.Extended!.Full!.Wounds!).ToArray();
            var componentsUpdates=plan.Updates.Where(u=>u.Extended?.Full?.Components is not null).Select(u=>u.Extended!.Full!.Components!).ToArray();
            if(wounds.Length>0)
            {
                Require(replicationPolicy.MixedLimits is not null,"Wounds-enabled world required.");var ml=replicationPolicy.MixedLimits!;
                Require(wounds.Sum(u=>(long)u.CommandCount)<=ml.MaximumWoundsCommands && wounds.Sum(u=>u.Commands.Sum(c=>(long)c.Values.Count))<=ml.MaximumWoundsValues,"Wounds aggregate command/value capacity.");
            }
            if(componentsUpdates.Length>0)
            {
                Require(replicationPolicy.ComponentsLimits is not null,"Components-enabled world required.");var cl=replicationPolicy.ComponentsLimits!;
                Require(componentsUpdates.Sum(u=>(long)u.Commands.Count)<=cl.MaximumCommands && componentsUpdates.Sum(u=>u.Commands.LongCount(c=>c.Kind!=CreatureComponentsCommandKind.Clear))<=cl.MaximumKeys,"Component aggregate command/key capacity.");
            }
            foreach(var update in plan.Updates.Where(u=>u.Name is not null).Select(u=>u.Name!))
                Require(update.Index==1?update.NameFile!.Length<=replicationPolicy.BaselineLimits.MaximumStringBytes && update.NameKey!.Length<=replicationPolicy.BaselineLimits.MaximumStringBytes:
                    update.CustomNameUnits!.Count<=replicationPolicy.BaselineLimits.MaximumUnicodeUnits,"Configured name string/unit capacity.");
            ulong next=checked(mutationRevision+1);
            Require(changes.ContainsKey(plan.ObjectId) || changes.Count<maximumPendingChanges,"Undelivered world change capacity exhausted.");
            var stagedChanges=new Dictionary<ulong,ChangeSlot>(changes);
            stagedChanges[plan.ObjectId]=changes.TryGetValue(plan.ObjectId,out var slot)?slot with {Kind=slot.Kind|SceneWorldChangeKind.Replication}:new(SceneWorldChangeKind.Replication,next);
            var current=registry.Snapshots().Where(o=>o.Phase==SceneObjectPhase.Finalized && replication!.InspectCurrent(o.Incarnation) is not null).Select(o=>o.Incarnation).ToHashSet();
            raw=rawStore.PrepareWorldCustomization(this,latest!,strings,current);
            if(scalars+wounds.Length+componentsUpdates.Length>0)typed=replication!.PrepareNamed(latest!.Replication!.State,plan);
            // Everything below is owner-held assignment of prebuilt state: no callbacks, parsing, allocations or checked increments.
            if(typed is not null)replication!.CommitScalars(typed);
            rawStore.CommitWorldCustomization(raw);changes=stagedChanges;mutationRevision=next;return true;
        }
        finally
        {
            if(typed is not null)replication!.ReleaseScalars(typed);
            if(raw is not null)rawStore.ReleaseWorldCustomization(raw);
            busy=false;
        }
    }
    public byte[]? CompleteMinimumSceneBarrier(long generation, bool callerBarrierSatisfied)
    {
        Enter(true); try
        {
            EnsureOpen(); if (generation != Generation || !callerBarrierSatisfied || State != ZoneSceneTransitionState.Initializing) return null;
            try
            {
                var publication = transition.CompleteMinimumResources(generation)!;
                foreach (var payload in publication.CopyStagedPayloads()) Apply(Decode(payload));
                return publication.CopyReadyMessage(); // Only after ordered replay succeeds; loads start on later Poll.
            }
            catch { Fail(); throw; }
        } finally { busy = false; }
    }
    public bool RestartResource(long generation, ulong objectId)
    {
        Enter(true); try
        {
            EnsureOpen(); if (generation != Generation) return false;
            try
            {
                Require(State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready, "Restart outside active world.");
                var snapshot = registry.Find(objectId); Require(snapshot?.Phase == SceneObjectPhase.Finalized, "Finalized object required.");
                CancelResource(objectId);
                Queue(snapshot!, true); return true;
            }
            catch { Fail(); throw; }
        } finally { busy = false; }
    }
    public SceneWorldResourcePoll PollResources()
    {
        Enter(true); try
        {
            EnsureOpen(); if (initializer is null) throw new InvalidOperationException("Generic world requires PollObjectResources.");
            var publications = new List<ScenePreparedCreaturePublication>();
            var retired = new List<SceneCreatureResourceCompletion>();
            try
            {
                var completions = initializer.Poll();
                foreach (var completion in completions)
                {
                    bool current = initializer.IsCurrent(completion.Ticket) && registry.Find(completion.Ticket.Handle.ObjectId)?.Phase == SceneObjectPhase.Finalized;
                    bool restarting = pending.Any(p => p.Handle.ObjectId == completion.Ticket.Handle.ObjectId && p.Restart);
                    if (!current || restarting || State is not (ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready)) { retired.Add(completion with { Resources = null }); continue; }
                    if (completion.Kind != SceneCreatureResourceCompletionKind.Prepared || completion.Resources is null) throw new InvalidDataException("Current resource preparation failed.", completion.Error);
                    publications.Add(new(identity, Generation, completion.Ticket, placements[completion.Ticket.Handle.ObjectId], completion.Resources!));
                }
                StartPending();
                return new(Array.AsReadOnly(publications.ToArray()), Array.AsReadOnly(retired.ToArray()));
            }
            catch { Fail(); throw; }
        } finally { busy = false; }
    }
    public SceneWorldObjectResourcePoll PollObjectResources()
    {
        Enter(true); try
        {
            EnsureOpen(); if (objectInitializer is null) throw new InvalidOperationException("Legacy world requires PollResources.");
            var publications = new List<ScenePreparedObjectPublication>(); var retired = new List<SceneObjectResourceCompletion>();
            try
            {
                foreach (var completion in objectInitializer.Poll())
                {
                    bool current = objectInitializer.IsCurrent(completion.Ticket) && registry.Find(completion.Ticket.Handle.ObjectId)?.Phase == SceneObjectPhase.Finalized;
                    bool restarting = pending.Any(p => p.Handle.ObjectId == completion.Ticket.Handle.ObjectId && p.Restart);
                    if (!current || restarting || State is not (ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready)) { retired.Add(completion with { Resources = null }); continue; }
                    if (completion.Kind != SceneObjectResourceCompletionKind.Prepared || completion.Resources is null) throw new InvalidDataException("Current object resource preparation failed.", completion.Error);
                    publications.Add(new(identity, Generation, completion.Ticket, placements[completion.Ticket.Handle.ObjectId], completion.Resources));
                }
                StartPending(); return new(Array.AsReadOnly(publications.ToArray()), Array.AsReadOnly(retired.ToArray()));
            }
            catch { Fail(); throw; }
        } finally { busy = false; }
    }
    public bool IsCurrent(ScenePreparedObjectPublication publication)
    {
        Enter(); try
        {
            ArgumentNullException.ThrowIfNull(publication);
            return !closed && ReferenceEquals(publication.Owner, identity) && publication.Generation == Generation &&
                State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready &&
                !pending.Any(p => p.Handle.ObjectId == publication.Ticket.Handle.ObjectId && p.Restart) &&
                objectInitializer?.IsCurrent(publication.Ticket) == true && registry.Find(publication.Ticket.Handle.ObjectId)?.Phase == SceneObjectPhase.Finalized;
        } finally { busy = false; }
    }
    private SceneObjectHandle? FindResourceHandle(ulong id) => objectInitializer is not null ? objectInitializer.Find(id)?.Handle : initializer!.Find(id)?.Handle;
    private void CancelResource(ulong id)
    {
        if (objectInitializer is not null) { if (objectInitializer.Find(id) is { } ticket) objectInitializer.Cancel(ticket); }
        else if (initializer!.Find(id) is { } ticket) initializer.Cancel(ticket);
    }
    private void StartPending()
    {
        while (pending.Count > 0 && OutstandingJobs < maximumOutstandingJobs)
        {
            var item = pending[0]; pending.RemoveAt(0);
            if (!registry.IsCurrent(item.Handle) || registry.Find(item.Handle.ObjectId) is not { Phase: SceneObjectPhase.Finalized } snapshot) continue;
            if (objectInitializer is not null) objectInitializer.Start(new(snapshot, catalog, objectOptions![catalog.Require(snapshot.Create.TemplateCrc).Recipe]), item.Restart);
            else initializer!.Start(new(snapshot, catalog, configuration!.Idle, configuration.Movement, configuration.MaximumResources, configuration.MaximumSourceBytes), item.Restart);
            started.Add(snapshot.Handle.ObjectId);
        }
    }
    private void RequireRecipe(SceneCreateObjectByCrcMessage create)
    {
        var recipe = catalog.Require(create.TemplateCrc).Recipe;
        Require(objectOptions is not null ? objectOptions.ContainsKey(recipe) : recipe == SceneObjectResourceRecipe.CreatureDetail0, "Unsupported scene object resource recipe/options.");
    }
    public bool IsCurrent(ScenePreparedCreaturePublication publication)
    {
        Enter(); try
        {
            ArgumentNullException.ThrowIfNull(publication);
            return !closed && ReferenceEquals(publication.Owner, identity) && publication.Generation == Generation &&
                State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready &&
                !pending.Any(p => p.Handle.ObjectId == publication.Ticket.Handle.ObjectId && p.Restart) &&
                initializer?.IsCurrent(publication.Ticket) == true && registry.Find(publication.Ticket.Handle.ObjectId)?.Phase == SceneObjectPhase.Finalized;
        } finally { busy = false; }
    }
    public SceneWorldChangeBatch? DrainChanges(long generation)
    {
        Enter(); try
        {
            if (closed || generation != Generation || State is not (ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready)) return null;
            var entries = changes.OrderBy(row => row.Value.FirstRevision).ThenBy(row => row.Key)
                .Select(row => new SceneWorldChangeEntry(row.Key, row.Value.Kind, ObjectFor(row.Key))).ToArray();
            return new(identity, Generation, mutationRevision, entries);
        } finally { busy = false; }
    }
    public bool IsCurrent(SceneWorldChangeBatch batch)
    {
        Enter(); try { ArgumentNullException.ThrowIfNull(batch); return CurrentBatch(batch); }
        finally { busy = false; }
    }
    public bool AcknowledgeChanges(SceneWorldChangeBatch batch)
    {
        Enter(true); try
        {
            ArgumentNullException.ThrowIfNull(batch); if (!CurrentBatch(batch)) return false;
            changes.Clear(); return true;
        } finally { busy = false; }
    }
    private bool CurrentBatch(SceneWorldChangeBatch batch) => !closed && ReferenceEquals(batch.Owner, identity) &&
        batch.Generation == Generation && batch.MutationRevision == mutationRevision &&
        State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready;
    private SceneWorldObjectSnapshot? ObjectFor(ulong objectId)
    {
        var snapshot = registry.Find(objectId);
        return snapshot is null ? null : new(snapshot, placements[objectId], Array.AsReadOnly(envelopes.GetValueOrDefault(objectId)?.ToArray() ?? Array.Empty<ReplicationEnvelope>()))
            { Replication = ReplicationFor(objectId),TangibleReplication=TangibleFor(objectId) };
    }
    public SceneWorldReplicationSnapshot? FindReplication(long generation, ulong objectId)
    {
        Enter(); try { return !closed && generation == Generation ? ReplicationFor(objectId) : null; }
        finally { busy = false; }
    }
    public bool IsCurrent(SceneWorldReplicationSnapshot snapshot)
    {
        Enter(); try
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            return !closed && ReferenceEquals(snapshot.Owner, identity) && snapshot.Generation == Generation &&
                State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready &&
                replication?.IsCurrent(snapshot.State) == true;
        } finally { busy = false; }
    }
    private SceneWorldReplicationSnapshot? ReplicationFor(ulong objectId)
    {
        var incarnation = registry.Find(objectId)?.Incarnation;
        return incarnation is { } token && replication?.Find(token) is { } state ? new(identity, Generation, state) : null;
    }
    private SceneWorldTangibleReplicationSnapshot? TangibleFor(ulong id)
    {
        var incarnation=registry.Find(id)?.Incarnation;
        return incarnation is {} token && tangible?.Find(token) is {} state?new(identity,Generation,state):null;
    }
    public SceneWorldTangibleReplicationSnapshot? FindTangibleReplication(long generation,ulong id)
    {Enter();try{return !closed&&generation==Generation?TangibleFor(id):null;}finally{busy=false;}}
    public bool IsCurrent(SceneWorldTangibleReplicationSnapshot snapshot)
    {Enter();try{ArgumentNullException.ThrowIfNull(snapshot);return !closed&&ReferenceEquals(snapshot.Owner,identity)&&snapshot.Generation==Generation&&State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready&&tangible?.IsCurrent(snapshot.State)==true;}finally{busy=false;}}
    private void ApplyTangibleCore(ulong id,TangibleView3BaselineSnapshot? baseline,TangibleView3DeltaPlan? plan)
    {
        Require(tangible is not null&&objectOptions is not null,"Typed tangible generic world required.");
        var target=registry.Find(id);
        Require(target is not null&&catalog.Require(target.Create.TemplateCrc).Recipe==SceneObjectResourceRecipe.StaticAppearance,"Static tangible recipe required.");
        if(plan is not null)Require(target!.Phase==SceneObjectPhase.Finalized,"Finalized tangible target required.");
        ulong next=checked(mutationRevision+1);
        Require(changes.ContainsKey(id)||changes.Count<maximumPendingChanges,"Undelivered world change capacity exhausted.");
        var staged=new Dictionary<ulong,ChangeSlot>(changes);
        staged[id]=changes.TryGetValue(id,out var slot)?slot with {Kind=slot.Kind|SceneWorldChangeKind.Replication}:new(SceneWorldChangeKind.Replication,next);
        SceneTangibleReplicationStore.Prepared? prepared=null;
        try
        {
            prepared=baseline is not null?tangible!.PrepareSeed(target!.Incarnation,baseline):tangible!.PrepareApply(target!.Incarnation,plan!);
            Require(prepared is not null,"Stale tangible preparation.");
            // Owner-held prebuilt assignment only. All validation/arithmetic/allocation precedes this point.
            tangible!.CommitPrepared(prepared!);changes=staged;mutationRevision=next;
        }
        finally{if(prepared is not null)tangible!.ReleasePrepared(prepared);}
    }
    internal sealed record TangibleIngressStamp(long Generation,ZoneSceneTransitionState State,ulong Revision,ulong CallbackVersion,
        SceneWorldChangeEntry[] Changes,SceneWorldObjectSnapshot[] Objects);
    internal TangibleIngressStamp CaptureTangibleIngress()
    {
        Enter();try{EnsureOpen();return new(Generation,State,mutationRevision,callbackVersion,
            changes.OrderBy(p=>p.Key).Select(p=>new SceneWorldChangeEntry(p.Key,p.Value.Kind,null)).ToArray(),
            registry.Snapshots().Select(o=>ObjectFor(o.Handle.ObjectId)!).ToArray());}finally{busy=false;}
    }
    internal bool MatchesTangibleIngress(TangibleIngressStamp stamp)
    {
        Enter();try
        {
            if(closed||stamp.CallbackVersion!=callbackVersion||stamp.Generation!=Generation||stamp.State!=State||stamp.Revision!=mutationRevision||stamp.Changes.Length!=changes.Count)return false;
            if(stamp.Changes.Any(c=>!changes.TryGetValue(c.ObjectId,out var slot)||slot.Kind!=c.Kind))return false;
            var now=registry.Snapshots();if(now.Count!=stamp.Objects.Length)return false;
            return stamp.Objects.All(o=>ReferenceEquals(registry.Find(o.Object.Handle.ObjectId),o.Object)&&
                ReplicationFor(o.Object.Handle.ObjectId)?.Revision==o.Replication?.Revision&&TangibleFor(o.Object.Handle.ObjectId)?.Revision==o.TangibleReplication?.Revision);
        }finally{busy=false;}
    }
    internal TangibleView3DeltaPlan ParseTangibleDeltaEnvelope(ReplicationEnvelope envelope)
    {Enter();try{EnsureOpen();Require(tangiblePolicy is not null,"Typed tangible policy required.");return TangibleView3DeltaCodec.Decode(envelope,tangiblePolicy!.DeltaLimits);}finally{busy=false;}}
    internal TangibleView3DeltaPlan DecodeTangibleDelta(ReplicationEnvelope envelope,TangibleView3DeltaPlan? parsed=null)
    {
        Enter();try
        {
            EnsureOpen();Require(tangiblePolicy is not null,"Typed tangible policy required.");
            var grammar=parsed??TangibleView3DeltaCodec.Decode(envelope,tangiblePolicy!.DeltaLimits);
            var plan=tangiblePolicy!.DeltaDecoder is {} decoder?decoder(envelope):grammar;
            if(reentered)throw new InvalidOperationException("Tangible decoder reentered world.");
            Require(plan is not null&&plan.ObjectId==envelope.ObjectId&&plan.DeclaredCount==envelope.DeclaredCount,"Decoded tangible identity mismatch.");return plan!;
        }finally{busy=false;}
    }
    public bool ApplyTangibleDelta(long generation,TangibleView3DeltaPlan plan)
    {
        Enter(true);try
        {
            EnsureOpen();if(generation!=Generation)return false;ArgumentNullException.ThrowIfNull(plan);
            Require(State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready,"Active tangible world required.");
            ApplyTangibleCore(plan.ObjectId,null,plan);return true;
        }finally{busy=false;}
    }
    public IReadOnlyList<SceneWorldObjectSnapshot> Snapshots()
    {
        Enter(); try { return Array.AsReadOnly(registry.Snapshots().Select(s => new SceneWorldObjectSnapshot(s, placements[s.Handle.ObjectId], Array.AsReadOnly(envelopes.GetValueOrDefault(s.Handle.ObjectId)?.ToArray() ?? Array.Empty<ReplicationEnvelope>())) { Replication = ReplicationFor(s.Handle.ObjectId),TangibleReplication=TangibleFor(s.Handle.ObjectId) }).ToArray()); }
        finally { busy = false; }
    }
    public void Close()
    {
        Enter(true); try { if (closed) return; transition.Close(); closed = true; registry.Close(); tangible?.Close(); replication?.Close(); changes.Clear(); placements.Clear(); envelopes.Clear(); pending.Clear(); started.Clear(); initializer?.Dispose(); objectInitializer?.Dispose(); }
        finally { busy = false; }
    }
    public void Dispose() => Close();
    private sealed record Ready;
    private object Decode(ReadOnlySpan<byte> message)
    {
        Require(message.Length >= 6, "Application discriminator truncated."); uint opcode = BinaryPrimitives.ReadUInt32LittleEndian(message[2..]);
        return opcode switch
        {
            ZoneSceneProtocolCodec.CmdStartSceneOpcode => ZoneSceneProtocolCodec.DecodeCmdStartScene(message),
            SceneObjectLifecycleCodec.CreateOpcode => DecodeCreate(message),
            SceneObjectLifecycleCodec.DestroyOpcode => SceneObjectLifecycleCodec.DecodeDestroy(message),
            SceneObjectLifecycleCodec.ContainmentOpcode => SceneObjectLifecycleCodec.DecodeContainment(message),
            SceneObjectLifecycleCodec.EndBaselinesOpcode => SceneObjectLifecycleCodec.DecodeEndBaselines(message),
            ReplicationEnvelopeCodec.BaselineOpcode => objectOptions is not null ? DecodeObjectReplication(message, true) : replication is null ? (object)ReplicationEnvelopeCodec.DecodeBaseline(message, maximumPayloadBytes) :
                CreatureView3BaselineCodec.Decode(message, replicationPolicy!.BaselineLimits),
            ReplicationEnvelopeCodec.DeltaOpcode => objectOptions is not null ? DecodeObjectReplication(message, false) : ReplicationMode == SceneWorldReplicationMode.CreatureView3MixedComponents ? CreatureView3CompositeDeltaCodec.Decode(message, compositeLimits!, replicationPolicy!.WoundsPolicy) : replication is null ? (object)ReplicationEnvelopeCodec.DecodeDelta(message, maximumPayloadBytes) :
                ReplicationMode == SceneWorldReplicationMode.CreatureView3Mixed ? CreatureView3MixedDeltaCodec.Decode(message, replicationPolicy!.MixedLimits!, replicationPolicy.WoundsPolicy) :
                CreatureView3ScalarDeltaCodec.Decode(message, maximumPayloadBytes, replicationPolicy!.MaximumScalarUpdates),
            SceneReadyProtocolCodec.Opcode => DecodeReady(message),
            _ => throw new InvalidDataException("Unknown world application opcode; generation rejected.")
        };
    }
    private sealed record ObjectReplication(ReplicationEnvelope Envelope, byte[] Message);
    private object DecodeObjectReplication(ReadOnlySpan<byte> message, bool baseline) => new ObjectReplication(
        baseline ? ReplicationEnvelopeCodec.DecodeBaseline(message, maximumPayloadBytes) : ReplicationEnvelopeCodec.DecodeDelta(message, maximumPayloadBytes), message.ToArray());
    private object ResolveObjectReplication(ObjectReplication value)
    {
        var envelope = value.Envelope; var snapshot = registry.Find(envelope.ObjectId);
        Require(snapshot is not null, "Replication for missing object.");
        bool creature = catalog.Require(snapshot!.Create.TemplateCrc).Recipe == SceneObjectResourceRecipe.CreatureDetail0;
        Require(creature ? envelope.RawTag == CreatureView3BaselineCodec.RawTag : envelope.RawTag == 0x54414E4F, "Replication family/recipe disagreement.");
        if(!creature && tangible is not null && envelope.View==3)
        {
            object grammar=envelope.Kind==ReplicationEnvelopeKind.Baseline?
                TangibleView3BaselineCodec.Decode(envelope,tangiblePolicy!.BaselineLimits):
                (object)TangibleView3DeltaCodec.Decode(envelope,tangiblePolicy!.DeltaLimits);
            object decoded=envelope.Kind==ReplicationEnvelopeKind.Baseline?
                (tangiblePolicy!.BaselineDecoder is {} bd?bd(envelope):grammar):
                (tangiblePolicy!.DeltaDecoder is {} dd?dd(envelope):grammar);
            if(reentered)throw new InvalidOperationException("Tangible decoder reentered world.");
            Require(decoded is not null,"Null tangible decoder result.");
            if(decoded is TangibleView3BaselineSnapshot tb)Require(tb.ObjectId==envelope.ObjectId&&tb.DeclaredCount==envelope.DeclaredCount,"Tangible decoder baseline identity mismatch.");
            if(decoded is TangibleView3DeltaPlan td)Require(td.ObjectId==envelope.ObjectId&&td.DeclaredCount==envelope.DeclaredCount,"Tangible decoder plan identity mismatch.");
            return decoded!;
        }
        if (!creature || replication is null) return envelope;
        if (envelope.Kind == ReplicationEnvelopeKind.Baseline) return CreatureView3BaselineCodec.Decode(value.Message, replicationPolicy!.BaselineLimits);
        return ReplicationMode switch
        {
            SceneWorldReplicationMode.CreatureView3MixedComponents => CreatureView3CompositeDeltaCodec.Decode(envelope, compositeLimits!, replicationPolicy!.WoundsPolicy),
            SceneWorldReplicationMode.CreatureView3Mixed => CreatureView3MixedDeltaCodec.Decode(envelope, replicationPolicy!.MixedLimits!, replicationPolicy.WoundsPolicy),
            _ => CreatureView3ScalarDeltaCodec.Decode(envelope, replicationPolicy!.MaximumScalarUpdates)
        };
    }
    private void RequireCreature(ulong id)
    { Require(registry.Find(id) is { } snapshot && catalog.Require(snapshot.Create.TemplateCrc).Recipe == SceneObjectResourceRecipe.CreatureDetail0, "Typed CREO3 for non-creature/missing object."); }
    private SceneCreateObjectByCrcMessage DecodeCreate(ReadOnlySpan<byte> message)
    { var create = SceneObjectLifecycleCodec.DecodeCreate(message); RequireRecipe(create); _ = SceneCreatePlacementBuilder.Create(create, zeroPolicy); return create; }
    private static Ready DecodeReady(ReadOnlySpan<byte> value) { SceneReadyProtocolCodec.Decode(value); return new(); }
    private void Apply(object value)
    {
        if (value is ObjectReplication incoming) value = ResolveObjectReplication(incoming);
        if(value is TangibleView3BaselineSnapshot tangibleBaseline){ApplyTangibleCore(tangibleBaseline.ObjectId,tangibleBaseline,null);return;}
        if(value is TangibleView3DeltaPlan tangibleDelta){ApplyTangibleCore(tangibleDelta.ObjectId,null,tangibleDelta);return;}
        ulong generation = registry.SceneGeneration;
        ulong nextMutationRevision = checked(mutationRevision + 1);
        ulong[] affected;
        SceneWorldChangeKind kind;
        switch (value)
        {
            case SceneCreateObjectByCrcMessage create: affected = new[] { create.ObjectId }; kind = SceneWorldChangeKind.Lifecycle; break;
            case SceneEndBaselinesMessage end: affected = new[] { end.ObjectId }; kind = SceneWorldChangeKind.Lifecycle; break;
            case UpdateContainmentMessage contain: affected = new[] { contain.ObjectId }; kind = SceneWorldChangeKind.Lifecycle; break;
            case SceneDestroyObjectMessage destroy:
                affected = new[] { destroy.ObjectId }.Concat(registry.Snapshots().Where(s => destroy.ObjectId != 0 && s.ContainerId == destroy.ObjectId).Select(s => s.Handle.ObjectId)).ToArray();
                kind = SceneWorldChangeKind.Lifecycle; break;
            case CreatureView3BaselineSnapshot baseline: affected = new[] { baseline.ObjectId }; kind = SceneWorldChangeKind.Replication; break;
            case CreatureView3ScalarDeltaPlan plan: affected = new[] { plan.ObjectId }; kind = SceneWorldChangeKind.Replication; break;
            case CreatureView3CompositeDeltaPlan composite: affected = new[] { composite.ObjectId }; kind = SceneWorldChangeKind.Replication; break;
            case CreatureView3ComponentsDeltaPlan components: affected = new[] { components.ObjectId }; kind = SceneWorldChangeKind.Replication; break;
            case CreatureView3MixedDeltaPlan mixed: affected = new[] { mixed.ObjectId }; kind = SceneWorldChangeKind.Replication; break;
            case ReplicationEnvelope envelope: affected = new[] { envelope.ObjectId }; kind = SceneWorldChangeKind.Replication; break;
            default: throw new InvalidDataException("Control message inside lifecycle replay.");
        }
        Require(affected.Count(id => !changes.ContainsKey(id)) <= maximumPendingChanges - changes.Count, "Undelivered world change capacity exhausted.");
        switch (value)
        {
            case SceneCreateObjectByCrcMessage create:
                RequireRecipe(create); var placement = SceneCreatePlacementBuilder.Create(create, zeroPolicy);
                var created = registry.Create(generation, create)!; placements[created.Handle.ObjectId] = placement; break;
            case SceneEndBaselinesMessage end:
                if(tangible is not null && registry.Find(end.ObjectId) is {} staticTarget && catalog.Require(staticTarget.Create.TemplateCrc).Recipe==SceneObjectResourceRecipe.StaticAppearance)Require(tangible.Find(staticTarget.Incarnation) is not null,"Complete TANO3 baseline required before static end barrier.");
                registry.EndBaselines(generation, end); Queue(registry.Find(end.ObjectId)!, false); break;
            case UpdateContainmentMessage contain:
                registry.Contain(generation, contain); var contained = registry.Find(contain.ObjectId)!;
                if (contained.Phase == SceneObjectPhase.Finalized) Queue(contained, false); break;
            case SceneDestroyObjectMessage destroy:
                registry.Destroy(generation, destroy); placements.Remove(destroy.ObjectId);
                if (envelopes.Remove(destroy.ObjectId, out var removed)) { envelopeCount -= removed.Count; envelopeBytes -= removed.Sum(e => checked(23 + (int)e.PayloadSize)); }
                pending.RemoveAll(p => p.Handle.ObjectId == destroy.ObjectId);
                foreach (var child in registry.Snapshots().Where(s => s.Phase == SceneObjectPhase.Finalized)) Queue(child, false);
                break;
            case CreatureView3BaselineSnapshot baseline:
                RequireCreature(baseline.ObjectId); Require(registry.Find(baseline.ObjectId) is not null, "Baseline for missing object.");
                replication!.Seed(registry.Find(baseline.ObjectId)!.Incarnation, baseline); break;
            case CreatureView3ScalarDeltaPlan plan:
                RequireCreature(plan.ObjectId); Require(registry.Find(plan.ObjectId) is not null, "Delta for missing object.");
                var token = registry.Find(plan.ObjectId)!.Incarnation;
                Require(replication!.Find(token) is not null, "Complete baseline required before scalar delta.");
                replication.Apply(token, plan); break;
            case CreatureView3MixedDeltaPlan mixed:
                RequireCreature(mixed.ObjectId); Require(registry.Find(mixed.ObjectId) is not null, "Mixed delta for missing object.");
                var mixedToken = registry.Find(mixed.ObjectId)!.Incarnation;
                Require(replication!.Find(mixedToken) is not null, "Complete baseline required before mixed delta.");
                replication.Apply(mixedToken, mixed); break;
            case CreatureView3CompositeDeltaPlan composite:
                RequireCreature(composite.ObjectId); Require(registry.Find(composite.ObjectId) is not null, "Composite delta for missing object.");
                var compositeToken = registry.Find(composite.ObjectId)!.Incarnation;
                Require(replication!.Find(compositeToken) is not null, "Complete baseline required before composite delta.");
                replication.Apply(compositeToken, composite); break;
            case CreatureView3ComponentsDeltaPlan components:
                RequireCreature(components.ObjectId); Require(registry.Find(components.ObjectId) is not null, "Components delta for missing object.");
                var componentToken = registry.Find(components.ObjectId)!.Incarnation;
                Require(replication!.Find(componentToken) is not null, "Complete baseline required before components delta.");
                replication.Apply(componentToken, components); break;
            case ReplicationEnvelope envelope:
                Require(registry.Find(envelope.ObjectId) is not null, "Envelope for missing object.");
                if (objectOptions is not null)
                {
                    bool creature = catalog.Require(registry.Find(envelope.ObjectId)!.Create.TemplateCrc).Recipe == SceneObjectResourceRecipe.CreatureDetail0;
                    Require(creature ? envelope.RawTag == CreatureView3BaselineCodec.RawTag : envelope.RawTag == 0x54414E4F, "Replication family/recipe disagreement.");
                    Require(!creature || replication is null, "Typed creature mode rejects opaque family/view fallback.");
                }
                int bytes = checked(23 + (int)envelope.PayloadSize);
                Require(envelopeCount < maximumEnvelopes && bytes <= maximumEnvelopeBytes - envelopeBytes, "Opaque envelope bookkeeping budget exhausted.");
                if (!envelopes.TryGetValue(envelope.ObjectId, out var history)) envelopes[envelope.ObjectId] = history = new();
                history.Add(envelope); envelopeCount++; envelopeBytes += bytes; break;
            default: throw new InvalidDataException("Control message inside lifecycle replay.");
        }
        foreach (ulong id in affected)
            changes[id] = changes.TryGetValue(id, out var slot) ? slot with { Kind = slot.Kind | kind } : new(kind, nextMutationRevision);
        mutationRevision = nextMutationRevision;
        _ = tangible?.Snapshots();
        _ = replication?.Snapshots(); // Retire destroyed incarnations; lifecycle revisions preserve scalar state.
        foreach (ulong id in started.ToArray())
            if (FindResourceHandle(id) is { } handle && !registry.IsCurrent(handle)) { CancelResource(id); started.Remove(id); }
    }
    private void Queue(SceneObjectSnapshot snapshot, bool restart)
    {
        var existing = FindResourceHandle(snapshot.Handle.ObjectId);
        if (!restart && existing is { } handle && handle == snapshot.Handle) return;
        bool priorRestart = pending.Any(p => p.Handle.ObjectId == snapshot.Handle.ObjectId && p.Restart);
        pending.RemoveAll(p => p.Handle.ObjectId == snapshot.Handle.ObjectId); pending.Add(new(snapshot.Handle, restart || priorRestart));
    }
    private void ClearWorld()
    {
        registry.Reset(); _ = tangible?.Snapshots(); _ = replication?.Snapshots(); changes.Clear(); placements.Clear(); envelopes.Clear(); pending.Clear(); envelopeCount = envelopeBytes = 0;
        foreach (ulong id in started) CancelResource(id);
        started.Clear();
    }
    private void Fail() { transition.Fail(Generation); ClearWorld(); }
    private void EnsureOpen() { ObjectDisposedException.ThrowIf(closed, this); }
    private void Enter(bool mutation=false) { if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("World serialized owner thread required."); if (busy){reentered=true;throw new InvalidOperationException("World owner is reentrant.");} if(mutation)callbackVersion=checked(callbackVersion+1);reentered=false;busy = true; }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}
