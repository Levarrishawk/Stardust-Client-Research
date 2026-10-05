using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[1],JsonSerializer.Serialize(TerrainModernHeightNormalsScenarios.Run(args[0])),new System.Text.UTF8Encoding(false));
Console.WriteLine("PASS modern stored-position normals and ownership");
