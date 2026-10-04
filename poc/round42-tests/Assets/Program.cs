using System.Text.Json;
using Stardust.Core.SmokeTests;
var result=TerrainFilterHeightScenarios.Run(args[0],args[2],args[3]);
File.WriteAllText(args[1],JsonSerializer.Serialize(result));
Console.WriteLine($"FHGT: {result.RawCases.Count} raw cases; {result.QueryCases.Count} query cases; {result.OwnershipChecks} ownership/budget checks.");
