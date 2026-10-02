using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Stardust.Core.Assets;

internal static class AnimationEventCursorScenarios
{
    internal static void Run()
    {
        byte[] bytes = Fixture();
        var source = CompressedLocomotion.Create(CompressedKeyframeDecoder.Decode(bytes));
        var cursor = new AnimationEventCursor(source); Array.Clear(bytes);
        Check(cursor.Advance(0, 0).Count == 0, "Initial zero tick");
        var zero = cursor.Advance(1, .25); Check(zero.Count == 1 && zero[0].Frame == 0, "Initial frame zero");
        Check(cursor.Advance(2, 4.999).Count == 0, "Fractional pre-boundary");
        var duplicates = cursor.Advance(3, 5);
        Check(duplicates.Count == 3 && duplicates.Select(e => (e.GroupIndex, e.OccurrenceIndex)).Distinct().Count() == 3, "Authored duplicates");
        Check(cursor.Advance(4, 5).Count == 0, "Repeated tick");
        var boundary = cursor.Advance(5, 10);
        Check(boundary.Count == 2 && boundary[0].Frame == 10 && boundary[0].Cycle == 0 && boundary[1].Frame == 0 && boundary[1].Cycle == 1, "Terminal then frame zero");
        cursor.Pause(); Check(cursor.Advance(6, 10).Count == 0, "Paused tick");
        RejectUnchanged(cursor, () => cursor.Advance(7, 11)); cursor.Resume();
        Check(cursor.Advance(7, 10.25).Count == 0, "Resume no duplicate boundary");
        foreach (double bad in new[] { 9.0, double.NaN, double.PositiveInfinity, AnimationEventCursor.MaximumPhase + 1 })
            RejectUnchanged(cursor, () => cursor.Advance(8, bad));
        foreach (double bad in new[] { 6.0, double.NaN, double.PositiveInfinity })
            RejectUnchanged(cursor, () => cursor.Advance(bad, 11));
        RejectUnchanged(cursor, () => cursor.Advance(7, 11));
        RejectUnchanged(cursor, () => cursor.Reset(double.NaN));
        cursor.Reset(20); Check(cursor.Phase == 0 && !cursor.IsPaused && cursor.Advance(21, .25).Count == 1, "Reset epoch");
        Check(zero.Count == 1 && duplicates.Count == 3 && boundary[0].Cycle == 0, "Returned batch lifetime");
        var single = new AnimationEventCursor(source, looping: false);
        Check(single.Advance(1, 10).Count == 5 && single.Advance(2, 10).Count == 0, "Nonloop terminal once");
        RejectUnchanged(single, () => single.Advance(3, 10.1));
        var eventBudget = new AnimationEventCursor(source, maximumEventsPerAdvance: 3);
        RejectUnchanged(eventBudget, () => eventBudget.Advance(1, 5));
        Check(eventBudget.Advance(1, .25).Count == 1 && eventBudget.Advance(2, 5).Count == 3, "Atomic event rejection recovery");
        var cycleBudget = new AnimationEventCursor(source, maximumCyclesPerAdvance: 1);
        RejectUnchanged(cycleBudget, () => cycleBudget.Advance(1, 20));
        Check(cycleBudget.Advance(1, 10).Count == 6, "Atomic cycle rejection recovery");
        var empty = CompressedLocomotion.Create(CompressedKeyframeDecoder.Decode(Fixture(empty: true)));
        Check(new AnimationEventCursor(empty).Advance(1, 30).Count == 0, "Empty stream progress");
        Reject(() => new AnimationEventCursor(source, maximumCyclesPerAdvance: 0));
        Reject(() => new AnimationEventCursor(source, maximumEventsPerAdvance: 262145));
        // Independent oracle enumerates authored identities and absolute integer times; no cursor bounds helper.
        for (int quarters = 1; quarters <= 160; quarters++)
        {
            double target = quarters / 4.0;
            var expected = Oracle(source, target);
            var whole = new AnimationEventCursor(source).Advance(target + 1, target);
            Check(whole.SequenceEqual(expected), "Independent whole interval");
            var split = new AnimationEventCursor(source); var actual = new List<AnimationEventOccurrence>();
            for (int i = 1; i <= quarters; i++)
            {
                actual.AddRange(split.Advance(i + 1, i / 4.0));
                Check(split.Advance(i + 1, i / 4.0).Count == 0, "Repeated partition tick");
            }
            Check(actual.SequenceEqual(expected), "Quarter partition invariance");
            Check(actual.Select(e => (e.Cycle, e.GroupIndex, e.OccurrenceIndex)).Distinct().Count() == actual.Count, "Occurrence uniqueness");
        }
        Console.WriteLine("PASS: animation cursor 160 independent/partition cases, fractional/end/zero/duplicate/loop/pause/reset/ownership and atomic guard/budget cases");
    }

