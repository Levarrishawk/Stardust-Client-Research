using System.Buffers.Binary;

namespace Stardust.Core.Assets;

public sealed record TerrainBoundaryPolygonInputLimits(int MaximumInputBytes=262144,
    int MaximumNodes=64,int MaximumDepth=8,int MaximumPoints=8192,int MaximumStringBytes=4096,
    int MaximumTotalStringBytes=8192,int MaximumDecodedCopyBytes=262144);

public readonly record struct TerrainBoundaryPolygonPoint(uint FirstBits,uint SecondBits)
{
    public float First=>BitConverter.UInt32BitsToSingle(FirstBits);
    public float Second=>BitConverter.UInt32BitsToSingle(SecondBits);
    public bool IsFinite=>(FirstBits&0x7F800000)!=0x7F800000&&(SecondBits&0x7F800000)!=0x7F800000;
}

/// <summary>Only serialized metadata; null distinguishes fields absent in this version.</summary>
public sealed record TerrainBoundaryPolygonMetadata(uint? LegacyPrefixBits,uint? SerializedPointCountBits,
    uint? FeatheringTypeBits,uint? FeatheringAmountBits,uint? ExtensionFlagBits,
    uint? ExtensionScalarFirstBits,uint? ExtensionScalarSecondBits,uint? LegacyBeforeNameBits)
{
    public bool? HasFiniteSerializedFeatheringAmount=>FeatheringAmountBits is uint bits?(bits&0x7F800000)!=0x7F800000:null;
    public bool? IsExtensionFlagSet=>ExtensionFlagBits is uint bits?bits!=0:null;
}

