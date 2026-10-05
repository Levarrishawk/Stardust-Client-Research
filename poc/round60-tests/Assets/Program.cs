using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[1],JsonSerializer.Serialize(TerrainHeightLayerSparseFirstCurveEvaluationScenarios.Run(args[0])));
Console.WriteLine("PASS modern first-curve height consumer scenarios");
