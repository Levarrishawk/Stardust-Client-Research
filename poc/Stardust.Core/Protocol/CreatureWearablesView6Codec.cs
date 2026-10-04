using System.Buffers.Binary;
namespace Stardust.Core.Protocol;
public sealed record CreatureWearablesView6Limits(int MaximumPayloadBytes=1048576,int MaximumFields=256,int MaximumCommands=4096,int MaximumElements=65536,int MaximumStringBytes=65536,int MaximumTotalStringBytes=262144,long MaximumDecodedBytes=2097152,long MaximumWork=1048576);
public sealed class CreatureWearablesView6Command
{
 public CreatureWearablesView6Command(byte opcode,ushort index,IEnumerable<CreatureView6Wearable> values){var copy=TangibleView6Codec.Freeze(values,65535);if(copy.Any(v=>v is null)||opcode>4||(opcode is 0 or 4&&copy.Length!=0)||(opcode is 1 or 2&&copy.Length!=1)||(opcode is 3 or 4&&index!=0))throw new InvalidDataException("Wearable command shape");Opcode=opcode;Index=index;Values=Array.AsReadOnly(copy);}
 public byte Opcode{get;}public ushort Index{get;}public IReadOnlyList<CreatureView6Wearable> Values{get;}
}
public sealed class CreatureWearablesView6Update
{
 public CreatureWearablesView6Update(uint extent,uint counter,IEnumerable<CreatureWearablesView6Command> commands){Extent=extent;Counter=counter;var copy=TangibleView6Codec.Freeze(commands,65536);if(copy.Any(v=>v is null))throw new InvalidDataException("Null wearable command");Commands=Array.AsReadOnly(copy);}
 public ushort Field=>15;public uint Extent{get;}public uint Counter{get;}public IReadOnlyList<CreatureWearablesView6Command> Commands{get;}
}
public sealed class CreatureWearablesView6Delta
{
 public CreatureWearablesView6Delta(ulong objectId,ushort declaredCount,IEnumerable<CreatureWearablesView6Update> updates){ObjectId=objectId;DeclaredCount=declaredCount;var copy=TangibleView6Codec.Freeze(updates,4096);if(copy.Any(v=>v is null))throw new InvalidDataException("Null wearable update");Updates=Array.AsReadOnly(copy);}
 public ulong ObjectId{get;}public ushort DeclaredCount{get;}public IReadOnlyList<CreatureWearablesView6Update> Updates{get;}
}
public static class CreatureWearablesView6Codec
{
 internal static void Validate(CreatureWearablesView6Limits l){ArgumentNullException.ThrowIfNull(l);ReplicationEnvelopeCodec.ValidateBudget(l.MaximumPayloadBytes);if(l.MaximumPayloadBytes>8388608||l.MaximumFields is <0 or >4096||l.MaximumCommands is <0 or >65536||l.MaximumElements is <0 or >1048576||l.MaximumStringBytes is <0 or >1048576||l.MaximumTotalStringBytes is <0 or >4194304||l.MaximumDecodedBytes is <0 or >16777216||l.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(l));}
 private sealed class Meter(CreatureWearablesView6Limits l)
 {
  public long Bytes{get;private set;}public long Work=>fields+commands+elements+strings;private long fields,commands,elements,strings;
  private void Check(){if(Bytes+2>l.MaximumPayloadBytes||Bytes>l.MaximumDecodedBytes||fields>l.MaximumFields||commands>l.MaximumCommands||elements>l.MaximumElements||strings>l.MaximumTotalStringBytes||Work>l.MaximumWork)throw new InvalidDataException("Wearable global message budget");}
  public void Add(long n){Bytes=checked(Bytes+n);Check();}public void Field(){fields++;Check();}public void Command(){commands++;Check();}public void Element(){elements++;Check();}public void String(uint n){if(n>l.MaximumStringBytes)throw new InvalidDataException("Wearable string budget");strings+=n;Check();}
 }
 private ref struct Reader(ReadOnlySpan<byte> source,Meter meter)
 {
  private readonly ReadOnlySpan<byte> source=source;private readonly Meter meter=meter;public int Position{get;private set;}public int Remaining=>source.Length-Position;
  public ReadOnlySpan<byte> Take(int n){if(n<0||n>Remaining)throw new InvalidDataException("Truncated wearable body");var result=source.Slice(Position,n);meter.Add(n);Position+=n;return result;}
  public byte U8()=>Take(1)[0];public ushort U16()=>BinaryPrimitives.ReadUInt16LittleEndian(Take(2));public uint U32()=>BinaryPrimitives.ReadUInt32LittleEndian(Take(4));public ulong U64()=>BinaryPrimitives.ReadUInt64LittleEndian(Take(8));
  public void Field()=>meter.Field();public void Command()=>meter.Command();
  public CreatureView6Wearable? Element(bool own){meter.Element();ushort shortLength=U16();bool extended=shortLength==65535;uint length=extended?U32():shortLength;meter.String(length);var raw=Take(checked((int)length));uint arrangement=U32();ulong id=U64();uint crc=U32();return own?new(ReplicationByteString.Create(raw,extended),arrangement,id,crc):null;}
 }
 private static CreatureWearablesView6Delta? Read(ref Reader r,bool own,ulong id,ushort count)
 {
  var updates=own?new List<CreatureWearablesView6Update>():null;int fields=0;
  while(r.Remaining>0){r.Field();fields++;if(r.U16()!=15)throw new InvalidDataException("Unsupported wearable field");uint extent=r.U32(),counter=r.U32();ulong cursor=0;var commands=own?new List<CreatureWearablesView6Command>():null;
   while(cursor<extent){r.Command();byte op=r.U8();cursor++;ushort index=0;int n=0;if(op<=2)index=r.U16();switch(op){case 0:case 4:break;case 1:case 2:n=1;break;case 3:n=r.U16();cursor+=(uint)n;break;default:throw new InvalidDataException("Unknown wearable opcode");}var values=own?new CreatureView6Wearable[n]:null;for(int i=0;i<n;i++){var e=r.Element(own);if(own)values![i]=e!;}if(own)commands!.Add(new(op,index,values!));}if(own)updates!.Add(new(extent,counter,commands!));}
  if(fields==0)throw new InvalidDataException("Empty wearable plan");return own?new(id,count,updates!):null;
 }
 private static void Write(CreatureWearablesView6Delta p,Meter m,BinaryWriter? w)
 {
  if(p.Updates.Count==0)throw new InvalidDataException("Empty wearable plan");void U8(byte v){m.Add(1);w?.Write(v);}void U16(ushort v){m.Add(2);w?.Write(v);}void U32(uint v){m.Add(4);w?.Write(v);}void U64(ulong v){m.Add(8);w?.Write(v);}
  foreach(var u in p.Updates){m.Field();U16(15);U32(u.Extent);U32(u.Counter);ulong cursor=0;foreach(var c in u.Commands){if(cursor>=u.Extent)throw new InvalidDataException("Extra wearable command");cursor+=1UL+(c.Opcode==3?(ulong)c.Values.Count:0);m.Command();U8(c.Opcode);if(c.Opcode!=4)U16(c.Opcode==3?(ushort)c.Values.Count:c.Index);foreach(var e in c.Values){m.Element();var s=e.Customization;m.String((uint)s.Length);if(s.UsesExtendedLength){U16(65535);U32((uint)s.Length);}else U16(checked((ushort)s.Length));m.Add(s.Length);w?.Write(s.Bytes);U32(e.Arrangement);U64(e.ObjectId);U32(e.TemplateCrc);}}if(cursor<u.Extent)throw new InvalidDataException("Incomplete wearable extent");}
 }
 internal static (long Bytes,long Work) Measure(CreatureWearablesView6Delta p,CreatureWearablesView6Limits l){Validate(l);ArgumentNullException.ThrowIfNull(p);var m=new Meter(l);Write(p,m,null);return(m.Bytes,m.Work);}
 public static CreatureWearablesView6Delta Decode(ReadOnlySpan<byte> wire,CreatureWearablesView6Limits l){Validate(l);return Decode(ReplicationEnvelopeCodec.DecodeDelta(wire,l.MaximumPayloadBytes),l);}
 public static CreatureWearablesView6Delta Decode(ReplicationEnvelope e,CreatureWearablesView6Limits l){Validate(l);ArgumentNullException.ThrowIfNull(e);if(e.Kind!=ReplicationEnvelopeKind.Delta||e.RawTag!=CreatureView6Codec.RawTag||e.View!=6||e.PayloadSize>l.MaximumPayloadBytes||e.OpaqueFieldLength>l.MaximumDecodedBytes)throw new InvalidDataException("Expected wearable CREO6 delta");var scan=new Reader(e.Fields,new(l));_=Read(ref scan,false,e.ObjectId,e.DeclaredCount);var reader=new Reader(e.Fields,new(l));return Read(ref reader,true,e.ObjectId,e.DeclaredCount)!;}
 public static ReplicationEnvelope EncodeEnvelope(CreatureWearablesView6Delta p,CreatureWearablesView6Limits l){var cost=Measure(p,l);using var s=new MemoryStream(checked((int)cost.Bytes));using var w=new BinaryWriter(s);Write(p,new(l),w);return new(ReplicationEnvelopeKind.Delta,p.ObjectId,CreatureView6Codec.RawTag,6,p.DeclaredCount,s.ToArray(),l.MaximumPayloadBytes);}
 public static byte[] Encode(CreatureWearablesView6Delta p,CreatureWearablesView6Limits l)=>ReplicationEnvelopeCodec.EncodeDelta(EncodeEnvelope(p,l),l.MaximumPayloadBytes);
}
