using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[1],JsonSerializer.Serialize(TerrainSeededNoiseKernelScenarios.Run(args[0])));
Console.WriteLine("PASS kernel native cases/lifecycle");
