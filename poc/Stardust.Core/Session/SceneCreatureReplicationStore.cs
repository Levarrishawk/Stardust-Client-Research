using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

public sealed record SceneCreatureComponentsReplicationPolicy(int MaximumRawKeys, int MaximumElements);
public sealed record SceneCreatureMixedReplicationPolicy(CreatureWoundsApplicationPolicy Policy, int MaximumWoundsElements);
public sealed class SceneCreatureReplicationSnapshot
{
    internal SceneCreatureReplicationSnapshot(object owner, SceneObjectIncarnation incarnation, ulong revision,
        CreatureView3BaselineSnapshot baseline, CreatureView3ScalarState scalars, CreatureView3WoundsState? wounds = null, CreatureView3ComponentsState? components = null,
        CreatureView3TangibleScalarState? tangibleScalars = null, CreatureView3NameState? names = null)
    { Owner = owner; Incarnation = incarnation; Revision = revision; Baseline = baseline; Scalars = scalars; Wounds = wounds; Components = components; TangibleScalars = tangibleScalars ?? CreatureView3TangibleScalarState.FromBaseline(baseline); Names = names ?? CreatureView3NameState.FromBaseline(baseline); }
    internal object Owner { get; }
    public SceneObjectIncarnation Incarnation { get; }
    public ulong Revision { get; }
    public CreatureView3BaselineSnapshot Baseline { get; }
    public CreatureView3ScalarState Scalars { get; }
    public CreatureView3WoundsState? Wounds { get; }
    public CreatureView3ComponentsState? Components { get; }
    public CreatureView3TangibleScalarState TangibleScalars { get; }
    public CreatureView3NameState Names { get; }
}

