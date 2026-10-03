try
{
    if (args.Length > 0 && args[0] == "dds-regression") DdsScenarios.Run();
    else if (args.Length > 0 && args[0] == "switch-regression") SwitchTextureStaticResourceScenarios.Run(args[1..]);
    else SwitchTextureFrameBundleScenarios.Run(args);
}
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
