using System.Buffers.Binary;
namespace Stardust.Core.Protocol;

public sealed record CreatureSkillModsView4Limits(int MaximumPayloadBytes=1048576,int MaximumFields=256,
    int MaximumCommands=4096,int MaximumKeyBytes=65536,int MaximumTotalKeyBytes=262144,
    long MaximumDecodedBytes=2097152,long MaximumWork=1048576);
public sealed class CreatureSkillModsView4Command
{
    public CreatureSkillModsView4Command(byte opcode,ReplicationByteString name,int baseValue,int bonus)
    {ArgumentNullException.ThrowIfNull(name);if(opcode>2)throw new InvalidDataException("Skill map opcode");Opcode=opcode;Name=name;BaseValue=baseValue;Bonus=bonus;}
    public byte Opcode{get;} public ReplicationByteString Name{get;} public int BaseValue{get;} public int Bonus{get;}
}
public sealed class CreatureSkillModsView4Update
{
    public CreatureSkillModsView4Update(ushort field,uint extent,uint counter,IEnumerable<CreatureSkillModsView4Command> commands)
    {if(field!=3)throw new InvalidDataException("Skill map field");Field=field;Extent=extent;Counter=counter;Commands=Array.AsReadOnly(CreatureSkillModsView4Codec.Freeze(commands,65536));if(Commands.Any(c=>c is null))throw new InvalidDataException("Null skill command");}
    public ushort Field{get;} public uint Extent{get;} public uint Counter{get;} public IReadOnlyList<CreatureSkillModsView4Command> Commands{get;}
}
public sealed class CreatureSkillModsView4Delta
{
    public CreatureSkillModsView4Delta(ulong objectId,ushort declaredCount,IEnumerable<CreatureSkillModsView4Update> updates)
    {ObjectId=objectId;DeclaredCount=declaredCount;Updates=Array.AsReadOnly(CreatureSkillModsView4Codec.Freeze(updates,4096));if(Updates.Any(u=>u is null))throw new InvalidDataException("Null skill update");}
    public ulong ObjectId{get;} public ushort DeclaredCount{get;} public IReadOnlyList<CreatureSkillModsView4Update> Updates{get;}
}
public static class CreatureSkillModsView4Codec
{
    internal static T[] Freeze<T>(IEnumerable<T> source,int maximum)
    {ArgumentNullException.ThrowIfNull(source);var values=new List<T>();foreach(var value in source){if(values.Count==maximum)throw new InvalidDataException("Skill owned collection cap");values.Add(value);}return values.ToArray();}
    internal static void Validate(CreatureSkillModsView4Limits l)
    {ArgumentNullException.ThrowIfNull(l);if(l.MaximumPayloadBytes is <2 or >8388608 || l.MaximumFields is <0 or >4096 || l.MaximumCommands is <0 or >65536 || l.MaximumKeyBytes is <0 or >1048576 || l.MaximumTotalKeyBytes is <0 or >4194304 || l.MaximumDecodedBytes is <0 or >16777216 || l.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(l));}
    private static void Budget(long bytes,int fields,long commands,long keys,CreatureSkillModsView4Limits l)
    {if(bytes+2>l.MaximumPayloadBytes || fields>l.MaximumFields || commands>l.MaximumCommands || keys>l.MaximumTotalKeyBytes || bytes+24*commands>l.MaximumDecodedBytes || fields+commands+keys>l.MaximumWork)throw new InvalidDataException("Skill aggregate message budget");}
    internal static (long Bytes,long Commands,long KeyBytes,long Work) Measure(CreatureSkillModsView4Delta plan,CreatureSkillModsView4Limits l)
    {
        Validate(l);ArgumentNullException.ThrowIfNull(plan);if(plan.Updates.Count==0)throw new InvalidDataException("Empty skill delta");long bytes=0,commands=0,keys=0;Budget(0,plan.Updates.Count,0,0,l);
        foreach(var u in plan.Updates){if((uint)u.Commands.Count!=u.Extent)throw new InvalidDataException("Skill extent mismatch");bytes+=10;Budget(bytes,plan.Updates.Count,commands,keys,l);foreach(var c in u.Commands){if(c.Name.Length>l.MaximumKeyBytes)throw new InvalidDataException("Skill key cap");commands++;keys+=c.Name.Length;bytes+=9+(c.Name.UsesExtendedLength?6:2)+c.Name.Length;Budget(bytes,plan.Updates.Count,commands,keys,l);}}
        return(bytes,commands,keys,plan.Updates.Count+commands+keys);
    }
    private ref struct Reader(ReadOnlySpan<byte> source)
    {
        private readonly ReadOnlySpan<byte> source=source;public int Position{get;private set;}public int Remaining=>source.Length-Position;
        public ReadOnlySpan<byte> Take(int n){if(n<0||n>Remaining)throw new InvalidDataException("Truncated skill delta");var value=source.Slice(Position,n);Position+=n;return value;}
        public byte U8()=>Take(1)[0];public ushort U16()=>BinaryPrimitives.ReadUInt16LittleEndian(Take(2));public uint U32()=>BinaryPrimitives.ReadUInt32LittleEndian(Take(4));public int S32()=>BinaryPrimitives.ReadInt32LittleEndian(Take(4));
        public int KeyLength(out bool extended){ushort shortLength=U16();extended=shortLength==65535;uint length=extended?U32():shortLength;if(length>1048576)throw new InvalidDataException("Skill key hard cap");return (int)length;}
    }
    private static void Scan(ReadOnlySpan<byte> fields,CreatureSkillModsView4Limits l)
    {
        var r=new Reader(fields);int count=0;long commands=0,keys=0;
        while(r.Remaining>0){if(r.U16()!=3)throw new InvalidDataException("Unsupported skill field");count++;uint extent=r.U32();_=r.U32();Budget(r.Position,count,commands,keys,l);
            for(uint i=0;i<extent;i++){if(r.U8()>2)throw new InvalidDataException("Unknown skill opcode");commands++;int length=r.KeyLength(out _);if(length>l.MaximumKeyBytes)throw new InvalidDataException("Skill key cap");keys+=length;Budget(r.Position+(long)length+8,count,commands,keys,l);r.Take(length);r.Take(8);}}
        if(count==0)throw new InvalidDataException("Empty skill delta");Budget(r.Position,count,commands,keys,l);
    }
    public static CreatureSkillModsView4Delta Decode(ReadOnlySpan<byte> wire,CreatureSkillModsView4Limits l)
    {Validate(l);return Decode(ReplicationEnvelopeCodec.DecodeDelta(wire,l.MaximumPayloadBytes),l);}
    public static CreatureSkillModsView4Delta Decode(ReplicationEnvelope envelope,CreatureSkillModsView4Limits l)
    {
        Validate(l);ArgumentNullException.ThrowIfNull(envelope);if(envelope.Kind!=ReplicationEnvelopeKind.Delta || envelope.RawTag!=CreatureView4Codec.RawTag || envelope.View!=4 || envelope.PayloadSize>l.MaximumPayloadBytes)throw new InvalidDataException("Expected CREO4 skill delta");
        byte[] fields=envelope.CopyOpaqueFields();Scan(fields,l);var r=new Reader(fields);var updates=new List<CreatureSkillModsView4Update>();
        while(r.Remaining>0){ushort field=r.U16();uint extent=r.U32(),counter=r.U32();var commands=new List<CreatureSkillModsView4Command>();for(uint i=0;i<extent;i++){byte op=r.U8();int length=r.KeyLength(out bool extended);var key=ReplicationByteString.Create(r.Take(length),extended);commands.Add(new(op,key,r.S32(),r.S32()));}updates.Add(new(field,extent,counter,commands));}
        return new(envelope.ObjectId,envelope.DeclaredCount,updates);
    }
    public static ReplicationEnvelope EncodeEnvelope(CreatureSkillModsView4Delta plan,CreatureSkillModsView4Limits l)
    {
        var cost=Measure(plan,l);using var stream=new MemoryStream(checked((int)cost.Bytes));using var w=new BinaryWriter(stream);
        foreach(var u in plan.Updates){w.Write(u.Field);w.Write(u.Extent);w.Write(u.Counter);foreach(var c in u.Commands){w.Write(c.Opcode);if(c.Name.UsesExtendedLength){w.Write(ushort.MaxValue);w.Write((uint)c.Name.Length);}else w.Write((ushort)c.Name.Length);w.Write(c.Name.CopyBytes());w.Write(c.BaseValue);w.Write(c.Bonus);}}
        return new(ReplicationEnvelopeKind.Delta,plan.ObjectId,CreatureView4Codec.RawTag,4,plan.DeclaredCount,stream.ToArray(),l.MaximumPayloadBytes);
    }
    public static byte[] Encode(CreatureSkillModsView4Delta plan,CreatureSkillModsView4Limits l)=>ReplicationEnvelopeCodec.EncodeDelta(EncodeEnvelope(plan,l),l.MaximumPayloadBytes);
}
