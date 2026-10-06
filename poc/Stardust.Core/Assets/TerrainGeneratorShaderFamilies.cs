using System.Buffers.Binary;
using System.Security.Cryptography;
namespace Stardust.Core.Assets;
public sealed record TerrainGeneratorShaderFamiliesLimits(int MaximumEncodedBytes=4194304,int MaximumFamilies=4096,int MaximumEntriesPerFamily=1024,
    int MaximumTotalEntries=65536,int MaximumStringBytes=999,long MaximumTotalStringBytes=1048576,long MaximumCopyBytes=8388608,
    long MaximumArrayBytes=8388608,long MaximumWork=16777216);
public sealed class TerrainGeneratorShaderEntry
{
    private readonly byte[] source;private readonly int text,length;
    internal TerrainGeneratorShaderEntry(byte[] source,int index,int offset,int bytes,int text,int length,uint word)
    {this.source=source;Index=index;Offset=offset;EncodedBytes=bytes;this.text=text;this.length=length;RawWordBits=word;}
    public int Index {get;} public int Offset {get;} public int EncodedBytes {get;} public uint RawWordBits {get;}
    public byte[] CopyStringBytes()=>source.AsSpan(text,length).ToArray();
    public byte[] CopyEncodedBytes()=>source.AsSpan(Offset,EncodedBytes).ToArray();
}
public sealed class TerrainGeneratorShaderFamily
{
    private readonly byte[] source;private readonly int first,firstLength,second,secondLength,triple;
    internal TerrainGeneratorShaderFamily(byte[] source,int index,int offset,int bytes,uint key,int first,int firstLength,int second,int secondLength,int triple,
        uint word0,uint word1,uint count,TerrainGeneratorShaderEntry[] entries)
    {this.source=source;Index=index;Offset=offset;EncodedBytes=bytes;FamilyKeyBits=key;this.first=first;this.firstLength=firstLength;this.second=second;this.secondLength=secondLength;
        this.triple=triple;RawWord0Bits=word0;RawWord1Bits=word1;EntryCountBits=count;Entries=Array.AsReadOnly(entries);}
    public int Index {get;} public int Offset {get;} public int EncodedBytes {get;} public uint FamilyKeyBits {get;}
    public uint RawWord0Bits {get;} public uint RawWord1Bits {get;} public uint EntryCountBits {get;}
    public IReadOnlyList<TerrainGeneratorShaderEntry> Entries {get;}
    public byte[] CopyFirstStringBytes()=>source.AsSpan(first,firstLength).ToArray();
    public byte[] CopySecondStringBytes()=>source.AsSpan(second,secondLength).ToArray();
    public byte[] CopyRawTripleBytes()=>source.AsSpan(triple,3).ToArray();
    public byte[] CopyEncodedBytes()=>source.AsSpan(Offset,EncodedBytes).ToArray();
}
/// <summary>Owned strict SGRP/0006 raw structure. No shader resolution, material meaning, GPU or legacy blending.</summary>
public sealed class TerrainGeneratorShaderFamilies
{
    private readonly byte[] source;
    private TerrainGeneratorShaderFamilies(byte[] source,TerrainGeneratorShaderFamily[] families,int entries,long strings,long copies,long arrays,long work,
        TerrainGeneratorInput? generator,TerrainGeneratorGroup? group)
    {this.source=source;Families=Array.AsReadOnly(families);TotalEntries=entries;TotalStringBytes=strings;LogicalCopyBytes=copies;ArrayBytes=arrays;Work=work;
        Generator=generator;Group=group;SourceSha256=Convert.ToHexString(SHA256.HashData(source));}
    public string Version=>"0006";public int EncodedBytes=>source.Length;public string SourceSha256 {get;}
    public TerrainGeneratorInput? Generator {get;} public TerrainGeneratorGroup? Group {get;}
    public int? GeneratorOffset=>Group?.Section.Offset;public string? GeneratorSha256=>Generator?.SourceSha256;
    public IReadOnlyList<TerrainGeneratorShaderFamily> Families {get;} public int TotalEntries {get;} public long TotalStringBytes {get;}
    public long LogicalCopyBytes {get;} public long ArrayBytes {get;} public long Work {get;}
    public long ExportedGroupBytes=>Generator is null?0:source.Length;
    public bool HasShaderSemantics=>false;
    public byte[] CopyEncodedBytes()=>(byte[])source.Clone();
    private static void Need(bool okay,string why){if(!okay)throw new InvalidDataException(why);}
    private static TerrainGeneratorShaderFamiliesLimits Policy(TerrainGeneratorShaderFamiliesLimits? limits)
    {
        limits??=new();if(limits.MaximumEncodedBytes is <0 or >8388608||limits.MaximumFamilies is <0 or >65536||limits.MaximumEntriesPerFamily is <0 or >65536
            ||limits.MaximumTotalEntries is <0 or >262144||limits.MaximumStringBytes is <0 or >999||limits.MaximumTotalStringBytes is <0 or >8388608
            ||limits.MaximumCopyBytes is <0 or >16777216||limits.MaximumArrayBytes is <0 or >33554432||limits.MaximumWork is <0 or >67108864)
            throw new ArgumentOutOfRangeException(nameof(limits));return limits;
    }
    private static uint Word(ReadOnlySpan<byte> b,int p,int end){Need(p>=0&&end-p>=4,"Truncated raw word.");return BinaryPrimitives.ReadUInt32LittleEndian(b[p..]);}
    private static int Extent(ReadOnlySpan<byte> b,int p,int end,ReadOnlySpan<byte> tag)
    {Need(p>=0&&end-p>=8&&b.Slice(p,4).SequenceEqual(tag),"Exact chunk/form tag required.");uint size=BinaryPrimitives.ReadUInt32BigEndian(b[(p+4)..]);Need(size<=(long)end-p-8,"Declared extent outside owner.");return checked(p+8+(int)size);}
    private static void Header(ReadOnlySpan<byte> b)
    {Need(b.Length>=24&&Extent(b,0,b.Length,"FORM"u8)==b.Length&&b.Slice(8,4).SequenceEqual("SGRP"u8)
        &&Extent(b,12,b.Length,"FORM"u8)==b.Length&&b.Slice(20,4).SequenceEqual("0006"u8),"Exact sole SGRP/0006 version and extent required.");}
    private static (int Start,int Length) Text(ReadOnlySpan<byte> b,ref int p,int end,TerrainGeneratorShaderFamiliesLimits limits,ref long strings,CancellationToken token)
    {
        int start=p;while(p<end&&b[p]!=0){token.ThrowIfCancellationRequested();Need((long)p-start<limits.MaximumStringBytes,"Individual raw string cap.");p++;}
        Need(p<end,"NUL termination inside SFAM required.");int length=p-start;p++;strings=checked(strings+length+1L);Need(strings<=limits.MaximumTotalStringBytes,"Aggregate raw string cap including NULs.");return(start,length);
    }
    private static (int Families,int Entries,long Strings,long Arrays,long Work) Walk(ReadOnlySpan<byte> b,TerrainGeneratorShaderFamiliesLimits limits,int copyMultiplier,CancellationToken token)
    {
        Need(b.Length<=limits.MaximumEncodedBytes&&(long)copyMultiplier*b.Length<=limits.MaximumCopyBytes&&(copyMultiplier+4L)*b.Length<=limits.MaximumWork,
            "Encoded/copy/minimum-work caps before scanning/snapshots.");Header(b);int p=24,families=0,entries=0;long strings=0;
        while(p<b.Length){
            token.ThrowIfCancellationRequested();int end=Extent(b,p,b.Length,"SFAM"u8);int q=p+8;_=Word(b,q,end);q+=4;
            _=Text(b,ref q,end,limits,ref strings,token);_=Text(b,ref q,end,limits,ref strings,token);
            Need(end-q>=15,"Three raw bytes, two words and entry count required.");uint count=Word(b,q+11,end);q+=15;
            Need(count<=int.MaxValue&&count<=limits.MaximumEntriesPerFamily&&5L*count<=end-q,"Nonnegative bounded count with minimum entry extent required.");
            entries=checked(entries+(int)count);Need(entries<=limits.MaximumTotalEntries,"Aggregate entry cap.");
            for(int j=0;j<(int)count;j++){_=Text(b,ref q,end,limits,ref strings,token);_=Word(b,q,end);q+=4;}
            Need(q==end,"Exact SFAM consumption; trailing payload unsupported.");p=end;families=checked(families+1);Need(families<=limits.MaximumFamilies,"Family count cap.");
            Need(copyMultiplier*(long)b.Length+256L+128L*families+64L*entries<=limits.MaximumArrayBytes&&(copyMultiplier+4L)*b.Length+64L*families+32L*entries<=limits.MaximumWork,"Array/work reservation before owned records.");
        }
        long arrays=copyMultiplier*(long)b.Length+256L+128L*families+64L*entries,work=(copyMultiplier+4L)*b.Length+64L*families+32L*entries;
        Need(arrays<=limits.MaximumArrayBytes&&work<=limits.MaximumWork,"Complete array/work caps.");return(families,entries,strings,arrays,work);
    }
    public static TerrainGeneratorShaderFamilies Decode(ReadOnlySpan<byte> encoded,TerrainGeneratorShaderFamiliesLimits? limits=null,CancellationToken token=default)
        =>DecodeCore(encoded,Policy(limits),token,null,null,1);
    public static TerrainGeneratorShaderFamilies Bind(TerrainGeneratorInput generator,TerrainGeneratorShaderFamiliesLimits? limits=null,CancellationToken token=default)
    {
        ArgumentNullException.ThrowIfNull(generator);limits=Policy(limits);token.ThrowIfCancellationRequested();
        Need(generator.Groups.Count>=1&&generator.Groups[0].Role==TerrainGeneratorGroupRole.Shader,"Actual shader group required.");var group=generator.Groups[0];var s=group.Section;
        Need(s.Tag=="FORM"&&s.FormType=="SGRP"&&s.Version=="0006"&&s.Offset>=24&&(long)s.Offset+s.EncodedBytes<=generator.EncodedBytes
            &&s.EncodedBytes<=limits.MaximumEncodedBytes&&group.RecordCount<=limits.MaximumFamilies&&2L*s.EncodedBytes<=limits.MaximumCopyBytes
            &&6L*s.EncodedBytes+64L*group.RecordCount<=limits.MaximumWork&&2L*s.EncodedBytes+256L+128L*group.RecordCount<=limits.MaximumArrayBytes,
            "Known actual group identity/extent/count/minimum budgets before export.");
        // Frozen section API requires one group export before string/entry inspection; it is fully charged.
        byte[] exported=s.CopyEncodedBytes();return DecodeCore(exported,limits,token,generator,group,2);
    }
    private static TerrainGeneratorShaderFamilies DecodeCore(ReadOnlySpan<byte> encoded,TerrainGeneratorShaderFamiliesLimits limits,CancellationToken token,
        TerrainGeneratorInput? generator,TerrainGeneratorGroup? group,int multiplier)
    {
        token.ThrowIfCancellationRequested();var count=Walk(encoded,limits,multiplier,token);token.ThrowIfCancellationRequested();byte[] source=encoded.ToArray();
        var owned=Walk(source,limits,multiplier,token);Need(count==owned,"Owned snapshot structural counts changed.");
        if(group is not null)Need(owned.Families==group.RecordCount&&source.Length==group.Section.EncodedBytes,"Actual group record count/extent identity.");
        var families=new TerrainGeneratorShaderFamily[owned.Families];int p=24;long strings=0;
        for(int i=0;i<families.Length;i++){
            token.ThrowIfCancellationRequested();int start=p,end=Extent(source,p,source.Length,"SFAM"u8),q=p+8;uint key=Word(source,q,end);q+=4;
            var first=Text(source,ref q,end,limits,ref strings,token);var second=Text(source,ref q,end,limits,ref strings,token);int triple=q;
            uint word0=Word(source,q+3,end),word1=Word(source,q+7,end),n=Word(source,q+11,end);q+=15;var entries=new TerrainGeneratorShaderEntry[(int)n];
            for(int j=0;j<entries.Length;j++){token.ThrowIfCancellationRequested();int a=q;var text=Text(source,ref q,end,limits,ref strings,token);uint word=Word(source,q,end);q+=4;entries[j]=new(source,j,a,q-a,text.Start,text.Length,word);}
            Need(q==end,"Owned exact family extent.");families[i]=new(source,i,start,end-start,key,first.Start,first.Length,second.Start,second.Length,triple,word0,word1,n,entries);p=end;
        }
        Need(strings==owned.Strings,"Owned string accounting identity.");token.ThrowIfCancellationRequested();
        var result=new TerrainGeneratorShaderFamilies(source,families,owned.Entries,owned.Strings,multiplier*(long)source.Length,owned.Arrays,owned.Work,generator,group);
        token.ThrowIfCancellationRequested();return result;
    }
}
