using System.Text.Json;
try { if(args.Length!=2) throw new ArgumentException("fixture-json export-json; run at repository root"); using var fixture=JsonDocument.Parse(File.ReadAllText(args[0])); SceneWorldTerrainMotionDriverScenarios.Run(fixture.RootElement,args[1]); }
catch(Exception error) { Console.Error.WriteLine(error); Environment.ExitCode=1; }
