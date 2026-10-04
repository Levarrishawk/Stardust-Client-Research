using System.Buffers.Binary;
namespace Stardust.Core.Protocol;
public sealed record CreatureDefendersView6Limits(int MaximumPayloadBytes=16384,int MaximumFields=256,int MaximumCommands=4096,int MaximumValues=65536,long MaximumDecodedBytes=1048576,long MaximumWork=1048576);
public sealed class CreatureDefendersView6Command
{
    public CreatureDefendersView6Command(byte opcode,ushort index,IEnumerable<ulong> values)
    {var copy=TangibleView6Codec.Freeze(values,65535);if(opcode>4 || (opcode is 0 or 4 && copy.Length!=0) || (opcode is 1 or 2 && copy.Length!=1) || (opcode is 3 or 4 && index!=0))throw new InvalidDataException("defender command shape");Opcode=opcode;Index=index;Values=Array.AsReadOnly(copy);}
    public byte Opcode{get;}public ushort Index{get;}public IReadOnlyList<ulong> Values{get;}
}
public sealed class CreatureDefendersView6Update
{
    public CreatureDefendersView6Update(ushort field,uint extent,uint counter,IEnumerable<CreatureDefendersView6Command> commands)
    {if(field!=1)throw new InvalidDataException("defender field");Field=field;Extent=extent;Counter=counter;var copy=TangibleView6Codec.Freeze(commands,65536);if(copy.Any(c=>c is null))throw new InvalidDataException("Null defender command");Commands=Array.AsReadOnly(copy);}
    public ushort Field{get;}public uint Extent{get;}public uint Counter{get;}public IReadOnlyList<CreatureDefendersView6Command> Commands{get;}
}
public sealed class CreatureDefendersView6Delta
{
    public CreatureDefendersView6Delta(ulong objectId,ushort declaredCount,IEnumerable<CreatureDefendersView6Update> updates)
    {ObjectId=objectId;DeclaredCount=declaredCount;var copy=TangibleView6Codec.Freeze(updates,4096);if(copy.Any(c=>c is null))throw new InvalidDataException("Null defender update");Updates=Array.AsReadOnly(copy);}
    public ulong ObjectId{get;}public ushort DeclaredCount{get;}public IReadOnlyList<CreatureDefendersView6Update> Updates{get;}
}
public static class CreatureDefendersView6Codec
{
    internal static void Validate(CreatureDefendersView6Limits l)
    {ArgumentNullException.ThrowIfNull(l);ReplicationEnvelopeCodec.ValidateBudget(l.MaximumPayloadBytes);if(l.MaximumPayloadBytes>8388608 || l.MaximumFields is <0 or >4096 || l.MaximumCommands is <0 or >65536 || l.MaximumValues is <0 or >1048576 || l.MaximumDecodedBytes is <0 or >16777216 || l.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(l));}
    private static void Budget(long bytes,int fields,long commands,long values,CreatureDefendersView6Limits l)
    {if(bytes+2>l.MaximumPayloadBytes || bytes>l.MaximumDecodedBytes || fields>l.MaximumFields || commands>l.MaximumCommands || values>l.MaximumValues || fields+commands+values>l.MaximumWork)throw new InvalidDataException("defender global message budget");}
    internal static (long Bytes,long Commands,long Values) Measure(CreatureDefendersView6Delta plan,CreatureDefendersView6Limits l)
    {
        Validate(l);ArgumentNullException.ThrowIfNull(plan);if(plan.Updates.Count==0)throw new InvalidDataException("Empty defender delta");long bytes=0,commands=0,values=0;
        Budget(bytes,plan.Updates.Count,commands,values,l);
        foreach(var u in plan.Updates){bytes+=10;ulong cursor=0;foreach(var c in u.Commands){if(cursor>=u.Extent)throw new InvalidDataException("Extra defender command");cursor+=1UL+(c.Opcode==3?(ulong)c.Values.Count:0);commands++;values+=c.Values.Count;bytes+=(c.Opcode==4?1:3)+8L*c.Values.Count;Budget(bytes,plan.Updates.Count,commands,values,l);}if(cursor<u.Extent)throw new InvalidDataException("Incomplete defender extent");}
        Budget(bytes,plan.Updates.Count,commands,values,l);return(bytes,commands,values);
    }
    private ref struct Reader(ReadOnlySpan<byte> bytes)
    {
        private readonly ReadOnlySpan<byte> bytes=bytes;public int Position{get;private set;}public int Remaining=>bytes.Length-Position;
        public ReadOnlySpan<byte> Take(int n){if(n<0||n>Remaining)throw new InvalidDataException("Truncated defender");var v=bytes.Slice(Position,n);Position+=n;return v;}
        public byte U8()=>Take(1)[0];public ushort U16()=>BinaryPrimitives.ReadUInt16LittleEndian(Take(2));public uint U32()=>BinaryPrimitives.ReadUInt32LittleEndian(Take(4));public ulong U64()=>BinaryPrimitives.ReadUInt64LittleEndian(Take(8));
    }
    private static void Scan(ReadOnlySpan<byte> bytes,CreatureDefendersView6Limits l)
    {
        var r=new Reader(bytes);int fields=0;long commands=0,values=0;
        while(r.Remaining>0){ushort field=r.U16();if(field!=1)throw new InvalidDataException("Unsupported defender delta field");fields++;uint extent=r.U32();_=r.U32();ulong cursor=0;Budget(r.Position,fields,commands,values,l);
            while(cursor<extent){byte op=r.U8();cursor++;commands++;switch(op){case 0:_=r.U16();break;case 1:case 2:_=r.U16();values++;r.Take(8);break;case 3:ushort n=r.U16();values+=n;cursor+=n;Budget(r.Position+8L*n,fields,commands,values,l);r.Take(8*n);break;case 4:break;default:throw new InvalidDataException("Unknown defender command");}Budget(r.Position,fields,commands,values,l);}}
        if(fields==0)throw new InvalidDataException("Empty defender delta");Budget(bytes.Length,fields,commands,values,l);
    }
    public static CreatureDefendersView6Delta Decode(ReadOnlySpan<byte> wire,CreatureDefendersView6Limits l)
    {Validate(l);return Decode(ReplicationEnvelopeCodec.DecodeDelta(wire,l.MaximumPayloadBytes),l);}
    public static CreatureDefendersView6Delta Decode(ReplicationEnvelope envelope,CreatureDefendersView6Limits l)
    {
        Validate(l);ArgumentNullException.ThrowIfNull(envelope);if(envelope.Kind!=ReplicationEnvelopeKind.Delta || envelope.RawTag!=CreatureView6Codec.RawTag || envelope.View!=6 || envelope.PayloadSize>l.MaximumPayloadBytes)throw new InvalidDataException("Expected CREO6 defender delta");Scan(envelope.Fields,l);var r=new Reader(envelope.Fields);var updates=new List<CreatureDefendersView6Update>();
        while(r.Remaining>0){ushort field=r.U16();uint extent=r.U32(),counter=r.U32();ulong cursor=0;var commands=new List<CreatureDefendersView6Command>();while(cursor<extent){byte op=r.U8();cursor++;ushort index=0;ulong[] values=[];if(op is 0 or 1 or 2)index=r.U16();if(op is 1 or 2)values=[r.U64()];if(op==3){ushort n=r.U16();cursor+=n;values=new ulong[n];for(int i=0;i<n;i++)values[i]=r.U64();}commands.Add(new(op,index,values));}updates.Add(new(field,extent,counter,commands));}
        return new(envelope.ObjectId,envelope.DeclaredCount,updates);
    }
    public static ReplicationEnvelope EncodeEnvelope(CreatureDefendersView6Delta plan,CreatureDefendersView6Limits l)
    {var cost=Measure(plan,l);using var stream=new MemoryStream(checked((int)cost.Bytes));using var w=new BinaryWriter(stream);foreach(var u in plan.Updates){w.Write(u.Field);w.Write(u.Extent);w.Write(u.Counter);foreach(var c in u.Commands){w.Write(c.Opcode);if(c.Opcode!=4)w.Write(c.Opcode==3?(ushort)c.Values.Count:c.Index);foreach(ulong value in c.Values)w.Write(value);}}return new(ReplicationEnvelopeKind.Delta,plan.ObjectId,CreatureView6Codec.RawTag,6,plan.DeclaredCount,stream.ToArray(),l.MaximumPayloadBytes);}
    public static byte[] Encode(CreatureDefendersView6Delta plan,CreatureDefendersView6Limits l)=>ReplicationEnvelopeCodec.EncodeDelta(EncodeEnvelope(plan,l),l.MaximumPayloadBytes);
}
