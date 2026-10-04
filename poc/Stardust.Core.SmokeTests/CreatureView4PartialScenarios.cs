using System.Buffers.Binary;
using System.Text.Json;
using Stardust.Core.Protocol;
namespace Stardust.Core.SmokeTests;
public static class CreatureView4PartialScenarios
{
 static int checks,negatives,budgets,ownership;
 static readonly ushort[] Fields=[0,1,4,5,6,7,8,9,10,11,12];
 static CreatureView4Limits Message=>new(8388608,4096,0,0,0,0,16777216,16777216);
 static CreatureView4PartialLimits Wide=>new(4096,11,4096,16777216,33554432,16777216,Message);
 static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
 static void Reject(Action action,bool budget=false){try{action();}catch(Exception e)when(e is InvalidDataException or ArgumentException or OverflowException or NotSupportedException){negatives++;if(budget)budgets++;return;}throw new Exception("Expected rejection");}
 static object Row(CreatureView4PartialState s)=>new{s.ObjectId,s.Revision,s.KnownMask,s.KnownCount,s.DecodedBytes,s.KnownFields};
 public static void Run(string fixture,string output)
 {
  if(new FileInfo(fixture).Length>33554432)throw new InvalidDataException("Partial synthetic fixture cap");
  using var doc=JsonDocument.Parse(File.ReadAllBytes(fixture));var records=new List<object>();
  foreach(var c in doc.RootElement.GetProperty("Cases").EnumerateArray())
  {
   string name=c.GetProperty("Name").GetString()!;ulong oid=c.GetProperty("ObjectId").GetUInt64();var state=CreatureView4PartialState.Empty(oid,Wide,c.GetProperty("RevisionSeed").GetUInt64());
   Check(state.KnownMask==0&&state.KnownCount==0&&state.KnownFields.Count==0&&state.DecodedBytes==16,name+" no fabricated fields");
   foreach(ushort f in Fields)Check(!state.IsKnown(f)&&!state.TryGetField(f,out var absent)&&absent is null,name+" hidden absent backing word");
   var steps=new List<object>();
   foreach(var step in c.GetProperty("Steps").EnumerateArray())
   {
    var wire=Convert.FromHexString(step.GetProperty("Hex").GetString()!);var plan=CreatureView4Codec.DecodeDelta(wire,Message);var before=state;string old=JsonSerializer.Serialize(Row(before));var receipt=before.KnownFields.FirstOrDefault();
    var change=state.Apply(plan);Check(ReferenceEquals(change.Previous,state)&&change.Current.Revision==checked(state.Revision+1),name+" one modern revision");Check(change.Events.Count==plan.Updates.Count,name+" equality event policy");
    Check(JsonSerializer.Serialize(Row(before))==old,name+" previous state retained");ownership++;
    if(receipt is not null){Check(before.IsCurrent(receipt)&&!change.Current.IsCurrent(receipt),name+" exact stale field receipt");ownership++;}
    foreach(var f in change.Current.KnownFields)Check(change.Current.IsKnown(f.Index)&&change.Current.TryGetField(f.Index,out var value)&&ReferenceEquals(value,f)&&change.Current.IsCurrent(f),name+" presence/current receipt");
    var branch=Task.Run(()=>before.Apply(plan)).GetAwaiter().GetResult();Check(JsonSerializer.Serialize(Row(branch.Current))==JsonSerializer.Serialize(Row(change.Current))&&!change.Current.IsCurrent(branch.Current.KnownFields[0]),name+" independent immutable branch receipts");ownership++;
    var alias=(byte[])wire.Clone();var owned=CreatureView4Codec.DecodeDelta(alias,Message);Array.Clear(alias);Check(CreatureView4Codec.EncodeDelta(owned,Message).SequenceEqual(wire),name+" wire ownership");ownership++;
    var ua=plan.Updates.ToArray();var frozen=new CreatureView4Delta(oid,plan.DeclaredCount,ua);Array.Clear(ua);Check(CreatureView4Codec.EncodeDelta(frozen,Message).SequenceEqual(wire),name+" update array ownership");ownership++;
    Reject(()=>((IList<CreatureView4PartialField>)change.Current.KnownFields).Clear());Reject(()=>((IList<CreatureView4PartialEvent>)change.Events).Clear());ownership+=2;
    var cost=step.GetProperty("Cost");int N(string k)=>cost.GetProperty(k).GetInt32();long L(string k)=>cost.GetProperty(k).GetInt64();
    var ml=new CreatureView4Limits(N("Payload"),N("Updates"),0,0,0,0,L("MessageDecoded"),L("MessageWork"));
    var exact=new CreatureView4PartialLimits(N("Updates"),N("Known"),N("Events"),L("State"),L("Output"),L("Work"),ml);
    var accepted=before.Apply(plan,exact);Check(JsonSerializer.Serialize(Row(accepted.Current))==JsonSerializer.Serialize(Row(change.Current))&&accepted.WorkUsed==L("Work")&&accepted.DecodedOutputBytes==L("Output"),name+" exact budgets");budgets++;
    foreach(var low in new[]{exact with{MaximumUpdates=exact.MaximumUpdates-1},exact with{MaximumKnownFields=exact.MaximumKnownFields-1},exact with{MaximumEvents=exact.MaximumEvents-1},exact with{MaximumStateBytes=exact.MaximumStateBytes-1},exact with{MaximumOutputBytes=exact.MaximumOutputBytes-1},exact with{MaximumWork=exact.MaximumWork-1}}){Reject(()=>before.Apply(plan,low),true);Check(JsonSerializer.Serialize(Row(before))==old,name+" atomic whole-plan budget failure");ownership++;}
    foreach(var low in new[]{ml with{MaximumPayloadBytes=ml.MaximumPayloadBytes-1},ml with{MaximumFields=ml.MaximumFields-1},ml with{MaximumDecodedBytes=ml.MaximumDecodedBytes-1},ml with{MaximumWork=ml.MaximumWork-1}}){Reject(()=>before.Apply(plan,Wide with{MessageLimits=low}),true);Reject(()=>CreatureView4Codec.DecodeDelta(wire,low),true);}
    Reject(()=>before.Apply(new(oid^0x8000000000000000UL,plan.DeclaredCount,plan.Updates)));ownership++;
    int increment=wire.Length>2048?Math.Max(1,wire.Length/100):1;
    for(int cut=0;cut<wire.Length;cut+=increment){int at=cut;Reject(()=>CreatureView4Codec.DecodeDelta(wire.AsSpan(0,at),Message));}
    var valid=step.GetProperty("ValidPrefixes").EnumerateArray().Select(e=>e.GetInt32()).ToHashSet();
    for(int cut=25;cut<wire.Length;cut+=increment){var partial=wire[..cut];BinaryPrimitives.WriteUInt32LittleEndian(partial.AsSpan(19),(uint)(cut-23));if(valid.Contains(cut)){var prefix=CreatureView4Codec.DecodeDelta(partial,Message);Check(CreatureView4Codec.EncodeDelta(prefix,Message).SequenceEqual(partial),name+" legal prefix");}else Reject(()=>CreatureView4Codec.DecodeDelta(partial,Message));}
    // Valid scalar prefix followed by a forbidden collection/unknown field must never yield a plan.
    foreach(ushort field in new ushort[]{2,3,13,14,65535}){var malformed=new byte[wire.Length+2];wire.CopyTo(malformed,0);BinaryPrimitives.WriteUInt32LittleEndian(malformed.AsSpan(19),(uint)(malformed.Length-23));BinaryPrimitives.WriteUInt16LittleEndian(malformed.AsSpan(wire.Length),field);Reject(()=>CreatureView4Codec.DecodeDelta(malformed,Message));Check(JsonSerializer.Serialize(Row(before))==old,name+" late malformed plan retains state");ownership++;}
    steps.Add(new{Hex=Convert.ToHexString(CreatureView4Codec.EncodeDelta(plan,Message)),Initial=Row(before),Current=Row(change.Current),change.Events,change.DeclaredCount,change.WorkUsed,change.DecodedOutputBytes});state=change.Current;
   }
   records.Add(new{Name=name,Steps=steps});
  }
  foreach(ushort f in new ushort[]{2,3,13,14,65535}){Reject(()=>new CreatureView4Update(f,0));var empty=CreatureView4PartialState.Empty(1,Wide);Reject(()=>empty.IsKnown(f));Reject(()=>empty.TryGetField(f,out _));}
  Reject(()=>new CreatureView4Update(0,1UL<<32));Reject(()=>new CreatureView4Delta(1,0,[null!]));Reject(()=>new CreatureView4Delta(1,0,Enumerable.Repeat(new CreatureView4Update(0,0),4097)));
  var unknown=CreatureView4PartialState.Empty(1,Wide);Reject(()=>unknown.Apply(new(1,0,[])));Reject(()=>unknown.Apply(null!));Reject(()=>unknown.IsCurrent(null!));
  var max=CreatureView4PartialState.Empty(1,Wide,ulong.MaxValue);Reject(()=>max.Apply(new(1,0,[new(0,0)])));Check(max.KnownCount==0&&max.Revision==ulong.MaxValue,"overflow preserves unknown state");
  _=CreatureView4PartialState.Empty(0,new(0,0,0,16,16,11,Message));budgets++;
  foreach(var low in new[]{Wide with{MaximumStateBytes=15},Wide with{MaximumOutputBytes=15},Wide with{MaximumWork=10}})Reject(()=>CreatureView4PartialState.Empty(0,low),true);
  foreach(var bad in new[]{Wide with{MaximumUpdates=-1},Wide with{MaximumUpdates=4097},Wide with{MaximumKnownFields=12},Wide with{MaximumEvents=4097},Wide with{MaximumStateBytes=-1},Wide with{MaximumStateBytes=16777217},Wide with{MaximumOutputBytes=33554433},Wide with{MaximumWork=-1},Wide with{MessageLimits=Message with{MaximumFields=4097}}})Reject(()=>CreatureView4PartialState.Empty(1,bad));
  var one=unknown.Apply(new(1,0,[new(0,0)]),new(1,1,1,48,112,26,Message));Reject(()=>one.Current.Apply(new(1,0,[new(1,0)])),true);Check(one.Current.IsKnown(0)&&!one.Current.IsKnown(1),"successful override retains policy and provenance");
  var same=one.Current.Apply(new(1,65535,[new(0,0)]),Wide);Check(same.Events[0].OldWasKnown&&same.Events[0].OldBits==0&&same.Events[0].Provenance.DeclaredCount==65535,"received zero distinct from absence/equality emits");
  File.WriteAllText(output,JsonSerializer.Serialize(new{Checks=checks,Negatives=negatives,Budgets=budgets,Ownership=ownership,Records=records},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"R44_CREO4_PARTIAL_PASS cases={records.Count} checks={checks} negatives={negatives} budgets={budgets} ownership={ownership}");
 }
}
