using System.Text.Json;
File.WriteAllText(args[1],JsonSerializer.Serialize(TerrainFractalFamilySetScenarios.Run(args[0])));
Console.WriteLine("PASS owned family set scenarios");
