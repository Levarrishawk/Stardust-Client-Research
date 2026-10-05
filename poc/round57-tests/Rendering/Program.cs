using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[1],JsonSerializer.Serialize(TerrainFirstCurveFractalBatchScenarios.Run(args[0])),new System.Text.UTF8Encoding(false));
Console.WriteLine("PASS explicitly modern first-curve batch and ownership checks");
