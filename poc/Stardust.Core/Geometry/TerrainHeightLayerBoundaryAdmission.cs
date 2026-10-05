using Stardust.Core.Assets;
namespace Stardust.Core.Geometry;
public readonly record struct TerrainHeightLayerBoundaryAdmissionControls(TerrainHeightArithmeticPrecision CachePrecision,
    TerrainHeightArithmeticPrecision QueryPrecision,TerrainHeightArithmeticPrecision CombinePrecision);
public sealed record TerrainHeightLayerBoundaryAdmissionLimits(int MaximumAxisCount=256,int MaximumCells=65536,
    int MaximumQueries=65536,long MaximumArrayBytes=16777216,long MaximumCopyBytes=1572864,long MaximumWork=4194304);
public sealed record TerrainHeightLayerBoundaryAdmissionCell(TerrainSampleCoordinatePair Coordinates,TerrainCircleQuery CircleQuery,
    TerrainLayerInfluenceQuery Carrier,TerrainLayerInfluenceReceipt BoundaryProbe)
{
    public uint BoundaryBits=>BoundaryProbe.LocalInfluenceBits;
    public bool Admitted=>BoundaryProbe.BoundaryPositive;
}
/// <summary>One complete Layer-phase grid's first boundary gate. No filter/height sampling or ancestor admission.</summary>
public sealed class TerrainHeightLayerBoundaryAdmission
{
    private TerrainHeightLayerBoundaryAdmission(TerrainGeneratorHeightLayerSlice slice,TerrainSampleCoordinateGrid grid,
        TerrainHeightLayerBoundaryAdmissionControls controls,TerrainHeightLayerBoundaryAdmissionCell[] cells,long arrays,long copies,long work)
    { Slice=slice;Grid=grid;Controls=controls;Cells=Array.AsReadOnly(cells);ArrayBytes=arrays;CopiedEntryBytes=copies;Work=work; }
    public TerrainGeneratorHeightLayerSlice Slice { get; }
    public TerrainSampleCoordinateGrid Grid { get; }
    public TerrainHeightLayerBoundaryAdmissionControls Controls { get; }
    public TerrainGeneratorHeightLayerChild CircleSource=>Slice.Children[0];
    public IReadOnlyList<TerrainHeightLayerBoundaryAdmissionCell> Cells { get; }
    public int AxisCount=>Grid.Layer.Count;
    public int QueryCount=>Cells.Count;
    public int CarrierCount=>Cells.Count;
    public int ReceiptCount=>Cells.Count;
    public long ArrayBytes { get; }
    public long CopiedEntryBytes { get; }
    public long CopiedInputBytes=>0;
    public long Work { get; }
    public uint[] CopyBoundaryBits()=>Cells.Select(c=>c.BoundaryBits).ToArray();
    public bool[] CopyAdmission()=>Cells.Select(c=>c.Admitted).ToArray();
    public static TerrainHeightLayerBoundaryAdmission Evaluate(TerrainGeneratorHeightLayerSlice slice,TerrainSampleCoordinateGrid grid,
        TerrainHeightLayerBoundaryAdmissionControls controls,TerrainHeightLayerBoundaryAdmissionLimits? limits=null,CancellationToken token=default)
    {
        ArgumentNullException.ThrowIfNull(slice);ArgumentNullException.ThrowIfNull(grid);limits??=new();
        if(limits.MaximumAxisCount is <0 or >1024||limits.MaximumCells is <0 or >1048576
            ||limits.MaximumQueries is <0 or >1048576||limits.MaximumArrayBytes is <0 or >268435456
            ||limits.MaximumCopyBytes is <0 or >25165824||limits.MaximumWork is <0 or >67108864)throw new ArgumentOutOfRangeException(nameof(limits));
        TerrainLayerInfluence.ValidatePrecision(controls.CachePrecision);TerrainLayerInfluence.ValidatePrecision(controls.QueryPrecision);
        TerrainLayerInfluence.ValidatePrecision(controls.CombinePrecision);token.ThrowIfCancellationRequested();
        Need(slice.Policy==TerrainGeneratorHeightLayerSlicePolicy.TypedEvidence&&slice.IsLocallyEnabled,"Locally enabled evidence slice required.");
        Need(slice.Children.Count==3&&slice.Circle.Version=="0002"&&slice.Circle.HasDecodedFields
            &&slice.Circle.Header is not null&&slice.Circle.Header.IsEnabled&&slice.Circle.Fields is not null
            &&slice.Circle.Fields.HasFiniteGeometry&&slice.Circle.Fields.HasFiniteSerializedFeatheringAmount==true,"Enabled finite typed BCIR required.");
        var source=slice.Children[0];
        Need(source.FormType=="BCIR"&&source.Version=="0002"&&source.ChildOrdinal==2&&source.GlobalRuleIndex>=0
            &&source.Offset>=slice.LayerOffset&&source.EncodedBytes>=24
            &&(long)source.Offset+source.EncodedBytes<=(long)slice.LayerOffset+slice.LayerBytes
            &&source.Sha256.Length==64,"Exact accepted circle provenance required.");
        int count=grid.Parameters.Count;
        Need(grid.Layer.Phase==TerrainSampleCoordinatePhase.Layer&&count>0&&count==grid.Layer.Count
            &&grid.Layer.FirstAxisBits.Count==count&&grid.Layer.SecondAxisBits.Count==count
            &&count==grid.Position.Count&&count<=limits.MaximumAxisCount,"Complete immutable Layer-phase grid required.");
        long cells=(long)count*count,arrays=256*cells,copies=24*cells,work=64*cells;
        Need(cells<=limits.MaximumCells&&cells<=limits.MaximumQueries&&arrays<=limits.MaximumArrayBytes
            &&copies<=limits.MaximumCopyBytes&&work<=limits.MaximumWork,"Full shape/array/query/copy/work preflight.");
        token.ThrowIfCancellationRequested();var owned=new TerrainHeightLayerBoundaryAdmissionCell[(int)cells];
        for(int row=0;row<count;row++)for(int column=0;column<count;column++)
        {
            token.ThrowIfCancellationRequested();var pair=grid.Layer.GetPair(column,row);
            var query=TerrainBoundaryCircleInfluence.Query(slice.Circle,pair.FirstBits,pair.SecondBits,controls.CachePrecision,controls.QueryPrecision);
            var carrier=TerrainLayerInfluenceQuery.FromCircle(query);
            var sample=TerrainLayerInfluenceSample.Create(0x3f800000,[new(true,slice.Circle.Fields!.FeatheringTypeBits!.Value,carrier)],[],
                1,0,slice.BoundaryInverted,false,controls.CombinePrecision,controls.CombinePrecision,1);
            var receipt=TerrainLayerInfluence.Evaluate(sample,new(1,1,1));
            Need(ReferenceEquals(carrier.Circle,query)&&ReferenceEquals(receipt.Sample.Boundaries[0].Query,carrier)
                &&ReferenceEquals(query.Fields,slice.Circle.Fields)&&receipt.FilterVisits.Count==0,"Actual retained query/carrier/probe identity.");
            token.ThrowIfCancellationRequested();owned[row*count+column]=new(pair,query,carrier,receipt);
        }
        token.ThrowIfCancellationRequested();return new(slice,grid,controls,owned,arrays,copies,work);
    }
    private static void Need(bool value,string reason){if(!value)throw new InvalidDataException(reason);}
}
