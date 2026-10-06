using System.Text;
using System.Text.Json;
using Stardust.Core.SmokeTests;
try{File.WriteAllText(args[1],JsonSerializer.Serialize(SceneWorldCurrentSpeedPlanPlaybackScenarios.Run(args[0])),new UTF8Encoding(false));Console.WriteLine("PASS actual native current-plan playback/fault/ownership guards");}
catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}
