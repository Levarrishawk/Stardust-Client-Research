namespace Stardust.Core.Runtime;

public sealed class ShaderObjectClockSnapshot
{
    internal ShaderObjectClockSnapshot(object owner,ulong tick,ulong revision,int count,int capacity,ulong maximumTicks)
    {Owner=owner;Tick=tick;Revision=revision;RegisteredCount=count;Capacity=capacity;MaximumTicksPerAdvance=maximumTicks;}
    internal object Owner {get;}
    public ulong Tick {get;}
    public ulong Revision {get;}
    public int RegisteredCount {get;}
    public int Capacity {get;}
    public ulong MaximumTicksPerAdvance {get;}
    public string NumericPolicy=>"binary64-multiply-truncate-min1";
    public string InputUnits=>"seconds";
    public bool OriginalPC24Parity=>false;
}
public sealed class ShaderObjectClockObjectSnapshot
{
    internal ShaderObjectClockObjectSnapshot(object owner,ulong id,ulong token,ulong from,ulong through,float seconds,ulong revision)
    {Owner=owner;ObjectId=id;RegistrationToken=token;FromTick=from;ThroughTick=through;ElapsedSeconds=seconds;Revision=revision;}
    internal object Owner {get;}
    public ulong ObjectId {get;}
    public ulong RegistrationToken {get;}
    public ulong FromTick {get;}
    public ulong ThroughTick {get;}
    public ulong LastServicedTick=>ThroughTick;
    public float ElapsedSeconds {get;}
    public ulong Revision {get;}
    public uint InverseScaleBits=>0x3A83126F;
    public string NumericPolicy=>"binary64-difference-times-float32-inverse-float32-storage";
    public bool OriginalPC24Parity=>false;
}
/// <summary>Standalone modern safe scheduler clock; no callbacks, wall clock, cursor or engine integration.</summary>
public sealed class ShaderObjectClock:IDisposable
{
    private const double ExclusiveU64Limit=18446744073709551616d;
    private static readonly float InverseScale=BitConverter.UInt32BitsToSingle(0x3A83126F);
    private readonly object identity=new();
    private readonly int ownerThread=Environment.CurrentManagedThreadId,capacity;
    private readonly ulong maximumTicks;
    private readonly double maximumTickValue;
    private SortedDictionary<ulong,ShaderObjectClockObjectSnapshot> entries=new();
    private ShaderObjectClockSnapshot current;
    private ulong registrationToken;
    private bool busy,closed;
    public ShaderObjectClock(int capacity=64,ulong maximumTicksPerAdvance=1000000,ulong tickSeed=0,ulong revisionSeed=0,ulong registrationTokenSeed=0)
    {
        if(capacity is <1 or >4096)throw new ArgumentOutOfRangeException(nameof(capacity));
        if(maximumTicksPerAdvance==0)throw new ArgumentOutOfRangeException(nameof(maximumTicksPerAdvance));
        this.capacity=capacity;maximumTicks=maximumTicksPerAdvance;registrationToken=registrationTokenSeed;
        double cap=(double)maximumTicksPerAdvance;
        if(cap>=ExclusiveU64Limit)cap=Math.BitDecrement(cap);
        else if(checked((ulong)cap)>maximumTicksPerAdvance)cap=Math.BitDecrement(cap);
        maximumTickValue=cap; // Largest representable binary64 integer not above the configured ulong cap.
        current=new(identity,tickSeed,revisionSeed,0,capacity,maximumTicks);
    }
    private void Enter(){if(Environment.CurrentManagedThreadId!=ownerThread)throw new InvalidOperationException("Shader clock owner thread required.");if(busy)throw new InvalidOperationException("Shader clock reentry.");busy=true;}
    private void Open()=>ObjectDisposedException.ThrowIf(closed,this);
    private ShaderObjectClockSnapshot Candidate(ulong tick,ulong revision,int count)=>new(identity,tick,revision,count,capacity,maximumTicks);
    public ShaderObjectClockSnapshot Snapshot {get{Enter();try{return current;}finally{busy=false;}}}
    public bool IsClosed {get{Enter();try{return closed;}finally{busy=false;}}}
    public ShaderObjectClockSnapshot Advance(float seconds)
    {
        Enter();try
        {
            Open();if(!float.IsFinite(seconds)||seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));
            double product=(double)seconds*1000d;
            if(!double.IsFinite(product)||product>=ExclusiveU64Limit)throw new InvalidDataException("Shader clock tick conversion range.");
            // Exclusive 2^64 preflight is essential: ulong.MaxValue rounds to 2^64 as double.
            double tickValue=Math.Max(1d,Math.Truncate(product));
            if(tickValue>maximumTickValue)throw new InvalidDataException("Shader clock per-advance tick capacity.");
            ulong ticks=checked((ulong)tickValue);
            ulong tick=checked(current.Tick+ticks),revision=checked(current.Revision+1);
            var candidate=Candidate(tick,revision,entries.Count);current=candidate;return candidate;
        }finally{busy=false;}
    }
    public ShaderObjectClockObjectSnapshot Register(ulong id)
    {
        Enter();try
        {
            Open();if(entries.ContainsKey(id))throw new InvalidDataException("Duplicate shader clock object.");
            if(entries.Count==capacity)throw new InvalidDataException("Shader clock object capacity.");
            ulong token=checked(registrationToken+1),revision=checked(current.Revision+1);
            var value=new ShaderObjectClockObjectSnapshot(identity,id,token,current.Tick,current.Tick,0,revision);
            var staged=new SortedDictionary<ulong,ShaderObjectClockObjectSnapshot>(entries){[id]=value};
            var candidate=Candidate(current.Tick,revision,staged.Count);
            entries=staged;registrationToken=token;current=candidate;return value;
        }finally{busy=false;}
    }
    public ShaderObjectClockObjectSnapshot? Service(ulong id)
    {
        Enter();try
        {
            Open();if(!entries.TryGetValue(id,out var value))return null;
            if(value.ThroughTick==current.Tick)return value; // Same receipt, no new delivery or revision.
            ulong diff=checked(current.Tick-value.ThroughTick),revision=checked(current.Revision+1);
            float seconds=(float)((double)diff*InverseScale);
            if(!float.IsFinite(seconds))throw new InvalidDataException("Shader clock service numeric range.");
            var replacement=new ShaderObjectClockObjectSnapshot(identity,id,value.RegistrationToken,value.ThroughTick,current.Tick,seconds,revision);
            var staged=new SortedDictionary<ulong,ShaderObjectClockObjectSnapshot>(entries){[id]=replacement};
            var candidate=Candidate(current.Tick,revision,staged.Count);entries=staged;current=candidate;return replacement;
        }finally{busy=false;}
    }
    public ShaderObjectClockObjectSnapshot? Find(ulong id)
    {Enter();try{return entries.GetValueOrDefault(id);}finally{busy=false;}}
    public IReadOnlyList<ShaderObjectClockObjectSnapshot> Snapshots()
    {Enter();try{return Array.AsReadOnly(entries.Values.ToArray());}finally{busy=false;}}
    public bool IsCurrent(ShaderObjectClockSnapshot snapshot)
    {Enter();try{ArgumentNullException.ThrowIfNull(snapshot);return !closed&&ReferenceEquals(snapshot.Owner,identity)&&ReferenceEquals(snapshot,current);}finally{busy=false;}}
    public bool IsCurrent(ShaderObjectClockObjectSnapshot snapshot)
    {Enter();try{ArgumentNullException.ThrowIfNull(snapshot);return !closed&&ReferenceEquals(snapshot.Owner,identity)&&snapshot.ThroughTick==current.Tick&&entries.TryGetValue(snapshot.ObjectId,out var latest)&&ReferenceEquals(snapshot,latest);}finally{busy=false;}}
    public bool Remove(ulong id)
    {
        Enter();try
        {
            Open();if(!entries.ContainsKey(id))return false;ulong revision=checked(current.Revision+1);
            var staged=new SortedDictionary<ulong,ShaderObjectClockObjectSnapshot>(entries);staged.Remove(id);var candidate=Candidate(current.Tick,revision,staged.Count);
            entries=staged;current=candidate;return true;
        }finally{busy=false;}
    }
    public ShaderObjectClockSnapshot Reset()
    {
        Enter();try
        {
            Open();ulong revision=checked(current.Revision+1);var staged=new SortedDictionary<ulong,ShaderObjectClockObjectSnapshot>();var candidate=Candidate(current.Tick,revision,0);
            entries=staged;current=candidate;return candidate; // Tick and registration-token counter never rewind.
        }finally{busy=false;}
    }
    public void Close()
    {
        Enter();try
        {
            if(closed)return;var staged=new SortedDictionary<ulong,ShaderObjectClockObjectSnapshot>();var candidate=Candidate(current.Tick,current.Revision,0);
            entries=staged;current=candidate;closed=true;
        }finally{busy=false;}
    }
    public void Dispose()=>Close();
}
