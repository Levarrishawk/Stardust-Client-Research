using System.Numerics;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
namespace Stardust.Core.SmokeTests;
public static class TerrainFamilyHeightPatchScenarios
{
    private static void Check(bool value){if(!value)throw new InvalidOperationException("Height patch ownership/atomicity invariant failed.");}
    private static int I(JsonElement c,string key,int fallback)=>c.TryGetProperty(key,out var v)?v.GetInt32():fallback;
    private static long L(JsonElement c,string key,long fallback)=>c.TryGetProperty(key,out var v)?v.GetInt64():fallback;
    private static uint U(JsonElement c,string key)=>c.GetProperty(key).GetUInt32();
    private static TerrainFamilyHeightFractalControls Controls(JsonElement c)=>new(I(c,"Table",24),I(c,"Construction",24),I(c,"Query",24),(TerrainHeightArithmeticPrecision)I(c,"Consumer",24));
    private static TerrainFamilyHeightPatch Build(TerrainFractalFamilySet families,byte[] rule,JsonElement c)=>TerrainFamilyHeightPatch.Build(families,rule,
        new(I(c,"Width",3),I(c,"Rows",2),U(c,"OriginX"),U(c,"OriginZ"),U(c,"SpacingX"),U(c,"SpacingZ"),U(c,"Height"),U(c,"Weight")),Controls(c),
        new(I(c,"MaximumSamples",256),I(c,"MaximumRuleBytes",65536),L(c,"MaximumCopyBytes",1048576),L(c,"MaximumRouteBytes",134217728),L(c,"MaximumWork",16777216)),
        new CancellationToken(c.TryGetProperty("Cancel",out var cancel)&&cancel.GetBoolean()));
    private static string Fraction(TerrainSuppliedFractalSample sample)
    {
        BigInteger n=sample.Magnitude,d=BigInteger.One;if(sample.IsNegative)n=-n;
        if(sample.Exponent>=0)n<<=sample.Exponent;else d<<=-sample.Exponent;
        var gcd=BigInteger.GreatestCommonDivisor(BigInteger.Abs(n),d);return $"{n/gcd}/{d/gcd}";
    }
    public static object Run(string fixture)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(fixture,Encoding.UTF8));var all=document.RootElement;
        var first=all[0];byte[] group=Convert.FromHexString(first.GetProperty("GroupHex").GetString()!),rule=Convert.FromHexString(first.GetProperty("RuleHex").GetString()!);
        var families=TerrainFractalFamilySet.Decode(group);var prior=Build(families,rule,first);uint[] saved=prior.CopyHeightBits();int checks=0;var output=new List<object>();
        foreach(var c in all.EnumerateArray())
        {
            string name=c.GetProperty("Name").GetString()!;
            try
            {
                var set=TerrainFractalFamilySet.Decode(Convert.FromHexString(c.GetProperty("GroupHex").GetString()!));
                var patch=Build(set,Convert.FromHexString(c.GetProperty("RuleHex").GetString()!),c);
                Check(patch.Samples.Count==patch.Width*patch.Rows);
                var samples=patch.Samples.Select((s,i)=>
                {
                    Check(s.Origin==TerrainConditionalFractalOrigin.ConditionalComputedMiss&&ReferenceEquals(s.Sample,s.Affector.Sample)&&ReferenceEquals(s.Raw,s.Family.Raw));
                    Check(s.Request.Width==1&&s.Request.Rows==1&&s.Request.Column==0&&s.Request.Row==0&&s.Request.XBits==patch.XAxisBits[i%patch.Width]&&s.Request.ZBits==patch.ZAxisBits[i/patch.Width]);
                    return new{FamilyId=s.Family.Id,Origin=s.Origin.ToString(),Retained=Fraction(s.Sample),Projection=s.Sample.ProjectionBits,Height=s.Affector.Consumer.ResultHeightBits,X=s.Request.XBits,Z=s.Request.ZBits};
                }).ToArray();
                var bounds=patch.Bounds;
                output.Add(new{Name=name,Reject="None",patch.Width,patch.Rows,SourceSha256=patch.FamilySetSha256,patch.RuleSha256,X=patch.CopyXAxisBits(),Z=patch.CopyZAxisBits(),Heights=patch.CopyHeightBits(),Samples=samples,Bounds=new[]{bounds.MinimumXBits,bounds.MaximumXBits,bounds.MinimumZBits,bounds.MaximumZBits,bounds.MinimumHeightBits,bounds.MaximumHeightBits},patch.LogicalCopyBytes,patch.AggregateRouteBytes,patch.AccountedWork});
            }
            catch(Exception ex)when(ex is ArgumentException or InvalidDataException or OperationCanceledException)
            {
                output.Add(new{Name=name,Reject="Input"});Check(prior.CopyHeightBits().SequenceEqual(saved));checks++;
                if(c.TryGetProperty("LateFailure",out var late)&&late.GetBoolean())
                {
                    var set=TerrainFractalFamilySet.Decode(Convert.FromHexString(c.GetProperty("GroupHex").GetString()!));
                    var firstCell=TerrainFamilyHeightFractal.Evaluate(set,Convert.FromHexString(c.GetProperty("RuleHex").GetString()!),new(1,1,0,0,U(c,"OriginX"),U(c,"OriginZ"),U(c,"Height"),U(c,"Weight")),Controls(c));
                    Check(firstCell.Affector.Consumer.ResultHeightBits==0);checks++;
                }
            }
        }
        var originalRule=(byte[])rule.Clone();Array.Fill(rule,(byte)0);Array.Fill(group,(byte)0);Check(prior.CopyEncodedRule().SequenceEqual(originalRule)&&families.SourceSha256==prior.FamilySetSha256);checks++;
        var h=prior.CopyHeightBits();Array.Fill(h,uint.MaxValue);Check(prior.CopyHeightBits().SequenceEqual(saved));checks++;
        var x=prior.CopyXAxisBits();Array.Fill(x,uint.MaxValue);Check(prior.XAxisBits.All(v=>v!=uint.MaxValue));checks++;
        var z=prior.CopyZAxisBits();Array.Fill(z,uint.MaxValue);Check(prior.ZAxisBits.All(v=>v!=uint.MaxValue));checks++;
        var exportedRule=prior.CopyEncodedRule();Array.Fill(exportedRule,(byte)0);Check(prior.CopyEncodedRule().SequenceEqual(originalRule));checks++;
        try{((IList<uint>)prior.HeightBits)[0]=0;throw new InvalidOperationException("Mutable height view.");}catch(NotSupportedException){checks++;}
        try{((IList<uint>)prior.XAxisBits)[0]=0;throw new InvalidOperationException("Mutable axis view.");}catch(NotSupportedException){checks++;}
        try{((IList<TerrainFamilyHeightFractalReceipt>)prior.Samples)[0]=prior.Samples[0];throw new InvalidOperationException("Mutable receipt view.");}catch(NotSupportedException){checks++;}
        var repeated=Build(families,originalRule,first);Check(repeated.CopyHeightBits().SequenceEqual(saved)&&!ReferenceEquals(repeated.Samples[0].Sample,prior.Samples[0].Sample));checks++;
        Check(prior.Samples.Select(s=>s.Sample).Distinct().Count()==prior.Samples.Count);checks++;
        Exception? threadError=null;TerrainFamilyHeightPatch? threaded=null;
        var thread=new Thread(()=>{try{threaded=Build(families,originalRule,first);}catch(Exception ex){threadError=ex;}});thread.Start();thread.Join();Check(threadError is null&&threaded is not null&&threaded.CopyHeightBits().SequenceEqual(saved));checks++;
        // A 3x3 height result feeds frozen assembly/query via explicitly supplied up normals/color.
        var surfaceCase=all.EnumerateArray().First(c=>I(c,"Width",0)==3&&I(c,"Rows",0)==3);
        var surface=Build(TerrainFractalFamilySet.Decode(Convert.FromHexString(surfaceCase.GetProperty("GroupHex").GetString()!)),Convert.FromHexString(surfaceCase.GetProperty("RuleHex").GetString()!),surfaceCase);
        var positions=surface.HeightBits.Select((bits,i)=>new Vector3(BitConverter.UInt32BitsToSingle(surface.XAxisBits[i%3]),BitConverter.UInt32BitsToSingle(bits),BitConverter.UInt32BitsToSingle(surface.ZAxisBits[i/3]))).ToArray();
        var assembled=TerrainPatchAssembly.Assemble(3,3,positions,Enumerable.Repeat(Vector3.UnitY,9).ToArray(),Enumerable.Repeat(0xff4080c0u,9).ToArray(),new(1,0,0,0,0),1);
        var query=new TerrainPatchSurfaceQuery(assembled.Geometry);
        foreach(int index in new[]{0,2,4,6,8}){var p=positions[index];var hit=query.Query(p.X,p.Z);Check(hit is not null&&Math.Abs(hit.Value.Height-p.Y)<1e-8);checks++;}
        Check(prior.CopyHeightBits().SequenceEqual(saved));checks++;
        return new{Cases=output,OwnershipChecks=checks};
    }
}
