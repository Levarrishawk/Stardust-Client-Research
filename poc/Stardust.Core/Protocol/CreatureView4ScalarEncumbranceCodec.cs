using System.Buffers.Binary;
namespace Stardust.Core.Protocol;

public sealed record CreatureView4ScalarEncumbranceLimits(int MaximumPayloadBytes=1048576,
    int MaximumFields=256,int MaximumCommands=4096,int MaximumValues=65536,
    long MaximumDecodedBytes=2097152,long MaximumWork=1048576);
public sealed class CreatureView4ScalarEncumbranceUpdate
{
    public CreatureView4ScalarEncumbranceUpdate(CreatureView4Update scalar)
    {ArgumentNullException.ThrowIfNull(scalar);Scalar=scalar;Index=scalar.Index;}
    public CreatureView4ScalarEncumbranceUpdate(CreatureEncumbrancesView4Update encumbrance)
    {ArgumentNullException.ThrowIfNull(encumbrance);Encumbrance=encumbrance;Index=2;}
    public ushort Index{get;} public CreatureView4Update? Scalar{get;}
    public CreatureEncumbrancesView4Update? Encumbrance{get;}
}
public sealed class CreatureView4ScalarEncumbranceDelta
{
    public CreatureView4ScalarEncumbranceDelta(ulong objectId,ushort declaredCount,
        IEnumerable<CreatureView4ScalarEncumbranceUpdate> updates)
    {ObjectId=objectId;DeclaredCount=declaredCount;var copy=CreatureView4Codec.Freeze(updates,4096);if(copy.Any(u=>u is null))throw new InvalidDataException("Null composed CREO4 update");Updates=Array.AsReadOnly(copy);}
    public ulong ObjectId{get;} public ushort DeclaredCount{get;}
    public IReadOnlyList<CreatureView4ScalarEncumbranceUpdate> Updates{get;}
}
public static class CreatureView4ScalarEncumbranceCodec
{
    internal static void Validate(CreatureView4ScalarEncumbranceLimits l)
    {ArgumentNullException.ThrowIfNull(l);if(l.MaximumPayloadBytes is <2 or >8388608||l.MaximumFields is <0 or >4096||l.MaximumCommands is <0 or >65536||l.MaximumValues is <0 or >1048576||l.MaximumDecodedBytes is <0 or >16777216||l.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(l));}
    private sealed class Meter(CreatureView4ScalarEncumbranceLimits limits)
    {
        public long Bytes{get;private set;} private long fields,commands,values;
        public long Work=>fields+commands+values;
        private void Check(){if(Bytes+2>limits.MaximumPayloadBytes||Bytes+10>limits.MaximumDecodedBytes||fields>limits.MaximumFields||commands>limits.MaximumCommands||values>limits.MaximumValues||Work>limits.MaximumWork)throw new InvalidDataException("Composed CREO4 global message budget");}
        public void Add(long n){Bytes=checked(Bytes+n);Check();}
        public void Field(){fields++;Check();} public void Command(){commands++;Check();}
        public void Values(int n){values+=n;Check();}
    }
    private ref struct Reader(ReadOnlySpan<byte> bytes,Meter meter)
    {
        private readonly ReadOnlySpan<byte> bytes=bytes;private readonly Meter meter=meter;
        private int pos;public int Remaining=>bytes.Length-pos;
        public ReadOnlySpan<byte> Take(int n){if(n<0||n>Remaining)throw new InvalidDataException("Truncated composed CREO4");var b=bytes.Slice(pos,n);meter.Add(n);pos+=n;return b;}
        public byte U8()=>Take(1)[0];public ushort U16()=>BinaryPrimitives.ReadUInt16LittleEndian(Take(2));
        public uint U32()=>BinaryPrimitives.ReadUInt32LittleEndian(Take(4));public ulong U64()=>BinaryPrimitives.ReadUInt64LittleEndian(Take(8));
        public void Field()=>meter.Field();public void Command()=>meter.Command();public void Values(int n)=>meter.Values(n);
    }
    private static CreatureView4ScalarEncumbranceDelta? Read(ref Reader r,bool own,ulong id,ushort metadata)
    {
        var updates=own?new List<CreatureView4ScalarEncumbranceUpdate>():null;int fields=0;
        while(r.Remaining>0)
        {
            r.Field();fields++;ushort field=r.U16();
            if(field==2)
            {
                uint extent=r.U32(),counter=r.U32();ulong cursor=0;
                var commands=own?new List<CreatureEncumbrancesView4Command>():null;
                while(cursor<extent)
                {
                    r.Command();byte op=r.U8();cursor++;ushort index=0;int count=0;
                    if(op<=2)index=r.U16();
                    switch(op){case 0:case 4:break;case 1:case 2:count=1;break;case 3:count=r.U16();cursor+=(uint)count;break;default:throw new InvalidDataException("Unknown composed CREO4 encumbrance command");}
                    r.Values(count);var bytes=r.Take(4*count);int[]? values=own?new int[count]:null;
                    if(own){for(int i=0;i<count;i++)values![i]=BinaryPrimitives.ReadInt32LittleEndian(bytes[(4*i)..]);commands!.Add(new(op,index,values!));}
                }
                if(own)updates!.Add(new(new CreatureEncumbrancesView4Update(2,extent,counter,commands!)));
            }
            else
            {
                if(field!=6)_=CreatureView4Codec.FloatSlot(field); // 3/13 and unknown reject during the allocation-free scan.
                ulong bits=field==6?r.U64():r.U32();if(own)updates!.Add(new(new CreatureView4Update(field,bits)));
            }
        }
        if(fields==0)throw new InvalidDataException("Empty composed CREO4 delta");
        return own?new(id,metadata,updates!):null;
    }
    public static CreatureView4ScalarEncumbranceDelta Decode(ReadOnlySpan<byte> wire,CreatureView4ScalarEncumbranceLimits limits)
    {Validate(limits);return Decode(ReplicationEnvelopeCodec.DecodeDelta(wire,limits.MaximumPayloadBytes),limits);}
    public static CreatureView4ScalarEncumbranceDelta Decode(ReplicationEnvelope envelope,CreatureView4ScalarEncumbranceLimits limits)
    {
        Validate(limits);ArgumentNullException.ThrowIfNull(envelope);
        if(envelope.Kind!=ReplicationEnvelopeKind.Delta||envelope.RawTag!=CreatureView4Codec.RawTag||envelope.View!=4||envelope.PayloadSize>limits.MaximumPayloadBytes||envelope.OpaqueFieldLength+10L>limits.MaximumDecodedBytes)
            throw new InvalidDataException("Expected bounded composed CREO4 delta");
        var bytes=envelope.CopyOpaqueFields();var scan=new Reader(bytes,new(limits));_=Read(ref scan,false,envelope.ObjectId,envelope.DeclaredCount);
        var reader=new Reader(bytes,new(limits));return Read(ref reader,true,envelope.ObjectId,envelope.DeclaredCount)!;
    }
    private sealed class Writer(BinaryWriter? output,Meter meter)
    {
        public void Field()=>meter.Field();public void Command()=>meter.Command();public void Values(int n)=>meter.Values(n);
        public void U8(byte v){meter.Add(1);output?.Write(v);}public void U16(ushort v){meter.Add(2);output?.Write(v);}
        public void U32(uint v){meter.Add(4);output?.Write(v);}public void U64(ulong v){meter.Add(8);output?.Write(v);}
        public void S32(int v){meter.Add(4);output?.Write(v);}
    }
    private static void Write(Writer w,CreatureView4ScalarEncumbranceDelta plan)
    {
        if(plan.Updates.Count==0)throw new InvalidDataException("Empty composed CREO4 delta");
        foreach(var u in plan.Updates)
        {
            w.Field();w.U16(u.Index);
            if(u.Scalar is {} s){if(s.Index==6)w.U64(s.Bits);else{_=CreatureView4Codec.FloatSlot(s.Index);w.U32((uint)s.Bits);}continue;}
            var v=u.Encumbrance!;w.U32(v.Extent);w.U32(v.Counter);ulong cursor=0;
            foreach(var c in v.Commands){if(cursor>=v.Extent)throw new InvalidDataException("Extra composed encumbrance command");cursor+=1UL+(c.Opcode==3?(ulong)c.Values.Count:0);w.Command();w.Values(c.Values.Count);w.U8(c.Opcode);if(c.Opcode!=4)w.U16(c.Opcode==3?(ushort)c.Values.Count:c.Index);foreach(int value in c.Values)w.S32(value);}
            if(cursor<v.Extent)throw new InvalidDataException("Incomplete composed encumbrance extent");
        }
    }
    internal static (long Bytes,long Work) Measure(CreatureView4ScalarEncumbranceDelta plan,CreatureView4ScalarEncumbranceLimits limits)
    {Validate(limits);ArgumentNullException.ThrowIfNull(plan);var meter=new Meter(limits);Write(new(null,meter),plan);return(meter.Bytes,meter.Work);}
    public static ReplicationEnvelope EncodeEnvelope(CreatureView4ScalarEncumbranceDelta plan,CreatureView4ScalarEncumbranceLimits limits)
    {var cost=Measure(plan,limits);using var stream=new MemoryStream(checked((int)cost.Bytes));using var output=new BinaryWriter(stream);Write(new(output,new(limits)),plan);return new(ReplicationEnvelopeKind.Delta,plan.ObjectId,CreatureView4Codec.RawTag,4,plan.DeclaredCount,stream.ToArray(),limits.MaximumPayloadBytes);}
    public static byte[] Encode(CreatureView4ScalarEncumbranceDelta plan,CreatureView4ScalarEncumbranceLimits limits)
        =>ReplicationEnvelopeCodec.EncodeDelta(EncodeEnvelope(plan,limits),limits.MaximumPayloadBytes);
}
