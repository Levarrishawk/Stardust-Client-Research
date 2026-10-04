using System.Buffers.Binary;

namespace Stardust.Core.Assets;

public sealed record TerrainBoundaryRectangleInputLimits(int MaximumInputBytes=65536,
    int MaximumNodes=64,int MaximumDepth=8,int MaximumStringBytes=4096,int MaximumTotalStringBytes=8192);

/// <summary>Serialized corners, not normalized runtime minima/maxima; world axes remain unassigned.</summary>
public sealed record TerrainBoundaryRectangleFields(uint CornerFirstABits,uint CornerSecondABits,
    uint CornerFirstBBits,uint CornerSecondBBits,uint? LegacyPrefixBits,uint? FeatheringTypeBits,
    uint? FeatheringAmountBits,uint? ExtensionFlagFirstBits,uint? ExtensionFlagSecondBits,
    uint? ExtensionScalarFirstBits,uint? ExtensionScalarSecondBits)
{
    public float CornerFirstA=>BitConverter.UInt32BitsToSingle(CornerFirstABits);
    public float CornerSecondA=>BitConverter.UInt32BitsToSingle(CornerSecondABits);
    public float CornerFirstB=>BitConverter.UInt32BitsToSingle(CornerFirstBBits);
    public float CornerSecondB=>BitConverter.UInt32BitsToSingle(CornerSecondBBits);
    public bool HasFiniteGeometry=>Finite(CornerFirstABits)&&Finite(CornerSecondABits)&&Finite(CornerFirstBBits)&&Finite(CornerSecondBBits);
    public bool? HasFiniteSerializedFeatheringAmount=>FeatheringAmountBits is uint bits?Finite(bits):null;
    public bool? IsExtensionFlagFirstSet=>ExtensionFlagFirstBits is uint bits?bits!=0:null;
    public bool? IsExtensionFlagSecondSet=>ExtensionFlagSecondBits is uint bits?bits!=0:null;
    private static bool Finite(uint bits)=>(bits&0x7F800000)!=0x7F800000;
}

