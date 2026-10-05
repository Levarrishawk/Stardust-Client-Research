using System.Text;
using System.Text.Json;
using Stardust.Core.Session;
if(args.Length!=2||File.Exists(args[1])||new FileInfo(args[0]).Length is <1 or >262144)throw new ArgumentException("Bounded fresh fixture/output");
using var document=JsonDocument.Parse(File.ReadAllBytes(args[0]));var root=document.RootElement;
if(root.GetProperty("Version").GetInt32()!=2||root.GetProperty("Policy").GetString()!="supplied_nominal_target_and_rate_modern_planar_trace"||root.GetProperty("OriginalEffectiveMovementParity").GetBoolean())throw new InvalidDataException("Synthetic supplied target trace");
uint current=root.GetProperty("InitialSpeedBits").GetUInt32();var rate=root.GetProperty("Rate");
var supplied=new CreatureNominalRate(rate.GetProperty("Magnitude").GetUInt64(),rate.GetProperty("Exponent").GetInt32(),rate.GetProperty("IsNegative").GetBoolean());
var results=new List<CreatureNominalSpeedResult>();var targets=new List<object>();
var precision=(CreatureNominalSpeedPrecision)root.GetProperty("Precision").GetInt32();
foreach(var step in root.GetProperty("Steps").EnumerateArray()){
 var w=step.GetProperty("DirectionWordsInRecoveredOrder");if(w.GetArrayLength()!=3)throw new InvalidDataException("Three ordered direction words");
 var f=step.GetProperty("Factor");var target=CreatureNominalTargetSpeed.Evaluate(step.GetProperty("RunSpeedBits").GetUInt32(),step.GetProperty("WalkSpeedBits").GetUInt32(),step.GetProperty("RunFlag").GetByte(),
  new(w[0].GetUInt32(),w[1].GetUInt32(),w[2].GetUInt32()),new(f.GetProperty("Magnitude").GetUInt64(),f.GetProperty("Exponent").GetInt32(),f.GetProperty("IsNegative").GetBoolean()),precision);
 targets.Add(new{target.TargetBits,target.IntermediateBits,target.SelectedSpeedBits,RunSelected=target.Selection==CreatureNominalTargetSelection.Run,
  Magnitude=target.RetainedMagnitude,Intermediate=target.RetainedIntermediate,target.RetainedTarget,target.RetainedSquaredMagnitude,target.ReservedWork});
 var result=CreatureNominalSpeedTransition.Evaluate(current,target.TargetBits,root.GetProperty("SecondsBits").GetUInt32(),precision,supplied);
 results.Add(result);current=result.StoredBits;
}
File.WriteAllText(args[1],JsonSerializer.Serialize(new{Targets=targets,Steps=results,OriginalEffectiveMovementParity=false})+"\n",new UTF8Encoding(false));
Console.WriteLine("PASS: supplied target/transition trace");

