using System.Buffers.Binary;
namespace Stardust.Core.Protocol;

/// <summary>Nested limits cap the entire ordered union; name fields have independent modern bounds.</summary>
public sealed record CreatureView3NamedDeltaLimits(CreatureView3ExtendedDeltaLimits Extended,
    int MaximumNameUpdates,int MaximumNameStringBytes,int MaximumUnicodeUnits);

public static class CreatureView3NamedDeltaCodec
{
    public const uint RawTag=CreatureView3ExtendedDeltaCodec.RawTag;
    public const byte View=CreatureView3ExtendedDeltaCodec.View;
    private static void Validate(CreatureView3NamedDeltaLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);ArgumentNullException.ThrowIfNull(limits.Extended);ArgumentNullException.ThrowIfNull(limits.Extended.Full);
        if(limits.MaximumNameUpdates is <0 or >4096||limits.MaximumNameStringBytes is <0 or >65536||limits.MaximumUnicodeUnits is <0 or >65536)throw new ArgumentOutOfRangeException(nameof(limits));
        if(limits.Extended.MaximumTangibleUpdates is <0 or >4096)throw new ArgumentOutOfRangeException(nameof(limits));
        var l=limits.Extended.Full;ReplicationEnvelopeCodec.ValidateBudget(l.MaximumPayloadBytes);
        if(l.MaximumFields is <0 or >4096||l.MaximumScalarUpdates is <0 or >4096||l.MaximumRawUpdates is <0 or >4096||l.MaximumStringBytes<0||l.MaximumDecodedBytes<0||
            l.MaximumComponentCommands is <0 or >65536||l.MaximumComponentKeys is <0 or >65536||l.MaximumComponentElements is <0 or >65536||
            l.MaximumWoundsCommands is <0 or >65536||l.MaximumWoundsValues is <0 or >65536||l.MaximumWoundsElements is <0 or >65536)throw new ArgumentOutOfRangeException(nameof(l));
    }
    private static void Policy(CreatureWoundsApplicationPolicy p)
    {if(p is not (CreatureWoundsApplicationPolicy.StrictInterval or CreatureWoundsApplicationPolicy.LegacyBounded))throw new ArgumentOutOfRangeException(nameof(p));}
    public static CreatureView3NamedDeltaPlan Decode(ReadOnlySpan<byte> message,CreatureView3NamedDeltaLimits limits,CreatureWoundsApplicationPolicy woundsPolicy=CreatureWoundsApplicationPolicy.StrictInterval)
    {Validate(limits);Policy(woundsPolicy);return Decode(ReplicationEnvelopeCodec.DecodeDelta(message,limits.Extended.Full.MaximumPayloadBytes),limits,woundsPolicy);}
    public static CreatureView3NamedDeltaPlan Decode(ReplicationEnvelope envelope,CreatureView3NamedDeltaLimits limits,CreatureWoundsApplicationPolicy woundsPolicy=CreatureWoundsApplicationPolicy.StrictInterval)
    {
        Validate(limits);Policy(woundsPolicy);ArgumentNullException.ThrowIfNull(envelope);var l=limits.Extended.Full;
        if(envelope.Kind!=ReplicationEnvelopeKind.Delta||envelope.RawTag!=RawTag||envelope.View!=View||envelope.PayloadSize>l.MaximumPayloadBytes)throw new InvalidDataException("Expected bounded named CREO:3 delta.");
        var ranges=Scan(envelope.Fields,limits,woundsPolicy);var updates=new CreatureView3NamedDeltaUpdate[ranges.Count];
        for(int i=0;i<updates.Length;i++)
        {
            var range=ranges[i];var bytes=envelope.Fields.Slice(range.Start,range.Length);
            if(range.Index is 1 or 2)
            {
                Reader r=new(bytes[2..]);CreatureView3NameUpdate name;
                if(range.Index==1){var file=NameString(ref r);uint spacer=r.U32();var key=NameString(ref r);name=CreatureView3NameUpdate.FromStringId(file,spacer,key);}
                else {int count=(int)r.U32();ushort[] units=new ushort[count];for(int n=0;n<count;n++)units[n]=r.U16();name=CreatureView3NameUpdate.FromCustomName(units);}
                updates[i]=CreatureView3NamedDeltaUpdate.FromName(name);
            }
            else
            {
                var single=new ReplicationEnvelope(ReplicationEnvelopeKind.Delta,envelope.ObjectId,RawTag,View,1,bytes,l.MaximumPayloadBytes);
                updates[i]=CreatureView3NamedDeltaUpdate.FromExtended(CreatureView3ExtendedDeltaCodec.Decode(single,limits.Extended,woundsPolicy).Updates[0]);
            }
        }
        return new(envelope.ObjectId,envelope.DeclaredCount,updates,woundsPolicy,l.MaximumWoundsElements,l.MaximumComponentElements);
    }
    private static ReplicationByteString NameString(ref Reader r)
    {ushort prefix=r.U16();bool extended=prefix==65535;int count=extended?(int)r.U32():prefix;return ReplicationByteString.Create(r.Read(count),extended);}
    private readonly record struct Range(int Start,int Length,ushort Index);
    private static List<Range> Scan(ReadOnlySpan<byte> fields,CreatureView3NamedDeltaLimits limits,CreatureWoundsApplicationPolicy policy)
    {
        var l=limits.Extended.Full;int tangible=0,names=0;long nameBytes=0;Reader r=new(fields);List<Range> ranges=new();int raws=0,scalars=0,cc=0,ck=0,wc=0,wv=0;long decoded=0;
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
                    if(++tangible>limits.Extended.MaximumTangibleUpdates)throw new InvalidDataException("Extended tangible budget.");
                    int commonWidth=index==10?1:4;Charge(commonWidth);
                    if(index==0){if(!float.IsFinite(BitConverter.Int32BitsToSingle(unchecked((int)r.U32()))))throw new InvalidDataException("Nonfinite complexity.");}
                    else r.Skip(commonWidth);break;
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
            if(nameBytes>1048576)throw new InvalidDataException("Frozen aggregate name budget.");
            ranges.Add(new(start,r.Position-start,index));
        }
        if(ranges.Count==0)throw new InvalidDataException("Full delta requires an update.");return ranges;
    }
    public static byte[] Encode(CreatureView3NamedDeltaPlan plan,CreatureView3NamedDeltaLimits limits)
        =>ReplicationEnvelopeCodec.EncodeDelta(EncodeEnvelope(plan,limits),limits.Extended.Full.MaximumPayloadBytes);
    public static ReplicationEnvelope EncodeEnvelope(CreatureView3NamedDeltaPlan plan,CreatureView3NamedDeltaLimits limits)
    {
        Validate(limits);ArgumentNullException.ThrowIfNull(plan);Policy(plan.WoundsPolicy);var l=limits.Extended.Full;
        if(plan.Updates.Count>l.MaximumFields||plan.MaximumWoundsElements>l.MaximumWoundsElements||plan.MaximumComponentsElements>l.MaximumComponentElements)throw new InvalidDataException("Extended plan metadata budget.");
        long length=0,decoded=0;long nameBytes=0;int names=0;int tangible=0,raws=0,scalars=0,cc=0,ck=0,wc=0,wv=0;
        foreach(var update in plan.Updates)
        {
            if(update.Name is {} name)
            {
                names++;nameBytes+=name.DecodedBytes;decoded+=name.DecodedBytes;
                if(name.Index==1)
                {
                    var file=name.NameFile!;var key=name.NameKey!;
                    if(file.Length>limits.MaximumNameStringBytes||key.Length>limits.MaximumNameStringBytes)throw new InvalidDataException("Name string budget.");
                    length+=2+(file.UsesExtendedLength?6:2)+file.Length+4+(key.UsesExtendedLength?6:2)+key.Length;
                }
                else if(name.Index==2){if(name.CustomNameUnits!.Count>limits.MaximumUnicodeUnits)throw new InvalidDataException("Name unicode budget.");length+=6+2L*name.CustomNameUnits.Count;}
                else throw new InvalidDataException("Known name field required.");
            }
            else if(update.Extended?.Tangible is {} t)
            {
                if(!(t.Index is 0 or 3 or >=6 and <=10)||(t.Index==0&&!float.IsFinite(t.Complexity)))throw new InvalidDataException("Known finite common update required.");
                tangible++;int width=t.Index==10?1:4;decoded+=width;length+=2+width;
            }
            else if(update.Extended?.Full is {} u)
            {
                if(u.Customization is {} raw){raws++;if(raw.Length>l.MaximumStringBytes)throw new InvalidDataException("Extended string budget.");decoded+=raw.Length;length+=2+(raw.UsesExtendedLength?6:2)+raw.Length;}
                else if(u.Scalar is {} scalar){_=CreatureView3CustomizationScalarUpdate.FromScalar(scalar);scalars++;int width=scalar.Index is 11 or 12?1:scalar.Index is 13 or 16?8:4;decoded+=width;length+=2+width;}
                else if(u.Components is {} c){cc+=c.Commands.Count;length+=10;foreach(var command in c.Commands){length++;if(command.Kind!=CreatureComponentsCommandKind.Clear){ck++;length+=4;decoded+=4;}}}
                else if(u.Wounds is {} w)
                {
                    wc+=checked((int)w.CommandCount);length+=10;ulong cursor=0;
                    foreach(var command in w.Commands)
                    {
                        if(cursor>=w.CommandCount)throw new InvalidDataException("Extended wounds raw loop overflow.");
                        cursor+=plan.WoundsPolicy==CreatureWoundsApplicationPolicy.LegacyBounded&&command.Kind==CreatureWoundsCommandKind.Assign?(ulong)command.Values.Count+1:1;
                        wv+=command.Values.Count;decoded+=4L*command.Values.Count;
                        length+=1+(command.Kind==CreatureWoundsCommandKind.Clear?0:2)+4L*command.Values.Count;
                    }
                    if(cursor<w.CommandCount)throw new InvalidDataException("Incomplete extended wounds raw loop.");
                }
                else throw new InvalidDataException("Empty full update.");
            }
            else throw new InvalidDataException("Empty extended update.");
            if(names>limits.MaximumNameUpdates||nameBytes>1048576)throw new InvalidDataException("Named update/aggregate budget.");
            if(length>l.MaximumPayloadBytes-2L||decoded>l.MaximumDecodedBytes||tangible>limits.Extended.MaximumTangibleUpdates||raws>l.MaximumRawUpdates||scalars>l.MaximumScalarUpdates||cc>l.MaximumComponentCommands||ck>l.MaximumComponentKeys||wc>l.MaximumWoundsCommands||wv>l.MaximumWoundsValues)throw new InvalidDataException("Extended encode budget.");
        }
        // Complete length, loop shape and all aggregate caps passed before output/sibling buffers.
        byte[] fields=new byte[(int)length];int offset=0;
        foreach(var update in plan.Updates)
        {
            if(update.Name is {} name)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(fields.AsSpan(offset),name.Index);offset+=2;
                if(name.Index==1)
                {
                    WriteString(fields,ref offset,name.NameFile!);BinaryPrimitives.WriteUInt32LittleEndian(fields.AsSpan(offset),name.NameSpacer);offset+=4;WriteString(fields,ref offset,name.NameKey!);
                }
                else {BinaryPrimitives.WriteUInt32LittleEndian(fields.AsSpan(offset),(uint)name.CustomNameUnits!.Count);offset+=4;foreach(ushort unit in name.CustomNameUnits){BinaryPrimitives.WriteUInt16LittleEndian(fields.AsSpan(offset),unit);offset+=2;}}
            }
            else
            {
                var single=new CreatureView3ExtendedDeltaPlan(plan.ObjectId,1,new[]{update.Extended!},plan.WoundsPolicy,plan.MaximumWoundsElements,plan.MaximumComponentsElements);
                var encoded=CreatureView3ExtendedDeltaCodec.EncodeEnvelope(single,limits.Extended);encoded.Fields.CopyTo(fields.AsSpan(offset));offset+=encoded.OpaqueFieldLength;
            }
        }
        return new(ReplicationEnvelopeKind.Delta,plan.ObjectId,RawTag,View,plan.DeclaredCount,fields,l.MaximumPayloadBytes);
    }
    private static void WriteString(Span<byte> bytes,ref int offset,ReplicationByteString value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(bytes[offset..],value.UsesExtendedLength?ushort.MaxValue:checked((ushort)value.Length));offset+=2;
        if(value.UsesExtendedLength){BinaryPrimitives.WriteUInt32LittleEndian(bytes[offset..],(uint)value.Length);offset+=4;}
        value.Bytes.CopyTo(bytes[offset..]);offset+=value.Length;
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
