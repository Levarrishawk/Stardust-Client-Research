using Stardust.Core.SmokeTests;
try {
SceneWorldCustomizationSourceScenarios.Run(args[0],args[1],args.Length>2?args[2]:null);
await SkeletalCustomizationRevisionScenarios.RunAsync();
SkeletalCustomizationScenarios.Run();
} catch(Exception failure) {Console.Error.WriteLine(failure);Environment.ExitCode=1;}
