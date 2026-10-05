using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[1],JsonSerializer.Serialize(TerrainGeneratorHeightLayerSliceScenarios.Run(args[0])));
Console.WriteLine("PASS explicit owned layer slice selection scenarios");
