namespace Stardust.Core.Protocol;
public sealed record CreatureView6StateLimits(int MaximumUpdates=4096,int MaximumStringBytes=65536,int MaximumTotalStringBytes=262144,int MaximumElements=4096,int MaximumTotalElements=16384,long MaximumStateBytes=2097152,int MaximumEvents=4096,long MaximumOutputBytes=4194304,long MaximumWork=1048576);
public sealed record CreatureView6StateEvent(ushort Index,int Ordinal);
public sealed class CreatureView6StateChange
{
 internal CreatureView6StateChange(CreatureView6State current,CreatureView6StateEvent[] events,long work){Current=current;Events=Array.AsReadOnly(events);Work=work;}
 public CreatureView6State Current{get;} public IReadOnlyList<CreatureView6StateEvent> Events{get;} public long Work{get;}
}
/// <summary>Modern immutable full CREO6 model; ordered supported assignment descriptions, no callbacks.</summary>
public sealed class CreatureView6State
{
 private CreatureView6State(CreatureView6Baseline model,ulong revision,long bytes){Model=model;Revision=revision;DecodedBytes=bytes;}
 public CreatureView6Baseline Model{get;} public ulong Revision{get;} public long DecodedBytes{get;} public ulong ObjectId=>Model.ObjectId;
 internal static void Validate(CreatureView6StateLimits l)
 {
  ArgumentNullException.ThrowIfNull(l);
  if(l.MaximumUpdates is <0 or >4096||l.MaximumStringBytes is <0 or >1048576||l.MaximumTotalStringBytes is <0 or >4194304||l.MaximumElements is <0 or >65536||l.MaximumTotalElements is <0 or >262144||l.MaximumStateBytes is <0 or >16777216||l.MaximumEvents is <0 or >262144||l.MaximumOutputBytes is <0 or >33554432||l.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(l));
 }
 private static (long Bytes,long Work) Measure(CreatureView6Baseline m,CreatureView6StateLimits l)
 {
  long bytes=102,elements=0,strings=0;
  void Vector(int count,int width){if(count>l.MaximumElements)throw new InvalidDataException("CREO6 state vector capacity.");elements+=count;bytes+=width*(long)count;}
  void Text(ReplicationByteString s){if(s.Length>l.MaximumStringBytes)throw new InvalidDataException("CREO6 state string capacity.");strings+=s.Length;bytes+=(s.UsesExtendedLength?6:2)+(long)s.Length;}
  Vector(m.Defenders.Values.Count,8);Vector(m.Ham.Values.Count,4);Vector(m.MaximumHam.Values.Count,4);Vector(m.Wearables.Values.Count,16);
  Text(m.PerformanceAnimation);Text(m.MoodString);Text(m.AlternateAppearance);foreach(var w in m.Wearables.Values)Text(w.Customization);
  if(elements>l.MaximumTotalElements||strings>l.MaximumTotalStringBytes||bytes>l.MaximumStateBytes)throw new InvalidDataException("CREO6 global state capacity.");
  return(bytes,18+elements+strings);
 }
 public static CreatureView6State FromBaseline(CreatureView6Baseline baseline,CreatureView6StateLimits? limits=null,ulong revisionSeed=0)
 {
  ArgumentNullException.ThrowIfNull(baseline);var l=limits??new();Validate(l);ulong next=checked(revisionSeed+1);var cost=Measure(baseline,l);
  if(cost.Work>l.MaximumWork||cost.Bytes>l.MaximumOutputBytes)throw new InvalidDataException("CREO6 seed work/output capacity.");return new(baseline,next,cost.Bytes);
 }
 private static CreatureView6Baseline Assign(CreatureView6Baseline m,CreatureView6Update u)
 {
  ushort i=u.Index;
  return new(m.ObjectId,m.DeclaredCount,i==0?(uint)u.Bits:m.Passive,m.Defenders,i==2?(ushort)u.Bits:m.Level,
   i==3?u.Text!:m.PerformanceAnimation,i==4?u.Text!:m.MoodString,i==5?u.Bits:m.WeaponId,i==6?u.Bits:m.GroupId,
   i==7?u.Bits:m.InviterId,i==7?u.SecondId:m.InviteCounter,i==8?(uint)u.Bits:m.Guild,i==9?u.Bits:m.TargetId,
   i==10?(byte)u.Bits:m.MoodId,i==11?(uint)u.Bits:m.PerformanceStart,i==12?u.PerformanceType:m.PerformanceType,
   m.Ham,m.MaximumHam,m.Wearables,i==16?u.Text!:m.AlternateAppearance,i==17?(byte)u.Bits:m.Frozen);
 }
 public CreatureView6StateChange Apply(CreatureView6Delta plan,CreatureView6StateLimits? limits=null)
 {
  ArgumentNullException.ThrowIfNull(plan);var l=limits??new();Validate(l);
  if(plan.ObjectId!=ObjectId||plan.Updates.Count==0||plan.Updates.Count>l.MaximumUpdates||plan.Updates.Count>l.MaximumEvents)throw new InvalidDataException("CREO6 plan target/update/event capacity.");
  ulong next=checked(Revision+1);var current=Model;long work=0,bytes=DecodedBytes;var events=new CreatureView6StateEvent[plan.Updates.Count];
  for(int n=0;n<plan.Updates.Count;n++)
  {
   var u=plan.Updates[n];if(u.Index is not (0 or 2 or 3 or 4 or 5 or 6 or 7 or 8 or 9 or 10 or 11 or 12 or 16 or 17))throw new InvalidDataException("Unsupported CREO6 state assignment.");
   current=Assign(current,u);var cost=Measure(current,l);bytes=cost.Bytes;work=checked(work+1+cost.Work);
   if(work>l.MaximumWork||bytes+8L*(n+1)>l.MaximumOutputBytes)throw new InvalidDataException("Intermediate CREO6 work/output capacity.");events[n]=new(u.Index,n);
  }
  return new(new(current,next,bytes),events,work);
 }
}
