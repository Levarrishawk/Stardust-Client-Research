using System.Text.Json;
using Stardust.Core.Protocol;

try
{
    using var fixture=JsonDocument.Parse(File.ReadAllBytes(args[0]));
    var root=fixture.RootElement;
    if(!root.GetProperty("Synthetic").GetBoolean())throw new InvalidDataException("Synthetic peer required");
    var baseline=TangibleView6Codec.DecodeBaseline(Convert.FromHexString(root.GetProperty("Baseline").GetString()!),new());
    var state=TangibleView6State.FromBaseline(baseline);
    var records=new List<object>();
    void Record(string name)=>records.Add(new{Name=name,state.ObjectId,state.Passive,state.Counter,Ids=state.Ids.ToArray(),state.Revision,state.DecodedBytes});
    Record("seed");
    foreach(var step in root.GetProperty("Steps").EnumerateArray())
    {
        var delta=TangibleView6Codec.DecodeDelta(Convert.FromHexString(step.GetProperty("Hex").GetString()!),new());
        state=state.Apply(delta).Current;
        Record(step.GetProperty("Name").GetString()!);
    }
    File.WriteAllText(args[1],JsonSerializer.Serialize(records,new JsonSerializerOptions{WriteIndented=true}));
    Console.WriteLine("R32_LEAD_TANO6_PEER_PASS states="+records.Count);
}
catch(Exception error){Console.Error.WriteLine(error);Environment.ExitCode=1;}
