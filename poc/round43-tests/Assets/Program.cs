using System.Text.Json;
using Stardust.Core.SmokeTests;
var result=TerrainFilterSlopeScenarios.Run(args[0],args[2],args[3]);
File.WriteAllText(args[1],JsonSerializer.Serialize(result));
Console.WriteLine($"FSLP: {result.RawCases.Count} raw cases; {result.OwnershipChecks} ownership/budget checks; influence deliberately unimplemented.");
