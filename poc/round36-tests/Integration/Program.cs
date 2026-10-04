using System.Text.Json;
using Stardust.Core.Protocol;
if(args.Length!=2)throw new ArgumentException("Synthetic peer and private receipt paths required.");
if(new FileInfo(args[0]).Length>32768)throw new InvalidDataException("Peer file capacity.");
using var doc=JsonDocument.Parse(File.ReadAllBytes(args[0]));
var fixture=doc.RootElement;
if(!fixture.GetProperty("Synthetic").GetBoolean()||fixture.GetProperty("Version").GetInt32()!=1)throw new InvalidDataException("Explicit synthetic peer required.");
var baseline=CreatureView6Codec.DecodeBaseline(Convert.FromHexString(fixture.GetProperty("Baseline").GetString()!),new(MaximumPayloadBytes:1024));
var state=CreatureView6CompositeState.FromBaseline(baseline);
var records=new List<object>();
void CheckModel(JsonElement expected){if(Convert.ToHexString(CreatureView6Codec.EncodeBaseline(state.Model,new(MaximumPayloadBytes:1024)))!=expected.GetProperty("ModelHex").GetString()||state.DecodedBytes!=expected.GetProperty("StateBytes").GetInt64())throw new InvalidDataException("Independent full model/charge mismatch.");}
CheckModel(fixture.GetProperty("Seed"));int index=0,totalEvents=0;
foreach(var step in fixture.GetProperty("Steps").EnumerateArray()){
 var plan=CreatureView6CompositeCodec.Decode(Convert.FromHexString(step.GetProperty("Hex").GetString()!),new(MaximumPayloadBytes:1024));
 var change=state.Apply(plan);state=change.Current;CheckModel(step);index++;
 var events=change.Events.Select(e=>new{Kind=e.Kind.ToString(),e.Field,e.UpdateOrdinal,e.Opcode,e.ElementIndex,e.OldValue,e.NewValue}).ToArray();
 if(JsonSerializer.Serialize(events)!=JsonSerializer.Serialize(step.GetProperty("Events"))||change.WorkUsed!=step.GetProperty("Peak").GetProperty("Work").GetInt64()||state.Revision!=(ulong)index+1||change.DecodedOutputBytes!=state.DecodedBytes+24L*events.Length)throw new InvalidDataException("Independent event/work/revision mismatch.");
 totalEvents+=events.Length;records.Add(new{Index=index,Name=step.GetProperty("Name").GetString(),state.Revision,state.DecodedBytes,change.WorkUsed,Events=events});
}
if(index!=5||totalEvents!=11)throw new InvalidDataException("Expected five plans/eleven events.");
File.WriteAllText(args[1],JsonSerializer.Serialize(new{Synthetic=true,Plans=index,Events=totalEvents,Records=records},new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine("PASS composite host peer kernel:5 plans/11 ordered events/full models/work/revisions; world join still separate");
