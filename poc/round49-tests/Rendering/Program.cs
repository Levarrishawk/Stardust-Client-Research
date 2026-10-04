using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[1],JsonSerializer.Serialize(TerrainConditionalFractalSamplerScenarios.Run(args[0])));
Console.WriteLine("PASS conditional sampler/native lifecycle");
