using System.Buffers.Binary;
using Stardust.Core.Protocol;
namespace Stardust.Core.Session;

/// <summary>Owned TANO3 delta ingress; borrows world and downstream, owns only its callback references.</summary>
public sealed class SceneWorldTangibleApplicationRouter:IDisposable
{
    private SceneWorldCoordinator? world;
    private Func<ReplicationEnvelope,TangibleView3DeltaPlan>? decoder;
    private Func<long,ReadOnlyMemory<byte>,bool>? downstream;
    private readonly int ownerThread=Environment.CurrentManagedThreadId,maximumPayloadBytes;
    private bool busy,closed,reentered;
    public SceneWorldTangibleApplicationRouter(SceneWorldCoordinator world,Func<ReplicationEnvelope,TangibleView3DeltaPlan>? decoder=null,
        Func<long,ReadOnlyMemory<byte>,bool>? downstream=null,int maximumPayloadBytes=65536)
    {
        ArgumentNullException.ThrowIfNull(world);if(maximumPayloadBytes is <2 or >1048576)throw new ArgumentOutOfRangeException(nameof(maximumPayloadBytes));
        if(!world.TangibleReplicationEnabled)throw new ArgumentException("Typed tangible world required.");
        _=world.CaptureTangibleIngress();this.world=world;this.decoder=decoder;
        this.downstream=downstream??((g,m)=>world.Accept(g,m.Span));this.maximumPayloadBytes=maximumPayloadBytes;
    }
    private void Enter(){if(Environment.CurrentManagedThreadId!=ownerThread)throw new InvalidOperationException("Tangible router owner thread required.");if(busy){reentered=true;throw new InvalidOperationException("Tangible router reentry.");}busy=true;reentered=false;}
    public bool Accept(long generation,ReadOnlyMemory<byte> message)
    {
        Enter();byte[]? owned=null;try
        {
            ObjectDisposedException.ThrowIf(closed,this);if(generation!=world!.Generation)return false;
            if(message.Length<1||message.Length>(long)maximumPayloadBytes+ReplicationEnvelopeCodec.OuterHeaderLength)throw new InvalidDataException("Tangible ingress budget.");
            owned=message.ToArray();var bytes=owned.AsSpan();
            bool candidate=bytes.Length>=ReplicationEnvelopeCodec.MinimumMessageLength&&BinaryPrimitives.ReadUInt16LittleEndian(bytes)==5&&
                BinaryPrimitives.ReadUInt32LittleEndian(bytes[2..])==ReplicationEnvelopeCodec.DeltaOpcode&&BinaryPrimitives.ReadUInt32LittleEndian(bytes[14..])==TangibleView3DeltaCodec.RawTag&&bytes[18]==3;
            if(!candidate){bool accepted=downstream!(generation,owned);if(reentered||closed||generation!=world.Generation)throw new InvalidOperationException("Downstream reentered tangible ingress or changed generation.");return accepted;}
            var envelope=ReplicationEnvelopeCodec.DecodeDelta(bytes,maximumPayloadBytes);
            var snapshot=world.Snapshots().SingleOrDefault(s=>s.Object.Handle.ObjectId==envelope.ObjectId);
            if(snapshot?.TangibleReplication is null||snapshot.Object.Phase!=SceneObjectPhase.Finalized||!world.IsCurrent(snapshot.TangibleReplication))throw new InvalidDataException("Finalized complete tangible required.");
            var grammar=world.ParseTangibleDeltaEnvelope(envelope);
            var stamp=world.CaptureTangibleIngress();var plan=decoder is {} decode?decode(envelope):world.DecodeTangibleDelta(envelope,grammar);
            if(reentered||closed||!world.MatchesTangibleIngress(stamp)||!world.IsCurrent(snapshot.TangibleReplication))throw new InvalidOperationException("Tangible decoder changed world/journal or reentered ingress.");
            if(plan is null||plan.ObjectId!=envelope.ObjectId||plan.DeclaredCount!=envelope.DeclaredCount)throw new InvalidDataException("Tangible plan/envelope mismatch.");
            return world.ApplyTangibleDelta(generation,plan);
        }
        catch{CloseCore();throw;}
        finally{if(owned is not null)Array.Clear(owned);busy=false;}
    }
    private void CloseCore(){closed=true;world=null;decoder=null;downstream=null;}
    public void Dispose(){Enter();try{CloseCore();}finally{busy=false;}}
}
