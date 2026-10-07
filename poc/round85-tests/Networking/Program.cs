using System.Text.Json;
using Stardust.Core.Protocol;
using var document=JsonDocument.Parse(File.ReadAllText(args[0]));
var root=document.RootElement;
var apps=root.GetProperty("Applications");
var four=CreatureView4Codec.DecodeBaseline(Convert.FromHexString(apps.GetProperty("SelectedCreo4").GetString()!),new());
if(root.GetProperty("Variant").GetString()=="truncated6") {
    try { CreatureView6Codec.DecodeBaseline(Convert.FromHexString(apps.GetProperty("SelectedCreo6").GetString()!),new()); }
    catch(InvalidDataException) { Console.WriteLine("PASS truncated6 native rejection");return; }
    throw new InvalidDataException("Truncated6 accepted");
}
var six=CreatureView6Codec.DecodeBaseline(Convert.FromHexString(apps.GetProperty("SelectedCreo6").GetString()!),new());
four=CreatureView4FullState.FromBaseline(four).Model;
six=CreatureView6FullState.FromBaseline(six).Model;
File.WriteAllText(args[1],JsonSerializer.Serialize(Networking85Observation.Project(four,six)));
Console.WriteLine("PASS native complete4/6 decoding/state projections; session join separate");
