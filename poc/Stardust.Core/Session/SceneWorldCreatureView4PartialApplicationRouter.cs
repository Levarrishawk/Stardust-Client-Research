using System.Buffers.Binary;
using Stardust.Core.Protocol;
namespace Stardust.Core.Session;
public sealed record SceneWorldCreatureView4PartialPolicy(CreatureView4PartialLimits StateLimits,SceneCreatureView4PartialStoreLimits StoreLimits,ulong RevisionSeed=0,long MaximumWorldBytes=134217728,long MaximumWorldWork=16777216);
public sealed class SceneWorldCreatureView4PartialSnapshot
{
 internal SceneWorldCreatureView4PartialSnapshot(object owner,long generation,SceneCreatureView4PartialSnapshot publication){Owner=owner;Generation=generation;Publication=publication;}
 internal object Owner{get;} internal SceneCreatureView4PartialSnapshot Publication{get;}
 public long Generation{get;} public SceneObjectIncarnation Incarnation=>Publication.Incarnation;
 public ulong Revision=>Publication.Revision; public long WorkUsed=>Publication.WorkUsed; public CreatureView4PartialState State=>Publication.State;
}
public sealed class SceneWorldCreatureView4PartialEventBatch
{
 internal SceneWorldCreatureView4PartialEventBatch(object owner,long generation,SceneCreatureView4PartialEventBatch batch){Owner=owner;Generation=generation;Batch=batch;}
 internal object Owner{get;} internal SceneCreatureView4PartialEventBatch Batch{get;}
 public long Generation{get;} public ulong StoreRevision=>Batch.StoreRevision; public IReadOnlyList<SceneCreatureView4PartialEventRecord> Events=>Batch.Events;
}
/// <summary>Owned exact CREO4 delta ingress; borrowed world/downstream; callbacks precede preparation.</summary>
public sealed class SceneWorldCreatureView4PartialApplicationRouter:IDisposable
{
 private SceneWorldCoordinator? world;private Func<ReplicationEnvelope,CreatureView4Delta>? decoder;private Func<long,ReadOnlyMemory<byte>,bool>? downstream;
 private readonly int ownerThread=Environment.CurrentManagedThreadId,maximumPayloadBytes;private bool busy,closed,reentered;
 public SceneWorldCreatureView4PartialApplicationRouter(SceneWorldCoordinator world,Func<ReplicationEnvelope,CreatureView4Delta>? decoder=null,Func<long,ReadOnlyMemory<byte>,bool>? downstream=null,int maximumPayloadBytes=65536)
 {
  ArgumentNullException.ThrowIfNull(world);if(maximumPayloadBytes is <2 or >1048576)throw new ArgumentOutOfRangeException(nameof(maximumPayloadBytes));
  if(!world.CreatureView4PartialEnabled)throw new ArgumentException("Typed CREO4 world required.");_=world.CaptureTangibleIngress();this.world=world;this.decoder=decoder;this.downstream=downstream??((g,m)=>world.Accept(g,m.Span));this.maximumPayloadBytes=maximumPayloadBytes;
 }
 private void Enter(){if(Environment.CurrentManagedThreadId!=ownerThread)throw new InvalidOperationException("CREO4 router owner thread required.");if(busy){reentered=true;throw new InvalidOperationException("CREO4 router reentry.");}busy=true;reentered=false;}
 public bool Accept(long generation,ReadOnlyMemory<byte> message)
 {
  Enter();byte[]? owned=null;try
  {
   ObjectDisposedException.ThrowIf(closed,this);if(generation!=world!.Generation)return false;
   if(message.Length<1||message.Length>(long)maximumPayloadBytes+ReplicationEnvelopeCodec.OuterHeaderLength)throw new InvalidDataException("CREO4 ingress budget.");
   owned=message.ToArray();var bytes=owned.AsSpan();bool candidate=bytes.Length>=ReplicationEnvelopeCodec.MinimumMessageLength&&BinaryPrimitives.ReadUInt16LittleEndian(bytes)==5&&BinaryPrimitives.ReadUInt32LittleEndian(bytes[2..])==ReplicationEnvelopeCodec.DeltaOpcode&&BinaryPrimitives.ReadUInt32LittleEndian(bytes[14..])==CreatureView4Codec.RawTag&&bytes[18]==4;
   if(!candidate){bool accepted=downstream!(generation,owned);if(reentered||closed||generation!=world.Generation)throw new InvalidOperationException("Downstream changed CREO4 ingress.");return accepted;}
   // Initialization has no live registry yet: preserve owned byte staging and replay-time typed validation.
   // Optional decoder is active-ingress only; never invent a receipt for an unreplayed create.
   if(world.State==ZoneSceneTransitionState.Initializing)
   {
    bool staged=world.Accept(generation,owned);
    if(reentered||closed||generation!=world.Generation||world.State!=ZoneSceneTransitionState.Initializing)throw new InvalidOperationException("Partial CREO4 staging changed ingress.");
    return staged;
   }
   var envelope=ReplicationEnvelopeCodec.DecodeDelta(bytes,maximumPayloadBytes);var snapshot=world.Snapshots().SingleOrDefault(s=>s.Object.Handle.ObjectId==envelope.ObjectId);
   if(snapshot is null||snapshot.Object.Phase is not (SceneObjectPhase.PendingBaselines or SceneObjectPhase.Finalized))throw new InvalidDataException("Current pending/finalized partial CREO4 target required.");
   var grammar=world.ParseCreatureView4PartialDelta(envelope);var stamp=world.CaptureTangibleIngress();var plan=decoder is {} decode?decode(envelope):grammar;
   if(reentered||closed||!world.MatchesTangibleIngress(stamp)||(snapshot.CreatureView4Partial is not null&&!world.IsCurrent(snapshot.CreatureView4Partial)))throw new InvalidOperationException("CREO4 decoder changed world/journal or reentered ingress.");
   if(plan is null||plan.ObjectId!=envelope.ObjectId||plan.DeclaredCount!=envelope.DeclaredCount||!plan.Updates.Select(u=>(u.Index,u.Bits)).SequenceEqual(grammar.Updates.Select(u=>(u.Index,u.Bits))))throw new InvalidDataException("CREO4 plan/envelope mismatch.");
   return world.ApplyCreatureView4PartialDelta(generation,snapshot.CreatureView4Partial,plan);
  }catch{CloseCore();throw;}finally{if(owned is not null)Array.Clear(owned);busy=false;}
 }
 private void CloseCore(){closed=true;world=null;decoder=null;downstream=null;}
 public void Dispose(){Enter();try{CloseCore();}finally{busy=false;}}
}
