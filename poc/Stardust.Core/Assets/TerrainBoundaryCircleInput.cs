using System.Buffers.Binary;

namespace Stardust.Core.Assets;

public sealed record TerrainBoundaryCircleInputLimits(int MaximumInputBytes=65536,
    int MaximumNodes=64,int MaximumDepth=8,int MaximumStringBytes=4096);

/// <summary>Serialized fields only. First/second center components do not assign world axes.</summary>
public sealed record TerrainBoundaryCircleFields(uint CenterFirstBits,uint CenterSecondBits,
    uint RadiusBits,uint? LegacyPrefixBits,uint? FeatheringTypeBits,uint? FeatheringAmountBits)
{
    public float CenterFirst=>BitConverter.UInt32BitsToSingle(CenterFirstBits);
    public float CenterSecond=>BitConverter.UInt32BitsToSingle(CenterSecondBits);
    public float Radius=>BitConverter.UInt32BitsToSingle(RadiusBits);
    public bool HasFiniteGeometry=>float.IsFinite(CenterFirst)&&float.IsFinite(CenterSecond)&&float.IsFinite(Radius);
    public bool? HasFiniteSerializedFeatheringAmount=>FeatheringAmountBits is uint bits
        ?float.IsFinite(BitConverter.UInt32BitsToSingle(bits)):null;
}

/// <summary>Owned BCIR input; unsupported versions remain opaque. No influence evaluation.</summary>
public sealed class TerrainBoundaryCircleInput
{
    private readonly byte[] source;
    private readonly uint[] words;
    private TerrainBoundaryCircleInput(byte[] source,string version,TerrainRuleHeader? header,
        TerrainBoundaryCircleFields? fields,uint[] words)
    { this.source=source;Version=version;Header=header;Fields=fields;this.words=words; }
    public string Version { get; }
    public bool ClientVersionRecognized=>Version is "0000" or "0001" or "0002";
    public bool HasDecodedFields=>Fields is not null;
    public TerrainRuleHeader? Header { get; }
    public TerrainBoundaryCircleFields? Fields { get; }
    public byte[] CopyEncodedBytes()=>(byte[])source.Clone();
    /// <summary>Serialized known DATA words in original order; empty for opaque inputs.</summary>
    public uint[] CopyDataWordBits()=>(uint[])words.Clone();
    public static TerrainBoundaryCircleInput Decode(TerrainGeneratorSection section,TerrainBoundaryCircleInputLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(section);limits??=new();Validate(limits);
        Need(section.EncodedBytes<=limits.MaximumInputBytes,"BCIR input budget before section copy.");
        return Decode(section.CopyEncodedBytes(),limits);
    }
    public static TerrainBoundaryCircleInput Decode(ReadOnlySpan<byte> encoded,TerrainBoundaryCircleInputLimits? limits=null)
    {
        limits??=new();Validate(limits);Need(encoded.Length<=limits.MaximumInputBytes,"BCIR input budget before snapshot.");
        byte[] owned=encoded.ToArray();
        var root=IffDocument.Parse(owned,new(limits.MaximumInputBytes,limits.MaximumNodes,limits.MaximumDepth)).Root;
        Need(root.IsForm("BCIR")&&root.Children.Count==1&&root.Children[0].FormType is not null,"Exact BCIR/version envelope.");
        var version=root.Children[0];string tag=version.FormType!;
        TerrainBoundaryCircleInput Opaque()=>new(owned,tag,null,null,Array.Empty<uint>());
        if(tag is not ("0000" or "0001" or "0002"))return Opaque();
        int count=tag=="0000"?3:tag=="0001"?4:5;
        Need(version.Children.Count==2&&version.Children[0].IsForm("IHDR")&&Leaf(version.Children[1],"DATA")&&version.Children[1].Payload.Length==count*4,"Known BCIR requires ordered IHDR/DATA with exact extent.");
        var ihdr=version.Children[0];Need(ihdr.Children.Count==1&&ihdr.Children[0].FormType is not null,"IHDR version envelope.");
        var hv=ihdr.Children[0];if(hv.FormType is not ("0000" or "0001"))return Opaque();
        Need(hv.Children.Count==1&&Leaf(hv.Children[0],"DATA"),"Exact known IHDR DATA.");
        var raw=hv.Children[0].Payload.Span;Need(raw.Length>=5,"Truncated IHDR flag/name.");
        uint enabled=BinaryPrimitives.ReadUInt32LittleEndian(raw);int nameLength=raw[4..].IndexOf((byte)0);
        Need(nameLength>=0&&nameLength<=limits.MaximumStringBytes,"Bounded IHDR byte string.");
        int legacy=hv.IsForm("0000")?3:0;Need(raw.Length==5+nameLength+legacy,"Exact known IHDR extent.");
        var header=new TerrainRuleHeader(hv.FormType!,enabled,raw.Slice(4,nameLength).ToArray(),raw[(5+nameLength)..].ToArray());
        var data=version.Children[1].Payload.Span;uint[] words=new uint[count];
        for(int i=0;i<count;i++)words[i]=BinaryPrimitives.ReadUInt32LittleEndian(data[(i*4)..]);
        int start=tag=="0001"?1:0;
        var fields=new TerrainBoundaryCircleFields(words[start],words[start+1],words[start+2],
            tag=="0001"?words[0]:null,tag=="0002"?words[3]:null,tag=="0002"?words[4]:null);
        return new(owned,tag,header,fields,words);
    }
    private static bool Leaf(IffNode node,string tag)=>node.Tag==tag&&node.FormType is null;
    private static void Validate(TerrainBoundaryCircleInputLimits limits)
    {
        if(limits.MaximumInputBytes is <24 or >4194304||limits.MaximumNodes is <1 or >65536||
            limits.MaximumDepth is <1 or >64||limits.MaximumStringBytes is <0 or >65536)
            throw new ArgumentOutOfRangeException(nameof(limits));
    }
    private static void Need(bool value,string reason) { if(!value)throw new InvalidDataException(reason); }
}
