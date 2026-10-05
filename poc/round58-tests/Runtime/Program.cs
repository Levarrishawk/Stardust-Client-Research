using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[1],JsonSerializer.Serialize(CreatureNominalSpeedPlanScenarios.Run(args[0])),new System.Text.UTF8Encoding(false));
Console.WriteLine("PASS atomic nominal plan native scenarios/owned receipts/repeat/thread checks");
