using System.Buffers.Binary;

namespace Stardust.Core.Assets;

public sealed record TerrainBoundaryPolylineInputLimits(int MaximumInputBytes=262144,
    int MaximumNodes=64,int MaximumDepth=8,int MaximumPoints=8192,int MaximumStringBytes=4096,
    int MaximumTotalStringBytes=8192,int MaximumDecodedCopyBytes=262144);

public readonly record struct TerrainBoundaryPolylinePoint(uint FirstBits,uint SecondBits)
{
    public float First=>BitConverter.UInt32BitsToSingle(FirstBits);
    public float Second=>BitConverter.UInt32BitsToSingle(SecondBits);
    public bool IsFinite=>(FirstBits&0x7F800000)!=0x7F800000&&(SecondBits&0x7F800000)!=0x7F800000;
}

/// <summary>Raw serialized metadata; count is absent in version 0000. Width labels the bounds-padding scalar without assigning units.</summary>
public sealed record TerrainBoundaryPolylineMetadata(uint? SerializedPointCountBits,
    uint FeatheringTypeBits,uint FeatheringAmountBits,uint WidthBits)
{
    public bool HasFiniteSerializedFeatheringAmount=>(FeatheringAmountBits&0x7F800000)!=0x7F800000;
    public bool HasFiniteWidth=>(WidthBits&0x7F800000)!=0x7F800000;
}

