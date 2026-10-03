using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

if(args.Length!=3)throw new ArgumentException("stage-json private-original-table-context-json private-export-json");
using var fixture=JsonDocument.Parse(File.ReadAllBytes(args[0]));var data=fixture.RootElement;
Need(data.GetProperty("Version").GetInt32()==1 && data.GetProperty("Synthetic").GetBoolean(),"Explicit synthetic stages");
var (tables,tableCalls)=LoadTables(args[1]);
var baselineLimits=new CreatureView3BaselineLimits(4096,1024,128,64,4096);
var deltaLimits=new TangibleView3DeltaLimits(4096,32,32,32,1024,4096,64,64,64,32,1024,128);
byte[] baselineWire=Convert.FromHexString(data.GetProperty("Baseline").GetString()!);
var baseline=TangibleView3BaselineCodec.Decode(baselineWire,baselineLimits);
bool baselineExact=TangibleView3BaselineCodec.Encode(baseline,baselineLimits).SequenceEqual(baselineWire);
Need(baselineExact && baseline.DecodedBytes==data.GetProperty("BaselineDecodedBytes").GetInt64(),"Independent full TANO baseline");
var registry=new SceneObjectRegistry(2,1);
var created=registry.Create(1,SceneObjectLifecycleCodec.DecodeCreate(Convert.FromHexString(data.GetProperty("Create").GetString()!)))!;
using var store=new SceneTangibleReplicationStore(registry,new(2,data.GetProperty("MaximumStoreBytes").GetInt64(),64,64,1024,128,32,64));
var current=store.Seed(created.Incarnation,baseline)!;
registry.EndBaselines(1,new(baseline.ObjectId));
Need(ReferenceEquals(current.Names.NameFile,baseline.NameFile) && ReferenceEquals(current.Names.CustomNameUnits,baseline.CustomNameUnits) && ReferenceEquals(current.Customization,baseline.Customization),"Historic seed aliases");
var rows=new List<object>();Capture("baseline");bool deltaExact=true;
foreach(var input in data.GetProperty("Updates").EnumerateArray()) {
 byte[] wire=Convert.FromHexString(input.GetProperty("Hex").GetString()!);
 var plan=TangibleView3DeltaCodec.Decode(wire,deltaLimits);deltaExact&=TangibleView3DeltaCodec.Encode(plan,deltaLimits).SequenceEqual(wire);
 current=store.Apply(current,plan)??throw new InvalidDataException("Current stage rejected");
 Capture(input.GetProperty("Name").GetString()!);Array.Clear(wire);
}
var before=current;long retained=store.RetainedDecodedBytes;ulong revision=store.Revision;
bool late=Reject(()=>TangibleView3DeltaCodec.Decode(Convert.FromHexString(data.GetProperty("LateUnsupported").GetString()!),deltaLimits));
var growth=TangibleView3DeltaCodec.Decode(Convert.FromHexString(data.GetProperty("IntermediateOverBudget").GetString()!),deltaLimits);
bool capped=Reject(()=>store.Apply(current,growth));
bool atomic=ReferenceEquals(store.Find(created.Incarnation),before) && store.RetainedDecodedBytes==retained && store.Revision==revision && store.IsCurrent(before);
Need(late && capped && atomic,"Late invalid and intermediate-growth failure atomicity");
var reset=store.Seed(created.Incarnation,baseline)!;
Need(reset.Names.AdditionalDecodedBytes(baseline)==0 && ReferenceEquals(reset.Customization,baseline.Customization) && reset.Components.Keys.SequenceEqual(new[]{-3,9}),"Genuine baseline reset");
registry.Destroy(1,new(baseline.ObjectId,255));
var recreated=registry.Create(1,created.Create)!;
bool stale=store.Apply(created.Incarnation,growth) is null && store.Apply(reset,growth) is null && recreated.Incarnation!=created.Incarnation;
store.Close();registry.Close();bool drained=store.Snapshots().Count==0 && store.RetainedDecodedBytes==0;
Need(stale && drained && deltaExact,"Incarnation cleanup and exact wires");
File.WriteAllText(args[2],JsonSerializer.Serialize(new {
 CompleteWorldTransaction=false,BaselineExact=baselineExact,DeltaWiresExact=deltaExact,
 RejectedLateUnsupported=late,RejectedGrowthThenClear=capped,AtomicSnapshots=atomic,StaleIncarnationRejected=stale,Drained=drained,
 Tables=new {Requested=tables.RequestedPaths.Count,Loaded=tables.Tables.Count,Missing=tables.MissingPaths.Count,SourceBytes=tables.SourceBytes,Records=tables.TotalRecords,ResourceCalls=tableCalls},Rows=rows
},new JsonSerializerOptions{WriteIndented=true}));
Array.Clear(baselineWire);
Console.WriteLine("PASS: exact TANO wire -> owned current store -> original bounded table resolution; atomic failures and incarnation cleanup; world transaction deferred");

