namespace Stardust.Core.Runtime;

public sealed class ShaderTexturePlaybackSnapshot
{
    internal ShaderTexturePlaybackSnapshot(object owner,ShaderObjectClockObjectSnapshot clock,DeltaTimeTextureCursorSnapshot cursor,ulong revision)
    {Owner=owner;Clock=clock;Cursor=cursor;Revision=revision;}
    internal object Owner {get;}
    public ShaderObjectClockObjectSnapshot Clock {get;}
    public DeltaTimeTextureCursorSnapshot Cursor {get;}
    public ulong ObjectId=>Clock.ObjectId;
    public ulong RegistrationToken=>Clock.RegistrationToken;
    public ulong Revision {get;}
    public string Policy=>"modern-clock-cursor-terminal-fault-on-delivery-failure";
    public bool OriginalParity=>false;
}
/// <summary>Owns clock and local cursors; failed consumed delivery terminally faults and drains this adapter.</summary>
public sealed class ShaderTexturePlayback:IDisposable
{
    private sealed record Entry(DeltaTimeTextureCursor Cursor,ShaderTexturePlaybackSnapshot Snapshot);
    private readonly object identity=new();
    private readonly int ownerThread=Environment.CurrentManagedThreadId;
    private readonly ShaderObjectClock clock;
    private SortedDictionary<ulong,Entry> entries=new();
    private ulong revision;
    private bool closed,faulted,busy;
    public ShaderTexturePlayback(int capacity=64,ulong maximumTicksPerAdvance=1000000,ulong tickSeed=0,ulong clockRevisionSeed=0,ulong registrationTokenSeed=0,ulong revisionSeed=0)
    {clock=new(capacity,maximumTicksPerAdvance,tickSeed,clockRevisionSeed,registrationTokenSeed);revision=revisionSeed;}
    private void Enter(){if(Environment.CurrentManagedThreadId!=ownerThread)throw new InvalidOperationException("Playback owner thread required.");if(busy)throw new InvalidOperationException("Playback reentry.");busy=true;}
    private void Open()=>ObjectDisposedException.ThrowIf(closed,this);
    public bool IsClosed{get{Enter();try{return closed;}finally{busy=false;}}}
    public bool IsFaulted{get{Enter();try{return faulted;}finally{busy=false;}}}
    public ulong Revision{get{Enter();try{return revision;}finally{busy=false;}}}
    public ShaderObjectClockSnapshot ClockSnapshot{get{Enter();try{return clock.Snapshot;}finally{busy=false;}}}
    public ShaderTexturePlaybackSnapshot Register(ulong id,int frameCount,float interval,int maximumCatchUpSteps=4096,ulong cursorRevisionSeed=0,ulong cumulativeAdvancesSeed=0)
    {
        Enter();DeltaTimeTextureCursor? cursor=null;bool consumed=false;try
        {
            Open();ulong next=checked(revision+1);cursor=new(frameCount,interval,maximumCatchUpSteps,cursorRevisionSeed,cumulativeAdvancesSeed);
            var receipt=clock.Register(id);consumed=true;
            var snapshot=new ShaderTexturePlaybackSnapshot(identity,receipt,cursor.Snapshot,next);
            var staged=new SortedDictionary<ulong,Entry>(entries){[id]=new(cursor,snapshot)};
            entries=staged;revision=next;cursor=null;return snapshot;
        }
        catch{if(consumed)Fault();throw;}
        finally{cursor?.Close();busy=false;}
    }
    public ShaderObjectClockSnapshot Advance(float seconds)
    {Enter();try{Open();ulong next=checked(revision+1);var value=clock.Advance(seconds);revision=next;return value;}finally{busy=false;}}
    public ShaderTexturePlaybackSnapshot? Service(ulong id)
    {
        Enter();bool consumed=false;try
        {
            Open();if(!entries.TryGetValue(id,out var entry))return null;
            if(clock.IsCurrent(entry.Snapshot.Clock))return entry.Snapshot; // Exact unchanged publication, no old elapsed redelivery.
            ulong next=checked(revision+1);var receipt=clock.Service(id)??throw new InvalidOperationException("Owned clock registration missing.");consumed=true;
            var cursor=entry.Cursor.Advance(receipt.ElapsedSeconds);
            var snapshot=new ShaderTexturePlaybackSnapshot(identity,receipt,cursor,next);
            var staged=new SortedDictionary<ulong,Entry>(entries){[id]=new(entry.Cursor,snapshot)};
            entries=staged;revision=next;return snapshot;
        }
        catch{if(consumed)Fault();throw;}
        finally{busy=false;}
    }
    public ShaderTexturePlaybackSnapshot? Find(ulong id)
    {Enter();try{return entries.TryGetValue(id,out var entry)?entry.Snapshot:null;}finally{busy=false;}}
    public IReadOnlyList<ShaderTexturePlaybackSnapshot> Snapshots()
    {Enter();try{return Array.AsReadOnly(entries.Values.Select(e=>e.Snapshot).ToArray());}finally{busy=false;}}
    public bool IsCurrent(ShaderTexturePlaybackSnapshot snapshot)
    {Enter();try{ArgumentNullException.ThrowIfNull(snapshot);return !closed&&ReferenceEquals(snapshot.Owner,identity)&&entries.TryGetValue(snapshot.ObjectId,out var entry)&&ReferenceEquals(entry.Snapshot,snapshot)&&clock.IsCurrent(snapshot.Clock)&&entry.Cursor.IsCurrent(snapshot.Cursor);}finally{busy=false;}}
    public bool Remove(ulong id)
    {
        Enter();try
        {
            Open();if(!entries.TryGetValue(id,out var entry))return false;ulong next=checked(revision+1);
            var staged=new SortedDictionary<ulong,Entry>(entries);staged.Remove(id);
            clock.Remove(id);entry.Cursor.Close();entries=staged;revision=next;return true;
        }finally{busy=false;}
    }
    public void Reset()
    {
        Enter();try{Open();ulong next=checked(revision+1);var staged=new SortedDictionary<ulong,Entry>();clock.Reset();foreach(var entry in entries.Values)entry.Cursor.Close();entries=staged;revision=next;}finally{busy=false;}
    }
    private void Fault(){faulted=true;CloseCore();}
    private void CloseCore(){if(closed)return;clock.Close();foreach(var entry in entries.Values)entry.Cursor.Close();entries.Clear();closed=true;}
    public void Close(){Enter();try{CloseCore();}finally{busy=false;}}
    public void Dispose()=>Close();
}
