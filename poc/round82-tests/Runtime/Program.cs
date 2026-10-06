using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Stardust.Core.Offline;

if (args.Length != 1 || !Path.IsPathFullyQualified(args[0])) throw new ArgumentException("Explicit fresh private test directory required.");
string dir = Path.GetFullPath(args[0]);
// The caller supplies a fresh directory; never remove a preexisting tree.
if (Directory.Exists(dir) || File.Exists(dir) || !dir.Replace('\\','/').Contains("/local-private/", StringComparison.Ordinal)) throw new ArgumentException("Fresh local-private directory required.");
Directory.CreateDirectory(dir); Directory.CreateDirectory(Path.Combine(dir,"assets"));
string file = "input space & $literal.json"; File.WriteAllText(Path.Combine(dir,file),"{}");
File.WriteAllText(Path.Combine(dir,"second.json"),"{}");
string[] names = ["asset-root","asset-config","mixed-peer","catalog-profile","motion-peer","face-profile","customization-peer","customization-dictionary","customization-asset-context","customization-name-context","tangible-peer","tangible6-peer","creature6-full-peer","creature4-partial-peer","terrain-peer","terrain-material-profile"];
string[] flags = ["local-input","customization-wire","customization-scalars","customization-full","customization-extended","customization-named","terminal-animation","terminal-clock"];
JsonObject Fixture(int count=2)
{
    var inputs = new JsonObject(); foreach(string key in names) inputs[key]=key=="asset-root"?"assets":file;
    var options = new JsonObject(); for(int i=0;i<flags.Length;i++) options[flags[i]]=i%2==0;
    var profiles = new JsonArray(); for(int i=0;i<count;i++) profiles.Add(i%2==0?file:"second.json");
    return new JsonObject { ["Schema"]=1,["RunCount"]=count,["Inputs"]=inputs,["Options"]=options,["PointTargetProfiles"]=profiles };
}
byte[] Bytes(JsonObject value)=>Encoding.UTF8.GetBytes(value.ToJsonString());
int checks=0,negative=0;
void Need(bool condition) { checks++; if(!condition)throw new Exception("Failed check "+checks); }
void Reject(byte[] value,string? basis=null)
{
    try { OfflineScenarioManifest.Parse(value,basis??dir); }
    catch(InvalidDataException) { negative++;return; }
    throw new Exception("Expected rejection "+negative);
}
void Mutation(Action<JsonObject> edit) { var value=Fixture();edit(value);Reject(Bytes(value)); }
var original=Bytes(Fixture()); var manifest=OfflineScenarioManifest.Parse(original,dir);
Need(manifest.RunCount==2);
var first=manifest.ArgumentsForRun(1);var second=manifest.ArgumentsForRun(2);
Need(first.Length==38 && first[32]=="--terrain-point-target-profile" && first[33]==Path.Combine(dir,file));
Need(second[33]==Path.Combine(dir,"second.json"));
for(int i=0;i<names.Length;i++)Need(first[i*2]=="--"+names[i]&&first[i*2+1]==Path.Combine(dir,i==0?"assets":file));
Need(first.Skip(34).SequenceEqual(flags.Where((_,i)=>i%2==0).Select(v=>"--"+v)));
first[0]="mutated";Array.Fill(original,(byte)0);Need(manifest.ArgumentsForRun(1)[0]=="--asset-root");
Need(!ReferenceEquals(manifest.ArgumentsForRun(1),manifest.ArgumentsForRun(1)));
string cwd=Environment.CurrentDirectory;
try { Environment.CurrentDirectory=Path.GetPathRoot(dir)!;Need(OfflineScenarioManifest.Parse(Bytes(Fixture()),dir).ArgumentsForRun(1)[1]==Path.Combine(dir,"assets")); }
finally { Environment.CurrentDirectory=cwd; }
var absolute=Fixture();foreach(string key in names)absolute["Inputs"]![key]=Path.Combine(dir,key=="asset-root"?"assets":file);
Need(OfflineScenarioManifest.Parse(Bytes(absolute),dir).ArgumentsForRun(1).SequenceEqual(manifest.ArgumentsForRun(1)));
Need(OfflineScenarioManifest.Parse(Bytes(Fixture(32)),dir).ArgumentsForRun(32)[33]==Path.Combine(dir,"second.json"));
var allFalse=Fixture();foreach(string key in flags)allFalse["Options"]![key]=false;
Need(OfflineScenarioManifest.Parse(Bytes(allFalse),dir).ArgumentsForRun(1).Length==34);
var allTrue=Fixture();foreach(string key in flags)allTrue["Options"]![key]=true;
Need(OfflineScenarioManifest.Parse(Bytes(allTrue),dir).ArgumentsForRun(1).Skip(34).SequenceEqual(flags.Select(v=>"--"+v)));
foreach(int index in new[]{int.MinValue,0,-1,3,int.MaxValue}) {try{manifest.ArgumentsForRun(index);throw new Exception("Accepted index");}catch(ArgumentOutOfRangeException){negative++;}}
foreach(string key in new[]{"Schema","RunCount","Inputs","Options","PointTargetProfiles"})Mutation(v=>v.Remove(key));
foreach(string key in names)Mutation(v=>((JsonObject)v["Inputs"]!).Remove(key));
foreach(string key in flags)Mutation(v=>((JsonObject)v["Options"]!).Remove(key));
Mutation(v=>v["Unknown"]=1);Mutation(v=>v["Inputs"]!["Unknown"]=file);Mutation(v=>v["Options"]!["Unknown"]=true);
Mutation(v=>v["schema"]=1);Mutation(v=>v["Schema"]=2);Mutation(v=>v["Schema"]="1");Mutation(v=>v["Schema"]=true);
foreach(int count in new[]{0,-1,33})Mutation(v=>v["RunCount"]=count);
Mutation(v=>v["RunCount"]=1.5);Mutation(v=>v["RunCount"]="2");Mutation(v=>v["RunCount"]=true);
Mutation(v=>v["PointTargetProfiles"]=new JsonArray(file));Mutation(v=>v["PointTargetProfiles"]=file);Mutation(v=>v["PointTargetProfiles"]![0]=1);
Mutation(v=>v["Inputs"]=new JsonArray());Mutation(v=>v["Options"]=null);
foreach(JsonNode? invalid in new JsonNode?[]{JsonValue.Create(1),JsonValue.Create("true"),null})Mutation(v=>v["Options"]![flags[0]]=invalid?.DeepClone());
foreach(string path in new[]{"",new string('a',4097),"x\0y","https://example.invalid/a","res://Thing","file:///x","C:relative","//server/share",@"\\server\share","missing.json"})Mutation(v=>v["Inputs"]!["asset-config"]=path);
Mutation(v=>v["Inputs"]!["asset-root"]=file);Mutation(v=>v["Inputs"]!["asset-config"]="assets");Mutation(v=>v["PointTargetProfiles"]![1]="missing-profile.json");
// Lexical input is within its cap, but adding the explicit directory exceeds the compiled argument cap.
Mutation(v=>v["Inputs"]!["asset-config"]=new string('a',4096));
Reject(Bytes(Fixture()),".");Reject(Bytes(Fixture()),Path.Combine(dir,"missing"));Reject(Bytes(Fixture()),"res://x");
string json=Fixture().ToJsonString();
Reject(Encoding.UTF8.GetBytes(json.Replace("\"Schema\":1","\"Schema\":1,\"Schema\":1")));
Reject(Encoding.UTF8.GetBytes(json.Replace("\"Schema\":1","\"Schema\":1,\"\\u0053chema\":1")));
Reject(Encoding.UTF8.GetBytes(json.Replace("\"Schema\":1","\"Schema\":1.0")));
Reject(Encoding.UTF8.GetBytes(json.Replace("\"RunCount\":2","\"RunCount\":2e0")));
Reject(Encoding.UTF8.GetBytes(json.Replace("\"Schema\":1","\"Schema\":{\"a\":{\"x\":1,\"x\":2}}")));
Reject(Encoding.UTF8.GetBytes(json.Replace("\"local-input\":true","\"local-input\":true,\"local-input\":false")));
Reject(Encoding.UTF8.GetBytes(json.Replace("\"asset-root\":\"assets\"","\"asset-root\":\"assets\",\"asset-root\":\"assets\"")));
Reject(Encoding.UTF8.GetBytes(json.Replace("\"Schema\":1","\"Schema\":{\"a\":{\"b\":{\"c\":{\"d\":1}}}}")));
foreach(string malformed in new[]{"", "[]", "null", "{}", json+"{}",json[..^1]+",}", "/*comment*/"+json})Reject(Encoding.UTF8.GetBytes(malformed));
Reject([0x7B,0x22,0xFF,0x22,0x3A,0x31,0x7D]);Reject(new byte[OfflineScenarioManifest.MaximumJsonBytes+1]);
// Byte limit is inclusive, independently of text whitespace and string-length limits.
var exact=Encoding.UTF8.GetBytes(json+new string(' ',OfflineScenarioManifest.MaximumJsonBytes-Encoding.UTF8.GetByteCount(json)));
Need(OfflineScenarioManifest.Parse(exact,dir).RunCount==2);
try { OfflineScenarioManifest.Parse(null!,dir);throw new Exception("Accepted null bytes"); }catch(ArgumentNullException){negative++;}
Console.WriteLine(JsonSerializer.Serialize(new{Passed=true,Checks=checks,NegativeCases=negative,EngineNeutral=true,FactoryOrAssetLoad=false,InputBytesRetained=false,TestDirectory=dir}));
