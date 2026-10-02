using Stardust.Core.Protocol;
using Stardust.Core.SmokeTests;
ZoneSceneTransitionScenarios.Run();
if (args.Length == 2 && args[0] == "--export")
{
    Directory.CreateDirectory(args[1]);
    File.WriteAllBytes(Path.Combine(args[1], "ready.bin"), SceneReadyProtocolCodec.Encode());
}
else if (args.Length != 0) throw new ArgumentException("Usage: [--export directory]");
