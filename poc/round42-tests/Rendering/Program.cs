using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[1],JsonSerializer.Serialize(TerrainSuppliedLayerSequenceScenarios.Run(args[0])));
