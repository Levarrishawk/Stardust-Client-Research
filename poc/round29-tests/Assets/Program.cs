try {
 if(args[0]=="router")SceneObjectResourceRouterScenarios.Run(args[1..]);
 else if(args[0]=="validation")SceneObjectResourceValidationScenarios.Run(args[1..]);
 else if(args[0]=="accounting")SceneObjectResourceAccountingScenarios.Run(args[1..]);
 else if(args[0]=="static")SwitchTextureStaticResourceScenarios.Run(args[1..3]);
 else if(args[0]=="creature")SceneCreatureAlbedoScenarios.Run(args[1..]);
 else SceneStaticSwitchFrameResourcesScenarios.Run(args);
} catch(Exception error) { Console.Error.WriteLine(error); Environment.ExitCode=1; }
