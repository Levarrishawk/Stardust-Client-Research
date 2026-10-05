using System.Numerics;
using System.Text.Json;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;
public static class CreatureNominalSpeedPlanScenarios
{
    private static void Check(bool v) { if(!v)throw new InvalidOperationException("Atomic nominal plan invariant."); }
    private static int I(JsonElement c,string k,int d)=>c.TryGetProperty(k,out var v)?v.GetInt32():d;
    private static long L(JsonElement c,string k,long d)=>c.TryGetProperty(k,out var v)?v.GetInt64():d;
    private static CreatureNominalSpeedPlanStep Step(JsonElement s)
    {
        var a=s.GetProperty("Direction").EnumerateArray().Select(v=>v.GetUInt32()).ToArray();if(a.Length!=3)throw new InvalidDataException("Direction shape.");
        var f=s.GetProperty("Factor");CreatureNominalRate? rate=null;
        if(s.TryGetProperty("Rate",out var r)&&r.ValueKind!=JsonValueKind.Null)rate=new(r.GetProperty("Magnitude").GetUInt64(),r.GetProperty("Exponent").GetInt32(),r.GetProperty("Negative").GetBoolean());
        return new(s.GetProperty("Run").GetUInt32(),s.GetProperty("Walk").GetUInt32(),checked((byte)s.GetProperty("Flag").GetInt32()),new(a[0],a[1],a[2]),
            new(f.GetProperty("Magnitude").GetUInt64(),f.GetProperty("Exponent").GetInt32(),f.GetProperty("Negative").GetBoolean()),s.GetProperty("Seconds").GetUInt32(),rate);
    }
    private static string F(CreatureNominalRetainedValue v)
    {
        if(v.Magnitude==0)return v.IsNegative?"-0/1":"0/1";BigInteger n=v.Magnitude,d=1;if(v.IsNegative)n=-n;if(v.Exponent>=0)n<<=v.Exponent;else d<<=-v.Exponent;var g=BigInteger.GreatestCommonDivisor(BigInteger.Abs(n),d);return $"{n/g}/{d/g}";
    }
    private static object Target(CreatureNominalTargetSpeedResult r)=>new {Selection=r.Selection.ToString(),Selected=r.SelectedSpeedBits,Sum=F(r.RetainedSquaredMagnitude),Magnitude=F(r.RetainedMagnitude),Intermediate=F(r.RetainedIntermediate),r.IntermediateBits,RetainedTarget=F(r.RetainedTarget),r.TargetBits,Work=r.ReservedWork,Counts=new[]{r.SuppliedGetterWords,r.DirectionWords,r.SelectedSpeedUses,r.FactorValuesUsed,r.ArithmeticOperations,r.F32Stores},Provenance=r.Provenance.ToString()};
    private static object Transition(CreatureNominalSpeedResult r)=>new {r.StoredBits,r.CandidateBits,Retained=F(r.RetainedCandidate),Step=r.RetainedStep.HasValue?F(r.RetainedStep.Value):null,Branch=r.Branch.ToString(),r.Clamped,Work=r.ReservedWork,r.RatesUsed,Provenance=r.RateProvenance.ToString()};
    public static object Run(string path)
    {
        var good=new CreatureNominalSpeedPlanStep(0x40000000,0x3f800000,1,new(0x3f800000,0,0),new(1,0),0x3f000000,new(1,0));
        var saved=CreatureNominalSpeedPlan.Build(0,new[]{good},CreatureNominalSpeedPrecision.NearestEven64);uint savedBits=saved.FinalCurrentBits;var savedRow=saved.Steps[0];
        using var doc=JsonDocument.Parse(File.ReadAllText(path));var output=new List<object>();int ownership=0,readOnly=0,receipts=0;
        foreach(var c in doc.RootElement.EnumerateArray())
        {
            string name=c.GetProperty("Name").GetString()!;
            try
            {
                var source=c.GetProperty("Steps").EnumerateArray().Select(Step).ToArray();var original=source.ToArray();
                var options=new CreatureNominalSpeedPlanLimits(I(c,"MaximumSteps",4096),L(c,"MaximumCopyBytes",4194304),L(c,"MaximumWork",268435456));
                var p=(CreatureNominalSpeedPrecision)c.GetProperty("Precision").GetInt32();uint initial=c.GetProperty("Initial").GetUInt32();
                var plan=CreatureNominalSpeedPlan.Build(initial,source,p,options,new CancellationToken(c.TryGetProperty("Cancel",out var cancel)&&cancel.GetBoolean()));
                var reservation=CreatureNominalSpeedPlan.GetRequiredReservations(initial,source,p,options);Check(plan.ReservedCopyBytes==reservation.CopyBytes&&plan.ReservedWork==reservation.Work);
                if(source.Length>0)source[0]=default;
                var rows=new List<object>();uint current=initial;
                foreach(var r in plan.Steps)
                {
                    Check(r.Source==original[r.Index]&&r.CurrentBeforeBits==current);current=r.CurrentAfterBits;
                    var t=CreatureNominalTargetSpeed.Evaluate(r.Source.RunBits,r.Source.WalkBits,r.Source.RunFlag,r.Source.Direction,r.Source.Factor,p);
                    var tr=CreatureNominalSpeedTransition.Evaluate(r.CurrentBeforeBits,t.TargetBits,r.Source.SecondsBits,p,r.Source.SuppliedRate);
                    Check(t.TargetBits==r.Target.TargetBits&&F(t.RetainedIntermediate)==F(r.Target.RetainedIntermediate)&&tr.StoredBits==r.Transition.StoredBits&&F(tr.RetainedCandidate)==F(r.Transition.RetainedCandidate));receipts++;
                    rows.Add(new {r.Index,r.CurrentBeforeBits,SourceMatches=true,Target=Target(r.Target),Transition=Transition(r.Transition)});
                }
                Check(current==plan.FinalCurrentBits);ownership++;
                if(plan.Steps is IList<CreatureNominalSpeedPlanReceipt> list)
                {
                    bool refused=false;try { list.Add(savedRow); }catch(NotSupportedException){refused=true;}Check(refused);readOnly++;
                }
                Check(typeof(CreatureNominalSpeedPlan).GetProperties().All(v=>v.SetMethod is null));
                output.Add(new {Name=name,Reject="None",Initial=plan.InitialCurrentBits,Final=plan.FinalCurrentBits,Rows=rows,Copy=plan.ReservedCopyBytes,Reserved=plan.ReservedWork,ActualTarget=plan.ActualTargetWork,ActualTransition=plan.ActualTransitionWork,Counts=new[]{plan.StepCount,plan.TargetEvaluations,plan.TransitionEvaluations,plan.RatesUsed,plan.Equalities,plan.Clamps,plan.TargetF32Stores,plan.TransitionF32Stores}});
            }
            catch(OperationCanceledException){output.Add(new {Name=name,Reject="Canceled"});}
            catch(Exception e) when(e is InvalidDataException or ArgumentOutOfRangeException or OverflowException){output.Add(new {Name=name,Reject="Input"});}
            Check(saved.FinalCurrentBits==savedBits&&ReferenceEquals(saved.Steps[0],savedRow)&&savedRow.Source==good);
        }
        for(int i=0;i<100;i++)Check(CreatureNominalSpeedPlan.Build(0,new[]{good},CreatureNominalSpeedPrecision.NearestEven64).FinalCurrentBits==savedBits);
        Parallel.For(0,64,_=>Check(CreatureNominalSpeedPlan.Build(0,new[]{good},CreatureNominalSpeedPrecision.NearestEven64).FinalCurrentBits==savedBits));
        return new {Cases=output,OwnershipChecks=ownership,ReadOnlyChecks=readOnly,ActualReceiptChecks=receipts,Repeated=100,Threaded=64,PriorResultPreserved=true};
    }
}
