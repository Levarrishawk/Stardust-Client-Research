try
{
    if (args.Length != 5) throw new ArgumentException("Mode plus original root/config/catalog/animation-bank required.");
    string[] inputs = args[1..];
    switch (args[0])
    {
        case "router": SceneObjectResourceRouterScenarios.Run(inputs); break;
        case "creature-regression": SceneCreatureAlbedoScenarios.Run(inputs); break;
        case "static-regression": SwitchTextureStaticResourceScenarios.Run(inputs[..2]); break;
        default: throw new ArgumentException("Unknown explicit probe mode.");
    }
}
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
