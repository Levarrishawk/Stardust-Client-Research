using System.Text.Json;
File.WriteAllText(args[1],JsonSerializer.Serialize(TerrainGeneratorIdentityScenarios.Run(args[0])));
File.WriteAllText(args[3],JsonSerializer.Serialize(TerrainGeneratorFractalFamilyScenarios.Run(args[2])));
File.WriteAllText(args[5],JsonSerializer.Serialize(TerrainFractalFamilySetScenarios.Run(args[4])));
Console.WriteLine("PASS cached identity, actual reservations and frozen bridge/family scenarios");
