if (args.Length > 0 && args[0] == "default-regression") SceneCreatureAlbedoScenarios.Run(args[1..]);
else if (args.Length > 0 && args[0] == "directory-regression") AssetCustomizationDirectoryScenarios.Run(args[1..]);
else AssetCustomizationContextScenarios.Run(args);
