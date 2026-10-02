using System.Text.Json;
using Stardust.Core.Protocol;
using Stardust.Core.SmokeTests;
CreatureView3ComponentsDeltaScenarios.Run();
string input=args[0],output=args[1];Directory.CreateDirectory(output);
byte[] bytes=File.ReadAllBytes(Path.Combine(input,"baseline.bin"));var full=CreatureView3BaselineCodec.Decode(bytes,new(bytes.Length-23,3,3,3,119));
var snapshot=CreatureView3ComponentsSnapshot.FromBaseline(full,3);var seed=snapshot.ToState(2);Array.Clear(bytes);
File.WriteAllText(Path.Combine(output,"seed.json"),JsonSerializer.Serialize(new{seed.ObjectId,seed.Counter,snapshot.RawCount,snapshot.RawKeys,seed.Keys}));
byte[] field=File.ReadAllBytes(Path.Combine(input,"snapshot.bin"));var standalone=CreatureView3ComponentsDeltaCodec.DecodeSnapshot(seed.ObjectId,field,3);Array.Clear(field);
if(!standalone.RawKeys.SequenceEqual(snapshot.RawKeys) || standalone.Counter!=snapshot.Counter)throw new Exception("Baseline/raw snapshot mismatch.");
File.WriteAllBytes(Path.Combine(output,"snapshot.bin"),CreatureView3ComponentsDeltaCodec.EncodeSnapshot(standalone,3));
var specs=JsonSerializer.Deserialize<CaseSpec[]>(File.ReadAllText(Path.Combine(input,"cases.json")))!;List<object> results=new();
foreach(var spec in specs)
{
    var initial=new CreatureView3ComponentsState(seed.ObjectId,spec.Counter,spec.RawKeys,spec.Elements);bool decoded=false;
    try
    {
        var limits=CreatureView3ComponentsDeltaScenarios.Limits with{MaximumElements=spec.Elements,MaximumKeys=spec.KeyBudget};
        var plan=CreatureView3ComponentsDeltaCodec.Decode(File.ReadAllBytes(Path.Combine(input,spec.File)),limits);decoded=true;
        File.WriteAllBytes(Path.Combine(output,spec.Name+".bin"),CreatureView3ComponentsDeltaCodec.Encode(plan,limits));
        var final=plan.Apply(initial);results.Add(new{spec.Name,Decoded=true,Accepted=true,final.Counter,final.Keys});
    }
    catch(InvalidDataException){results.Add(new{spec.Name,Decoded=decoded,Accepted=false,initial.Counter,initial.Keys});}
    if(initial.Counter!=spec.Counter || !initial.Keys.SequenceEqual(spec.RawKeys.Distinct().Order()))throw new Exception("Initial set mutated.");
}
File.WriteAllText(Path.Combine(output,"results.json"),JsonSerializer.Serialize(results));
record CaseSpec(string Name,string File,uint Counter,int[] RawKeys,int Elements,int KeyBudget);
