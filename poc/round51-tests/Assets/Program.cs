using System.Text.Json;
File.WriteAllText(args[1],JsonSerializer.Serialize(TerrainGeneratorFractalFamilyScenarios.Run(args[0])));
File.WriteAllText(args[3],JsonSerializer.Serialize(TerrainFractalFamilySetScenarios.Run(args[2])));
Console.WriteLine("PASS generator publication and frozen family scenarios");
