try
{
    if (args.Length > 0 && args[0] == "--r7-regression") SceneStaticResourceScenarios.Run(args[1..]);
    else SwitchTextureStaticResourceScenarios.Run(args);
}
catch (Exception failure) { Console.Error.WriteLine(failure); Environment.ExitCode = 1; }
