using Stardust.Core.SmokeTests;
if(args.Length!=2)throw new ArgumentException("Peer/export required");
PackedCustomizationScenarios.Run(args[0],args[1]);
