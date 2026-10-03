using Stardust.Core.SmokeTests;
if(args.Length!=6)throw new ArgumentException("Root, configuration, catalog, animation bank, peer JSON and export JSON required.");
SceneMixedPresentationPeerScenarios.Run(args[0],args[1],args[2],args[3],args[4],args[5]);
