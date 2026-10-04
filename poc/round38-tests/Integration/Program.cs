using System.Text.Json;
using Stardust.Core.Protocol;
if(args[0]=="--world") { try { SceneWorldCreatureView6FullScenarios.Run(args[1],args[2]); } catch(Exception error) { Console.Error.WriteLine(error);Environment.ExitCode=1; } return; }
using var fixture=JsonDocument.Parse(File.ReadAllText(args[0]));var root=fixture.RootElement;
var baseline=CreatureView6Codec.DecodeBaseline(ReplicationEnvelopeCodec.DecodeBaseline(Convert.FromHexString(root.GetProperty("Baseline").GetString()!),1024),new(MaximumPayloadBytes:1024));
var state=CreatureView6FullState.FromBaseline(baseline);int index=0,total=0;var records=new List<object>();
void Model(JsonElement step){string hex=Convert.ToHexString(CreatureView6Codec.EncodeBaseline(state.Model,new(MaximumPayloadBytes:1024)));if(hex!=step.GetProperty("ModelHex").GetString()||state.DecodedBytes!=step.GetProperty("StateBytes").GetInt64())throw new InvalidDataException("Independent complete model mismatch.");}
Model(root.GetProperty("Seed"));
foreach(var step in root.GetProperty("Steps").EnumerateArray())
{
 var plan=CreatureView6FullCodec.Decode(ReplicationEnvelopeCodec.DecodeDelta(Convert.FromHexString(step.GetProperty("Hex").GetString()!),1024),new(MaximumPayloadBytes:1024));
 var change=state.Apply(plan);state=change.Current;Model(step);index++;
 var events=change.Events.Select(e=>new{Kind=e.Kind.ToString(),e.Field,e.UpdateOrdinal,e.Opcode,e.Index,e.OldId,e.NewId,e.OldValue,e.NewValue}).ToArray();
 if(JsonSerializer.Serialize(events)!=JsonSerializer.Serialize(step.GetProperty("Events"))||change.WorkUsed!=step.GetProperty("Peak").GetProperty("Work").GetInt64()||state.Revision!=(ulong)index+1||change.DecodedOutputBytes!=state.DecodedBytes+48L*events.Length)throw new InvalidDataException("Independent complete event/work/revision mismatch.");
 total+=events.Length;records.Add(new{Index=index,state.Revision,state.DecodedBytes,change.WorkUsed,Events=events});
}
if(index!=5)throw new InvalidDataException("Five plans required.");
File.WriteAllText(args[1],JsonSerializer.Serialize(new{Synthetic=true,Plans=index,Events=total,Records=records}));
Console.WriteLine($"PASS complete host peer:5 plans/{total} events/full models/work/revisions; world join separate");
