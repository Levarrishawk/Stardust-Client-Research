using System.Buffers.Binary;
namespace Stardust.Core.Protocol;

public sealed record TangibleView3DeltaLimits(int MaximumPayloadBytes,int MaximumFields,
    int MaximumTangibleUpdates,int MaximumRawUpdates,int MaximumStringBytes,long MaximumDecodedBytes,
    int MaximumComponentCommands,int MaximumComponentKeys,int MaximumComponentElements,
    int MaximumNameUpdates,int MaximumNameStringBytes,int MaximumUnicodeUnits);

public static class TangibleView3DeltaCodec
{
    public const uint RawTag=TangibleView3BaselineCodec.RawTag;
    public const byte View=3;
    private static void Validate(TangibleView3DeltaLimits l)
    {
        ArgumentNullException.ThrowIfNull(l);ReplicationEnvelopeCodec.ValidateBudget(l.MaximumPayloadBytes);
        if(l.MaximumFields is <0 or >4096||l.MaximumTangibleUpdates is <0 or >4096||l.MaximumRawUpdates is <0 or >4096||l.MaximumStringBytes<0||l.MaximumDecodedBytes<0||l.MaximumComponentCommands is <0 or >65536||l.MaximumComponentKeys is <0 or >65536||l.MaximumComponentElements is <0 or >65536||l.MaximumNameUpdates is <0 or >4096||l.MaximumNameStringBytes is <0 or >65536||l.MaximumUnicodeUnits is <0 or >65536)throw new ArgumentOutOfRangeException(nameof(l));
    }
    private static CreatureView3NamedDeltaLimits CommonLimits(TangibleView3DeltaLimits l)=>new(new(new(l.MaximumPayloadBytes,l.MaximumFields,0,l.MaximumRawUpdates,l.MaximumStringBytes,l.MaximumDecodedBytes,l.MaximumComponentCommands,l.MaximumComponentKeys,l.MaximumComponentElements,0,0,0),l.MaximumTangibleUpdates),l.MaximumNameUpdates,l.MaximumNameStringBytes,l.MaximumUnicodeUnits);
    public static TangibleView3DeltaPlan Decode(ReadOnlySpan<byte> message,TangibleView3DeltaLimits limits)
    {Validate(limits);return Decode(ReplicationEnvelopeCodec.DecodeDelta(message,limits.MaximumPayloadBytes),limits);}
    public static TangibleView3DeltaPlan Decode(ReplicationEnvelope envelope,TangibleView3DeltaLimits limits)
    {
        Validate(limits);ArgumentNullException.ThrowIfNull(envelope);
        if(envelope.Kind!=ReplicationEnvelopeKind.Delta||envelope.RawTag!=RawTag||envelope.View!=View||envelope.PayloadSize>limits.MaximumPayloadBytes)throw new InvalidDataException("Expected bounded TANO:3 delta.");
        var ranges=Scan(envelope.Fields,limits);var updates=new CreatureView3NamedDeltaUpdate[ranges.Count];var common=CommonLimits(limits);
        for(int i=0;i<updates.Length;i++)
        {
            var r=ranges[i];var single=new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,envelope.ObjectId,CreatureView3NamedDeltaCodec.RawTag,View,1,envelope.Fields.Slice(r.Start,r.Length),limits.MaximumPayloadBytes);
            updates[i]=CreatureView3NamedDeltaCodec.Decode(single,common).Updates[0];
        }
        return new(envelope.ObjectId,envelope.DeclaredCount,updates,limits.MaximumComponentElements);
    }
    public static byte[] Encode(TangibleView3DeltaPlan plan,TangibleView3DeltaLimits limits)
        =>ReplicationEnvelopeCodec.EncodeDelta(EncodeEnvelope(plan,limits),limits.MaximumPayloadBytes);
    public static ReplicationEnvelope EncodeEnvelope(TangibleView3DeltaPlan plan,TangibleView3DeltaLimits limits)
    {
        Validate(limits);ArgumentNullException.ThrowIfNull(plan);
        if(plan.Updates.Any(u=>u.Index>10))throw new InvalidDataException("TANO common indices only.");
        var commonPlan=new CreatureView3NamedDeltaPlan(plan.ObjectId,plan.DeclaredCount,plan.Updates,CreatureWoundsApplicationPolicy.StrictInterval,0,plan.MaximumComponentsElements);
        var encoded=CreatureView3NamedDeltaCodec.EncodeEnvelope(commonPlan,CommonLimits(limits));
        return new(ReplicationEnvelopeKind.Delta,plan.ObjectId,RawTag,View,plan.DeclaredCount,encoded.Fields,limits.MaximumPayloadBytes);
    }
    private readonly record struct Range(int Start,int Length,ushort Index);
    private static List<Range> Scan(ReadOnlySpan<byte> fields,TangibleView3DeltaLimits limits)
    {
        var l=limits;int tangible=0,names=0;long nameBytes=0;Reader r=new(fields);List<Range> ranges=new();int raws=0,cc=0,ck=0;long decoded=0;
        void Charge(long n){if(n>l.MaximumDecodedBytes-decoded)throw new InvalidDataException("Full decoded budget.");decoded+=n;}
        while(!r.End)
        {
            if(ranges.Count>=l.MaximumFields)throw new InvalidDataException("Full field budget.");
            int start=r.Position;ushort index=r.U16();
            switch(index)
            {
                case 1:
                    if(++names>limits.MaximumNameUpdates)throw new InvalidDataException("Named update budget.");
                    ushort filePrefix=r.U16();uint fileLength=filePrefix==65535?r.U32():filePrefix;
                    if(fileLength>limits.MaximumNameStringBytes)throw new InvalidDataException("Name file budget.");
                    Charge(fileLength);nameBytes+=fileLength;r.Skip((int)fileLength);
                    Charge(4);nameBytes+=4;r.U32();
                    ushort keyPrefix=r.U16();uint keyLength=keyPrefix==65535?r.U32():keyPrefix;
                    if(keyLength>limits.MaximumNameStringBytes)throw new InvalidDataException("Name key budget.");
                    Charge(keyLength);nameBytes+=keyLength;r.Skip((int)keyLength);break;
                case 2:
                    if(++names>limits.MaximumNameUpdates)throw new InvalidDataException("Named update budget.");
                    uint unitCount=r.U32();if(unitCount>limits.MaximumUnicodeUnits)throw new InvalidDataException("Name unicode budget.");
                    Charge(2L*unitCount);nameBytes+=2L*unitCount;r.Skip((int)unitCount*2);break;
                case 0:
                case 3:
                case >=6 and <=10:
                    if(++tangible>limits.MaximumTangibleUpdates)throw new InvalidDataException("Extended tangible budget.");
                    int commonWidth=index==10?1:4;Charge(commonWidth);
                    if(index==0){if(!float.IsFinite(BitConverter.Int32BitsToSingle(unchecked((int)r.U32()))))throw new InvalidDataException("Nonfinite complexity.");}
                    else r.Skip(commonWidth);break;
                case 4:
                    if(++raws>l.MaximumRawUpdates)throw new InvalidDataException("Full raw budget.");
                    ushort prefix=r.U16();uint size=prefix==65535?r.U32():prefix;
                    if(size>l.MaximumStringBytes)throw new InvalidDataException("Full string budget.");Charge(size);r.Skip((int)size);break;
                case 5:
                    uint count=r.U32();r.U32();if(count>4096||count>l.MaximumComponentCommands-cc)throw new InvalidDataException("Full component command budget.");cc+=(int)count;r.Require((int)count);
                    for(uint n=0;n<count;n++){byte kind=r.Byte();if(kind<2){if(++ck>l.MaximumComponentKeys)throw new InvalidDataException("Full component key budget.");Charge(4);r.Skip(4);}else if(kind!=2)throw new InvalidDataException("Unknown component command.");}break;
                default:throw new InvalidDataException("Unsupported full field.");
            }
            if(nameBytes>1048576)throw new InvalidDataException("Frozen aggregate name budget.");
            ranges.Add(new(start,r.Position-start,index));
        }
        if(ranges.Count==0)throw new InvalidDataException("Full delta requires an update.");return ranges;
    }
    private ref struct Reader(ReadOnlySpan<byte> bytes)
    {
        private readonly ReadOnlySpan<byte> bytes=bytes;private int cursor;
        public int Position=>cursor;public bool End=>cursor==bytes.Length;
        public void Require(int n){if(n<0||n>bytes.Length-cursor)throw new InvalidDataException("Truncated full field.");}
        public ReadOnlySpan<byte> Read(int n){Require(n);var s=bytes.Slice(cursor,n);cursor+=n;return s;}
        public void Skip(int n)=>_=Read(n);public byte Byte()=>Read(1)[0];public ushort U16()=>BinaryPrimitives.ReadUInt16LittleEndian(Read(2));public uint U32()=>BinaryPrimitives.ReadUInt32LittleEndian(Read(4));
    }
}