void Capture(string name) {
 var display=LocalizedObjectNameResolver.Resolve(current.Names,tables.Find);
 rows.Add(new {Name=name,NameFileHex=Convert.ToHexString(current.Names.NameFile.CopyBytes()),NameKeyHex=Convert.ToHexString(current.Names.NameKey.CopyBytes()),NameSpacer=current.Names.NameSpacer,
  NameCustomUnits=current.Names.CustomNameUnits,NameFileExtended=current.Names.NameFile.UsesExtendedLength,NameKeyExtended=current.Names.NameKey.UsesExtendedLength,
  CustomizationHex=Convert.ToHexString(current.Customization.CopyBytes()),CustomizationExtended=current.Customization.UsesExtendedLength,
  Components=current.Components.Keys,ComponentCounter=current.Components.Counter,Visibility=current.TangibleScalars.Visibility,
  DisplayText=display.DisplayText,Origin=display.Origin.ToString(),AdditionalNames=current.Names.AdditionalDecodedBytes(baseline),RetainedDecodedBytes=store.RetainedDecodedBytes,
  ComplexityBits=BitConverter.SingleToUInt32Bits(current.TangibleScalars.Complexity).ToString("X8"),ConditionDamage=current.TangibleScalars.ConditionDamage,UseCount=current.TangibleScalars.UseCount,
  BaselineRetained=ReferenceEquals(current.Baseline,baseline),HistoricBaselineWire=Convert.ToHexString(TangibleView3BaselineCodec.Encode(current.Baseline,baselineLimits))});
}
static bool Reject(Action action) {try{action();return false;}catch(InvalidDataException){return true;}}
static void Need(bool value,string message) {if(!value)throw new InvalidDataException(message);}
static (LocalizedStringTableSet Set,int Calls) LoadTables(string path) {
 byte[] input=File.ReadAllBytes(path);var buffers=new Dictionary<string,byte[]>();int calls=0;
 try {
  Need(input.Length<=32768,"Bounded private table context");using var document=JsonDocument.Parse(input);
  var pins=new Dictionary<string,(int Bytes,string Sha,int Records)> {
   ["string/en/obj_n.stf"]=(2527,"3EEA0D2B5E785F593B9C9180877665A495CD67B68BF3D6DAF74F7820FCAFB054",40),
   ["string/en/terminal_name.stf"]=(4610,"CDEDCD3ECE3E5FE6C7455B70760DDDB047B2F16281D740A1034D465F081FF603",57)};
  foreach(var row in document.RootElement.GetProperty("Tables").EnumerateArray()) {
   string resource=row.GetProperty("Path").GetString()!;Need(pins.TryGetValue(resource,out var pin),"Pinned table path");
   byte[] bytes=Convert.FromHexString(row.GetProperty("Hex").GetString()!);buffers.Add(resource,bytes);
   Need(bytes.Length==pin.Bytes && Convert.ToHexString(SHA256.HashData(bytes))==pin.Sha && row.GetProperty("Records").GetInt32()==pin.Records && row.GetProperty("Priority").GetInt32()==53,"Original table identity");
  }
  Need(buffers.Count==2,"Exactly two pinned original tables");
  var set=LocalizedStringTableSet.Build(buffers.Keys.Append("string/en/missing_native.stf"),resource=> {
   calls++;if(buffers.TryGetValue(resource,out var bytes))return new ReadOnlyMemory<byte>(bytes);
   return (ReadOnlyMemory<byte>?)null;
  },new(3,7137,97,1724,1723));
  return(set,calls);
 } finally {foreach(var bytes in buffers.Values)Array.Clear(bytes);Array.Clear(input);}
}
