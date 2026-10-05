using System.Security.Cryptography;
using Stardust.Core.Assets;
using Number=Stardust.Core.Geometry.TerrainCircleArithmetic.Number;
namespace Stardust.Core.Geometry;

public readonly record struct TerrainFamilyHeightPatchRequest(int Width,int Rows,uint OriginXBits,uint OriginZBits,
    uint SpacingXBits,uint SpacingZBits,uint InitialHeightBits,uint InfluenceBits);
public sealed record TerrainFamilyHeightPatchLimits(int MaximumSamples=256,int MaximumRuleBytes=65536,
    long MaximumCopyBytes=1048576,long MaximumRouteBytes=134217728,long MaximumWork=16777216);
public readonly record struct TerrainFamilyHeightPatchBounds(uint MinimumXBits,uint MaximumXBits,uint MinimumZBits,
    uint MaximumZBits,uint MinimumHeightBits,uint MaximumHeightBits);

public enum TerrainFamilyHeightPatchSamplingPolicy { FreshSamplerPerSample, SingleFreshSamplerUniqueCells }
public readonly record struct TerrainFamilyHeightPatchSamplingCounts(int RuleDecodes,int FamilyLookups,int SamplerConstructions,
    int SamplerQueryBatches,int ComputedCells,int HeightApplications,int SamplerDisposals,int KernelOctaveQueries);

/// <summary>Owned rectangular height samples under explicit modern coordinate/fresh-call policy; no terrain generator or mesh.</summary>
public sealed class TerrainFamilyHeightPatch
{
    private readonly uint[] xAxis,zAxis,heights;
    private readonly byte[] encodedRule;
    private TerrainFamilyHeightPatch(TerrainFamilyHeightPatchRequest request,TerrainFamilyHeightFractalControls controls,
        uint[] x,uint[] z,uint[] height,byte[] rule,TerrainFamilyHeightFractalReceipt[] receipts,
        TerrainFamilyHeightPatchBounds bounds,long copies,long routes,long work,
        TerrainFamilyHeightPatchSamplingPolicy samplingPolicy=TerrainFamilyHeightPatchSamplingPolicy.FreshSamplerPerSample,
        long sharedBytes=0,long sharedWork=0,TerrainFamilyHeightPatchSamplingCounts? counts=null)
    {
        Request=request;Controls=controls;xAxis=x;zAxis=z;heights=height;encodedRule=rule;
        XAxisBits=Array.AsReadOnly(x);ZAxisBits=Array.AsReadOnly(z);HeightBits=Array.AsReadOnly(height);Samples=Array.AsReadOnly(receipts);
        Bounds=bounds;LogicalCopyBytes=copies;AggregateRouteBytes=routes;AccountedWork=work;
        SamplingPolicy=samplingPolicy;SharedPreparationBytes=sharedBytes;SharedPreparationWork=sharedWork;
        int n=receipts.Length;SamplingCounts=counts??new(n,n,n,n,n,n,n,n*receipts[0].Raw.Fields.SignedOctaveCount);
        FamilySetSha256=receipts[0].FamilySetSha256;RuleSha256=Convert.ToHexString(SHA256.HashData(rule));
    }
    public TerrainFamilyHeightPatchRequest Request{get;}
    public TerrainFamilyHeightFractalControls Controls{get;}
    public int Width=>Request.Width;
    public int Rows=>Request.Rows;
    public string FamilySetSha256{get;}
    public string RuleSha256{get;}
    public IReadOnlyList<uint> XAxisBits{get;}
    public IReadOnlyList<uint> ZAxisBits{get;}
    public IReadOnlyList<uint> HeightBits{get;}
    public IReadOnlyList<TerrainFamilyHeightFractalReceipt> Samples{get;}
    public TerrainFamilyHeightPatchBounds Bounds{get;}
    public long LogicalCopyBytes{get;}
    public long AggregateRouteBytes{get;}
    public long AccountedWork{get;}
    public TerrainFamilyHeightPatchSamplingPolicy SamplingPolicy{get;}
    public TerrainFamilyHeightPatchSamplingCounts SamplingCounts{get;}
    public long SharedPreparationBytes{get;}
    public long SharedPreparationWork{get;}
    public uint[] CopyXAxisBits()=>(uint[])xAxis.Clone();
    public uint[] CopyZAxisBits()=>(uint[])zAxis.Clone();
    public uint[] CopyHeightBits()=>(uint[])heights.Clone();
    public byte[] CopyEncodedRule()=>(byte[])encodedRule.Clone();
    private static void Need(bool value,string message){if(!value)throw new InvalidDataException(message);}
    private static bool Finite(uint bits)=>(bits&0x7f800000)!=0x7f800000;
    private static void Precision(int value){if(value is not (24 or 53 or 64))throw new ArgumentOutOfRangeException(nameof(value));}
    private static void Display(uint bits)=>Need(Finite(bits)&&Math.Abs(BitConverter.UInt32BitsToSingle(bits))<=1_000_000,"Finite bounded display component required.");
    internal static uint[] Axis(uint origin,uint spacing,int count,CancellationToken cancellation)
    {
        var axis=new uint[count];var start=Number.FromBits(origin);var step=Number.FromBits(spacing);
        for(int i=0;i<count;i++)
        {
            cancellation.ThrowIfCancellationRequested();
            // Modern policy: separately stored nearest-even F32 product then sum; no accumulated stepping/FMA.
            uint offset=Number.Multiply(new(i,0),step,24).ToFloatBits();Need(Finite(offset),"Finite coordinate product required.");
            axis[i]=Number.Add(start,Number.FromBits(offset),24).ToFloatBits();Display(axis[i]);
            if(i>0)Need(BitConverter.UInt32BitsToSingle(axis[i])>BitConverter.UInt32BitsToSingle(axis[i-1]),"Strict stored axis order required.");
        }
        return axis;
    }
    public static TerrainFamilyHeightPatch Build(TerrainFractalFamilySet families,ReadOnlySpan<byte> ruleEncoded,
        TerrainFamilyHeightPatchRequest request,TerrainFamilyHeightFractalControls controls,
        TerrainFamilyHeightPatchLimits? limits=null,CancellationToken cancellation=default)
    {
        limits??=new();int count=ValidateRequest(families,ruleEncoded,request,controls,limits);
        long copies=ruleEncoded.Length+4L*(request.Width+request.Rows+count)+8L*count+256;
        // Frozen51 route allowance for one fresh 1x1 cell, conservatively reserved for EVERY sample.
        long perBytes=8L*ruleEncoded.Length+65536+9512+136+32+160+16*80+256;
        long perWork=ruleEncoded.Length+20000+16*16+1+16*512+64+32;
        long routes=count*perBytes,work=ruleEncoded.Length+32L*(request.Width+request.Rows)+count*(perWork+16);
        Need(copies<=limits.MaximumCopyBytes&&routes<=limits.MaximumRouteBytes&&work<=limits.MaximumWork,"Complete copy/route/work budget before first evaluation.");
        cancellation.ThrowIfCancellationRequested();
        byte[] rule=ruleEncoded.ToArray();
        uint[] x=Axis(request.OriginXBits,request.SpacingXBits,request.Width,cancellation),z=Axis(request.OriginZBits,request.SpacingZBits,request.Rows,cancellation);
        var height=new uint[count];var receipts=new TerrainFamilyHeightFractalReceipt[count];
        var routeLimits=new TerrainFamilyHeightFractalLimits(limits.MaximumRuleBytes,perBytes,perWork);
        uint minimum=0,maximum=0;
        for(int row=0;row<request.Rows;row++)for(int column=0;column<request.Width;column++)
        {
            cancellation.ThrowIfCancellationRequested();int i=row*request.Width+column;
            var sample=TerrainFamilyHeightFractal.Evaluate(families,rule,new(1,1,0,0,x[column],z[row],request.InitialHeightBits,request.InfluenceBits),controls,routeLimits,cancellation);
            uint bits=sample.Affector.Consumer.ResultHeightBits;Display(bits);height[i]=bits;receipts[i]=sample;
            if(i==0||BitConverter.UInt32BitsToSingle(bits)<BitConverter.UInt32BitsToSingle(minimum))minimum=bits;
            if(i==0||BitConverter.UInt32BitsToSingle(bits)>BitConverter.UInt32BitsToSingle(maximum))maximum=bits;
        }
        cancellation.ThrowIfCancellationRequested();
        return new(request,controls,x,z,height,rule,receipts,new(x[0],x[^1],z[0],z[^1],minimum,maximum),copies,routes,work);
    }

