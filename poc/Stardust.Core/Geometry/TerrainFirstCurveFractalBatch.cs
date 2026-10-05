using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;

namespace Stardust.Core.Geometry;

public enum TerrainFirstCurveNumericPolicy { ModernBinary64LogPowApproximation }
public sealed record TerrainFirstCurveFractalControls(int TablePrecision, int ConstructionPrecision, int QueryPrecision);
public sealed record TerrainFirstCurveFractalLimits(int MaximumSamples=256, int MaximumInputBytes=84,
    long MaximumCopyBytes=1048576, long MaximumRouteBytes=4194304, long MaximumWork=16777216,
    int MaximumQueryBytes=2097152, int MaximumQueryWork=16777216);

/// <summary>Owned modern output, never a recovered kernel result. Conversion is explicitly caller supplied.</summary>
public sealed class TerrainFirstCurveApproximationSample
{
    internal TerrainFirstCurveApproximationSample(uint basis, double exponent, double value)
    {
        BaseF32Bits=basis;ExponentBinary64Bits=unchecked((ulong)BitConverter.DoubleToInt64Bits(exponent));
        OutputBinary64Bits=unchecked((ulong)BitConverter.DoubleToInt64Bits(value));
        ulong fraction=OutputBinary64Bits&0x000fffffffffffffUL;
        int encoded=(int)((OutputBinary64Bits>>52)&2047);
        ulong magnitude=encoded==0?fraction:fraction|(1UL<<52);
        int power=encoded==0?-1074:encoded-1075;
        if(magnitude==0)power=0;
        else { int zeros=BitOperations.TrailingZeroCount(magnitude);magnitude>>=zeros;power+=zeros; }
        if(power is <-512 or >512)throw new InvalidDataException("Nonzero modern binary64 output cannot be exported exactly through the frozen supplied primitive.");
        Magnitude=magnitude;Exponent=power;
    }
    public TerrainFirstCurveNumericPolicy Policy=>TerrainFirstCurveNumericPolicy.ModernBinary64LogPowApproximation;
    public bool OriginalNumericParity=>false;
    public uint BaseF32Bits { get; }
    public ulong ExponentBinary64Bits { get; }
    public ulong OutputBinary64Bits { get; }
    public ulong Magnitude { get; }
    public int Exponent { get; }
    /// <summary>Exact dyadic conversion; SuppliedWideMiss means caller supplied, not original numeric parity.</summary>
    public TerrainSuppliedFractalSample ToSuppliedSample()=>TerrainSuppliedFractalSample.FromWideMagnitude(Magnitude,false,Exponent);
}

