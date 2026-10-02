using Stardust.Core.SmokeTests;
if (args.Length != 2) throw new ArgumentException("Peer JSON and export JSON required.");
MixedObjectLifecyclePeerScenarios.Run(args[0], args[1]);
