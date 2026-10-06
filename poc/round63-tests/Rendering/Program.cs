using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[1],JsonSerializer.Serialize(TerrainModernPatchSetRayQueryScenarios.Run(args[0],args[2])),new System.Text.UTF8Encoding(false));
Console.WriteLine("PASS modern ordered set ray query");
