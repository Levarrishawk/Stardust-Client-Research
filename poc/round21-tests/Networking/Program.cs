using Stardust.Core.SmokeTests;
if(args.Length!=2)throw new ArgumentException("Peer/export required");
CreatureView3CustomizationDeltaScenarios.Run(args[0],args[1]);
