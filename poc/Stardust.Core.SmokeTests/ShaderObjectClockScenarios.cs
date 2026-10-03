using System.Text.Json;
using Stardust.Core.Runtime;
internal static class ShaderObjectClockScenarios
{
    private sealed record Operation(string Kind,ulong Id=0,uint SecondsBits=0);
    private sealed record Fixture(string Name,int Capacity,ulong MaximumTicks,ulong TickSeed,ulong RevisionSeed,ulong TokenSeed,Operation[] Operations);
    private static int checks;
    private static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
    private static object Global(ShaderObjectClockSnapshot s)=>new{s.Tick,s.Revision,s.RegisteredCount,s.Capacity,s.MaximumTicksPerAdvance,s.NumericPolicy,s.InputUnits,s.OriginalPC24Parity};
    private static object Object(ShaderObjectClockObjectSnapshot s,ShaderObjectClock clock)=>new{s.ObjectId,s.RegistrationToken,s.FromTick,s.ThroughTick,s.LastServicedTick,ElapsedBits=BitConverter.SingleToUInt32Bits(s.ElapsedSeconds),s.Revision,s.InverseScaleBits,s.NumericPolicy,s.OriginalPC24Parity,Current=clock.IsCurrent(s)};
    internal static void Run(string fixturePath,string exportPath)
    {
        checks=0;var fixtures=JsonSerializer.Deserialize<Fixture[]>(File.ReadAllText(fixturePath))!;var results=new List<object>();
        foreach(var f in fixtures)
        {
            ShaderObjectClock? clock=null;
            try{clock=new(f.Capacity,f.MaximumTicks,f.TickSeed,f.RevisionSeed,f.TokenSeed);}
            catch(ArgumentOutOfRangeException){checks++;results.Add(new{f.Name,Constructor="argument",Rows=Array.Empty<object>()});continue;}
            using(clock)
            {
                var initial=clock.Snapshot;Check(initial.Tick==f.TickSeed&&initial.Revision==f.RevisionSeed&&initial.RegisteredCount==0&&clock.IsCurrent(initial),"Initial seeds and current global receipt");
                var rows=new List<object>();var issued=new List<(ShaderObjectClockObjectSnapshot Receipt,ulong From,ulong Through,uint Bits,ulong Revision)>();
                foreach(var op in f.Operations)
                {
                    var before=clock.Snapshot;var beforeObjects=clock.Snapshots().ToArray();var old=clock.Find(op.Id);bool wasClosed=clock.IsClosed;
                    string outcome="ok";ShaderObjectClockObjectSnapshot? receipt=null;bool? removed=null;bool delivery=false;
                    try
                    {
                        switch(op.Kind)
                        {
                            case "advance":clock.Advance(BitConverter.UInt32BitsToSingle(op.SecondsBits));
                                Check(clock.Snapshot.Tick>before.Tick&&clock.Snapshot.Revision==before.Revision+1&&!clock.IsCurrent(before),"Advance changes global only");
                                Check(beforeObjects.All(o=>ReferenceEquals(o,clock.Find(o.ObjectId))&&!clock.IsCurrent(o)),"Advance stales receipt but retains service facts");break;
                            case "register":receipt=clock.Register(op.Id);delivery=true;
                                Check(receipt.FromTick==before.Tick&&receipt.ThroughTick==before.Tick&&receipt.ElapsedSeconds==0&&clock.IsCurrent(receipt)&&clock.Snapshot.RegisteredCount==before.RegisteredCount+1,"Modern registration at current tick");break;
                            case "service":receipt=clock.Service(op.Id);delivery=receipt is not null&&!ReferenceEquals(receipt,old);
                                if(receipt is null)Check(ReferenceEquals(before,clock.Snapshot),"Missing service no mutation");
                                else if(old!.ThroughTick==before.Tick)Check(ReferenceEquals(receipt,old)&&ReferenceEquals(before,clock.Snapshot)&&!delivery&&BitConverter.SingleToUInt32Bits(receipt.ElapsedSeconds)==BitConverter.SingleToUInt32Bits(old.ElapsedSeconds),"Same-tick exact receipt is no new delivery, elapsed value retained");
                                else Check(delivery&&receipt.FromTick==old.ThroughTick&&receipt.ThroughTick==before.Tick&&receipt.RegistrationToken==old.RegistrationToken&&clock.IsCurrent(receipt)&&float.IsFinite(receipt.ElapsedSeconds),"Accumulated per-object service");break;
                            case "remove":removed=clock.Remove(op.Id);Check(removed.Value?clock.Find(op.Id) is null&&!clock.IsCurrent(old!):ReferenceEquals(before,clock.Snapshot),"Remove exact identity/noop");break;
                            case "reset":clock.Reset();Check(clock.Snapshot.Tick==before.Tick&&clock.Snapshot.Revision==before.Revision+1&&clock.Snapshots().Count==0&&beforeObjects.All(o=>!clock.IsCurrent(o)),"Reset clears registrations without tick rewind");break;
                            case "close":clock.Close();Check(clock.IsClosed&&clock.Snapshots().Count==0&&clock.Snapshot.Tick==before.Tick&&clock.Snapshot.Revision==before.Revision&&!clock.IsCurrent(before)&&(!wasClosed||ReferenceEquals(before,clock.Snapshot)),"Close drains/idempotent/counters retained");break;
                            default:throw new Exception("Unknown fixture operation");
                        }
                    }
                    catch(ArgumentOutOfRangeException){outcome="argument";}
                    catch(InvalidDataException){outcome="invalid";}
                    catch(OverflowException){outcome="overflow";}
                    catch(ObjectDisposedException){outcome="closed";}
                    if(outcome!="ok")Check(ReferenceEquals(before,clock.Snapshot)&&beforeObjects.Length==clock.Snapshots().Count&&beforeObjects.All(o=>ReferenceEquals(o,clock.Find(o.ObjectId))),"Rejected operation preserves exact global/object receipts");
                    if(receipt is not null)issued.Add((receipt,receipt.FromTick,receipt.ThroughTick,BitConverter.SingleToUInt32Bits(receipt.ElapsedSeconds),receipt.Revision));
                    rows.Add(new{op.Kind,op.Id,op.SecondsBits,Outcome=outcome,Global=Global(clock.Snapshot),GlobalChanged=!ReferenceEquals(before,clock.Snapshot),DeliveryNew=delivery,Removed=removed,Receipt=receipt is null?null:Object(receipt,clock),Objects=clock.Snapshots().Select(o=>Object(o,clock)).ToArray(),Closed=clock.IsClosed});
                }
                Check(issued.All(p=>p.Receipt.FromTick==p.From&&p.Receipt.ThroughTick==p.Through&&BitConverter.SingleToUInt32Bits(p.Receipt.ElapsedSeconds)==p.Bits&&p.Receipt.Revision==p.Revision),"All issued facts immutable");
                Check(issued.All(p=>clock.IsCurrent(p.Receipt)==(!clock.IsClosed&&ReferenceEquals(clock.Find(p.Receipt.ObjectId),p.Receipt)&&p.Receipt.ThroughTick==clock.Snapshot.Tick)),"Historical service token freshness independent of unrelated revisions");
                var final=clock.Snapshot;var all=clock.Snapshots().ToArray();Exception? error=null;
                Task.Run(()=>{try{clock.Advance(0);}catch(Exception e){error=e;}}).GetAwaiter().GetResult();Check(error is InvalidOperationException&&ReferenceEquals(final,clock.Snapshot),"Foreign owner preflight");
                using(var foreign=new ShaderObjectClock())Check(!foreign.IsCurrent(final)&&all.All(o=>!foreign.IsCurrent(o)),"Foreign tokens rejected");
                clock.Close();clock.Close();Check(clock.Snapshots().Count==0&&!clock.IsCurrent(final),"Final cleanup releases registrations");
                results.Add(new{f.Name,Constructor="ok",Initial=Global(initial),Rows=rows,Final=Global(final)});
            }
        }
        File.WriteAllText(exportPath,JsonSerializer.Serialize(new{Checks=checks,Cases=fixtures.Length,OriginalPC24Parity=false,Results=results},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"R29_SHADER_CLOCK_PASS checks={checks} cases={fixtures.Length}");
    }
}
