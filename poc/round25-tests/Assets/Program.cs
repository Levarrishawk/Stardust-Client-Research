if (args.Length > 0 && args[0] == "stf-regression") LocalizedStringTableScenarios.Run(args[1..]);
else if (args.Length > 0 && args[0] == "default-regression") SceneCreatureAlbedoScenarios.Run(args[1..]);
else LocalizedObjectNameScenarios.Run(args);
