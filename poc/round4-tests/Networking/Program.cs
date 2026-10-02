using System.Text.Json;
using Stardust.Core.Protocol;
using Stardust.Core.SmokeTests;
SceneObjectLifecycleScenarios.Run();
if (args.Length == 2 && args[0] == "--export")
{
    Directory.CreateDirectory(args[1]);
    var c = SceneObjectLifecycleScenarios.CreateValue();
    var messages = new Dictionary<string, byte[]> {
        ["create"] = SceneObjectLifecycleCodec.EncodeCreate(c),
        ["destroy"] = SceneObjectLifecycleCodec.EncodeDestroy(new(c.ObjectId, 255)),
        ["containment"] = SceneObjectLifecycleCodec.EncodeContainment(new(c.ObjectId, 0xFE02030405060708, 0xFFFFFFFE)),
        ["end"] = SceneObjectLifecycleCodec.EncodeEndBaselines(new(c.ObjectId)) };
    foreach (var message in messages) File.WriteAllBytes(Path.Combine(args[1], message.Key + ".bin"), message.Value);
    var values = new Dictionary<string, object> {
        ["create"] = SceneObjectLifecycleCodec.DecodeCreate(Convert.FromHexString(SceneObjectLifecycleScenarios.CreateHex)),
        ["destroy"] = SceneObjectLifecycleCodec.DecodeDestroy(Convert.FromHexString(SceneObjectLifecycleScenarios.DestroyHex)),
        ["containment"] = SceneObjectLifecycleCodec.DecodeContainment(Convert.FromHexString(SceneObjectLifecycleScenarios.ContainmentHex)),
        ["end"] = SceneObjectLifecycleCodec.DecodeEndBaselines(Convert.FromHexString(SceneObjectLifecycleScenarios.EndHex)) };
    File.WriteAllText(Path.Combine(args[1], "decoded.json"), JsonSerializer.Serialize(values));
}
else if (args.Length != 0) throw new ArgumentException("Usage: [--export directory]");
