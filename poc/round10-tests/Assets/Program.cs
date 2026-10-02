try
{
    if (args.Length > 0 && args[0] == "--default-regression") SceneCreatureResourceScenarios.Run(args[1..]);
    else SceneCreatureAlbedoScenarios.Run(args);
}
catch (Exception failure) { Console.Error.WriteLine(failure); Environment.ExitCode = 1; }
