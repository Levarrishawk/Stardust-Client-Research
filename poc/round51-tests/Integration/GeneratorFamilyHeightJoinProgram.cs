using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
using P=Stardust.Core.Geometry.TerrainHeightArithmeticPrecision;
if(args.Length!=2)throw new ArgumentException("Private fixture and output required");
using var document=JsonDocument.Parse(File.ReadAllBytes(args[0]));var data=document.RootElement;
if(data.GetProperty("Cases").GetArrayLength()!=486||data.GetProperty("Frames").GetArrayLength()!=12||data.GetProperty("OriginalTerrainParity").GetBoolean())throw new InvalidDataException("Bounded route fixture");
var bindings=new Dictionary<string,TerrainGeneratorFractalFamilies>();var frames=new List<object>();
foreach(var frame in data.GetProperty("Frames").EnumerateArray())
{
    string name=frame.GetProperty("Name").GetString()!;
    byte[] encoded=Convert.FromHexString(frame.GetProperty("GeneratorHex").GetString()!);
    if(Convert.ToHexString(SHA256.HashData(encoded))!=frame.GetProperty("GeneratorSha256").GetString())throw new InvalidDataException("Whole generator identity");
    var generator=TerrainGeneratorInput.Decode(encoded);var binding=TerrainGeneratorFractalFamilies.Bind(generator);
    if(binding.GroupOffset!=6602||binding.Families.Find(1) is null)throw new InvalidDataException("Explicit selected group/family identity");
    if(frame.GetProperty("Original").GetBoolean()&&(binding.GeneratorBytes!=139608||binding.GroupBytes!=5633||binding.Families.Entries.Count!=43))throw new InvalidDataException("One selected original generator");
    bindings.Add(name,binding);frames.Add(new{Name=name,binding.GeneratorSha256,binding.GroupSha256,binding.GroupBytes,binding.GroupOffset,Entries=binding.Families.Entries.Count});
}
var rows=new List<object>();
foreach(var c in data.GetProperty("Cases").EnumerateArray())
{
    uint U(string k)=>c.GetProperty(k).GetUInt32();int I(string k)=>c.GetProperty(k).GetInt32();
    string key=c.GetProperty("Frame").GetString()!;var binding=bindings[key];
    byte[] rule=Convert.FromHexString(c.GetProperty("RuleHex").GetString()!);
    var request=new TerrainFamilyHeightFractalRequest(1,1,0,0,U("X"),U("Z"),U("Initial"),U("Influence"));
    var controls=new TerrainFamilyHeightFractalControls(I("TablePrecision"),I("ConstructionPrecision"),I("QueryPrecision"),(P)I("ConsumerPrecision"));
    var result=TerrainFamilyHeightFractal.Evaluate(binding.Families,rule,request,controls);
    if(result.Origin!=TerrainConditionalFractalOrigin.ConditionalComputedMiss||result.Sample.Provenance==TerrainSuppliedFractalProvenance.CacheReloadF32||!ReferenceEquals(result.Raw,result.Family.Raw)||!ReferenceEquals(result.Affector.Sample,result.Sample)||result.Family.Id!=1||result.FamilySetSha256!=binding.GroupSha256||result.FamilySetInputBytes!=binding.GroupBytes)throw new InvalidDataException("Exact owned route/provenance");
    if(!ReferenceEquals(binding.Families.Find(1),result.Family)||result.Rule.Fields!.FamilyKeyBits!=1||result.Request!=request||result.Controls!=controls)throw new InvalidDataException("Selection or request mutation");
    rows.Add(new{Name=c.GetProperty("Name").GetString(),Frame=key,FamilyId=result.Family.Id,Sample=new{Magnitude=result.Sample.Magnitude,Exponent=result.Sample.Exponent,Negative=result.Sample.IsNegative},Height=result.Affector.Consumer.ResultHeightBits});
}
File.WriteAllText(args[1],JsonSerializer.Serialize(new{Frames=frames,Cases=rows}),new UTF8Encoding(false));
Console.WriteLine($"Actual generator/family/AHFR joins={rows.Count}; fresh-call controls; original terrain parity unclaimed");
