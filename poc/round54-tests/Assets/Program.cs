using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[1],JsonSerializer.Serialize(TerrainGeneratorFractalFilterRuleScenarios.Run(args[0])));
Console.WriteLine("PASS explicit owned FFRA selection scenarios");
