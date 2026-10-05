using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[1],JsonSerializer.Serialize(TerrainFamilyHeightPatchScenarios.Run(args[0])),new System.Text.UTF8Encoding(false));
Console.WriteLine("PASS family height patch, immutable ownership and surface handoff");
