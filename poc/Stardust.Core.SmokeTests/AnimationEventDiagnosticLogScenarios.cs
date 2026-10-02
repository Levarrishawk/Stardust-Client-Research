using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Stardust.Core.Assets;

internal static class AnimationEventDiagnosticLogScenarios
{
    internal static void Run()
    {
        byte[] bytes = Fixture(); var motion = Decode(bytes); var playback = new AnimationEventPlayback(motion, 10);
        Array.Clear(bytes); var log = new AnimationEventDiagnosticLog(playback, 3);
        Reject(() => new AnimationEventDiagnosticLog(playback, 0)); Reject(() => new AnimationEventDiagnosticLog(playback, 4097));
        Check(new AnimationEventDiagnosticLog(playback, 4096).Capacity == 4096, "Maximum capacity");
        RejectUnchanged(log, () => log.Deliver(playback.Advance(0)));
        var zero = playback.Advance(.01); log.Deliver(zero); var retained = log.Recent;
        var five = playback.Advance(.49); log.Deliver(five);
        Check(log.DeliveredTotal == 4 && log.GenerationDeliveredTotal == 4 && log.Count == 3 && log.Recent.Select(e => e.Occurrence).SequenceEqual(five), "Ring preserves authored duplicates");
        Check(retained.Count == 1 && retained[0].Occurrence.Frame == 0 && retained[0].Sequence == 1, "Owned old snapshot");
        RejectUnchanged(log, () => log.Deliver(five)); RejectUnchanged(log, () => log.Deliver(zero));
        log.ClearRecent(); Check(log.Count == 0 && log.DeliveredTotal == 4, "Clear only history");
        RejectUnchanged(log, () => log.Deliver(five)); log.SynchronizeGeneration(); RejectUnchanged(log, () => log.Deliver(zero));
        var end = playback.Advance(.5); log.Deliver(end);
        Check(log.Recent[0].Occurrence.Frame == 10 && log.Recent[1].Occurrence.Frame == 0 && log.DeliveredTotal == 6, "Terminal and next-zero order");
        var foreign = new AnimationEventPlayback(motion, 10); RejectUnchanged(log, () => log.Deliver(foreign.Advance(.5)));
        var empty = playback.Advance(0); playback.Restart(); RejectUnchanged(log, () => log.Deliver(empty)); RejectUnchanged(log, () => log.Deliver(end));
        var resetBatch = playback.Advance(.5); log.Deliver(resetBatch);
        Check(log.Generation == 2 && log.GenerationDeliveredTotal == 4 && log.DeliveredTotal == 10 && log.Count == 3, "Automatic generation synchronization");
        Check(retained[0].Generation == 1 && retained[0].Occurrence.Name == "same", "Snapshot across restart");
        playback.Reset(); log.SynchronizeGeneration();
        Check(log.Generation == 3 && log.Count == 0 && log.GenerationDeliveredTotal == 0 && log.DeliveredTotal == 10, "Explicit reset policy");
        RejectUnchanged(log, () => log.Deliver(resetBatch));
        playback.ReplaceClip(Decode(Fixture(replacement: true)), 8); log.SynchronizeGeneration();
        var replacement = playback.Advance(1); log.Deliver(replacement);
        Check(log.Generation == 4 && log.DeliveredTotal == 13 && log.GenerationDeliveredTotal == 3 && log.Recent.All(e => e.Generation == 4 && e.Occurrence.Name == "replacement"), "Clip replacement identity");
        // Acceptance requires ascending identities, even if a caller skips earlier batches.
        var orderedPlayback = new AnimationEventPlayback(motion, 10); var ordered = new AnimationEventDiagnosticLog(orderedPlayback, 1);
        var older = orderedPlayback.Advance(.5); var newer = orderedPlayback.Advance(.5); ordered.Deliver(newer);
        RejectUnchanged(ordered, () => ordered.Deliver(older)); Check(ordered.Recent[0].Occurrence.Frame == 0, "Capacity one wrap");
        var longPlayback = new AnimationEventPlayback(motion, 10); var bounded = new AnimationEventDiagnosticLog(longPlayback, 3);
        for (int cycle = 0; cycle < 1000; cycle++) bounded.Deliver(longPlayback.Advance(1));
        Check(bounded.Count == 3 && bounded.DeliveredTotal == 5001 && bounded.GenerationDeliveredTotal == 5001 && bounded.Recent[^1].Sequence == 5001, "Bounded ring over thousand cycles");
        var snapshot = bounded.Recent; bounded.ClearRecent(); Check(snapshot.Count == 3 && snapshot[^1].Sequence == 5001, "Snapshot survives ring clear");
        Console.WriteLine("PASS: diagnostic owner/generation/frontier, authored duplicates, atomic empty/stale/replay/foreign rejection, retained snapshots and bounded 1000-cycle ring");
    }
    internal static void Original(string root, string configuration)
    {
        byte[] config = File.ReadAllBytes(configuration);
        Check(Convert.ToHexString(SHA256.HashData(config)) == "22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524", "Configuration identity");
        using var reader = new StringReader(Encoding.UTF8.GetString(config)); using var overlay = ConfiguredTreOverlay.Open(root, reader);
        foreach (var sample in new[] {
            ("all_b_loc_walk_male", "FF86567A4CDBFC1EB574C5C8FC399144A37DAD218AA49DE0070AC9F1F19F65A6", (ushort)33, new ushort[] {7,23}),
            ("all_b_loc_run", "72D5842D2959D8438884B44F4220F3E323D221A745D849CB8726E905A8AAF2F4", (ushort)22, new ushort[] {6,17}) })
        {
            var entry = overlay.Lookup("appearance/animation/" + sample.Item1 + ".ans"); byte[] bytes = entry.Payload ?? throw new InvalidDataException("Pinned clip missing.");
            Check(entry.Priority == 30 && Convert.ToHexString(SHA256.HashData(bytes)) == sample.Item2, "Member identity");
            var clip = CompressedKeyframeDecoder.Decode(bytes); var motion = CompressedLocomotion.Create(clip); Array.Clear(bytes);
            Check(clip.FrameRate == 30 && clip.EndFrame == sample.Item3 && motion.Messages.Count == 1 && motion.Messages[0].Frames.SequenceEqual(sample.Item4), "Original records");
            var playback = new AnimationEventPlayback(motion, clip.FrameRate); var log = new AnimationEventDiagnosticLog(playback, 3);
            var actual = new List<AnimationEventOccurrence>(); AnimationEventPlaybackBatch? last = null;
            for (int tick = 0; tick < clip.EndFrame * 12; tick++)
            {
                var batch = playback.Advance(1.0 / 120); if (batch.Count == 0) continue; last = batch; actual.AddRange(batch); log.Deliver(batch);
            }
            var expected = Enumerable.Range(0, 3).SelectMany(c => sample.Item4.Select((f, i) => new AnimationEventOccurrence(c,0,i,"event_footstep",f))).ToArray();
            Check(actual.SequenceEqual(expected) && log.DeliveredTotal == 6 && log.Count == 3 && log.Recent.Select(e => e.Occurrence).SequenceEqual(expected.TakeLast(3)), "Original six events/ring");
            var retained = log.Recent; RejectUnchanged(log, () => log.Deliver(last!)); log.ClearRecent(); RejectUnchanged(log, () => log.Deliver(last!));
            playback.Restart(); log.SynchronizeGeneration(); RejectUnchanged(log, () => log.Deliver(last!));
            log.Deliver(playback.Advance(sample.Item4[0] / 30.0));
            Check(log.DeliveredTotal == 7 && log.GenerationDeliveredTotal == 1 && retained.Count == 3, "Original restart lifetime");
            Console.WriteLine($"PASS: pinned {sample.Item1} six footstep diagnostics/three cycles, last-three ring, clear/replay/stale and owned restart");
        }
    }
    private static void RejectUnchanged(AnimationEventDiagnosticLog log, Action action)
    {
        var before = (log.Generation, log.DeliveredTotal, log.GenerationDeliveredTotal, log.Count); var records = log.Recent; Reject(action);
        Check(before == (log.Generation,log.DeliveredTotal,log.GenerationDeliveredTotal,log.Count) && records.SequenceEqual(log.Recent), "Failed delivery changed diagnostic state");
    }
    private static void Reject(Action action) { try { action(); } catch (InvalidDataException) { return; } throw new Exception("Expected rejection"); }
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static CompressedLocomotion Decode(byte[] bytes) => CompressedLocomotion.Create(CompressedKeyframeDecoder.Decode(bytes));
    private static byte[] Fixture(bool replacement = false)
    {
        byte[] Chunk(string tag,byte[] payload) { byte[] length = new byte[4]; BinaryPrimitives.WriteInt32BigEndian(length,payload.Length); return Encoding.ASCII.GetBytes(tag).Concat(length).Concat(payload).ToArray(); }
        byte[] Form(string tag,params byte[][] children) => Chunk("FORM",Encoding.ASCII.GetBytes(tag).Concat(children.SelectMany(c=>c)).ToArray());
        byte[] Group(string name,params ushort[] frames) => Chunk("MESG",BitConverter.GetBytes((ushort)frames.Length).Concat(Encoding.ASCII.GetBytes(name+"\0")).Concat(frames.SelectMany(BitConverter.GetBytes)).ToArray());
        byte[] info=BitConverter.GetBytes(30f).Concat(new short[] {(short)(replacement?8:10),0,0,0,0,0}.SelectMany(BitConverter.GetBytes)).ToArray();
        byte[] messages = replacement ? Form("MSGS",Chunk("INFO",BitConverter.GetBytes((ushort)1)),Group("replacement",0,8)) : Form("MSGS",Chunk("INFO",BitConverter.GetBytes((ushort)2)),Group("same",0,5,5,10),Group("same",5));
        return Form("CKAT",Form("0001",Chunk("INFO",info),Form("XFRM"),messages));
    }
}
