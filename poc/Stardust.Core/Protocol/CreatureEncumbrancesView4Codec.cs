using System.Buffers.Binary;
namespace Stardust.Core.Protocol;
public sealed record CreatureEncumbrancesView4Limits(int MaximumPayloadBytes=16384,int MaximumFields=256,int MaximumCommands=4096,int MaximumValues=65536,long MaximumDecodedBytes=1048576);
public sealed class CreatureEncumbrancesView4Command
{
    public CreatureEncumbrancesView4Command(byte opcode,ushort index,IEnumerable<int> values)
    {var copy=CreatureEncumbrancesView4Codec.Freeze(values,65535);if(opcode>4 || (opcode is 0 or 4 && copy.Length!=0) || (opcode is 1 or 2 && copy.Length!=1) || (opcode is 3 or 4 && index!=0))throw new InvalidDataException("encumbrance command shape");Opcode=opcode;Index=index;Values=Array.AsReadOnly(copy);}
    public byte Opcode{get;}public ushort Index{get;}public IReadOnlyList<int> Values{get;}
}
public sealed class CreatureEncumbrancesView4Update
{
    public CreatureEncumbrancesView4Update(ushort field,uint extent,uint counter,IEnumerable<CreatureEncumbrancesView4Command> commands)
    {if(field != 2)throw new InvalidDataException("encumbrance field");Field=field;Extent=extent;Counter=counter;var copy=CreatureEncumbrancesView4Codec.Freeze(commands,65536);if(copy.Any(c=>c is null))throw new InvalidDataException("Null encumbrance command");Commands=Array.AsReadOnly(copy);}
    public ushort Field{get;}public uint Extent{get;}public uint Counter{get;}public IReadOnlyList<CreatureEncumbrancesView4Command> Commands{get;}
}
public sealed class CreatureEncumbrancesView4Delta
{
    public CreatureEncumbrancesView4Delta(ulong objectId,ushort declaredCount,IEnumerable<CreatureEncumbrancesView4Update> updates)
    {ObjectId=objectId;DeclaredCount=declaredCount;var copy=CreatureEncumbrancesView4Codec.Freeze(updates,4096);if(copy.Any(c=>c is null))throw new InvalidDataException("Null encumbrance update");Updates=Array.AsReadOnly(copy);}
    public ulong ObjectId{get;}public ushort DeclaredCount{get;}public IReadOnlyList<CreatureEncumbrancesView4Update> Updates{get;}
}
public static class CreatureEncumbrancesView4Codec
{
    internal static T[] Freeze<T>(IEnumerable<T> source,int maximum)
    {
        ArgumentNullException.ThrowIfNull(source);var values=new List<T>();
        foreach(var value in source){if(values.Count>=maximum)throw new InvalidDataException("Encumbrance owned collection limit");values.Add(value);}return values.ToArray();
    }
    internal static void Validate(CreatureEncumbrancesView4Limits l)
    {ArgumentNullException.ThrowIfNull(l);ReplicationEnvelopeCodec.ValidateBudget(l.MaximumPayloadBytes);if(l.MaximumPayloadBytes>8388608 || l.MaximumFields is <0 or >4096 || l.MaximumCommands is <0 or >65536 || l.MaximumValues is <0 or >1048576 || l.MaximumDecodedBytes is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(l));}
    private static void Budget(long bytes,int fields,long commands,long values,CreatureEncumbrancesView4Limits l)
    {if(bytes+2>l.MaximumPayloadBytes || bytes>l.MaximumDecodedBytes || fields>l.MaximumFields || commands>l.MaximumCommands || values>l.MaximumValues)throw new InvalidDataException("encumbrance global message budget");}
    internal static (long Bytes,long Commands,long Values) Measure(CreatureEncumbrancesView4Delta plan,CreatureEncumbrancesView4Limits l)
    {
        Validate(l);ArgumentNullException.ThrowIfNull(plan);if(plan.Updates.Count==0)throw new InvalidDataException("Empty encumbrance delta");long bytes=0,commands=0,values=0;
        Budget(bytes,plan.Updates.Count,commands,values,l);
        foreach(var u in plan.Updates){bytes+=10;ulong cursor=0;foreach(var c in u.Commands){if(cursor>=u.Extent)throw new InvalidDataException("Extra encumbrance command");cursor+=1UL+(c.Opcode==3?(ulong)c.Values.Count:0);commands++;values+=c.Values.Count;bytes+=(c.Opcode==4?1:3)+4L*c.Values.Count;Budget(bytes,plan.Updates.Count,commands,values,l);}if(cursor<u.Extent)throw new InvalidDataException("Incomplete encumbrance extent");}
        Budget(bytes,plan.Updates.Count,commands,values,l);return(bytes,commands,values);
    }
    private ref struct Reader(ReadOnlySpan<byte> bytes)
    {
        private readonly ReadOnlySpan<byte> bytes=bytes;public int Position{get;private set;}public int Remaining=>bytes.Length-Position;
        public ReadOnlySpan<byte> Take(int n){if(n<0||n>Remaining)throw new InvalidDataException("Truncated encumbrance");var v=bytes.Slice(Position,n);Position+=n;return v;}
        public byte U8()=>Take(1)[0];public ushort U16()=>BinaryPrimitives.ReadUInt16LittleEndian(Take(2));public uint U32()=>BinaryPrimitives.ReadUInt32LittleEndian(Take(4));public int S32()=>BinaryPrimitives.ReadInt32LittleEndian(Take(4));
    }
    private static void Scan(ReadOnlySpan<byte> bytes,CreatureEncumbrancesView4Limits l)
    {
        var r=new Reader(bytes);int fields=0;long commands=0,values=0;
        while(r.Remaining>0){ushort field=r.U16();if(field != 2)throw new InvalidDataException("Unsupported encumbrance delta field");fields++;uint extent=r.U32();_=r.U32();ulong cursor=0;Budget(r.Position,fields,commands,values,l);
            while(cursor<extent){byte op=r.U8();cursor++;commands++;switch(op){case 0:_=r.U16();break;case 1:case 2:_=r.U16();values++;r.Take(4);break;case 3:ushort n=r.U16();values+=n;cursor+=n;Budget(r.Position+4L*n,fields,commands,values,l);r.Take(4*n);break;case 4:break;default:throw new InvalidDataException("Unknown encumbrance command");}Budget(r.Position,fields,commands,values,l);}}
        if(fields==0)throw new InvalidDataException("Empty encumbrance delta");Budget(bytes.Length,fields,commands,values,l);
    }
    public static CreatureEncumbrancesView4Delta Decode(ReadOnlySpan<byte> wire,CreatureEncumbrancesView4Limits l)
    {Validate(l);return Decode(ReplicationEnvelopeCodec.DecodeDelta(wire,l.MaximumPayloadBytes),l);}
    public static CreatureEncumbrancesView4Delta Decode(ReplicationEnvelope envelope,CreatureEncumbrancesView4Limits l)
    {
        Validate(l);ArgumentNullException.ThrowIfNull(envelope);if(envelope.Kind!=ReplicationEnvelopeKind.Delta || envelope.RawTag!=CreatureView4Codec.RawTag || envelope.View!=4 || envelope.PayloadSize>l.MaximumPayloadBytes)throw new InvalidDataException("Expected CREO4 encumbrance delta");var fields=envelope.CopyOpaqueFields();Scan(fields,l);var r=new Reader(fields);var updates=new List<CreatureEncumbrancesView4Update>();
        while(r.Remaining>0){ushort field=r.U16();uint extent=r.U32(),counter=r.U32();ulong cursor=0;var commands=new List<CreatureEncumbrancesView4Command>();while(cursor<extent){byte op=r.U8();cursor++;ushort index=0;int[] values=[];if(op is 0 or 1 or 2)index=r.U16();if(op is 1 or 2)values=[r.S32()];if(op==3){ushort n=r.U16();cursor+=n;values=new int[n];for(int i=0;i<n;i++)values[i]=r.S32();}commands.Add(new(op,index,values));}updates.Add(new(field,extent,counter,commands));}
        return new(envelope.ObjectId,envelope.DeclaredCount,updates);
    }
    public static ReplicationEnvelope EncodeEnvelope(CreatureEncumbrancesView4Delta plan,CreatureEncumbrancesView4Limits l)
    {var cost=Measure(plan,l);using var stream=new MemoryStream(checked((int)cost.Bytes));using var w=new BinaryWriter(stream);foreach(var u in plan.Updates){w.Write(u.Field);w.Write(u.Extent);w.Write(u.Counter);foreach(var c in u.Commands){w.Write(c.Opcode);if(c.Opcode!=4)w.Write(c.Opcode==3?(ushort)c.Values.Count:c.Index);foreach(int value in c.Values)w.Write(value);}}return new(ReplicationEnvelopeKind.Delta,plan.ObjectId,CreatureView4Codec.RawTag,4,plan.DeclaredCount,stream.ToArray(),l.MaximumPayloadBytes);}
    public static byte[] Encode(CreatureEncumbrancesView4Delta plan,CreatureEncumbrancesView4Limits l)=>ReplicationEnvelopeCodec.EncodeDelta(EncodeEnvelope(plan,l),l.MaximumPayloadBytes);
}
