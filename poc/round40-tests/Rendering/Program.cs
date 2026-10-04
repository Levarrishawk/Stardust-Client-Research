using System.Text.Json;
using Stardust.Core.SmokeTests;
var result=TerrainLayerInfluenceScenarios.Run(args[0]);
File.WriteAllText(args[1],JsonSerializer.Serialize(result));
