using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[8],JsonSerializer.Serialize(SamplerResearch.Run(args[0])));
File.WriteAllText(args[9],JsonSerializer.Serialize(new {Fresh=TerrainFilterFractalScenarios.Run(args[1]),CarrierRegression=TerrainLayerInfluenceScenarios.Run(args[2]),FilteredRegression=TerrainSuppliedHeightFilteredSequenceScenarios.Run(args[3])}));
File.WriteAllText(args[10],JsonSerializer.Serialize(TerrainHeightFractalScenarios.Run(args[4],args[5],args[6])));
Console.WriteLine("Research sampler vectors and frozen conditional consumers completed.");
