using System.Buffers.Binary;
using System.Text.Json;
using Stardust.Core.Protocol;
using Stardust.Core.SmokeTests;
CreatureView3WoundsDeltaScenarios.Run();
string input=args[0], output=args[1]; Directory.CreateDirectory(output);
byte[] wire=File.ReadAllBytes(Path.Combine(input,"input.bin"));
if (!wire.SequenceEqual(CreatureView3WoundsDeltaScenarios.Golden())) throw new Exception("Independent golden disagrees.");
var limits=CreatureView3WoundsDeltaScenarios.Limits;
var plan=CreatureView3WoundsDeltaCodec.Decode(wire,limits);
File.WriteAllBytes(Path.Combine(output,"encoded.bin"),CreatureView3WoundsDeltaCodec.Encode(plan,limits));
BinaryPrimitives.WriteUInt16LittleEndian(wire.AsSpan(23),65535);
File.WriteAllBytes(Path.Combine(output,"encoded-count65535.bin"),CreatureView3WoundsDeltaCodec.Encode(CreatureView3WoundsDeltaCodec.Decode(wire,limits),limits));
File.WriteAllText(Path.Combine(output,"plan.json"),JsonSerializer.Serialize(new{plan.ObjectId,plan.DeclaredCount,Updates=plan.Updates.Select(u=>new{u.Counter,u.CommandCount,Commands=u.Commands.Select(c=>new{Kind=(byte)c.Kind,c.Index,c.Values})})}));
byte[] baseline=File.ReadAllBytes(Path.Combine(input,"baseline.bin"));
var snapshot=CreatureView3BaselineCodec.Decode(baseline,new(baseline.Length-23,3,3,3,119));
var seeded=CreatureView3WoundsState.FromBaseline(snapshot,8); var final=plan.Apply(seeded);
File.WriteAllText(Path.Combine(output,"baseline-result.json"),JsonSerializer.Serialize(new{final.ObjectId,final.Counter,final.Values}));
var specs=JsonSerializer.Deserialize<CaseSpec[]>(File.ReadAllText(Path.Combine(input,"cases.json")))!;
List<object> results=new();
foreach(var spec in specs)
{
    var initial=new CreatureView3WoundsState(plan.ObjectId,spec.Counter,spec.Values,8);
    try
    {
        var decoded=CreatureView3WoundsDeltaCodec.Decode(File.ReadAllBytes(Path.Combine(input,spec.File)),limits);
        var state=decoded.Apply(initial); results.Add(new{spec.Name,Accepted=true,state.Counter,state.Values});
    }
    catch(InvalidDataException) { results.Add(new{spec.Name,Accepted=false,initial.Counter,initial.Values}); }
    if (initial.Counter!=spec.Counter || !initial.Values.SequenceEqual(spec.Values)) throw new Exception("Atomic state failure.");
}
File.WriteAllText(Path.Combine(output,"results.json"),JsonSerializer.Serialize(results));
record CaseSpec(string Name,string File,uint Counter,int[] Values);
