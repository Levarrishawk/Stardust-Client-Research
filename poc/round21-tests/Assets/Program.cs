if (args.Length > 0 && args[0] == "default-regression") SceneCreatureAlbedoScenarios.Run(args[1..]);
else if (args.Length > 0 && args[0] == "dictionary-regression") CustomizationIdDictionaryScenarios.Run(args[1..]);
else if (args.Length > 0 && args[0] == "packed-regression") Stardust.Core.SmokeTests.PackedCustomizationScenarios.Run(args[1], args[2]);
else CustomizationVariableProjectionScenarios.Run(args);
