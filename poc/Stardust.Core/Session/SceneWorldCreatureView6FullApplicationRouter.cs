using System.Buffers.Binary;
using Stardust.Core.Protocol;
namespace Stardust.Core.Session;
public sealed record SceneWorldCreatureView6FullPolicy(CreatureView6Limits BaselineLimits,CreatureView6FullLimits DeltaLimits,CreatureView6FullStateLimits StateLimits,SceneCreatureView6FullStoreLimits StoreLimits,ulong RevisionSeed=0);
public sealed class SceneWorldCreatureView6FullSnapshot
{
 internal SceneWorldCreatureView6FullSnapshot(object owner,long generation,SceneCreatureView6FullSnapshot publication){Owner=owner;Generation=generation;Publication=publication;}
 internal object Owner{get;} internal SceneCreatureView6FullSnapshot Publication{get;}
 public long Generation{get;} public SceneObjectIncarnation Incarnation=>Publication.Incarnation;
 public ulong Revision=>Publication.Revision; public CreatureView6FullState State=>Publication.State; public CreatureView6Baseline Model=>Publication.State.Model;
}
public sealed class SceneWorldCreatureView6FullEventBatch
{
 internal SceneWorldCreatureView6FullEventBatch(object owner,long generation,SceneCreatureView6FullEventBatch batch){Owner=owner;Generation=generation;Batch=batch;}
 internal object Owner{get;} internal SceneCreatureView6FullEventBatch Batch{get;}
 public long Generation{get;} public ulong StoreRevision=>Batch.StoreRevision; public IReadOnlyList<SceneCreatureView6FullEventRecord> Events=>Batch.Events;
}
/// <summary>Owned exact CREO6 delta ingress; borrowed world/downstream; callbacks precede preparation.</summary>
public sealed class SceneWorldCreatureView6FullApplicationRouter:IDisposable
{
 private SceneWorldCoordinator? world;private Func<ReplicationEnvelope,CreatureView6FullDelta>? decoder;private Func<long,ReadOnlyMemory<byte>,bool>? downstream;
 private readonly int ownerThread=Environment.CurrentManagedThreadId,maximumPayloadBytes;private bool busy,closed,reentered;
 public SceneWorldCreatureView6FullApplicationRouter(SceneWorldCoordinator world,Func<ReplicationEnvelope,CreatureView6FullDelta>? decoder=null,Func<long,ReadOnlyMemory<byte>,bool>? downstream=null,int maximumPayloadBytes=65536)
 {
  ArgumentNullException.ThrowIfNull(world);if(maximumPayloadBytes is <2 or >1048576)throw new ArgumentOutOfRangeException(nameof(maximumPayloadBytes));
  if(!world.CreatureView6FullEnabled)throw new ArgumentException("Typed CREO6 world required.");_=world.CaptureTangibleIngress();this.world=world;this.decoder=decoder;this.downstream=downstream??((g,m)=>world.Accept(g,m.Span));this.maximumPayloadBytes=maximumPayloadBytes;
 }
 private void Enter(){if(Environment.CurrentManagedThreadId!=ownerThread)throw new InvalidOperationException("CREO6 router owner thread required.");if(busy){reentered=true;throw new InvalidOperationException("CREO6 router reentry.");}busy=true;reentered=false;}
 public bool Accept(long generation,ReadOnlyMemory<byte> message)
 {
  Enter();byte[]? owned=null;try
  {
   ObjectDisposedException.ThrowIf(closed,this);if(generation!=world!.Generation)return false;
   if(message.Length<1||message.Length>(long)maximumPayloadBytes+ReplicationEnvelopeCodec.OuterHeaderLength)throw new InvalidDataException("CREO6 ingress budget.");
   owned=message.ToArray();var bytes=owned.AsSpan();bool candidate=bytes.Length>=ReplicationEnvelopeCodec.MinimumMessageLength&&BinaryPrimitives.ReadUInt16LittleEndian(bytes)==5&&BinaryPrimitives.ReadUInt32LittleEndian(bytes[2..])==ReplicationEnvelopeCodec.DeltaOpcode&&BinaryPrimitives.ReadUInt32LittleEndian(bytes[14..])==CreatureView6Codec.RawTag&&bytes[18]==6;
   if(!candidate){bool accepted=downstream!(generation,owned);if(reentered||closed||generation!=world.Generation)throw new InvalidOperationException("Downstream changed CREO6 ingress.");return accepted;}
   var envelope=ReplicationEnvelopeCodec.DecodeDelta(bytes,maximumPayloadBytes);var snapshot=world.Snapshots().SingleOrDefault(s=>s.Object.Handle.ObjectId==envelope.ObjectId);
   if(snapshot?.CreatureView6Full is null||snapshot.Object.Phase!=SceneObjectPhase.Finalized||!world.IsCurrent(snapshot.CreatureView6Full))throw new InvalidDataException("Finalized seeded CREO6 target required.");
   var grammar=world.ParseCreatureView6FullDelta(envelope);var stamp=world.CaptureTangibleIngress();var plan=decoder is {} decode?decode(envelope):grammar;
   if(reentered||closed||!world.MatchesTangibleIngress(stamp)||!world.IsCurrent(snapshot.CreatureView6Full))throw new InvalidOperationException("CREO6 decoder changed world/journal or reentered ingress.");
   if(plan is null||plan.ObjectId!=envelope.ObjectId||plan.DeclaredCount!=envelope.DeclaredCount)throw new InvalidDataException("CREO6 plan/envelope mismatch.");
   return world.ApplyCreatureView6FullDelta(generation,snapshot.CreatureView6Full,plan);
  }catch{CloseCore();throw;}finally{if(owned is not null)Array.Clear(owned);busy=false;}
 }
 private void CloseCore(){closed=true;world=null;decoder=null;downstream=null;}
 public void Dispose(){Enter();try{CloseCore();}finally{busy=false;}}
}
