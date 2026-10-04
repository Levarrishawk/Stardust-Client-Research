namespace Stardust.Core.Protocol;
public sealed record CreatureDefendersView6StateLimits(int MaximumElements=4096,long MaximumStateBytes=1048576,int MaximumEvents=4096,long MaximumOutputBytes=2097152,long MaximumWork=1048576,CreatureDefendersView6Limits? MessageLimits=null);
public enum CreatureDefendersView6EventKind{Removed,Added,Set,Changed}
public readonly record struct CreatureDefendersView6Event(ushort Field,byte Opcode,CreatureDefendersView6EventKind Kind,ushort? Index,ulong? OldId,ulong? NewId);
public sealed class CreatureDefendersView6Change
{
 internal CreatureDefendersView6Change(CreatureDefendersView6State current,ushort count,CreatureDefendersView6Event[] events,long work){Current=current;DeclaredCount=count;Events=Array.AsReadOnly(events);WorkUsed=work;}
 public CreatureDefendersView6State Current{get;}public ushort DeclaredCount{get;}public IReadOnlyList<CreatureDefendersView6Event> Events{get;}public long WorkUsed{get;}public long DecodedOutputBytes=>Current.DecodedBytes+32L*Events.Count;
}
public sealed class CreatureDefendersView6State
{
 private readonly CreatureDefendersView6StateLimits limits;
 private CreatureDefendersView6State(ulong id,ulong revision,uint counter,IEnumerable<ulong> values,CreatureDefendersView6StateLimits l){ObjectId=id;Revision=revision;Defenders=new(counter,values);limits=l;}
 public ulong ObjectId{get;}public ulong Revision{get;}public CreatureView6Vector<ulong> Defenders{get;}public long DecodedBytes=>8+8L*Defenders.Values.Count;
 private static CreatureDefendersView6StateLimits Validate(CreatureDefendersView6StateLimits l){ArgumentNullException.ThrowIfNull(l);if(l.MaximumElements is <0 or >65536||l.MaximumStateBytes is <8 or >16777216||l.MaximumEvents is <0 or >262144||l.MaximumOutputBytes is <8 or >33554432||l.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(l));var message=l.MessageLimits??new();CreatureDefendersView6Codec.Validate(message);return l with{MessageLimits=message with{}};}
 private static void Capacity(int count,int events,CreatureDefendersView6StateLimits l){if(count>l.MaximumElements||8+8L*count>l.MaximumStateBytes||events>l.MaximumEvents||8+8L*count+32L*events>l.MaximumOutputBytes)throw new InvalidDataException("Defender intermediate capacity");}
 public static CreatureDefendersView6State FromBaseline(CreatureView6Baseline b,CreatureDefendersView6StateLimits? limits=null,ulong revisionSeed=0){ArgumentNullException.ThrowIfNull(b);var l=Validate(limits??new());ulong revision=checked(revisionSeed+1);Capacity(b.Defenders.Values.Count,0,l);if(b.Defenders.Values.Count>l.MaximumWork)throw new InvalidDataException("Defender seed work");return new(b.ObjectId,revision,b.Defenders.Counter,b.Defenders.Values,l);}
 public CreatureDefendersView6State Reseed(CreatureView6Baseline b){ArgumentNullException.ThrowIfNull(b);if(b.ObjectId!=ObjectId)throw new InvalidDataException("Defender reseed recipient");return FromBaseline(b,limits,Revision);}
 public CreatureDefendersView6Change Apply(CreatureDefendersView6Delta plan,CreatureDefendersView6StateLimits? operationLimits=null)
 {
  ArgumentNullException.ThrowIfNull(plan);if(plan.ObjectId!=ObjectId)throw new InvalidDataException("Defender recipient");var l=Validate(operationLimits??limits);ulong revision=checked(Revision+1);var cost=CreatureDefendersView6Codec.Measure(plan,l.MessageLimits!);long work=Defenders.Values.Count+plan.Updates.Count+cost.Commands+cost.Values;
  void Charge(long n){work=checked(work+n);if(work>l.MaximumWork)throw new InvalidDataException("Defender logical work");}Charge(0);Capacity(Defenders.Values.Count,0,l);var ids=Defenders.Values.ToList();uint counter=Defenders.Counter;var events=new List<CreatureDefendersView6Event>();
  foreach(var u in plan.Updates){uint skip=Math.Min(unchecked(counter-u.Counter+u.Extent),u.Extent);ulong cursor=0;foreach(var c in u.Commands){bool ignored=cursor<skip;cursor+=1UL+(c.Opcode==3?(ulong)c.Values.Count:0);if(ignored)continue;int ix=c.Index;
   void Cap(int count,int added){Capacity(count,checked(events.Count+added),l);}void Emit(CreatureDefendersView6EventKind kind,ushort? index=null,ulong? old=null,ulong? next=null)=>events.Add(new(1,c.Opcode,kind,index,old,next));
   switch(c.Opcode){
    case 0:if(ix>=ids.Count)continue;Cap(ids.Count,2);Charge(ids.Count-ix);ulong removed=ids[ix];ids.RemoveAt(ix);counter=unchecked(counter+1);Emit(CreatureDefendersView6EventKind.Removed,c.Index,removed);Emit(CreatureDefendersView6EventKind.Changed);break;
    case 1:int added=checked(Math.Max(ids.Count,ix)+1);Cap(added,2);Charge(added-ids.Count+Math.Max(0,ids.Count-ix));while(ids.Count<ix)ids.Add(0);ids.Insert(ix,c.Values[0]);counter=unchecked(counter+1);Emit(CreatureDefendersView6EventKind.Added,c.Index,null,c.Values[0]);Emit(CreatureDefendersView6EventKind.Changed);break;
    case 2:if(ix<ids.Count&&ids[ix]==c.Values[0])continue;int count=checked(Math.Max(ids.Count,ix+1));Cap(count,2);Charge(count-ids.Count+1);while(ids.Count<=ix)ids.Add(0);ulong old=ids[ix];ids[ix]=c.Values[0];counter=unchecked(counter+1);Emit(CreatureDefendersView6EventKind.Set,c.Index,old,c.Values[0]);Emit(CreatureDefendersView6EventKind.Changed);break;
    case 3:Cap(c.Values.Count,1);Charge(ids.Count+c.Values.Count);ids=c.Values.ToList();counter=unchecked(counter+1+(uint)c.Values.Count);Emit(CreatureDefendersView6EventKind.Changed);break;
    case 4:if(ids.Count==0)continue;Cap(ids.Count,checked(ids.Count+1));Charge(ids.Count);for(int i=0;i<ids.Count;i++)Emit(CreatureDefendersView6EventKind.Removed,checked((ushort)i),ids[i]);ids.Clear();counter=unchecked(counter+1);Emit(CreatureDefendersView6EventKind.Changed);break;
    default:throw new InvalidDataException("Defender opcode");
   }
  }}Capacity(ids.Count,events.Count,l);Charge(ids.Count+events.Count);return new(new(ObjectId,revision,counter,ids,l),plan.DeclaredCount,events.ToArray(),work);
 }
}
