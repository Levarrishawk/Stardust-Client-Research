using Stardust.Core.SmokeTests;
try { if(args.Length!=2)throw new ArgumentException("fixture-json receipt-json; run at repository root");SceneCreatureView4SupportedStoreScenarios.Run(args[0],args[1]);PreparedScenarios.Run(); }
catch(Exception error){Console.Error.WriteLine(error);Environment.ExitCode=1;}
