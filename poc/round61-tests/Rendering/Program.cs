using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[1],JsonSerializer.Serialize(TerrainModernHeightPatchSetScenarios.Run(args[0],args[2])),new System.Text.UTF8Encoding(false));
Console.WriteLine("PASS modern supplied patch-set admission");
