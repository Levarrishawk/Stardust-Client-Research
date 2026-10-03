using System.Text.Json;
try{if(args.Length!=6)throw new ArgumentException("root config catalog bank peer-json export-json");SceneWorldObjectPresentationScenarios.Run(args[..4],JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(args[4]))!,args[5]);}catch(Exception error){Console.Error.WriteLine(error);Environment.ExitCode=1;}