/// <summary>Modern immutable baseline plus scalar or opt-in mixed overlay, bound to stable registry incarnation.</summary>
public sealed class SceneCreatureReplicationStore : IDisposable
{
    private readonly SceneObjectRegistry registry;
    private readonly int capacity, ownerThread = Environment.CurrentManagedThreadId;
    private readonly long maximumDecodedBytes;
    private readonly SceneCreatureMixedReplicationPolicy? mixedPolicy;
    private readonly SceneCreatureComponentsReplicationPolicy? componentsPolicy;
    private readonly object identity = new();
    private SortedDictionary<ulong, SceneCreatureReplicationSnapshot> entries = new();
    private ulong revision;
    private long retainedDecodedBytes;
    private bool busy, closed;
    public SceneCreatureReplicationStore(SceneObjectRegistry registry, int capacity = 64,
        long maximumDecodedBytes = 16 * 1024 * 1024, ulong revisionSeed = 0, SceneCreatureMixedReplicationPolicy? mixedPolicy = null, SceneCreatureComponentsReplicationPolicy? componentsPolicy = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        if (capacity is < 1 or > 4096 || maximumDecodedBytes is < CreatureView3BaselineCodec.FixedDecodedBytes or > 128 * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        if (mixedPolicy is not null && (mixedPolicy.Policy is not (CreatureWoundsApplicationPolicy.StrictInterval or CreatureWoundsApplicationPolicy.LegacyBounded) ||
            mixedPolicy.MaximumWoundsElements is < 0 or > 262144)) throw new ArgumentOutOfRangeException(nameof(mixedPolicy));
        if (componentsPolicy is not null && (componentsPolicy.MaximumRawKeys is < 0 or > 262144 || componentsPolicy.MaximumElements is < 0 or > 262144))
            throw new ArgumentOutOfRangeException(nameof(componentsPolicy));
        this.componentsPolicy = componentsPolicy;
        this.mixedPolicy = mixedPolicy;
        this.registry = registry; this.capacity = capacity; this.maximumDecodedBytes = maximumDecodedBytes; revision = revisionSeed;
    }
    public long RetainedDecodedBytes
    {
        get { Enter(); try { RetireStale(); return retainedDecodedBytes; } finally { busy = false; } }
    }
    public SceneCreatureReplicationSnapshot? Seed(SceneObjectIncarnation incarnation, CreatureView3BaselineSnapshot baseline)
    {
        Enter(); try
        {
            EnsureOpen(); RetireStale(); if (!registry.IsCurrent(incarnation)) return null;
            ArgumentNullException.ThrowIfNull(baseline);
            if (baseline.ObjectId != incarnation.ObjectId) throw new InvalidDataException("Baseline object does not match caller incarnation.");
            entries.TryGetValue(incarnation.ObjectId, out var old);
            if (old is null && entries.Count >= capacity) throw new InvalidDataException("Replication store capacity exhausted.");
            var wounds = mixedPolicy is null ? null : CreatureView3WoundsState.FromBaseline(baseline, mixedPolicy.MaximumWoundsElements);
            var components = componentsPolicy is null ? null : CreatureView3ComponentsSnapshot.FromBaseline(baseline, componentsPolicy.MaximumRawKeys).ToState(componentsPolicy.MaximumElements);
            long otherBytes = retainedDecodedBytes - (old is null ? 0 : Charge(old.Baseline, old.Wounds, old.Components,old.Names));
            long bytes = Charge(baseline, wounds, components);
            if (bytes > maximumDecodedBytes - otherBytes) throw new InvalidDataException("Retained baseline/current-wounds decoded-byte budget exhausted.");
            ulong next = checked(revision + 1);
            var candidate = new SceneCreatureReplicationSnapshot(identity, incarnation, next, baseline, baseline.Scalars with { }, wounds, components);
            entries[incarnation.ObjectId] = candidate; retainedDecodedBytes = otherBytes + bytes; revision = next; return candidate;
        } finally { busy = false; }
    }
    public SceneCreatureReplicationSnapshot? Apply(SceneObjectIncarnation incarnation, CreatureView3ScalarDeltaPlan plan)
    {
        Enter(); try
        {
            EnsureOpen(); RetireStale(); if (!registry.IsCurrent(incarnation)) return null;
            ArgumentNullException.ThrowIfNull(plan);
            if (plan.ObjectId != incarnation.ObjectId) throw new InvalidDataException("Delta object does not match caller incarnation.");
            if (!entries.TryGetValue(incarnation.ObjectId, out var current) || current.Incarnation != incarnation)
                throw new InvalidOperationException("Complete baseline seed required for this incarnation.");
            var scalars = plan.Apply(current.Scalars); ulong next = checked(revision + 1);
            var candidate = new SceneCreatureReplicationSnapshot(identity, incarnation, next, current.Baseline, scalars, current.Wounds, current.Components,current.TangibleScalars,current.Names);
            entries[incarnation.ObjectId] = candidate; revision = next; return candidate;
        } finally { busy = false; }
    }
    public SceneCreatureReplicationSnapshot? Apply(SceneObjectIncarnation incarnation, CreatureView3MixedDeltaPlan plan)
    {
        Enter(); try
        {
            EnsureOpen(); RetireStale(); if (!registry.IsCurrent(incarnation)) return null;
            ArgumentNullException.ThrowIfNull(plan);
            if (mixedPolicy is null) throw new InvalidOperationException("Explicit mixed configuration required.");
            if (plan.ObjectId != incarnation.ObjectId || plan.Policy != mixedPolicy.Policy || plan.MaximumWoundsElements != mixedPolicy.MaximumWoundsElements)
                throw new InvalidDataException("Mixed plan identity/policy/element limit mismatch.");
            if (!entries.TryGetValue(incarnation.ObjectId, out var current) || current.Incarnation != incarnation || current.Wounds is null)
                throw new InvalidOperationException("Complete mixed baseline seed required for this incarnation.");
            var state = plan.Apply(new CreatureView3MixedState(incarnation.ObjectId, current.Scalars, current.Wounds));
            long otherBytes = retainedDecodedBytes - Charge(current.Baseline, current.Wounds, current.Components,current.Names);
            long bytes = Charge(current.Baseline, state.Wounds, current.Components,current.Names);
            if (bytes > maximumDecodedBytes - otherBytes) throw new InvalidDataException("Retained baseline/current-wounds decoded-byte budget exhausted.");
            ulong next = checked(revision + 1);
            var candidate = new SceneCreatureReplicationSnapshot(identity, incarnation, next, current.Baseline, state.Scalars, state.Wounds, current.Components,current.TangibleScalars,current.Names);
            entries[incarnation.ObjectId] = candidate; retainedDecodedBytes = otherBytes + bytes; revision = next; return candidate;
        } finally { busy = false; }
    }
    public SceneCreatureReplicationSnapshot? Apply(SceneObjectIncarnation incarnation, CreatureView3ComponentsDeltaPlan plan)
    {
        Enter(); try
        {
            EnsureOpen(); RetireStale(); if (!registry.IsCurrent(incarnation)) return null;
            ArgumentNullException.ThrowIfNull(plan);
            if (componentsPolicy is null) throw new InvalidOperationException("Explicit components configuration required.");
            if (plan.ObjectId != incarnation.ObjectId || plan.MaximumElements != componentsPolicy.MaximumElements)
                throw new InvalidDataException("Components plan identity/element limit mismatch.");
            if (!entries.TryGetValue(incarnation.ObjectId, out var current) || current.Incarnation != incarnation || current.Components is null)
                throw new InvalidOperationException("Complete components baseline seed required for this incarnation.");
            var components = plan.Apply(current.Components);
            long otherBytes = retainedDecodedBytes - Charge(current.Baseline, current.Wounds, current.Components,current.Names);
            long bytes = Charge(current.Baseline, current.Wounds, components,current.Names);
            if (bytes > maximumDecodedBytes - otherBytes) throw new InvalidDataException("Retained replication decoded-byte budget exhausted.");
            ulong next = checked(revision + 1);
            var candidate = new SceneCreatureReplicationSnapshot(identity, incarnation, next, current.Baseline, current.Scalars, current.Wounds, components,current.TangibleScalars,current.Names);
            entries[incarnation.ObjectId] = candidate; retainedDecodedBytes = otherBytes + bytes; revision = next; return candidate;
        } finally { busy = false; }
    }
    public SceneCreatureReplicationSnapshot? Apply(SceneObjectIncarnation incarnation, CreatureView3CompositeDeltaPlan plan)
    {
        Enter(); try
        {
            EnsureOpen(); RetireStale(); if (!registry.IsCurrent(incarnation)) return null;
            ArgumentNullException.ThrowIfNull(plan);
            if (mixedPolicy is null || componentsPolicy is null) throw new InvalidOperationException("Explicit wounds and components configurations required.");
            if (plan.ObjectId != incarnation.ObjectId || plan.Policy != mixedPolicy.Policy ||
                plan.MaximumWoundsElements != mixedPolicy.MaximumWoundsElements || plan.MaximumComponentsElements != componentsPolicy.MaximumElements)
                throw new InvalidDataException("Composite plan identity/policy/element limits mismatch.");
            if (!entries.TryGetValue(incarnation.ObjectId, out var current) || current.Incarnation != incarnation || current.Wounds is null || current.Components is null)
                throw new InvalidOperationException("Complete composite baseline seed required for this incarnation.");
            var raw = CreatureView3ComponentsSnapshot.FromBaseline(current.Baseline, componentsPolicy.MaximumRawKeys);
            var state = plan.Apply(new CreatureView3CompositeState(incarnation.ObjectId, current.Scalars, current.Wounds, current.Components, raw));
            long otherBytes = retainedDecodedBytes - Charge(current.Baseline, current.Wounds, current.Components,current.Names);
            long bytes = Charge(current.Baseline, state.Wounds, state.Components,current.Names);
            if (bytes > maximumDecodedBytes - otherBytes) throw new InvalidDataException("Retained composite decoded-byte budget exhausted.");
            ulong next = checked(revision + 1);
            var candidate = new SceneCreatureReplicationSnapshot(identity, incarnation, next, current.Baseline, state.Scalars, state.Wounds, state.Components,current.TangibleScalars,current.Names);
            entries[incarnation.ObjectId] = candidate; retainedDecodedBytes = otherBytes + bytes; revision = next; return candidate;
        } finally { busy = false; }
    }
    // Used only by the coordinator transaction: inspection must not retire unrelated state on rejection.
    internal SceneCreatureReplicationSnapshot? InspectCurrent(SceneObjectIncarnation incarnation)
    {
        Enter();try{return !closed && registry.IsCurrent(incarnation) && entries.TryGetValue(incarnation.ObjectId,out var value) && value.Incarnation==incarnation?value:null;}
        finally{busy=false;}
    }
    internal sealed class PreparedScalars
    {
        internal PreparedScalars(SceneCreatureReplicationStore owner, SortedDictionary<ulong, SceneCreatureReplicationSnapshot> entries, ulong revision,long retainedBytes)
        { Owner=owner; Entries=entries; Revision=revision;RetainedBytes=retainedBytes; }
        internal SceneCreatureReplicationStore Owner {get;}
        internal SortedDictionary<ulong, SceneCreatureReplicationSnapshot> Entries {get;}
        internal ulong Revision {get;}
        internal long RetainedBytes {get;}
    }
    internal PreparedScalars PrepareScalars(SceneCreatureReplicationSnapshot current, IReadOnlyList<CreatureView3ScalarUpdate> updates)
    {
        Enter();try
        {
            EnsureOpen();
            if(!ReferenceEquals(current.Owner,identity) || !registry.IsCurrent(current.Incarnation) ||
                !entries.TryGetValue(current.Incarnation.ObjectId,out var latest) || !ReferenceEquals(current,latest))
                throw new InvalidDataException("Current owned replication seed required.");
            var plan=new CreatureView3ScalarDeltaPlan(current.Incarnation.ObjectId,0,updates.ToList());
            var scalars=plan.Apply(current.Scalars);ulong next=checked(revision+1);
            long bytes=Charge(current.Baseline,current.Wounds,current.Components,current.Names);
            if(bytes>maximumDecodedBytes || retainedDecodedBytes>maximumDecodedBytes || entries.Count>capacity)
                throw new InvalidDataException("Replication capacity exhausted.");
            var candidate=new SceneCreatureReplicationSnapshot(identity,current.Incarnation,next,current.Baseline,scalars,current.Wounds,current.Components,current.TangibleScalars,current.Names);
            var staged=new SortedDictionary<ulong,SceneCreatureReplicationSnapshot>(entries){[current.Incarnation.ObjectId]=candidate};
            return new(this,staged,next,retainedDecodedBytes);
        }
        catch{busy=false;throw;}
    }
    internal PreparedScalars PrepareFull(SceneCreatureReplicationSnapshot current,CreatureView3FullDeltaPlan plan) =>
        PrepareExtended(current,new CreatureView3ExtendedDeltaPlan(plan.ObjectId,plan.DeclaredCount,
            plan.Updates.Select(CreatureView3ExtendedDeltaUpdate.FromFull).ToArray(),plan.WoundsPolicy,plan.MaximumWoundsElements,plan.MaximumComponentsElements));
    internal PreparedScalars PrepareExtended(SceneCreatureReplicationSnapshot current,CreatureView3ExtendedDeltaPlan plan) =>
        PrepareNamed(current,new CreatureView3NamedDeltaPlan(plan.ObjectId,plan.DeclaredCount,
            plan.Updates.Select(CreatureView3NamedDeltaUpdate.FromExtended).ToArray(),plan.WoundsPolicy,plan.MaximumWoundsElements,plan.MaximumComponentsElements));
    internal PreparedScalars PrepareNamed(SceneCreatureReplicationSnapshot current,CreatureView3NamedDeltaPlan plan)
    {
        Enter();try
        {
            EnsureOpen();
            if(!ReferenceEquals(current.Owner,identity) || !registry.IsCurrent(current.Incarnation) ||
                !entries.TryGetValue(current.Incarnation.ObjectId,out var latest) || !ReferenceEquals(current,latest) || plan.ObjectId!=current.Incarnation.ObjectId)
                throw new InvalidDataException("Current owned full replication seed required.");
            bool hasWounds=plan.Updates.Any(u=>u.Extended?.Full?.Wounds is not null),hasComponents=plan.Updates.Any(u=>u.Extended?.Full?.Components is not null);
            if(hasWounds && (mixedPolicy is null || current.Wounds is null || plan.WoundsPolicy!=mixedPolicy.Policy || plan.MaximumWoundsElements!=mixedPolicy.MaximumWoundsElements))
                throw new InvalidDataException("Full wounds configuration/policy/element mismatch.");
            if(hasComponents && (componentsPolicy is null || current.Components is null || plan.MaximumComponentsElements!=componentsPolicy.MaximumElements))
                throw new InvalidDataException("Full components configuration/element mismatch.");
            ulong next=checked(revision+1);var scalars=current.Scalars;var tangible=current.TangibleScalars;var names=current.Names;var wounds=current.Wounds;var components=current.Components;
            long other=checked(retainedDecodedBytes-Charge(current.Baseline,wounds,components,names)),bytes=Charge(current.Baseline,wounds,components,names);
            if(entries.Count>capacity)throw new InvalidDataException("Replication actor capacity.");
            foreach(var named in plan.Updates)
            {
                if(named.Name is {} name)
                {
                    names=names.Apply(name);bytes=Charge(current.Baseline,wounds,components,names);
                    if(bytes>maximumDecodedBytes-other)throw new InvalidDataException("Intermediate current names decoded-byte capacity.");
                    continue;
                }
                var item=named.Extended!;
                if(item.Tangible is {} common){tangible=tangible.Apply(common);continue;}
                var update=item.Full!;
                if(update.Scalar is {} scalar)scalars=new CreatureView3ScalarDeltaPlan(plan.ObjectId,plan.DeclaredCount,new(){scalar}).Apply(scalars);
                else if(update.Wounds is {} wound)
                {
                    long fixedBytes=checked(names.AdditionalDecodedBytes(current.Baseline)+current.Baseline.DecodedBytes+16+(components is null?0:16+4L*components.Keys.Count));
                    wounds=new CreatureView3WoundsDeltaPlan(plan.ObjectId,plan.DeclaredCount,new[]{wound},plan.MaximumWoundsElements,plan.WoundsPolicy).Apply(wounds!,count=>
                    {
                        if(checked(fixedBytes+4L*count)>maximumDecodedBytes-other)throw new InvalidDataException("Intermediate wounds decoded-byte capacity.");
                    });
                }
                else if(update.Components is {} component)
                {
                    long fixedBytes=checked(names.AdditionalDecodedBytes(current.Baseline)+current.Baseline.DecodedBytes+16+(wounds is null?0:16+4L*wounds.Values.Count));
                    components=new CreatureView3ComponentsDeltaPlan(plan.ObjectId,plan.DeclaredCount,new[]{component},plan.MaximumComponentsElements).Apply(components!,count=>
                    {
                        if(checked(fixedBytes+4L*count)>maximumDecodedBytes-other)throw new InvalidDataException("Intermediate components decoded-byte capacity.");
                    });
                }
                else continue;
                bytes=Charge(current.Baseline,wounds,components,names);
                if(bytes>maximumDecodedBytes-other)throw new InvalidDataException("Intermediate retained replication decoded-byte capacity.");
            }
            var candidate=new SceneCreatureReplicationSnapshot(identity,current.Incarnation,next,current.Baseline,scalars,wounds,components,tangible,names);
            var staged=new SortedDictionary<ulong,SceneCreatureReplicationSnapshot>(entries){[plan.ObjectId]=candidate};
            return new(this,staged,next,checked(other+bytes));
        }
        catch{busy=false;throw;}
    }
    internal void CommitScalars(PreparedScalars token)
    { entries=token.Entries;revision=token.Revision;retainedDecodedBytes=token.RetainedBytes; }
    internal void ReleaseScalars(PreparedScalars token){busy=false;}
    private static long Charge(CreatureView3BaselineSnapshot baseline, CreatureView3WoundsState? wounds, CreatureView3ComponentsState? components,CreatureView3NameState? names=null) =>
        checked((names?.AdditionalDecodedBytes(baseline)??0)+baseline.DecodedBytes + (wounds is null ? 0 : 16 + 4L * wounds.Values.Count) + (components is null ? 0 : 16 + 4L * components.Keys.Count));
    public SceneCreatureReplicationSnapshot? Find(SceneObjectIncarnation incarnation)
    {
        Enter(); try { RetireStale(); return !closed && registry.IsCurrent(incarnation) && entries.TryGetValue(incarnation.ObjectId, out var value) && value.Incarnation == incarnation ? value : null; }
        finally { busy = false; }
    }
    public bool IsCurrent(SceneCreatureReplicationSnapshot snapshot)
    {
        Enter(); try
        {
            ArgumentNullException.ThrowIfNull(snapshot); RetireStale();
            return !closed && ReferenceEquals(snapshot.Owner, identity) && registry.IsCurrent(snapshot.Incarnation) &&
                entries.TryGetValue(snapshot.Incarnation.ObjectId, out var current) && current.Incarnation == snapshot.Incarnation && current.Revision == snapshot.Revision;
        } finally { busy = false; }
    }
    public IReadOnlyList<SceneCreatureReplicationSnapshot> Snapshots()
    {
        Enter(); try { RetireStale(); return Array.AsReadOnly(entries.Values.ToArray()); }
        finally { busy = false; }
    }
    public void Close()
    {
        Enter(); try { closed = true; entries.Clear(); retainedDecodedBytes = 0; }
        finally { busy = false; }
    }
    public void Dispose() => Close();
    private void RetireStale()
    {
        foreach (ulong id in entries.Where(row => !registry.IsCurrent(row.Value.Incarnation)).Select(row => row.Key).ToArray())
        { retainedDecodedBytes -= Charge(entries[id].Baseline, entries[id].Wounds, entries[id].Components,entries[id].Names); entries.Remove(id); }
    }
    private void EnsureOpen() => ObjectDisposedException.ThrowIf(closed, this);
    private void Enter()
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Replication store serialized owner thread required.");
        if (busy) throw new InvalidOperationException("Replication store owner is reentrant.");
        busy = true;
    }
}
