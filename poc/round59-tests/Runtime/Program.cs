using System.Text;
using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[1],JsonSerializer.Serialize(CreatureSuppliedCurrentSpeedReaderScenarios.Run(args[0])),new UTF8Encoding(false));
Console.WriteLine("PASS supplied local reader native scenarios/ownership/repeat/thread checks");