/// <summary>Owned BREC serialized input. No runtime normalization, caches or influence evaluation.</summary>
public sealed class TerrainBoundaryRectangleInput
{
    private readonly byte[] source,extensionName;
    private readonly uint[] words;
    private TerrainBoundaryRectangleInput(byte[] source,string version,TerrainRuleHeader? header,
        TerrainBoundaryRectangleFields? fields,uint[] words,byte[] extensionName,int strings)
    { this.source=source;Version=version;Header=header;Fields=fields;this.words=words;this.extensionName=extensionName;DecodedStringBytes=strings; }
    public string Version { get; }
    public bool ClientVersionRecognized=>Version is "0000" or "0001" or "0002" or "0003";
    public bool HasDecodedFields=>Fields is not null;
    public bool HasSerializedExtension=>Fields?.ExtensionFlagFirstBits is not null;
    public int DecodedStringBytes { get; }
    public TerrainRuleHeader? Header { get; }
    public TerrainBoundaryRectangleFields? Fields { get; }
    public byte[] CopyEncodedBytes()=>(byte[])source.Clone();
    public uint[] CopyDataWordBits()=>(uint[])words.Clone();
    /// <summary>Raw0003 trailing byte string, excluding NUL; empty for absent/opaque inputs.</summary>
    public byte[] CopyExtensionNameBytes()=>(byte[])extensionName.Clone();
    public static TerrainBoundaryRectangleInput Decode(TerrainGeneratorSection section,TerrainBoundaryRectangleInputLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(section);limits??=new();Validate(limits);
        Need(section.EncodedBytes<=limits.MaximumInputBytes,"BREC input budget before section copy.");
        return Decode(section.CopyEncodedBytes(),limits);
    }
    public static TerrainBoundaryRectangleInput Decode(ReadOnlySpan<byte> encoded,TerrainBoundaryRectangleInputLimits? limits=null)
    {
        limits??=new();Validate(limits);Need(encoded.Length<=limits.MaximumInputBytes,"BREC input budget before snapshot.");
        byte[] owned=encoded.ToArray();
        var root=IffDocument.Parse(owned,new(limits.MaximumInputBytes,limits.MaximumNodes,limits.MaximumDepth)).Root;
        Need(root.IsForm("BREC")&&root.Children.Count==1&&root.Children[0].FormType is not null,"Exact BREC/version envelope.");
        var version=root.Children[0];string tag=version.FormType!;
        TerrainBoundaryRectangleInput Opaque()=>new(owned,tag,null,null,Array.Empty<uint>(),Array.Empty<byte>(),0);
        if(tag is not ("0000" or "0001" or "0002" or "0003"))return Opaque();
        int count=tag switch { "0000"=>4,"0001"=>5,"0002"=>6,_=>10 };
        Need(version.Children.Count==2&&version.Children[0].IsForm("IHDR")&&Leaf(version.Children[1],"DATA"),"Known BREC requires ordered IHDR/DATA.");
        var data=version.Children[1].Payload.Span;
        Need(tag=="0003"?data.Length>=count*4+1:data.Length==count*4,"Known BREC DATA extent.");
        var ihdr=version.Children[0];Need(ihdr.Children.Count==1&&ihdr.Children[0].FormType is not null,"IHDR version envelope.");
        var hv=ihdr.Children[0];if(hv.FormType is not ("0000" or "0001"))return Opaque();
        Need(hv.Children.Count==1&&Leaf(hv.Children[0],"DATA"),"Exact known IHDR DATA.");
        var raw=hv.Children[0].Payload.Span;Need(raw.Length>=5,"Truncated IHDR flag/name.");
        int nameLength=StringLength(raw[4..],limits.MaximumStringBytes);int legacy=hv.IsForm("0000")?3:0;
        Need(raw.Length==5+nameLength+legacy,"Exact known IHDR extent.");
        int extensionLength=tag=="0003"?StringLength(data[(count*4)..],limits.MaximumStringBytes):0;
        if(tag=="0003")Need(data.Length==count*4+extensionLength+1,"Exact BREC0003 trailing string extent.");
        int strings=nameLength+1+(tag=="0003"?extensionLength+1:0);
        Need(strings<=limits.MaximumTotalStringBytes,"Combined decoded string budget before copies.");
        var header=new TerrainRuleHeader(hv.FormType!,BinaryPrimitives.ReadUInt32LittleEndian(raw),raw.Slice(4,nameLength).ToArray(),raw[(5+nameLength)..].ToArray());
        uint[] words=new uint[count];for(int i=0;i<count;i++)words[i]=BinaryPrimitives.ReadUInt32LittleEndian(data[(i*4)..]);
        int start=tag=="0001"?1:0;bool feather=tag is "0002" or "0003",extension=tag=="0003";
        var fields=new TerrainBoundaryRectangleFields(words[start],words[start+1],words[start+2],words[start+3],
            tag=="0001"?words[0]:null,feather?words[4]:null,feather?words[5]:null,
            extension?words[6]:null,extension?words[7]:null,extension?words[8]:null,extension?words[9]:null);
        return new(owned,tag,header,fields,words,extension?data.Slice(count*4,extensionLength).ToArray():Array.Empty<byte>(),strings);
    }
    private static int StringLength(ReadOnlySpan<byte> raw,int cap)
    { int length=raw.IndexOf((byte)0);Need(length>=0&&length<=cap,"Bounded NUL byte string.");return length; }
    private static bool Leaf(IffNode node,string tag)=>node.Tag==tag&&node.FormType is null;
    private static void Validate(TerrainBoundaryRectangleInputLimits limits)
    {
        if(limits.MaximumInputBytes is <24 or >4194304||limits.MaximumNodes is <1 or >65536||
            limits.MaximumDepth is <1 or >64||limits.MaximumStringBytes is <0 or >65536||
            limits.MaximumTotalStringBytes is <0 or >131072)throw new ArgumentOutOfRangeException(nameof(limits));
    }
    private static void Need(bool value,string reason) { if(!value)throw new InvalidDataException(reason); }
}
