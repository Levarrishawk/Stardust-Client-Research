using System.Text.Json;
using Stardust.Core.Protocol;
using Stardust.Core.SmokeTests;
ZoneSceneProtocolScenarios.Run();
if (args.Length == 2 && args[0] == "--export")
{
    Directory.CreateDirectory(args[1]);
    File.WriteAllBytes(Path.Combine(args[1], "select.bin"), ZoneSceneProtocolCodec.EncodeSelectCharacter(0xF122334455667788));
    File.WriteAllText(Path.Combine(args[1], "scene.json"), JsonSerializer.Serialize(ZoneSceneProtocolCodec.DecodeCmdStartScene(Convert.FromHexString(ZoneSceneProtocolScenarios.SceneHex))));
}
else if (args.Length != 0) throw new ArgumentException("Usage: [--export directory]");
