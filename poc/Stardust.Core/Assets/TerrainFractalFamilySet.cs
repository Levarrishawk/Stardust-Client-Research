using System.Buffers.Binary;
using System.Security.Cryptography;
using Stardust.Core.Geometry;

namespace Stardust.Core.Assets;

public sealed record TerrainFractalFamilySetLimits(int MaximumInputBytes=1048576,int MaximumEntries=4096,
    int MaximumNameBytes=4096,long MaximumAggregateNameBytes=1048576,long MaximumEmbeddedBytes=344064,
    long MaximumSnapshotBytes=2097152,long MaximumWork=2097152);

/// <summary>Owned raw family evidence. Name bytes include their terminator; no text encoding is inferred.</summary>
public sealed class TerrainFractalFamilyEntry
{
    private readonly byte[] name;
    internal TerrainFractalFamilyEntry(int id,int offset,int encodedBytes,byte[] name,TerrainFractalRawInput raw)
    { Id=id;Offset=offset;EncodedBytes=encodedBytes;this.name=name;Raw=raw; }
    public int Id { get; }
    public int Offset { get; }
    public int EncodedBytes { get; }
    public int NameBytes=>name.Length;
    public TerrainFractalRawInput Raw { get; }
    public byte[] CopyNameBytes()=>(byte[])name.Clone();
}

/// <summary>Atomic MGRP/0000 evidence snapshot, independent of original map pointers and runtime controls.</summary>
public sealed class TerrainFractalFamilySet
{
    private readonly Dictionary<int,TerrainFractalFamilyEntry> byId;
    private TerrainFractalFamilySet(TerrainFractalFamilyEntry[] entries,string hash,int inputBytes,long names,long snapshot,long work)
    {
        Entries=Array.AsReadOnly(entries);byId=entries.ToDictionary(e=>e.Id);
        SourceSha256=hash;InputBytes=inputBytes;AggregateNameBytes=names;
        EmbeddedBytes=84L*entries.Length;SnapshotBytes=snapshot;Work=work;
    }
    public IReadOnlyList<TerrainFractalFamilyEntry> Entries { get; }
    public string SourceSha256 { get; }
    public int InputBytes { get; }
    public long AggregateNameBytes { get; }
    public long EmbeddedBytes { get; }
    public long SnapshotBytes { get; }
    public long Work { get; }
    public TerrainFractalFamilyEntry? Find(int id)=>byId.GetValueOrDefault(id);