/// <summary>Owned ordered BPOL serialized points; no cache, winding or influence evaluation.</summary>
public sealed class TerrainBoundaryPolygonInput
{
    private readonly byte[] source,extensionName;
    private readonly TerrainBoundaryPolygonPoint[] points;
    private readonly uint[] metadataWords;
    private TerrainBoundaryPolygonInput(byte[] source,string version,TerrainRuleHeader? header,
        TerrainBoundaryPolygonMetadata? metadata,TerrainBoundaryPolygonPoint[] points,uint[] words,
        byte[] extensionName,int strings,int copies,bool? finite)
    {
        this.source=source;Version=version;Header=header;Metadata=metadata;this.points=points;
        Points=Array.AsReadOnly(points);metadataWords=words;this.extensionName=extensionName;
        DecodedStringBytes=strings;DecodedCopyBytes=copies;HasFinitePoints=finite;
    }
    public string Version { get; }
    public bool ClientVersionRecognized=>Version is "0000" or "0001" or "0002" or "0003" or "0004" or "0005" or "0006";
    public bool HasDecodedFields=>Metadata is not null;
    public bool HasSerializedExtension=>Metadata?.ExtensionFlagBits is not null;
    public bool? HasFinitePoints { get; }
    public int DecodedStringBytes { get; }
    /// <summary>Logical bytes in decoded point/metadata/header/name copies, excluding owned input and fixed objects.</summary>
    public int DecodedCopyBytes { get; }
    public TerrainRuleHeader? Header { get; }
    public TerrainBoundaryPolygonMetadata? Metadata { get; }
    public IReadOnlyList<TerrainBoundaryPolygonPoint> Points { get; }
    public byte[] CopyEncodedBytes()=>(byte[])source.Clone();
    public TerrainBoundaryPolygonPoint[] CopyPoints()=>(TerrainBoundaryPolygonPoint[])points.Clone();
    /// <summary>Non-point DATA words in serialized order, including discarded/count words.</summary>
    public uint[] CopyMetadataWordBits()=>(uint[])metadataWords.Clone();
    public byte[] CopyExtensionNameBytes()=>(byte[])extensionName.Clone();
    public static TerrainBoundaryPolygonInput Decode(TerrainGeneratorSection section,TerrainBoundaryPolygonInputLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(section);limits??=new();Validate(limits);
        Need(section.EncodedBytes<=limits.MaximumInputBytes,"BPOL byte cap before section copy.");return Decode(section.CopyEncodedBytes(),limits);
    }
    public static TerrainBoundaryPolygonInput Decode(ReadOnlySpan<byte> encoded,TerrainBoundaryPolygonInputLimits? limits=null)
    {
        limits??=new();Validate(limits);Need(encoded.Length<=limits.MaximumInputBytes,"BPOL byte cap before snapshot.");
        byte[] owned=encoded.ToArray();var root=IffDocument.Parse(owned,new(limits.MaximumInputBytes,limits.MaximumNodes,limits.MaximumDepth)).Root;
        Need(root.IsForm("BPOL")&&root.Children.Count==1&&root.Children[0].FormType is not null,"Exact BPOL/version envelope.");
        var version=root.Children[0];string tag=version.FormType!;
        TerrainBoundaryPolygonInput Opaque()=>new(owned,tag,null,null,Array.Empty<TerrainBoundaryPolygonPoint>(),Array.Empty<uint>(),Array.Empty<byte>(),0,0,null);
        if(tag is not ("0000" or "0001" or "0002" or "0003" or "0004" or "0005" or "0006"))return Opaque();
        Need(version.Children.Count==2&&version.Children[0].IsForm("IHDR")&&Leaf(version.Children[1],"DATA"),"Known BPOL IHDR/DATA order.");
        var ihdr=version.Children[0];Need(ihdr.Children.Count==1&&ihdr.Children[0].FormType is not null,"IHDR version envelope.");
        var hv=ihdr.Children[0];if(hv.FormType is not ("0000" or "0001"))return Opaque();
        Need(hv.Children.Count==1&&Leaf(hv.Children[0],"DATA"),"Exact known IHDR DATA.");
        var raw=hv.Children[0].Payload.Span;Need(raw.Length>=5,"Truncated IHDR flag/name.");
        int nameLength=StringLength(raw[4..],limits.MaximumStringBytes),legacy=hv.IsForm("0000")?3:0;
        Need(raw.Length==5+nameLength+legacy,"Exact known IHDR extent.");
        var data=version.Children[1].Payload.Span;int cursor=0;
        uint? prefix=null,countBits=null,type=null,amount=null,flag=null,scalarFirst=null,scalarSecond=null,beforeName=null;
        int extensionStart=0,extensionLength=0,pointStart,pointCount,wordCount=0;
        bool extension=tag is "0003" or "0004" or "0005" or "0006";
        bool explicitCount=tag is "0005" or "0006";
        if(explicitCount)
        {
            countBits=Word(data,ref cursor);wordCount++;
            Need(countBits.Value<=int.MaxValue&&countBits.Value<=limits.MaximumPoints,"Nonnegative bounded explicit point count.");
            pointCount=(int)countBits.Value;pointStart=cursor;
            Need((long)pointCount*8<=data.Length-cursor,"Explicit point bytes fit before allocation.");cursor+=pointCount*8;
        }
        else
        {
            if(tag=="0001") { prefix=Word(data,ref cursor);wordCount++; }
            pointStart=pointCount=0;
        }
        if(tag is "0002" or "0003" or "0004" or "0005" or "0006")
        { type=Word(data,ref cursor);amount=Word(data,ref cursor);wordCount+=2; }
        if(extension)
        {
            flag=Word(data,ref cursor);scalarFirst=Word(data,ref cursor);wordCount+=2;
            if(tag!="0003") { scalarSecond=Word(data,ref cursor);wordCount++; }
            if(tag=="0006") { beforeName=Word(data,ref cursor);wordCount++; }
            extensionStart=cursor;extensionLength=StringLength(data[cursor..],limits.MaximumStringBytes);cursor+=extensionLength+1;
        }
        if(!explicitCount)
        {
            pointStart=cursor;int remaining=data.Length-cursor;Need(remaining%8==0,"Exact inferred point-pair extent.");
            pointCount=remaining/8;Need(pointCount<=limits.MaximumPoints,"Inferred point budget before allocation.");cursor=data.Length;
        }
        Need(cursor==data.Length,"Exact BPOL DATA completion.");
        int strings=nameLength+1+(extension?extensionLength+1:0);
        Need(strings<=limits.MaximumTotalStringBytes,"Combined decoded string budget.");
        long copies=(long)pointCount*8+wordCount*4+nameLength+legacy+extensionLength;
        Need(copies<=limits.MaximumDecodedCopyBytes,"Aggregate decoded copy budget before typed arrays.");
        var header=new TerrainRuleHeader(hv.FormType!,BinaryPrimitives.ReadUInt32LittleEndian(raw),raw.Slice(4,nameLength).ToArray(),raw[(5+nameLength)..].ToArray());
        var points=new TerrainBoundaryPolygonPoint[pointCount];bool finite=true;
        for(int i=0;i<pointCount;i++)
        { int at=pointStart+i*8;points[i]=new(BinaryPrimitives.ReadUInt32LittleEndian(data[at..]),BinaryPrimitives.ReadUInt32LittleEndian(data[(at+4)..]));finite&=points[i].IsFinite; }
        var metadata=new TerrainBoundaryPolygonMetadata(prefix,countBits,type,amount,flag,scalarFirst,scalarSecond,beforeName);
        uint[] words=new uint[wordCount];int next=0;
        foreach(uint? value in new uint?[]{prefix,countBits,type,amount,flag,scalarFirst,scalarSecond,beforeName})if(value is uint bits)words[next++]=bits;
        return new(owned,tag,header,metadata,points,words,extension?data.Slice(extensionStart,extensionLength).ToArray():Array.Empty<byte>(),strings,(int)copies,finite);
    }
    private static uint Word(ReadOnlySpan<byte> raw,ref int cursor)
    { Need(raw.Length-cursor>=4,"Truncated BPOL word.");uint value=BinaryPrimitives.ReadUInt32LittleEndian(raw[cursor..]);cursor+=4;return value; }
    private static int StringLength(ReadOnlySpan<byte> raw,int cap)
    { int length=raw.IndexOf((byte)0);Need(length>=0&&length<=cap,"Bounded NUL byte string.");return length; }
    private static bool Leaf(IffNode node,string tag)=>node.Tag==tag&&node.FormType is null;
    private static void Validate(TerrainBoundaryPolygonInputLimits limits)
    {
        if(limits.MaximumInputBytes is <24 or >8388608||limits.MaximumNodes is <1 or >65536||limits.MaximumDepth is <1 or >64||
            limits.MaximumPoints is <0 or >262144||limits.MaximumStringBytes is <0 or >65536||limits.MaximumTotalStringBytes is <0 or >131072||
            limits.MaximumDecodedCopyBytes is <0 or >8388608)throw new ArgumentOutOfRangeException(nameof(limits));
    }
    private static void Need(bool value,string reason) { if(!value)throw new InvalidDataException(reason); }
}
