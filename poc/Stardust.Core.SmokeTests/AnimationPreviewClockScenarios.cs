using Stardust.Core.Assets;

internal static class AnimationPreviewClockScenarios
{
    internal static void Run()
    {
        var clock=new AnimationPreviewClock(30,120);clock.SetPlaying(true);
        for(int i=0;i<480;i++)clock.Advance(1d/60);
        if(clock.Frame!=0||clock.CompletedLoops!=2)throw new Exception("Preview repeated loop boundary mismatch.");
        foreach(ushort end in new ushort[]{1,7,110,117,131,32767})foreach(double scale in new[]{.3,1,2.5})
        {
            var fractional=new AnimationPreviewClock(30,end,scale);fractional.SetPlaying(true);
            for(int tick=0;tick<480;tick++)fractional.Advance(end/(30d*scale*240));
            var stopped=new AnimationPreviewClock(30,end,scale,loop:false);stopped.SetPlaying(true);
            for(int tick=0;tick<240;tick++)stopped.Advance(end/(30d*scale*240));
            if(stopped.Playing||stopped.Frame!=end)throw new Exception("Fractional one-shot failed to stop.");
            if(fractional.CompletedLoops!=2||Math.Abs(fractional.Frame)>1e-9)throw new Exception($"Fractional loop precision mismatch end={end} scale={scale} frame={fractional.Frame:R} loops={fractional.CompletedLoops}");
        }
        clock.Seek(30);clock.SetPlaying(false);if(clock.Advance(1)||clock.Frame!=30)throw new Exception("Paused preview moved.");
        clock.SetPlaying(true);clock.SetFocused(false);if(clock.Advance(10)||clock.Frame!=30)throw new Exception("Unfocused preview moved.");
        clock.SetFocused(true);clock.Advance(.5);if(clock.Frame!=45)throw new Exception("Focused preview caught up unexpectedly.");
        clock.Seek(120);clock.Advance(.5);if(clock.Frame!=15||clock.CompletedLoops!=3)throw new Exception("Endpoint seek/loop mismatch.");
        var once=new AnimationPreviewClock(30,120,2,loop:false);once.SetPlaying(true);once.Advance(1);if(once.Frame!=60)throw new Exception("Proxy preview scale mismatch.");
        once.Advance(2);if(once.Frame!=120||once.Playing||once.Advance(1))throw new Exception("One-shot endpoint did not stop.");
        var nearBoundary=new AnimationPreviewClock(30,131);nearBoundary.SetPlaying(true);nearBoundary.Seek(130);
        nearBoundary.Advance((1-1e-10)/30);
        if(nearBoundary.CompletedLoops!=0||nearBoundary.Frame>=131)throw new Exception("Clock snapped a materially incomplete loop.");
        nearBoundary.Seek(0);nearBoundary.Advance(1d/30);if(nearBoundary.Frame!=1)throw new Exception("Seek retained prior numerical compensation.");
        void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
        Reject(()=>clock.Advance(double.NaN));Reject(()=>clock.Advance(-1));Reject(()=>clock.Advance(61));Reject(()=>clock.Seek(-1));Reject(()=>clock.Seek(121));
        Reject(()=>new AnimationPreviewClock(0,120));Reject(()=>new AnimationPreviewClock(30,0));Reject(()=>new AnimationPreviewClock(30,120,double.PositiveInfinity));
        if(clock.Frame!=15)throw new Exception("Invalid preview request mutated frame.");
        Console.WriteLine("PASS: explicit preview clock fractional durations/scales compensated loop/one-shot boundaries, fixed ticks/two loops, seek/end, pause/focus without catch-up, proxy scale, one-shot stop and pre-mutation invalid time gates");
    }
}
