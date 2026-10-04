using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[1],JsonSerializer.Serialize(TerrainConditionalFractalModeScenarios.Run(args[0])),new System.Text.UTF8Encoding(false));
Console.WriteLine("PASS conditional sampler/native lifecycle");
