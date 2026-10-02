try
{
    if (args.Length != 5) throw new ArgumentException("Explicit mode/root/config/catalog/bank required.");
    switch (args[0])
    {
        case "validation": SceneObjectResourceValidationScenarios.Run(args[1..]); break;
        case "router": SceneObjectResourceRouterScenarios.Run(args[1..]); break;
        case "creature-regression": SceneCreatureAlbedoScenarios.Run(args[1..]); break;
        case "static-regression": SwitchTextureStaticResourceScenarios.Run(args[1..3]); break;
        default: throw new ArgumentException("Unknown mode.");
    }
}
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
