using Stardust.Core.SmokeTests;
if(args.Length!=2)throw new ArgumentException("Peer/export required");
TangibleView3CodecScenarios.Run(args[0],args[1]);
