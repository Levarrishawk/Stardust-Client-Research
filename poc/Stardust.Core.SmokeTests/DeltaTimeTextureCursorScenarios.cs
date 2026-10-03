using System.Text.Json;
using Stardust.Core.Runtime;
internal static class DeltaTimeTextureCursorScenarios
{
    private sealed record Operation(string Kind,uint DeltaBits=0);
    private sealed record Fixture(string Name,int FrameCount,uint IntervalBits,int Work,ulong RevisionSeed,ulong CumulativeSeed,Operation[] Operations);
    private static int checks;
    private static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
    private static void Reject<T>(Action action)where T:Exception{try{action();}catch(T){checks++;return;}throw new Exception("Expected "+typeof(T).Name);}
    private static object State(DeltaTimeTextureCursorSnapshot s)=>new{s.FrameCount,IntervalBits=BitConverter.SingleToUInt32Bits(s.Interval),s.MaximumCatchUpSteps,s.NumericPolicy,s.InputUnits,s.OriginalX87Parity,s.Index,ResidualBits=BitConverter.SingleToUInt32Bits(s.Residual),s.Revision,s.CumulativeAdvances,s.LastAdvanceSteps};
    internal static void Run(string fixturePath,string exportPath)
    {
        checks=0;var fixtures=JsonSerializer.Deserialize<Fixture[]>(File.ReadAllText(fixturePath))!;var results=new List<object>();
        foreach(var f in fixtures)
        {
            DeltaTimeTextureCursor? cursor=null;
            try{cursor=new(f.FrameCount,BitConverter.UInt32BitsToSingle(f.IntervalBits),f.Work,f.RevisionSeed,f.CumulativeSeed);}
            catch(ArgumentOutOfRangeException){checks++;results.Add(new{f.Name,Constructor="argument",Rows=Array.Empty<object>()});continue;}
            using(cursor)
            {
                var initial=cursor.Snapshot;Check(initial.Index==0&&BitConverter.SingleToUInt32Bits(initial.Residual)==0&&initial.LastAdvanceSteps==0&&initial.Revision==f.RevisionSeed&&initial.CumulativeAdvances==f.CumulativeSeed,"Initial state/counter seeds");
                var rows=new List<object>();var issued=new List<(DeltaTimeTextureCursorSnapshot Snapshot,int Index,uint Bits,ulong Revision,ulong Advances)>();
                foreach(var op in f.Operations)
                {
                    var before=cursor.Snapshot;issued.Add((before,before.Index,BitConverter.SingleToUInt32Bits(before.Residual),before.Revision,before.CumulativeAdvances));
                    string outcome="ok";
                    try
                    {
                        var after=op.Kind=="reset"?cursor.Reset():cursor.Advance(BitConverter.UInt32BitsToSingle(op.DeltaBits));
                        Check(ReferenceEquals(after,cursor.Snapshot)&&cursor.IsCurrent(after)&&!cursor.IsCurrent(before),"Exact receipt supersession");
                        Check(after.Revision==before.Revision+1&&after.LastAdvanceSteps>=0&&after.LastAdvanceSteps<=f.Work,"Monotonic revision and bounded exact steps");
                        Check(float.IsFinite(after.Residual)&&after.Residual>=0&&after.Residual<=after.Interval,"Float32 residual permits equality");
                        if(op.Kind=="reset")Check(after.Index==0&&BitConverter.SingleToUInt32Bits(after.Residual)==0&&after.CumulativeAdvances==before.CumulativeAdvances&&after.LastAdvanceSteps==0,"Reset keeps cumulative counter");
                        else Check(after.CumulativeAdvances==before.CumulativeAdvances+(ulong)after.LastAdvanceSteps&&after.Index==(before.Index+(long)after.LastAdvanceSteps)%f.FrameCount,"Exact work/modulus/cumulative relationship");
                    }
                    catch(ArgumentOutOfRangeException){outcome="argument";}
                    catch(InvalidDataException){outcome="budget";}
                    catch(OverflowException){outcome="overflow";}
                    if(outcome!="ok")Check(ReferenceEquals(before,cursor.Snapshot)&&cursor.IsCurrent(before),"Failed advance/reset atomic exact receipt");
                    rows.Add(new{op.Kind,op.DeltaBits,Outcome=outcome,State=State(cursor.Snapshot)});
                }
                Check(issued.All(p=>p.Snapshot.Index==p.Index&&BitConverter.SingleToUInt32Bits(p.Snapshot.Residual)==p.Bits&&p.Snapshot.Revision==p.Revision&&p.Snapshot.CumulativeAdvances==p.Advances),"Issued receipts immutable across updates/failures");
                var final=cursor.Snapshot;Exception? foreignError=null;
                Task.Run(()=>{try{cursor.Advance(0);}catch(Exception e){foreignError=e;}}).GetAwaiter().GetResult();
                Check(foreignError is InvalidOperationException&&ReferenceEquals(final,cursor.Snapshot),"Foreign owner rejection precedes mutation");
                using(var foreign=new DeltaTimeTextureCursor(f.FrameCount,BitConverter.UInt32BitsToSingle(f.IntervalBits),f.Work))Check(!foreign.IsCurrent(final),"Foreign receipt identity");
                cursor.Close();cursor.Close();Check(cursor.IsClosed&&!cursor.IsCurrent(final)&&ReferenceEquals(final,cursor.Snapshot),"Idempotent close preserves receipt/counters");
                Reject<ObjectDisposedException>(()=>cursor.Advance(0));Reject<ObjectDisposedException>(()=>cursor.Reset());
                results.Add(new{f.Name,Constructor="ok",Initial=State(initial),Rows=rows,Final=State(final)});
            }
        }
        File.WriteAllText(exportPath,JsonSerializer.Serialize(new{Checks=checks,Cases=fixtures.Length,NumericPolicy="binary64-intermediate-float32-persistent",InputUnits="abstract-matching-units",OriginalX87Parity=false,Results=results},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"R28_DELTA_TIME_CURSOR_PASS checks={checks} cases={fixtures.Length}");
    }
}
