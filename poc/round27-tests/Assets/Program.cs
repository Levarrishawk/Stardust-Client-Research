try
{
    if (args.Length > 0 && args[0] == "template-regression") SharedCreatureTemplateScenarios.Run();
    else if (args.Length > 0 && args[0] == "preload-regression") LocalizedStringTableSetScenarios.Run(args[1..]);
    else if (args.Length > 0 && args[0] == "name-regression") LocalizedObjectNameScenarios.Run(args[1..]);
    else SharedTemplateLocalizedNameScenarios.Run(args);
}
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
