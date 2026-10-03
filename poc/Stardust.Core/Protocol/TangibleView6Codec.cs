using System.Buffers.Binary;
namespace Stardust.Core.Protocol;

public sealed record TangibleView6Limits(int MaximumPayloadBytes=16384,int MaximumFields=256,
    int MaximumOperations=4096,int MaximumAssignmentValues=65535,int MaximumIds=65536,long MaximumDecodedBytes=1048576);
public sealed class TangibleView6Baseline
{
    public TangibleView6Baseline(ulong objectId,ushort declaredCount,uint passive,uint counter,IEnumerable<ulong> ids)
    {ObjectId=objectId;DeclaredCount=declaredCount;Passive=passive;Counter=counter;Ids=Array.AsReadOnly(TangibleView6Codec.Freeze(ids,1048576));}
    public ulong ObjectId{get;} public ushort DeclaredCount{get;} public uint Passive{get;} public uint Counter{get;}
    public IReadOnlyList<ulong> Ids{get;} public long DecodedBytes=>12+8L*Ids.Count;
}
public sealed class TangibleView6Command
{
    public TangibleView6Command(byte opcode,ushort index,IEnumerable<ulong> values)
    {
        var copy=TangibleView6Codec.Freeze(values,65535);
        if(opcode>4 || (opcode is 0 or 4 && copy.Length!=0) || (opcode is 1 or 2 && copy.Length!=1) || (opcode is 3 or 4 && index!=0))throw new InvalidDataException("View6 command shape");
        Opcode=opcode;Index=index;Values=Array.AsReadOnly(copy);
    }
    public byte Opcode{get;} public ushort Index{get;} public IReadOnlyList<ulong> Values{get;}
}
public sealed class TangibleView6Update
{
    public TangibleView6Update(uint passive){Index=0;Passive=passive;Commands=Array.AsReadOnly(Array.Empty<TangibleView6Command>());}
    public TangibleView6Update(uint declaredOperations,uint counter,IEnumerable<TangibleView6Command> commands)
    {Index=1;DeclaredOperations=declaredOperations;Counter=counter;Commands=Array.AsReadOnly(TangibleView6Codec.Freeze(commands,65536));if(Commands.Any(c=>c is null))throw new InvalidDataException("Null command");}
    public ushort Index{get;} public uint Passive{get;} public uint DeclaredOperations{get;} public uint Counter{get;}
    public IReadOnlyList<TangibleView6Command> Commands{get;}
}
public sealed class TangibleView6Delta
{
    public TangibleView6Delta(ulong objectId,ushort declaredCount,IEnumerable<TangibleView6Update> updates)
    {ObjectId=objectId;DeclaredCount=declaredCount;Updates=Array.AsReadOnly(TangibleView6Codec.Freeze(updates,4096));if(Updates.Any(u=>u is null))throw new InvalidDataException("Null field");}
    public ulong ObjectId{get;} public ushort DeclaredCount{get;} public IReadOnlyList<TangibleView6Update> Updates{get;}
}
/// <summary>Exact owned TANO6 consume grammar only; no vector state application.</summary>
public static class TangibleView6Codec
{
    public const uint RawTag=0x54414E4F;public const byte View=6;
    internal static T[] Freeze<T>(IEnumerable<T> values,int maximum)
    {
        ArgumentNullException.ThrowIfNull(values);var result=new List<T>();
        foreach(var value in values){if(result.Count==maximum)throw new InvalidDataException("Frozen view6 capacity");result.Add(value);}
        return result.ToArray();
    }
    private static void Validate(TangibleView6Limits l)
    {
        ArgumentNullException.ThrowIfNull(l);ReplicationEnvelopeCodec.ValidateBudget(l.MaximumPayloadBytes);
        if(l.MaximumPayloadBytes>8388608 || l.MaximumFields is <0 or >4096 || l.MaximumOperations is <0 or >65536 || l.MaximumAssignmentValues is <0 or >65535 ||
            l.MaximumIds is <0 or >1048576 || l.MaximumDecodedBytes is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(l));
    }
    private static void Envelope(ReplicationEnvelope e,ReplicationEnvelopeKind kind,TangibleView6Limits l)
    {if(e.Kind!=kind || e.RawTag!=RawTag || e.View!=View || e.PayloadSize>l.MaximumPayloadBytes)throw new InvalidDataException("Expected bounded TANO6 envelope");}
    private static void Budget(long bytes,int fields,long ops,long assignment,long ids,TangibleView6Limits l)
    {if(bytes>l.MaximumDecodedBytes || bytes+2>l.MaximumPayloadBytes || fields>l.MaximumFields || ops>l.MaximumOperations || assignment>l.MaximumAssignmentValues || ids>l.MaximumIds)throw new InvalidDataException("View6 global budget");}
    public static TangibleView6Baseline DecodeBaseline(ReadOnlySpan<byte> wire,TangibleView6Limits l)
    {Validate(l);return DecodeBaseline(ReplicationEnvelopeCodec.DecodeBaseline(wire,l.MaximumPayloadBytes),l);}
    public static TangibleView6Baseline DecodeBaseline(ReplicationEnvelope e,TangibleView6Limits l)
    {
        Validate(l);Envelope(e,ReplicationEnvelopeKind.Baseline,l);var r=new Reader(e.Fields);uint passive=r.U32(),count=r.U32(),counter=r.U32();
        Budget(12+8L*count,2,0,0,count,l);r.Take(checked((int)(8L*count)));r.End();
        r=new Reader(e.Fields);_=r.U32();_=r.U32();_=r.U32();var ids=new ulong[(int)count];for(int i=0;i<ids.Length;i++)ids[i]=r.U64();
        return new(e.ObjectId,e.DeclaredCount,passive,counter,ids);
    }
    private static void Scan(ReadOnlySpan<byte> fields,TangibleView6Limits l)
    {
        var r=new Reader(fields);int fieldCount=0;long operations=0,assignment=0,ids=0;
        while(r.Remaining>0)
        {
            ushort index=r.U16();fieldCount++;
            if(index==0){_=r.U32();Budget(r.Position,fieldCount,operations,assignment,ids,l);continue;}
            if(index!=1)throw new InvalidDataException("Unknown TANO6 field");
            uint declared=r.U32();_=r.U32();ulong units=0;
            // Count is a raw loop extent. Assignment advances it by N+1, possibly past declared.
            while(units<declared)
            {
                byte op=r.U8();operations++;units++;
                switch(op)
                {
                    case 0:_=r.U16();break;
                    case 1:case 2:_=r.U16();ids++;r.Take(8);break;
                    case 3:ushort n=r.U16();assignment+=n;ids+=n;units+=n;r.Take(8*n);break;
                    case 4:break;
                    default:throw new InvalidDataException("Unknown CachedNetworkId command");
                }
                Budget(r.Position,fieldCount,operations,assignment,ids,l);
            }
            Budget(r.Position,fieldCount,operations,assignment,ids,l);
        }
        if(fieldCount==0)throw new InvalidDataException("View6 delta requires a field");
        Budget(fields.Length,fieldCount,operations,assignment,ids,l);r.End();
    }
    public static TangibleView6Delta DecodeDelta(ReadOnlySpan<byte> wire,TangibleView6Limits l)
    {Validate(l);return DecodeDelta(ReplicationEnvelopeCodec.DecodeDelta(wire,l.MaximumPayloadBytes),l);}
    public static TangibleView6Delta DecodeDelta(ReplicationEnvelope e,TangibleView6Limits l)
    {
        Validate(l);Envelope(e,ReplicationEnvelopeKind.Delta,l);Scan(e.Fields,l);var r=new Reader(e.Fields);var updates=new List<TangibleView6Update>();
        while(r.Remaining>0)
        {
            if(r.U16()==0){updates.Add(new(r.U32()));continue;}
            uint declared=r.U32(),counter=r.U32();ulong units=0;var commands=new List<TangibleView6Command>();
            while(units<declared)
            {
                byte op=r.U8();ushort index=0;ulong[] values=[];units++;
                if(op is 0 or 1 or 2)index=r.U16();
                if(op is 1 or 2)values=[r.U64()];
                if(op==3){ushort n=r.U16();units+=n;values=new ulong[n];for(int i=0;i<n;i++)values[i]=r.U64();}
                commands.Add(new(op,index,values));
            }
            updates.Add(new(declared,counter,commands));
        }
        return new(e.ObjectId,e.DeclaredCount,updates);
    }
    private static ReplicationEnvelope Wrap(ReplicationEnvelopeKind kind,ulong id,ushort count,byte[] fields,TangibleView6Limits l)=>new(kind,id,RawTag,View,count,fields,l.MaximumPayloadBytes);
    public static byte[] EncodeBaseline(TangibleView6Baseline v,TangibleView6Limits l)
    {
        Validate(l);ArgumentNullException.ThrowIfNull(v);Budget(v.DecodedBytes,2,0,0,v.Ids.Count,l);
        using var stream=new MemoryStream();using var w=new BinaryWriter(stream);w.Write(v.Passive);w.Write((uint)v.Ids.Count);w.Write(v.Counter);foreach(ulong id in v.Ids)w.Write(id);
        return ReplicationEnvelopeCodec.EncodeBaseline(Wrap(ReplicationEnvelopeKind.Baseline,v.ObjectId,v.DeclaredCount,stream.ToArray(),l),l.MaximumPayloadBytes);
    }
    public static byte[] EncodeDelta(TangibleView6Delta v,TangibleView6Limits l)
    {
        Validate(l);ArgumentNullException.ThrowIfNull(v);long size=0,ops=0,assignment=0,ids=0;
        Budget(0,v.Updates.Count,0,0,0,l);
        foreach(var u in v.Updates)
        {
            size+=u.Index==0?6:10;if(u.Index==0)continue;ulong units=0;
            foreach(var c in u.Commands)
            {
                if(units>=u.DeclaredOperations)throw new InvalidDataException("Commands beyond raw loop extent");
                units++;ops++;size+=c.Opcode==4?1:3;
                if(c.Opcode is 1 or 2){size+=8;ids++;}
                if(c.Opcode==3){units+=(uint)c.Values.Count;size+=8L*c.Values.Count;ids+=c.Values.Count;assignment+=c.Values.Count;}
                Budget(size,v.Updates.Count,ops,assignment,ids,l);
            }
            if(units<u.DeclaredOperations)throw new InvalidDataException("Incomplete raw loop extent");
        }
        if(v.Updates.Count==0)throw new InvalidDataException("View6 delta requires a field");
        Budget(size,v.Updates.Count,ops,assignment,ids,l);
        using var stream=new MemoryStream();using var w=new BinaryWriter(stream);
        foreach(var u in v.Updates)
        {
            w.Write(u.Index);if(u.Index==0){w.Write(u.Passive);continue;}w.Write(u.DeclaredOperations);w.Write(u.Counter);
            foreach(var c in u.Commands){w.Write(c.Opcode);if(c.Opcode!=4)w.Write(c.Opcode==3?(ushort)c.Values.Count:c.Index);foreach(ulong id in c.Values)w.Write(id);}
        }
        var fields=stream.ToArray();Scan(fields,l);
        return ReplicationEnvelopeCodec.EncodeDelta(Wrap(ReplicationEnvelopeKind.Delta,v.ObjectId,v.DeclaredCount,fields,l),l.MaximumPayloadBytes);
    }
    private ref struct Reader
    {
        private readonly ReadOnlySpan<byte> bytes;public int Position{get;private set;}public int Remaining=>bytes.Length-Position;
        public Reader(ReadOnlySpan<byte> value){bytes=value;Position=0;}
        public ReadOnlySpan<byte> Take(int n){if(n<0||n>Remaining)throw new InvalidDataException("Truncated TANO6");var value=bytes.Slice(Position,n);Position+=n;return value;}
        public byte U8()=>Take(1)[0];public ushort U16()=>BinaryPrimitives.ReadUInt16LittleEndian(Take(2));
        public uint U32()=>BinaryPrimitives.ReadUInt32LittleEndian(Take(4));public ulong U64()=>BinaryPrimitives.ReadUInt64LittleEndian(Take(8));
        public void End(){if(Remaining!=0)throw new InvalidDataException("Trailing TANO6 bytes");}
    }
}
