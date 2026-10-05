using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[1],JsonSerializer.Serialize(CreatureNominalSpeedTransitionScenarios.Run(args[0])),new System.Text.UTF8Encoding(false));
Console.WriteLine("PASS nominal speed native cases/repeated/threaded/prior-result ownership checks");
