using System.Text.Json;
using Stardust.Core.Proof;
using Stardust.Core.SmokeTests;
OfflineZoneSceneScenarios.Run();
if (args.Length == 4 && args[0] == "--peer" && args[2] == "--export")
{
    Dictionary<string, string> peer = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(args[1]))!;
    OfflineZoneSceneTranscript result = OfflineZoneSceneScenario.Run(peer);
    Directory.CreateDirectory(args[3]);
    File.WriteAllText(Path.Combine(args[3], "transcript.json"), JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
}
else if (args.Length != 0) throw new ArgumentException("Usage: [--peer fixture.json --export directory]");
