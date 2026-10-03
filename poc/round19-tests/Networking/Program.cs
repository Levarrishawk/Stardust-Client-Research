using System.Text.Json;
using Stardust.Core.Protocol;
if(args.Length!=2)throw new ArgumentException("Peer/export required");
var fixtures=JsonSerializer.Deserialize<Fixture[]>(File.ReadAllBytes(args[0]))!;
var limits=new CreatureView3BaselineLimits(4096,1024,16,16,4096);
var results=new List<object>();int negative=0;
void Check(bool v){if(!v)throw new Exception("Outer-field preservation failed");}
void Reject(Action a){try{a();}catch(InvalidDataException){negative++;return;}throw new Exception("Expected rejection");}
foreach(var f in fixtures)
{
    byte[] source=Convert.FromHexString(f.Baseline),expected=Convert.FromHexString(f.Raw);
    var snapshot=CreatureView3BaselineCodec.Decode(source,limits);Array.Clear(source);
    Check(snapshot.Customization.CopyBytes().SequenceEqual(expected)&&snapshot.Customization.UsesExtendedLength==f.Extended);
    var borrowed=snapshot.Customization.CopyBytes();Array.Clear(borrowed);
    Check(snapshot.Customization.CopyBytes().SequenceEqual(expected));
    string encoded=Convert.ToHexString(CreatureView3BaselineCodec.Encode(snapshot,limits));Check(encoded==f.Baseline);
    byte[] delta=Convert.FromHexString(f.Delta);var env=ReplicationEnvelopeCodec.DecodeDelta(delta,4096);Array.Clear(delta);
    Check(Convert.ToHexString(ReplicationEnvelopeCodec.EncodeDelta(env,4096))==f.Delta);
    Reject(()=>CreatureView3ScalarDeltaCodec.Decode(Convert.FromHexString(f.Delta),4096,16));
    Reject(()=>CreatureView3MixedDeltaCodec.Decode(Convert.FromHexString(f.Delta),new(4096,16,16,16,16,16)));
    byte[] tano=Convert.FromHexString(f.Tano);var opaque=ReplicationEnvelopeCodec.DecodeBaseline(tano,4096);Array.Clear(tano);
    Check(Convert.ToHexString(ReplicationEnvelopeCodec.EncodeBaseline(opaque,4096))==f.Tano);
    byte[] full=Convert.FromHexString(f.Baseline);
    for(int i=0;i<full.Length;i++){int n=i;Reject(()=>CreatureView3BaselineCodec.Decode(full.AsSpan(0,n),limits));}
    Reject(()=>CreatureView3BaselineCodec.Decode(full.Concat(new byte[]{0}).ToArray(),limits));
    if(expected.Length>0)Reject(()=>CreatureView3BaselineCodec.Decode(full,limits with{MaximumStringBytes=expected.Length-1}));
    results.Add(new{f.Name,Raw=Convert.ToHexString(snapshot.Customization.CopyBytes()),Extended=snapshot.Customization.UsesExtendedLength,Baseline=encoded,Delta=Convert.ToHexString(ReplicationEnvelopeCodec.EncodeDelta(env,4096)),Tano=Convert.ToHexString(ReplicationEnvelopeCodec.EncodeBaseline(opaque,4096))});
}
File.WriteAllText(args[1],JsonSerializer.Serialize(new{Cases=results,NegativeChecks=negative},new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"Outer customization: {results.Count} exact cases, {negative} negative probes passed");
record Fixture(string Name,string Raw,bool Extended,string Baseline,string Delta,string Tano);
