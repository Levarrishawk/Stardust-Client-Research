using System.Text.Json;
using Stardust.Core.Protocol;
using Stardust.Core.SmokeTests;
ReplicationEnvelopeScenarios.Run();
if (args.Length == 2 && args[0] == "--export")
{
    Directory.CreateDirectory(args[1]);
    var baseline = ReplicationEnvelopeCodec.DecodeBaseline(Convert.FromHexString(ReplicationEnvelopeScenarios.BaselineHex),6);
    var delta = ReplicationEnvelopeCodec.DecodeDelta(Convert.FromHexString(ReplicationEnvelopeScenarios.DeltaHex),3);
    File.WriteAllBytes(Path.Combine(args[1],"baseline.bin"),ReplicationEnvelopeCodec.EncodeBaseline(baseline,6));
    File.WriteAllBytes(Path.Combine(args[1],"delta.bin"),ReplicationEnvelopeCodec.EncodeDelta(delta,3));
    object Snapshot(ReplicationEnvelope e) => new { e.ObjectId, e.RawTag, e.View, e.DeclaredCount, e.PayloadSize, OpaqueHex=Convert.ToHexString(e.CopyOpaqueFields()) };
    File.WriteAllText(Path.Combine(args[1],"decoded.json"),JsonSerializer.Serialize(new { baseline=Snapshot(baseline), delta=Snapshot(delta) }));
}
else if (args.Length != 0) throw new ArgumentException("Usage: [--export directory]");
