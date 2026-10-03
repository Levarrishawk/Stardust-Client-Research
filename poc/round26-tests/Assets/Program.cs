try
{
    if (args.Length > 0 && args[0] == "name-regression") LocalizedObjectNameScenarios.Run(args[1..]);
    else if (args.Length > 0 && args[0] == "stf-regression") LocalizedStringTableScenarios.Run(args[1..]);
    else LocalizedStringTableSetScenarios.Run(args);
}
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
