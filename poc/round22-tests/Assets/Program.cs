if (args.Length > 0 && args[0] == "default-regression") SceneCreatureAlbedoScenarios.Run(args[1..]);
else if (args.Length > 0 && args[0] == "dictionary-regression") CustomizationIdDictionaryScenarios.Run(args[1..]);
else AssetCustomizationDirectoryScenarios.Run(args);
