using System.Buffers.Binary;
namespace Stardust.Core.Protocol;

public sealed record CreatureView6Limits(int MaximumPayloadBytes=1048576,int MaximumFields=256,
    int MaximumStringBytes=65536,int MaximumTotalStringBytes=262144,int MaximumElements=4096,
    int MaximumTotalElements=16384,long MaximumDecodedBytes=2097152,long MaximumWork=1048576);
public sealed class CreatureView6Vector<T>
{
    public CreatureView6Vector(uint counter,IEnumerable<T> values)
    {Counter=counter;var copy=TangibleView6Codec.Freeze(values,65536);if(copy.Any(v=>v is null))throw new InvalidDataException("Null CREO6 element");Values=Array.AsReadOnly(copy);}
    public uint Counter{get;} public IReadOnlyList<T> Values{get;}
}
public sealed class CreatureView6Wearable
{
    public CreatureView6Wearable(ReplicationByteString customization,uint arrangement,ulong objectId,uint templateCrc)
    {ArgumentNullException.ThrowIfNull(customization);Customization=customization;Arrangement=arrangement;ObjectId=objectId;TemplateCrc=templateCrc;}
    public ReplicationByteString Customization{get;} public uint Arrangement{get;} public ulong ObjectId{get;} public uint TemplateCrc{get;}
}
public sealed class CreatureView6Baseline
{
    public CreatureView6Baseline(ulong objectId,ushort declaredCount,uint passive,CreatureView6Vector<ulong> defenders,
        ushort level,ReplicationByteString performanceAnimation,ReplicationByteString moodString,ulong weaponId,ulong groupId,
        ulong inviterId,ulong inviteCounter,uint guild,ulong targetId,byte moodId,uint performanceStart,int performanceType,
        CreatureView6Vector<int> ham,CreatureView6Vector<int> maximumHam,CreatureView6Vector<CreatureView6Wearable> wearables,
        ReplicationByteString alternateAppearance,byte frozen)
    {
        ArgumentNullException.ThrowIfNull(defenders);ArgumentNullException.ThrowIfNull(performanceAnimation);ArgumentNullException.ThrowIfNull(moodString);
        ArgumentNullException.ThrowIfNull(ham);ArgumentNullException.ThrowIfNull(maximumHam);ArgumentNullException.ThrowIfNull(wearables);ArgumentNullException.ThrowIfNull(alternateAppearance);
        ObjectId=objectId;DeclaredCount=declaredCount;Passive=passive;Defenders=defenders;Level=level;PerformanceAnimation=performanceAnimation;MoodString=moodString;
        WeaponId=weaponId;GroupId=groupId;InviterId=inviterId;InviteCounter=inviteCounter;Guild=guild;TargetId=targetId;MoodId=moodId;
        PerformanceStart=performanceStart;PerformanceType=performanceType;Ham=ham;MaximumHam=maximumHam;Wearables=wearables;AlternateAppearance=alternateAppearance;Frozen=frozen;
    }
    public ulong ObjectId{get;} public ushort DeclaredCount{get;} public uint Passive{get;} public CreatureView6Vector<ulong> Defenders{get;}
    public ushort Level{get;} public ReplicationByteString PerformanceAnimation{get;} public ReplicationByteString MoodString{get;}
    public ulong WeaponId{get;} public ulong GroupId{get;} public ulong InviterId{get;} public ulong InviteCounter{get;} public uint Guild{get;}
    public ulong TargetId{get;} public byte MoodId{get;} public uint PerformanceStart{get;} public int PerformanceType{get;}
    public CreatureView6Vector<int> Ham{get;} public CreatureView6Vector<int> MaximumHam{get;} public CreatureView6Vector<CreatureView6Wearable> Wearables{get;}
    public ReplicationByteString AlternateAppearance{get;} public byte Frozen{get;}
}
public sealed class CreatureView6Update
{
    private CreatureView6Update(ushort index,ulong bits,ReplicationByteString? text,ulong second)
    {Index=index;Bits=bits;Text=text;SecondId=second;}
    public static CreatureView6Update Scalar(ushort index,ulong bits)
    {int width=CreatureView6Codec.ScalarWidth(index);if(width==0 || (width<8 && bits> (width==1?byte.MaxValue:width==2?ushort.MaxValue:uint.MaxValue)))throw new InvalidDataException("CREO6 scalar shape");return new(index,bits,null,0);}
    public static CreatureView6Update String(ushort index,ReplicationByteString value)
    {ArgumentNullException.ThrowIfNull(value);if(index is not (3 or 4 or 16))throw new InvalidDataException("CREO6 string index");return new(index,0,value,0);}
    public static CreatureView6Update Pair(ulong first,ulong second)=>new(7,first,null,second);
    public ushort Index{get;} public ulong Bits{get;} public ReplicationByteString? Text{get;} public ulong SecondId{get;}
    public int PerformanceType=>Index==12?unchecked((int)(uint)Bits):throw new InvalidOperationException("Not performance type");
}
public sealed class CreatureView6Delta
{
    public CreatureView6Delta(ulong objectId,ushort declaredCount,IEnumerable<CreatureView6Update> updates)
    {ObjectId=objectId;DeclaredCount=declaredCount;var copy=TangibleView6Codec.Freeze(updates,4096);if(copy.Any(v=>v is null))throw new InvalidDataException("Null CREO6 update");Updates=Array.AsReadOnly(copy);}
    public ulong ObjectId{get;} public ushort DeclaredCount{get;} public IReadOnlyList<CreatureView6Update> Updates{get;}
}
/// <summary>Owned complete CREO6 baseline and ordered scalar/string/pair deltas. No state or callbacks.</summary>
public static class CreatureView6Codec
{
    public const uint RawTag=0x4352454F;public const byte View=6;
    internal static int ScalarWidth(ushort index)=>index switch{0 or 8 or 11 or 12=>4,2=>2,5 or 6 or 9=>8,10 or 17=>1,_=>0};
    private static void Validate(CreatureView6Limits l)
    {
        ArgumentNullException.ThrowIfNull(l);ReplicationEnvelopeCodec.ValidateBudget(l.MaximumPayloadBytes);
        if(l.MaximumPayloadBytes>8388608 || l.MaximumFields is <0 or >4096 || l.MaximumStringBytes is <0 or >1048576 ||
            l.MaximumTotalStringBytes is <0 or >4194304 || l.MaximumElements is <0 or >65536 || l.MaximumTotalElements is <0 or >262144 ||
            l.MaximumDecodedBytes is <0 or >16777216 || l.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(l));
    }
    private static void Envelope(ReplicationEnvelope e,ReplicationEnvelopeKind kind,CreatureView6Limits l)
    {ArgumentNullException.ThrowIfNull(e);if(e.Kind!=kind || e.RawTag!=RawTag || e.View!=View || e.PayloadSize>l.MaximumPayloadBytes || e.OpaqueFieldLength+10L>l.MaximumDecodedBytes)throw new InvalidDataException("Expected bounded CREO6 envelope");}
    private sealed class Meter(CreatureView6Limits limits)
    {
        public long Bytes{get;private set;} private long fields,elements,strings;
        private void Check(){if(fields>limits.MaximumFields || elements>limits.MaximumTotalElements || strings>limits.MaximumTotalStringBytes || fields+elements+strings>limits.MaximumWork)throw new InvalidDataException("CREO6 aggregate budget");}
        public void Field(){fields++;Check();}
        public void Elements(uint count){if(count>limits.MaximumElements)throw new InvalidDataException("CREO6 vector capacity");elements+=count;Check();}
        public void String(uint count){if(count>limits.MaximumStringBytes)throw new InvalidDataException("CREO6 string capacity");strings+=count;Check();}
        public void Add(long count){Bytes=checked(Bytes+count);if(Bytes+2>limits.MaximumPayloadBytes || Bytes+10>limits.MaximumDecodedBytes)throw new InvalidDataException("CREO6 byte budget");}
    }
    private ref struct Reader
    {
        private readonly ReadOnlySpan<byte> source;private readonly Meter meter;
        public Reader(ReadOnlySpan<byte> source,Meter meter){this.source=source;this.meter=meter;Position=0;}
        public int Position{get;private set;} public int Remaining=>source.Length-Position;
        public ReadOnlySpan<byte> Take(int count){if(count<0||count>Remaining)throw new InvalidDataException("Truncated CREO6");var v=source.Slice(Position,count);meter.Add(count);Position+=count;return v;}
        public void Field()=>meter.Field();public byte U8()=>Take(1)[0];public ushort U16()=>BinaryPrimitives.ReadUInt16LittleEndian(Take(2));
        public uint U32()=>BinaryPrimitives.ReadUInt32LittleEndian(Take(4));public ulong U64()=>BinaryPrimitives.ReadUInt64LittleEndian(Take(8));
        public ReplicationByteString? String(bool own){ushort shortLength=U16();bool extended=shortLength==65535;uint length=extended?U32():shortLength;meter.String(length);var bytes=Take(checked((int)length));return own?ReplicationByteString.Create(bytes,extended):null;}
        public CreatureView6Vector<ulong>? Ids(bool own){uint n=U32(),counter=U32();meter.Elements(n);var raw=Take(checked((int)(8L*n)));if(!own)return null;var values=new ulong[(int)n];for(int i=0;i<values.Length;i++)values[i]=BinaryPrimitives.ReadUInt64LittleEndian(raw[(8*i)..]);return new(counter,values);}
        public CreatureView6Vector<int>? Ints(bool own){uint n=U32(),counter=U32();meter.Elements(n);var raw=Take(checked((int)(4L*n)));if(!own)return null;var values=new int[(int)n];for(int i=0;i<values.Length;i++)values[i]=BinaryPrimitives.ReadInt32LittleEndian(raw[(4*i)..]);return new(counter,values);}
        public CreatureView6Vector<CreatureView6Wearable>? Wearables(bool own){uint n=U32(),counter=U32();meter.Elements(n);var values=own?new CreatureView6Wearable[(int)n]:null;for(int i=0;i<n;i++){var text=String(own);uint arrangement=U32();ulong id=U64();uint crc=U32();if(own)values![i]=new(text!,arrangement,id,crc);}return own?new(counter,values!):null;}
        public void End(){if(Remaining!=0)throw new InvalidDataException("Trailing CREO6 bytes");}
    }
    private static CreatureView6Baseline? Baseline(ref Reader r,bool own,ulong id,ushort count)
    {
        for(int i=0;i<18;i++)r.Field();uint passive=r.U32();var defenders=r.Ids(own);ushort level=r.U16();var animation=r.String(own);var mood=r.String(own);
        ulong weapon=r.U64(),group=r.U64(),inviter=r.U64(),inviteCounter=r.U64();uint guild=r.U32();ulong target=r.U64();byte moodId=r.U8();uint start=r.U32();int type=unchecked((int)r.U32());
        var ham=r.Ints(own);var maximum=r.Ints(own);var wearables=r.Wearables(own);var alternate=r.String(own);byte frozen=r.U8();r.End();
        return own?new(id,count,passive,defenders!,level,animation!,mood!,weapon,group,inviter,inviteCounter,guild,target,moodId,start,type,ham!,maximum!,wearables!,alternate!,frozen):null;
    }
    public static CreatureView6Baseline DecodeBaseline(ReadOnlySpan<byte> wire,CreatureView6Limits limits)
    {Validate(limits);return DecodeBaseline(ReplicationEnvelopeCodec.DecodeBaseline(wire,limits.MaximumPayloadBytes),limits);}
    public static CreatureView6Baseline DecodeBaseline(ReplicationEnvelope envelope,CreatureView6Limits limits)
    {
        Validate(limits);Envelope(envelope,ReplicationEnvelopeKind.Baseline,limits);var scan=new Reader(envelope.Fields,new(limits));_=Baseline(ref scan,false,envelope.ObjectId,envelope.DeclaredCount);
        var r=new Reader(envelope.Fields,new(limits));return Baseline(ref r,true,envelope.ObjectId,envelope.DeclaredCount)!;
    }
    private static CreatureView6Delta? Delta(ref Reader r,bool own,ulong id,ushort count)
    {
        var values=own?new List<CreatureView6Update>():null;int fields=0;
        while(r.Remaining>0)
        {
            r.Field();fields++;ushort index=r.U16();CreatureView6Update? update=null;
            if(index is 3 or 4 or 16){var text=r.String(own);if(own)update=CreatureView6Update.String(index,text!);}
            else if(index==7){ulong first=r.U64(),second=r.U64();if(own)update=CreatureView6Update.Pair(first,second);}
            else{int width=ScalarWidth(index);ulong bits=width switch{1=>r.U8(),2=>r.U16(),4=>r.U32(),8=>r.U64(),_=>throw new InvalidDataException("Unsupported CREO6 delta field")};if(own)update=CreatureView6Update.Scalar(index,bits);}
            if(own)values!.Add(update!);
        }
        if(fields==0)throw new InvalidDataException("Empty CREO6 delta");r.End();return own?new(id,count,values!):null;
    }
    public static CreatureView6Delta DecodeDelta(ReadOnlySpan<byte> wire,CreatureView6Limits limits)
    {Validate(limits);return DecodeDelta(ReplicationEnvelopeCodec.DecodeDelta(wire,limits.MaximumPayloadBytes),limits);}
    public static CreatureView6Delta DecodeDelta(ReplicationEnvelope envelope,CreatureView6Limits limits)
    {Validate(limits);Envelope(envelope,ReplicationEnvelopeKind.Delta,limits);var scan=new Reader(envelope.Fields,new(limits));_=Delta(ref scan,false,envelope.ObjectId,envelope.DeclaredCount);var r=new Reader(envelope.Fields,new(limits));return Delta(ref r,true,envelope.ObjectId,envelope.DeclaredCount)!;}
    private sealed class Writer(BinaryWriter? output,Meter meter)
    {
        public void Field()=>meter.Field();public void U8(byte v){meter.Add(1);output?.Write(v);}public void U16(ushort v){meter.Add(2);output?.Write(v);}
        public void U32(uint v){meter.Add(4);output?.Write(v);}public void U64(ulong v){meter.Add(8);output?.Write(v);}
        public void String(ReplicationByteString v){meter.String((uint)v.Length);if(v.UsesExtendedLength){U16(65535);U32((uint)v.Length);}else{if(v.Length>=65535)throw new InvalidDataException("CREO6 short string overflow");U16((ushort)v.Length);}meter.Add(v.Length);output?.Write(v.Bytes);}
        public void Ids(CreatureView6Vector<ulong> v){meter.Elements((uint)v.Values.Count);U32((uint)v.Values.Count);U32(v.Counter);foreach(ulong id in v.Values)U64(id);}
        public void Ints(CreatureView6Vector<int> v){meter.Elements((uint)v.Values.Count);U32((uint)v.Values.Count);U32(v.Counter);foreach(int item in v.Values)U32(unchecked((uint)item));}
        public void Wearables(CreatureView6Vector<CreatureView6Wearable> v){meter.Elements((uint)v.Values.Count);U32((uint)v.Values.Count);U32(v.Counter);foreach(var item in v.Values){String(item.Customization);U32(item.Arrangement);U64(item.ObjectId);U32(item.TemplateCrc);}}
    }
    private static void WriteBaseline(Writer w,CreatureView6Baseline v)
    {
        for(int i=0;i<18;i++)w.Field();w.U32(v.Passive);w.Ids(v.Defenders);w.U16(v.Level);w.String(v.PerformanceAnimation);w.String(v.MoodString);
        w.U64(v.WeaponId);w.U64(v.GroupId);w.U64(v.InviterId);w.U64(v.InviteCounter);w.U32(v.Guild);w.U64(v.TargetId);w.U8(v.MoodId);w.U32(v.PerformanceStart);w.U32(unchecked((uint)v.PerformanceType));
        w.Ints(v.Ham);w.Ints(v.MaximumHam);w.Wearables(v.Wearables);w.String(v.AlternateAppearance);w.U8(v.Frozen);
    }
    private static void WriteDelta(Writer w,CreatureView6Delta v)
    {
        if(v.Updates.Count==0)throw new InvalidDataException("Empty CREO6 delta");
        foreach(var u in v.Updates){w.Field();w.U16(u.Index);if(u.Text is not null){w.String(u.Text);continue;}if(u.Index==7){w.U64(u.Bits);w.U64(u.SecondId);continue;}switch(ScalarWidth(u.Index)){case 1:w.U8((byte)u.Bits);break;case 2:w.U16((ushort)u.Bits);break;case 4:w.U32((uint)u.Bits);break;case 8:w.U64(u.Bits);break;default:throw new InvalidDataException("Unsupported CREO6 delta field");}}
    }
    public static ReplicationEnvelope EncodeBaselineEnvelope(CreatureView6Baseline value,CreatureView6Limits limits)
    {Validate(limits);ArgumentNullException.ThrowIfNull(value);var meter=new Meter(limits);WriteBaseline(new(null,meter),value);using var stream=new MemoryStream(checked((int)meter.Bytes));using var writer=new BinaryWriter(stream);WriteBaseline(new(writer,new(limits)),value);return new(ReplicationEnvelopeKind.Baseline,value.ObjectId,RawTag,View,value.DeclaredCount,stream.ToArray(),limits.MaximumPayloadBytes);}
    public static ReplicationEnvelope EncodeDeltaEnvelope(CreatureView6Delta value,CreatureView6Limits limits)
    {Validate(limits);ArgumentNullException.ThrowIfNull(value);var meter=new Meter(limits);WriteDelta(new(null,meter),value);using var stream=new MemoryStream(checked((int)meter.Bytes));using var writer=new BinaryWriter(stream);WriteDelta(new(writer,new(limits)),value);return new(ReplicationEnvelopeKind.Delta,value.ObjectId,RawTag,View,value.DeclaredCount,stream.ToArray(),limits.MaximumPayloadBytes);}
    public static byte[] EncodeBaseline(CreatureView6Baseline value,CreatureView6Limits limits)=>ReplicationEnvelopeCodec.EncodeBaseline(EncodeBaselineEnvelope(value,limits),limits.MaximumPayloadBytes);
    public static byte[] EncodeDelta(CreatureView6Delta value,CreatureView6Limits limits)=>ReplicationEnvelopeCodec.EncodeDelta(EncodeDeltaEnvelope(value,limits),limits.MaximumPayloadBytes);
}
