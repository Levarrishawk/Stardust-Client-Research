using Number=Stardust.Core.Geometry.TerrainCircleArithmetic.Number;

namespace Stardust.Core.Geometry;

public readonly record struct TerrainSampleCoordinateRequest(uint ChunkWidthBits,int Tiles,int Margin,
    int First,int Second,int Scale);
public readonly record struct TerrainSampleCoordinatePolicies(TerrainHeightArithmeticPrecision MetadataLoading,
    TerrainHeightArithmeticPrecision ChunkBuilding,TerrainHeightArithmeticPrecision LayerCoordinates,
    TerrainHeightArithmeticPrecision PositionCoordinates);
public sealed record TerrainSampleCoordinateLimits(int MaximumCount=256,int MaximumPairs=65536);
public sealed record TerrainSampleCoordinateBatchLimits(int MaximumRequests=64,int MaximumTotalPairs=1048576);
public readonly record struct TerrainSampleCoordinateParameters(int Count,uint DerivedSpacingBits,uint ScaleStoredBits,
    uint StepBits,uint MarginBits,uint FirstOriginBits,uint VerticalOriginBits,uint SecondOriginBits);
public enum TerrainSampleCoordinatePhase { Layer,Position }
public readonly record struct TerrainSampleCoordinatePair(int Column,int Row,uint FirstBits,uint SecondBits);

public sealed class TerrainSampleCoordinateAxes
{
    private readonly uint[] first,second;
    internal TerrainSampleCoordinateAxes(uint[] first,uint[] second,TerrainSampleCoordinatePhase phase)
    {
        this.first=first;this.second=second;Phase=phase;FirstAxisBits=Array.AsReadOnly(first);SecondAxisBits=Array.AsReadOnly(second);
        FirstStrictlyIncreasing=Increasing(first);SecondStrictlyIncreasing=Increasing(second);
    }
    public TerrainSampleCoordinatePhase Phase { get; }
    public int Count=>first.Length;
    public IReadOnlyList<uint> FirstAxisBits { get; }
    public IReadOnlyList<uint> SecondAxisBits { get; }
    public bool FirstStrictlyIncreasing { get; }
    public bool SecondStrictlyIncreasing { get; }
    public bool HasStrictlyIncreasingDisplayAxes=>FirstStrictlyIncreasing&&SecondStrictlyIncreasing;
    public uint[] CopyFirstAxisBits()=>(uint[])first.Clone();
    public uint[] CopySecondAxisBits()=>(uint[])second.Clone();
    public TerrainSampleCoordinatePair GetPair(int column,int row)
    {
        if((uint)column>=Count || (uint)row>=Count)throw new ArgumentOutOfRangeException(nameof(column));
        return new(column,row,first[column],second[row]);
    }
    public TerrainSampleCoordinatePair[] CopyPairs()
    {
        var result=new TerrainSampleCoordinatePair[checked(Count*Count)];
        for(int row=0;row<Count;row++)for(int column=0;column<Count;column++)result[row*Count+column]=GetPair(column,row);
        return result;
    }
    private static bool Increasing(uint[] bits)
    {
        for(int i=1;i<bits.Length;i++)if(!(BitConverter.UInt32BitsToSingle(bits[i])>BitConverter.UInt32BitsToSingle(bits[i-1])))return false;
        return true;
    }
}

public sealed class TerrainSampleCoordinateBatch
{
    internal TerrainSampleCoordinateBatch(TerrainSampleCoordinateGrid[] grids,int pairs)
    { Grids=Array.AsReadOnly(grids);TotalPairs=pairs; }
    public IReadOnlyList<TerrainSampleCoordinateGrid> Grids { get; }
    public int TotalPairs { get; }
}

