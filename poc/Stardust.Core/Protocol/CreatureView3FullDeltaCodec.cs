using System.Buffers.Binary;
namespace Stardust.Core.Protocol;

public sealed record CreatureView3FullDeltaLimits(int MaximumPayloadBytes,int MaximumFields,
    int MaximumScalarUpdates,int MaximumRawUpdates,int MaximumStringBytes,long MaximumDecodedBytes,
    int MaximumComponentCommands,int MaximumComponentKeys,int MaximumComponentElements,
    int MaximumWoundsCommands,int MaximumWoundsValues,int MaximumWoundsElements);

/// <summary>Owned ordered CREO:3 union; compound application and packed interpretation remain separate.</summary>
public static class CreatureView3FullDeltaCodec
{
    public const uint RawTag=CreatureView3ScalarDeltaCodec.RawTag;
    public const byte View=3;
    private static void Validate(CreatureView3FullDeltaLimits l)
    {
        ArgumentNullException.ThrowIfNull(l);ReplicationEnvelopeCodec.ValidateBudget(l.MaximumPayloadBytes);
        if(l.MaximumFields is <0 or >4096||l.MaximumScalarUpdates is <0 or >4096||l.MaximumRawUpdates is <0 or >4096||l.MaximumStringBytes<0||l.MaximumDecodedBytes<0||
            l.MaximumComponentCommands is <0 or >65536||l.MaximumComponentKeys is <0 or >65536||l.MaximumComponentElements is <0 or >65536||
            l.MaximumWoundsCommands is <0 or >65536||l.MaximumWoundsValues is <0 or >65536||l.MaximumWoundsElements is <0 or >65536)throw new ArgumentOutOfRangeException(nameof(l));
    }
    private static void Policy(CreatureWoundsApplicationPolicy p)
    {if(p is not (CreatureWoundsApplicationPolicy.StrictInterval or CreatureWoundsApplicationPolicy.LegacyBounded))throw new ArgumentOutOfRangeException(nameof(p));}
    public static CreatureView3FullDeltaPlan Decode(ReadOnlySpan<byte> message,CreatureView3FullDeltaLimits limits,CreatureWoundsApplicationPolicy woundsPolicy=CreatureWoundsApplicationPolicy.StrictInterval)
    {Validate(limits);Policy(woundsPolicy);return Decode(ReplicationEnvelopeCodec.DecodeDelta(message,limits.MaximumPayloadBytes),limits,woundsPolicy);}
    public static CreatureView3FullDeltaPlan Decode(ReplicationEnvelope envelope,CreatureView3FullDeltaLimits limits,CreatureWoundsApplicationPolicy woundsPolicy=CreatureWoundsApplicationPolicy.StrictInterval)
    {
        Validate(limits);Policy(woundsPolicy);ArgumentNullException.ThrowIfNull(envelope);
        if(envelope.Kind!=ReplicationEnvelopeKind.Delta||envelope.RawTag!=RawTag||envelope.View!=View||envelope.PayloadSize>limits.MaximumPayloadBytes)throw new InvalidDataException("Expected bounded full CREO:3 delta.");
        // First scan the whole span, including every allocation budget, before publishing/copying nested values.
        var ranges=Scan(envelope.Fields,limits,woundsPolicy);
        var updates=new CreatureView3FullDeltaUpdate[ranges.Count];
        for(int i=0;i<updates.Length;i++)
        {
            var r=ranges[i];var single=new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,envelope.ObjectId,RawTag,View,1,envelope.Fields.Slice(r.Start,r.Length),limits.MaximumPayloadBytes);
            updates[i]=r.Index switch
            {
                4=>CreatureView3FullDeltaUpdate.FromCustomization(CreatureView3CustomizationDeltaCodec.Decode(single,new(limits.MaximumPayloadBytes,1,limits.MaximumStringBytes,limits.MaximumDecodedBytes)).Updates[0]),
                5=>CreatureView3FullDeltaUpdate.FromComponents(CreatureView3ComponentsDeltaCodec.Decode(single,new(limits.MaximumPayloadBytes,1,limits.MaximumComponentCommands,limits.MaximumComponentKeys,limits.MaximumComponentElements)).Updates[0]),
                17=>CreatureView3FullDeltaUpdate.FromWounds(CreatureView3WoundsDeltaCodec.Decode(single,new(limits.MaximumPayloadBytes,1,limits.MaximumWoundsCommands,limits.MaximumWoundsValues,limits.MaximumWoundsElements),woundsPolicy).Updates[0]),
                _=>CreatureView3FullDeltaUpdate.FromScalar(CreatureView3CustomizationScalarDeltaCodec.Decode(single,new(limits.MaximumPayloadBytes,1,1,0,0,limits.MaximumDecodedBytes)).Updates[0].Scalar!)
            };
        }
        return new(envelope.ObjectId,envelope.DeclaredCount,updates,woundsPolicy,limits.MaximumWoundsElements,limits.MaximumComponentElements);
    }
    private readonly record struct Range(int Start,int Length,ushort Index);
    private static List<Range> Scan(ReadOnlySpan<byte> fields,CreatureView3FullDeltaLimits l,CreatureWoundsApplicationPolicy policy)
    {
        Reader r=new(fields);List<Range> ranges=new();int raws=0,scalars=0,cc=0,ck=0,wc=0,wv=0;long decoded=0;
        void Charge(long n){if(n>l.MaximumDecodedBytes-decoded)throw new InvalidDataException("Full decoded budget.");decoded+=n;}
        while(!r.End)
        {
            if(ranges.Count>=l.MaximumFields)throw new InvalidDataException("Full field budget.");
            int start=r.Position;ushort index=r.U16();
            switch(index)
            {
                case 4:
                    if(++raws>l.MaximumRawUpdates)throw new InvalidDataException("Full raw budget.");
                    ushort prefix=r.U16();uint size=prefix==65535?r.U32():prefix;
                    if(size>l.MaximumStringBytes)throw new InvalidDataException("Full string budget.");Charge(size);r.Skip((int)size);break;
                case >=11 and <=16:
                    if(++scalars>l.MaximumScalarUpdates)throw new InvalidDataException("Full scalar budget.");
                    int width=index is 11 or 12?1:index is 13 or 16?8:4;Charge(width);
                    if(index==14&&!float.IsFinite(BitConverter.Int32BitsToSingle(unchecked((int)r.U32()))))throw new InvalidDataException("Nonfinite Height.");
                    if(index!=14)r.Skip(width);break;
                case 5:
                    uint count=r.U32();r.U32();if(count>4096||count>l.MaximumComponentCommands-cc)throw new InvalidDataException("Full component command budget.");cc+=(int)count;r.Require((int)count);
                    for(uint n=0;n<count;n++){byte kind=r.Byte();if(kind<2){if(++ck>l.MaximumComponentKeys)throw new InvalidDataException("Full component key budget.");Charge(4);r.Skip(4);}else if(kind!=2)throw new InvalidDataException("Unknown component command.");}break;
                case 17:
                    uint bound=r.U32();r.U32();if(bound>l.MaximumWoundsCommands-wc)throw new InvalidDataException("Full wounds command budget.");wc+=(int)bound;r.Require((int)bound);int actual=0;
                    for(ulong n=0;n<bound;n++)
                    {
                        if(++actual>4096)throw new InvalidDataException("Full per-field wounds budget.");
                        byte kind=r.Byte();int values=0;
                        if(kind<3){r.U16();values=kind==0?0:1;}else if(kind==3)values=r.U16();else if(kind!=4)throw new InvalidDataException("Unknown wounds command.");
                        if(values>l.MaximumWoundsValues-wv)throw new InvalidDataException("Full wounds value budget.");
                        wv+=values;Charge(4L*values);r.Skip(values*4);
                        if(policy==CreatureWoundsApplicationPolicy.LegacyBounded&&kind==3)n+=(uint)values;
                    }break;
                default:throw new InvalidDataException("Unsupported full field.");
            }
            if((long)cc+wc>65536)throw new InvalidDataException("Full aggregate command budget.");
            ranges.Add(new(start,r.Position-start,index));
        }
        if(ranges.Count==0)throw new InvalidDataException("Full delta requires an update.");return ranges;
    }
    public static byte[] Encode(CreatureView3FullDeltaPlan plan,CreatureView3FullDeltaLimits limits)=>ReplicationEnvelopeCodec.EncodeDelta(EncodeEnvelope(plan,limits),limits.MaximumPayloadBytes);
    public static ReplicationEnvelope EncodeEnvelope(CreatureView3FullDeltaPlan plan,CreatureView3FullDeltaLimits limits)
    {
        Validate(limits);ArgumentNullException.ThrowIfNull(plan);Policy(plan.WoundsPolicy);
        if(plan.MaximumWoundsElements>limits.MaximumWoundsElements||plan.MaximumComponentsElements>limits.MaximumComponentElements||plan.Updates.Count>limits.MaximumFields)throw new InvalidDataException("Full plan metadata budget.");
        // Preflight all length/type/loop-shape checks before allocating output or sibling buffers.
        long length=0;
        foreach(var u in plan.Updates)
        {
            if(u.Customization is {} raw)length+=2+(raw.UsesExtendedLength?6:2)+raw.Length;
            else if(u.Scalar is {} scalar)length+=2+(scalar.Index is 11 or 12?1:scalar.Index is 13 or 16?8:4);
            else if(u.Components is {} c){length+=10;foreach(var cmd in c.Commands)length+=cmd.Kind==CreatureComponentsCommandKind.Clear?1:5;}
            else if(u.Wounds is {} w)
            {
                length+=10;ulong cursor=0;
                foreach(var cmd in w.Commands)
                {
                    if(cursor>=w.CommandCount)throw new InvalidDataException("Wounds commands exceed raw loop.");
                    cursor+=plan.WoundsPolicy==CreatureWoundsApplicationPolicy.LegacyBounded&&cmd.Kind==CreatureWoundsCommandKind.Assign?(ulong)cmd.Values.Count+1:1;
                    length+=1+(cmd.Kind==CreatureWoundsCommandKind.Clear?0:2)+4L*cmd.Values.Count;
                }
                if(cursor<w.CommandCount)throw new InvalidDataException("Incomplete wounds raw loop.");
            }
            else throw new InvalidDataException("Empty full update.");
            if(length>limits.MaximumPayloadBytes-2L)throw new InvalidDataException("Full encode payload budget.");
        }
        // Independent budgets are counted without temporary wire allocation.
        Preflight(plan,limits);
        byte[] fields=new byte[(int)length];int offset=0;
        foreach(var u in plan.Updates)
        {
            byte[] bytes;
            if(u.Customization is {} raw)bytes=CreatureView3CustomizationDeltaCodec.Encode(new(plan.ObjectId,1,new[]{raw}),new(limits.MaximumPayloadBytes,1,limits.MaximumStringBytes,limits.MaximumDecodedBytes));
            else if(u.Scalar is {} scalar)bytes=CreatureView3CustomizationScalarDeltaCodec.Encode(new(plan.ObjectId,1,new[]{CreatureView3CustomizationScalarUpdate.FromScalar(scalar)}),new(limits.MaximumPayloadBytes,1,1,0,0,limits.MaximumDecodedBytes));
            else if(u.Components is {} c)bytes=CreatureView3ComponentsDeltaCodec.Encode(new(plan.ObjectId,1,new[]{c},limits.MaximumComponentElements),new(limits.MaximumPayloadBytes,1,limits.MaximumComponentCommands,limits.MaximumComponentKeys,limits.MaximumComponentElements));
            else bytes=CreatureView3WoundsDeltaCodec.Encode(new(plan.ObjectId,1,new[]{u.Wounds!},limits.MaximumWoundsElements,plan.WoundsPolicy),new(limits.MaximumPayloadBytes,1,limits.MaximumWoundsCommands,limits.MaximumWoundsValues,limits.MaximumWoundsElements));
            bytes.AsSpan(25).CopyTo(fields.AsSpan(offset));offset+=bytes.Length-25;
        }
        return new(ReplicationEnvelopeKind.Delta,plan.ObjectId,RawTag,View,plan.DeclaredCount,fields,limits.MaximumPayloadBytes);
    }
    private static void Preflight(CreatureView3FullDeltaPlan p,CreatureView3FullDeltaLimits l)
    {
        int raw=0,scalar=0,cc=0,ck=0,wc=0,wv=0;long decoded=0;
        foreach(var u in p.Updates)
        {
            if(u.Customization is {} s){raw++;if(s.Length>l.MaximumStringBytes)throw new InvalidDataException("Full string budget.");decoded+=s.Length;}
            if(u.Scalar is {} v){scalar++;decoded+=v.Index is 11 or 12?1:v.Index is 13 or 16?8:4;_=CreatureView3CustomizationScalarUpdate.FromScalar(v);}
            if(u.Components is {} c){cc+=c.Commands.Count;foreach(var cmd in c.Commands)if(cmd.Kind!=CreatureComponentsCommandKind.Clear)ck++;}
            if(u.Wounds is {} w){wc+=checked((int)w.CommandCount);foreach(var cmd in w.Commands){wv+=cmd.Values.Count;}}
        }
        decoded+=4L*(ck+wv);
        if(raw>l.MaximumRawUpdates||scalar>l.MaximumScalarUpdates||cc>l.MaximumComponentCommands||ck>l.MaximumComponentKeys||wc>l.MaximumWoundsCommands||wv>l.MaximumWoundsValues||decoded>l.MaximumDecodedBytes)throw new InvalidDataException("Full encode aggregate budget.");
    }
    private ref struct Reader(ReadOnlySpan<byte> bytes)
    {
        private readonly ReadOnlySpan<byte> bytes=bytes;private int cursor;
        public int Position=>cursor;public bool End=>cursor==bytes.Length;
        public void Require(int n){if(n<0||n>bytes.Length-cursor)throw new InvalidDataException("Truncated full field.");}
        private ReadOnlySpan<byte> Read(int n){Require(n);var s=bytes.Slice(cursor,n);cursor+=n;return s;}
        public void Skip(int n)=>_=Read(n);public byte Byte()=>Read(1)[0];public ushort U16()=>BinaryPrimitives.ReadUInt16LittleEndian(Read(2));public uint U32()=>BinaryPrimitives.ReadUInt32LittleEndian(Read(4));
    }
}
