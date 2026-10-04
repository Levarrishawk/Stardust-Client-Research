using System.Text.Json;
var result=SceneCreatureMovementResourceScenarios.Run(args[0],args[1],args[2],args[3]);
var profile=SharedCreatureMovementInputScenarios.Run(args[5]);var authored=SharedCreatureAuthoredMovementInputScenarios.Run(args[6]);
File.WriteAllText(args[4],JsonSerializer.Serialize(new{Result=result,FrozenProfile=profile,FrozenAuthored=authored}));Console.WriteLine("MOVEMENT_RESOURCE_PASS");
SceneCreatureResourceScenarios.Run(new[]{args[0],args[1],args[3]});
SceneCreatureAlbedoScenarios.Run(new[]{args[0],args[1],args[2],args[3]});
