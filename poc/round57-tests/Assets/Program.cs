using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[1],JsonSerializer.Serialize(TerrainHeightLayerFilteredInfluenceScenarios.Run(args[0])));
Console.WriteLine("PASS boundary-admitted FFRA composition scenarios");
