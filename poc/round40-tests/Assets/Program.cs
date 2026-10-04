using System.Text.Json;
using Stardust.Core.SmokeTests;
var result=TerrainBoundaryPolygonInfluenceScenarios.Run(args[0],args[2],args[3]);
File.WriteAllText(args[1],JsonSerializer.Serialize(new {result.Cases,result.OwnershipChecks,result.ArgumentNegatives,result.Original}));
Console.WriteLine($"Polygon: {result.Cases.Count} cases, {result.OwnershipChecks} ownership/budget/identity checks, {result.ArgumentNegatives} null negatives.");
