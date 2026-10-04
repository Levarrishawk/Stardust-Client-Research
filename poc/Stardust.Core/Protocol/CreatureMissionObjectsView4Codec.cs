using System.Buffers.Binary;
namespace Stardust.Core.Protocol;
public sealed record CreatureMissionObjectsView4Limits(int MaximumPayloadBytes=1048576,int MaximumFields=256,int MaximumCommands=4096,long MaximumDecodedBytes=2097152,long MaximumWork=1048576);
public sealed class CreatureMissionObjectsView4Command
{
    public CreatureMissionObjectsView4Command(byte opcode,CreatureView4MissionPair? pair)
    {if(opcode>2 || (opcode==2)!=(pair is null))throw new InvalidDataException("Mission command shape");Opcode=opcode;Pair=pair;}
    public byte Opcode{get;}public CreatureView4MissionPair? Pair{get;}
}
public sealed class CreatureMissionObjectsView4Update
{
    public CreatureMissionObjectsView4Update(ushort field,uint extent,uint counter,IEnumerable<CreatureMissionObjectsView4Command> commands)
    {if(field!=13)throw new InvalidDataException("Mission field");Field=field;Extent=extent;Counter=counter;Commands=Array.AsReadOnly(CreatureMissionObjectsView4Codec.Freeze(commands,65536));if(Commands.Any(c=>c is null))throw new InvalidDataException("Null mission command");}
    public ushort Field{get;}public uint Extent{get;}public uint Counter{get;}public IReadOnlyList<CreatureMissionObjectsView4Command> Commands{get;}
}
public sealed class CreatureMissionObjectsView4Delta
{
    public CreatureMissionObjectsView4Delta(ulong objectId,ushort declaredCount,IEnumerable<CreatureMissionObjectsView4Update> updates)
    {ObjectId=objectId;DeclaredCount=declaredCount;Updates=Array.AsReadOnly(CreatureMissionObjectsView4Codec.Freeze(updates,4096));if(Updates.Any(u=>u is null))throw new InvalidDataException("Null mission update");}
    public ulong ObjectId{get;}public ushort DeclaredCount{get;}public IReadOnlyList<CreatureMissionObjectsView4Update> Updates{get;}
}
public static class CreatureMissionObjectsView4Codec
{
    internal static T[] Freeze<T>(IEnumerable<T> values,int maximum)
    {ArgumentNullException.ThrowIfNull(values);var copy=new List<T>();foreach(var value in values){if(copy.Count==maximum)throw new InvalidDataException("Mission collection hard cap");copy.Add(value);}return copy.ToArray();}
    internal static void Validate(CreatureMissionObjectsView4Limits l)
    {ArgumentNullException.ThrowIfNull(l);if(l.MaximumPayloadBytes is <2 or >8388608 || l.MaximumFields is <0 or >4096 || l.MaximumCommands is <0 or >65536 || l.MaximumDecodedBytes is <0 or >16777216 || l.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(l));}
    private static void Budget(long bytes,int fields,long commands,CreatureMissionObjectsView4Limits l)
    {if(bytes+2>l.MaximumPayloadBytes || fields>l.MaximumFields || commands>l.MaximumCommands || bytes+24*commands>l.MaximumDecodedBytes || fields+commands>l.MaximumWork)throw new InvalidDataException("Mission aggregate message budget");}
    internal static (long Bytes,long Commands,long Work) Measure(CreatureMissionObjectsView4Delta plan,CreatureMissionObjectsView4Limits l)
    {
        Validate(l);ArgumentNullException.ThrowIfNull(plan);if(plan.Updates.Count==0)throw new InvalidDataException("Empty mission delta");long bytes=0,commands=0;Budget(0,plan.Updates.Count,0,l);
        foreach(var u in plan.Updates){if((uint)u.Commands.Count!=u.Extent)throw new InvalidDataException("Mission extent mismatch");bytes+=10;Budget(bytes,plan.Updates.Count,commands,l);foreach(var c in u.Commands){commands++;bytes+=c.Opcode==2?1:17;Budget(bytes,plan.Updates.Count,commands,l);}}
        return(bytes,commands,plan.Updates.Count+commands);
    }
    private ref struct Reader(ReadOnlySpan<byte> source)
    {
        private readonly ReadOnlySpan<byte> source=source;public int Position{get;private set;}public int Remaining=>source.Length-Position;
        public ReadOnlySpan<byte> Take(int n){if(n<0||n>Remaining)throw new InvalidDataException("Truncated mission delta");var value=source.Slice(Position,n);Position+=n;return value;}
        public byte U8()=>Take(1)[0];public ushort U16()=>BinaryPrimitives.ReadUInt16LittleEndian(Take(2));public uint U32()=>BinaryPrimitives.ReadUInt32LittleEndian(Take(4));public ulong U64()=>BinaryPrimitives.ReadUInt64LittleEndian(Take(8));
    }
    private static void Scan(ReadOnlySpan<byte> fields,CreatureMissionObjectsView4Limits l)
    {
        var r=new Reader(fields);int count=0;long commands=0;
        while(r.Remaining>0){if(r.U16()!=13)throw new InvalidDataException("Unsupported mission field");count++;uint extent=r.U32();_=r.U32();Budget(r.Position,count,commands,l);for(uint n=0;n<extent;n++){byte op=r.U8();if(op>2)throw new InvalidDataException("Unknown mission opcode");commands++;Budget(r.Position+(op==2?0L:16L),count,commands,l);if(op!=2)r.Take(16);}}
        if(count==0)throw new InvalidDataException("Empty mission delta");Budget(r.Position,count,commands,l);
    }
    public static CreatureMissionObjectsView4Delta Decode(ReadOnlySpan<byte> wire,CreatureMissionObjectsView4Limits l)
    {Validate(l);return Decode(ReplicationEnvelopeCodec.DecodeDelta(wire,l.MaximumPayloadBytes),l);}
    public static CreatureMissionObjectsView4Delta Decode(ReplicationEnvelope envelope,CreatureMissionObjectsView4Limits l)
    {
        Validate(l);ArgumentNullException.ThrowIfNull(envelope);if(envelope.Kind!=ReplicationEnvelopeKind.Delta || envelope.RawTag!=CreatureView4Codec.RawTag || envelope.View!=4 || envelope.PayloadSize>l.MaximumPayloadBytes)throw new InvalidDataException("Expected CREO4 mission delta");byte[] fields=envelope.CopyOpaqueFields();Scan(fields,l);var r=new Reader(fields);var updates=new List<CreatureMissionObjectsView4Update>();
        while(r.Remaining>0){ushort field=r.U16();uint extent=r.U32(),counter=r.U32();var commands=new List<CreatureMissionObjectsView4Command>();for(uint n=0;n<extent;n++){byte op=r.U8();CreatureView4MissionPair? pair=op==2?null:new(r.U64(),r.U64());commands.Add(new(op,pair));}updates.Add(new(field,extent,counter,commands));}
        return new(envelope.ObjectId,envelope.DeclaredCount,updates);
    }
    public static ReplicationEnvelope EncodeEnvelope(CreatureMissionObjectsView4Delta plan,CreatureMissionObjectsView4Limits l)
    {
        var cost=Measure(plan,l);using var stream=new MemoryStream(checked((int)cost.Bytes));using var w=new BinaryWriter(stream);
        foreach(var u in plan.Updates){w.Write(u.Field);w.Write(u.Extent);w.Write(u.Counter);foreach(var c in u.Commands){w.Write(c.Opcode);if(c.Pair is {} pair){w.Write(pair.OwnerId);w.Write(pair.MissionId);}}}
        return new(ReplicationEnvelopeKind.Delta,plan.ObjectId,CreatureView4Codec.RawTag,4,plan.DeclaredCount,stream.ToArray(),l.MaximumPayloadBytes);
    }
    public static byte[] Encode(CreatureMissionObjectsView4Delta plan,CreatureMissionObjectsView4Limits l)=>ReplicationEnvelopeCodec.EncodeDelta(EncodeEnvelope(plan,l),l.MaximumPayloadBytes);
}
