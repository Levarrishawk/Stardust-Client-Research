using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[2],JsonSerializer.Serialize(TerrainSingleSamplerHeightPatchScenarios.Run(args[0],args[1])),new System.Text.UTF8Encoding(false));
Console.WriteLine("PASS opt-in single sampler, frozen defaults, counts and immutable ownership");
