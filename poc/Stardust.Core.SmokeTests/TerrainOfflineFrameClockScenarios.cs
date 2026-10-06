using System.Text.Json;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;

public static class TerrainOfflineFrameClockScenarios
{
    private static int checks, negatives;
    private static void Check(bool value,string why) { checks++; if(!value)throw new InvalidDataException(why); }
    private static void Reject(Action action)
    {
        try { action(); } catch(Exception e) when(e is ArgumentException or InvalidOperationException or ObjectDisposedException or OperationCanceledException or OverflowException)
        { checks++; negatives++; return; } throw new InvalidDataException("Expected planner rejection.");
    }
    public static object Run(string input)
    {
        checks=negatives=0;using var doc=JsonDocument.Parse(File.ReadAllText(input));var output=new List<object>();
        foreach(var c in doc.RootElement.GetProperty("Cases").EnumerateArray())
        {
            long step=c.GetProperty("Step").GetInt64(),frameCap=c.GetProperty("FrameCap").GetInt64();int perFrame=c.GetProperty("PerFrame").GetInt32(),totalCap=c.GetProperty("TotalCap").GetInt32();
            using var clock=new TerrainOfflineFrameClock(step,perFrame,totalCap,frameCap);var rows=new List<TerrainOfflineFrameReceipt>();long elapsedTotal=0;int skipped=0,completed=0,index=0;
            void Unchanged(Action action){var before=clock.State;Reject(action);Check(ReferenceEquals(before,clock.State),"planner preflight retained exact state");}
            foreach(var v in c.GetProperty("Frames").EnumerateArray())
            {
                long delta=v.GetInt64();
                if(index<2 || index==c.GetProperty("Frames").GetArrayLength()-1)
                {Unchanged(()=>clock.AdvanceFrame(-1));Unchanged(()=>clock.AdvanceFrame(frameCap+1));Unchanged(()=>clock.AdvanceFrame(long.MaxValue));Unchanged(()=>clock.AdvanceFrame(delta,new CancellationToken(true)));}
                var prior=clock.State;var result=clock.AdvanceFrame(delta);elapsedTotal+=delta;
                Check(ReferenceEquals(result,clock.State)&&result.Steps<=perFrame&&result.TotalReservedSteps<=totalCap&&result.RemainderMicroseconds>=0&&result.RemainderMicroseconds<step&&result.TotalReservedSteps==prior.TotalReservedSteps+result.Steps&&clock.Exhausted==result.Exhausted,"bounded owned receipt and total reservation");
                Check(elapsedTotal==(long)result.TotalReservedSteps*step+result.TotalDroppedMicroseconds+result.RemainderMicroseconds,"exact elapsed conservation across late/drop/cap");
                if(index%17==0)skipped+=result.Steps;else completed+=result.Steps;
                rows.Add(result);index++;
            }
            Check(completed+skipped==clock.State.TotalReservedSteps&&skipped>0,"reserved slots independent of mock caller completion, no refund");
            var first=rows[0];Check(first.TotalReservedSteps<=clock.State.TotalReservedSteps&&ReferenceEquals(first,rows[0]),"historical immutable receipt retained");
            var owner=Task.Run(()=>{try{_ = clock.Exhausted;return false;}catch(InvalidOperationException){return true;}}).GetAwaiter().GetResult();Check(owner,"owner exhausted getter");
            owner=Task.Run(()=>{try{clock.AdvanceFrame(0);return false;}catch(InvalidOperationException){return true;}}).GetAwaiter().GetResult();Check(owner,"owner mutation");
            owner=Task.Run(()=>{try{clock.Dispose();return false;}catch(InvalidOperationException){return true;}}).GetAwaiter().GetResult();Check(owner,"owner dispose");
            var last=clock.State;bool exhausted=clock.Exhausted;clock.Dispose();clock.Dispose();Check(ReferenceEquals(last,clock.State)&&clock.Exhausted==exhausted,"terminal disposal preserves owned historical state");Unchanged(()=>clock.AdvanceFrame(0));
            output.Add(new {Name=c.GetProperty("Name").GetString(),Rows=rows,CallerCompleted=completed,CallerSkipped=skipped});
        }
        foreach(var step in new long[]{0,999,100001,long.MaxValue})Reject(()=>new TerrainOfflineFrameClock(step));
        foreach(var perFrame in new[]{0,33,int.MaxValue})Reject(()=>new TerrainOfflineFrameClock(maxStepsPerFrame:perFrame));
        foreach(var total in new[]{0,6001,int.MaxValue})Reject(()=>new TerrainOfflineFrameClock(maximumReservedSteps:total));
        foreach(var frame in new long[]{0,250001,long.MaxValue})Reject(()=>new TerrainOfflineFrameClock(maximumFrameMicroseconds:frame));
        Reject(()=>new TerrainOfflineFrameClock(token:new CancellationToken(true)));
        return new {Cases=output,NativeChecks=checks,NegativeChecks=negatives};
    }
}