/// <summary>Owned ordered BPLN serialized points; no cache, winding or influence evaluation.</summary>
public sealed class TerrainBoundaryPolylineInput
{
    private readonly byte[] source;
    private readonly TerrainBoundaryPolylinePoint[] points;
    private readonly uint[] metadataWords;
    private TerrainBoundaryPolylineInput(byte[] source,string version,TerrainRuleHeader? header,
        TerrainBoundaryPolylineMetadata? metadata,TerrainBoundaryPolylinePoint[] points,uint[] words,
        int strings,int copies,bool? finite)
    {
        this.source=source;Version=version;Header=header;Metadata=metadata;this.points=points;
        Points=Array.AsReadOnly(points);metadataWords=words;
        DecodedStringBytes=strings;DecodedCopyBytes=copies;HasFinitePoints=finite;
    }
    public string Version { get; }
    public bool ClientVersionRecognized=>Version is "0000" or "0001";
    public bool HasDecodedFields=>Metadata is not null;
    public bool? HasFinitePoints { get; }
    public int DecodedStringBytes { get; }
    /// <summary>Logical bytes in decoded point/metadata/header/name copies, excluding owned input and fixed objects.</summary>
    public int DecodedCopyBytes { get; }
    public TerrainRuleHeader? Header { get; }
    public TerrainBoundaryPolylineMetadata? Metadata { get; }
    public IReadOnlyList<TerrainBoundaryPolylinePoint> Points { get; }
    public byte[] CopyEncodedBytes()=>(byte[])source.Clone();
    public TerrainBoundaryPolylinePoint[] CopyPoints()=>(TerrainBoundaryPolylinePoint[])points.Clone();
    /// <summary>Non-point DATA words in serialized order, including the explicit version-0001 count.</summary>
    public uint[] CopyMetadataWordBits()=>(uint[])metadataWords.Clone();
    public static TerrainBoundaryPolylineInput Decode(TerrainGeneratorSection section,TerrainBoundaryPolylineInputLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(section);limits??=new();Validate(limits);
        Need(section.EncodedBytes<=limits.MaximumInputBytes,"BPLN byte cap before section copy.");return Decode(section.CopyEncodedBytes(),limits);
    }
    public static TerrainBoundaryPolylineInput Decode(ReadOnlySpan<byte> encoded,TerrainBoundaryPolylineInputLimits? limits=null)
    {
        limits??=new();Validate(limits);Need(encoded.Length<=limits.MaximumInputBytes,"BPLN byte cap before snapshot.");
        byte[] owned=encoded.ToArray();var root=IffDocument.Parse(owned,new(limits.MaximumInputBytes,limits.MaximumNodes,limits.MaximumDepth)).Root;
        Need(root.IsForm("BPLN")&&root.Children.Count==1&&root.Children[0].FormType is not null,"Exact BPLN/version envelope.");
        var version=root.Children[0];string tag=version.FormType!;
        TerrainBoundaryPolylineInput Opaque()=>new(owned,tag,null,null,Array.Empty<TerrainBoundaryPolylinePoint>(),Array.Empty<uint>(),0,0,null);
        if(tag is not ("0000" or "0001"))return Opaque();
        Need(version.Children.Count==2&&version.Children[0].IsForm("IHDR")&&Leaf(version.Children[1],"DATA"),"Known BPLN IHDR/DATA order.");
        var ihdr=version.Children[0];Need(ihdr.Children.Count==1&&ihdr.Children[0].FormType is not null,"IHDR version envelope.");
        var hv=ihdr.Children[0];if(hv.FormType is not ("0000" or "0001"))return Opaque();
        Need(hv.Children.Count==1&&Leaf(hv.Children[0],"DATA"),"Exact known IHDR DATA.");
        var raw=hv.Children[0].Payload.Span;Need(raw.Length>=5,"Truncated IHDR flag/name.");
        int nameLength=StringLength(raw[4..],limits.MaximumStringBytes),legacy=hv.IsForm("0000")?3:0;
        Need(raw.Length==5+nameLength+legacy,"Exact known IHDR extent.");
        var data=version.Children[1].Payload.Span;int cursor=0;
        uint? countBits=null;int pointStart=0,pointCount=0;
        bool explicitCount=tag=="0001";
        if(explicitCount)
        {
            countBits=Word(data,ref cursor);
            Need(countBits.Value<=int.MaxValue&&countBits.Value<=limits.MaximumPoints,"Nonnegative bounded explicit point count.");
            pointCount=(int)countBits.Value;pointStart=cursor;
            Need((long)pointCount*8<=data.Length-cursor,"Explicit point bytes fit before allocation.");cursor+=pointCount*8;
        }
        uint type=Word(data,ref cursor),amount=Word(data,ref cursor),width=Word(data,ref cursor);
        int wordCount=explicitCount?4:3;
        if(!explicitCount)
        {
            pointStart=cursor;int remaining=data.Length-cursor;Need(remaining%8==0,"Exact inferred point-pair extent.");
            pointCount=remaining/8;Need(pointCount<=limits.MaximumPoints,"Inferred point budget before allocation.");cursor=data.Length;
        }
        Need(cursor==data.Length,"Exact BPLN DATA completion.");
        int strings=nameLength+1;
        Need(strings<=limits.MaximumTotalStringBytes,"Combined decoded string budget.");
        long copies=(long)pointCount*8+wordCount*4+nameLength+legacy;
        Need(copies<=limits.MaximumDecodedCopyBytes,"Aggregate decoded copy budget before typed arrays.");
        var header=new TerrainRuleHeader(hv.FormType!,BinaryPrimitives.ReadUInt32LittleEndian(raw),raw.Slice(4,nameLength).ToArray(),raw[(5+nameLength)..].ToArray());
        var points=new TerrainBoundaryPolylinePoint[pointCount];bool finite=true;
        for(int i=0;i<pointCount;i++)
        { int at=pointStart+i*8;points[i]=new(BinaryPrimitives.ReadUInt32LittleEndian(data[at..]),BinaryPrimitives.ReadUInt32LittleEndian(data[(at+4)..]));finite&=points[i].IsFinite; }
        var metadata=new TerrainBoundaryPolylineMetadata(countBits,type,amount,width);
        uint[] words=explicitCount?new[]{countBits!.Value,type,amount,width}:new[]{type,amount,width};
        return new(owned,tag,header,metadata,points,words,strings,(int)copies,finite);
    }
    private static uint Word(ReadOnlySpan<byte> raw,ref int cursor)
    { Need(raw.Length-cursor>=4,"Truncated BPLN word.");uint value=BinaryPrimitives.ReadUInt32LittleEndian(raw[cursor..]);cursor+=4;return value; }
    private static int StringLength(ReadOnlySpan<byte> raw,int cap)
    { int length=raw.IndexOf((byte)0);Need(length>=0&&length<=cap,"Bounded NUL byte string.");return length; }
    private static bool Leaf(IffNode node,string tag)=>node.Tag==tag&&node.FormType is null;
    private static void Validate(TerrainBoundaryPolylineInputLimits limits)
    {
        if(limits.MaximumInputBytes is <24 or >8388608||limits.MaximumNodes is <1 or >65536||limits.MaximumDepth is <1 or >64||
            limits.MaximumPoints is <0 or >262144||limits.MaximumStringBytes is <0 or >65536||limits.MaximumTotalStringBytes is <0 or >131072||
            limits.MaximumDecodedCopyBytes is <0 or >8388608)throw new ArgumentOutOfRangeException(nameof(limits));
    }
    private static void Need(bool value,string reason) { if(!value)throw new InvalidDataException(reason); }
}
