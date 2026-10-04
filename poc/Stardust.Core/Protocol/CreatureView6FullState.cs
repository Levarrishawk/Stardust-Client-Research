namespace Stardust.Core.Protocol;

public sealed record CreatureView6FullStateLimits(CreatureView6StateLimits? ModelLimits=null,CreatureView6FullLimits? MessageLimits=null);
public enum CreatureView6FullEventKind { Assignment,DefenderRemoved,DefenderAdded,DefenderSet,DefenderChanged,HamMutation,WearableMutation }
/// <summary>Modern descriptions only. Each event costs 48 logical output bytes, independent of managed layout.</summary>
public readonly record struct CreatureView6FullEvent(CreatureView6FullEventKind Kind,ushort Field,int UpdateOrdinal,byte? Opcode,ushort? Index,ulong? OldId,ulong? NewId,int? OldValue,int? NewValue);
public sealed class CreatureView6FullChange
{
    internal CreatureView6FullChange(CreatureView6FullState current,ushort count,CreatureView6FullEvent[] events,long work)
    {Current=current;DeclaredCount=count;Events=Array.AsReadOnly(events);WorkUsed=work;}
    public CreatureView6FullState Current{get;} public ushort DeclaredCount{get;} public IReadOnlyList<CreatureView6FullEvent> Events{get;} public long WorkUsed{get;}
    public long DecodedOutputBytes=>Current.DecodedBytes+48L*Events.Count;
}
public sealed class CreatureView6FullState
{
    private readonly CreatureView6FullStateLimits limits;
    private CreatureView6FullState(CreatureView6Baseline model,ulong revision,long bytes,CreatureView6FullStateLimits limits)
    {Model=model;Revision=revision;DecodedBytes=bytes;this.limits=limits;}
    public CreatureView6Baseline Model{get;} public ulong ObjectId=>Model.ObjectId; public ulong Revision{get;} public long DecodedBytes{get;}
    private static CreatureView6FullStateLimits Validate(CreatureView6FullStateLimits l)
    {
        ArgumentNullException.ThrowIfNull(l);var m=l.ModelLimits??new();var c=l.MessageLimits??new();
        if(m.MaximumUpdates is <0 or >4096||m.MaximumStringBytes is <0 or >1048576||m.MaximumTotalStringBytes is <0 or >4194304||m.MaximumElements is <0 or >65536||m.MaximumTotalElements is <0 or >262144||m.MaximumStateBytes is <0 or >16777216||m.MaximumEvents is <0 or >262144||m.MaximumOutputBytes is <0 or >33554432||m.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(l));
        CreatureView6FullCodec.Validate(c);return new(m with{},c with{});
    }
    public static CreatureView6FullState FromBaseline(CreatureView6Baseline baseline,CreatureView6FullStateLimits? limits=null,ulong revisionSeed=0)
    {ArgumentNullException.ThrowIfNull(baseline);var l=Validate(limits??new());var seed=CreatureView6State.FromBaseline(baseline,l.ModelLimits,revisionSeed);return new(seed.Model,seed.Revision,seed.DecodedBytes,l);}
    public CreatureView6FullState Reseed(CreatureView6Baseline baseline)
    {ArgumentNullException.ThrowIfNull(baseline);if(baseline.ObjectId!=ObjectId)throw new InvalidDataException("Full CREO6 reseed recipient");return FromBaseline(baseline,limits,Revision);}
    private static int Elements(CreatureView6Baseline m)=>m.Defenders.Values.Count+m.Ham.Values.Count+m.MaximumHam.Values.Count+m.Wearables.Values.Count;
    private static long WearStrings(CreatureView6Baseline m)=>m.Wearables.Values.Sum(w=>(long)w.Customization.Length);
    private static long Strings(CreatureView6Baseline m)=>m.PerformanceAnimation.Length+(long)m.MoodString.Length+m.AlternateAppearance.Length+WearStrings(m);
    private static long Size(CreatureView6Wearable w)=>16+(w.Customization.UsesExtendedLength?6:2)+(long)w.Customization.Length;
    private static long WearBytes(CreatureView6Baseline m)=>8+m.Wearables.Values.Sum(Size);
    private static CreatureView6Baseline Vectors(CreatureView6Baseline m,CreatureView6Vector<ulong>? d=null,CreatureView6Vector<int>? h=null,CreatureView6Vector<int>? x=null,CreatureView6Vector<CreatureView6Wearable>? w=null)
        =>new(m.ObjectId,m.DeclaredCount,m.Passive,d??m.Defenders,m.Level,m.PerformanceAnimation,m.MoodString,m.WeaponId,m.GroupId,m.InviterId,m.InviteCounter,m.Guild,m.TargetId,m.MoodId,m.PerformanceStart,m.PerformanceType,h??m.Ham,x??m.MaximumHam,w??m.Wearables,m.AlternateAppearance,m.Frozen);
    public CreatureView6FullChange Apply(CreatureView6FullDelta plan,CreatureView6FullStateLimits? operationLimits=null)
    {
        ArgumentNullException.ThrowIfNull(plan);var l=Validate(operationLimits??limits);var ml=l.ModelLimits!;
        if(plan.ObjectId!=ObjectId||plan.Updates.Count>ml.MaximumUpdates)throw new InvalidDataException("Full CREO6 recipient/update capacity");
        var message=CreatureView6FullCodec.Measure(plan,l.MessageLimits!);ulong revision=checked(Revision+1);
        var seed=CreatureView6State.FromBaseline(Model,ml with{MaximumWork=16777216});var current=Model;long bytes=seed.DecodedBytes,work=0;var events=new List<CreatureView6FullEvent>();
        void Charge(long n){work=checked(work+n);if(work>ml.MaximumWork)throw new InvalidDataException("Full CREO6 logical work");}
        long Output()=>ml.MaximumOutputBytes-48L*events.Count;
        long Remaining()=>ml.MaximumWork-work;
        void Gate(int oldCount,int count,long candidateBytes,long candidateStrings,int added)
        {
            if(count>ml.MaximumElements||Elements(current)-oldCount+(long)count>ml.MaximumTotalElements||candidateStrings>ml.MaximumTotalStringBytes||candidateBytes>ml.MaximumStateBytes||events.Count+(long)added>ml.MaximumEvents||candidateBytes+48L*(events.Count+(long)added)>ml.MaximumOutputBytes)throw new InvalidDataException("Full CREO6 intermediate capacity");
        }
        Charge(18+Elements(current)+Strings(current)+message.Work);
        CreatureDefendersView6StateLimits DefLimits()
        {
            int n=current.Defenders.Values.Count,other=Elements(current)-n;long otherBytes=bytes-(8+8L*n);
            return new(Math.Min(ml.MaximumElements,ml.MaximumTotalElements-other),ml.MaximumStateBytes-otherBytes,ml.MaximumEvents-events.Count,Output()-otherBytes,Remaining(),new(8388608,4096,65536,1048576,16777216,16777216));
        }
        CreatureHamView6StateLimits HamLimits()
        {
            int n=current.Ham.Values.Count+current.MaximumHam.Values.Count,other=Elements(current)-n;long otherBytes=bytes-(16+4L*n);
            return new(ml.MaximumElements,Math.Min(131072,ml.MaximumTotalElements-other),ml.MaximumStateBytes-otherBytes,ml.MaximumEvents-events.Count,Output()-otherBytes,Remaining(),new(8388608,4096,65536,1048576,16777216));
        }
        CreatureWearablesView6StateLimits WearLimits()
        {
            int n=current.Wearables.Values.Count,other=Elements(current)-n;long otherBytes=bytes-WearBytes(current),otherStrings=Strings(current)-WearStrings(current);
            return new(Math.Min(ml.MaximumElements,ml.MaximumTotalElements-other),ml.MaximumStringBytes,checked((int)(ml.MaximumTotalStringBytes-otherStrings)),ml.MaximumStateBytes-otherBytes,ml.MaximumEvents-events.Count,Output()-otherBytes,Remaining(),new(8388608,4096,65536,1048576,1048576,4194304,16777216,16777216));
        }
        for(int ordinal=0;ordinal<plan.Updates.Count;ordinal++)
        {
            var u=plan.Updates[ordinal];
            if(u.Scalar is {} scalar)
            {
                if(events.Count>=ml.MaximumEvents||Output()<40)throw new InvalidDataException("Full CREO6 assignment capacity");
                Charge(18+Elements(current)+Strings(current));
                var previous=CreatureView6State.FromBaseline(current,ml with{MaximumWork=16777216,MaximumOutputBytes=Output()});
                var r=previous.Apply(new(ObjectId,plan.DeclaredCount,[scalar]),ml with{MaximumUpdates=1,MaximumEvents=1,MaximumOutputBytes=Output()-40,MaximumWork=Remaining()});
                Charge(r.Work);current=r.Current.Model;bytes=r.Current.DecodedBytes;events.Add(new(CreatureView6FullEventKind.Assignment,u.Index,ordinal,null,null,null,null,null,null));
            }
            else if(u.Defender is {} d)
            {
                Charge(current.Defenders.Values.Count);var state=CreatureDefendersView6State.FromBaseline(current,DefLimits() with{MaximumWork=16777216});
                uint skip=Math.Min(unchecked(d.Extent-d.Counter+state.Defenders.Counter),d.Extent);ulong cursor=0;
                foreach(var c in d.Commands)
                {
                    bool ignored=cursor<skip;uint extent=1+(c.Opcode==3?(uint)c.Values.Count:0);cursor+=extent;if(ignored)continue;
                    int n=current.Defenders.Values.Count,ix=c.Index,count=n,added=0;
                    switch(c.Opcode){case 0:if(ix<n){count=n-1;added=2;}break;case 1:count=Math.Max(n,ix)+1;added=2;break;case 2:if(ix>=n||current.Defenders.Values[ix]!=c.Values[0]){count=Math.Max(n,ix+1);added=2;}break;case 3:count=c.Values.Count;added=1;break;case 4:if(n>0){count=0;added=n+1;}break;}
                    Gate(n,count,bytes+8L*(count-n),Strings(current),added);
                    var r=state.Apply(new(ObjectId,plan.DeclaredCount,[new(1,extent,unchecked(state.Defenders.Counter+extent),[c])]),DefLimits());Charge(r.WorkUsed);
                    if(r.Events.Count!=added)throw new InvalidDataException("Defender capacity prediction mismatch");
                    state=r.Current;bytes+=state.DecodedBytes-(8+8L*n);current=Vectors(current,d:state.Defenders);
                    foreach(var e in r.Events)events.Add(new(e.Kind switch{CreatureDefendersView6EventKind.Removed=>CreatureView6FullEventKind.DefenderRemoved,CreatureDefendersView6EventKind.Added=>CreatureView6FullEventKind.DefenderAdded,CreatureDefendersView6EventKind.Set=>CreatureView6FullEventKind.DefenderSet,_=>CreatureView6FullEventKind.DefenderChanged},1,ordinal,e.Opcode,e.Index,e.OldId,e.NewId,null,null));
                }
            }
            else if(u.Ham is {} h)
            {
                Charge(current.Ham.Values.Count+(long)current.MaximumHam.Values.Count);var state=CreatureHamView6State.FromBaseline(current,HamLimits() with{MaximumWork=16777216});
                uint counter=h.Field==13?state.Ham.Counter:state.MaximumHam.Counter,skip=Math.Min(unchecked(h.Extent-h.Counter+counter),h.Extent);ulong cursor=0;
                foreach(var c in h.Commands)
                {
                    bool ignored=cursor<skip;uint extent=1+(c.Opcode==3?(uint)c.Values.Count:0);cursor+=extent;if(ignored)continue;
                    var values=h.Field==13?current.Ham.Values:current.MaximumHam.Values;int n=values.Count,ix=c.Index,count=n,added=0;
                    switch(c.Opcode){case 0:if(ix<n){count=n-1;added=1;}break;case 1:count=Math.Max(n,ix)+1;added=1;break;case 2:if(ix>=n||values[ix]!=c.Values[0]){count=Math.Max(n,ix+1);added=1;}break;case 3:count=c.Values.Count;added=1;break;case 4:if(n>0){count=0;added=1;}break;}
                    Gate(n,count,bytes+4L*(count-n),Strings(current),added);
                    counter=h.Field==13?state.Ham.Counter:state.MaximumHam.Counter;long oldBytes=state.DecodedBytes;
                    var r=state.Apply(new(ObjectId,plan.DeclaredCount,[new(h.Field,extent,unchecked(counter+extent),[c])]),HamLimits());Charge(r.WorkUsed);
                    if(r.Events.Count!=added)throw new InvalidDataException("HAM capacity prediction mismatch");
                    state=r.Current;bytes+=state.DecodedBytes-oldBytes;current=Vectors(current,h:state.Ham,x:state.MaximumHam);
                    foreach(var e in r.Events)events.Add(new(CreatureView6FullEventKind.HamMutation,e.Field,ordinal,e.Opcode,e.Index,null,null,e.OldValue,e.NewValue));
                }
            }
            else
            {
                var w=u.Wearable!;Charge(current.Wearables.Values.Count+WearStrings(current));var state=CreatureWearablesView6State.FromBaseline(current,WearLimits() with{MaximumWork=16777216});
                uint skip=Math.Min(unchecked(w.Extent-w.Counter+state.Wearables.Counter),w.Extent);ulong cursor=0;
                foreach(var c in w.Commands)
                {
                    bool ignored=cursor<skip;uint extent=1+(c.Opcode==3?(uint)c.Values.Count:0);cursor+=extent;if(ignored)continue;
                    var values=current.Wearables.Values;int n=values.Count,ix=c.Index,count=n,added=0;long nb=bytes,ns=Strings(current);
                    switch(c.Opcode)
                    {
                        case 0:if(ix<n){count=n-1;added=1;nb-=Size(values[ix]);ns-=values[ix].Customization.Length;}break;
                        case 1:count=Math.Max(n,ix)+1;added=1;nb+=18L*Math.Max(0,ix-n)+Size(c.Values[0]);ns+=c.Values[0].Customization.Length;break;
                        case 2:if(ix>=n||values[ix].ObjectId!=c.Values[0].ObjectId){count=Math.Max(n,ix+1);added=1;nb+=18L*(count-n)-(ix<n?Size(values[ix]):18)+Size(c.Values[0]);ns-=(ix<n?values[ix].Customization.Length:0);ns+=c.Values[0].Customization.Length;}break;
                        case 3:count=c.Values.Count;added=1;nb=bytes-WearBytes(current)+8+c.Values.Sum(Size);ns=Strings(current)-WearStrings(current)+c.Values.Sum(e=>(long)e.Customization.Length);break;
                        case 4:if(n>0){count=0;added=1;nb=bytes-WearBytes(current)+8;ns=Strings(current)-WearStrings(current);}break;
                    }
                    if(added>0&&c.Values.Any(e=>e.Customization.Length>ml.MaximumStringBytes))throw new InvalidDataException("Full CREO6 wearable string capacity");
                    Gate(n,count,nb,ns,added);long oldBytes=state.DecodedBytes;
                    var r=state.Apply(new(ObjectId,plan.DeclaredCount,[new(extent,unchecked(state.Wearables.Counter+extent),[c])]),WearLimits());Charge(r.WorkUsed);
                    if(r.Events.Count!=added)throw new InvalidDataException("Wearable capacity prediction mismatch");
                    state=r.Current;bytes+=state.DecodedBytes-oldBytes;current=Vectors(current,w:state.Wearables);
                    foreach(var e in r.Events)events.Add(new(CreatureView6FullEventKind.WearableMutation,15,ordinal,e.Opcode,e.Index,e.OldObjectId,e.NewObjectId,null,null));
                }
            }
            Gate(0,0,bytes,Strings(current),0);
        }
        return new(new(current,revision,bytes,l),plan.DeclaredCount,events.ToArray(),work);
    }
}
