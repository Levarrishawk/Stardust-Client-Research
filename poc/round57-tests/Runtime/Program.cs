using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[1],JsonSerializer.Serialize(CreatureNominalTargetSpeedScenarios.Run(args[0])),new System.Text.UTF8Encoding(false));
Console.WriteLine("PASS supplied target native scenarios/ownership/repeat/thread checks");
