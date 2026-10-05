using System.Numerics;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
namespace Stardust.Core.SmokeTests;
public static class TerrainFamilyHeightFieldScenarios
{
    private static void Check(bool value){if(!value)throw new InvalidOperationException("Field ownership/skip/identity invariant failed.");}
    private static int I(JsonElement c,string key,int fallback)=>c.TryGetProperty(key,out var v)?v.GetInt32():fallback;
    private static long L(JsonElement c,string key,long fallback)=>c.TryGetProperty(key,out var v)?v.GetInt64():fallback;
    private static uint U(JsonElement c,string key)=>c.GetProperty(key).GetUInt32();
    private static uint[] A(JsonElement c,string key)=>c.GetProperty(key).EnumerateArray().Select(x=>x.GetUInt32()).ToArray();
    private static TerrainFamilyHeightFractalControls Controls(JsonElement c)=>new(I(c,"Table",24),I(c,"Construction",24),I(c,"Query",24),(TerrainHeightArithmeticPrecision)I(c,"Consumer",24));
    private static TerrainFamilyHeightFieldRequest Request(JsonElement c)=>new(I(c,"Width",3),I(c,"Rows",2),U(c,"OriginX"),U(c,"OriginZ"),U(c,"SpacingX"),U(c,"SpacingZ"));
    private static TerrainFamilyHeightField Build(TerrainFractalFamilySet set,byte[] rule,JsonElement c,uint[] prior,uint[] influence)=>TerrainFamilyHeightField.Build(set,rule,Request(c),prior,influence,Controls(c),
        new(I(c,"MaximumSamples",256),I(c,"MaximumRuleBytes",65536),L(c,"MaximumCopyBytes",1048576),L(c,"MaximumRouteBytes",134217728),L(c,"MaximumWork",16777216),I(c,"MaximumQueryBytes",2097152),I(c,"MaximumQueryWork",16777216)),new CancellationToken(c.TryGetProperty("Cancel",out var token)&&token.GetBoolean()));
    private static string Fraction(TerrainSuppliedFractalSample sample)
    {BigInteger n=sample.Magnitude,d=BigInteger.One;if(sample.IsNegative)n=-n;if(sample.Exponent>=0)n<<=sample.Exponent;else d<<=-sample.Exponent;var gcd=BigInteger.GreatestCommonDivisor(BigInteger.Abs(n),d);return $"{n/gcd}/{d/gcd}";}
    private static int[] Counts(TerrainFamilyHeightField f){var c=f.SamplingCounts;return [c.RuleDecodes,c.FamilyLookups,c.SamplerConstructions,c.SamplerQueryBatches,c.ComputedCells,c.HeightApplications,c.SamplerDisposals,c.KernelOctaveQueries];}
    public static object Run(string fixture)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(fixture,Encoding.UTF8));var all=document.RootElement;var first=all[0];var group=Convert.FromHexString(first.GetProperty("GroupHex").GetString()!);var rule=Convert.FromHexString(first.GetProperty("RuleHex").GetString()!);var set=TerrainFractalFamilySet.Decode(group);
        var p=A(first,"Prior");var w=A(first,"Influence");var prior=Build(set,rule,first,p,w);var saved=prior.CopyHeightBits();var outputs=new List<object>();int checks=0,uniform=0;
        foreach(var c in all.EnumerateArray())
        {
            string name=c.GetProperty("Name").GetString()!;
            try
            {
                var families=TerrainFractalFamilySet.Decode(Convert.FromHexString(c.GetProperty("GroupHex").GetString()!));var bytes=Convert.FromHexString(c.GetProperty("RuleHex").GetString()!);var input=A(c,"Prior");var weights=A(c,"Influence");if(c.TryGetProperty("Alias",out var alias)&&alias.GetBoolean())weights=input;
                var field=Build(families,bytes,c,input,weights);int selected=field.Cells.Count(x=>x.Status==TerrainFamilyHeightFieldCellStatus.Applied);
                Check(field.SamplingCounts.ComputedCells==selected&&field.SamplingCounts.SamplerConstructions==(selected==0?0:1)&&field.SamplingCounts.SamplerDisposals==(selected==0?0:1));checks++;
                var cells=field.Cells.Select(cell=>
                {
                    object? applied=null;
                    if(cell.Applied is {} a)
                    {
                        Check(cell.Status==TerrainFamilyHeightFieldCellStatus.Applied&&a.Origin==TerrainConditionalFractalOrigin.ConditionalComputedMiss&&ReferenceEquals(a.Sample,a.Affector.Sample));
                        Check(ReferenceEquals(a.Rule,field.Rule)&&ReferenceEquals(a.Family,field.Family)&&ReferenceEquals(a.Raw,field.Raw));
                        Check(a.Request.Width==field.Request.Width&&a.Request.Rows==field.Request.Rows&&a.Request.Column==cell.Column&&a.Request.Row==cell.Row&&a.Request.InitialHeightBits==cell.PriorHeightBits&&a.Request.InfluenceBits==cell.InfluenceBits);
                        applied=new{Origin=a.Origin.ToString(),Retained=Fraction(a.Sample),Projection=a.Sample.ProjectionBits,Height=a.Affector.Consumer.ResultHeightBits,FamilyId=a.Family.Id,CacheWidth=a.Request.Width,CacheRows=a.Request.Rows,a.Request.Column,a.Request.Row,AccountingPolicy=a.AccountingPolicy.ToString(),a.AccountedBytes,a.AccountedWork};
                    }
                    else Check(cell.Status==TerrainFamilyHeightFieldCellStatus.SkippedNonPositiveInfluence&&cell.HeightBits==cell.PriorHeightBits);
                    return new{cell.Index,cell.Column,cell.Row,X=cell.XBits,Z=cell.ZBits,Prior=cell.PriorHeightBits,Influence=cell.InfluenceBits,Height=cell.HeightBits,Status=cell.Status.ToString(),Applied=applied};
                }).ToArray();
                if(c.TryGetProperty("Uniform",out var same)&&same.GetBoolean())
                {
                    var request=new TerrainFamilyHeightPatchRequest(field.Request.Width,field.Request.Rows,field.Request.OriginXBits,field.Request.OriginZBits,field.Request.SpacingXBits,field.Request.SpacingZBits,input[0],weights[0]);var limits=new TerrainFamilyHeightPatchLimits(MaximumSamples:1024,MaximumWork:67108864);
                    var batched=TerrainFamilyHeightPatch.BuildSingleSampler(families,bytes,request,Controls(c),limits);var reference=TerrainFamilyHeightPatch.Build(families,bytes,request,Controls(c),limits);
                    Check(field.CopyHeightBits().SequenceEqual(batched.CopyHeightBits())&&field.CopyHeightBits().SequenceEqual(reference.CopyHeightBits()));
                    Check(field.Cells.Select(x=>Fraction(x.Applied!.Sample)).SequenceEqual(batched.Samples.Select(x=>Fraction(x.Sample))));uniform++;
                }
                var b=field.Bounds;outputs.Add(new{Name=name,Reject="None",SourceSha256=field.FamilySetSha256,field.RuleSha256,X=field.CopyXAxisBits(),Z=field.CopyZAxisBits(),Prior=field.CopyPriorHeightBits(),Influence=field.CopyInfluenceBits(),Heights=field.CopyHeightBits(),Cells=cells,Bounds=new[]{b.MinimumXBits,b.MaximumXBits,b.MinimumZBits,b.MaximumZBits,b.MinimumHeightBits,b.MaximumHeightBits},Counts=Counts(field),field.LogicalCopyBytes,field.ReservedRouteBytes,field.ReservedWork,field.ReservedQueryBytes,field.ReservedQueryWork,field.SharedPreparationBytes,field.SharedPreparationWork,field.AccountedRouteBytes,field.AccountedWork});
            }
            catch(Exception ex)when(ex is ArgumentException or InvalidDataException or OperationCanceledException){outputs.Add(new{Name=name,Reject="Input"});Check(prior.CopyHeightBits().SequenceEqual(saved));checks++;}
        }
        var originalRule=(byte[])rule.Clone();Array.Fill(rule,(byte)0);Array.Fill(group,(byte)0);Array.Fill(p,uint.MaxValue);Array.Fill(w,uint.MaxValue);Check(prior.CopyHeightBits().SequenceEqual(saved)&&prior.CopyEncodedRule().SequenceEqual(originalRule));checks++;
        var exported=prior.CopyPriorHeightBits();Array.Fill(exported,uint.MaxValue);Check(prior.PriorHeightBits.All(x=>x!=uint.MaxValue));checks++;
        exported=prior.CopyInfluenceBits();Array.Fill(exported,uint.MaxValue);Check(prior.InfluenceBits.All(x=>x!=uint.MaxValue));checks++;
        exported=prior.CopyHeightBits();Array.Fill(exported,uint.MaxValue);Check(prior.CopyHeightBits().SequenceEqual(saved));checks++;
        try{((IList<uint>)prior.PriorHeightBits)[0]=0;throw new InvalidOperationException("Mutable prior view.");}catch(NotSupportedException){checks++;}
        try{((IList<uint>)prior.InfluenceBits)[0]=0;throw new InvalidOperationException("Mutable influence view.");}catch(NotSupportedException){checks++;}
        try{((IList<TerrainFamilyHeightFieldCell>)prior.Cells)[0]=prior.Cells[0];throw new InvalidOperationException("Mutable cells.");}catch(NotSupportedException){checks++;}
        var repeated=Build(set,originalRule,first,A(first,"Prior"),A(first,"Influence"));Check(repeated.CopyHeightBits().SequenceEqual(saved)&&!ReferenceEquals(prior.Cells[0].Applied!.Sample,repeated.Cells[0].Applied!.Sample));checks++;
        Exception? error=null;TerrainFamilyHeightField? threaded=null;var thread=new Thread(()=>{try{threaded=Build(set,originalRule,first,A(first,"Prior"),A(first,"Influence"));}catch(Exception ex){error=ex;}});thread.Start();thread.Join();Check(error is null&&threaded is not null&&threaded.CopyHeightBits().SequenceEqual(saved));checks++;
        return new{Cases=outputs,OwnershipChecks=checks,UniformComparisons=uniform};
    }
}
