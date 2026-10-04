using Stardust.Core.SmokeTests;
try { if(args.Length!=2)throw new ArgumentException("fixture-json receipt-json; run at repository root");CreatureView4FullScenarios.Run(args[0],args[1]); }
catch(Exception error){Console.Error.WriteLine(error);Environment.ExitCode=1;}
