namespace Stardust.Core.Assets;

/// <summary>Explicit modern preview time policy; independent of original gameplay animation controllers.</summary>
public sealed class AnimationPreviewClock
{
    public double Frame{get;private set;}
    public float FrameRate{get;}
    public ushort EndFrame{get;}
    public double TimeScale{get;}
    public bool Loop{get;set;}
    public bool Playing{get;private set;}
    public bool Focused{get;private set;}=true;
    public long CompletedLoops{get;private set;}
    private double compensation;

    public AnimationPreviewClock(float frameRate,ushort endFrame,double timeScale=1,bool loop=true)
    {
        if(!float.IsFinite(frameRate)||frameRate<=0||frameRate>1000||endFrame==0||!double.IsFinite(timeScale)||timeScale<=0||timeScale>1e6)
            throw new InvalidDataException("Invalid preview clock time base.");
        FrameRate=frameRate;EndFrame=endFrame;TimeScale=timeScale;Loop=loop;
    }
    public void Seek(double frame)
    {
        if(!double.IsFinite(frame)||frame<0||frame>EndFrame)throw new InvalidDataException("Preview seek outside clip.");
        Frame=frame;compensation=0;
    }
    public void SetPlaying(bool playing)=>Playing=playing;
    public void SetFocused(bool focused)=>Focused=focused;
    public bool Advance(double seconds)
    {
        if(!double.IsFinite(seconds)||seconds<0||seconds>60)throw new InvalidDataException("Preview elapsed time outside bounded range.");
        if(!Playing||!Focused||seconds==0)return false;
        double delta=seconds*FrameRate*TimeScale-compensation;
        double next=Frame+delta;
        double nextCompensation=(next-Frame)-delta;
        // Modern preview policy: compensated accumulation and an eight-epsilon boundary snap.
        // Fractional fixed ticks must not leave an intended complete cycle just below its endpoint.
        double boundary=Math.Round(next/EndFrame)*EndFrame;
        double tolerance=8*2.2204460492503131e-16*Math.Max(EndFrame,Math.Abs(next));
        if(boundary>0&&Math.Abs(next-boundary)<=tolerance){next=boundary;nextCompensation=0;}
        long loops=CompletedLoops;
        if(Loop)
        {
            long crossed=(long)Math.Floor(next/EndFrame);loops=checked(loops+crossed);
            next%=EndFrame;
        }
        else if(next>=EndFrame){next=EndFrame;Playing=false;}
        bool changed=next!=Frame;Frame=next;CompletedLoops=loops;compensation=nextCompensation;return changed;
    }
}
