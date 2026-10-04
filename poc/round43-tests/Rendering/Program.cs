using System.Text.Json;
using Stardust.Core.SmokeTests;
var fresh=TerrainSuppliedHeightFilteredSequenceScenarios.Run(args[0]);
var old=TerrainLayerInfluenceScenarios.Run(args[2]);
File.WriteAllText(args[1],JsonSerializer.Serialize(new {fresh.Cases,fresh.OwnershipChecks,Regression=old}));
