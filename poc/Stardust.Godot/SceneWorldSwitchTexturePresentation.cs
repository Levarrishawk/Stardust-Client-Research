using System;
using System.Threading.Tasks;
using Stardust.Core.Session;
using Stardust.Core.Runtime;

/// <summary>Owns inner presentation and texture adapter; modern supplied seconds, no legacy scheduler parity.</summary>
public sealed class SceneWorldSwitchTexturePresentation : ISceneWorldObjectPresentation, ISceneWorldObjectPresentationDrain
{
    private readonly int owner=Environment.CurrentManagedThreadId;
    private ISceneWorldObjectPresentation? inner;
    private bool disposed;
    private readonly float? fixedStepSeconds;
    private Task finalDrain=Task.CompletedTask;
    public ScenePreparedObjectPublication Publication {get;}
    public SceneStaticSwitchTextureVisual Animation {get;}
    public ShaderTexturePlayback? Playback {get;}
    public bool Playing {get;private set;}
    public bool IsRetired=>disposed;
    public Task DrainCompletion=>disposed?finalDrain:(inner as ISceneWorldObjectPresentationDrain)?.DrainCompletion??Task.CompletedTask;
    public SceneWorldSwitchTexturePresentation(ISceneWorldObjectPresentation inner,SceneWorldObjectVisual basis,float? fixedStepSeconds=null,bool useObjectClock=false)
    {
        ArgumentNullException.ThrowIfNull(inner);ArgumentNullException.ThrowIfNull(basis);
        if(fixedStepSeconds is {} supplied && (!float.IsFinite(supplied)||supplied<0))throw new ArgumentOutOfRangeException(nameof(fixedStepSeconds));
        var source=basis.Publication.Resources.Static;var visual=basis.StaticVisual;
        if(!ReferenceEquals(inner.Publication,basis.Publication)||visual is null||source is null||source.SwitchFrames is not {} bundle)
            throw new ArgumentException("Exact current static frame publication required.");
        Publication=inner.Publication;
        this.fixedStepSeconds=fixedStepSeconds;
        Animation=new(visual,source,bundle);
        if(useObjectClock) {
            try{Playback=new(capacity:1);Playback.Register(Publication.Ticket.Handle.ObjectId,bundle.FrameCount,bundle.Interval);}
            catch{try{Playback?.Dispose();}finally{Animation.Dispose();}throw;}
        }
        this.inner=inner;
    }
    private void Owner(){if(Environment.CurrentManagedThreadId!=owner)throw new InvalidOperationException("Switch presentation owner thread required.");}
    public void SetPlaying(bool value){Owner();ObjectDisposedException.ThrowIf(disposed,this);Playing=value;}
    public bool Tick(double seconds)
    {
        Owner();if(disposed)return false;
        if(!double.IsFinite(seconds)||seconds<0||seconds>float.MaxValue)throw new ArgumentOutOfRangeException(nameof(seconds));
        try
        {
            if(!inner!.Tick(seconds)){Dispose();return false;}
            bool alive=Playing?AdvancePlayback(fixedStepSeconds??(float)seconds):Animation.Tick();
            if(!alive)Dispose();return alive;
        }
        catch{Dispose();throw;}
    }
    public bool VerifyClockCursor()
    {
        Owner();if(disposed)return false;if(Playback is null)return true;
        var state=Playback.Find(Publication.Ticket.Handle.ObjectId);var cursor=Animation.Snapshot;
        return state is not null&&Playback.IsCurrent(state)&&state.Cursor.Index==cursor.Index&&
            BitConverter.SingleToUInt32Bits(state.Cursor.Residual)==BitConverter.SingleToUInt32Bits(cursor.Residual)&&
            state.Cursor.Revision==cursor.Revision&&state.Cursor.CumulativeAdvances==cursor.CumulativeAdvances;
    }
    public bool AdvancePlayback(float seconds)
    {
        Owner();ObjectDisposedException.ThrowIf(disposed,this);
        if(Playback is null)return Animation.Advance(seconds);
        // Atomic clock preflight errors retain both owners. Once time advances, delivery errors retire this presentation.
        Playback.Advance(seconds);
        try{
            var state=Playback.Service(Publication.Ticket.Handle.ObjectId)??throw new InvalidOperationException("Missing owned playback registration.");
            bool alive=Animation.Advance(state.Clock.ElapsedSeconds);
            if(!alive){Dispose();return false;}
            if(!VerifyClockCursor())throw new InvalidOperationException("Clock/cached visual selector mismatch.");
            return true;
        }catch{Dispose();throw;}
    }
    public bool ResetPlayback()
    {
        Owner();ObjectDisposedException.ThrowIf(disposed,this);
        if(!Animation.Reset()){Dispose();return false;}
        if(Playback is null)return true;
        try{
            ulong id=Publication.Ticket.Handle.ObjectId;var cursor=Animation.Snapshot;
            if(!Playback.Remove(id))throw new InvalidOperationException("Missing reset registration.");
            Playback.Register(id,cursor.FrameCount,cursor.Interval,cursor.MaximumCatchUpSteps,cursor.Revision,cursor.CumulativeAdvances);
            if(!VerifyClockCursor())throw new InvalidOperationException("Reset selector mismatch.");
            return true;
        }catch{Dispose();throw;}
    }
    public bool ServiceSameTick()
    {
        Owner();ObjectDisposedException.ThrowIf(disposed,this);
        if(Playback is null)throw new InvalidOperationException("Object clock required.");
        try{
            var prior=Playback.Find(Publication.Ticket.Handle.ObjectId);
            var current=Playback.Service(Publication.Ticket.Handle.ObjectId);
            if(!ReferenceEquals(prior,current)||!VerifyClockCursor())throw new InvalidOperationException("Unexpected same-tick delivery.");
            return Animation.Tick();
        }catch{Dispose();throw;}
    }
    public bool Apply(SceneWorldObjectSnapshot snapshot)
    {
        Owner();ArgumentNullException.ThrowIfNull(snapshot);if(disposed)return false;
        try{bool alive=inner!.Apply(snapshot)&&Animation.Tick();if(!alive)Dispose();return alive;}
        catch{Dispose();throw;}
    }
    public void Dispose()
    {
        Owner();if(disposed)return;disposed=true;Playing=false;
        var owned=inner;inner=null;
        try{try{Playback?.Dispose();}finally{Animation.Dispose();}}
        finally{try{owned?.Dispose();}finally{finalDrain=(owned as ISceneWorldObjectPresentationDrain)?.DrainCompletion??Task.CompletedTask;}}
    }
}
