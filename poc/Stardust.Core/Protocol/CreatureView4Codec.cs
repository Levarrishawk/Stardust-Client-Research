using System.Buffers.Binary;
namespace Stardust.Core.Protocol;
public sealed record CreatureView4Limits(int MaximumPayloadBytes=1048576,int MaximumFields=256,int MaximumElements=65536,int MaximumTotalElements=196608,int MaximumStringBytes=65536,int MaximumTotalStringBytes=262144,long MaximumDecodedBytes=2097152,long MaximumWork=1048576);
public sealed class CreatureView4Collection<T>
{
    public CreatureView4Collection(uint counter,IEnumerable<T> values){Counter=counter;Values=Array.AsReadOnly(CreatureView4Codec.Freeze(values,65536));if(Values.Any(v=>v is null))throw new InvalidDataException("Null CREO4 entry");}
    public uint Counter{get;} public IReadOnlyList<T> Values{get;}
}
public sealed class CreatureView4SkillMod
{
    public CreatureView4SkillMod(byte rawCommand,ReplicationByteString name,int baseValue,int bonus){ArgumentNullException.ThrowIfNull(name);RawCommand=rawCommand;Name=name;BaseValue=baseValue;Bonus=bonus;}
    public byte RawCommand{get;} public ReplicationByteString Name{get;} public int BaseValue{get;} public int Bonus{get;}
}
public readonly record struct CreatureView4MissionPair(ulong OwnerId,ulong MissionId);
/// <summary>Owned wire snapshot entries. No map/set mutation or gameplay shape is implied.</summary>
public sealed class CreatureView4Baseline
{
    public CreatureView4Baseline(ulong objectId,ushort declaredCount,IEnumerable<uint> floatBits,ulong listenTo,CreatureView4Collection<int> encumbrances,CreatureView4Collection<CreatureView4SkillMod> skillMods,CreatureView4Collection<CreatureView4MissionPair> spaceMissionObjects)
    {
        ArgumentNullException.ThrowIfNull(encumbrances);ArgumentNullException.ThrowIfNull(skillMods);ArgumentNullException.ThrowIfNull(spaceMissionObjects);var copy=CreatureView4Codec.Freeze(floatBits,10);if(copy.Length!=10)throw new InvalidDataException("Ten raw CREO4 floats required");
        ObjectId=objectId;DeclaredCount=declaredCount;FloatBits=Array.AsReadOnly(copy);ListenTo=listenTo;Encumbrances=encumbrances;SkillMods=skillMods;SpaceMissionObjects=spaceMissionObjects;
    }
    public ulong ObjectId{get;} public ushort DeclaredCount{get;} public IReadOnlyList<uint> FloatBits{get;} public ulong ListenTo{get;}
    public CreatureView4Collection<int> Encumbrances{get;} public CreatureView4Collection<CreatureView4SkillMod> SkillMods{get;} public CreatureView4Collection<CreatureView4MissionPair> SpaceMissionObjects{get;}
    public ulong RawBits(ushort field)=>field==6?ListenTo:FloatBits[CreatureView4Codec.FloatSlot(field)];
}
public sealed class CreatureView4Update
{
    public CreatureView4Update(ushort index,ulong bits){if(index!=6){_=CreatureView4Codec.FloatSlot(index);if(bits>uint.MaxValue)throw new InvalidDataException("CREO4 raw F32 width");}Index=index;Bits=bits;}
    public ushort Index{get;} public ulong Bits{get;}
}
public sealed class CreatureView4Delta
{
    public CreatureView4Delta(ulong objectId,ushort declaredCount,IEnumerable<CreatureView4Update> updates){ObjectId=objectId;DeclaredCount=declaredCount;var copy=CreatureView4Codec.Freeze(updates,4096);if(copy.Any(v=>v is null))throw new InvalidDataException("Null CREO4 update");Updates=Array.AsReadOnly(copy);}
    public ulong ObjectId{get;} public ushort DeclaredCount{get;} public IReadOnlyList<CreatureView4Update> Updates{get;}
}
public static class CreatureView4Codec
{
    public const uint RawTag=0x4352454F; public const byte View=4;
    internal static T[] Freeze<T>(IEnumerable<T> values,int cap){ArgumentNullException.ThrowIfNull(values);var copy=new List<T>();foreach(var value in values){if(copy.Count==cap)throw new InvalidDataException("CREO4 collection hard limit");copy.Add(value);}return copy.ToArray();}
    internal static int FloatSlot(ushort field)=>field switch{0=>0,1=>1,4=>2,5=>3,7=>4,8=>5,9=>6,10=>7,11=>8,12=>9,_=>throw new InvalidDataException("Unsupported CREO4 scalar field; collection deltas2/3/13 remain closed")};
    internal static void Validate(CreatureView4Limits l){ArgumentNullException.ThrowIfNull(l);if(l.MaximumPayloadBytes is <2 or >8388608||l.MaximumFields is <0 or >4096||l.MaximumElements is <0 or >65536||l.MaximumTotalElements is <0 or >196608||l.MaximumStringBytes is <0 or >1048576||l.MaximumTotalStringBytes is <0 or >4194304||l.MaximumDecodedBytes is <0 or >16777216||l.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(l));}
    private sealed class Meter(CreatureView4Limits l)
    {
        public long Bytes{get;private set;} private long fields,elements,strings; public long Work=>fields+elements+strings;
        private void Check(){if(Bytes+2>l.MaximumPayloadBytes||Bytes+10>l.MaximumDecodedBytes||fields>l.MaximumFields||elements>l.MaximumTotalElements||strings>l.MaximumTotalStringBytes||Work>l.MaximumWork)throw new InvalidDataException("CREO4 global message budget");}
        public void Add(long n){Bytes=checked(Bytes+n);Check();} public void Field(){fields++;Check();}
        public void Collection(uint count){if(count>l.MaximumElements)throw new InvalidDataException("CREO4 per-collection cap");elements+=count;Check();}
        public void String(int n){if(n>l.MaximumStringBytes)throw new InvalidDataException("CREO4 string cap");strings+=n;Check();}
    }
    private ref struct Reader(ReadOnlySpan<byte> source,Meter meter)
    {
        private readonly ReadOnlySpan<byte> source=source;private readonly Meter meter=meter;private int pos;public int Remaining=>source.Length-pos;
        public ReadOnlySpan<byte> Take(int n){if(n<0||n>Remaining)throw new InvalidDataException("Truncated CREO4");var bytes=source.Slice(pos,n);meter.Add(n);pos+=n;return bytes;}
        public byte U8()=>Take(1)[0];public ushort U16()=>BinaryPrimitives.ReadUInt16LittleEndian(Take(2));public uint U32()=>BinaryPrimitives.ReadUInt32LittleEndian(Take(4));public int S32()=>BinaryPrimitives.ReadInt32LittleEndian(Take(4));public ulong U64()=>BinaryPrimitives.ReadUInt64LittleEndian(Take(8));public void Field()=>meter.Field();
        public int Count(){uint n=U32();meter.Collection(n);return checked((int)n);}
        public ReplicationByteString? Text(bool own){ushort n=U16();bool extended=n==65535;uint size=extended?U32():n;if(size>1048576)throw new InvalidDataException("CREO4 string hard cap");meter.String((int)size);var bytes=Take((int)size);return own?ReplicationByteString.Create(bytes,extended):null;}
    }
    private static CreatureView4Baseline? Baseline(ref Reader r,bool own,ulong id,ushort metadata)
    {
        uint[]? floats=own?new uint[10]:null;ulong listen=0;CreatureView4Collection<int>? enc=null;CreatureView4Collection<CreatureView4SkillMod>? skills=null;CreatureView4Collection<CreatureView4MissionPair>? missions=null;
        for(ushort field=0;field<14;field++)
        {
            r.Field();switch(field)
            {
                case 2:{int n=r.Count();uint counter=r.U32();var values=own?new int[n]:null;for(int i=0;i<n;i++){int v=r.S32();if(own)values![i]=v;}if(own)enc=new(counter,values!);break;}
                case 3:{int n=r.Count();uint counter=r.U32();var values=own?new CreatureView4SkillMod[n]:null;for(int i=0;i<n;i++){byte op=r.U8();var name=r.Text(own);int a=r.S32(),b=r.S32();if(own)values![i]=new(op,name!,a,b);}if(own)skills=new(counter,values!);break;}
                case 13:{int n=r.Count();uint counter=r.U32();var values=own?new CreatureView4MissionPair[n]:null;for(int i=0;i<n;i++){ulong a=r.U64(),b=r.U64();if(own)values![i]=new(a,b);}if(own)missions=new(counter,values!);break;}
                case 6:listen=r.U64();break;
                default:uint bits=r.U32();if(own)floats![FloatSlot(field)]=bits;break;
            }
        }
        if(r.Remaining!=0)throw new InvalidDataException("CREO4 baseline trailing bytes");return own?new(id,metadata,floats!,listen,enc!,skills!,missions!):null;
    }
    private static CreatureView4Delta? Delta(ref Reader r,bool own,ulong id,ushort metadata)
    {var values=own?new List<CreatureView4Update>():null;int count=0;while(r.Remaining>0){r.Field();count++;ushort field=r.U16();if(field!=6)_=FloatSlot(field);ulong bits=field==6?r.U64():r.U32();if(own)values!.Add(new(field,bits));}if(count==0)throw new InvalidDataException("Empty CREO4 scalar delta");return own?new(id,metadata,values!):null;}
    private static byte[] Fields(ReplicationEnvelope e,ReplicationEnvelopeKind kind,CreatureView4Limits l)
    {Validate(l);ArgumentNullException.ThrowIfNull(e);if(e.Kind!=kind||e.RawTag!=RawTag||e.View!=View||e.PayloadSize>l.MaximumPayloadBytes||e.OpaqueFieldLength+10L>l.MaximumDecodedBytes)throw new InvalidDataException("Expected bounded CREO4 envelope");return e.CopyOpaqueFields();}
    public static CreatureView4Baseline DecodeBaseline(ReadOnlySpan<byte> wire,CreatureView4Limits limits){Validate(limits);return DecodeBaseline(ReplicationEnvelopeCodec.DecodeBaseline(wire,limits.MaximumPayloadBytes),limits);}
    public static CreatureView4Baseline DecodeBaseline(ReplicationEnvelope envelope,CreatureView4Limits limits){var fields=Fields(envelope,ReplicationEnvelopeKind.Baseline,limits);var scan=new Reader(fields,new(limits));_=Baseline(ref scan,false,envelope.ObjectId,envelope.DeclaredCount);var reader=new Reader(fields,new(limits));return Baseline(ref reader,true,envelope.ObjectId,envelope.DeclaredCount)!;}
    public static CreatureView4Delta DecodeDelta(ReadOnlySpan<byte> wire,CreatureView4Limits limits){Validate(limits);return DecodeDelta(ReplicationEnvelopeCodec.DecodeDelta(wire,limits.MaximumPayloadBytes),limits);}
    public static CreatureView4Delta DecodeDelta(ReplicationEnvelope envelope,CreatureView4Limits limits){var fields=Fields(envelope,ReplicationEnvelopeKind.Delta,limits);var scan=new Reader(fields,new(limits));_=Delta(ref scan,false,envelope.ObjectId,envelope.DeclaredCount);var reader=new Reader(fields,new(limits));return Delta(ref reader,true,envelope.ObjectId,envelope.DeclaredCount)!;}
    private sealed class Writer(BinaryWriter? output,Meter meter)
    {
        public void Field()=>meter.Field();public void Count(int n){meter.Collection((uint)n);U32((uint)n);}public void U8(byte v){meter.Add(1);output?.Write(v);}public void U16(ushort v){meter.Add(2);output?.Write(v);}public void U32(uint v){meter.Add(4);output?.Write(v);}public void S32(int v){meter.Add(4);output?.Write(v);}public void U64(ulong v){meter.Add(8);output?.Write(v);}
        public void Text(ReplicationByteString s){meter.String(s.Length);if(s.UsesExtendedLength){U16(65535);U32((uint)s.Length);}else U16((ushort)s.Length);meter.Add(s.Length);if(output is not null)output.Write(s.CopyBytes());}
    }
    private static void WriteBaseline(Writer w,CreatureView4Baseline m)
    {for(ushort f=0;f<14;f++){w.Field();switch(f){case 2:w.Count(m.Encumbrances.Values.Count);w.U32(m.Encumbrances.Counter);foreach(int v in m.Encumbrances.Values)w.S32(v);break;case 3:w.Count(m.SkillMods.Values.Count);w.U32(m.SkillMods.Counter);foreach(var v in m.SkillMods.Values){w.U8(v.RawCommand);w.Text(v.Name);w.S32(v.BaseValue);w.S32(v.Bonus);}break;case 13:w.Count(m.SpaceMissionObjects.Values.Count);w.U32(m.SpaceMissionObjects.Counter);foreach(var v in m.SpaceMissionObjects.Values){w.U64(v.OwnerId);w.U64(v.MissionId);}break;case 6:w.U64(m.ListenTo);break;default:w.U32((uint)m.RawBits(f));break;}}}
    private static void WriteDelta(Writer w,CreatureView4Delta m){if(m.Updates.Count==0)throw new InvalidDataException("Empty CREO4 scalar delta");foreach(var u in m.Updates){w.Field();w.U16(u.Index);if(u.Index==6)w.U64(u.Bits);else{_=FloatSlot(u.Index);w.U32((uint)u.Bits);}}}
    internal static (long Bytes,long Work) MeasureBaseline(CreatureView4Baseline m,CreatureView4Limits l){Validate(l);ArgumentNullException.ThrowIfNull(m);var meter=new Meter(l);WriteBaseline(new(null,meter),m);return(meter.Bytes,meter.Work);}
    internal static (long Bytes,long Work) MeasureDelta(CreatureView4Delta m,CreatureView4Limits l){Validate(l);ArgumentNullException.ThrowIfNull(m);var meter=new Meter(l);WriteDelta(new(null,meter),m);return(meter.Bytes,meter.Work);}
    public static ReplicationEnvelope EncodeBaselineEnvelope(CreatureView4Baseline m,CreatureView4Limits l){var cost=MeasureBaseline(m,l);using var stream=new MemoryStream((int)cost.Bytes);using var output=new BinaryWriter(stream);WriteBaseline(new(output,new(l)),m);return new(ReplicationEnvelopeKind.Baseline,m.ObjectId,RawTag,View,m.DeclaredCount,stream.ToArray(),l.MaximumPayloadBytes);}
    public static ReplicationEnvelope EncodeDeltaEnvelope(CreatureView4Delta m,CreatureView4Limits l){var cost=MeasureDelta(m,l);using var stream=new MemoryStream((int)cost.Bytes);using var output=new BinaryWriter(stream);WriteDelta(new(output,new(l)),m);return new(ReplicationEnvelopeKind.Delta,m.ObjectId,RawTag,View,m.DeclaredCount,stream.ToArray(),l.MaximumPayloadBytes);}
    public static byte[] EncodeBaseline(CreatureView4Baseline m,CreatureView4Limits l)=>ReplicationEnvelopeCodec.EncodeBaseline(EncodeBaselineEnvelope(m,l),l.MaximumPayloadBytes);
    public static byte[] EncodeDelta(CreatureView4Delta m,CreatureView4Limits l)=>ReplicationEnvelopeCodec.EncodeDelta(EncodeDeltaEnvelope(m,l),l.MaximumPayloadBytes);
}
