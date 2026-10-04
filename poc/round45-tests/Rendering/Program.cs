using System.Text.Json;
using Stardust.Core.SmokeTests;
var fresh=TerrainFilterFractalScenarios.Run(args[0]);
File.WriteAllText(args[1],JsonSerializer.Serialize(new {Fresh=fresh,CarrierRegression=TerrainLayerInfluenceScenarios.Run(args[2]),FilteredRegression=TerrainSuppliedHeightFilteredSequenceScenarios.Run(args[3])}));
