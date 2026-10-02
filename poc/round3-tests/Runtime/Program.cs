AnimationEventDiagnosticLogScenarios.Run();
if (args.Length == 2) AnimationEventDiagnosticLogScenarios.Original(args[0], args[1]);
else if (args.Length != 0) throw new ArgumentException("Expected optional archive root and pinned configuration path.");
