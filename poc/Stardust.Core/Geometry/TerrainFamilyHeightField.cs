using System.Security.Cryptography;
using Stardust.Core.Assets;
namespace Stardust.Core.Geometry;

public readonly record struct TerrainFamilyHeightFieldRequest(int Width,int Rows,uint OriginXBits,uint OriginZBits,uint SpacingXBits,uint SpacingZBits);
public sealed record TerrainFamilyHeightFieldLimits(int MaximumSamples=256,int MaximumRuleBytes=65536,long MaximumCopyBytes=1048576,
    long MaximumRouteBytes=134217728,long MaximumWork=16777216,int MaximumQueryBytes=2097152,int MaximumQueryWork=16777216);
public enum TerrainFamilyHeightFieldCellStatus { Applied, SkippedNonPositiveInfluence }
public sealed record TerrainFamilyHeightFieldCell(int Index,int Column,int Row,uint XBits,uint ZBits,uint PriorHeightBits,
    uint InfluenceBits,uint HeightBits,TerrainFamilyHeightFieldCellStatus Status,TerrainFamilyHeightFractalReceipt? Applied);

/// <summary>Owned per-cell prior/influence field; one fresh unique-positive-cell sampler, explicit noise-free skips.</summary>
public sealed class TerrainFamilyHeightField
{
    private readonly byte[] rule;
    private readonly uint[] x,z,prior,influence,height;
    private TerrainFamilyHeightField(TerrainFamilyHeightFieldRequest request,TerrainFamilyHeightFractalControls controls,
        TerrainFamilyHeightFractal.Preparation prepared,byte[] rule,uint[] x,uint[] z,uint[] prior,uint[] influence,uint[] height,
        TerrainFamilyHeightFieldCell[] cells,TerrainFamilyHeightPatchBounds bounds,TerrainFamilyHeightPatchSamplingCounts counts,
        long copies,long reservedRoutes,long reservedWork,int reservedQueryBytes,int reservedQueryWork,long sharedBytes,long sharedWork,
        long routes,long work)
    {
        Request=request;Controls=controls;Family=prepared.Family;Raw=prepared.Raw;Rule=prepared.Rule;FamilySetSha256=prepared.Families.SourceSha256;
        this.rule=rule;this.x=x;this.z=z;this.prior=prior;this.influence=influence;this.height=height;RuleSha256=Convert.ToHexString(SHA256.HashData(rule));
        XAxisBits=Array.AsReadOnly(x);ZAxisBits=Array.AsReadOnly(z);PriorHeightBits=Array.AsReadOnly(prior);InfluenceBits=Array.AsReadOnly(influence);
        HeightBits=Array.AsReadOnly(height);Cells=Array.AsReadOnly(cells);Bounds=bounds;SamplingCounts=counts;LogicalCopyBytes=copies;
        ReservedRouteBytes=reservedRoutes;ReservedWork=reservedWork;ReservedQueryBytes=reservedQueryBytes;ReservedQueryWork=reservedQueryWork;
        SharedPreparationBytes=sharedBytes;SharedPreparationWork=sharedWork;AccountedRouteBytes=routes;AccountedWork=work;
    }
    public TerrainFamilyHeightFieldRequest Request{get;}
    public TerrainFamilyHeightFractalControls Controls{get;}
    public TerrainFractalFamilyEntry Family{get;}
    public TerrainFractalRawInput Raw{get;}
    public TerrainHeightFractalInput Rule{get;}
    public string FamilySetSha256{get;}
    public string RuleSha256{get;}
    public IReadOnlyList<uint> XAxisBits{get;}
    public IReadOnlyList<uint> ZAxisBits{get;}
    public IReadOnlyList<uint> PriorHeightBits{get;}
    public IReadOnlyList<uint> InfluenceBits{get;}
    public IReadOnlyList<uint> HeightBits{get;}
    public IReadOnlyList<TerrainFamilyHeightFieldCell> Cells{get;}
    public TerrainFamilyHeightPatchBounds Bounds{get;}
    public TerrainFamilyHeightPatchSamplingCounts SamplingCounts{get;}
    public long LogicalCopyBytes{get;}
    public long ReservedRouteBytes{get;}
    public long ReservedWork{get;}
    public int ReservedQueryBytes{get;}
    public int ReservedQueryWork{get;}
    public long SharedPreparationBytes{get;}
    public long SharedPreparationWork{get;}
    public long AccountedRouteBytes{get;}
    public long AccountedWork{get;}
    public byte[] CopyEncodedRule()=>(byte[])rule.Clone();
    public uint[] CopyXAxisBits()=>(uint[])x.Clone();
    public uint[] CopyZAxisBits()=>(uint[])z.Clone();
    public uint[] CopyPriorHeightBits()=>(uint[])prior.Clone();
    public uint[] CopyInfluenceBits()=>(uint[])influence.Clone();
    public uint[] CopyHeightBits()=>(uint[])height.Clone();
    private static bool Finite(uint bits)=>(bits&0x7f800000)!=0x7f800000;
    private static bool Positive(uint bits)=>(bits&0x80000000)==0&&(bits&0x7fffffff)!=0;
    private static void Need(bool v,string message){if(!v)throw new InvalidDataException(message);}
    private static void Precision(int p){if(p is not (24 or 53 or 64))throw new ArgumentOutOfRangeException(nameof(p));}
    private static void Display(uint bits)=>Need(Finite(bits)&&Math.Abs(BitConverter.UInt32BitsToSingle(bits))<=1_000_000,"Finite bounded resulting field height required.");
    public static TerrainFamilyHeightField Build(TerrainFractalFamilySet families,ReadOnlySpan<byte> ruleEncoded,
        TerrainFamilyHeightFieldRequest request,ReadOnlySpan<uint> priorHeightBits,ReadOnlySpan<uint> influenceBits,
        TerrainFamilyHeightFractalControls controls,TerrainFamilyHeightFieldLimits? limits=null,CancellationToken cancellation=default)
    {
        ArgumentNullException.ThrowIfNull(families);limits??=new();
        if(limits.MaximumSamples is <0 or >1024||limits.MaximumRuleBytes is <0 or >65536||limits.MaximumCopyBytes is <0 or >4194304||
            limits.MaximumRouteBytes is <0 or >268435456||limits.MaximumWork is <0 or >67108864||
            limits.MaximumQueryBytes is <0 or >2097152||limits.MaximumQueryWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(limits));
        Precision(controls.TablePrecision);Precision(controls.ConstructionPrecision);Precision(controls.QueryPrecision);Precision((int)controls.ConsumerPrecision);
        Need(request.Width is >=2 and <=128&&request.Rows is >=2 and <=128,"Bounded rectangular sample shape required.");
        int n=request.Width*request.Rows;Need(n<=limits.MaximumSamples&&priorHeightBits.Length==n&&influenceBits.Length==n,"Complete separately supplied row-major input shapes required.");
        Need(Finite(request.OriginXBits)&&Finite(request.OriginZBits)&&Finite(request.SpacingXBits)&&Finite(request.SpacingZBits)&&Positive(request.SpacingXBits)&&Positive(request.SpacingZBits),"Finite origin/positive spacing required.");
        Need(ruleEncoded.Length>=24&&ruleEncoded.Length<=limits.MaximumRuleBytes,"Bounded complete encoded rule required.");
        // Reserve EVERY cell as positive before reading/copying borrowed values. Owned values may only reduce these bounds.
        long copies=ruleEncoded.Length+4L*(request.Width+request.Rows)+100L*n+256;
        long decodeBytes=8L*ruleEncoded.Length+65536;
        long sharedMaximumBytes=decodeBytes+TerrainSeededNoiseKernel.TableBytes+136+32L*n;
        long sharedMaximumWork=ruleEncoded.Length+TerrainSeededNoiseKernel.BuildWork+16*16+n;
        const long cellBytes=1696,cellWork=8288;
        long reservedRoutes=sharedMaximumBytes+n*cellBytes;
        long reservedWork=ruleEncoded.Length+32L*(request.Width+request.Rows)+24L*n+sharedMaximumWork+n*(cellWork+16);
        int queryBytes=16*n+n*(160+16*80),queryWork=n+n*(16*512+64);
        Need(copies<=limits.MaximumCopyBytes&&reservedRoutes<=limits.MaximumRouteBytes&&reservedWork<=limits.MaximumWork&&
            queryBytes<=limits.MaximumQueryBytes&&queryWork<=limits.MaximumQueryWork,"Complete all-positive snapshot/route/query reservations before copies.");
        cancellation.ThrowIfCancellationRequested();byte[] rule=ruleEncoded.ToArray();uint[] prior=priorHeightBits.ToArray(),weights=influenceBits.ToArray();
        int positives=0;
        for(int i=0;i<n;i++){cancellation.ThrowIfCancellationRequested();Need(Finite(prior[i])&&Finite(weights[i]),"Finite owned per-cell inputs required.");if(Positive(weights[i]))positives++;}
        // All subsequent selection and arithmetic use these inaccessible owned arrays, never borrowed values.
        uint[] x=TerrainFamilyHeightPatch.Axis(request.OriginXBits,request.SpacingXBits,request.Width,cancellation),z=TerrainFamilyHeightPatch.Axis(request.OriginZBits,request.SpacingZBits,request.Rows,cancellation);
        var prepared=TerrainFamilyHeightFractal.Prepare(families,rule,limits.MaximumRuleBytes,cancellation);
        var requests=new TerrainConditionalFractalSampler.Request[positives];int cursor=0;
        for(int i=0;i<n;i++)if(Positive(weights[i]))requests[cursor++]=new(i%request.Width,i/request.Width,x[i%request.Width],z[i/request.Width]);
        var heights=new uint[n];var cells=new TerrainFamilyHeightFieldCell[n];uint minimum=0,maximum=0;int constructions=0,batches=0,applications=0,disposals=0;
        void CompleteCells(IReadOnlyList<TerrainConditionalFractalSampler.Result>? computed)
        {
            int selected=0;
            for(int i=0;i<n;i++)
            {
                cancellation.ThrowIfCancellationRequested();int column=i%request.Width,row=i/request.Width;TerrainFamilyHeightFractalReceipt? applied=null;
                if(Positive(weights[i]))
                {
                    applied=TerrainFamilyHeightFractal.Complete(prepared,new(request.Width,request.Rows,column,row,x[column],z[row],prior[i],weights[i]),
                        controls,computed![selected++],cellBytes,cellWork,cancellation,TerrainFamilyHeightFractalAccountingPolicy.SharedPreparationCell);
                    heights[i]=applied.Affector.Consumer.ResultHeightBits;applications++;
                }
                else heights[i]=prior[i];
                Display(heights[i]);cells[i]=new(i,column,row,x[column],z[row],prior[i],weights[i],heights[i],
                    applied is null?TerrainFamilyHeightFieldCellStatus.SkippedNonPositiveInfluence:TerrainFamilyHeightFieldCellStatus.Applied,applied);
                if(i==0||BitConverter.UInt32BitsToSingle(heights[i])<BitConverter.UInt32BitsToSingle(minimum))minimum=heights[i];
                if(i==0||BitConverter.UInt32BitsToSingle(heights[i])>BitConverter.UInt32BitsToSingle(maximum))maximum=heights[i];
            }
        }
        if(positives==0)CompleteCells(null);
        else
        {
            using(var sampler=TerrainConditionalFractalSampler.CreateForSupportedModes(prepared.Raw,request.Width,request.Rows,controls.TablePrecision,
                controls.ConstructionPrecision,TerrainSeededNoiseKernel.TableBytes+16*n+136,TerrainSeededNoiseKernel.BuildWork+16*16,false,cancellation))
            {
                constructions++;var computed=sampler.Query(requests,controls.QueryPrecision,queryBytes,queryWork,cancellation);batches++;
                Need(computed.Count==positives,"Complete selected-cell query required.");CompleteCells(computed);
            }
            disposals++;
        }
        cancellation.ThrowIfCancellationRequested();long sharedBytes=decodeBytes+(positives==0?0:TerrainSeededNoiseKernel.TableBytes+136+32L*n);
        long sharedWork=ruleEncoded.Length+(positives==0?0:TerrainSeededNoiseKernel.BuildWork+16*16+n);
        long routes=sharedBytes+positives*cellBytes,work=ruleEncoded.Length+32L*(request.Width+request.Rows)+24L*n+sharedWork+positives*(cellWork+16);
        var counts=new TerrainFamilyHeightPatchSamplingCounts(1,1,constructions,batches,positives,applications,disposals,positives*prepared.Raw.Fields.SignedOctaveCount);
        return new(request,controls,prepared,rule,x,z,prior,weights,heights,cells,new(x[0],x[^1],z[0],z[^1],minimum,maximum),counts,
            copies,reservedRoutes,reservedWork,queryBytes,queryWork,sharedBytes,sharedWork,routes,work);
    }
}
