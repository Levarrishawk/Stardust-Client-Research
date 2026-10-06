using Stardust.Core.SmokeTests;
try { OfflineSoeAcknowledgementFactoryScenarios.Run(args[0], args[1]); }
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
