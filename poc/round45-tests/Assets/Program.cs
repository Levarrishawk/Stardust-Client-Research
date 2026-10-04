using System.Text.Json;
using Stardust.Core.SmokeTests;
var result=TerrainHeightFractalScenarios.Run(args[0],args[2],args[3]);
File.WriteAllText(args[1],JsonSerializer.Serialize(result));
Console.WriteLine($"AHFR: {result.RawCases.Count} raw, {result.Queries.Count} supplied consumers, {result.OwnershipChecks} ownership/budget checks.");
