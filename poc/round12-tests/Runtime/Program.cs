using System.Text.Json;
try { if(args.Length!=2)throw new ArgumentException("peer-json export-json");var peer=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(args[0]))!;SceneWorldMixedReplicationScenarios.Run(peer,args[1]); }
catch(Exception error) { Console.Error.WriteLine(error);Environment.ExitCode=1; }
