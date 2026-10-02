try
{
    if (args.Length > 0 && args[0] == "--creature-regression") SceneCreatureResourceScenarios.Run(args[1..]);
    else SceneStaticResourceScenarios.Run(args);
}
catch (Exception failure) { Console.Error.WriteLine(failure); Environment.ExitCode = 1; }
