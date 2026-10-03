using System.Buffers.Binary;
using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

/// <summary>Optional full creature ingress over borrowed world/store; strict inner parsing is injected.</summary>
public sealed class SceneWorldFullCreatureApplicationRouter : IDisposable
{
    private SceneWorldCoordinator? world;
    private SceneWorldCustomizationStateStore? store;
    private Func<ReplicationEnvelope,CreatureView3FullDeltaPlan>? decoder;
    private Func<long,ReadOnlyMemory<byte>,bool>? downstream;
    private readonly int ownerThread=Environment.CurrentManagedThreadId, maximumPayloadBytes;
    private bool busy,closed,reentered;
    public SceneWorldFullCreatureApplicationRouter(SceneWorldCoordinator world,SceneWorldCustomizationStateStore store,
        Func<ReplicationEnvelope,CreatureView3FullDeltaPlan> decoder,Func<long,ReadOnlyMemory<byte>,bool>? downstream=null,int maximumPayloadBytes=65536)
    {
        ArgumentNullException.ThrowIfNull(world);ArgumentNullException.ThrowIfNull(store);ArgumentNullException.ThrowIfNull(decoder);
        if(maximumPayloadBytes is <2 or >1048576)throw new ArgumentOutOfRangeException(nameof(maximumPayloadBytes));
        _=world.Snapshots();if(!store.Borrows(world))throw new ArgumentException("Store must borrow this open world.");
        this.world=world;this.store=store;this.decoder=decoder;this.maximumPayloadBytes=maximumPayloadBytes;
        this.downstream=downstream??((g,m)=>world.Accept(g,m.Span));
    }
    private void Enter(){if(Environment.CurrentManagedThreadId!=ownerThread)throw new InvalidOperationException("Customization router owner thread required.");if(busy){reentered=true;throw new InvalidOperationException("Customization router reentrancy.");}busy=true;}
    private void Healthy(long generation,ulong sourceMutation)
    {
        if(reentered || closed || world!.Generation!=generation || store!.IsClosed || store.MutationVersion!=sourceMutation)
            throw new InvalidOperationException("Customization callback changed source/generation or reentered ingress.");
    }
    public bool Accept(long generation,ReadOnlyMemory<byte> message)
    {
        Enter();byte[]? owned=null;try
        {
            ObjectDisposedException.ThrowIf(closed,this);if(generation!=world!.Generation)return false; // Before any parse/copy/callback.
            if(message.Length is <1 || message.Length>(long)maximumPayloadBytes+ReplicationEnvelopeCodec.OuterHeaderLength)throw new InvalidDataException("Customization ingress message budget.");
            owned=message.ToArray();var bytes=owned.AsSpan();ulong sourceMutation=store!.MutationVersion;
            bool candidate=bytes.Length>=ReplicationEnvelopeCodec.MinimumMessageLength && BinaryPrimitives.ReadUInt16LittleEndian(bytes)==5 &&
                BinaryPrimitives.ReadUInt32LittleEndian(bytes[2..])==ReplicationEnvelopeCodec.DeltaOpcode && BinaryPrimitives.ReadUInt32LittleEndian(bytes[14..])==CreatureView3BaselineCodec.RawTag && bytes[18]==3;
            if(candidate)
            {
                var envelope=ReplicationEnvelopeCodec.DecodeDelta(bytes,maximumPayloadBytes);
                if(envelope.OpaqueFieldLength>=2 && (BinaryPrimitives.ReadUInt16LittleEndian(envelope.Fields) is 4 or 5 or >=11 and <=17))
                {
                    var snapshot=world.Snapshots().SingleOrDefault(s=>s.Object.Handle.ObjectId==envelope.ObjectId);
                    if(snapshot?.Replication is null || !world.IsCurrent(snapshot.Replication) || snapshot.Object.Phase!=SceneObjectPhase.Finalized)throw new InvalidDataException("Current complete creature target required.");
                    var before=world.DrainChanges(generation) ?? throw new InvalidDataException("Active customization world required.");
                    var plan=decoder!(envelope) ?? throw new InvalidDataException("Null customization plan.");Healthy(generation,sourceMutation);
                    var after=world.DrainChanges(generation);
                    bool samePending=after is not null && after.Entries.Count==before.Entries.Count && before.Entries.Zip(after.Entries).All(p=>p.First.ObjectId==p.Second.ObjectId && p.First.Kind==p.Second.Kind && ReferenceEquals(p.First.Current?.Object,p.Second.Current?.Object) && p.First.Current?.Replication?.Revision==p.Second.Current?.Replication?.Revision);
                    if(!samePending || !world.IsCurrent(before) || !world.IsCurrent(snapshot.Replication) || world.Snapshots().SingleOrDefault(s=>s.Object.Handle.ObjectId==envelope.ObjectId)?.Object!=snapshot.Object)
                        throw new InvalidOperationException("Customization decoder mutated the world target.");
                    if(plan.ObjectId!=envelope.ObjectId || plan.DeclaredCount!=envelope.DeclaredCount)throw new InvalidDataException("Decoded plan/envelope identity mismatch.");
                    return world.ApplyFullCreatureDelta(generation,plan,store);
                }
            }
            bool accepted=downstream!(generation,owned);Healthy(generation,sourceMutation);return accepted;
        }
        catch{CloseCore();throw;}
        finally{if(owned is not null)Array.Clear(owned);busy=false;}
    }
    private void CloseCore(){closed=true;decoder=null;downstream=null;world=null;store=null;}
    public void Dispose(){Enter();try{CloseCore();}finally{busy=false;}}
}
