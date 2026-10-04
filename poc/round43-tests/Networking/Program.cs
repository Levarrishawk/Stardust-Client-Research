using System.Text.Json;
using Stardust.Core.Protocol;
try
{
 if(args.Length!=2)throw new ArgumentException("fixture receipt");using var doc=JsonDocument.Parse(File.ReadAllBytes(args[0]));var records=new List<object>();int negatives=0,budgets=0,ownership=0;
 void Check(bool v,string why){if(!v)throw new Exception(why);}
 void Reject(Action a,bool budget=false){try{a();}catch(Exception e)when(e is InvalidDataException or ArgumentException){negatives++;if(budget)budgets++;return;}throw new Exception("Expected rejection");}
 var exact=new CreatureView4FullLimits(20,3,0,0,0,0,28,3);
 foreach(var c in doc.RootElement.GetProperty("Cases").EnumerateArray())
 {
  var wire=Convert.FromHexString(c.GetProperty("Wire").GetString()!);var plan=CreatureView4FullCodec.Decode(wire,exact);Check(plan.Updates.Count==3&&plan.Updates.All(u=>u.Scalar is not null),"Scalar-only observer update");Check(CreatureView4FullCodec.Encode(plan,exact).SequenceEqual(wire),"Exact literal bytes");budgets++;
  var initial=c.GetProperty("Initial").EnumerateArray().Select(e=>e.GetUInt32()).ToArray();var baseline=new CreatureView4Baseline(plan.ObjectId,65535,initial,ulong.MaxValue,new(0,Array.Empty<int>()),new(0,Array.Empty<CreatureView4SkillMod>()),new(0,Array.Empty<CreatureView4MissionPair>()));
  var state=CreatureView4FullState.FromBaseline(baseline);var result=state.Apply(plan).Current;Check(result.Model.FloatBits.SequenceEqual(c.GetProperty("Result").EnumerateArray().Select(e=>e.GetUInt32())),"Raw producer arithmetic / existing full model");Check(state.Model.FloatBits.SequenceEqual(initial),"Previous model retained");ownership++;
  var alias=(byte[])wire.Clone();var own=CreatureView4FullCodec.Decode(alias,exact);Array.Clear(alias);Check(CreatureView4FullCodec.Encode(own,exact).SequenceEqual(wire),"Owned literal decode");ownership++;
  for(int cut=0;cut<wire.Length;cut++){int at=cut;Reject(()=>CreatureView4FullCodec.Decode(wire.AsSpan(0,at),exact));}
  foreach(var low in new[]{exact with{MaximumPayloadBytes=19},exact with{MaximumFields=2},exact with{MaximumDecodedBytes=27},exact with{MaximumWork=2}}){Reject(()=>CreatureView4FullCodec.Decode(wire,low),true);Reject(()=>CreatureView4FullCodec.Encode(plan,low),true);}
  records.Add(new{Name=c.GetProperty("Name").GetString(),plan.ObjectId,plan.DeclaredCount,Updates=plan.Updates.Select(u=>new{u.Index,Bits=u.Scalar!.Bits}).ToArray(),FloatBits=result.Model.FloatBits.ToArray(),result.Model.ListenTo,StateRevision=result.Revision,BaselineDeclaredCount=result.Model.DeclaredCount});
 }
 File.WriteAllText(args[1],JsonSerializer.Serialize(new{Records=records,Negatives=negatives,Budgets=budgets,Ownership=ownership},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"R43_LITERAL_PRODUCER_MODEL_PASS cases={records.Count} negatives={negatives} budgets={budgets} ownership={ownership}");
}
catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}
