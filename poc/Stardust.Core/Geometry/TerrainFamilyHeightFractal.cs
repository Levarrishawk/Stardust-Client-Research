using Stardust.Core.Assets;
namespace Stardust.Core.Geometry;

public readonly record struct TerrainFamilyHeightFractalRequest(int Width,int Rows,int Column,int Row,
    uint XBits,uint ZBits,uint InitialHeightBits,uint InfluenceBits);
public readonly record struct TerrainFamilyHeightFractalControls(int TablePrecision,int ConstructionPrecision,
    int QueryPrecision,TerrainHeightArithmeticPrecision ConsumerPrecision);
public sealed record TerrainFamilyHeightFractalLimits(int MaximumRuleBytes=65536,long MaximumOwnedBytes=1048576,long MaximumWork=1048576);
public sealed record TerrainFamilyHeightFractalReceipt(string FamilySetSha256,int FamilySetInputBytes,
    TerrainFractalFamilyEntry Family,TerrainFractalRawInput Raw,TerrainHeightFractalInput Rule,
    TerrainFamilyHeightFractalRequest Request,TerrainFamilyHeightFractalControls Controls,
    TerrainConditionalFractalOrigin Origin,TerrainSuppliedFractalSample Sample,TerrainRetainedHeightFractalResult Affector,
    long AccountedBytes,long AccountedWork);

/// <summary>Explicit enabled AHFR route with exact signed family lookup and one fresh computed miss per call.</summary>
public static class TerrainFamilyHeightFractal
{
    private static bool Finite(uint bits)=>(bits&0x7f800000)!=0x7f800000;
    private static void Need(bool value,string message){if(!value)throw new InvalidDataException(message);}
    private static void Precision(int value){if(value is not (24 or 53 or 64))throw new ArgumentOutOfRangeException(nameof(value));}
    public static TerrainFamilyHeightFractalReceipt Evaluate(TerrainFractalFamilySet families,ReadOnlySpan<byte> ruleEncoded,
        TerrainFamilyHeightFractalRequest request,TerrainFamilyHeightFractalControls controls,
        TerrainFamilyHeightFractalLimits? limits=null,CancellationToken cancellation=default)
    {
        ArgumentNullException.ThrowIfNull(families);limits??=new();
        if(limits.MaximumRuleBytes is <0 or >65536||limits.MaximumOwnedBytes is <0 or >4194304||limits.MaximumWork is <0 or >4194304)throw new ArgumentOutOfRangeException(nameof(limits));
        Precision(controls.TablePrecision);Precision(controls.ConstructionPrecision);Precision(controls.QueryPrecision);Precision((int)controls.ConsumerPrecision);
        Need(request.Width is >=1 and <=128&&request.Rows is >=1 and <=128&&request.Width*request.Rows<=1024,"Bounded caller cache shape required.");
        Need(request.Column>=0&&request.Column<request.Width&&request.Row>=0&&request.Row<request.Rows,"Exact caller cell outside shape.");
        Need(Finite(request.XBits)&&Finite(request.ZBits)&&Finite(request.InitialHeightBits)&&Finite(request.InfluenceBits),"Finite explicit operands required.");
        Need((request.InfluenceBits&0x80000000)==0&&(request.InfluenceBits&0x7fffffff)!=0,"Explicit positive influence required; no early-skip route.");
        Need(ruleEncoded.Length<=limits.MaximumRuleBytes,"Rule input cap before decode.");
        int cells=request.Width*request.Rows;
        // Conservative aggregate allowance includes decode copies, all16 possible octaves and receipt payload.
        long bytes=8L*ruleEncoded.Length+65536+TerrainSeededNoiseKernel.TableBytes+136+32L*cells+160+16*80+256;
        long work=ruleEncoded.Length+TerrainSeededNoiseKernel.BuildWork+16*16+cells+16*512+64+32;
        Need(bytes<=limits.MaximumOwnedBytes&&work<=limits.MaximumWork,"Complete logical route budget before decoding/builds.");
        cancellation.ThrowIfCancellationRequested();
        var rule=TerrainHeightFractalInput.Decode(ruleEncoded,new(limits.MaximumRuleBytes,64,8,4096,65536));
        Need(rule.Version=="0003"&&rule.Header is not null&&rule.Fields is not null,"Known typed AHFR/0003 required.");
        Need(rule.Header!.IsEnabled,"Explicitly enabled rule required; disabled is rejected.");
        var fields=rule.Fields!;Need(fields.OperationBits<=3&&fields.HasFiniteScale,"Only supported operations0..3 and finite scale are operational.");
        int id=unchecked((int)fields.FamilyKeyBits);
        var family=families.Find(id);Need(family is not null,"Exact signed family ID missing; no fallback.");
        var raw=family!.Raw;var f=raw.Fields;
        Need(f.ModeBits<=5&&!f.FirstCurveEnabled&&!f.SecondCurveEnabled&&f.SignedOctaveCount is >=1 and <=16&&f.SignedSeed!=int.MaxValue,"Family sampler operational subset required.");
        Need(Finite(f.FrequencyMultiplierBits)&&Finite(f.AmplitudeMultiplierBits)&&Finite(f.FirstCoordinateScaleBits)&&Finite(f.SecondCoordinateScaleBits)&&Finite(f.FirstOffsetBits)&&Finite(f.SecondOffsetBits),"Finite used family fields required; disabled curve words remain unused.");
        cancellation.ThrowIfCancellationRequested();
        using var sampler=TerrainConditionalFractalSampler.CreateForSupportedModes(raw,request.Width,request.Rows,
            controls.TablePrecision,controls.ConstructionPrecision,1048576,1048576,false,cancellation);
        var computed=sampler.Query([new(request.Column,request.Row,request.XBits,request.ZBits)],controls.QueryPrecision,2097152,16777216,cancellation)[0];
        Need(computed.Origin==TerrainConditionalFractalOrigin.ConditionalComputedMiss,"Fresh-call miss provenance required.");
        var affector=TerrainSuppliedHeightFractal.Evaluate(rule,request.InitialHeightBits,request.InfluenceBits,computed.Sample,controls.ConsumerPrecision);
        Need(affector.Consumer.Outcome==TerrainHeightFractalOutcome.Applied&&ReferenceEquals(affector.Sample,computed.Sample),"Actual retained affector/sample identity required.");
        cancellation.ThrowIfCancellationRequested();
        return new(families.SourceSha256,families.InputBytes,family,raw,rule,request,controls,computed.Origin,computed.Sample,affector,bytes,work);
    }
}
