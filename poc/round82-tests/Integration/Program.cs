using System.Text.Json;
using Stardust.Core.Offline;

if(args.Length!=3)throw new ArgumentException("Explicit manifest, accepted base arguments and fresh private receipt required");
string path=Path.GetFullPath(args[0]),basePath=Path.GetFullPath(args[1]),output=Path.GetFullPath(args[2]);
if(File.Exists(output)||!output.Replace('\\','/').Contains("/local-private/",StringComparison.Ordinal))
    throw new ArgumentException("Fresh ignored output required");
byte[] raw=File.ReadAllBytes(path);
var scenario=OfflineScenarioManifest.Parse(raw,Path.GetDirectoryName(path)!);
var original=JsonSerializer.Deserialize<string[]>(File.ReadAllBytes(basePath))!;
using var document=JsonDocument.Parse(raw);
var profilePaths=document.RootElement.GetProperty("PointTargetProfiles");
int profileIndex=Array.IndexOf(original,"--terrain-point-target-profile");
if(profileIndex<0)throw new InvalidDataException("Original profile argument required");
for(int i=1;i<=scenario.RunCount;i++)
{
    var expected=(string[])original.Clone();
    expected[profileIndex+1]=Path.GetFullPath(profilePaths[i-1].GetString()!,Path.GetDirectoryName(path)!);
    if(!scenario.ArgumentsForRun(i).SequenceEqual(expected))throw new InvalidDataException("Producer/Core argument join differs from accepted explicit context");
}
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllText(output,JsonSerializer.Serialize(new {Passed=true,Runs=scenario.RunCount,
    ActualCoreParser=true,AcceptedBaseContextPreserved=true,ProfileOnlySubstitution=true,NativeExecuted=false})+"\n");
Console.WriteLine(File.ReadAllText(output));
