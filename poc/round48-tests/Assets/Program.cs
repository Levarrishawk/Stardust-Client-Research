using System.Text.Json;
using Stardust.Core.SmokeTests;
var result=TerrainFractalRawInputScenarios.Run(args[0]);File.WriteAllText(args[1],JsonSerializer.Serialize(result));
Console.WriteLine($"MFRC raw {result.Raw.Count}, full-U64 samples {result.Wide.Count}, ownership {result.OwnershipChecks}, budgets {result.BudgetChecks}.");
