using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[1],JsonSerializer.Serialize(TerrainFirstCurveFractalSparseBatchScenarios.Run(args[0])),new System.Text.UTF8Encoding(false));
Console.WriteLine("PASS owned sparse modern curve, empty receipts and frozen full equivalence");
