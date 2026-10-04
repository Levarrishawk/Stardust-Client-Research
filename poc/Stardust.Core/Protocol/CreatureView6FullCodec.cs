using System.Buffers.Binary;
namespace Stardust.Core.Protocol;
public sealed record CreatureView6FullLimits(int MaximumPayloadBytes=1048576,int MaximumFields=256,int MaximumStringBytes=65536,
    int MaximumTotalStringBytes=262144,int MaximumCommands=4096,int MaximumElements=65536,long MaximumDecodedBytes=2097152,long MaximumWork=1048576);
public sealed class CreatureView6FullUpdate
{
    public CreatureView6FullUpdate(CreatureView6Update scalar){ArgumentNullException.ThrowIfNull(scalar);Scalar=scalar;Index=scalar.Index;}
    public CreatureView6FullUpdate(CreatureHamView6Update ham){ArgumentNullException.ThrowIfNull(ham);Ham=ham;Index=ham.Field;}
    public CreatureView6FullUpdate(CreatureDefendersView6Update defender){ArgumentNullException.ThrowIfNull(defender);Defender=defender;Index=1;}
    public CreatureView6FullUpdate(CreatureWearablesView6Update wearable){ArgumentNullException.ThrowIfNull(wearable);Wearable=wearable;Index=15;}
    public ushort Index{get;}public CreatureView6Update? Scalar{get;}public CreatureHamView6Update? Ham{get;}public CreatureDefendersView6Update? Defender{get;}public CreatureWearablesView6Update? Wearable{get;}
}
public sealed class CreatureView6FullDelta
{
    public CreatureView6FullDelta(ulong objectId,ushort declaredCount,IEnumerable<CreatureView6FullUpdate> updates)
    {ObjectId=objectId;DeclaredCount=declaredCount;ArgumentNullException.ThrowIfNull(updates);var list=new List<CreatureView6FullUpdate>();foreach(var u in updates){if(list.Count==4096)throw new InvalidDataException("Full update hard limit");list.Add(u);}var copy=list.ToArray();if(copy.Any(v=>v is null))throw new InvalidDataException("Null full CREO6 update");Updates=Array.AsReadOnly(copy);}
    public ulong ObjectId{get;}public ushort DeclaredCount{get;}public IReadOnlyList<CreatureView6FullUpdate> Updates{get;}
}
public static class CreatureView6FullCodec
{
    internal static void Validate(CreatureView6FullLimits l)
    {ArgumentNullException.ThrowIfNull(l);if(l.MaximumPayloadBytes<2)throw new ArgumentOutOfRangeException(nameof(l));if(l.MaximumPayloadBytes>8388608||l.MaximumFields is <0 or >4096||l.MaximumStringBytes is <0 or >1048576||l.MaximumTotalStringBytes is <0 or >4194304||l.MaximumCommands is <0 or >65536||l.MaximumElements is <0 or >1048576||l.MaximumDecodedBytes is <0 or >16777216||l.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(l));}
    private static int Width(ushort index)=>index switch{0 or 8 or 11 or 12=>4,2=>2,5 or 6 or 9=>8,10 or 17=>1,_=>0};
    private sealed class Meter(CreatureView6FullLimits limits)
    {
        public long Bytes{get;private set;}private long fields,commands,values,strings;
        public long Work=>fields+commands+values+strings;
        private void Check(){if(Bytes+2>limits.MaximumPayloadBytes||Bytes+10>limits.MaximumDecodedBytes||fields>limits.MaximumFields||commands>limits.MaximumCommands||values>limits.MaximumElements||strings>limits.MaximumTotalStringBytes||Work>limits.MaximumWork)throw new InvalidDataException("Full CREO6 global message budget");}
        public void Add(long n){Bytes=checked(Bytes+n);Check();}public void Field(){fields++;Check();}public void Command(){commands++;Check();}public void Values(int n){values+=n;Check();}
        public void String(uint n){if(n>limits.MaximumStringBytes)throw new InvalidDataException("Full CREO6 string budget");strings+=n;Check();}
    }
    private ref struct Reader(ReadOnlySpan<byte> source,Meter meter)
    {
        private readonly ReadOnlySpan<byte> source=source;private readonly Meter meter=meter;public int Position{get;private set;}public int Remaining=>source.Length-Position;
        public ReadOnlySpan<byte> Take(int n){if(n<0||n>Remaining)throw new InvalidDataException("Truncated full CREO6");var v=source.Slice(Position,n);meter.Add(n);Position+=n;return v;}
        public byte U8()=>Take(1)[0];public ushort U16()=>BinaryPrimitives.ReadUInt16LittleEndian(Take(2));public uint U32()=>BinaryPrimitives.ReadUInt32LittleEndian(Take(4));public ulong U64()=>BinaryPrimitives.ReadUInt64LittleEndian(Take(8));public int S32()=>BinaryPrimitives.ReadInt32LittleEndian(Take(4));
        public void Field()=>meter.Field();public void Command()=>meter.Command();public void Values(int n)=>meter.Values(n);
        public CreatureView6Wearable? Wear(bool own){var text=String(own);uint arrangement=U32();ulong id=U64();uint crc=U32();return own?new(text!,arrangement,id,crc):null;}
        public ReplicationByteString? String(bool own){ushort n=U16();bool extended=n==65535;uint size=extended?U32():n;meter.String(size);var bytes=Take(checked((int)size));return own?ReplicationByteString.Create(bytes,extended):null;}
    }
    private static CreatureView6FullDelta? Read(ref Reader r,bool own,ulong id,ushort metadata)
    {
        var updates=own?new List<CreatureView6FullUpdate>():null;int fields=0;
        while(r.Remaining>0)
        {
            r.Field();fields++;ushort index=r.U16();
            if(index is 1 or 15){uint extent=r.U32(),counter=r.U32();ulong cursor=0;var ds=own&&index==1?new List<CreatureDefendersView6Command>():null;var ws=own&&index==15?new List<CreatureWearablesView6Command>():null;
                while(cursor<extent){r.Command();byte op=r.U8();cursor++;ushort ix=0;int n=0;if(op<=2)ix=r.U16();switch(op){case 0:case 4:break;case 1:case 2:n=1;break;case 3:n=r.U16();cursor+=(uint)n;break;default:throw new InvalidDataException("Unknown full vector opcode");}r.Values(n);var dv=own&&index==1?new ulong[n]:null;var wv=own&&index==15?new CreatureView6Wearable[n]:null;for(int i=0;i<n;i++){if(index==1){ulong v=r.U64();if(own)dv![i]=v;}else{var v=r.Wear(own);if(own)wv![i]=v!;}}if(own){if(index==1)ds!.Add(new(op,ix,dv!));else ws!.Add(new(op,ix,wv!));}}
                if(own){if(index==1)updates!.Add(new(new CreatureDefendersView6Update(1,extent,counter,ds!)));else updates!.Add(new(new CreatureWearablesView6Update(extent,counter,ws!)));}
            }
            else if(index is 13 or 14)
            {
                uint extent=r.U32(),counter=r.U32();ulong cursor=0;var commands=own?new List<CreatureHamView6Command>():null;
                while(cursor<extent){r.Command();byte op=r.U8();cursor++;ushort ix=0;int[]? values=null;if(op<=2)ix=r.U16();switch(op){case 0:case 4:if(own)values=[];break;case 1:case 2:r.Values(1);int value=r.S32();if(own)values=[value];break;case 3:ushort n=r.U16();cursor+=n;r.Values(n);var bytes=r.Take(4*n);if(own){values=new int[n];for(int i=0;i<n;i++)values[i]=BinaryPrimitives.ReadInt32LittleEndian(bytes[(4*i)..]);}break;default:throw new InvalidDataException("Unknown full CREO6 HAM opcode");}if(own)commands!.Add(new(op,ix,values!));}
                if(own)updates!.Add(new(new CreatureHamView6Update(index,extent,counter,commands!)));
            }
            else if(index is 3 or 4 or 16){var text=r.String(own);if(own)updates!.Add(new(CreatureView6Update.String(index,text!)));}
            else if(index==7){ulong first=r.U64(),second=r.U64();if(own)updates!.Add(new(CreatureView6Update.Pair(first,second)));}
            else{int width=Width(index);ulong bits=width switch{1=>r.U8(),2=>r.U16(),4=>r.U32(),8=>r.U64(),_=>throw new InvalidDataException("Unsupported full CREO6 field")};if(own)updates!.Add(new(CreatureView6Update.Scalar(index,bits)));}
        }
        if(fields==0)throw new InvalidDataException("Empty full CREO6 delta");return own?new(id,metadata,updates!):null;
    }
    public static CreatureView6FullDelta Decode(ReadOnlySpan<byte> wire,CreatureView6FullLimits limits)
    {Validate(limits);return Decode(ReplicationEnvelopeCodec.DecodeDelta(wire,limits.MaximumPayloadBytes),limits);}
    public static CreatureView6FullDelta Decode(ReplicationEnvelope envelope,CreatureView6FullLimits limits)
    {Validate(limits);ArgumentNullException.ThrowIfNull(envelope);if(envelope.Kind!=ReplicationEnvelopeKind.Delta||envelope.RawTag!=CreatureView6Codec.RawTag||envelope.View!=6||envelope.PayloadSize>limits.MaximumPayloadBytes||envelope.OpaqueFieldLength+10L>limits.MaximumDecodedBytes)throw new InvalidDataException("Expected bounded CREO6 full CREO6 delta");var fields=envelope.CopyOpaqueFields();var scan=new Reader(fields,new(limits));_=Read(ref scan,false,envelope.ObjectId,envelope.DeclaredCount);var reader=new Reader(fields,new(limits));return Read(ref reader,true,envelope.ObjectId,envelope.DeclaredCount)!;}
    private sealed class Writer(BinaryWriter? output,Meter meter)
    {
        public void Field()=>meter.Field();public void Command()=>meter.Command();public void Values(int n)=>meter.Values(n);
        public void U8(byte v){meter.Add(1);output?.Write(v);}public void U16(ushort v){meter.Add(2);output?.Write(v);}public void U32(uint v){meter.Add(4);output?.Write(v);}public void U64(ulong v){meter.Add(8);output?.Write(v);}public void S32(int v){meter.Add(4);output?.Write(v);}
        public void Wear(CreatureView6Wearable e){String(e.Customization);U32(e.Arrangement);U64(e.ObjectId);U32(e.TemplateCrc);}
        public void String(ReplicationByteString s){meter.String((uint)s.Length);if(s.UsesExtendedLength){U16(65535);U32((uint)s.Length);}else U16(checked((ushort)s.Length));meter.Add(s.Length);if(output is not null)output.Write(s.CopyBytes());}
    }
    private static void Write(Writer w,CreatureView6FullDelta plan)
    {
        if(plan.Updates.Count==0)throw new InvalidDataException("Empty full CREO6 delta");
        foreach(var u in plan.Updates){w.Field();w.U16(u.Index);if(u.Defender is {} d){w.U32(d.Extent);w.U32(d.Counter);ulong cursor=0;foreach(var c in d.Commands){if(cursor>=d.Extent)throw new InvalidDataException("Extra full defender command");cursor+=1UL+(c.Opcode==3?(ulong)c.Values.Count:0);w.Command();w.Values(c.Values.Count);w.U8(c.Opcode);if(c.Opcode!=4)w.U16(c.Opcode==3?(ushort)c.Values.Count:c.Index);foreach(ulong value in c.Values)w.U64(value);}if(cursor<d.Extent)throw new InvalidDataException("Incomplete full defender extent");continue;}if(u.Wearable is {} v){w.U32(v.Extent);w.U32(v.Counter);ulong cursor=0;foreach(var c in v.Commands){if(cursor>=v.Extent)throw new InvalidDataException("Extra full wearable command");cursor+=1UL+(c.Opcode==3?(ulong)c.Values.Count:0);w.Command();w.Values(c.Values.Count);w.U8(c.Opcode);if(c.Opcode!=4)w.U16(c.Opcode==3?(ushort)c.Values.Count:c.Index);foreach(var value in c.Values)w.Wear(value);}if(cursor<v.Extent)throw new InvalidDataException("Incomplete full wearable extent");continue;}if(u.Ham is {} h){w.U32(h.Extent);w.U32(h.Counter);ulong cursor=0;foreach(var c in h.Commands){if(cursor>=h.Extent)throw new InvalidDataException("Extra full CREO6 HAM command");cursor+=1UL+(c.Opcode==3?(ulong)c.Values.Count:0);w.Command();w.Values(c.Values.Count);w.U8(c.Opcode);if(c.Opcode!=4)w.U16(c.Opcode==3?(ushort)c.Values.Count:c.Index);foreach(int value in c.Values)w.S32(value);}if(cursor<h.Extent)throw new InvalidDataException("Incomplete full CREO6 HAM extent");continue;}var s=u.Scalar!;if(s.Text is not null){w.String(s.Text);continue;}if(s.Index==7){w.U64(s.Bits);w.U64(s.SecondId);continue;}switch(Width(s.Index)){case 1:w.U8((byte)s.Bits);break;case 2:w.U16((ushort)s.Bits);break;case 4:w.U32((uint)s.Bits);break;case 8:w.U64(s.Bits);break;default:throw new InvalidDataException("Full CREO6 scalar field");}}
    }
    internal static (long Bytes,long Work) Measure(CreatureView6FullDelta plan,CreatureView6FullLimits limits)
    {Validate(limits);ArgumentNullException.ThrowIfNull(plan);var meter=new Meter(limits);Write(new(null,meter),plan);return(meter.Bytes,meter.Work);}
    public static ReplicationEnvelope EncodeEnvelope(CreatureView6FullDelta plan,CreatureView6FullLimits limits)
    {var cost=Measure(plan,limits);using var stream=new MemoryStream(checked((int)cost.Bytes));using var writer=new BinaryWriter(stream);Write(new(writer,new(limits)),plan);return new(ReplicationEnvelopeKind.Delta,plan.ObjectId,CreatureView6Codec.RawTag,6,plan.DeclaredCount,stream.ToArray(),limits.MaximumPayloadBytes);}
    public static byte[] Encode(CreatureView6FullDelta plan,CreatureView6FullLimits limits)=>ReplicationEnvelopeCodec.EncodeDelta(EncodeEnvelope(plan,limits),limits.MaximumPayloadBytes);
}
