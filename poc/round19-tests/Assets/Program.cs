try
{
    if(args.Length>0 && args[0]=="default-regression") SceneCreatureAlbedoScenarios.Run(args[1..]);
    else SceneCreatureFaceResourceScenarios.Run(args);
}
catch(Exception error) {Console.Error.WriteLine(error);Environment.ExitCode=1;}
