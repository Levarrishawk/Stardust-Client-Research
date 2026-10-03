using Stardust.Core.SmokeTests;
try {
    SceneWorldCustomizationHeightRaceScenarios.Run(args[0]);
    SceneWorldCustomizationSourceScenarios.Run(args[0],args[1]);
    await SkeletalCustomizationRevisionScenarios.RunAsync();
} catch(Exception failure){Console.Error.WriteLine(failure);Environment.ExitCode=1;}
