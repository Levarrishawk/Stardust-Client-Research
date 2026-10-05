using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[1],JsonSerializer.Serialize(TerrainGeneratorHeightFractalRuleScenarios.Run(args[0])));
Console.WriteLine("PASS explicit owned AHFR selection scenarios");
