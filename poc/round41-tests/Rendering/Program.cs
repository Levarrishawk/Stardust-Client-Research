using System.Text.Json;
using Stardust.Core.SmokeTests;
var result=new {Height=TerrainSuppliedLayerHeightScenarios.Run(args[0]),FactoryRegression=TerrainLayerInfluenceScenarios.Run(args[2])};
File.WriteAllText(args[1],JsonSerializer.Serialize(result));
