using System.Text.Json;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

try {
if(args.Length!=3) throw new ArgumentException("baseline.bin delta.bin export.json");
var baseline=CreatureView3BaselineCodec.Decode(File.ReadAllBytes(args[0]),new(262144,65536,64,64,262144));
var delta=CreatureView3ScalarDeltaCodec.Decode(File.ReadAllBytes(args[1]),65536,64);
var registry=new SceneObjectRegistry(2,1);
using var stable=new SceneCreatureReplicationStore(registry,2);
using var exact=new SceneCreatureScalarStore(registry,2);
var create=new SceneCreateObjectByCrcMessage(baseline.ObjectId,0,0,0,1,0,0,0,2937962913,255);
var first=registry.Create(1,create)!;
var seed=stable.Seed(first.Incarnation,baseline)!;
var exactSeed=exact.Seed(first.Handle,baseline.Scalars)!;
registry.Contain(1,new(baseline.ObjectId,0,0xFFFFFFFE));
Check(!registry.IsCurrent(first.Handle)&&registry.IsCurrent(first.Incarnation),"Separate freshness");
Check(!exact.IsCurrent(exactSeed)&&stable.IsCurrent(seed)&&ReferenceEquals(stable.Find(first.Incarnation),seed),"Containment state retention");
registry.EndBaselines(1,new(baseline.ObjectId));
Check(stable.IsCurrent(seed)&&registry.Find(baseline.ObjectId)!.Incarnation==first.Incarnation,"Finalize retains");
var applied=stable.Apply(first.Incarnation,delta)!;
Check(!stable.IsCurrent(seed)&&stable.IsCurrent(applied)&&ReferenceEquals(applied.Baseline,baseline),"Immutable baseline overlay");
var parent=registry.Create(1,create with {ObjectId=1})!;
registry.Contain(1,new(baseline.ObjectId,1,7));
var contained=registry.Find(baseline.ObjectId)!;
registry.Destroy(1,new(1,0));
Check(!registry.IsCurrent(parent.Incarnation)&&!registry.IsCurrent(contained.Handle)&&registry.IsCurrent(first.Incarnation)&&stable.IsCurrent(applied),"Detached child identity");
var replaced=registry.Replace(1,create)!;
Check(replaced.Incarnation!=first.Incarnation&&!stable.IsCurrent(applied)&&stable.Apply(first.Incarnation,delta) is null,"Replace retires");
var reseeded=stable.Seed(replaced.Incarnation,baseline)!;
registry.Destroy(1,new(baseline.ObjectId,255));
var recreated=registry.Create(1,create)!;
Check(!stable.IsCurrent(reseeded)&&stable.Find(recreated.Incarnation) is null&&stable.Apply(replaced.Incarnation,delta) is null,"Recreate no inherited seed");
stable.Seed(recreated.Incarnation,baseline);
registry.Reset(2);
Check(stable.Snapshots().Count==0&&stable.RetainedDecodedBytes==0,"Reset releases");
File.WriteAllText(args[2],JsonSerializer.Serialize(new {
baseline.ObjectId,Initial=Export(seed.Scalars),Applied=Export(applied.Scalars),
BaselineHex=Convert.ToHexString(CreatureView3BaselineCodec.Encode(applied.Baseline,new(262144,65536,64,64,262144))),
First=first.Incarnation,Replacement=replaced.Incarnation,Recreated=recreated.Incarnation,
ExactRevisionExpired=true,StableContainmentAndFinalize=true,DetachedChildRetained=true,NoInheritedSeed=true,ResetReleased=true}));
registry.Close();
Console.WriteLine("PASS: exact revision versus stable incarnation; full baseline/scalar overlay, detach/replace/recreate/reset");
} catch(Exception e) {Console.Error.WriteLine(e);Environment.ExitCode=1;}
static void Check(bool v,string s) {if(!v)throw new InvalidDataException(s);}
static object Export(CreatureView3ScalarState s)=>new {s.Posture,s.Rank,s.LinkId,HeightBits=BitConverter.SingleToUInt32Bits(s.Height),s.Shock,s.States};