/// <summary>Bounded conditional native coordinate graph; active original phase profiles remain unknown.</summary>
public sealed class TerrainSampleCoordinateGrid
{
    private TerrainSampleCoordinateGrid(TerrainSampleCoordinateRequest request,TerrainSampleCoordinatePolicies policies,
        TerrainSampleCoordinateParameters parameters,TerrainSampleCoordinateAxes layer,TerrainSampleCoordinateAxes position)
    { Request=request;Policies=policies;Parameters=parameters;Layer=layer;Position=position; }
    public TerrainSampleCoordinateRequest Request { get; }
    public TerrainSampleCoordinatePolicies Policies { get; }
    public TerrainSampleCoordinateParameters Parameters { get; }
    public TerrainSampleCoordinateAxes Layer { get; }
    public TerrainSampleCoordinateAxes Position { get; }
    public static TerrainSampleCoordinateGrid Build(TerrainSampleCoordinateRequest request,
        TerrainHeightArithmeticPrecision metadataLoading,TerrainHeightArithmeticPrecision chunkBuilding,
        TerrainHeightArithmeticPrecision layerCoordinates,TerrainHeightArithmeticPrecision positionCoordinates,
        TerrainSampleCoordinateLimits? limits=null)
    {
        var policies=new TerrainSampleCoordinatePolicies(metadataLoading,chunkBuilding,layerCoordinates,positionCoordinates);
        ValidatePolicies(policies);limits??=new();ValidateLimits(limits);int count=ValidateRequest(request,limits);
        return Compute(request,policies,count);
    }
    public static TerrainSampleCoordinateBatch BuildBatch(ReadOnlySpan<TerrainSampleCoordinateRequest> requests,
        TerrainHeightArithmeticPrecision metadataLoading,TerrainHeightArithmeticPrecision chunkBuilding,
        TerrainHeightArithmeticPrecision layerCoordinates,TerrainHeightArithmeticPrecision positionCoordinates,
        TerrainSampleCoordinateLimits? limits=null,TerrainSampleCoordinateBatchLimits? batchLimits=null)
    {
        var policies=new TerrainSampleCoordinatePolicies(metadataLoading,chunkBuilding,layerCoordinates,positionCoordinates);
        ValidatePolicies(policies);limits??=new();ValidateLimits(limits);batchLimits??=new();
        if(batchLimits.MaximumRequests is <0 or >4096 || batchLimits.MaximumTotalPairs is <0 or >16777216)
            throw new ArgumentOutOfRangeException(nameof(batchLimits));
        Need(requests.Length<=batchLimits.MaximumRequests,"Request cap.");
        int total=0;
        foreach(var request in requests)
        {
            int n=ValidateRequest(request,limits);int pairs=n*n;
            Need(pairs<=batchLimits.MaximumTotalPairs-total,"Aggregate pair cap.");total+=pairs;
        }
        var snapshot=requests.ToArray();var grids=new TerrainSampleCoordinateGrid[snapshot.Length];
        for(int i=0;i<snapshot.Length;i++)grids[i]=Compute(snapshot[i],policies,ValidateRequest(snapshot[i],limits));
        return new(grids,total);
    }
    private static TerrainSampleCoordinateGrid Compute(TerrainSampleCoordinateRequest request,TerrainSampleCoordinatePolicies policies,int count)
    {
        var width=Number.FromBits(request.ChunkWidthBits);int loading=(int)policies.MetadataLoading,building=(int)policies.ChunkBuilding;
        uint derived=Store(Number.Divide(width,Integer(request.Tiles),loading));
        uint scaleStored=Store(Integer(request.Scale)); // FST inspection slot; exact integer stays on x87 stack.
        uint step=Store(Number.Multiply(Number.Multiply(Integer(request.Scale),Number.FromBits(derived),building),Number.FromBits(0x3f000000),building));
        uint margin=Store(Number.Multiply(Integer(request.Margin),Number.FromBits(step),building));
        uint Origin(int index)=>Store(Number.Subtract(Number.Multiply(Integer(index),width,building),Number.FromBits(margin),building));
        uint first=Origin(request.First),second=Origin(request.Second);
        var parameters=new TerrainSampleCoordinateParameters(count,derived,scaleStored,step,margin,first,0,second);
        TerrainSampleCoordinateAxes Axes(TerrainSampleCoordinatePhase phase,int precision)
        {
            var x=new uint[count];var z=new uint[count];var spacing=Number.FromBits(step);var xo=Number.FromBits(first);var zo=Number.FromBits(second);
            for(int i=0;i<count;i++)
            {
                var integer=Integer(i);var row=phase==TerrainSampleCoordinatePhase.Layer?Number.FromBits(Store(integer)):integer;
                x[i]=Store(Number.Add(Number.Multiply(integer,spacing,precision),xo,precision));
                z[i]=Store(Number.Add(Number.Multiply(row,spacing,precision),zo,precision));
            }
            return new(x,z,phase);
        }
        var layer=Axes(TerrainSampleCoordinatePhase.Layer,(int)policies.LayerCoordinates);
        var position=Axes(TerrainSampleCoordinatePhase.Position,(int)policies.PositionCoordinates);
        return new(request,policies,parameters,layer,position);
    }
    private static Number Integer(int value)=>new(value,0);
    private static uint Store(Number value)
    { uint bits=value.ToFloatBits();Need((bits&0x7f800000)!=0x7f800000,"Nonfinite stored coordinate parameter or axis.");return bits; }
    private static void ValidatePolicies(TerrainSampleCoordinatePolicies policies)
    {
        foreach(var p in new[]{policies.MetadataLoading,policies.ChunkBuilding,policies.LayerCoordinates,policies.PositionCoordinates})
            if(p is not (TerrainHeightArithmeticPrecision.Binary24 or TerrainHeightArithmeticPrecision.Binary53 or TerrainHeightArithmeticPrecision.Binary64))throw new ArgumentOutOfRangeException(nameof(policies));
    }
    private static void ValidateLimits(TerrainSampleCoordinateLimits limits)
    { if(limits.MaximumCount is <2 or >1024 || limits.MaximumPairs is <4 or >1048576)throw new ArgumentOutOfRangeException(nameof(limits)); }
    private static int ValidateRequest(TerrainSampleCoordinateRequest request,TerrainSampleCoordinateLimits limits)
    {
        Need((request.ChunkWidthBits&0x80000000)==0 && (request.ChunkWidthBits&0x7fffffff)!=0 && (request.ChunkWidthBits&0x7f800000)!=0x7f800000,
            "Positive finite chunk width.");
        Need(request.Tiles>0 && request.Scale>0,"Positive signed tile/scale inputs.");
        int count;
        try{count=checked(2*checked(request.Tiles+request.Margin));}catch(OverflowException e){throw new InvalidDataException("Checked sample count overflow.",e);}
        Need(count>0 && count<=limits.MaximumCount && (long)count*count<=limits.MaximumPairs,"Positive bounded sample count/pair budget.");
        return count;
    }
    private static void Need(bool condition,string reason){if(!condition)throw new InvalidDataException(reason);}
}
