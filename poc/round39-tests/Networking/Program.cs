using Stardust.Core.SmokeTests;
try{if(args.Length!=2)throw new ArgumentException("peer export");CreatureEncumbrancesView4Scenarios.Run(args[0],args[1]);}
catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=2;}
