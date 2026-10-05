using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Stardust.Core.Geometry;

public sealed record TerrainFirstCurveFractalSparseLimits(int MaximumCells=1024,int MaximumRequests=256,int MaximumInputBytes=84,
    long MaximumCopyBytes=1048576,long MaximumRouteBytes=4194304,long MaximumWork=16777216,
    int MaximumQueryBytes=2097152,int MaximumQueryWork=16777216);

/// <summary>Owned arbitrary-order unique subset under the frozen explicit modern policy; no curved cache.</summary>
public sealed class TerrainFirstCurveFractalSparseBatch
{
    public sealed record Cell(int RequestIndex,int Index,TerrainConditionalFractalSampler.Request Request,
        TerrainConditionalFractalSampler.Result KernelResult,TerrainFirstCurveApproximationSample Approximation);
    private readonly byte[] original,derived;
    private TerrainFirstCurveFractalSparseBatch(byte[] original,byte[] derived,Cell[] cells,int width,int rows,
        TerrainFirstCurveFractalControls controls,int octaves,int powers,long copies,long route,long work,int queryBytes,int queryWork)
    {
        this.original=original;this.derived=derived;Cells=Array.AsReadOnly(cells);Width=width;Rows=rows;Controls=controls;
        OriginalSha256=Convert.ToHexString(SHA256.HashData(original));DerivedSha256=Convert.ToHexString(SHA256.HashData(derived));
        LogicalCopyBytes=copies;ReservedRouteBytes=route;ReservedWork=work;ReservedQueryBytes=queryBytes;ReservedQueryWork=queryWork;
        KernelOctaves=checked(cells.Length*octaves);PowerCalls=powers;
        AccountedRouteBytes=cells.Length==0?440:10088L+32L*ShapeCells+cells.Length*(320L+80L*octaves);
        AccountedWork=cells.Length==0?168:20424L+ShapeCells+cells.Length*(96L+512L*octaves);
    }
    public TerrainFirstCurveNumericPolicy Policy=>TerrainFirstCurveNumericPolicy.ModernBinary64LogPowApproximation;
    public bool OriginalNumericParity=>false;
    public int Width { get; }
    public int Rows { get; }
    public int ShapeCells=>Width*Rows;
    public TerrainFirstCurveFractalControls Controls { get; }
    public IReadOnlyList<Cell> Cells { get; }
    public string OriginalSha256 { get; }
    public string DerivedSha256 { get; }
    public byte[] CopyOriginalBytes()=>(byte[])original.Clone();
    public byte[] CopyDerivedKernelBytes()=>(byte[])derived.Clone();
    public int SuppliedRequests=>Cells.Count;
    public int SamplerFactories=>Cells.Count==0?0:1;
    public int KernelBatches=>Cells.Count==0?0:1;
    public int SamplerDisposals=>Cells.Count==0?0:1;
    public int ComputedMisses=>Cells.Count;
    public int KernelOctaves { get; }
    public int CurveEvaluations=>Cells.Count;
    public int LogCalls=>Cells.Count==0?0:2;
    public int PowerCalls { get; }
    public int EndpointEvaluations=>Cells.Count-PowerCalls;
    public long LogicalCopyBytes { get; }
    public long ReservedRouteBytes { get; }
    public long ReservedWork { get; }
    public int ReservedQueryBytes { get; }
    public int ReservedQueryWork { get; }
    public long AccountedRouteBytes { get; }
    public long AccountedWork { get; }
    private static void Need(bool okay,string reason){if(!okay)throw new InvalidDataException(reason);}
    private static bool Finite(uint bits)=>(bits&0x7f800000)!=0x7f800000;
    private static void Precision(int p){if(p is not (24 or 53 or 64))throw new ArgumentOutOfRangeException(nameof(p));}
    private static void Requests(ReadOnlySpan<TerrainConditionalFractalSampler.Request> requests,int width,int rows,CancellationToken cancellation)
    {
        Span<ulong> visited=stackalloc ulong[16];visited.Clear();
        foreach(var r in requests)
        {
            cancellation.ThrowIfCancellationRequested();
            Need(r.Column>=0&&r.Column<width&&r.Row>=0&&r.Row<rows&&Finite(r.XBits)&&Finite(r.ZBits),"Finite coordinates and exact indices inside full shape required.");
            int index=r.Row*width+r.Column;ulong mask=1UL<<(index&63);
            Need((visited[index>>6]&mask)==0,"Duplicate cell indices reject rather than reuse a cache entry.");
            visited[index>>6]|=mask;
        }
    }
    public static TerrainFirstCurveFractalSparseBatch Build(ReadOnlySpan<byte> encoded,int width,int rows,
        ReadOnlySpan<TerrainConditionalFractalSampler.Request> requests,TerrainFirstCurveFractalControls controls,
        TerrainFirstCurveNumericPolicy policy,TerrainFirstCurveFractalSparseLimits? limits=null,CancellationToken cancellation=default)
    {
        ArgumentNullException.ThrowIfNull(controls);limits??=new();
        if(policy!=TerrainFirstCurveNumericPolicy.ModernBinary64LogPowApproximation)throw new ArgumentOutOfRangeException(nameof(policy));
        Precision(controls.TablePrecision);Precision(controls.ConstructionPrecision);Precision(controls.QueryPrecision);
        if(limits.MaximumCells is <0 or >1024||limits.MaximumRequests is <0 or >1024||limits.MaximumInputBytes is <0 or >65536
            ||limits.MaximumCopyBytes is <0 or >4194304||limits.MaximumRouteBytes is <0 or >16777216
            ||limits.MaximumWork is <0 or >67108864||limits.MaximumQueryBytes is <0 or >2097152
            ||limits.MaximumQueryWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(limits));
        Need(width is >=1 and <=128&&rows is >=1 and <=128,"Bounded full shape required.");
        int shape=checked(width*rows),count=requests.Length;bool has=count!=0;
        Need(shape<=limits.MaximumCells&&count<=shape&&count<=limits.MaximumRequests,"Bounded full shape and subset counts required.");
        Need(encoded.Length==84&&encoded.Length<=limits.MaximumInputBytes,"Exact bounded MFRC/0001 required.");
        long copies=696L+176L*count,route=has?10088L+32L*shape+1600L*count:440,
            work=has?20424L+shape+8288L*count:168;
        int qb=has?checked(16*shape+1440*count):0,qw=has?checked(shape+8256*count):0;
        Need(copies<=limits.MaximumCopyBytes&&route<=limits.MaximumRouteBytes&&work<=limits.MaximumWork
            &&qb<=limits.MaximumQueryBytes&&qw<=limits.MaximumQueryWork,"Conservative full-cache/subset preflight before copies, arrays and sampler.");
        cancellation.ThrowIfCancellationRequested();Requests(requests,width,rows,cancellation);
        byte[] original=encoded.ToArray();var raw=TerrainFractalRawInput.Decode(original);var f=raw.Fields;
        Need(f.FirstCurveEnabled&&!f.SecondCurveEnabled,"First full-U32 flag enabled and second disabled required.");
        float parameter=BitConverter.Int32BitsToSingle(unchecked((int)f.FirstCurveParameterBits));
        Need(float.IsFinite(parameter)&&parameter>0&&parameter<1,"Modern parameter domain strictly (0,1).");
        Need(f.ModeBits<=5&&f.SignedOctaveCount is >=1 and <=16&&f.SignedSeed!=int.MaxValue,"Bounded supported raw kernel options required.");
        Need(Finite(f.FrequencyMultiplierBits)&&Finite(f.AmplitudeMultiplierBits)&&Finite(f.FirstCoordinateScaleBits)
            &&Finite(f.SecondCoordinateScaleBits)&&Finite(f.FirstOffsetBits)&&Finite(f.SecondOffsetBits),"Finite used kernel parameters required.");
        byte[] derived=(byte[])original.Clone();BinaryPrimitives.WriteUInt32LittleEndian(derived.AsSpan(36,4),0);
        for(int i=0;i<84;i++)Need(i is >=36 and <40||original[i]==derived[i],"Derivation changed another byte.");
        var kernelRaw=TerrainFractalRawInput.Decode(derived);
        Need(!kernelRaw.Fields.FirstCurveEnabled&&!kernelRaw.Fields.SecondCurveEnabled,"Derived kernel must have both curves disabled.");
        var owned=requests.ToArray();Requests(owned,width,rows,cancellation);
        if(!has)
        {
            // Structural/raw eligibility only: no normalization/table construction or transcendental calls.
            cancellation.ThrowIfCancellationRequested();
            return new(original,derived,Array.Empty<Cell>(),width,rows,controls,f.SignedOctaveCount,0,copies,route,work,qb,qw);
        }
        var cells=new Cell[count];int powers=0;
        double exponent=Math.Log((double)parameter)/Math.Log(0.5);
        Need(double.IsFinite(exponent)&&exponent>0,"Finite positive modern exponent required.");
        using(var sampler=TerrainConditionalFractalSampler.CreateForSupportedModes(kernelRaw,width,rows,controls.TablePrecision,
            controls.ConstructionPrecision,9512+16*shape+136,20256+16*f.SignedOctaveCount,false,cancellation))
        {
            var results=sampler.Query(owned,controls.QueryPrecision,qb,qw,cancellation);
            for(int i=0;i<count;i++)
            {
                cancellation.ThrowIfCancellationRequested();var result=results[i];
                Need(result.Origin==TerrainConditionalFractalOrigin.ConditionalComputedMiss,"Actual fresh computed miss required.");
                uint basis=result.Sample.ProjectionBits;double value=BitConverter.Int32BitsToSingle(unchecked((int)basis));
                Need(double.IsFinite(value)&&value>=0&&value<=1,"Modern base domain [0,1] required.");
                double output;
                if(value==0)output=0;else if(value==1)output=1;else{output=Math.Pow(value,exponent);powers++;}
                Need(double.IsFinite(output)&&output>=0&&output<=1,"Finite modern output in [0,1] required.");
                var approximation=new TerrainFirstCurveApproximationSample(basis,exponent,output);
                _=approximation.ToSuppliedSample();
                cells[i]=new(i,owned[i].Row*width+owned[i].Column,owned[i],result,approximation);
            }
            cancellation.ThrowIfCancellationRequested();
        }
        cancellation.ThrowIfCancellationRequested();
        return new(original,derived,cells,width,rows,controls,f.SignedOctaveCount,powers,copies,route,work,qb,qw);
    }
}
