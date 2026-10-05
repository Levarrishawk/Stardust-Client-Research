using System.Numerics;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
namespace Stardust.Core.SmokeTests;
public static class TerrainSingleSamplerHeightPatchScenarios
{
    private static void Check(bool value){if(!value)throw new InvalidOperationException("Single-sampler invariant failed.");}
    private static int I(JsonElement c,string key,int fallback)=>c.TryGetProperty(key,out var v)?v.GetInt32():fallback;
    private static long L(JsonElement c,string key,long fallback)=>c.TryGetProperty(key,out var v)?v.GetInt64():fallback;
    private static uint U(JsonElement c,string key)=>c.GetProperty(key).GetUInt32();
    private static TerrainFamilyHeightFractalControls Controls(JsonElement c)=>new(I(c,"Table",24),I(c,"Construction",24),I(c,"Query",24),(TerrainHeightArithmeticPrecision)I(c,"Consumer",24));
    private static TerrainFamilyHeightPatchRequest Request(JsonElement c)=>new(I(c,"Width",3),I(c,"Rows",2),U(c,"OriginX"),U(c,"OriginZ"),U(c,"SpacingX"),U(c,"SpacingZ"),U(c,"Height"),U(c,"Weight"));
    private static TerrainFamilyHeightPatch Build(TerrainFractalFamilySet set,byte[] rule,JsonElement c)=>TerrainFamilyHeightPatch.BuildSingleSampler(set,rule,Request(c),Controls(c),
        new(I(c,"MaximumSamples",256),I(c,"MaximumRuleBytes",65536),L(c,"MaximumCopyBytes",1048576),L(c,"MaximumRouteBytes",134217728),L(c,"MaximumWork",16777216)),
        new CancellationToken(c.TryGetProperty("Cancel",out var cancel)&&cancel.GetBoolean()));
    private static string Fraction(TerrainSuppliedFractalSample sample)
    {
        BigInteger n=sample.Magnitude,d=BigInteger.One;if(sample.IsNegative)n=-n;
        if(sample.Exponent>=0)n<<=sample.Exponent;else d<<=-sample.Exponent;
        var gcd=BigInteger.GreatestCommonDivisor(BigInteger.Abs(n),d);return $"{n/gcd}/{d/gcd}";
    }
    private static int[] Counts(TerrainFamilyHeightPatch p)
    {var c=p.SamplingCounts;return [c.RuleDecodes,c.FamilyLookups,c.SamplerConstructions,c.SamplerQueryBatches,c.ComputedCells,c.HeightApplications,c.SamplerDisposals,c.KernelOctaveQueries];}
    public static object Run(string defaultFixture,string singleFixture)
    {
        object frozen=TerrainFamilyHeightPatchScenarios.Run(defaultFixture);
        using var document=JsonDocument.Parse(File.ReadAllText(singleFixture,Encoding.UTF8));var all=document.RootElement;var first=all[0];
        byte[] group=Convert.FromHexString(first.GetProperty("GroupHex").GetString()!),rule=Convert.FromHexString(first.GetProperty("RuleHex").GetString()!);
        var families=TerrainFractalFamilySet.Decode(group);var prior=Build(families,rule,first);var saved=prior.CopyHeightBits();int checks=0;var output=new List<object>();
        foreach(var c in all.EnumerateArray())
        {
            string name=c.GetProperty("Name").GetString()!;
            try
            {
                var set=TerrainFractalFamilySet.Decode(Convert.FromHexString(c.GetProperty("GroupHex").GetString()!));var bytes=Convert.FromHexString(c.GetProperty("RuleHex").GetString()!);
                var patch=Build(set,bytes,c);int n=patch.Width*patch.Rows;
                Check(patch.SamplingPolicy==TerrainFamilyHeightPatchSamplingPolicy.SingleFreshSamplerUniqueCells&&Counts(patch).SequenceEqual(new[]{1,1,1,1,n,n,1,n*patch.Samples[0].Raw.Fields.SignedOctaveCount}));checks++;
                Check(patch.Samples.All(s=>ReferenceEquals(s.Rule,patch.Samples[0].Rule)&&ReferenceEquals(s.Family,patch.Samples[0].Family)&&ReferenceEquals(s.Raw,patch.Samples[0].Raw)));checks++;
                Check(patch.AggregateRouteBytes==patch.SharedPreparationBytes+patch.Samples.Sum(s=>s.AccountedBytes));checks++;
                var samples=patch.Samples.Select((s,i)=>
                {
                    Check(s.Origin==TerrainConditionalFractalOrigin.ConditionalComputedMiss&&ReferenceEquals(s.Sample,s.Affector.Sample));
                    Check(s.Request.Width==patch.Width&&s.Request.Rows==patch.Rows&&s.Request.Column==i%patch.Width&&s.Request.Row==i/patch.Width);
                    return new{FamilyId=s.Family.Id,Origin=s.Origin.ToString(),Retained=Fraction(s.Sample),Projection=s.Sample.ProjectionBits,Height=s.Affector.Consumer.ResultHeightBits,X=s.Request.XBits,Z=s.Request.ZBits,CacheWidth=s.Request.Width,CacheRows=s.Request.Rows,s.Request.Column,s.Request.Row,AccountingPolicy=s.AccountingPolicy.ToString(),s.AccountedBytes,s.AccountedWork};
                }).ToArray();
                var b=patch.Bounds;output.Add(new{Name=name,Reject="None",patch.Width,patch.Rows,SourceSha256=patch.FamilySetSha256,patch.RuleSha256,X=patch.CopyXAxisBits(),Z=patch.CopyZAxisBits(),Heights=patch.CopyHeightBits(),Samples=samples,Bounds=new[]{b.MinimumXBits,b.MaximumXBits,b.MinimumZBits,b.MaximumZBits,b.MinimumHeightBits,b.MaximumHeightBits},patch.LogicalCopyBytes,patch.AggregateRouteBytes,patch.AccountedWork,patch.SharedPreparationBytes,patch.SharedPreparationWork,SamplingPolicy=patch.SamplingPolicy.ToString(),Counts=Counts(patch)});
            }
            catch(Exception ex)when(ex is ArgumentException or InvalidDataException or OperationCanceledException)
            {output.Add(new{Name=name,Reject="Input"});Check(prior.CopyHeightBits().SequenceEqual(saved));checks++;}
        }
        var defaultPatch=TerrainFamilyHeightPatch.Build(families,rule,Request(first),Controls(first));int count=defaultPatch.Samples.Count;
        Check(defaultPatch.SamplingPolicy==TerrainFamilyHeightPatchSamplingPolicy.FreshSamplerPerSample&&defaultPatch.SharedPreparationBytes==0&&defaultPatch.SharedPreparationWork==0);checks++;
        Check(Counts(defaultPatch).SequenceEqual(new[]{count,count,count,count,count,count,count,count*defaultPatch.Samples[0].Raw.Fields.SignedOctaveCount}));checks++;
        Check(defaultPatch.Samples.Select(s=>s.Rule).Distinct().Count()==count&&defaultPatch.Samples.All(s=>s.AccountingPolicy==TerrainFamilyHeightFractalAccountingPolicy.IndependentRoute&&s.Request.Width==1&&s.Request.Rows==1&&s.Request.Column==0&&s.Request.Row==0));checks++;
        Check(defaultPatch.CopyHeightBits().SequenceEqual(saved)&&defaultPatch.Samples.Select(s=>Fraction(s.Sample)).SequenceEqual(prior.Samples.Select(s=>Fraction(s.Sample))));checks++;
        Check(defaultPatch.AccountedWork>prior.AccountedWork&&defaultPatch.AggregateRouteBytes+defaultPatch.LogicalCopyBytes>prior.AggregateRouteBytes+prior.LogicalCopyBytes);checks++;
        var originalRule=(byte[])rule.Clone();Array.Fill(rule,(byte)0);Array.Fill(group,(byte)0);Check(prior.CopyEncodedRule().SequenceEqual(originalRule));checks++;
        var export=prior.CopyHeightBits();Array.Fill(export,uint.MaxValue);Check(prior.CopyHeightBits().SequenceEqual(saved));checks++;
        var encoded=prior.CopyEncodedRule();Array.Fill(encoded,(byte)0);Check(prior.CopyEncodedRule().SequenceEqual(originalRule));checks++;
        try{((IList<uint>)prior.HeightBits)[0]=0;throw new InvalidOperationException("Mutable heights.");}catch(NotSupportedException){checks++;}
        try{((IList<TerrainFamilyHeightFractalReceipt>)prior.Samples)[0]=prior.Samples[0];throw new InvalidOperationException("Mutable samples.");}catch(NotSupportedException){checks++;}
        var repeated=Build(families,originalRule,first);Check(repeated.CopyHeightBits().SequenceEqual(saved)&&!ReferenceEquals(prior.Samples[0].Sample,repeated.Samples[0].Sample)&&!ReferenceEquals(prior.Samples[0].Rule,repeated.Samples[0].Rule));checks++;
        Check(prior.Samples.Select(s=>s.Sample).Distinct().Count()==prior.Samples.Count&&prior.Samples.All(s=>s.Origin==TerrainConditionalFractalOrigin.ConditionalComputedMiss));checks++;
        Exception? threadError=null;TerrainFamilyHeightPatch? threaded=null;var thread=new Thread(()=>{try{threaded=Build(families,originalRule,first);}catch(Exception ex){threadError=ex;}});thread.Start();thread.Join();Check(threadError is null&&threaded is not null&&threaded.CopyHeightBits().SequenceEqual(saved));checks++;
        Check(prior.CopyHeightBits().SequenceEqual(saved));checks++;
        return new{Default=frozen,Cases=output,OwnershipChecks=checks};
    }
}
