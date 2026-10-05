using System.Numerics;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
namespace Stardust.Core.SmokeTests;
public static class TerrainFamilyFilteredHeightFieldScenarios
{
    private static void Check(bool v){if(!v)throw new InvalidOperationException("Filtered-field ownership/composition invariant.");}
    private static int I(JsonElement c,string k,int d)=>c.TryGetProperty(k,out var v)?v.GetInt32():d;
    private static long L(JsonElement c,string k,long d)=>c.TryGetProperty(k,out var v)?v.GetInt64():d;
    private static uint[] A(JsonElement c,string k)=>c.GetProperty(k).EnumerateArray().Select(v=>v.GetUInt32()).ToArray();
    private static uint U(JsonElement c,string k)=>c.GetProperty(k).GetUInt32();
    private static bool Flag(JsonElement c,string k)=>c.TryGetProperty(k,out var v)&&v.GetBoolean();
    private static TerrainFamilyHeightFieldRequest Request(JsonElement c)=>new(I(c,"Width",3),I(c,"Rows",2),U(c,"OriginX"),U(c,"OriginZ"),U(c,"SpacingX"),U(c,"SpacingZ"));
    private static TerrainFamilyFilteredHeightFieldControls Controls(JsonElement c)=>new(new(I(c,"Table",53),I(c,"Construction",53),I(c,"Query",64),(TerrainHeightArithmeticPrecision)I(c,"Consumer",64)),(TerrainHeightArithmeticPrecision)I(c,"FilterQuery",64),(TerrainHeightArithmeticPrecision)I(c,"Combine",64),(TerrainHeightArithmeticPrecision)I(c,"Product",64));
    private static TerrainFamilyFilteredHeightFieldLimits Limits(JsonElement c)=>new(I(c,"MaximumSamples",256),I(c,"MaximumRuleBytes",65536),L(c,"MaximumCopyBytes",4194304),L(c,"MaximumRouteBytes",134217728),L(c,"MaximumWork",67108864),I(c,"MaximumFilterQueryBytes",2097152),I(c,"MaximumFilterQueryWork",16777216),new(MaximumSamples:I(c,"HeightMaximumSamples",256),MaximumCopyBytes:L(c,"HeightMaximumCopyBytes",1048576),MaximumRouteBytes:L(c,"HeightMaximumRouteBytes",134217728),MaximumWork:L(c,"HeightMaximumWork",16777216),MaximumQueryBytes:I(c,"HeightMaximumQueryBytes",2097152),MaximumQueryWork:I(c,"HeightMaximumQueryWork",16777216)));
    private static TerrainFamilyFilteredHeightField Build(JsonElement c,TerrainFractalFamilySet families,TerrainGeneratorFractalFilterRule selected,byte[] rule,uint[] prior,uint[] incoming)=>TerrainFamilyFilteredHeightField.Build(families,selected,rule,Request(c),prior,incoming,Controls(c),Limits(c),new CancellationToken(Flag(c,"Cancel")));
    private static string Fraction(TerrainSuppliedFractalSample s)
    {
        if(s.Magnitude==0)return s.NegativeZero?"-0/1":"0/1";BigInteger n=s.Magnitude,d=1;if(s.IsNegative)n=-n;if(s.Exponent>=0)n<<=s.Exponent;else d<<=-s.Exponent;var gcd=BigInteger.GreatestCommonDivisor(BigInteger.Abs(n),d);return $"{n/gcd}/{d/gcd}";
    }
    private static int[] Counts(TerrainFamilyFilteredHeightField f)
    {
        var c=f.SamplingCounts;var h=c.Height;return [c.HeightPrevalidations,c.FilterSamplerConstructions,c.FilterQueryBatches,c.FilterComputedCells,c.FilterQueries,c.Products,c.FilterSamplerDisposals,c.FilterKernelOctaveQueries,h.RuleDecodes,h.FamilyLookups,h.SamplerConstructions,h.SamplerQueryBatches,h.ComputedCells,h.HeightApplications,h.SamplerDisposals,h.KernelOctaveQueries];
    }
    public static object Run(string path)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(path,Encoding.UTF8));var first=doc.RootElement[0];
        var source=Convert.FromHexString(first.GetProperty("GeneratorHex").GetString()!);var g=TerrainGeneratorInput.Decode(source);var binding=TerrainGeneratorFractalFamilies.Bind(g);
        var selected=TerrainGeneratorFractalFilterRule.Select(g,binding,0,TerrainGeneratorFractalFilterRulePolicy.EnabledOperationalSubset);
        var rule=Convert.FromHexString(first.GetProperty("RuleHex").GetString()!);var prior=A(first,"Prior");var incoming=A(first,"Incoming");var saved=Build(first,binding.Families,selected,rule,prior,incoming);var old=saved.HeightField.CopyHeightBits();
        int checks=0,comparisons=0,uniform=0;var output=new List<object>();
        foreach(var c in doc.RootElement.EnumerateArray())
        {
            string name=c.GetProperty("Name").GetString()!;
            try
            {
                var gen=TerrainGeneratorInput.Decode(Convert.FromHexString(c.GetProperty("GeneratorHex").GetString()!));var bind=TerrainGeneratorFractalFamilies.Bind(gen);
                var filter=TerrainGeneratorFractalFilterRule.Select(gen,bind,I(c,"Index",0),(TerrainGeneratorFractalFilterRulePolicy)I(c,"Policy",1));
                var fam=Flag(c,"ForeignFamilySet")?TerrainFractalFamilySet.Decode(Convert.FromHexString(c.GetProperty("GroupHex").GetString()!)):bind.Families;
                var bytes=Convert.FromHexString(c.GetProperty("RuleHex").GetString()!);var ph=A(c,"Prior");var iw=A(c,"Incoming");if(Flag(c,"Alias"))iw=ph;
                var field=Build(c,fam,filter,bytes,ph,iw);var h=field.HeightField;Check(ReferenceEquals(field.Filter,filter));
                Check(field.SamplingCounts.FilterSamplerConstructions==1&&field.SamplingCounts.FilterSamplerDisposals==1&&field.SamplingCounts.FilterComputedCells==ph.Length);
                foreach(var cell in field.Cells)
                {
                    Check(cell.Computed.Origin==TerrainConditionalFractalOrigin.ConditionalComputedMiss&&ReferenceEquals(cell.Computed.Sample,cell.Query.RetainedSample)&&ReferenceEquals(cell.Query.Input,filter.Input));
                    Check(ReferenceEquals(cell.Carrier.FractalFilter,cell.Query)&&ReferenceEquals(cell.Carrier.RetainedFractalSample,cell.Computed.Sample));
                    Check(ReferenceEquals(cell.Influence.Sample.Filters[0].Query,cell.Carrier)&&cell.Influence.Sample.Boundaries.Count==0&&cell.Influence.BoundaryFallback&&!cell.Influence.Sample.InvertBoundary&&!cell.Influence.Sample.InvertFilter);
                    Check(ReferenceEquals(cell.Height,h.Cells[cell.Index])&&cell.Influence.ProductBits==h.InfluenceBits[cell.Index]&&cell.IncomingWeightBits==cell.Influence.Sample.IncomingWeightBits);
                }
                // Frozen54 explicit composition: independent fresh full-shape filter sampler and the unchanged field boundary.
                var ctl=Controls(c);var rq=Enumerable.Range(0,ph.Length).Select(i=>new TerrainConditionalFractalSampler.Request(i%h.Request.Width,i/h.Request.Width,h.XAxisBits[i%h.Request.Width],h.ZAxisBits[i/h.Request.Width])).ToArray();var referenceProducts=new uint[ph.Length];
                using(var sampler=TerrainConditionalFractalSampler.CreateForSupportedModes(filter.Family.Raw,h.Request.Width,h.Request.Rows,ctl.Sampling.TablePrecision,ctl.Sampling.ConstructionPrecision,1048576,1048576))
                {
                    var computed=sampler.Query(rq,ctl.Sampling.QueryPrecision,2097152,16777216);
                    for(int i=0;i<ph.Length;i++)
                    {
                        var q=TerrainFilterFractalInfluence.Query(filter.Input,computed[i].Sample,ctl.FilterQueryPrecision);var carrier=TerrainLayerInfluenceQuery.FromRetainedFractalFilter(q);
                        referenceProducts[i]=TerrainLayerInfluence.Evaluate(TerrainLayerInfluenceSample.Create(iw[i],[],[new(true,filter.Fields.FeatheringTypeBits,carrier)],1,1,false,false,ctl.CombinePrecision,ctl.ProductPrecision)).ProductBits;
                        Check(Fraction(computed[i].Sample)==Fraction(field.Cells[i].Computed.Sample));
                    }
                    sampler.Dispose();bool rejected=false;try{sampler.Query(rq,ctl.Sampling.QueryPrecision,2097152,16777216);}catch(ObjectDisposedException){rejected=true;}Check(rejected);checks++;
                }
                Check(field.SamplingCounts.HeightPositiveCells+field.SamplingCounts.HeightSkippedCells==ph.Length);Check(referenceProducts.SequenceEqual(field.ProductBits));var reference=TerrainFamilyHeightField.Build(fam,bytes,h.Request,ph,referenceProducts,ctl.Sampling,new(MaximumSamples:I(c,"HeightMaximumSamples",256)));Check(reference.CopyHeightBits().SequenceEqual(h.HeightBits));comparisons++;
                if(Flag(c,"Uniform"))
                {
                    var patch=new TerrainFamilyHeightPatchRequest(h.Request.Width,h.Request.Rows,h.Request.OriginXBits,h.Request.OriginZBits,h.Request.SpacingXBits,h.Request.SpacingZBits,ph[0],referenceProducts[0]);
                    Check(TerrainFamilyHeightPatch.Build(fam,bytes,patch,ctl.Sampling).CopyHeightBits().SequenceEqual(h.HeightBits));uniform++;
                }
                var cells=field.Cells.Select(cell=>new{cell.Index,Retained=Fraction(cell.Computed.Sample),Projection=cell.Computed.Sample.ProjectionBits,Scaled=cell.Query.ScaledSampleBits,Amount=cell.Query.RuntimeAmountBits,Branch=(int)cell.Query.Branch,Query=cell.Query.QueryProjectionBits,Span=cell.Query.FeatherSpanProjectionBits,Local=cell.Influence.LocalInfluenceBits,Product=cell.Influence.ProductBits}).ToArray();
                output.Add(new{Name=name,Reject="None",Prior=field.CopyPriorHeightBits(),Incoming=field.CopyIncomingWeightBits(),Products=field.CopyProductBits(),Heights=h.CopyHeightBits(),Cells=cells,Counts=Counts(field),field.LogicalCopyBytes,field.ReservedRouteBytes,field.ReservedWork,field.ReservedFilterQueryBytes,field.ReservedFilterQueryWork,field.AccountedRouteBytes,field.AccountedWork});
            }
            catch(Exception ex)when(ex is InvalidDataException or ArgumentException or OperationCanceledException){output.Add(new{Name=name,Reject="Input"});Check(saved.HeightField.CopyHeightBits().SequenceEqual(old));checks++;}
        }
        Array.Fill(source,(byte)0);Array.Fill(rule,(byte)0);Array.Fill(prior,uint.MaxValue);Array.Fill(incoming,uint.MaxValue);Check(saved.HeightField.CopyHeightBits().SequenceEqual(old));checks++;
        foreach(var copy in new[]{saved.CopyPriorHeightBits(),saved.CopyIncomingWeightBits(),saved.CopyProductBits()})Array.Fill(copy,uint.MaxValue);
        Check(saved.PriorHeightBits.All(v=>v!=uint.MaxValue)&&saved.IncomingWeightBits.All(v=>v!=uint.MaxValue)&&saved.ProductBits.All(v=>v!=uint.MaxValue));checks++;
        foreach(var view in new[]{saved.PriorHeightBits,saved.IncomingWeightBits,saved.ProductBits}){try{((IList<uint>)view)[0]=0;throw new InvalidOperationException("Mutable exported view.");}catch(NotSupportedException){checks++;}}
        try{((IList<TerrainFamilyFilteredHeightFieldCell>)saved.Cells)[0]=saved.Cells[0];throw new InvalidOperationException("Mutable cell view.");}catch(NotSupportedException){checks++;}
        var exportRule=saved.CopyEncodedHeightRule();Array.Fill(exportRule,(byte)0);Check(saved.CopyEncodedHeightRule().Any(v=>v!=0));checks++;
        var rr=Convert.FromHexString(first.GetProperty("RuleHex").GetString()!);var again=Build(first,binding.Families,selected,rr,A(first,"Prior"),A(first,"Incoming"));Check(again.HeightField.CopyHeightBits().SequenceEqual(old)&&!ReferenceEquals(again.Cells[0].Computed.Sample,saved.Cells[0].Computed.Sample));checks++;
        Exception? error=null;TerrainFamilyFilteredHeightField? threaded=null;var thread=new Thread(()=>{try{threaded=Build(first,binding.Families,selected,rr,A(first,"Prior"),A(first,"Incoming"));}catch(Exception ex){error=ex;}});thread.Start();thread.Join();Check(error is null&&threaded is not null&&threaded.HeightField.CopyHeightBits().SequenceEqual(old));checks++;
        return new{Cases=output,OwnershipChecks=checks,CompositionComparisons=comparisons,UniformComparisons=uniform};
    }
}
