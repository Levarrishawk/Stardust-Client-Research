using System.Buffers.Binary;

namespace Stardust.Core.Geometry;

public sealed record TerrainFractalRawInputLimits(int MaximumInputBytes=65536,int MaximumInputs=65536,
    long MaximumAggregateInputBytes=5505024,long MaximumOwnedCopyBytes=8912896);
public sealed record TerrainFractalRawFields(uint SeedBits,uint FirstCurveFlagBits,uint FirstCurveParameterBits,
    uint SecondCurveFlagBits,uint SecondCurveParameterBits,uint OctaveCountBits,uint FrequencyMultiplierBits,
    uint AmplitudeMultiplierBits,uint FirstCoordinateScaleBits,uint SecondCoordinateScaleBits,
    uint FirstOffsetBits,uint SecondOffsetBits,uint ModeBits)
{
    public int SignedSeed=>unchecked((int)SeedBits);
    public int SignedOctaveCount=>unchecked((int)OctaveCountBits);
    public bool FirstCurveEnabled=>FirstCurveFlagBits!=0;
    public bool SecondCurveEnabled=>SecondCurveFlagBits!=0;
    private static bool Finite(uint bits)=>(bits&0x7f800000)!=0x7f800000;
    public bool HasFiniteParameters=>Finite(FirstCurveParameterBits)&&Finite(SecondCurveParameterBits)
        &&Finite(FrequencyMultiplierBits)&&Finite(AmplitudeMultiplierBits)&&Finite(FirstCoordinateScaleBits)
        &&Finite(SecondCoordinateScaleBits)&&Finite(FirstOffsetBits)&&Finite(SecondOffsetBits);
}
public sealed class TerrainFractalRawBatch
{
    internal TerrainFractalRawBatch(TerrainFractalRawInput[] inputs,long bytes,long copies)
    { Inputs=Array.AsReadOnly(inputs);InputBytes=bytes;OwnedCopyBytes=copies; }
    public IReadOnlyList<TerrainFractalRawInput> Inputs { get; }
    public long InputBytes { get; }
    public long OwnedCopyBytes { get; }
}
/// <summary>Strict MFRC/0001 raw words only. No runtime sampler, normalization or 0000 grammar.</summary>
public sealed class TerrainFractalRawInput
{
    public const int EncodedBytes=84,PayloadBytes=52,OwnedCopyBytes=136;
    private readonly byte[] source;
    private readonly uint[] words;
    private TerrainFractalRawInput(byte[] source,uint[] words)
    {
        this.source=source;this.words=words;
        Fields=new(words[0],words[1],words[2],words[3],words[4],words[5],words[6],words[7],words[8],words[9],words[10],words[11],words[12]);
    }
    public string Version=>"0001";
    public TerrainFractalRawFields Fields { get; }
    public byte[] CopyEncodedBytes()=>(byte[])source.Clone();
    public uint[] CopyDataWordBits()=>(uint[])words.Clone();
    public static TerrainFractalRawInput Decode(ReadOnlySpan<byte> encoded,TerrainFractalRawInputLimits? limits=null)
    {
        limits=Policy(limits);Cap(1,encoded.Length,OwnedCopyBytes,limits);Envelope(encoded,limits);
        return Copy(encoded,limits);
    }
    public static TerrainFractalRawBatch DecodeBatch(IReadOnlyList<ReadOnlyMemory<byte>> encoded,TerrainFractalRawInputLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(encoded);limits=Policy(limits);int count=encoded.Count;
        long copies=136L*count;Cap(count,0,copies,limits);long bytes=0;
        // Every aggregate bound and envelope is checked before any payload snapshot/result array.
        for(int i=0;i<count;i++)
        { var input=encoded[i];bytes+=input.Length;Cap(count,bytes,copies,limits);Envelope(input.Span,limits); }
        var results=new TerrainFractalRawInput[count];
        for(int i=0;i<count;i++)results[i]=Copy(encoded[i].Span,limits);
        return new(results,bytes,copies);
    }
    private static TerrainFractalRawInput Copy(ReadOnlySpan<byte> encoded,TerrainFractalRawInputLimits limits)
    {
        // Revalidate a borrowed source before copying and the owned snapshot before interpreting words.
        Envelope(encoded,limits);byte[] source=encoded.ToArray();Envelope(source,limits);
        uint[] words=new uint[13];for(int i=0;i<words.Length;i++)words[i]=BinaryPrimitives.ReadUInt32LittleEndian(source.AsSpan(32+i*4,4));
        return new(source,words);
    }
    private static void Envelope(ReadOnlySpan<byte> source,TerrainFractalRawInputLimits limits)
    {
        Need(source.Length<=limits.MaximumInputBytes&&source.Length==EncodedBytes,"Exact bounded MFRC/0001 extent required.");
        Need(source[..4].SequenceEqual("FORM"u8)&&source.Slice(8,4).SequenceEqual("MFRC"u8)
            &&source.Slice(12,4).SequenceEqual("FORM"u8)&&source.Slice(20,4).SequenceEqual("0001"u8)
            &&source.Slice(24,4).SequenceEqual("DATA"u8),"Exact MFRC/0001/DATA tags; 0000 unsupported.");
        Need(BinaryPrimitives.ReadUInt32BigEndian(source.Slice(4,4))==76
            &&BinaryPrimitives.ReadUInt32BigEndian(source.Slice(16,4))==64
            &&BinaryPrimitives.ReadUInt32BigEndian(source.Slice(28,4))==52,"Exact MFRC form/chunk sizes, no trailing bytes.");
    }
    private static TerrainFractalRawInputLimits Policy(TerrainFractalRawInputLimits? limits)
    {
        limits??=new();
        if(limits.MaximumInputBytes is <0 or >1048576||limits.MaximumInputs is <0 or >1048576
            ||limits.MaximumAggregateInputBytes is <0 or >88080384||limits.MaximumOwnedCopyBytes is <0 or >142606336)
            throw new ArgumentOutOfRangeException(nameof(limits));
        return limits;
    }
    private static void Cap(int count,long bytes,long copies,TerrainFractalRawInputLimits limits)
        =>Need(count<=limits.MaximumInputs&&bytes<=limits.MaximumAggregateInputBytes&&copies<=limits.MaximumOwnedCopyBytes,"Aggregate input/count/owned-copy caps before snapshots.");
    private static void Need(bool okay,string reason) { if(!okay)throw new InvalidDataException(reason); }
}
