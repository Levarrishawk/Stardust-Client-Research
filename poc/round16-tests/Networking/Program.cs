using Stardust.Core.SmokeTests;
if(args.Length!=6)throw new ArgumentException("Root/config/catalog/bank/peer/export required.");
SceneWorldPumpPeerScenarios.Run(args[..4],args[4],args[5],null);
