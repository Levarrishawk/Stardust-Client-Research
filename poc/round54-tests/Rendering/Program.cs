using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[1],JsonSerializer.Serialize(TerrainFamilyHeightFieldScenarios.Run(args[0])),new System.Text.UTF8Encoding(false));
Console.WriteLine("PASS per-cell height field, exact skips, ownership and prior-policy equivalence");
