using System.Text;
using System.Text.Json;
using Stardust.Core.Session;
if(args.Length!=2||File.Exists(args[1])||new FileInfo(args[0]).Length is <1 or >262144)throw new ArgumentException("Bounded fresh fixture/output");
using var document=JsonDocument.Parse(File.ReadAllBytes(args[0]));var root=document.RootElement;
if(root.GetProperty("Version").GetInt32()!=3||root.GetProperty("Policy").GetString()!="supplied_local_current_reader_and_nominal_plan_modern_planar_trace"||root.GetProperty("OriginalEffectiveMovementParity").GetBoolean())throw new InvalidDataException("Synthetic supplied target trace");
uint current=root.GetProperty("InitialSpeedBits").GetUInt32();var rate=root.GetProperty("Rate");
var supplied=new CreatureNominalRate(rate.GetProperty("Magnitude").GetUInt64(),rate.GetProperty("Exponent").GetInt32(),rate.GetProperty("IsNegative").GetBoolean());
var precision=(CreatureNominalSpeedPrecision)root.GetProperty("Precision").GetInt32();
var words=root.GetProperty("CurrentSourceWords");if(words.GetArrayLength()!=4)throw new InvalidDataException("Four supplied current source words");
var run=CreatureSuppliedCurrentSpeedReader.Evaluate(words[0].GetUInt32(),words[2].GetUInt32(),words[3].GetUInt32(),CreatureSuppliedCurrentSpeedPolicy.SuppliedLocalOnly,precision);
var walk=CreatureSuppliedCurrentSpeedReader.Evaluate(words[1].GetUInt32(),words[2].GetUInt32(),words[3].GetUInt32(),CreatureSuppliedCurrentSpeedPolicy.SuppliedLocalOnly,precision);
var steps=root.GetProperty("Steps").EnumerateArray().Select(step=>{
 var w=step.GetProperty("DirectionWordsInRecoveredOrder");if(w.GetArrayLength()!=3)throw new InvalidDataException("Three ordered direction words");
 var f=step.GetProperty("Factor");return new CreatureNominalSpeedPlanStep(run.ReturnBits,walk.ReturnBits,step.GetProperty("RunFlag").GetByte(),
 new(w[0].GetUInt32(),w[1].GetUInt32(),w[2].GetUInt32()),new(f.GetProperty("Magnitude").GetUInt64(),f.GetProperty("Exponent").GetInt32(),f.GetProperty("IsNegative").GetBoolean()),root.GetProperty("SecondsBits").GetUInt32(),supplied);
}).ToArray();
var plan=CreatureNominalSpeedPlan.Build(current,steps,precision,new(128,81920,8388608));
var results=plan.Steps.Select(s=>s.Transition).ToArray();
var targetReceipts=plan.Steps.Select(s=>s.Target).ToArray();
var targets=targetReceipts.Select(target=>{return new{target.TargetBits,target.IntermediateBits,target.SelectedSpeedBits,RunSelected=target.Selection==CreatureNominalTargetSelection.Run,
 Magnitude=target.RetainedMagnitude,Intermediate=target.RetainedIntermediate,target.RetainedTarget,target.RetainedSquaredMagnitude,target.ReservedWork};}).ToArray();
var counters=new{plan.StepCount,plan.TargetEvaluations,plan.TransitionEvaluations,plan.RatesUsed,plan.Equalities,plan.Clamps,plan.TargetF32Stores,plan.TransitionF32Stores,
 plan.ReservedCopyBytes,plan.ReservedWork,plan.ActualTargetWork,plan.ActualTransitionWork,plan.ActualWork,ActualReferences=plan.Steps.Select((s,i)=>ReferenceEquals(s.Transition,results[i])&&ReferenceEquals(s.Target,targetReceipts[i])).All(x=>x),OriginalEffectiveMovementParity=false};
File.WriteAllText(args[1],JsonSerializer.Serialize(new{RunReader=run,WalkReader=walk,Plan=counters,Targets=targets,Steps=results,OriginalEffectiveMovementParity=false})+"\n",new UTF8Encoding(false));
Console.WriteLine("PASS: supplied target/transition trace");