    private static int ValidateRequest(TerrainFractalFamilySet families,ReadOnlySpan<byte> ruleEncoded,
        TerrainFamilyHeightPatchRequest request,TerrainFamilyHeightFractalControls controls,TerrainFamilyHeightPatchLimits limits)
    {
        ArgumentNullException.ThrowIfNull(families);
        if(limits.MaximumSamples is <0 or >1024||limits.MaximumRuleBytes is <0 or >65536||
            limits.MaximumCopyBytes is <0 or >4194304||limits.MaximumRouteBytes is <0 or >268435456||limits.MaximumWork is <0 or >67108864)
            throw new ArgumentOutOfRangeException(nameof(limits));
        Precision(controls.TablePrecision);Precision(controls.ConstructionPrecision);Precision(controls.QueryPrecision);Precision((int)controls.ConsumerPrecision);
        Need(request.Width is >=2 and <=128&&request.Rows is >=2 and <=128,"Rectangular sample dimensions 2..128 required.");
        int count=request.Width*request.Rows;Need(count<=limits.MaximumSamples,"Complete sample cap before copies/evaluation.");
        Need(Finite(request.OriginXBits)&&Finite(request.OriginZBits)&&Finite(request.SpacingXBits)&&Finite(request.SpacingZBits)&&
            Finite(request.InitialHeightBits)&&Finite(request.InfluenceBits),"Finite caller fields required.");
        Need((request.SpacingXBits&0x80000000)==0&&(request.SpacingXBits&0x7fffffff)!=0&&
            (request.SpacingZBits&0x80000000)==0&&(request.SpacingZBits&0x7fffffff)!=0,"Strict positive axis spacing required.");
        Need((request.InfluenceBits&0x80000000)==0&&(request.InfluenceBits&0x7fffffff)!=0,"Explicit positive influence required.");
        Need(ruleEncoded.Length>=24&&ruleEncoded.Length<=limits.MaximumRuleBytes,"Complete bounded rule bytes required.");
        return count;
    }
    /// <summary>Opt-in one fresh sampler, one row-major batch of unique cells, no cache reuse between calls.</summary>
    public static TerrainFamilyHeightPatch BuildSingleSampler(TerrainFractalFamilySet families,ReadOnlySpan<byte> ruleEncoded,
        TerrainFamilyHeightPatchRequest request,TerrainFamilyHeightFractalControls controls,
        TerrainFamilyHeightPatchLimits? limits=null,CancellationToken cancellation=default)
    {
        limits??=new();int count=ValidateRequest(families,ruleEncoded,request,controls,limits);
        long copies=ruleEncoded.Length+4L*(request.Width+request.Rows+count)+8L*count+256+16L*count;
        // Reserve maximum16 octaves before decoding. Shared costs occur once; cells never include another table build.
        long sharedBytes=8L*ruleEncoded.Length+65536+TerrainSeededNoiseKernel.TableBytes+136+32L*count;
        long sharedWork=ruleEncoded.Length+TerrainSeededNoiseKernel.BuildWork+16*16+count;
        const long cellBytes=160+16*80+256,cellWork=16*512+64+32;
        long routes=sharedBytes+count*cellBytes,work=ruleEncoded.Length+32L*(request.Width+request.Rows)+sharedWork+count*(cellWork+32);
        Need(copies<=limits.MaximumCopyBytes&&routes<=limits.MaximumRouteBytes&&work<=limits.MaximumWork,
            "Complete single-sampler copy/shared/cell/work budget before copies or preparation.");
        cancellation.ThrowIfCancellationRequested();byte[] rule=ruleEncoded.ToArray();
        uint[] x=Axis(request.OriginXBits,request.SpacingXBits,request.Width,cancellation),z=Axis(request.OriginZBits,request.SpacingZBits,request.Rows,cancellation);
        var prepared=TerrainFamilyHeightFractal.Prepare(families,rule,limits.MaximumRuleBytes,cancellation);
        var requests=new TerrainConditionalFractalSampler.Request[count];
        for(int row=0;row<request.Rows;row++)for(int column=0;column<request.Width;column++)
        { cancellation.ThrowIfCancellationRequested();requests[row*request.Width+column]=new(column,row,x[column],z[row]); }
        var height=new uint[count];var receipts=new TerrainFamilyHeightFractalReceipt[count];uint minimum=0,maximum=0;
        int constructions=0,batches=0,applications=0,disposals=0;
        using(var sampler=TerrainConditionalFractalSampler.CreateForSupportedModes(prepared.Raw,request.Width,request.Rows,
            controls.TablePrecision,controls.ConstructionPrecision,TerrainSeededNoiseKernel.TableBytes+16*count+136,
            TerrainSeededNoiseKernel.BuildWork+16*16,false,cancellation))
        {
            constructions++;
            var computed=sampler.Query(requests,controls.QueryPrecision,16*count+count*(160+16*80),
                count+count*(16*512+64),cancellation);batches++;
            Need(computed.Count==count,"Complete unique-cell query result required.");
            for(int i=0;i<count;i++)
            {
                cancellation.ThrowIfCancellationRequested();var q=requests[i];
                var sample=TerrainFamilyHeightFractal.Complete(prepared,new(request.Width,request.Rows,q.Column,q.Row,q.XBits,q.ZBits,
                    request.InitialHeightBits,request.InfluenceBits),controls,computed[i],cellBytes,cellWork,cancellation,
                    TerrainFamilyHeightFractalAccountingPolicy.SharedPreparationCell);
                uint bits=sample.Affector.Consumer.ResultHeightBits;Display(bits);height[i]=bits;receipts[i]=sample;applications++;
                if(i==0||BitConverter.UInt32BitsToSingle(bits)<BitConverter.UInt32BitsToSingle(minimum))minimum=bits;
                if(i==0||BitConverter.UInt32BitsToSingle(bits)>BitConverter.UInt32BitsToSingle(maximum))maximum=bits;
            }
        }
        disposals++;cancellation.ThrowIfCancellationRequested();
        var counts=new TerrainFamilyHeightPatchSamplingCounts(1,1,constructions,batches,count,applications,disposals,
            count*prepared.Raw.Fields.SignedOctaveCount);
        return new(request,controls,x,z,height,rule,receipts,new(x[0],x[^1],z[0],z[^1],minimum,maximum),copies,routes,work,
            TerrainFamilyHeightPatchSamplingPolicy.SingleFreshSamplerUniqueCells,sharedBytes,sharedWork,counts);
    }
}
