try
{
    DdsScenarios.Run();
    DdsHdrScenarios.Run();

    if (args.Length != 2) throw new ArgumentException("Explicit original archive root and configuration required.");
    DdsOpaqueColorScenarios.Probe(args[0], args[1]);
}
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
