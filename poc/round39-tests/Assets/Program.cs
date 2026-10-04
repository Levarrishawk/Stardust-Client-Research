using System.Text.Json;
using Stardust.Core.SmokeTests;
var result=TerrainBoundaryRectangleInfluenceScenarios.Run(args[0],args[2],args[3]);
File.WriteAllText(args[1],JsonSerializer.Serialize(new {result.Cases,result.BatchChecks,result.ArgumentNegatives,result.Original}));
Console.WriteLine($"Rectangle: {result.Cases.Count} cases, {result.BatchChecks} ownership/budget checks, {result.ArgumentNegatives} null negatives.");
