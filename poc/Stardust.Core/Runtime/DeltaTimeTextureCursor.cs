namespace Stardust.Core.Runtime;

/// <summary>Immutable receipt of modern abstract-unit selector state; no original x87 or scheduler parity claim.</summary>
public sealed class DeltaTimeTextureCursorSnapshot
{
    internal DeltaTimeTextureCursorSnapshot(object owner,int count,float interval,int index,float residual,ulong revision,ulong advances,int steps,int maximumCatchUpSteps)
    {Owner=owner;FrameCount=count;Interval=interval;Index=index;Residual=residual;Revision=revision;CumulativeAdvances=advances;LastAdvanceSteps=steps;MaximumCatchUpSteps=maximumCatchUpSteps;}
    internal object Owner {get;}
    public int FrameCount {get;}
    public float Interval {get;}
    public int Index {get;}
    public float Residual {get;}
    public ulong Revision {get;}
    public ulong CumulativeAdvances {get;}
    public int LastAdvanceSteps {get;}
    public int MaximumCatchUpSteps {get;}
    public string NumericPolicy=>"binary64-intermediate-float32-persistent";
    public string InputUnits=>"abstract-matching-units";
    public bool OriginalX87Parity=>false;
}
/// <summary>Bounded local selector: binary64 update arithmetic, persistent float32 residual, abstract matching units.</summary>
public sealed class DeltaTimeTextureCursor:IDisposable
{
    private readonly int ownerThread=Environment.CurrentManagedThreadId;
    private readonly object identity=new();
    private readonly int maximumCatchUpSteps;
    private DeltaTimeTextureCursorSnapshot current;
    private bool closed,busy;
    public DeltaTimeTextureCursor(int frameCount,float interval,int maximumCatchUpSteps=4096,ulong revisionSeed=0,ulong cumulativeAdvancesSeed=0)
    {
        if(frameCount<1)throw new ArgumentOutOfRangeException(nameof(frameCount));
        if(!float.IsFinite(interval)||interval<=0)throw new ArgumentOutOfRangeException(nameof(interval));
        if(maximumCatchUpSteps is <1 or >1048576)throw new ArgumentOutOfRangeException(nameof(maximumCatchUpSteps));
        this.maximumCatchUpSteps=maximumCatchUpSteps;current=new(identity,frameCount,interval,0,0,revisionSeed,cumulativeAdvancesSeed,0,maximumCatchUpSteps);
    }
    private void Enter(){if(Environment.CurrentManagedThreadId!=ownerThread)throw new InvalidOperationException("Texture cursor owner thread required.");if(busy)throw new InvalidOperationException("Texture cursor reentry.");busy=true;}
    private void Open()=>ObjectDisposedException.ThrowIf(closed,this);
    public DeltaTimeTextureCursorSnapshot Snapshot {get{Enter();try{return current;}finally{busy=false;}}}
    public bool IsClosed {get{Enter();try{return closed;}finally{busy=false;}}}
    public bool IsCurrent(DeltaTimeTextureCursorSnapshot snapshot)
    {Enter();try{ArgumentNullException.ThrowIfNull(snapshot);return !closed&&ReferenceEquals(snapshot.Owner,identity)&&ReferenceEquals(snapshot,current);}finally{busy=false;}}
    public DeltaTimeTextureCursorSnapshot Advance(float delta)
    {
        Enter();try
        {
            Open();if(!float.IsFinite(delta)||delta<0)throw new ArgumentOutOfRangeException(nameof(delta));
            ulong revision=checked(current.Revision+1);double remaining=(double)current.Residual+delta;
            double interval=current.Interval;
            // At most budget subtractions can incur budget*ulp(total) error; their magnitudes never exceed total.
            // Product (budget+1)*float32 interval is exact in binary64 with this <=2^20 work ceiling.
            double error=maximumCatchUpSteps*(Math.BitIncrement(remaining)-remaining);
            if(remaining>=(maximumCatchUpSteps+1d)*interval+error)throw new InvalidDataException("Texture cursor catch-up budget exhausted.");
            int steps=0,index=current.Index;
            while(remaining>=interval)
            {
                if(steps==maximumCatchUpSteps)throw new InvalidDataException("Texture cursor catch-up budget exhausted.");
                remaining-=interval;steps++;index++;if(index==current.FrameCount)index=0;
            }
            ulong advances=checked(current.CumulativeAdvances+(ulong)steps);
            // Float32 persistence may round a remainder up to interval. Preserve it; zero delta may advance next time.
            float residual=(float)remaining;
            var candidate=new DeltaTimeTextureCursorSnapshot(identity,current.FrameCount,current.Interval,index,residual,revision,advances,steps,maximumCatchUpSteps);
            current=candidate;return candidate;
        }finally{busy=false;}
    }
    public DeltaTimeTextureCursorSnapshot Reset()
    {Enter();try{Open();ulong revision=checked(current.Revision+1);var candidate=new DeltaTimeTextureCursorSnapshot(identity,current.FrameCount,current.Interval,0,0,revision,current.CumulativeAdvances,0,maximumCatchUpSteps);current=candidate;return candidate;}finally{busy=false;}}
    public void Close(){Enter();try{closed=true;}finally{busy=false;}}
    public void Dispose()=>Close();
}
