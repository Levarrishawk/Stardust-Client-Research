using System.Text.Json;
using Stardust.Core.SmokeTests;
if(args.Length==2){File.WriteAllText(args[1],JsonSerializer.Serialize(SeedResearch.Run(args[0])));Console.WriteLine("Seed/noise research completed.");return;}
File.WriteAllText(args[8],JsonSerializer.Serialize(SeedResearch.Run(args[0])));
File.WriteAllText(args[9],JsonSerializer.Serialize(SamplerResearch.Run(args[1])));
File.WriteAllText(args[10],JsonSerializer.Serialize(new{Fresh=TerrainFilterFractalScenarios.Run(args[2]),CarrierRegression=TerrainLayerInfluenceScenarios.Run(args[3]),FilteredRegression=TerrainSuppliedHeightFilteredSequenceScenarios.Run(args[4])}));
File.WriteAllText(args[11],JsonSerializer.Serialize(TerrainHeightFractalScenarios.Run(args[5],args[6],args[7])));
Console.WriteLine("Seed/noise research and frozen conditional regressions completed.");
