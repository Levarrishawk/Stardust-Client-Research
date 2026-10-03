using Stardust.Core.SmokeTests;
try {
    SceneStaticPresentationScenarios.Run<SceneStaticPresentationProjection>(args[0],
        (incarnation,revision,names,visibility)=>new(incarnation,revision,names,visibility),
        (value,expected,previous)=>value.Validate(expected,previous),
        value=>(value.Incarnation,value.Revision,value.Names,value.Visibility));
    SceneWorldObjectNameScenarios.Run(args[0]);
    SceneWorldCustomizationContainerRaceScenarios.Run(args[0]);
    await SkeletalCustomizationRevisionScenarios.RunAsync();
} catch(Exception failure){Console.Error.WriteLine(failure);Environment.ExitCode=1;}
