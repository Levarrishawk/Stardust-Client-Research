using System.Text;
using System.Text.Json;
using Stardust.Core.SmokeTests;
try { File.WriteAllText(args[1],JsonSerializer.Serialize(SceneWorldTerrainPlaybackSlotScenarios.Run(args[0])),new UTF8Encoding(false)); Console.WriteLine("PASS actual terrain replacement slot"); } catch(Exception e) { Console.Error.WriteLine(e); Environment.ExitCode=1; }
