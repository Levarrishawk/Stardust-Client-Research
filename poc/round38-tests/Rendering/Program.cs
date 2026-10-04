using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[1],JsonSerializer.Serialize(TerrainSampleCoordinateGridScenarios.Run(args[0])));
