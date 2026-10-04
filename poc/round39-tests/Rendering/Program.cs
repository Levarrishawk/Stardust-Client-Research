using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[1],JsonSerializer.Serialize(TerrainCoordinateHeightPatchScenarios.Run(args[0])));
