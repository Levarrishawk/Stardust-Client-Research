using Stardust.Core.SmokeTests;
if(args.Length!=2)throw new ArgumentException("Peer/export required");
TangibleMixedTranscriptScenarios.Run(args[0],args[1]);
