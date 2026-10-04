using System.Text.Json;
using Stardust.Core.SmokeTests;
var result=TerrainRetainedFractalSampleScenarios.Run(args[0]);
File.WriteAllText(args[1],JsonSerializer.Serialize(result));
Console.WriteLine($"Retained supplied samples: {result.Cases.Count} cases, {result.OwnershipChecks} ownership, {result.BudgetChecks} budgets, {result.FactoryNegatives} factories.");
