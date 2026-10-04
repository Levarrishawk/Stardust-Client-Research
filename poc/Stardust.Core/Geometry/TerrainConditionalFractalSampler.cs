using System.Collections.ObjectModel;
using System.Numerics;
using Number = Stardust.Core.Geometry.TerrainCircleArithmetic.Number;
namespace Stardust.Core.Geometry;

public enum TerrainConditionalFractalOrigin { ConditionalComputedMiss, CacheReloadF32 }
/// <summary>Fresh curves-disabled sampler under explicit nearest-even profiles; mode0 default, modes0..5 opt-in.</summary>
public sealed class TerrainConditionalFractalSampler : IDisposable
{
    public readonly record struct Request(int Column, int Row, uint XBits, uint ZBits);
    public readonly record struct CacheCell(bool Occupied, uint XBits, uint ZBits, uint ValueBits);
    public sealed record Result(TerrainConditionalFractalOrigin Origin, TerrainSuppliedFractalSample Sample);
    public const int MaximumCells = 1024, MaximumRequests = 1024;
    private readonly int owner = Environment.CurrentManagedThreadId;
    private readonly TerrainFractalRawFields fields;
    private TerrainSeededNoiseKernel? kernel;
    private CacheCell[] cache;
    private bool busy;
    public int Width { get; }
    public int Rows { get; }
    public int TablePrecision { get; }
    public int ConstructionPrecision { get; }
    public uint NormalizationBits { get; }
    private TerrainConditionalFractalSampler(TerrainFractalRawFields fields, TerrainSeededNoiseKernel kernel, int width, int rows, int tp, int cp, uint normalization)
    { this.fields=fields;this.kernel=kernel;Width=width;Rows=rows;TablePrecision=tp;ConstructionPrecision=cp;NormalizationBits=normalization;cache=new CacheCell[width*rows]; }
    private static Number I(long value)=>new(new BigInteger(value),0);
    private static Number N(uint bits)=>Number.FromBits(bits);
    private static bool Finite(uint bits)=>(bits&0x7f800000)!=0x7f800000;
    private static void Need(bool value,string message){if(!value)throw new InvalidDataException(message);}
    private static void Precision(int p){if(p is not (24 or 53 or 64))throw new ArgumentOutOfRangeException(nameof(p));}
    private static void Cap(int actual,int required,int maximum){if(actual<required||actual>maximum)throw new ArgumentOutOfRangeException(nameof(actual),"Bounded explicit budget required.");}
    private static uint Store(Number value){uint bits=value.ToFloatBits();Need(Finite(bits),"Finite recovered F32 store required.");return bits;}
    public static TerrainConditionalFractalSampler Create(TerrainFractalRawInput raw, int width, int rows, int tablePrecision, int constructionPrecision,
        int byteBudget, int workBudget, bool sineEnabled=false, CancellationToken cancellation=default)
        => CreateInternal(raw,width,rows,tablePrecision,constructionPrecision,byteBudget,workBudget,sineEnabled,cancellation,false);
    /// <summary>Explicit opt-in for the recovered finite mode0..5 bodies; optional curves/sine remain excluded.</summary>
    public static TerrainConditionalFractalSampler CreateForSupportedModes(TerrainFractalRawInput raw, int width, int rows, int tablePrecision, int constructionPrecision,
        int byteBudget, int workBudget, bool sineEnabled=false, CancellationToken cancellation=default)
        => CreateInternal(raw,width,rows,tablePrecision,constructionPrecision,byteBudget,workBudget,sineEnabled,cancellation,true);
    private static TerrainConditionalFractalSampler CreateInternal(TerrainFractalRawInput raw, int width, int rows, int tablePrecision, int constructionPrecision,
        int byteBudget, int workBudget, bool sineEnabled, CancellationToken cancellation, bool supportedModes)
    {
        ArgumentNullException.ThrowIfNull(raw);Precision(tablePrecision);Precision(constructionPrecision);
        if(width is <1 or >128||rows is <1 or >128||width*rows>MaximumCells)throw new ArgumentOutOfRangeException(nameof(width));
        var f=raw.Fields;
        Need((supportedModes?f.ModeBits<=5:f.ModeBits==0)&&!f.FirstCurveEnabled&&!f.SecondCurveEnabled&&!sineEnabled,supportedModes?"Only fresh curves-disabled sine-disabled modes0..5 are operational.":"Only fresh curves-disabled sine-disabled mode0 is operational.");
        Need(f.SignedOctaveCount is >=1 and <=16,"Positive bounded octaves required.");
        Need(Finite(f.FrequencyMultiplierBits)&&Finite(f.AmplitudeMultiplierBits)&&Finite(f.FirstCoordinateScaleBits)&&Finite(f.SecondCoordinateScaleBits)&&Finite(f.FirstOffsetBits)&&Finite(f.SecondOffsetBits),"Used parameters must be finite; disabled curve words are not evaluated.");
        Cap(byteBudget,TerrainSeededNoiseKernel.TableBytes+16*width*rows+TerrainFractalRawInput.OwnedCopyBytes,1048576);
        Cap(workBudget,TerrainSeededNoiseKernel.BuildWork+16*f.SignedOctaveCount,1048576);
        cancellation.ThrowIfCancellationRequested();
        Number weight=I(1),sum=I(0),factor=N(f.AmplitudeMultiplierBits);
        for(int i=0;i<f.SignedOctaveCount;i++)
        { cancellation.ThrowIfCancellationRequested();sum=Number.Add(sum,weight,constructionPrecision);weight=Number.Multiply(weight,factor,constructionPrecision); }
        uint storedSum=Store(sum);Need((storedSum&0x7fffffff)!=0,"Zero stored weight sum has no eligible reciprocal.");
        uint normalization=Store(Number.Divide(I(1),N(storedSum),constructionPrecision));
        var kernel=TerrainSeededNoiseKernel.Create(f.SignedSeed,tablePrecision,TerrainSeededNoiseKernel.TableBytes,TerrainSeededNoiseKernel.BuildWork,cancellation);
        try{cancellation.ThrowIfCancellationRequested();return new(f,kernel,width,rows,tablePrecision,constructionPrecision,normalization);}
        catch{kernel.Dispose();throw;}
    }
    private void Enter()
    {
        if(Environment.CurrentManagedThreadId!=owner)throw new InvalidOperationException("Sampler belongs to its creating thread.");
        if(kernel is null)throw new ObjectDisposedException(nameof(TerrainConditionalFractalSampler));
        if(busy)throw new InvalidOperationException("Sampler operation already active.");busy=true;
    }
    public ReadOnlyCollection<Result> Query(ReadOnlySpan<Request> requests,int queryPrecision,int byteBudget,int workBudget,CancellationToken cancellation=default)
    {
        Enter();try
        {
            Precision(queryPrecision);if(requests.Length>MaximumRequests)throw new ArgumentOutOfRangeException(nameof(requests));
            Cap(byteBudget,checked(cache.Length*16+requests.Length*(160+fields.SignedOctaveCount*80)),2097152);
            Cap(workBudget,checked(cache.Length+requests.Length*(fields.SignedOctaveCount*512+64)),16777216);
            cancellation.ThrowIfCancellationRequested();var owned=requests.ToArray();var candidate=(CacheCell[])cache.Clone();var result=new Result[owned.Length];
            for(int i=0;i<owned.Length;i++)
            {
                cancellation.ThrowIfCancellationRequested();var request=owned[i];
                Need(request.Column>=0&&request.Column<Width&&request.Row>=0&&request.Row<Rows,"Exact caller cell index outside fixed shape.");
                Need(Finite(request.XBits)&&Finite(request.ZBits),"Finite F32 coordinates required.");
                int index=request.Row*Width+request.Column;var cell=candidate[index];
                if(Hit(cell,request,queryPrecision))result[i]=new(TerrainConditionalFractalOrigin.CacheReloadF32,TerrainSuppliedFractalSample.FromCacheF32(cell.ValueBits));
                else
                {
                    var sample=Miss(request,queryPrecision,cancellation);
                    candidate[index]=new(true,request.XBits,request.ZBits,sample.ProjectionBits);
                    result[i]=new(TerrainConditionalFractalOrigin.ConditionalComputedMiss,sample);
                }
            }
            cancellation.ThrowIfCancellationRequested();var published=Array.AsReadOnly(result);cache=candidate;return published;
        }finally{busy=false;}
    }
    private static bool Hit(CacheCell cell,Request request,int p)
    {
        if(!cell.Occupied)return false;Number epsilon=N(0x3727c5ac),x=N(request.XBits),z=N(request.ZBits),cx=N(cell.XBits),cz=N(cell.ZBits);
        return Number.Compare(Number.Subtract(cx,epsilon,p),x)<=0&&Number.Compare(x,Number.Add(cx,epsilon,p))<=0
            &&Number.Compare(Number.Subtract(cz,epsilon,p),z)<=0&&Number.Compare(z,Number.Add(cz,epsilon,p))<=0;
    }
    private TerrainSuppliedFractalSample Miss(Request r,int p,CancellationToken cancellation)
    {
        Number Mul(Number a,Number b)=>Number.Multiply(a,b,p);Number Add(Number a,Number b)=>Number.Add(a,b,p);
        uint scaledX=Store(Mul(N(r.XBits),N(fields.FirstCoordinateScaleBits))),scaledZ=Store(Mul(N(r.ZBits),N(fields.SecondCoordinateScaleBits)));
        uint shiftedZ=Store(Add(N(scaledZ),N(fields.SecondOffsetBits))),shiftedX=Store(Add(N(scaledX),N(fields.FirstOffsetBits)));
        uint frequency=0x3f800000,amplitude=0x3f800000,sum=0;
        for(int i=0;i<fields.SignedOctaveCount;i++)
        {
            cancellation.ThrowIfCancellationRequested();uint z=Store(Mul(N(shiftedZ),N(frequency))),x=Store(Mul(N(shiftedX),N(frequency)));
            var noise=kernel!.Query([new(x,z)],p,40,256,false,cancellation)[0];
            var retained=TerrainSuppliedFractalSample.FromWideMagnitude(noise.Magnitude,noise.Negative,noise.Exponent);
            Number contribution=ModeContribution(retained.Value,fields.ModeBits,p);
            sum=Store(Add(Mul(contribution,N(amplitude)),N(sum)));
            frequency=Store(Mul(N(frequency),N(fields.FrequencyMultiplierBits)));
            amplitude=Store(Mul(N(amplitude),N(fields.AmplitudeMultiplierBits)));
        }
        Number value=Mul(N(sum),N(NormalizationBits));
        if(fields.ModeBits<=1)value=Mul(Add(value,I(1)),N(0x3f000000));
        BigInteger magnitude=BigInteger.Abs(value.Mantissa);int exponent=value.Exponent;
        if(magnitude.IsZero)exponent=0;else while(magnitude.IsEven){magnitude>>=1;exponent++;}
        Need(magnitude<=ulong.MaxValue,"Retained result exceeds unsigned64.");
        return TerrainSuppliedFractalSample.FromWideMagnitude((ulong)magnitude,value.Negative,exponent);
    }
    private static Number ModeContribution(Number value,uint mode,int precision)
    {
        Number absolute=new(BigInteger.Abs(value.Mantissa),value.Exponent);
        Number clamped=value;
        if(mode is 4 or 5)
        {
            // Strict comparisons preserve negative zero at the lower endpoint.
            if(Number.Compare(value,I(0))<0)clamped=I(0);
            else if(Number.Compare(value,I(1))>0)clamped=I(1);
        }
        return mode switch
        {
            0 or 1=>value,
            2=>Number.Subtract(I(1),absolute,precision),
            3=>absolute,
            4=>Number.Subtract(I(1),clamped,precision),
            5=>clamped,
            _=>throw new InvalidDataException("Unsupported mode.")
        };
    }
    public ReadOnlyCollection<CacheCell> CopyCache(int byteBudget,CancellationToken cancellation=default)
    {
        Enter();try{Cap(byteBudget,cache.Length*16,16384);cancellation.ThrowIfCancellationRequested();var copy=(CacheCell[])cache.Clone();cancellation.ThrowIfCancellationRequested();return Array.AsReadOnly(copy);}finally{busy=false;}
    }
    public void Clear(CancellationToken cancellation=default)
    {Enter();try{cancellation.ThrowIfCancellationRequested();Array.Clear(cache);}finally{busy=false;}}
    public void Dispose()
    {
        if(Environment.CurrentManagedThreadId!=owner)throw new InvalidOperationException("Sampler belongs to its creating thread.");
        if(busy)throw new InvalidOperationException("Cannot dispose active operation.");
        kernel?.Dispose();kernel=null;cache=Array.Empty<CacheCell>();
    }
}
