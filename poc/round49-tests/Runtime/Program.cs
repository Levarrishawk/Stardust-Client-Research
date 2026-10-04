using Stardust.Core.SmokeTests;
try { SceneWorldExplicitStopScenarios.Run(args[0],args[1]); }
catch(Exception e) { Console.Error.WriteLine(e); Environment.ExitCode=1; }
