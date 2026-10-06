using Stardust.Core.SmokeTests;
try { OfflineSoeRootMotionSessionOwnerScenarios.Run(args[0], args[1]); }
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
