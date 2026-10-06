using System.Text.Json;
using Stardust.Core.SmokeTests;
File.WriteAllText(args[1],JsonSerializer.Serialize(TerrainGeneratorShaderFamiliesScenarios.Run(args[0])));
Console.WriteLine("PASS SGRP0006 raw shader-family scenarios");
