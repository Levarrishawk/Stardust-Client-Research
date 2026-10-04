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
    public SceneWorldTangibleView6Snapshot? TangibleView6 {get;init;}
    public SceneWorldCreatureView6Snapshot? CreatureView6 {get;init;}
    public SceneWorldCreatureView6CompositeSnapshot? CreatureView6Composite {get;init;}
    public SceneWorldCreatureView6FullSnapshot? CreatureView6Full {get;init;}
    public SceneWorldCreatureView4FullSnapshot? CreatureView4Full {get;init;}
    public SceneWorldCreatureView4PartialSnapshot? CreatureView4Partial {get;init;}
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
    private readonly SceneCreatureView6FullReplicationStore? creature6Full;
    private readonly SceneWorldCreatureView6FullPolicy? creature6FullPolicy;
    private readonly SceneCreatureView4PartialReplicationStore? creature4Partial;
    private readonly SceneWorldCreatureView4PartialPolicy? creature4PartialPolicy;
    private Dictionary<ulong,SceneCreatureView4PartialSnapshot> partialPublications=new();
    private long lastPartialWorldWork;
    private readonly SceneCreatureView4FullReplicationStore? creature4Full;
    private readonly SceneWorldCreatureView4FullPolicy? creature4FullPolicy;
    private readonly SceneCreatureView6CompositeReplicationStore? creature6Composite;
    private readonly SceneWorldCreatureView6CompositePolicy? creature6CompositePolicy;
    private readonly SceneCreatureView6ReplicationStore? creature6;
    private readonly SceneWorldCreatureView6Policy? creature6Policy;
    private readonly SceneTangibleView6ReplicationStore? tangible6;
    private readonly SceneWorldTangibleView6Policy? tangible6Policy;
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
        SceneWorldReplicationPolicy? replicationPolicy = null, SceneWorldChangePolicy? changePolicy = null, SceneWorldTangibleReplicationPolicy? tangibleReplicationPolicy = null, SceneWorldTangibleView6Policy? tangibleView6Policy = null, SceneWorldCreatureView6Policy? creatureView6Policy = null, SceneWorldCreatureView6CompositePolicy? creatureView6CompositePolicy = null, SceneWorldCreatureView6FullPolicy? creatureView6FullPolicy = null, SceneWorldCreatureView4FullPolicy? creatureView4FullPolicy = null, SceneWorldCreatureView4PartialPolicy? creatureView4PartialPolicy = null)
        : this(catalog, zeroPolicy, maximumObjects, maximumOutstandingJobs, maximumStagedMessages, maximumStagedBytes, maximumEnvelopes, maximumEnvelopeBytes, maximumPayloadBytes, replicationPolicy, changePolicy, tangibleReplicationPolicy, tangibleView6Policy, creatureView6Policy, creatureView6CompositePolicy, creatureView6FullPolicy, creatureView4FullPolicy, creatureView4PartialPolicy)
    {
        if(tangibleView6Policy is not null)throw new ArgumentException("TANO6 requires the generic object world constructor.",nameof(tangibleView6Policy));
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
        SceneWorldReplicationPolicy? replicationPolicy = null, SceneWorldChangePolicy? changePolicy = null, SceneWorldTangibleReplicationPolicy? tangibleReplicationPolicy = null, SceneWorldTangibleView6Policy? tangibleView6Policy = null, SceneWorldCreatureView6Policy? creatureView6Policy = null, SceneWorldCreatureView6CompositePolicy? creatureView6CompositePolicy = null, SceneWorldCreatureView6FullPolicy? creatureView6FullPolicy = null, SceneWorldCreatureView4FullPolicy? creatureView4FullPolicy = null, SceneWorldCreatureView4PartialPolicy? creatureView4PartialPolicy = null)
        : this(catalog, zeroPolicy, maximumObjects, maximumOutstandingJobs, maximumStagedMessages, maximumStagedBytes, maximumEnvelopes, maximumEnvelopeBytes, maximumPayloadBytes, replicationPolicy, changePolicy, tangibleReplicationPolicy, tangibleView6Policy, creatureView6Policy, creatureView6CompositePolicy, creatureView6FullPolicy, creatureView4FullPolicy, creatureView4PartialPolicy)
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
        SceneWorldReplicationPolicy? replicationPolicy = null, SceneWorldChangePolicy? changePolicy = null, SceneWorldTangibleReplicationPolicy? tangibleReplicationPolicy = null, SceneWorldTangibleView6Policy? tangibleView6Policy = null, SceneWorldCreatureView6Policy? creatureView6Policy = null, SceneWorldCreatureView6CompositePolicy? creatureView6CompositePolicy = null, SceneWorldCreatureView6FullPolicy? creatureView6FullPolicy = null, SceneWorldCreatureView4FullPolicy? creatureView4FullPolicy = null, SceneWorldCreatureView4PartialPolicy? creatureView4PartialPolicy = null)
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
        if(tangibleView6Policy is {} t6)
        {
            ArgumentNullException.ThrowIfNull(t6.CodecLimits);ArgumentNullException.ThrowIfNull(t6.StateLimits);ArgumentNullException.ThrowIfNull(t6.StoreLimits);
            var c6=t6.CodecLimits;
            if(c6.MaximumPayloadBytes is <2 or >8388608||c6.MaximumPayloadBytes>maximumPayloadBytes||c6.MaximumFields is <0 or >4096||c6.MaximumOperations is <0 or >65536||c6.MaximumAssignmentValues is <0 or >65535||c6.MaximumIds is <0 or >1048576||c6.MaximumDecodedBytes is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(tangibleView6Policy));
            tangible6Policy=t6 with {CodecLimits=t6.CodecLimits with {},StateLimits=t6.StateLimits with {},StoreLimits=t6.StoreLimits with {}};
            tangible6=new(registry,tangible6Policy.StoreLimits,tangible6Policy.StateLimits,t6.RevisionSeed);
        }
        if((creatureView6Policy is null?0:1)+(creatureView6CompositePolicy is null?0:1)+(creatureView6FullPolicy is null?0:1)>1)throw new ArgumentException("Choose one CREO6 partition.",nameof(creatureView6FullPolicy));
        if(creatureView6Policy is {} c6p)
        {
            ArgumentNullException.ThrowIfNull(c6p.CodecLimits);ArgumentNullException.ThrowIfNull(c6p.StateLimits);ArgumentNullException.ThrowIfNull(c6p.StoreLimits);
            var limits6=c6p.CodecLimits;
            if(limits6.MaximumPayloadBytes is <2 or >8388608||limits6.MaximumPayloadBytes>maximumPayloadBytes||limits6.MaximumFields is <0 or >4096||limits6.MaximumStringBytes is <0 or >1048576||limits6.MaximumTotalStringBytes is <0 or >4194304||limits6.MaximumElements is <0 or >65536||limits6.MaximumTotalElements is <0 or >262144||limits6.MaximumDecodedBytes is <0 or >16777216||limits6.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(creatureView6Policy));
            creature6Policy=c6p with {CodecLimits=c6p.CodecLimits with {},StateLimits=c6p.StateLimits with {},StoreLimits=c6p.StoreLimits with {}};
            creature6=new(registry,creature6Policy.StoreLimits,creature6Policy.StateLimits,c6p.RevisionSeed);
        }
        if(creatureView6CompositePolicy is {} c6cp)
        {
            ArgumentNullException.ThrowIfNull(c6cp.BaselineLimits);ArgumentNullException.ThrowIfNull(c6cp.DeltaLimits);ArgumentNullException.ThrowIfNull(c6cp.StateLimits);ArgumentNullException.ThrowIfNull(c6cp.StoreLimits);
            var limits6=c6cp.BaselineLimits;
            if(limits6.MaximumPayloadBytes is <2 or >8388608||limits6.MaximumPayloadBytes>maximumPayloadBytes||limits6.MaximumFields is <0 or >4096||limits6.MaximumStringBytes is <0 or >1048576||limits6.MaximumTotalStringBytes is <0 or >4194304||limits6.MaximumElements is <0 or >65536||limits6.MaximumTotalElements is <0 or >262144||limits6.MaximumDecodedBytes is <0 or >16777216||limits6.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(creatureView6CompositePolicy));
            CreatureView6CompositeCodec.Validate(c6cp.DeltaLimits);
            if(c6cp.DeltaLimits.MaximumPayloadBytes>maximumPayloadBytes)throw new ArgumentOutOfRangeException(nameof(creatureView6CompositePolicy));
            creature6CompositePolicy=c6cp with {BaselineLimits=c6cp.BaselineLimits with {},DeltaLimits=c6cp.DeltaLimits with {},StateLimits=new(c6cp.StateLimits.ModelLimits is {} ml6?ml6 with {}:null,c6cp.StateLimits.MessageLimits is {} dl6?dl6 with {}:null),StoreLimits=c6cp.StoreLimits with {}};
            creature6Composite=new(registry,creature6CompositePolicy.StoreLimits,creature6CompositePolicy.StateLimits,c6cp.RevisionSeed);
        }
        if(creatureView6FullPolicy is {} c6fp)
        {
            ArgumentNullException.ThrowIfNull(c6fp.BaselineLimits);ArgumentNullException.ThrowIfNull(c6fp.DeltaLimits);ArgumentNullException.ThrowIfNull(c6fp.StateLimits);ArgumentNullException.ThrowIfNull(c6fp.StoreLimits);
            var limits6=c6fp.BaselineLimits;
            if(limits6.MaximumPayloadBytes is <2 or >8388608||limits6.MaximumPayloadBytes>maximumPayloadBytes||limits6.MaximumFields is <0 or >4096||limits6.MaximumStringBytes is <0 or >1048576||limits6.MaximumTotalStringBytes is <0 or >4194304||limits6.MaximumElements is <0 or >65536||limits6.MaximumTotalElements is <0 or >262144||limits6.MaximumDecodedBytes is <0 or >16777216||limits6.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(creatureView6FullPolicy));
            CreatureView6FullCodec.Validate(c6fp.DeltaLimits);
            if(c6fp.DeltaLimits.MaximumPayloadBytes>maximumPayloadBytes)throw new ArgumentOutOfRangeException(nameof(creatureView6FullPolicy));
            creature6FullPolicy=c6fp with {BaselineLimits=c6fp.BaselineLimits with {},DeltaLimits=c6fp.DeltaLimits with {},StateLimits=new(c6fp.StateLimits.ModelLimits is {} ml6?ml6 with {}:null,c6fp.StateLimits.MessageLimits is {} dl6?dl6 with {}:null),StoreLimits=c6fp.StoreLimits with {}};
            creature6Full=new(registry,creature6FullPolicy.StoreLimits,creature6FullPolicy.StateLimits,c6fp.RevisionSeed);
        }
        if(creatureView4FullPolicy is not null&&creatureView4PartialPolicy is not null)throw new ArgumentException("Full and received-only partial CREO4 policies are mutually exclusive.");
        if(creatureView4PartialPolicy is {} c4pp)
        {
            ArgumentNullException.ThrowIfNull(c4pp.StateLimits);ArgumentNullException.ThrowIfNull(c4pp.StoreLimits);
            var message=c4pp.StateLimits.MessageLimits??new();CreatureView4Codec.Validate(message);
            if(message.MaximumPayloadBytes>maximumPayloadBytes||c4pp.MaximumWorldBytes is <16 or >268435456||c4pp.MaximumWorldWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(creatureView4PartialPolicy));
            creature4PartialPolicy=c4pp with{StateLimits=c4pp.StateLimits with{MessageLimits=message with{}},StoreLimits=c4pp.StoreLimits with{}};
            creature4Partial=new(registry,creature4PartialPolicy.StoreLimits,creature4PartialPolicy.StateLimits,c4pp.RevisionSeed);
        }
        if(creatureView4FullPolicy is {} c4fp)
        {
            ArgumentNullException.ThrowIfNull(c4fp.BaselineLimits);ArgumentNullException.ThrowIfNull(c4fp.DeltaLimits);ArgumentNullException.ThrowIfNull(c4fp.StateLimits);ArgumentNullException.ThrowIfNull(c4fp.StoreLimits);
            var limits4=c4fp.BaselineLimits;
            if(limits4.MaximumPayloadBytes is <2 or >8388608||limits4.MaximumPayloadBytes>maximumPayloadBytes||limits4.MaximumFields is <0 or >4096||limits4.MaximumStringBytes is <0 or >1048576||limits4.MaximumTotalStringBytes is <0 or >4194304||limits4.MaximumElements is <0 or >65536||limits4.MaximumTotalElements is <0 or >196608||limits4.MaximumDecodedBytes is <0 or >16777216||limits4.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(creatureView4FullPolicy));
            CreatureView4FullCodec.Validate(c4fp.DeltaLimits);
            if(c4fp.DeltaLimits.MaximumPayloadBytes>maximumPayloadBytes)throw new ArgumentOutOfRangeException(nameof(creatureView4FullPolicy));
            creature4FullPolicy=c4fp with {BaselineLimits=c4fp.BaselineLimits with {},DeltaLimits=c4fp.DeltaLimits with {},StateLimits=new(c4fp.StateLimits.ModelLimits is {} ml4?ml4 with {}:null,c4fp.StateLimits.MessageLimits is {} dl4?dl4 with {}:null),StoreLimits=c4fp.StoreLimits with {}};
            creature4Full=new(registry,creature4FullPolicy.StoreLimits,creature4FullPolicy.StateLimits,c4fp.RevisionSeed);
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
            bool typedAttempt=application.Length>=ReplicationEnvelopeCodec.MinimumMessageLength&&
                ((BinaryPrimitives.ReadUInt32LittleEndian(application[14..])==TangibleView3BaselineCodec.RawTag&&((tangible is not null&&application[18]==3)||(tangible6 is not null&&application[18]==6)))||
                 ((creature6 is not null||creature6Composite is not null||creature6Full is not null)&&BinaryPrimitives.ReadUInt32LittleEndian(application[14..])==CreatureView6Codec.RawTag&&application[18]==6)||
                 ((creature4Full is not null||creature4Partial is not null)&&BinaryPrimitives.ReadUInt32LittleEndian(application[14..])==CreatureView4Codec.RawTag&&application[18]==4))&&
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
                {Replication=state is not null?new(identity,Generation,state):null,CreatureView6=Creature6For(plan.ObjectId),CreatureView6Composite=Creature6CompositeFor(plan.ObjectId),CreatureView4Full=Creature4FullFor(plan.ObjectId),CreatureView4Partial=Creature4PartialFor(plan.ObjectId),CreatureView6Full=Creature6FullFor(plan.ObjectId),TangibleView6=Tangible6For(plan.ObjectId),TangibleReplication=TangibleFor(plan.ObjectId)}:null;
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
                {Replication=state is not null?new(identity,Generation,state):null,CreatureView6=Creature6For(plan.ObjectId),CreatureView6Composite=Creature6CompositeFor(plan.ObjectId),CreatureView4Full=Creature4FullFor(plan.ObjectId),CreatureView4Partial=Creature4PartialFor(plan.ObjectId),CreatureView6Full=Creature6FullFor(plan.ObjectId),TangibleView6=Tangible6For(plan.ObjectId),TangibleReplication=TangibleFor(plan.ObjectId)}:null;
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
                {Replication=state is not null?new(identity,Generation,state):null,CreatureView6=Creature6For(plan.ObjectId),CreatureView6Composite=Creature6CompositeFor(plan.ObjectId),CreatureView4Full=Creature4FullFor(plan.ObjectId),CreatureView4Partial=Creature4PartialFor(plan.ObjectId),CreatureView6Full=Creature6FullFor(plan.ObjectId),TangibleView6=Tangible6For(plan.ObjectId),TangibleReplication=TangibleFor(plan.ObjectId)}:null;
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
                {Replication=state is not null?new(identity,Generation,state):null,CreatureView6=Creature6For(plan.ObjectId),CreatureView6Composite=Creature6CompositeFor(plan.ObjectId),CreatureView4Full=Creature4FullFor(plan.ObjectId),CreatureView4Partial=Creature4PartialFor(plan.ObjectId),CreatureView6Full=Creature6FullFor(plan.ObjectId),TangibleView6=Tangible6For(plan.ObjectId),TangibleReplication=TangibleFor(plan.ObjectId)}:null;
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
            { Replication = ReplicationFor(objectId),CreatureView6=Creature6For(objectId),CreatureView6Composite=Creature6CompositeFor(objectId),CreatureView4Full=Creature4FullFor(objectId),CreatureView4Partial=Creature4PartialFor(objectId),CreatureView6Full=Creature6FullFor(objectId),TangibleView6=Tangible6For(objectId),TangibleReplication=TangibleFor(objectId) };
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
    public bool TangibleView6Enabled=>tangible6Policy is not null;
    public long RetainedTangibleView6Bytes {get{Enter();try{return checked((tangible6?.RetainedStateBytes??0)+24L*(tangible6?.PendingEventCount??0));}finally{busy=false;}}}
    private SceneWorldTangibleView6Snapshot? Tangible6For(ulong id)
    {var target=registry.Find(id);return target is not null&&tangible6?.Find(target.Handle) is {} state?new(identity,Generation,state):null;}
    public SceneWorldTangibleView6Snapshot? FindTangibleView6(long generation,ulong id)
    {Enter();try{return !closed&&generation==Generation?Tangible6For(id):null;}finally{busy=false;}}
    private bool CurrentTangible6(SceneWorldTangibleView6Snapshot value)=>!closed&&ReferenceEquals(value.Owner,identity)&&value.Generation==Generation&&State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready&&registry.IsCurrent(value.Incarnation)&&registry.Find(value.Incarnation.ObjectId) is {} target&&ReferenceEquals(tangible6?.Find(target.Handle),value.Publication);
    public bool IsCurrent(SceneWorldTangibleView6Snapshot value)
    {Enter();try{ArgumentNullException.ThrowIfNull(value);return CurrentTangible6(value);}finally{busy=false;}}
    private void ApplyTangible6Core(ulong id,TangibleView6Baseline? baseline,TangibleView6Delta? plan,SceneWorldTangibleView6Snapshot? expected)
    {
        Require(tangible6 is not null&&objectOptions is not null,"Typed TANO6 generic world required.");
        var target=registry.Find(id);Require(target is not null&&catalog.Require(target.Create.TemplateCrc).Recipe==SceneObjectResourceRecipe.StaticAppearance,"Static TANO6 recipe required.");
        if(plan is not null)Require(target!.Phase==SceneObjectPhase.Finalized,"Finalized TANO6 target required.");
        if(expected is not null)Require(CurrentTangible6(expected)&&expected.Incarnation==target!.Incarnation,"Current exact TANO6 world receipt required.");
        var current=plan is not null?tangible6!.Find(target!.Handle):null;
        if(plan is not null)Require(current is not null,"Complete TANO6 baseline required.");
        ulong next=checked(mutationRevision+1);Require(changes.ContainsKey(id)||changes.Count<maximumPendingChanges,"Undelivered world change capacity exhausted.");
        var staged=new Dictionary<ulong,ChangeSlot>(changes);
        staged[id]=changes.TryGetValue(id,out var slot)?slot with {Kind=slot.Kind|SceneWorldChangeKind.Replication}:new(SceneWorldChangeKind.Replication,next);
        SceneTangibleView6ReplicationStore.Prepared? prepared=null;
        try
        {
            prepared=baseline is not null?tangible6!.PrepareSeed(target!.Handle,baseline):tangible6!.PrepareApply(target!.Handle,current!,plan!);
            Require(prepared is not null,"Stale TANO6 preparation.");
            tangible6!.CheckPrepared(prepared!);
            // All candidates and bounds are ready: callback-free assignments only from here.
            tangible6.CommitPrepared(prepared!);changes=staged;mutationRevision=next;
        }finally{if(prepared is not null)tangible6!.ReleasePrepared(prepared);}
    }
    public bool ApplyTangibleView6Delta(long generation,SceneWorldTangibleView6Snapshot expected,TangibleView6Delta plan)
    {Enter(true);try{EnsureOpen();if(generation!=Generation)return false;ArgumentNullException.ThrowIfNull(expected);if(!CurrentTangible6(expected))return false;ArgumentNullException.ThrowIfNull(plan);Require(plan.ObjectId==expected.Incarnation.ObjectId,"TANO6 plan/receipt mismatch.");ApplyTangible6Core(plan.ObjectId,null,plan,expected);return true;}finally{busy=false;}}
    internal TangibleView6Delta ParseTangibleView6Delta(ReplicationEnvelope envelope)
    {Enter();try{EnsureOpen();Require(tangible6Policy is not null,"Typed TANO6 policy required.");return TangibleView6Codec.DecodeDelta(envelope,tangible6Policy!.CodecLimits);}finally{busy=false;}}
    public SceneWorldTangibleView6EventBatch? DrainTangibleView6Events(long generation)
    {Enter(true);try{EnsureOpen();return generation==Generation&&tangible6 is not null?new(identity,Generation,tangible6.DrainEvents()):null;}finally{busy=false;}}
    public bool AcknowledgeTangibleView6Events(SceneWorldTangibleView6EventBatch batch)
    {Enter(true);try{ArgumentNullException.ThrowIfNull(batch);return !closed&&ReferenceEquals(batch.Owner,identity)&&batch.Generation==Generation&&tangible6?.Acknowledge(batch.Batch)==true;}finally{busy=false;}}
    public bool IsCurrentTangibleView6Event(SceneWorldTangibleView6EventBatch batch,int index)
    {Enter();try{ArgumentNullException.ThrowIfNull(batch);if(closed||!ReferenceEquals(batch.Owner,identity)||batch.Generation!=Generation||index<0||index>=batch.Events.Count||tangible6?.IsCurrentBatch(batch.Batch)!=true)return false;var e=batch.Events[index];return registry.IsCurrent(e.Incarnation)&&registry.Find(e.Incarnation.ObjectId)?.Phase==SceneObjectPhase.Finalized;}finally{busy=false;}}
    public bool CreatureView6Enabled=>creature6Policy is not null;
    public long RetainedCreatureView6Bytes {get{Enter();try{return checked((creature6?.RetainedStateBytes??0)+8L*(creature6?.PendingEventCount??0));}finally{busy=false;}}}
    private SceneWorldCreatureView6Snapshot? Creature6For(ulong id)
    {var target=registry.Find(id);return target is not null&&creature6?.Find(target.Handle) is {} state?new(identity,Generation,state):null;}
    public SceneWorldCreatureView6Snapshot? FindCreatureView6(long generation,ulong id)
    {Enter();try{return !closed&&generation==Generation?Creature6For(id):null;}finally{busy=false;}}
    private bool CurrentCreature6(SceneWorldCreatureView6Snapshot value)=>!closed&&ReferenceEquals(value.Owner,identity)&&value.Generation==Generation&&State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready&&registry.IsCurrent(value.Incarnation)&&registry.Find(value.Incarnation.ObjectId) is {} target&&ReferenceEquals(creature6?.Find(target.Handle),value.Publication);
    public bool IsCurrent(SceneWorldCreatureView6Snapshot value)
    {Enter();try{ArgumentNullException.ThrowIfNull(value);return CurrentCreature6(value);}finally{busy=false;}}
    private void ApplyCreature6Core(ulong id,CreatureView6Baseline? baseline,CreatureView6Delta? plan,SceneWorldCreatureView6Snapshot? expected)
    {
        Require(creature6 is not null,"Typed CREO6 world required.");
        var target=registry.Find(id);Require(target is not null&&catalog.Require(target.Create.TemplateCrc).Recipe==SceneObjectResourceRecipe.CreatureDetail0,"Creature CREO6 recipe required.");
        if(plan is not null)Require(target!.Phase==SceneObjectPhase.Finalized,"Finalized CREO6 target required.");
        if(expected is not null)Require(CurrentCreature6(expected)&&expected.Incarnation==target!.Incarnation,"Current exact CREO6 world receipt required.");
        var current=plan is not null?creature6!.Find(target!.Handle):null;
        if(plan is not null)Require(current is not null,"Complete CREO6 baseline required.");
        ulong next=checked(mutationRevision+1);Require(changes.ContainsKey(id)||changes.Count<maximumPendingChanges,"Undelivered world change capacity exhausted.");
        var staged=new Dictionary<ulong,ChangeSlot>(changes);
        staged[id]=changes.TryGetValue(id,out var slot)?slot with {Kind=slot.Kind|SceneWorldChangeKind.Replication}:new(SceneWorldChangeKind.Replication,next);
        SceneCreatureView6ReplicationStore.Prepared? prepared=null;
        try
        {
            prepared=baseline is not null?creature6!.PrepareSeed(target!.Handle,baseline):creature6!.PrepareApply(target!.Handle,current!,plan!);
            Require(prepared is not null,"Stale CREO6 preparation.");
            creature6!.CheckPrepared(prepared!);
            // All candidates and bounds are ready: callback-free assignments only from here.
            creature6.CommitPrepared(prepared!);changes=staged;mutationRevision=next;
        }finally{if(prepared is not null)creature6!.ReleasePrepared(prepared);}
    }
    public bool ApplyCreatureView6Delta(long generation,SceneWorldCreatureView6Snapshot expected,CreatureView6Delta plan)
    {Enter(true);try{EnsureOpen();if(generation!=Generation)return false;ArgumentNullException.ThrowIfNull(expected);if(!CurrentCreature6(expected))return false;ArgumentNullException.ThrowIfNull(plan);Require(plan.ObjectId==expected.Incarnation.ObjectId,"CREO6 plan/receipt mismatch.");ApplyCreature6Core(plan.ObjectId,null,plan,expected);return true;}finally{busy=false;}}
    internal CreatureView6Delta ParseCreatureView6Delta(ReplicationEnvelope envelope)
    {Enter();try{EnsureOpen();Require(creature6Policy is not null,"Typed CREO6 policy required.");return CreatureView6Codec.DecodeDelta(envelope,creature6Policy!.CodecLimits);}finally{busy=false;}}
    public SceneWorldCreatureView6EventBatch? DrainCreatureView6Events(long generation)
    {Enter(true);try{EnsureOpen();return generation==Generation&&creature6 is not null?new(identity,Generation,creature6.DrainEvents()):null;}finally{busy=false;}}
    public bool AcknowledgeCreatureView6Events(SceneWorldCreatureView6EventBatch batch)
    {Enter(true);try{ArgumentNullException.ThrowIfNull(batch);return !closed&&ReferenceEquals(batch.Owner,identity)&&batch.Generation==Generation&&creature6?.Acknowledge(batch.Batch)==true;}finally{busy=false;}}
    public bool IsCurrentCreatureView6Event(SceneWorldCreatureView6EventBatch batch,int index)
    {Enter();try{ArgumentNullException.ThrowIfNull(batch);if(closed||!ReferenceEquals(batch.Owner,identity)||batch.Generation!=Generation||index<0||index>=batch.Events.Count||creature6?.IsCurrentBatch(batch.Batch)!=true)return false;var e=batch.Events[index];return registry.IsCurrent(e.Incarnation)&&registry.Find(e.Incarnation.ObjectId)?.Phase==SceneObjectPhase.Finalized;}finally{busy=false;}}
    public bool CreatureView6CompositeEnabled=>creature6CompositePolicy is not null;
    public long RetainedCreatureView6CompositeBytes {get{Enter();try{return checked((creature6Composite?.RetainedStateBytes??0)+24L*(creature6Composite?.PendingEventCount??0));}finally{busy=false;}}}
    private SceneWorldCreatureView6CompositeSnapshot? Creature6CompositeFor(ulong id)
    {var target=registry.Find(id);return target is not null&&creature6Composite?.Find(target.Handle) is {} state?new(identity,Generation,state):null;}
    public SceneWorldCreatureView6CompositeSnapshot? FindCreatureView6Composite(long generation,ulong id)
    {Enter();try{return !closed&&generation==Generation?Creature6CompositeFor(id):null;}finally{busy=false;}}
    private bool CurrentCreature6Composite(SceneWorldCreatureView6CompositeSnapshot value)=>!closed&&ReferenceEquals(value.Owner,identity)&&value.Generation==Generation&&State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready&&registry.IsCurrent(value.Incarnation)&&registry.Find(value.Incarnation.ObjectId) is {} target&&ReferenceEquals(creature6Composite?.Find(target.Handle),value.Publication);
    public bool IsCurrent(SceneWorldCreatureView6CompositeSnapshot value)
    {Enter();try{ArgumentNullException.ThrowIfNull(value);return CurrentCreature6Composite(value);}finally{busy=false;}}
    private void ApplyCreature6CompositeCore(ulong id,CreatureView6Baseline? baseline,CreatureView6CompositeDelta? plan,SceneWorldCreatureView6CompositeSnapshot? expected)
    {
        Require(creature6Composite is not null,"Typed CREO6 world required.");
        var target=registry.Find(id);Require(target is not null&&catalog.Require(target.Create.TemplateCrc).Recipe==SceneObjectResourceRecipe.CreatureDetail0,"Creature CREO6 recipe required.");
        if(plan is not null)Require(target!.Phase==SceneObjectPhase.Finalized,"Finalized CREO6 target required.");
        if(expected is not null)Require(CurrentCreature6Composite(expected)&&expected.Incarnation==target!.Incarnation,"Current exact CREO6 world receipt required.");
        var current=plan is not null?creature6Composite!.Find(target!.Handle):null;
        if(plan is not null)Require(current is not null,"Complete CREO6 baseline required.");
        ulong next=checked(mutationRevision+1);Require(changes.ContainsKey(id)||changes.Count<maximumPendingChanges,"Undelivered world change capacity exhausted.");
        var staged=new Dictionary<ulong,ChangeSlot>(changes);
        staged[id]=changes.TryGetValue(id,out var slot)?slot with {Kind=slot.Kind|SceneWorldChangeKind.Replication}:new(SceneWorldChangeKind.Replication,next);
        SceneCreatureView6CompositeReplicationStore.Prepared? prepared=null;
        try
        {
            prepared=baseline is not null?creature6Composite!.PrepareSeed(target!.Handle,baseline):creature6Composite!.PrepareApply(target!.Handle,current!,plan!);
            Require(prepared is not null,"Stale CREO6 preparation.");
            creature6Composite!.CheckPrepared(prepared!);
            // All candidates and bounds are ready: callback-free assignments only from here.
            creature6Composite.CommitPrepared(prepared!);changes=staged;mutationRevision=next;
        }finally{if(prepared is not null)creature6Composite!.ReleasePrepared(prepared);}
    }
    public bool ApplyCreatureView6CompositeDelta(long generation,SceneWorldCreatureView6CompositeSnapshot expected,CreatureView6CompositeDelta plan)
    {Enter(true);try{EnsureOpen();if(generation!=Generation)return false;ArgumentNullException.ThrowIfNull(expected);if(!CurrentCreature6Composite(expected))return false;ArgumentNullException.ThrowIfNull(plan);Require(plan.ObjectId==expected.Incarnation.ObjectId,"CREO6 plan/receipt mismatch.");ApplyCreature6CompositeCore(plan.ObjectId,null,plan,expected);return true;}finally{busy=false;}}
    internal CreatureView6CompositeDelta ParseCreatureView6CompositeDelta(ReplicationEnvelope envelope)
    {Enter();try{EnsureOpen();Require(creature6CompositePolicy is not null,"Typed CREO6 policy required.");return CreatureView6CompositeCodec.Decode(envelope,creature6CompositePolicy!.DeltaLimits);}finally{busy=false;}}
    public SceneWorldCreatureView6CompositeEventBatch? DrainCreatureView6CompositeEvents(long generation)
    {Enter(true);try{EnsureOpen();return generation==Generation&&creature6Composite is not null?new(identity,Generation,creature6Composite.DrainEvents()):null;}finally{busy=false;}}
    public bool AcknowledgeCreatureView6CompositeEvents(SceneWorldCreatureView6CompositeEventBatch batch)
    {Enter(true);try{ArgumentNullException.ThrowIfNull(batch);return !closed&&ReferenceEquals(batch.Owner,identity)&&batch.Generation==Generation&&creature6Composite?.Acknowledge(batch.Batch)==true;}finally{busy=false;}}
    public bool IsCurrentCreatureView6CompositeEvent(SceneWorldCreatureView6CompositeEventBatch batch,int index)
    {Enter();try{ArgumentNullException.ThrowIfNull(batch);if(closed||!ReferenceEquals(batch.Owner,identity)||batch.Generation!=Generation||index<0||index>=batch.Events.Count||creature6Composite?.IsCurrentBatch(batch.Batch)!=true)return false;var e=batch.Events[index];return registry.IsCurrent(e.Incarnation)&&registry.Find(e.Incarnation.ObjectId)?.Phase==SceneObjectPhase.Finalized;}finally{busy=false;}}
    public bool CreatureView6FullEnabled=>creature6FullPolicy is not null;
    public long RetainedCreatureView6FullBytes {get{Enter();try{return checked((creature6Full?.RetainedStateBytes??0)+48L*(creature6Full?.PendingEventCount??0));}finally{busy=false;}}}
    private SceneWorldCreatureView6FullSnapshot? Creature6FullFor(ulong id)
    {var target=registry.Find(id);return target is not null&&creature6Full?.Find(target.Handle) is {} state?new(identity,Generation,state):null;}
    public SceneWorldCreatureView6FullSnapshot? FindCreatureView6Full(long generation,ulong id)
    {Enter();try{return !closed&&generation==Generation?Creature6FullFor(id):null;}finally{busy=false;}}
    private bool CurrentCreature6Full(SceneWorldCreatureView6FullSnapshot value)=>!closed&&ReferenceEquals(value.Owner,identity)&&value.Generation==Generation&&State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready&&registry.IsCurrent(value.Incarnation)&&registry.Find(value.Incarnation.ObjectId) is {} target&&ReferenceEquals(creature6Full?.Find(target.Handle),value.Publication);
    public bool IsCurrent(SceneWorldCreatureView6FullSnapshot value)
    {Enter();try{ArgumentNullException.ThrowIfNull(value);return CurrentCreature6Full(value);}finally{busy=false;}}
    private void ApplyCreature6FullCore(ulong id,CreatureView6Baseline? baseline,CreatureView6FullDelta? plan,SceneWorldCreatureView6FullSnapshot? expected)
    {
        Require(creature6Full is not null,"Typed CREO6 world required.");
        var target=registry.Find(id);Require(target is not null&&catalog.Require(target.Create.TemplateCrc).Recipe==SceneObjectResourceRecipe.CreatureDetail0,"Creature CREO6 recipe required.");
        if(plan is not null)Require(target!.Phase==SceneObjectPhase.Finalized,"Finalized CREO6 target required.");
        if(expected is not null)Require(CurrentCreature6Full(expected)&&expected.Incarnation==target!.Incarnation,"Current exact CREO6 world receipt required.");
        var current=plan is not null?creature6Full!.Find(target!.Handle):null;
        if(plan is not null)Require(current is not null,"Complete CREO6 baseline required.");
        ulong next=checked(mutationRevision+1);Require(changes.ContainsKey(id)||changes.Count<maximumPendingChanges,"Undelivered world change capacity exhausted.");
        var staged=new Dictionary<ulong,ChangeSlot>(changes);
        staged[id]=changes.TryGetValue(id,out var slot)?slot with {Kind=slot.Kind|SceneWorldChangeKind.Replication}:new(SceneWorldChangeKind.Replication,next);
        SceneCreatureView6FullReplicationStore.Prepared? prepared=null;
        try
        {
            prepared=baseline is not null?creature6Full!.PrepareSeed(target!.Handle,baseline):creature6Full!.PrepareApply(target!.Handle,current!,plan!);
            Require(prepared is not null,"Stale CREO6 preparation.");
            creature6Full!.CheckPrepared(prepared!);
            // All candidates and bounds are ready: callback-free assignments only from here.
            creature6Full.CommitPrepared(prepared!);changes=staged;mutationRevision=next;
        }finally{if(prepared is not null)creature6Full!.ReleasePrepared(prepared);}
    }
    public bool ApplyCreatureView6FullDelta(long generation,SceneWorldCreatureView6FullSnapshot expected,CreatureView6FullDelta plan)
    {Enter(true);try{EnsureOpen();if(generation!=Generation)return false;ArgumentNullException.ThrowIfNull(expected);if(!CurrentCreature6Full(expected))return false;ArgumentNullException.ThrowIfNull(plan);Require(plan.ObjectId==expected.Incarnation.ObjectId,"CREO6 plan/receipt mismatch.");ApplyCreature6FullCore(plan.ObjectId,null,plan,expected);return true;}finally{busy=false;}}
    internal CreatureView6FullDelta ParseCreatureView6FullDelta(ReplicationEnvelope envelope)
    {Enter();try{EnsureOpen();Require(creature6FullPolicy is not null,"Typed CREO6 policy required.");return CreatureView6FullCodec.Decode(envelope,creature6FullPolicy!.DeltaLimits);}finally{busy=false;}}
    public SceneWorldCreatureView6FullEventBatch? DrainCreatureView6FullEvents(long generation)
    {Enter(true);try{EnsureOpen();return generation==Generation&&creature6Full is not null?new(identity,Generation,creature6Full.DrainEvents()):null;}finally{busy=false;}}
    public bool AcknowledgeCreatureView6FullEvents(SceneWorldCreatureView6FullEventBatch batch)
    {Enter(true);try{ArgumentNullException.ThrowIfNull(batch);return !closed&&ReferenceEquals(batch.Owner,identity)&&batch.Generation==Generation&&creature6Full?.Acknowledge(batch.Batch)==true;}finally{busy=false;}}
    public bool IsCurrentCreatureView6FullEvent(SceneWorldCreatureView6FullEventBatch batch,int index)
    {Enter();try{ArgumentNullException.ThrowIfNull(batch);if(closed||!ReferenceEquals(batch.Owner,identity)||batch.Generation!=Generation||index<0||index>=batch.Events.Count||creature6Full?.IsCurrentBatch(batch.Batch)!=true)return false;var e=batch.Events[index];return registry.IsCurrent(e.Incarnation)&&registry.Find(e.Incarnation.ObjectId)?.Phase==SceneObjectPhase.Finalized;}finally{busy=false;}}

    public long LastCreatureView4PartialWork {get{Enter();try{return lastPartialWorldWork;}finally{busy=false;}}}
    public long RetainedCreatureView4PartialWorldBytes {get{Enter();try{return closed||creature4Partial is null?0:checked(OtherPartialWorldBytes()+(creature4Partial?.RetainedStateBytes??0)+(creature4Partial?.PendingEventBytes??0)+32L*changes.Count);}finally{busy=false;}}}
    public bool CreatureView4PartialEnabled=>creature4PartialPolicy is not null;
    public long RetainedCreatureView4PartialBytes {get{Enter();try{return checked((creature4Partial?.RetainedStateBytes??0)+(creature4Partial?.PendingEventBytes??0));}finally{busy=false;}}}
    private SceneWorldCreatureView4PartialSnapshot? Creature4PartialFor(ulong id)
    {return partialPublications.TryGetValue(id,out var p)&&registry.IsCurrent(p.Incarnation)?new(identity,Generation,p):null;}
    public SceneWorldCreatureView4PartialSnapshot? FindCreatureView4Partial(long generation,ulong id)
    {Enter();try{return !closed&&generation==Generation?Creature4PartialFor(id):null;}finally{busy=false;}}
    private bool CurrentCreature4Partial(SceneWorldCreatureView4PartialSnapshot value)=>!closed&&ReferenceEquals(value.Owner,identity)&&value.Generation==Generation&&State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready&&registry.IsCurrent(value.Incarnation)&&partialPublications.TryGetValue(value.Incarnation.ObjectId,out var p)&&ReferenceEquals(p,value.Publication);
    public bool IsCurrent(SceneWorldCreatureView4PartialSnapshot value)
    {Enter();try{ArgumentNullException.ThrowIfNull(value);return CurrentCreature4Partial(value);}finally{busy=false;}}
    private long OtherPartialWorldBytes()=>checked(envelopeBytes+(replication?.RetainedDecodedBytes??0)+(tangible?.RetainedDecodedBytes??0)+(tangible6?.RetainedStateBytes??0)+24L*(tangible6?.PendingEventCount??0)+(creature6?.RetainedStateBytes??0)+8L*(creature6?.PendingEventCount??0)+(creature6Composite?.RetainedStateBytes??0)+24L*(creature6Composite?.PendingEventCount??0)+(creature6Full?.RetainedStateBytes??0)+48L*(creature6Full?.PendingEventCount??0));
    private bool ApplyCreature4PartialCore(CreatureView4Delta plan,SceneWorldCreatureView4PartialSnapshot? expected,bool exact)
    {
        Require(creature4Partial is not null,"Received-only partial CREO4 policy required.");
        var target=registry.Find(plan.ObjectId);Require(target is not null&&catalog.Require(target.Create.TemplateCrc).Recipe==SceneObjectResourceRecipe.CreatureDetail0,"Creature partial CREO4 recipient required.");
        partialPublications.TryGetValue(plan.ObjectId,out var old);var current=old?.Incarnation==target!.Incarnation?old:null;
        if(exact&&(expected is null?current is not null:!CurrentCreature4Partial(expected)||!ReferenceEquals(current,expected.Publication)))return false;
        var message=CreatureView4Codec.MeasureDelta(plan,creature4PartialPolicy!.StateLimits.MessageLimits!);Require(plan.Updates.Count>0,"Nonempty partial CREO4 plan required.");
        ulong next=checked(mutationRevision+1);Require(changes.ContainsKey(plan.ObjectId)||changes.Count<maximumPendingChanges,"Undelivered world change capacity exhausted.");
        ushort mask=current?.State.KnownMask??0;foreach(var u in plan.Updates)mask|=(ushort)(1<<u.Index);int known=System.Numerics.BitOperations.PopCount((uint)mask),n=plan.Updates.Count,a=partialPublications.Count,pendingCount=creature4Partial!.PendingEventCount;
        long state=16+32L*known,totalState=checked(partialPublications.Values.Sum(p=>p.State.DecodedBytes)-(old?.State.DecodedBytes??0)+state);
        long storeWork=checked(23L+message.Work+5L*n+known+(current is null?11:0)+a+pendingCount),overhead=checked(1L+message.Work+n+changes.Count+a);
        long other=OtherPartialWorldBytes(),worldBytes=checked(other+totalState+64L*(pendingCount+n)+32L*(changes.Count+(changes.ContainsKey(plan.ObjectId)?0:1)));
        Require(worldBytes<=creature4PartialPolicy.MaximumWorldBytes&&storeWork+overhead<=creature4PartialPolicy.MaximumWorldWork,"Partial CREO4 global world byte/work reservation exhausted.");
        SceneCreatureView4PartialReplicationStore.Prepared? prepared=null;
        try
        {
            prepared=creature4Partial.PrepareApply(target!.Handle,current,plan);Require(prepared is not null,"Stale partial CREO4 preparation.");creature4Partial.CheckPrepared(prepared!);
            Require(prepared!.Snapshot.WorkUsed==storeWork&&prepared.Bytes==totalState&&prepared.Events.Length==pendingCount+n,"Partial CREO4 global prediction mismatch.");
            var staged=new Dictionary<ulong,ChangeSlot>(changes);staged[plan.ObjectId]=changes.TryGetValue(plan.ObjectId,out var slot)?slot with{Kind=slot.Kind|SceneWorldChangeKind.Replication}:new(SceneWorldChangeKind.Replication,next);
            var publications=new Dictionary<ulong,SceneCreatureView4PartialSnapshot>(partialPublications){[plan.ObjectId]=prepared.Snapshot};
            creature4Partial.CheckPrepared(prepared);creature4Partial.CommitPrepared(prepared);partialPublications=publications;changes=staged;mutationRevision=next;lastPartialWorldWork=storeWork+overhead;return true;
        }finally{if(prepared is not null)creature4Partial.ReleasePrepared(prepared);}
    }
    public bool ApplyCreatureView4PartialDelta(long generation,SceneWorldCreatureView4PartialSnapshot? expected,CreatureView4Delta plan)
    {Enter(true);try{EnsureOpen();if(generation!=Generation)return false;ArgumentNullException.ThrowIfNull(plan);Require(State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready,"Active partial CREO4 world required.");return ApplyCreature4PartialCore(plan,expected,true);}finally{busy=false;}}
    internal CreatureView4Delta ParseCreatureView4PartialDelta(ReplicationEnvelope envelope)
    {Enter();try{EnsureOpen();Require(creature4PartialPolicy is not null,"Partial CREO4 policy required.");return CreatureView4Codec.DecodeDelta(envelope,creature4PartialPolicy!.StateLimits.MessageLimits!);}finally{busy=false;}}
    public SceneWorldCreatureView4PartialEventBatch? DrainCreatureView4PartialEvents(long generation)
    {Enter(true);try{EnsureOpen();return generation==Generation&&creature4Partial is not null?new(identity,Generation,creature4Partial.DrainEvents()):null;}finally{busy=false;}}
    public bool AcknowledgeCreatureView4PartialEvents(SceneWorldCreatureView4PartialEventBatch batch)
    {Enter(true);try{ArgumentNullException.ThrowIfNull(batch);return !closed&&ReferenceEquals(batch.Owner,identity)&&batch.Generation==Generation&&creature4Partial?.Acknowledge(batch.Batch)==true;}finally{busy=false;}}
    public bool IsCurrentCreatureView4PartialEvent(SceneWorldCreatureView4PartialEventBatch batch,int index)
    {Enter();try{ArgumentNullException.ThrowIfNull(batch);return !closed&&ReferenceEquals(batch.Owner,identity)&&batch.Generation==Generation&&State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready&&creature4Partial?.IsCurrentEvent(batch.Batch,index)==true;}finally{busy=false;}}
    public bool CreatureView4FullEnabled=>creature4FullPolicy is not null;
    public long RetainedCreatureView4FullBytes {get{Enter();try{return checked((creature4Full?.RetainedStateBytes??0)+(creature4Full?.PendingEventBytes??0));}finally{busy=false;}}}
    private SceneWorldCreatureView4FullSnapshot? Creature4FullFor(ulong id)
    {var target=registry.Find(id);return target is not null&&creature4Full?.Find(target.Handle) is {} state?new(identity,Generation,state):null;}
    public SceneWorldCreatureView4FullSnapshot? FindCreatureView4Full(long generation,ulong id)
    {Enter();try{return !closed&&generation==Generation?Creature4FullFor(id):null;}finally{busy=false;}}
    private bool CurrentCreature4Full(SceneWorldCreatureView4FullSnapshot value)=>!closed&&ReferenceEquals(value.Owner,identity)&&value.Generation==Generation&&State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready&&registry.IsCurrent(value.Incarnation)&&registry.Find(value.Incarnation.ObjectId) is {} target&&ReferenceEquals(creature4Full?.Find(target.Handle),value.Publication);
    public bool IsCurrent(SceneWorldCreatureView4FullSnapshot value)
    {Enter();try{ArgumentNullException.ThrowIfNull(value);return CurrentCreature4Full(value);}finally{busy=false;}}
    private void ApplyCreature4FullCore(ulong id,CreatureView4Baseline? baseline,CreatureView4FullDelta? plan,SceneWorldCreatureView4FullSnapshot? expected)
    {
        Require(creature4Full is not null,"Typed CREO4 world required.");
        var target=registry.Find(id);Require(target is not null&&catalog.Require(target.Create.TemplateCrc).Recipe==SceneObjectResourceRecipe.CreatureDetail0,"Creature CREO4 recipe required.");
        if(plan is not null)Require(target!.Phase==SceneObjectPhase.Finalized,"Finalized CREO4 target required.");
        if(expected is not null)Require(CurrentCreature4Full(expected)&&expected.Incarnation==target!.Incarnation,"Current exact CREO4 world receipt required.");
        var current=plan is not null?creature4Full!.Find(target!.Handle):null;
        if(plan is not null)Require(current is not null,"Complete CREO4 baseline required.");
        ulong next=checked(mutationRevision+1);Require(changes.ContainsKey(id)||changes.Count<maximumPendingChanges,"Undelivered world change capacity exhausted.");
        var staged=new Dictionary<ulong,ChangeSlot>(changes);
        staged[id]=changes.TryGetValue(id,out var slot)?slot with {Kind=slot.Kind|SceneWorldChangeKind.Replication}:new(SceneWorldChangeKind.Replication,next);
        SceneCreatureView4FullReplicationStore.Prepared? prepared=null;
        try
        {
            prepared=baseline is not null?creature4Full!.PrepareSeed(target!.Handle,baseline):creature4Full!.PrepareApply(target!.Handle,current!,plan!);
            Require(prepared is not null,"Stale CREO4 preparation.");
            creature4Full!.CheckPrepared(prepared!);
            // All candidates and bounds are ready: callback-free assignments only from here.
            creature4Full.CommitPrepared(prepared!);changes=staged;mutationRevision=next;
        }finally{if(prepared is not null)creature4Full!.ReleasePrepared(prepared);}
    }
    public bool ApplyCreatureView4FullDelta(long generation,SceneWorldCreatureView4FullSnapshot expected,CreatureView4FullDelta plan)
    {Enter(true);try{EnsureOpen();if(generation!=Generation)return false;ArgumentNullException.ThrowIfNull(expected);if(!CurrentCreature4Full(expected))return false;ArgumentNullException.ThrowIfNull(plan);Require(plan.ObjectId==expected.Incarnation.ObjectId,"CREO4 plan/receipt mismatch.");ApplyCreature4FullCore(plan.ObjectId,null,plan,expected);return true;}finally{busy=false;}}
    internal CreatureView4FullDelta ParseCreatureView4FullDelta(ReplicationEnvelope envelope)
    {Enter();try{EnsureOpen();Require(creature4FullPolicy is not null,"Typed CREO4 policy required.");return CreatureView4FullCodec.Decode(envelope,creature4FullPolicy!.DeltaLimits);}finally{busy=false;}}
    public SceneWorldCreatureView4FullEventBatch? DrainCreatureView4FullEvents(long generation)
    {Enter(true);try{EnsureOpen();return generation==Generation&&creature4Full is not null?new(identity,Generation,creature4Full.DrainEvents()):null;}finally{busy=false;}}
    public bool AcknowledgeCreatureView4FullEvents(SceneWorldCreatureView4FullEventBatch batch)
    {Enter(true);try{ArgumentNullException.ThrowIfNull(batch);return !closed&&ReferenceEquals(batch.Owner,identity)&&batch.Generation==Generation&&creature4Full?.Acknowledge(batch.Batch)==true;}finally{busy=false;}}
    public bool IsCurrentCreatureView4FullEvent(SceneWorldCreatureView4FullEventBatch batch,int index)
    {Enter();try{ArgumentNullException.ThrowIfNull(batch);if(closed||!ReferenceEquals(batch.Owner,identity)||batch.Generation!=Generation||index<0||index>=batch.Events.Count||creature4Full?.IsCurrentBatch(batch.Batch)!=true)return false;var e=batch.Events[index];return registry.IsCurrent(e.Incarnation)&&registry.Find(e.Incarnation.ObjectId)?.Phase==SceneObjectPhase.Finalized;}finally{busy=false;}}
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
                ReplicationFor(o.Object.Handle.ObjectId)?.Revision==o.Replication?.Revision&&TangibleFor(o.Object.Handle.ObjectId)?.Revision==o.TangibleReplication?.Revision&&Tangible6For(o.Object.Handle.ObjectId)?.Revision==o.TangibleView6?.Revision&&Creature6For(o.Object.Handle.ObjectId)?.Revision==o.CreatureView6?.Revision&&Creature6CompositeFor(o.Object.Handle.ObjectId)?.Revision==o.CreatureView6Composite?.Revision&&Creature6FullFor(o.Object.Handle.ObjectId)?.Revision==o.CreatureView6Full?.Revision&&Creature4FullFor(o.Object.Handle.ObjectId)?.Revision==o.CreatureView4Full?.Revision&&Creature4PartialFor(o.Object.Handle.ObjectId)?.Revision==o.CreatureView4Partial?.Revision);
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
        Enter(); try { return Array.AsReadOnly(registry.Snapshots().Select(s => new SceneWorldObjectSnapshot(s, placements[s.Handle.ObjectId], Array.AsReadOnly(envelopes.GetValueOrDefault(s.Handle.ObjectId)?.ToArray() ?? Array.Empty<ReplicationEnvelope>())) { Replication = ReplicationFor(s.Handle.ObjectId),CreatureView6=Creature6For(s.Handle.ObjectId),CreatureView6Composite=Creature6CompositeFor(s.Handle.ObjectId),CreatureView4Full=Creature4FullFor(s.Handle.ObjectId),CreatureView4Partial=Creature4PartialFor(s.Handle.ObjectId),CreatureView6Full=Creature6FullFor(s.Handle.ObjectId),TangibleView6=Tangible6For(s.Handle.ObjectId),TangibleReplication=TangibleFor(s.Handle.ObjectId) }).ToArray()); }
        finally { busy = false; }
    }
    public void Close()
    {
        Enter(true); try { if (closed) return; transition.Close(); closed = true; registry.Close(); creature4Partial?.Close(); partialPublications.Clear(); lastPartialWorldWork=0; creature4Full?.Close(); creature6Full?.Close(); creature6Composite?.Close(); creature6?.Close(); tangible6?.Close(); tangible?.Close(); replication?.Close(); changes.Clear(); placements.Clear(); envelopes.Clear(); pending.Clear(); started.Clear(); initializer?.Dispose(); objectInitializer?.Dispose(); }
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
            ReplicationEnvelopeCodec.BaselineOpcode => objectOptions is not null||creature6 is not null||creature6Composite is not null||creature6Full is not null||creature4Full is not null||creature4Partial is not null ? DecodeObjectReplication(message, true) : replication is null ? (object)ReplicationEnvelopeCodec.DecodeBaseline(message, maximumPayloadBytes) :
                CreatureView3BaselineCodec.Decode(message, replicationPolicy!.BaselineLimits),
            ReplicationEnvelopeCodec.DeltaOpcode => objectOptions is not null||creature6 is not null||creature6Composite is not null||creature6Full is not null||creature4Full is not null||creature4Partial is not null ? DecodeObjectReplication(message, false) : ReplicationMode == SceneWorldReplicationMode.CreatureView3MixedComponents ? CreatureView3CompositeDeltaCodec.Decode(message, compositeLimits!, replicationPolicy!.WoundsPolicy) : replication is null ? (object)ReplicationEnvelopeCodec.DecodeDelta(message, maximumPayloadBytes) :
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
        if(creature&&creature4Partial is not null&&envelope.View==4)
        {
            Require(envelope.Kind==ReplicationEnvelopeKind.Delta,"Received-only partial CREO4 mode excludes full view4 baselines.");
            return CreatureView4Codec.DecodeDelta(envelope,creature4PartialPolicy!.StateLimits.MessageLimits!);
        }
        if(creature && creature4Full is not null&&envelope.View==4)
            return envelope.Kind==ReplicationEnvelopeKind.Baseline?(object)CreatureView4Codec.DecodeBaseline(envelope,creature4FullPolicy!.BaselineLimits):CreatureView4FullCodec.Decode(envelope,creature4FullPolicy!.DeltaLimits);
        if(creature && creature6Full is not null&&envelope.View==6)
            return envelope.Kind==ReplicationEnvelopeKind.Baseline?(object)CreatureView6Codec.DecodeBaseline(envelope,creature6FullPolicy!.BaselineLimits):CreatureView6FullCodec.Decode(envelope,creature6FullPolicy!.DeltaLimits);
        if(creature && creature6Composite is not null&&envelope.View==6)
            return envelope.Kind==ReplicationEnvelopeKind.Baseline?(object)CreatureView6Codec.DecodeBaseline(envelope,creature6CompositePolicy!.BaselineLimits):CreatureView6CompositeCodec.Decode(envelope,creature6CompositePolicy!.DeltaLimits);
        if(creature && creature6 is not null&&envelope.View==6)
            return envelope.Kind==ReplicationEnvelopeKind.Baseline?(object)CreatureView6Codec.DecodeBaseline(envelope,creature6Policy!.CodecLimits):CreatureView6Codec.DecodeDelta(envelope,creature6Policy!.CodecLimits);
        if(!creature && tangible6 is not null && envelope.View==6)
            return envelope.Kind==ReplicationEnvelopeKind.Baseline?(object)TangibleView6Codec.DecodeBaseline(envelope,tangible6Policy!.CodecLimits):TangibleView6Codec.DecodeDelta(envelope,tangible6Policy!.CodecLimits);
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
        if(value is CreatureView4Delta partial4){ApplyCreature4PartialCore(partial4,null,false);return;}
        if(value is CreatureView4Baseline b4){ApplyCreature4FullCore(b4.ObjectId,b4,null,null);return;}
        if(value is CreatureView4FullDelta d4){ApplyCreature4FullCore(d4.ObjectId,null,d4,null);return;}
        if(value is CreatureView6Baseline creatureBaseline6){if(creature6Full is not null)ApplyCreature6FullCore(creatureBaseline6.ObjectId,creatureBaseline6,null,null);else if(creature6Composite is not null)ApplyCreature6CompositeCore(creatureBaseline6.ObjectId,creatureBaseline6,null,null);else ApplyCreature6Core(creatureBaseline6.ObjectId,creatureBaseline6,null,null);return;}
        if(value is CreatureView6FullDelta creatureFullDelta6){ApplyCreature6FullCore(creatureFullDelta6.ObjectId,null,creatureFullDelta6,null);return;}
        if(value is CreatureView6CompositeDelta creatureCompositeDelta6){ApplyCreature6CompositeCore(creatureCompositeDelta6.ObjectId,null,creatureCompositeDelta6,null);return;}
        if(value is CreatureView6Delta creatureDelta6){ApplyCreature6Core(creatureDelta6.ObjectId,null,creatureDelta6,null);return;}
        if(value is TangibleView6Baseline baseline6){ApplyTangible6Core(baseline6.ObjectId,baseline6,null,null);return;}
        if(value is TangibleView6Delta delta6){ApplyTangible6Core(delta6.ObjectId,null,delta6,null);return;}
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
                if(tangible6 is not null&&registry.Find(end.ObjectId) is {} target6&&catalog.Require(target6.Create.TemplateCrc).Recipe==SceneObjectResourceRecipe.StaticAppearance)Require(tangible6.Find(target6.Handle) is not null,"Complete TANO6 baseline required before static end barrier.");
                if(creature6Full is not null&&registry.Find(end.ObjectId) is {} creatureFullTarget6&&catalog.Require(creatureFullTarget6.Create.TemplateCrc).Recipe==SceneObjectResourceRecipe.CreatureDetail0)Require(creature6Full.Find(creatureFullTarget6.Handle) is not null,"Complete full CREO6 baseline required before creature end barrier.");
                if(creature6Composite is not null&&registry.Find(end.ObjectId) is {} creatureCompositeTarget6&&catalog.Require(creatureCompositeTarget6.Create.TemplateCrc).Recipe==SceneObjectResourceRecipe.CreatureDetail0)Require(creature6Composite.Find(creatureCompositeTarget6.Handle) is not null,"Complete composite CREO6 baseline required before creature end barrier.");
                if(creature6 is not null&&registry.Find(end.ObjectId) is {} creatureTarget6&&catalog.Require(creatureTarget6.Create.TemplateCrc).Recipe==SceneObjectResourceRecipe.CreatureDetail0)Require(creature6.Find(creatureTarget6.Handle) is not null,"Complete CREO6 baseline required before creature end barrier.");
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
        _ = creature4Partial?.RetainedStateBytes; foreach(var id4 in partialPublications.Where(p=>!registry.IsCurrent(p.Value.Incarnation)).Select(p=>p.Key).ToArray())partialPublications.Remove(id4);
        _ = creature4Full?.RetainedStateBytes; _ = creature6Full?.RetainedStateBytes; _ = creature6Composite?.RetainedStateBytes;
        _ = creature6?.RetainedStateBytes;
        _ = tangible6?.RetainedStateBytes;
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
        registry.Reset(); _ = creature4Partial?.RetainedStateBytes; foreach(var id4 in partialPublications.Where(p=>!registry.IsCurrent(p.Value.Incarnation)).Select(p=>p.Key).ToArray())partialPublications.Remove(id4);
        _ = creature4Full?.RetainedStateBytes; _ = creature6Full?.RetainedStateBytes; _ = creature6Composite?.RetainedStateBytes; _ = creature6?.RetainedStateBytes;
        _ = tangible6?.RetainedStateBytes;
        partialPublications.Clear(); lastPartialWorldWork=0;
        if(creature4Partial is not null)creature4Partial.Acknowledge(creature4Partial.DrainEvents());
        if(creature4Full is not null)creature4Full.Acknowledge(creature4Full.DrainEvents());
        if(creature6Full is not null)creature6Full.Acknowledge(creature6Full.DrainEvents());
        if(creature6Composite is not null)creature6Composite.Acknowledge(creature6Composite.DrainEvents());
        if(creature6 is not null)creature6.Acknowledge(creature6.DrainEvents());
        if(tangible6 is not null)tangible6.Acknowledge(tangible6.DrainEvents());
        _ = tangible?.Snapshots(); _ = replication?.Snapshots(); changes.Clear(); placements.Clear(); envelopes.Clear(); pending.Clear(); envelopeCount = envelopeBytes = 0;
        foreach (ulong id in started) CancelResource(id);
        started.Clear();
    }
    private void Fail() { transition.Fail(Generation); ClearWorld(); }
    private void EnsureOpen() { ObjectDisposedException.ThrowIf(closed, this); }
    private void Enter(bool mutation=false) { if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("World serialized owner thread required."); if (busy){reentered=true;throw new InvalidOperationException("World owner is reentrant.");} if(mutation)callbackVersion=checked(callbackVersion+1);reentered=false;busy = true; }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}
