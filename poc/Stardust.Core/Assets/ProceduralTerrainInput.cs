using System.Buffers.Binary;

namespace Stardust.Core.Assets;

public sealed record ProceduralTerrainInputLimits(
    int MaximumInputBytes=16777216, int MaximumDataBytes=65536,
    int MaximumGeneratorBytes=8388608, int MaximumStringBytes=4096,
    int MaximumNodes=100000, int MaximumDepth=32, int MaximumTilesPerChunk=4096,
    TerrainMapMaskLimits? MapLimits=null);

/// <summary>Owned PTAT input envelope. TGEN is retained, never evaluated.</summary>
public sealed class ProceduralTerrainInput
{
    private readonly byte[] data, generator, terrainName, waterShader, legacyBranch, tail;
    private readonly uint[] words;
    private ProceduralTerrainInput(string version, byte[] data, byte[] generator,
        byte[] terrainName, byte[] waterShader, byte[] legacyBranch, byte[] tail,
        uint[] prefix, uint[] words, TerrainMapMasks maps)
    {
        Version=version;this.data=data;this.generator=generator;this.terrainName=terrainName;
        this.waterShader=waterShader;this.legacyBranch=legacyBranch;this.tail=tail;this.words=words;Maps=maps;
        WorldSizeBits=prefix[0];ChunkSizeBits=prefix[1];TilesPerChunk=prefix[2];
        GlobalWaterFlagBits=prefix[3];GlobalWaterHeightBits=prefix[4];GlobalWaterShaderSizeBits=prefix[5];
        EnvironmentCycleDurationBits=prefix[6];
    }
    public string Version { get; }
    public uint WorldSizeBits { get; }
    public uint ChunkSizeBits { get; }
    public uint TilesPerChunk { get; }
    public uint GlobalWaterFlagBits { get; }
    public bool UsesGlobalWater=>GlobalWaterFlagBits!=0;
    public uint GlobalWaterHeightBits { get; }
    public uint GlobalWaterShaderSizeBits { get; }
    public uint EnvironmentCycleDurationBits { get; }
    public float WorldSize=>BitConverter.UInt32BitsToSingle(WorldSizeBits);
    public float ChunkSize=>BitConverter.UInt32BitsToSingle(ChunkSizeBits);
    public float EnvironmentCycleDuration=>BitConverter.UInt32BitsToSingle(EnvironmentCycleDurationBits);
    public TerrainMapMasks Maps { get; }
    public int DataBytes=>data.Length;
    public int GeneratorBytes=>generator.Length;
    public int UnknownTailBytes=>tail.Length;
    public byte[] CopyDataRecord()=>(byte[])data.Clone();
    public byte[] CopyGeneratorForm()=>(byte[])generator.Clone();
    /// <summary>Original byte string without its terminator; no text encoding is asserted.</summary>
    public byte[] CopyTerrainNameBytes()=>(byte[])terrainName.Clone();
    public byte[] CopyWaterShaderBytes()=>(byte[])waterShader.Clone();
    public byte[] CopyVersion13Branch()=>(byte[])legacyBranch.Clone();
    public byte[] CopyUnknownDataTail()=>(byte[])tail.Clone();
    /// <summary>Twenty LE four-byte words stored by the client at template offsets 0x44 through 0x90.</summary>
    public uint GetMetadataWordBits(int index)
    {
        if(index is <0 or >=20)throw new ArgumentOutOfRangeException(nameof(index));
        return words[index];
    }
    public static ProceduralTerrainInput Decode(ReadOnlySpan<byte> source,ProceduralTerrainInputLimits? limits=null)
    {
        limits??=new();
        if(limits.MaximumInputBytes is <36 or >67108864 || limits.MaximumDataBytes is <1 or >1048576 ||
            limits.MaximumGeneratorBytes is <12 or >33554432 || limits.MaximumStringBytes is <0 or >65536 ||
            limits.MaximumNodes is <1 or >1000000 || limits.MaximumDepth is <1 or >128 ||
            limits.MaximumTilesPerChunk is <1 or >1048576)throw new ArgumentOutOfRangeException(nameof(limits));
        // Snapshot first, so all retained sections and scalars come from the same owned input.
        Need(source.Length<=limits.MaximumInputBytes,"PTAT input byte budget.");
        byte[] owned=source.ToArray();
        var root=IffDocument.Parse(owned,new(limits.MaximumInputBytes,limits.MaximumNodes,limits.MaximumDepth)).Root;
        Need(root.IsForm("PTAT")&&root.Children.Count==1,"Exact PTAT outer form required.");
        var version=root.Children[0];
        Need((version.IsForm("0013")||version.IsForm("0014"))&&version.Children.Count==3,"PTAT version/order required.");
        var data=version.Children[0];var generator=version.Children[1];var map=version.Children[2];
        Need(data.Tag=="DATA"&&data.FormType is null&&data.Payload.Length<=limits.MaximumDataBytes,"DATA tag/byte budget.");
        Need(generator.IsForm("TGEN")&&generator.Children.Count==1&&generator.Children[0].IsForm("0000")&&generator.Payload.Length+12<=limits.MaximumGeneratorBytes,"TGEN form/byte budget.");
        Need(map.IsForm("0000")||map.IsForm("0001"),"Direct map version form required.");
        var maps=TerrainMapMasks.Decode(owned.AsSpan(map.Offset,map.Payload.Length+12),limits.MapLimits);
        byte[] raw=data.Payload.ToArray();int cursor=0;
        byte[] name=ReadString(raw,ref cursor,limits.MaximumStringBytes);
        uint[] prefix=new uint[7];for(int i=0;i<6;i++)prefix[i]=ReadWord(raw,ref cursor);
        byte[] shader=ReadString(raw,ref cursor,limits.MaximumStringBytes);prefix[6]=ReadWord(raw,ref cursor);
        int branchStart=cursor;
        if(version.IsForm("0013"))
        {
            _=ReadString(raw,ref cursor,limits.MaximumStringBytes);
            _=ReadString(raw,ref cursor,limits.MaximumStringBytes);
            for(int i=0;i<3;i++) { _=ReadWord(raw,ref cursor);_=ReadString(raw,ref cursor,limits.MaximumStringBytes); }
            _=ReadWord(raw,ref cursor);
            _=ReadWord(raw,ref cursor);_=ReadString(raw,ref cursor,limits.MaximumStringBytes);
        }
        int branchEnd=cursor;
        uint[] words=new uint[20];for(int i=0;i<20;i++)words[i]=ReadWord(raw,ref cursor);
        Need(Positive(prefix[0])&&Positive(prefix[1]),"Modern finite positive terrain dimensions.");
        Need(prefix[2]>0&&prefix[2]<=limits.MaximumTilesPerChunk,"Modern tiles-per-chunk budget.");
        Need(Positive(prefix[6]),"Modern finite positive environment cycle duration.");
        if(prefix[3]!=0)Need(float.IsFinite(BitConverter.UInt32BitsToSingle(prefix[4]))&&Positive(prefix[5]),"Modern active-water eligibility.");
        return new(version.FormType!,raw,owned.AsSpan(generator.Offset,generator.Payload.Length+12).ToArray(),
            name,shader,raw.AsSpan(branchStart,branchEnd-branchStart).ToArray(),raw.AsSpan(cursor).ToArray(),prefix,words,maps);
    }
    private static uint ReadWord(byte[] bytes,ref int cursor)
    {
        Need(bytes.Length-cursor>=4,"Truncated DATA word.");
        uint value=BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(cursor,4));cursor+=4;return value;
    }
    private static byte[] ReadString(byte[] bytes,ref int cursor,int maximum)
    {
        int end=bytes.AsSpan(cursor).IndexOf((byte)0);
        Need(end>=0&&end<=maximum,"Missing/bounded DATA string terminator.");
        byte[] value=bytes.AsSpan(cursor,end).ToArray();cursor+=end+1;return value;
    }
    private static bool Positive(uint bits) { float value=BitConverter.UInt32BitsToSingle(bits);return float.IsFinite(value)&&value>0; }
    private static void Need(bool value,string message) { if(!value)throw new InvalidDataException(message); }
}