/// <summary>One fresh full rectangle, curves-disabled derived kernel, then an explicitly modern first curve.</summary>
public sealed class TerrainFirstCurveFractalBatch
{
    public sealed record Cell(int Index, TerrainConditionalFractalSampler.Request Request,
        TerrainConditionalFractalSampler.Result KernelResult, TerrainFirstCurveApproximationSample Approximation);
    private readonly byte[] original,derived;
    private TerrainFirstCurveFractalBatch(byte[] original,byte[] derived,Cell[] cells,int width,int rows,
        TerrainFirstCurveFractalControls controls,int octaves,long copies,long route,long work,int queryBytes,int queryWork,int powers)
    {
        this.original=original;this.derived=derived;Cells=Array.AsReadOnly(cells);Width=width;Rows=rows;Controls=controls;
        OriginalSha256=Convert.ToHexString(SHA256.HashData(original));DerivedSha256=Convert.ToHexString(SHA256.HashData(derived));
        LogicalCopyBytes=copies;ReservedRouteBytes=route;ReservedWork=work;ReservedQueryBytes=queryBytes;ReservedQueryWork=queryWork;
        ComputedMisses=cells.Length;KernelOctaves=checked(cells.Length*octaves);PowerCalls=powers;
        AccountedRouteBytes=10088L+cells.Length*(352L+80L*octaves);
        AccountedWork=20424L+cells.Length*(97L+512L*octaves);
    }
    public TerrainFirstCurveNumericPolicy Policy=>TerrainFirstCurveNumericPolicy.ModernBinary64LogPowApproximation;
    public bool OriginalNumericParity=>false;
    public int Width { get; }
    public int Rows { get; }
    public TerrainFirstCurveFractalControls Controls { get; }
    public IReadOnlyList<Cell> Cells { get; }
    public string OriginalSha256 { get; }
    public string DerivedSha256 { get; }
    public byte[] CopyOriginalBytes()=>(byte[])original.Clone();
    public byte[] CopyDerivedKernelBytes()=>(byte[])derived.Clone();
    public int SamplerFactories=>1;
    public int KernelBatches=>1;
    public int SamplerDisposals=>1;
    public int ComputedMisses { get; }
    public int KernelOctaves { get; }
    public int CurveEvaluations=>Cells.Count;
    public int LogCalls=>2;
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
    public static TerrainFirstCurveFractalBatch Build(ReadOnlySpan<byte> encoded,int width,int rows,
        ReadOnlySpan<TerrainConditionalFractalSampler.Request> requests,TerrainFirstCurveFractalControls controls,
        TerrainFirstCurveNumericPolicy policy,TerrainFirstCurveFractalLimits? limits=null,CancellationToken cancellation=default)
    {
        ArgumentNullException.ThrowIfNull(controls);limits??=new();
        if(policy!=TerrainFirstCurveNumericPolicy.ModernBinary64LogPowApproximation)throw new ArgumentOutOfRangeException(nameof(policy));
        Precision(controls.TablePrecision);Precision(controls.ConstructionPrecision);Precision(controls.QueryPrecision);
        if(limits.MaximumSamples is <0 or >1024||limits.MaximumInputBytes is <0 or >65536
            ||limits.MaximumCopyBytes is <0 or >4194304||limits.MaximumRouteBytes is <0 or >16777216
            ||limits.MaximumWork is <0 or >67108864||limits.MaximumQueryBytes is <0 or >2097152
            ||limits.MaximumQueryWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(limits));
        Need(width is >=1 and <=128&&rows is >=1 and <=128,"Bounded rectangle required.");
        int count=checked(width*rows);
        Need(count<=limits.MaximumSamples&&count==requests.Length,"Complete bounded rectangle required.");
        Need(encoded.Length==84&&encoded.Length<=limits.MaximumInputBytes,"Exact bounded MFRC/0001 bytes required.");
        long copies=440L+176L*count+256,route=10088L+1632L*count,work=20424L+8289L*count;
        int qb=checked(1456*count),qw=checked(8257*count);
        Need(copies<=limits.MaximumCopyBytes&&route<=limits.MaximumRouteBytes&&work<=limits.MaximumWork
            &&qb<=limits.MaximumQueryBytes&&qw<=limits.MaximumQueryWork,"Conservative count/copy/route/work/query preflight before allocation.");
        cancellation.ThrowIfCancellationRequested();
        for(int i=0;i<count;i++)
        {
            cancellation.ThrowIfCancellationRequested();var r=requests[i];
            Need(r.Column==i%width&&r.Row==i/width,"Unique complete row-major rectangle required.");
            Need(Finite(r.XBits)&&Finite(r.ZBits),"Finite supplied F32 coordinates required.");
        }
        byte[] original=encoded.ToArray();var raw=TerrainFractalRawInput.Decode(original);var f=raw.Fields;
        Need(f.FirstCurveEnabled&&!f.SecondCurveEnabled,"First full-U32 flag enabled and second disabled required.");
        float parameter=BitConverter.Int32BitsToSingle(unchecked((int)f.FirstCurveParameterBits));
        Need(float.IsFinite(parameter)&&parameter>0&&parameter<1,"Modern parameter domain strictly (0,1).");
        Need(f.ModeBits<=5&&f.SignedOctaveCount is >=1 and <=16&&f.SignedSeed!=int.MaxValue,"Bounded supported kernel required.");
        Need(Finite(f.FrequencyMultiplierBits)&&Finite(f.AmplitudeMultiplierBits)&&Finite(f.FirstCoordinateScaleBits)
            &&Finite(f.SecondCoordinateScaleBits)&&Finite(f.FirstOffsetBits)&&Finite(f.SecondOffsetBits),"Finite used kernel parameters required.");
        byte[] derived=(byte[])original.Clone();BinaryPrimitives.WriteUInt32LittleEndian(derived.AsSpan(36,4),0);
        for(int i=0;i<84;i++)Need(i is >=36 and <40||derived[i]==original[i],"Derivation changed another byte.");
        var kernelRaw=TerrainFractalRawInput.Decode(derived);
        Need(!kernelRaw.Fields.FirstCurveEnabled&&!kernelRaw.Fields.SecondCurveEnabled,"Derived kernel must remain curves disabled.");
        var owned=requests.ToArray();
        // Revalidate the owned request snapshot: a borrowed input can change after initial preflight.
        for(int i=0;i<count;i++)
        {
            cancellation.ThrowIfCancellationRequested();var r=owned[i];
            Need(r.Column==i%width&&r.Row==i/width&&Finite(r.XBits)&&Finite(r.ZBits),"Owned complete row-major finite snapshot required.");
        }
        var cells=new Cell[count];int powers=0;
        double exponent=Math.Log((double)parameter)/Math.Log(0.5);
        Need(double.IsFinite(exponent)&&exponent>0,"Finite positive modern exponent required.");
        using(var sampler=TerrainConditionalFractalSampler.CreateForSupportedModes(kernelRaw,width,rows,controls.TablePrecision,
            controls.ConstructionPrecision,9512+16*count+136,20256+16*f.SignedOctaveCount,false,cancellation))
        {
            var results=sampler.Query(owned,controls.QueryPrecision,qb,qw,cancellation);
            for(int i=0;i<count;i++)
            {
                cancellation.ThrowIfCancellationRequested();var result=results[i];
                Need(result.Origin==TerrainConditionalFractalOrigin.ConditionalComputedMiss,"Fresh unique actual computed miss required.");
                uint basis=result.Sample.ProjectionBits;double value=BitConverter.Int32BitsToSingle(unchecked((int)basis));
                Need(double.IsFinite(value)&&value>=0&&value<=1,"Modern base domain [0,1] required.");
                double output;
                if(value==0)output=0;else if(value==1)output=1;else{output=Math.Pow(value,exponent);powers++;}
                Need(double.IsFinite(output)&&output>=0&&output<=1,"Finite modern output in [0,1] required.");
                var approximation=new TerrainFirstCurveApproximationSample(basis,exponent,output);
                _=approximation.ToSuppliedSample();
                cells[i]=new(i,owned[i],result,approximation);
            }
            cancellation.ThrowIfCancellationRequested();
        }
        cancellation.ThrowIfCancellationRequested();
        return new(original,derived,cells,width,rows,controls,f.SignedOctaveCount,copies,route,work,qb,qw,powers);
    }
}
