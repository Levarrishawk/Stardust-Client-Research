using System.Text.Json;
using Stardust.Core.Protocol;
using Stardust.Core.SmokeTests;
CreatureView3WoundsDeltaScenarios.Run(); CreatureWoundsLegacyPolicyScenarios.Run();
string input=args[0],output=args[1];Directory.CreateDirectory(output);
var specs=JsonSerializer.Deserialize<CaseSpec[]>(File.ReadAllText(Path.Combine(input,"cases.json")))!;
List<object> results=new();
foreach(var spec in specs)
{
    var initial=new CreatureView3WoundsState(0xF122334455667788,spec.Counter,spec.Values,spec.Elements);
    bool decoded=false;
    try
    {
        byte[] wire=File.ReadAllBytes(Path.Combine(input,spec.File));
        var limits=CreatureWoundsLegacyPolicyScenarios.Limits with{MaximumElements=spec.Elements};
        var plan=CreatureView3WoundsDeltaCodec.Decode(wire,limits,CreatureWoundsApplicationPolicy.LegacyBounded);decoded=true;
        File.WriteAllBytes(Path.Combine(output,spec.Name+".bin"),CreatureView3WoundsDeltaCodec.Encode(plan,limits));
        var final=plan.Apply(initial);results.Add(new{spec.Name,Decoded=true,Accepted=true,final.Counter,final.Values});
    }
    catch(InvalidDataException){results.Add(new{spec.Name,Decoded=decoded,Accepted=false,initial.Counter,initial.Values});}
    if(initial.Counter!=spec.Counter || !initial.Values.SequenceEqual(spec.Values))throw new Exception("Initial state mutated.");
}
File.WriteAllText(Path.Combine(output,"results.json"),JsonSerializer.Serialize(results));
record CaseSpec(string Name,string File,uint Counter,int[] Values,int Elements);
