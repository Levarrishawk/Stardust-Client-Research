using System.Text.Json;
using Stardust.Core.Runtime;
internal static class SceneShaderTexturePlaybackScenarios
{
 private sealed record Op(string Kind,ulong Id=1,int Count=8,uint IntervalBits=0x3DCCCCCD,int Work=4096,ulong CursorSeed=0,uint SecondsBits=0);
 private sealed record Fixture(string Name,int Capacity,ulong MaxTicks,ulong Tick,ulong ClockRevision,ulong Token,ulong Revision,Op[] Operations);
 private static int checks;
 private static void Check(bool v,string why){checks++;if(!v)throw new Exception(why);}
 private static object State(ShaderTexturePlaybackSnapshot s,ShaderTexturePlayback p)=>new{s.ObjectId,s.RegistrationToken,s.Revision,Through=s.Clock.ThroughTick,From=s.Clock.FromTick,ElapsedBits=BitConverter.SingleToUInt32Bits(s.Clock.ElapsedSeconds),ClockRevision=s.Clock.Revision,s.Cursor.Index,ResidualBits=BitConverter.SingleToUInt32Bits(s.Cursor.Residual),CursorRevision=s.Cursor.Revision,s.Cursor.CumulativeAdvances,s.Cursor.LastAdvanceSteps,Current=p.IsCurrent(s)};
 internal static void Run(string input,string output)
 {
  checks=0;var fs=JsonSerializer.Deserialize<Fixture[]>(File.ReadAllText(input))!;var results=new List<object>();
  foreach(var f in fs)
  {
   using var p=new ShaderTexturePlayback(f.Capacity,f.MaxTicks,f.Tick,f.ClockRevision,f.Token,f.Revision);var rows=new List<object>();
   foreach(var op in f.Operations)
   {
    var before=p.ClockSnapshot;var states=p.Snapshots().ToArray();var old=p.Find(op.Id);string outcome="ok";ShaderTexturePlaybackSnapshot? result=null;
    try{switch(op.Kind){case "register":result=p.Register(op.Id,op.Count,BitConverter.UInt32BitsToSingle(op.IntervalBits),op.Work,op.CursorSeed);break;case "advance":p.Advance(BitConverter.UInt32BitsToSingle(op.SecondsBits));break;case "service":result=p.Service(op.Id);if(ReferenceEquals(old,result)&&result is not null)Check(ReferenceEquals(old,result)&&p.ClockSnapshot.Revision==before.Revision,"Same receipt never redelivered");break;case "remove":p.Remove(op.Id);break;case "reset":p.Reset();break;case "close":p.Close();break;default:throw new Exception("operation");}}
    catch(ArgumentOutOfRangeException){outcome="argument";}catch(InvalidDataException){outcome="invalid";}catch(OverflowException){outcome="overflow";}catch(ObjectDisposedException){outcome="closed";}
    if(outcome!="ok")Check(p.IsFaulted?(p.IsClosed&&p.ClockSnapshot.RegisteredCount==0&&p.Snapshots().Count==0&&states.All(s=>!p.IsCurrent(s))):(ReferenceEquals(before,p.ClockSnapshot)&&states.All(s=>ReferenceEquals(s,p.Find(s.ObjectId)))),"Retained failure or explicit terminal drain");
    Check(states.All(s=>s.ObjectId==s.Clock.ObjectId&&s.Cursor.FrameCount>0),"Issued immutable owned records");
    rows.Add(new{Outcome=outcome,Tick=p.ClockSnapshot.Tick,ClockRevision=p.ClockSnapshot.Revision,Revision=p.Revision,Closed=p.IsClosed,Faulted=p.IsFaulted,Delivery=result is not null&&!ReferenceEquals(old,result),States=p.Snapshots().Select(s=>State(s,p)).ToArray()});
   }
   var final=p.ClockSnapshot;Exception? error=null;Task.Run(()=>{try{p.Advance(0);}catch(Exception e){error=e;}}).GetAwaiter().GetResult();Check(error is InvalidOperationException&&ReferenceEquals(final,p.ClockSnapshot),"Owner rejection");
   using var foreign=new ShaderTexturePlayback();Check(p.Snapshots().All(s=>!foreign.IsCurrent(s)),"Foreign receipts");p.Close();p.Close();Check(p.Snapshots().Count==0,"Cleanup");results.Add(new{f.Name,Rows=rows});
  }
  File.WriteAllText(output,JsonSerializer.Serialize(new{Checks=checks,Cases=fs.Length,OriginalParity=false,Results=results},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"R30_PLAYBACK_PASS checks={checks} cases={fs.Length}");
 }
}
