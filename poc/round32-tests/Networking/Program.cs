using Stardust.Core.SmokeTests;
if(args.Length!=2)throw new ArgumentException("peer.json export.json");
CreatureView6CodecScenarios.Run(args[0],args[1]);
