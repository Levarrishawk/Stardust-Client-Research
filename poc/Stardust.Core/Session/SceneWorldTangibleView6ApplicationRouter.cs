using System.Buffers.Binary;
using Stardust.Core.Protocol;
namespace Stardust.Core.Session;
public sealed record SceneWorldTangibleView6Policy(TangibleView6Limits CodecLimits,TangibleView6StateLimits StateLimits,SceneTangibleView6StoreLimits StoreLimits,ulong RevisionSeed=0);
public sealed class SceneWorldTangibleView6Snapshot
{
 internal SceneWorldTangibleView6Snapshot(object owner,long generation,SceneTangibleView6Snapshot publication){Owner=owner;Generation=generation;Publication=publication;}
 internal object Owner{get;} internal SceneTangibleView6Snapshot Publication{get;}
 public long Generation{get;} public SceneObjectIncarnation Incarnation=>Publication.Incarnation;
 public ulong Revision=>Publication.Revision; public TangibleView6State State=>Publication.State;
}
public sealed class SceneWorldTangibleView6EventBatch
{
 internal SceneWorldTangibleView6EventBatch(object owner,long generation,SceneTangibleView6EventBatch batch){Owner=owner;Generation=generation;Batch=batch;}
 internal object Owner{get;} internal SceneTangibleView6EventBatch Batch{get;}
 public long Generation{get;} public ulong StoreRevision=>Batch.StoreRevision; public IReadOnlyList<SceneTangibleView6EventRecord> Events=>Batch.Events;
}
/// <summary>Owned exact TANO6 delta ingress; borrowed world/downstream; callbacks precede preparation.</summary>
public sealed class SceneWorldTangibleView6ApplicationRouter:IDisposable
{
 private SceneWorldCoordinator? world;private Func<ReplicationEnvelope,TangibleView6Delta>? decoder;private Func<long,ReadOnlyMemory<byte>,bool>? downstream;
 private readonly int ownerThread=Environment.CurrentManagedThreadId,maximumPayloadBytes;private bool busy,closed,reentered;
 public SceneWorldTangibleView6ApplicationRouter(SceneWorldCoordinator world,Func<ReplicationEnvelope,TangibleView6Delta>? decoder=null,Func<long,ReadOnlyMemory<byte>,bool>? downstream=null,int maximumPayloadBytes=65536)
 {
  ArgumentNullException.ThrowIfNull(world);if(maximumPayloadBytes is <2 or >1048576)throw new ArgumentOutOfRangeException(nameof(maximumPayloadBytes));
  if(!world.TangibleView6Enabled)throw new ArgumentException("Typed TANO6 world required.");_=world.CaptureTangibleIngress();this.world=world;this.decoder=decoder;this.downstream=downstream??((g,m)=>world.Accept(g,m.Span));this.maximumPayloadBytes=maximumPayloadBytes;
 }
 private void Enter(){if(Environment.CurrentManagedThreadId!=ownerThread)throw new InvalidOperationException("TANO6 router owner thread required.");if(busy){reentered=true;throw new InvalidOperationException("TANO6 router reentry.");}busy=true;reentered=false;}
 public bool Accept(long generation,ReadOnlyMemory<byte> message)
 {
  Enter();byte[]? owned=null;try
  {
   ObjectDisposedException.ThrowIf(closed,this);if(generation!=world!.Generation)return false;
   if(message.Length<1||message.Length>(long)maximumPayloadBytes+ReplicationEnvelopeCodec.OuterHeaderLength)throw new InvalidDataException("TANO6 ingress budget.");
   owned=message.ToArray();var bytes=owned.AsSpan();bool candidate=bytes.Length>=ReplicationEnvelopeCodec.MinimumMessageLength&&BinaryPrimitives.ReadUInt16LittleEndian(bytes)==5&&BinaryPrimitives.ReadUInt32LittleEndian(bytes[2..])==ReplicationEnvelopeCodec.DeltaOpcode&&BinaryPrimitives.ReadUInt32LittleEndian(bytes[14..])==TangibleView6Codec.RawTag&&bytes[18]==6;
   if(!candidate){bool accepted=downstream!(generation,owned);if(reentered||closed||generation!=world.Generation)throw new InvalidOperationException("Downstream changed TANO6 ingress.");return accepted;}
   var envelope=ReplicationEnvelopeCodec.DecodeDelta(bytes,maximumPayloadBytes);var snapshot=world.Snapshots().SingleOrDefault(s=>s.Object.Handle.ObjectId==envelope.ObjectId);
   if(snapshot?.TangibleView6 is null||snapshot.Object.Phase!=SceneObjectPhase.Finalized||!world.IsCurrent(snapshot.TangibleView6))throw new InvalidDataException("Finalized seeded TANO6 target required.");
   var grammar=world.ParseTangibleView6Delta(envelope);var stamp=world.CaptureTangibleIngress();var plan=decoder is {} decode?decode(envelope):grammar;
   if(reentered||closed||!world.MatchesTangibleIngress(stamp)||!world.IsCurrent(snapshot.TangibleView6))throw new InvalidOperationException("TANO6 decoder changed world/journal or reentered ingress.");
   if(plan is null||plan.ObjectId!=envelope.ObjectId||plan.DeclaredCount!=envelope.DeclaredCount)throw new InvalidDataException("TANO6 plan/envelope mismatch.");
   return world.ApplyTangibleView6Delta(generation,snapshot.TangibleView6,plan);
  }catch{CloseCore();throw;}finally{if(owned is not null)Array.Clear(owned);busy=false;}
 }
 private void CloseCore(){closed=true;world=null;decoder=null;downstream=null;}
 public void Dispose(){Enter();try{CloseCore();}finally{busy=false;}}
}
