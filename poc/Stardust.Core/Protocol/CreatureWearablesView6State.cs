namespace Stardust.Core.Protocol;
public sealed record CreatureWearablesView6StateLimits(int MaximumElements=4096,int MaximumStringBytes=65536,int MaximumTotalStringBytes=262144,long MaximumStateBytes=2097152,int MaximumEvents=4096,long MaximumOutputBytes=4194304,long MaximumWork=1048576,CreatureWearablesView6Limits? MessageLimits=null);
public readonly record struct CreatureWearablesView6Event(ushort Field,byte Opcode,ushort? Index,ulong? OldObjectId,ulong? NewObjectId);
public sealed class CreatureWearablesView6Change
{
 internal CreatureWearablesView6Change(CreatureWearablesView6State current,ushort count,CreatureWearablesView6Event[] events,long work){Current=current;DeclaredCount=count;Events=Array.AsReadOnly(events);WorkUsed=work;}
 public CreatureWearablesView6State Current{get;}public ushort DeclaredCount{get;}public IReadOnlyList<CreatureWearablesView6Event> Events{get;}public long WorkUsed{get;}public long DecodedOutputBytes=>Current.DecodedBytes+32L*Events.Count;
}
public sealed class CreatureWearablesView6State
{
 private readonly CreatureWearablesView6StateLimits limits;
 private CreatureWearablesView6State(ulong id,ulong revision,uint counter,IEnumerable<CreatureView6Wearable> values,long bytes,CreatureWearablesView6StateLimits l){ObjectId=id;Revision=revision;Wearables=new(counter,values);DecodedBytes=bytes;limits=l;}
 public ulong ObjectId{get;}public ulong Revision{get;}public CreatureView6Vector<CreatureView6Wearable> Wearables{get;}public long DecodedBytes{get;}
 private static readonly CreatureView6Wearable Default=new(ReplicationByteString.Create([],false),uint.MaxValue,0,0);
 private static CreatureWearablesView6StateLimits Validate(CreatureWearablesView6StateLimits l){ArgumentNullException.ThrowIfNull(l);if(l.MaximumElements is <0 or >65536||l.MaximumStringBytes is <0 or >1048576||l.MaximumTotalStringBytes is <0 or >4194304||l.MaximumStateBytes is <8 or >16777216||l.MaximumEvents is <0 or >262144||l.MaximumOutputBytes is <8 or >33554432||l.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(l));var message=l.MessageLimits??new();CreatureWearablesView6Codec.Validate(message);return l with{MessageLimits=message with{}};}
 private static long Size(CreatureView6Wearable e)=>16+(e.Customization.UsesExtendedLength?6:2)+(long)e.Customization.Length;
 private static void Text(CreatureView6Wearable e,CreatureWearablesView6StateLimits l){if(e.Customization.Length>l.MaximumStringBytes)throw new InvalidDataException("Wearable state string capacity");}
 private static void Capacity(int count,long bytes,long strings,int events,CreatureWearablesView6StateLimits l){if(count>l.MaximumElements||strings>l.MaximumTotalStringBytes||bytes>l.MaximumStateBytes||events>l.MaximumEvents||bytes+32L*events>l.MaximumOutputBytes)throw new InvalidDataException("Wearable intermediate state/output capacity");}
 public static CreatureWearablesView6State FromBaseline(CreatureView6Baseline b,CreatureWearablesView6StateLimits? limits=null,ulong revisionSeed=0){ArgumentNullException.ThrowIfNull(b);var l=Validate(limits??new());ulong revision=checked(revisionSeed+1);long bytes=8,strings=0;foreach(var e in b.Wearables.Values){Text(e,l);bytes+=Size(e);strings+=e.Customization.Length;}Capacity(b.Wearables.Values.Count,bytes,strings,0,l);if(b.Wearables.Values.Count+strings>l.MaximumWork)throw new InvalidDataException("Wearable seed work");return new(b.ObjectId,revision,b.Wearables.Counter,b.Wearables.Values,bytes,l);}
 public CreatureWearablesView6State Reseed(CreatureView6Baseline b){ArgumentNullException.ThrowIfNull(b);if(b.ObjectId!=ObjectId)throw new InvalidDataException("Wearable reseed recipient");return FromBaseline(b,limits,Revision);}
 public CreatureWearablesView6Change Apply(CreatureWearablesView6Delta plan,CreatureWearablesView6StateLimits? operationLimits=null)
 {
  ArgumentNullException.ThrowIfNull(plan);if(plan.ObjectId!=ObjectId)throw new InvalidDataException("Wearable recipient");var l=Validate(operationLimits??limits);ulong revision=checked(Revision+1);var cost=CreatureWearablesView6Codec.Measure(plan,l.MessageLimits!);long strings=0,bytes=8;foreach(var e in Wearables.Values){Text(e,l);strings+=e.Customization.Length;bytes+=Size(e);}long work=Wearables.Values.Count+strings+cost.Work;
  void Charge(long n){work=checked(work+n);if(work>l.MaximumWork)throw new InvalidDataException("Wearable logical work");}Charge(0);Capacity(Wearables.Values.Count,bytes,strings,0,l);var values=Wearables.Values.ToList();uint counter=Wearables.Counter;var events=new List<CreatureWearablesView6Event>();
  foreach(var u in plan.Updates){uint skip=Math.Min(unchecked(u.Extent-u.Counter+counter),u.Extent);ulong cursor=0;foreach(var c in u.Commands){bool ignored=cursor<skip;cursor+=1UL+(c.Opcode==3?(ulong)c.Values.Count:0);if(ignored)continue;int ix=c.Index;ulong? old=null,next=null;ushort? eventIndex=c.Opcode<=2?c.Index:null;
   void Cap(int count,long b,long s){Capacity(count,b,s,checked(events.Count+1),l);}
   switch(c.Opcode){
    case 0:if(ix>=values.Count)continue;Cap(values.Count,bytes,strings);Charge(values.Count-ix);old=values[ix].ObjectId;bytes-=Size(values[ix]);strings-=values[ix].Customization.Length;values.RemoveAt(ix);counter=unchecked(counter+1);break;
    case 1:{var e=c.Values[0];Text(e,l);int count=checked(Math.Max(values.Count,ix)+1),gap=Math.Max(0,ix-values.Count);long nb=bytes+18L*gap+Size(e),ns=strings+e.Customization.Length;Cap(count,nb,ns);Charge(count-values.Count+Math.Max(0,values.Count-ix));while(values.Count<ix)values.Add(Default);values.Insert(ix,e);bytes=nb;strings=ns;next=e.ObjectId;counter=unchecked(counter+1);break;}
    case 2:{var e=c.Values[0];if(ix<values.Count&&values[ix].ObjectId==e.ObjectId)continue;Text(e,l);int count=checked(Math.Max(values.Count,ix+1)),growth=count-values.Count;var prior=ix<values.Count?values[ix]:Default;long nb=bytes+18L*growth-Size(prior)+Size(e),ns=strings-prior.Customization.Length+e.Customization.Length;Cap(count,nb,ns);Charge(growth+1);while(values.Count<=ix)values.Add(Default);old=values[ix].ObjectId;values[ix]=e;bytes=nb;strings=ns;next=e.ObjectId;counter=unchecked(counter+1);break;}
    case 3:{long nb=8,ns=0;foreach(var e in c.Values){Text(e,l);nb+=Size(e);ns+=e.Customization.Length;}Cap(c.Values.Count,nb,ns);Charge(values.Count+c.Values.Count);values=c.Values.ToList();bytes=nb;strings=ns;counter=unchecked(counter+1+(uint)c.Values.Count);break;}
    case 4:if(values.Count==0)continue;Cap(values.Count,bytes,strings);Charge(values.Count);values.Clear();bytes=8;strings=0;counter=unchecked(counter+1);break;
    default:throw new InvalidDataException("Wearable opcode");
   }events.Add(new(15,c.Opcode,eventIndex,old,next));
  }}Capacity(values.Count,bytes,strings,events.Count,l);Charge(values.Count+strings+events.Count);return new(new(ObjectId,revision,counter,values,bytes,l),plan.DeclaredCount,events.ToArray(),work);
 }
}
