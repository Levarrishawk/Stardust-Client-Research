using System.Text.Json;
using Stardust.Core.SmokeTests;
var scenario=TerrainBoundaryCircleInfluenceScenarios.Run(args[0]);
var roots=CircleSquareRootChecks.Run(args[0]);
File.WriteAllText(args[1],JsonSerializer.Serialize(new {scenario.Cases,SquareRoots=roots,scenario.BatchChecks,scenario.ArgumentNegatives}));
Console.WriteLine($"Circle: {scenario.Cases.Count} query cases, {roots.Count} exact sqrt cases, {scenario.BatchChecks} batch checks, {scenario.ArgumentNegatives} null negatives.");
