using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[1],JsonSerializer.Serialize(TerrainHeightLayerBoundaryAdmissionScenarios.Run(args[0])));
Console.WriteLine("PASS complete-grid boundary admission scenarios");