    public static TerrainFractalFamilySet Decode(ReadOnlySpan<byte> source,TerrainFractalFamilySetLimits? limits=null,CancellationToken token=default)
    {
        limits??=new();Policy(limits);token.ThrowIfCancellationRequested();
        Need(source.Length<=limits.MaximumInputBytes,"Family input cap.");
        int root=Form(source,0,source.Length,"MGRP"u8);Need(root==source.Length,"Trailing MGRP bytes.");
        int version=Form(source,12,root,"0000"u8);Need(version==root,"Trailing MGRP children.");
        int count=0;long names=0;
        // Full extents, names, embedded envelopes and aggregate budgets before snapshots or decoders.
        for(int cursor=24;cursor<version;)
        {
            token.ThrowIfCancellationRequested();var e=Entry(source,cursor,version);
            Need(e.NameBytes<=limits.MaximumNameBytes,"Per-name cap.");
            count++;names+=e.NameBytes;Caps(source.Length,count,names,limits);cursor=e.End;
        }
        Caps(source.Length,count,names,limits);
        var ids=new HashSet<int>();
        for(int cursor=24;cursor<version;)
        { token.ThrowIfCancellationRequested();var e=Entry(source,cursor,version);Need(ids.Add(e.Id),"Duplicate family ID policy.");cursor=e.End; }
        token.ThrowIfCancellationRequested();string hash=Convert.ToHexString(SHA256.HashData(source));
        var entries=new TerrainFractalFamilyEntry[count];int index=0;
        for(int cursor=24;cursor<version;)
        {
            token.ThrowIfCancellationRequested();var e=Entry(source,cursor,version);
            byte[] name=source.Slice(cursor+24,e.NameBytes).ToArray();
            var raw=TerrainFractalRawInput.Decode(source.Slice(e.RawOffset,84));
            entries[index++]=new(e.Id,cursor,e.End-cursor,name,raw);cursor=e.End;
        }
        token.ThrowIfCancellationRequested();
        var result=new TerrainFractalFamilySet(entries,hash,source.Length,names,names+136L*count,source.Length+84L*count);
        token.ThrowIfCancellationRequested();return result;
    }
    private readonly record struct Extent(int Id,int End,int NameBytes,int RawOffset);
    private static Extent Entry(ReadOnlySpan<byte> source,int cursor,int parent)
    {
        int end=Form(source,cursor,parent,"MFAM"u8);int data=cursor+12;
        int stop=Chunk(source,data,end,"DATA"u8);int length=stop-data-8;
        Need(length>=5,"Family DATA needs signed ID and terminated name.");
        var name=source.Slice(data+12,length-4);
        Need(name[^1]==0&&name[..^1].IndexOf((byte)0)<0,"Exact final name terminator; no trailing fields.");
        int raw=stop;int rawEnd=Form(source,raw,end,"MFRC"u8);Need(rawEnd==end&&rawEnd-raw==84,"Exact embedded raw extent.");
        int rv=Form(source,raw+12,rawEnd,"0001"u8);Need(rv==rawEnd,"Exact raw version extent.");
        int rd=Chunk(source,raw+24,rv,"DATA"u8);Need(rd==rv&&rd-(raw+24)-8==52,"Exact 13-word raw DATA.");
        return new(BinaryPrimitives.ReadInt32LittleEndian(source.Slice(data+8,4)),end,name.Length,raw);
    }
    private static int Chunk(ReadOnlySpan<byte> source,int offset,int parent,ReadOnlySpan<byte> tag)
    {
        Need(offset>=0&&parent<=source.Length&&parent-offset>=8,"Truncated IFF extent.");
        Need(source.Slice(offset,4).SequenceEqual(tag),"Unexpected IFF tag.");
        uint length=BinaryPrimitives.ReadUInt32BigEndian(source.Slice(offset+4,4));
        Need(length<=(uint)(parent-offset-8),"IFF extent outside parent.");return offset+8+(int)length;
    }
    private static int Form(ReadOnlySpan<byte> source,int offset,int parent,ReadOnlySpan<byte> type)
    { int end=Chunk(source,offset,parent,"FORM"u8);Need(end-offset>=12&&source.Slice(offset+8,4).SequenceEqual(type),"Unexpected FORM type/version.");return end; }
    private static void Caps(int bytes,int count,long names,TerrainFractalFamilySetLimits limits)
    {
        Need(count<=limits.MaximumEntries&&names<=limits.MaximumAggregateNameBytes
            &&84L*count<=limits.MaximumEmbeddedBytes&&names+136L*count<=limits.MaximumSnapshotBytes
            &&bytes+84L*count<=limits.MaximumWork,"Family aggregate cap before snapshots.");
    }
    private static void Policy(TerrainFractalFamilySetLimits p)
    {
        if(p.MaximumInputBytes is <0 or >16777216||p.MaximumEntries is <0 or >65536
            ||p.MaximumNameBytes is <1 or >65536||p.MaximumAggregateNameBytes is <0 or >16777216
            ||p.MaximumEmbeddedBytes is <0 or >5505024||p.MaximumSnapshotBytes is <0 or >33554432
            ||p.MaximumWork is <0 or >33554432)throw new ArgumentOutOfRangeException(nameof(p));
    }
    private static void Need(bool condition,string reason) { if(!condition)throw new InvalidDataException(reason); }
}
