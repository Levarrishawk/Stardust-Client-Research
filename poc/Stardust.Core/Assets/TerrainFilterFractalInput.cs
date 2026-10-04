using System.Buffers.Binary;

namespace Stardust.Core.Assets;

public sealed record TerrainFilterFractalInputLimits(int MaximumInputBytes=65536,
    int MaximumNodes=64,int MaximumDepth=8,int MaximumStringBytes=4096,int MaximumDecodedCopyBytes=65536);

/// <summary>Version 0005 serialized fields; no fractal sampler or map lookup.</summary>
public sealed record TerrainFilterFractalFields(uint LowerSampleBits,uint UpperSampleBits,uint FamilyIdBits,uint FeatheringTypeBits,uint FeatheringAmountBits,uint SampleScaleBits)
{
    public bool HasFiniteRange=>(LowerSampleBits&0x7f800000)!=0x7f800000&&(UpperSampleBits&0x7f800000)!=0x7f800000;
    public bool HasFiniteSerializedFeatheringAmount=>float.IsFinite(BitConverter.UInt32BitsToSingle(FeatheringAmountBits));
}

/// <summary>Owned FFRA input; only 0005 is typed, other versions remain opaque. No influence evaluation.</summary>
public sealed class TerrainFilterFractalInput
{
    private readonly byte[] source;
    private readonly uint[] words;
    private readonly int decodedCopyBytes;
    private TerrainFilterFractalInput(byte[] source,string version,TerrainRuleHeader? header,
        TerrainFilterFractalFields? fields,uint[] words,int copies=0)
    { this.source=source;Version=version;Header=header;Fields=fields;this.words=words;decodedCopyBytes=copies; }
    public string Version { get; }
    public bool ClientVersionRecognized=>Version is "0000" or "0001" or "0002" or "0003" or "0004" or "0005";
    public bool HasDecodedFields=>Fields is not null;
    public TerrainRuleHeader? Header { get; }
    public TerrainFilterFractalFields? Fields { get; }
    public int DecodedCopyBytes=>decodedCopyBytes;
    public byte[] CopyEncodedBytes()=>(byte[])source.Clone();
    /// <summary>Serialized known DATA words in original order; empty for opaque inputs.</summary>
    public uint[] CopyDataWordBits()=>(uint[])words.Clone();
    public static TerrainFilterFractalInput Decode(TerrainGeneratorSection section,TerrainFilterFractalInputLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(section);limits??=new();Validate(limits);
        Need(section.EncodedBytes<=limits.MaximumInputBytes,"FFRA input budget before section copy.");
        return Decode(section.CopyEncodedBytes(),limits);
    }
    public static TerrainFilterFractalInput Decode(ReadOnlySpan<byte> encoded,TerrainFilterFractalInputLimits? limits=null)
    {
        limits??=new();Validate(limits);Need(encoded.Length<=limits.MaximumInputBytes,"FFRA input budget before snapshot.");
        byte[] owned=encoded.ToArray();
        var root=IffDocument.Parse(owned,new(limits.MaximumInputBytes,limits.MaximumNodes,limits.MaximumDepth)).Root;
        Need(root.IsForm("FFRA")&&root.Children.Count==1&&root.Children[0].FormType is not null,"Exact FFRA/version envelope.");
        var version=root.Children[0];string tag=version.FormType!;
        TerrainFilterFractalInput Opaque()=>new(owned,tag,null,null,Array.Empty<uint>());
        if(tag!="0005")return Opaque();
        const int count=6;
        Need(version.Children.Count==2&&version.Children[0].IsForm("IHDR")&&version.Children[1].IsForm("DATA")&&version.Children[1].Children.Count==1&&Leaf(version.Children[1].Children[0],"PARM")&&version.Children[1].Children[0].Payload.Length==count*4,"Known FFRA requires ordered IHDR/DATA with exact extent.");
        var ihdr=version.Children[0];Need(ihdr.Children.Count==1&&ihdr.Children[0].FormType is not null,"IHDR version envelope.");
        var hv=ihdr.Children[0];if(hv.FormType is not ("0000" or "0001"))return Opaque();
        Need(hv.Children.Count==1&&Leaf(hv.Children[0],"DATA"),"Exact known IHDR DATA.");
        var raw=hv.Children[0].Payload.Span;Need(raw.Length>=5,"Truncated IHDR flag/name.");
        uint enabled=BinaryPrimitives.ReadUInt32LittleEndian(raw);int nameLength=raw[4..].IndexOf((byte)0);
        Need(nameLength>=0&&nameLength<=limits.MaximumStringBytes,"Bounded IHDR byte string.");
        int legacy=hv.IsForm("0000")?3:0;Need(raw.Length==5+nameLength+legacy,"Exact known IHDR extent.");
        Need(4*count+nameLength+legacy<=limits.MaximumDecodedCopyBytes,"Decoded copy budget before header/word allocation.");
        var header=new TerrainRuleHeader(hv.FormType!,enabled,raw.Slice(4,nameLength).ToArray(),raw[(5+nameLength)..].ToArray());
        var data=version.Children[1].Children[0].Payload.Span;uint[] words=new uint[count];
        for(int i=0;i<count;i++)words[i]=BinaryPrimitives.ReadUInt32LittleEndian(data[(i*4)..]);
        var fields=new TerrainFilterFractalFields(words[3],words[4],words[0],words[1],words[2],words[5]);
        return new(owned,tag,header,fields,words,4*count+nameLength+legacy);
    }
    private static bool Leaf(IffNode node,string tag)=>node.Tag==tag&&node.FormType is null;
    private static void Validate(TerrainFilterFractalInputLimits limits)
    {
        if(limits.MaximumInputBytes is <24 or >4194304||limits.MaximumNodes is <1 or >65536||
            limits.MaximumDepth is <1 or >64||limits.MaximumStringBytes is <0 or >65536||limits.MaximumDecodedCopyBytes is <0 or >4194304)
            throw new ArgumentOutOfRangeException(nameof(limits));
    }
    private static void Need(bool value,string reason) { if(!value)throw new InvalidDataException(reason); }
}
