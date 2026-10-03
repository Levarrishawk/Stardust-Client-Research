using System.Text.Json;
try {if(args.Length!=7)throw new ArgumentException("mode root config catalog bank peer-json export-json");var peer=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(args[5]))!;
if(args[0]=="state")SceneWorldCustomizationStateScenarios.Run(args[1..5],peer,args[6]);else if(args[0]=="consumer")SceneWorldObjectPresentationScenarios.Run(args[1..5],peer,args[6]);else if(args[0]=="pump")SceneWorldPresentationPumpScenarios.Run(args[1..5],peer,args[6]);else if(args[0]=="drain")SceneWorldPresentationDrainScenarios.Run(args[1..5],peer,args[6]);else throw new ArgumentException("Unknown mode");
}catch(Exception error){Console.Error.WriteLine(error);Environment.ExitCode=1;}
