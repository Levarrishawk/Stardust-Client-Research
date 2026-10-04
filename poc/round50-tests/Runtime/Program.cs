using Stardust.Core.SmokeTests;
try { if(args[0]=="original")SceneWorldCurrentMovementAdmissionScenarios.Original(args[1],args[2],args[3],args[4],args[5]);else SceneWorldCurrentMovementAdmissionScenarios.Run(args[0],args[1]); }
catch(Exception e) { Console.Error.WriteLine(e); Environment.ExitCode=1; }