    internal static void Original(string root, string configuration)
    {
        byte[] config = File.ReadAllBytes(configuration);
        Check(Convert.ToHexString(SHA256.HashData(config)) == "22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524", "Configuration identity");
        using var reader = new StringReader(Encoding.UTF8.GetString(config));
        using var overlay = ConfiguredTreOverlay.Open(root, reader);
        foreach (var sample in new[] {
            ("all_b_loc_walk_male", "FF86567A4CDBFC1EB574C5C8FC399144A37DAD218AA49DE0070AC9F1F19F65A6", (ushort)33, new ushort[] {7,23}),
            ("all_b_loc_run", "72D5842D2959D8438884B44F4220F3E323D221A745D849CB8726E905A8AAF2F4", (ushort)22, new ushort[] {6,17}) })
        {
            var entry = overlay.Lookup("appearance/animation/" + sample.Item1 + ".ans");
            byte[] bytes = entry.Payload ?? throw new InvalidDataException("Missing pinned clip.");
            Check(entry.Priority == 30 && Convert.ToHexString(SHA256.HashData(bytes)) == sample.Item2, "Original member identity");
            var motion = CompressedLocomotion.Create(CompressedKeyframeDecoder.Decode(bytes));
            Check(motion.EndFrame == sample.Item3 && motion.Messages.Count == 1 && motion.Messages[0].Name == "event_footstep" && motion.Messages[0].Frames.SequenceEqual(sample.Item4), "Original authored records");
            Array.Clear(bytes);
            var cursor = new AnimationEventCursor(motion);
            var actual = cursor.Advance(1, motion.EndFrame * 3.0);
            Check(actual.SequenceEqual(Oracle(motion, motion.EndFrame * 3.0)) && actual.Count == 6, "Original three cycles");
            var split = new AnimationEventCursor(motion); var partitions = new List<AnimationEventOccurrence>();
            for (int q = 1; q <= motion.EndFrame * 12; q++) partitions.AddRange(split.Advance(q, q / 4.0));
            Check(partitions.SequenceEqual(actual), "Original partition independence");
            Check(motion.CollectMessages(0, motion.EndFrame + 1).Select(e => e.Frame).SequenceEqual(sample.Item4), "Existing collector agreement");
            Console.WriteLine($"PASS: pinned {sample.Item1} MSGS, priority 30, six owned occurrences/three cycles and quarter partitions");
        }
    }

    private static AnimationEventOccurrence[] Oracle(CompressedLocomotion source, double target)
    {
        var result = new List<(double At, AnimationEventOccurrence Event)>();
        for (long cycle = 0; cycle <= (long)(target / source.EndFrame); cycle++)
            for (int g = 0; g < source.Messages.Count; g++)
                for (int o = 0; o < source.Messages[g].Frames.Count; o++)
                {
                    ushort frame = source.Messages[g].Frames[o]; double at = cycle * source.EndFrame + frame;
                    if (at <= target) result.Add((at, new(cycle, g, o, source.Messages[g].Name, frame)));
                }
        return result.OrderBy(e => e.At).ThenBy(e => e.Event.Cycle).ThenBy(e => e.Event.GroupIndex).ThenBy(e => e.Event.OccurrenceIndex).Select(e => e.Event).ToArray();
    }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Reject(Action action)
    { try { action(); } catch (InvalidDataException) { return; } throw new Exception("Expected rejection."); }
    private static void RejectUnchanged(AnimationEventCursor cursor, Action action)
    {
        var before = (cursor.Timestamp, cursor.Phase, cursor.IsPaused); Reject(action);
        Check(before == (cursor.Timestamp, cursor.Phase, cursor.IsPaused), "Rejection changed state");
    }
    private static byte[] Fixture(bool empty = false)
    {
        byte[] Chunk(string tag, byte[] payload)
        {
            byte[] length = new byte[4]; BinaryPrimitives.WriteInt32BigEndian(length, payload.Length);
            return Encoding.ASCII.GetBytes(tag).Concat(length).Concat(payload).ToArray();
        }
        byte[] Form(string tag, params byte[][] children) => Chunk("FORM", Encoding.ASCII.GetBytes(tag).Concat(children.SelectMany(c => c)).ToArray());
        byte[] Group(string name, params ushort[] frames) => Chunk("MESG", BitConverter.GetBytes((ushort)frames.Length)
            .Concat(Encoding.ASCII.GetBytes(name + "\0")).Concat(frames.SelectMany(BitConverter.GetBytes)).ToArray());
        byte[] info = BitConverter.GetBytes(30f).Concat(new short[] {10,0,0,0,0,0}.SelectMany(BitConverter.GetBytes)).ToArray();
        return Form("CKAT", Form("0001", Chunk("INFO", info), Form("XFRM"),
            empty ? Form("MSGS", Chunk("INFO", BitConverter.GetBytes((ushort)0))) :
            Form("MSGS", Chunk("INFO", BitConverter.GetBytes((ushort)2)), Group("same",0,5,5,10), Group("same",5))));
    }
}
