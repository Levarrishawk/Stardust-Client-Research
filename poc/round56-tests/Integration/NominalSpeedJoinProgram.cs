using System.Text;
using System.Text.Json;
using Stardust.Core.Session;
if(args.Length!=2||File.Exists(args[1])||new FileInfo(args[0]).Length is <1 or >262144)throw new ArgumentException("Bounded fresh fixture/output");
using var document=JsonDocument.Parse(File.ReadAllBytes(args[0]));var root=document.RootElement;
if(root.GetProperty("Version").GetInt32()!=1||root.GetProperty("OriginalEffectiveMovementParity").GetBoolean())throw new InvalidDataException("Synthetic nominal trace");
uint current=root.GetProperty("InitialSpeedBits").GetUInt32();var rate=root.GetProperty("Rate");
var supplied=new CreatureNominalRate(rate.GetProperty("Magnitude").GetUInt64(),rate.GetProperty("Exponent").GetInt32(),rate.GetProperty("IsNegative").GetBoolean());
var results=new List<CreatureNominalSpeedResult>();
foreach(var step in root.GetProperty("Steps").EnumerateArray()){
 var result=CreatureNominalSpeedTransition.Evaluate(current,step.GetProperty("TargetBits").GetUInt32(),root.GetProperty("SecondsBits").GetUInt32(),(CreatureNominalSpeedPrecision)root.GetProperty("Precision").GetInt32(),supplied);
 results.Add(result);current=result.StoredBits;
}
File.WriteAllText(args[1],JsonSerializer.Serialize(new{Steps=results,OriginalEffectiveMovementParity=false})+"\n",new UTF8Encoding(false));
Console.WriteLine("PASS: supplied nominal transition trace");
