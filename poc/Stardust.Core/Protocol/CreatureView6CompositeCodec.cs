using System.Buffers.Binary;
namespace Stardust.Core.Protocol;
public sealed record CreatureView6CompositeLimits(int MaximumPayloadBytes=1048576,int MaximumFields=256,int MaximumStringBytes=65536,
    int MaximumTotalStringBytes=262144,int MaximumCommands=4096,int MaximumValues=65536,long MaximumDecodedBytes=2097152,long MaximumWork=1048576);
public sealed class CreatureView6CompositeUpdate
{
    public CreatureView6CompositeUpdate(CreatureView6Update scalar){ArgumentNullException.ThrowIfNull(scalar);Scalar=scalar;Index=scalar.Index;}
    public CreatureView6CompositeUpdate(CreatureHamView6Update ham){ArgumentNullException.ThrowIfNull(ham);Ham=ham;Index=ham.Field;}
    public ushort Index{get;}public CreatureView6Update? Scalar{get;}public CreatureHamView6Update? Ham{get;}
}
public sealed class CreatureView6CompositeDelta
{
    public CreatureView6CompositeDelta(ulong objectId,ushort declaredCount,IEnumerable<CreatureView6CompositeUpdate> updates)
    {ObjectId=objectId;DeclaredCount=declaredCount;var copy=TangibleView6Codec.Freeze(updates,4096);if(copy.Any(v=>v is null))throw new InvalidDataException("Null composite update");Updates=Array.AsReadOnly(copy);}
    public ulong ObjectId{get;}public ushort DeclaredCount{get;}public IReadOnlyList<CreatureView6CompositeUpdate> Updates{get;}
}
public static class CreatureView6CompositeCodec
{
    internal static void Validate(CreatureView6CompositeLimits l)
    {ArgumentNullException.ThrowIfNull(l);ReplicationEnvelopeCodec.ValidateBudget(l.MaximumPayloadBytes);if(l.MaximumPayloadBytes>8388608||l.MaximumFields is <0 or >4096||l.MaximumStringBytes is <0 or >1048576||l.MaximumTotalStringBytes is <0 or >4194304||l.MaximumCommands is <0 or >65536||l.MaximumValues is <0 or >1048576||l.MaximumDecodedBytes is <0 or >16777216||l.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(l));}
    private sealed class Meter(CreatureView6CompositeLimits limits)
    {
        public long Bytes{get;private set;}private long fields,commands,values,strings;
        public long Work=>fields+commands+values+strings;
        private void Check(){if(Bytes+2>limits.MaximumPayloadBytes||Bytes+10>limits.MaximumDecodedBytes||fields>limits.MaximumFields||commands>limits.MaximumCommands||values>limits.MaximumValues||strings>limits.MaximumTotalStringBytes||Work>limits.MaximumWork)throw new InvalidDataException("Composite global message budget");}
        public void Add(long n){Bytes=checked(Bytes+n);Check();}public void Field(){fields++;Check();}public void Command(){commands++;Check();}public void Values(int n){values+=n;Check();}
        public void String(uint n){if(n>limits.MaximumStringBytes)throw new InvalidDataException("Composite string budget");strings+=n;Check();}
    }
    private ref struct Reader(ReadOnlySpan<byte> source,Meter meter)
    {
        private readonly ReadOnlySpan<byte> source=source;private readonly Meter meter=meter;public int Position{get;private set;}public int Remaining=>source.Length-Position;
        public ReadOnlySpan<byte> Take(int n){if(n<0||n>Remaining)throw new InvalidDataException("Truncated composite");var v=source.Slice(Position,n);meter.Add(n);Position+=n;return v;}
        public byte U8()=>Take(1)[0];public ushort U16()=>BinaryPrimitives.ReadUInt16LittleEndian(Take(2));public uint U32()=>BinaryPrimitives.ReadUInt32LittleEndian(Take(4));public ulong U64()=>BinaryPrimitives.ReadUInt64LittleEndian(Take(8));public int S32()=>BinaryPrimitives.ReadInt32LittleEndian(Take(4));
        public void Field()=>meter.Field();public void Command()=>meter.Command();public void Values(int n)=>meter.Values(n);
        public ReplicationByteString? String(bool own){ushort n=U16();bool extended=n==65535;uint size=extended?U32():n;meter.String(size);var bytes=Take(checked((int)size));return own?ReplicationByteString.Create(bytes,extended):null;}
    }
    private static CreatureView6CompositeDelta? Read(ref Reader r,bool own,ulong id,ushort metadata)
    {
        var updates=own?new List<CreatureView6CompositeUpdate>():null;int fields=0;
        while(r.Remaining>0)
        {
            r.Field();fields++;ushort index=r.U16();
            if(index is 13 or 14)
            {
                uint extent=r.U32(),counter=r.U32();ulong cursor=0;var commands=own?new List<CreatureHamView6Command>():null;
                while(cursor<extent){r.Command();byte op=r.U8();cursor++;ushort ix=0;int[]? values=null;if(op<=2)ix=r.U16();switch(op){case 0:case 4:if(own)values=[];break;case 1:case 2:r.Values(1);int value=r.S32();if(own)values=[value];break;case 3:ushort n=r.U16();cursor+=n;r.Values(n);var bytes=r.Take(4*n);if(own){values=new int[n];for(int i=0;i<n;i++)values[i]=BinaryPrimitives.ReadInt32LittleEndian(bytes[(4*i)..]);}break;default:throw new InvalidDataException("Unknown composite HAM opcode");}if(own)commands!.Add(new(op,ix,values!));}
                if(own)updates!.Add(new(new CreatureHamView6Update(index,extent,counter,commands!)));
            }
            else if(index is 3 or 4 or 16){var text=r.String(own);if(own)updates!.Add(new(CreatureView6Update.String(index,text!)));}
            else if(index==7){ulong first=r.U64(),second=r.U64();if(own)updates!.Add(new(CreatureView6Update.Pair(first,second)));}
            else{int width=CreatureView6Codec.ScalarWidth(index);ulong bits=width switch{1=>r.U8(),2=>r.U16(),4=>r.U32(),8=>r.U64(),_=>throw new InvalidDataException("Unsupported composite field")};if(own)updates!.Add(new(CreatureView6Update.Scalar(index,bits)));}
        }
        if(fields==0)throw new InvalidDataException("Empty composite delta");return own?new(id,metadata,updates!):null;
    }
    public static CreatureView6CompositeDelta Decode(ReadOnlySpan<byte> wire,CreatureView6CompositeLimits limits)
    {Validate(limits);return Decode(ReplicationEnvelopeCodec.DecodeDelta(wire,limits.MaximumPayloadBytes),limits);}
    public static CreatureView6CompositeDelta Decode(ReplicationEnvelope envelope,CreatureView6CompositeLimits limits)
    {Validate(limits);ArgumentNullException.ThrowIfNull(envelope);if(envelope.Kind!=ReplicationEnvelopeKind.Delta||envelope.RawTag!=CreatureView6Codec.RawTag||envelope.View!=6||envelope.PayloadSize>limits.MaximumPayloadBytes||envelope.OpaqueFieldLength+10L>limits.MaximumDecodedBytes)throw new InvalidDataException("Expected bounded CREO6 composite delta");var scan=new Reader(envelope.Fields,new(limits));_=Read(ref scan,false,envelope.ObjectId,envelope.DeclaredCount);var reader=new Reader(envelope.Fields,new(limits));return Read(ref reader,true,envelope.ObjectId,envelope.DeclaredCount)!;}
    private sealed class Writer(BinaryWriter? output,Meter meter)
    {
        public void Field()=>meter.Field();public void Command()=>meter.Command();public void Values(int n)=>meter.Values(n);
        public void U8(byte v){meter.Add(1);output?.Write(v);}public void U16(ushort v){meter.Add(2);output?.Write(v);}public void U32(uint v){meter.Add(4);output?.Write(v);}public void U64(ulong v){meter.Add(8);output?.Write(v);}public void S32(int v){meter.Add(4);output?.Write(v);}
        public void String(ReplicationByteString s){meter.String((uint)s.Length);if(s.UsesExtendedLength){U16(65535);U32((uint)s.Length);}else U16(checked((ushort)s.Length));meter.Add(s.Length);output?.Write(s.Bytes);}
    }
    private static void Write(Writer w,CreatureView6CompositeDelta plan)
    {
        if(plan.Updates.Count==0)throw new InvalidDataException("Empty composite delta");
        foreach(var u in plan.Updates){w.Field();w.U16(u.Index);if(u.Ham is {} h){w.U32(h.Extent);w.U32(h.Counter);ulong cursor=0;foreach(var c in h.Commands){if(cursor>=h.Extent)throw new InvalidDataException("Extra composite HAM command");cursor+=1UL+(c.Opcode==3?(ulong)c.Values.Count:0);w.Command();w.Values(c.Values.Count);w.U8(c.Opcode);if(c.Opcode!=4)w.U16(c.Opcode==3?(ushort)c.Values.Count:c.Index);foreach(int value in c.Values)w.S32(value);}if(cursor<h.Extent)throw new InvalidDataException("Incomplete composite HAM extent");continue;}var s=u.Scalar!;if(s.Text is not null){w.String(s.Text);continue;}if(s.Index==7){w.U64(s.Bits);w.U64(s.SecondId);continue;}switch(CreatureView6Codec.ScalarWidth(s.Index)){case 1:w.U8((byte)s.Bits);break;case 2:w.U16((ushort)s.Bits);break;case 4:w.U32((uint)s.Bits);break;case 8:w.U64(s.Bits);break;default:throw new InvalidDataException("Composite scalar field");}}
    }
    internal static (long Bytes,long Work) Measure(CreatureView6CompositeDelta plan,CreatureView6CompositeLimits limits)
    {Validate(limits);ArgumentNullException.ThrowIfNull(plan);var meter=new Meter(limits);Write(new(null,meter),plan);return(meter.Bytes,meter.Work);}
    public static ReplicationEnvelope EncodeEnvelope(CreatureView6CompositeDelta plan,CreatureView6CompositeLimits limits)
    {var cost=Measure(plan,limits);using var stream=new MemoryStream(checked((int)cost.Bytes));using var writer=new BinaryWriter(stream);Write(new(writer,new(limits)),plan);return new(ReplicationEnvelopeKind.Delta,plan.ObjectId,CreatureView6Codec.RawTag,6,plan.DeclaredCount,stream.ToArray(),limits.MaximumPayloadBytes);}
    public static byte[] Encode(CreatureView6CompositeDelta plan,CreatureView6CompositeLimits limits)=>ReplicationEnvelopeCodec.EncodeDelta(EncodeEnvelope(plan,limits),limits.MaximumPayloadBytes);
}
