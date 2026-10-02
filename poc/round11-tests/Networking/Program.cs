using System.Text.Json;
using Stardust.Core.Protocol;
using Stardust.Core.SmokeTests;
CreatureView3MixedDeltaScenarios.Run();
string input=args[0],output=args[1];Directory.CreateDirectory(output);
byte[] bytes=File.ReadAllBytes(Path.Combine(input,"baseline.bin"));
var baseline=CreatureView3BaselineCodec.Decode(bytes,new(bytes.Length-23,3,3,3,119));
var seed=CreatureView3MixedState.FromBaseline(baseline,16); Array.Clear(bytes);
object Export(CreatureView3MixedState state)=>new{state.ObjectId,state.Scalars.Posture,state.Scalars.Rank,state.Scalars.LinkId,HeightBits=(uint)BitConverter.SingleToInt32Bits(state.Scalars.Height),state.Scalars.Shock,state.Scalars.States,WoundsCounter=state.Wounds.Counter,Wounds=state.Wounds.Values};
File.WriteAllText(Path.Combine(output,"seed.json"),JsonSerializer.Serialize(Export(seed)));
var specs=JsonSerializer.Deserialize<CaseSpec[]>(File.ReadAllText(Path.Combine(input,"cases.json")))!;List<object> results=new();
foreach(var spec in specs)
{
    bool decoded=false;
    try
    {
        byte[] wire=File.ReadAllBytes(Path.Combine(input,spec.File));
        var plan=CreatureView3MixedDeltaCodec.Decode(wire,CreatureView3MixedDeltaScenarios.Limits,(CreatureWoundsApplicationPolicy)spec.Policy);decoded=true;
        File.WriteAllBytes(Path.Combine(output,spec.Name+".bin"),CreatureView3MixedDeltaCodec.Encode(plan,CreatureView3MixedDeltaScenarios.Limits));
        var final=plan.Apply(seed);results.Add(new{spec.Name,Decoded=true,Accepted=true,final.ObjectId,final.Scalars.Posture,final.Scalars.Rank,final.Scalars.LinkId,HeightBits=(uint)BitConverter.SingleToInt32Bits(final.Scalars.Height),final.Scalars.Shock,final.Scalars.States,WoundsCounter=final.Wounds.Counter,Wounds=final.Wounds.Values});
    }
    catch(InvalidDataException){results.Add(new{spec.Name,Decoded=decoded,Accepted=false,seed.ObjectId,seed.Scalars.Posture,seed.Scalars.Rank,seed.Scalars.LinkId,HeightBits=(uint)BitConverter.SingleToInt32Bits(seed.Scalars.Height),seed.Scalars.Shock,seed.Scalars.States,WoundsCounter=seed.Wounds.Counter,Wounds=seed.Wounds.Values});}
    if(seed.Wounds.Counter!=0 || !seed.Wounds.Values.SequenceEqual(new[]{-2,int.MaxValue,-2}))throw new Exception("Seed mutation.");
}
File.WriteAllText(Path.Combine(output,"results.json"),JsonSerializer.Serialize(results));
record CaseSpec(string Name,string File,int Policy);
