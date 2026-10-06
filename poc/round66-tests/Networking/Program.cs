using Stardust.Core.SmokeTests;
try { SceneWorldCombinedMotionApplicationScenarios.Run(args[0], args[1]); }
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
