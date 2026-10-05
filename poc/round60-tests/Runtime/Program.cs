using System.Text;
using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[1],JsonSerializer.Serialize(SceneWorldSuppliedCurrentSpeedInputsScenarios.Run(args[0])),new UTF8Encoding(false));
Console.WriteLine("PASS paired current speed snapshots/native lifecycle/ownership guards");
