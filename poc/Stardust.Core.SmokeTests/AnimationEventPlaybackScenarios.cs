using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Stardust.Core.Assets;

internal static class AnimationEventPlaybackScenarios
{
    internal static void Run()
    {
        byte[] bytes = Fixture(); var source = Decode(bytes); var playback = new AnimationEventPlayback(source, 10);
        Array.Clear(bytes);
        Check(playback.Advance(0).Count == 0 && playback.ActiveSeconds == 0 && playback.Generation == 1, "Zero tick");
        var initial = playback.Advance(.01); Check(initial.Count == 1 && initial[0].Frame == 0, "Initial zero");
        Check(playback.Advance(.489).Count == 0, "Fraction before event");
        var atFive = playback.Advance(.001); Check(atFive.Count == 3 && playback.UnwrappedPhase == 5, "Integer snap/duplicate occurrence");
        var boundary = playback.Advance(.5);
        Check(boundary.Count == 2 && boundary[0].Frame == 10 && boundary[0].Cycle == 0 && boundary[1].Frame == 0 && boundary[1].Cycle == 1 && playback.CurrentFrame == 0, "Loop boundary");
        playback.Pause(); var paused = State(playback); Check(playback.Advance(60).Count == 0 && State(playback) == paused, "Paused time discard");
        RejectUnchanged(playback, () => playback.Advance(-1)); playback.Resume();
        playback.SetFocused(false); var unfocused = State(playback); Check(playback.Advance(60).Count == 0 && State(playback) == unfocused, "Focus time discard");
        playback.SetFocused(true); Check(playback.Advance(.01).Count == 0 && Close(playback.UnwrappedPhase, 10.1), "No catch-up");
        Check(playback.IsCurrent(initial) && initial.Generation == 1 && initial.Count == 1, "Owned earlier current batch");
        var other = new AnimationEventPlayback(source, 10); Check(!other.IsCurrent(initial), "Owner identity despite matching generation");
        foreach (double bad in new[] {-1.0, 60.01, double.NaN, double.PositiveInfinity, double.Epsilon}) RejectUnchanged(playback, () => playback.Advance(bad));
        foreach (float bad in new[] {0f, -1f, 1001f, float.NaN, float.PositiveInfinity})
        {
            Reject(() => new AnimationEventPlayback(source, bad));
            RejectUnchanged(playback, () => playback.ReplaceClip(source, bad));
        }
        playback.Reset(); Check(playback.Generation == 2 && !playback.Playing && playback.ActiveSeconds == 0 && !playback.IsCurrent(initial), "Stopped reset epoch");
        Check(playback.Advance(1).Count == 0, "Reset stops"); playback.Resume(); Check(playback.Advance(.01).Count == 1, "Reset rearms zero");
        playback.SetFocused(false); playback.Restart(); Check(playback.Generation == 3 && playback.Playing && !playback.Focused && playback.UnwrappedPhase == 0, "Restart retains focus");
        var replacement = Decode(Fixture(replacement: true)); playback.ReplaceClip(replacement, 20, looping: false);
        Check(playback.Generation == 4 && playback.FrameRate == 20 && !playback.Focused && playback.Advance(1).Count == 0, "Replacement retains focus");
        playback.SetFocused(true); var replacementBatch = playback.Advance(.4);
        Check(replacementBatch.Generation == 4 && replacementBatch.Count == 2 && replacementBatch.All(e => e.Name == "replacement") && playback.EndFrame == 8 && playback.UnwrappedPhase == 8 && playback.CurrentFrame == 8 && !playback.Playing, "Replacement identities and nonloop stop");
        double endedTime = playback.ActiveSeconds; playback.Resume(); Check(!playback.Playing && playback.Advance(60).Count == 0 && playback.ActiveSeconds == endedTime, "Terminal resume cannot restart");
        Check(initial[0].Name == "same" && atFive.Count == 3 && !playback.IsCurrent(boundary), "Old batch survives replacement");
        var overshoot = new AnimationEventPlayback(source, 10, looping: false);
        Check(overshoot.Advance(2).Count == 5 && overshoot.ActiveSeconds == 2 && !overshoot.Playing, "Nonloop accepts entire final tick but clamps phase");
        var eventBudget = new AnimationEventPlayback(source, 10, maximumEventsPerAdvance: 3);
        RejectUnchanged(eventBudget, () => eventBudget.Advance(.5));
        Check(eventBudget.Advance(.01).Count == 1 && eventBudget.Advance(.49).Count == 3 && eventBudget.UnwrappedPhase == 5, "Event budget retry");
        var cycleBudget = new AnimationEventPlayback(source, 10, maximumCyclesPerAdvance: 1);
        RejectUnchanged(cycleBudget, () => cycleBudget.Advance(2));
        Check(cycleBudget.Advance(1).Count == 6 && cycleBudget.Advance(1).Count == 5 && cycleBudget.CompletedLoops == 2, "Cycle budget subdivisions");
        var fractional = new AnimationEventPlayback(source, 10, maximumEventsPerAdvance: 3);
        fractional.Advance(.1); RejectUnchanged(fractional, () => fractional.Advance(1));
        var control = new AnimationEventPlayback(source, 10, maximumEventsPerAdvance: 3); control.Advance(.1);
        Check(fractional.Advance(.4).SequenceEqual(control.Advance(.4)) && State(fractional) == State(control), "Compensation preserved after rejection");
        var empty = new AnimationEventPlayback(Decode(Fixture(empty: true)), 30);
        Check(empty.Advance(.01).Count == 0 && empty.UnwrappedPhase > 0, "Empty clip still advances");
        Reject(() => new AnimationEventPlayback(source, 30, maximumCyclesPerAdvance: 0));
        int cases = 0;
        foreach (float fps in new[] {1f, 7.5f, 24f, 29.97f, 30f, 59.94f, 1000f})
            for (int quarters = 1; quarters <= 160; quarters++)
            {
                double target = quarters / 4.0, seconds = target / fps;
                var expected = Oracle(source, target);
                var whole = new AnimationEventPlayback(source, fps);
                Check(whole.Advance(seconds).SequenceEqual(expected), "Absolute seconds/FPS oracle");
                var split = new AnimationEventPlayback(source, fps); var collected = new List<AnimationEventOccurrence>();
                // Unequal partitions with accumulated fractional time; total equals target seconds to rounding tolerance.
                double used = 0;
                for (int part = 1; part <= 17; part++)
                {
                    double next = seconds * part * part / (17 * 17.0);
                    collected.AddRange(split.Advance(next - used)); used = next;
                    Check(split.Advance(0).Count == 0, "Zero partition");
                }
                Check(collected.SequenceEqual(expected) && Close(split.UnwrappedPhase, whole.UnwrappedPhase), "Unequal partition invariance");
                Check(collected.Select(e => (e.Cycle, e.GroupIndex, e.OccurrenceIndex)).Distinct().Count() == collected.Count, "Unique identities");
                cases++;
            }
        // A sub-snap advance must remain in raw accumulated time, not be dropped at each repeated tick.
        var tiny = new AnimationEventPlayback(source, 10); tiny.Advance(.5);
        for (int i = 0; i < 100; i++) tiny.Advance(1e-15);
        Check(tiny.ActiveSeconds > .5 && tiny.UnwrappedPhase > 5, "Snap retains fractional accumulation");
        Console.WriteLine($"PASS: event playback {cases} varied-FPS absolute/unequal partition cases; generation/owner/control/stop/snap and atomic budget retry");
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
            var entry = overlay.Lookup("appearance/animation/" + sample.Item1 + ".ans");
            byte[] bytes = entry.Payload ?? throw new InvalidDataException("Missing pinned clip.");
            Check(entry.Priority == 30 && Convert.ToHexString(SHA256.HashData(bytes)) == sample.Item2, "Original identity");
            var clip = CompressedKeyframeDecoder.Decode(bytes); var motion = CompressedLocomotion.Create(clip); Array.Clear(bytes);
            Check(clip.FrameRate == 30 && motion.EndFrame == sample.Item3 && motion.Messages.Count == 1 && motion.Messages[0].Name == "event_footstep" && motion.Messages[0].Frames.SequenceEqual(sample.Item4), "Original records");
            var actual = new List<AnimationEventOccurrence>(); var playback = new AnimationEventPlayback(motion, clip.FrameRate);
            for (int i = 0; i < motion.EndFrame * 12; i++) actual.AddRange(playback.Advance(1.0 / 120));
            Check(actual.SequenceEqual(Oracle(motion, motion.EndFrame * 3.0)) && actual.Count == 6 && playback.CompletedLoops == 3 && playback.CurrentFrame == 0, "Original fixed ticks");
            var single = new AnimationEventPlayback(motion, clip.FrameRate);
            Check(single.Advance(motion.EndFrame / 10.0).SequenceEqual(actual), "Original absolute interval");
            playback.Pause(); Check(playback.Advance(60).Count == 0, "Original paused"); playback.Resume(); playback.Restart();
            Check(playback.Advance(sample.Item4[0] / 30.0).Count == 1, "Original reset first footstep");
            Console.WriteLine($"PASS: pinned {sample.Item1}, authored FPS30, six occurrences/three loops at 120Hz and absolute-time oracle");
        }
    }
    private static AnimationEventOccurrence[] Oracle(CompressedLocomotion source, double target)
    {
        var events = new List<(double At, AnimationEventOccurrence Event)>();
        for (long c = 0; c <= (long)(target / source.EndFrame); c++)
            for (int g = 0; g < source.Messages.Count; g++)
                for (int o = 0; o < source.Messages[g].Frames.Count; o++)
                {
                    ushort f = source.Messages[g].Frames[o]; double at = c * source.EndFrame + f;
                    if (at <= target) events.Add((at, new(c,g,o,source.Messages[g].Name,f)));
                }
        return events.OrderBy(e => e.At).ThenBy(e => e.Event.Cycle).ThenBy(e => e.Event.GroupIndex).ThenBy(e => e.Event.OccurrenceIndex).Select(e => e.Event).ToArray();
    }
    private static (double,double,double,long,float,ushort,bool,bool,bool) State(AnimationEventPlayback p) =>
        (p.ActiveSeconds,p.UnwrappedPhase,p.CurrentFrame,p.Generation,p.FrameRate,p.EndFrame,p.Playing,p.Focused,p.Looping);
    private static void RejectUnchanged(AnimationEventPlayback p, Action a) { var before = State(p); Reject(a); Check(State(p) == before,"Atomic rejection"); }
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static bool Close(double a,double b) => Math.Abs(a-b) <= 1e-11 * Math.Max(1,Math.Abs(a));
    private static void Reject(Action a) { try { a(); } catch (InvalidDataException) { return; } throw new Exception("Expected rejection"); }
    private static CompressedLocomotion Decode(byte[] bytes) => CompressedLocomotion.Create(CompressedKeyframeDecoder.Decode(bytes));
    private static byte[] Fixture(bool empty = false,bool replacement = false)
    {
        byte[] Chunk(string tag,byte[] payload) { byte[] length = new byte[4]; BinaryPrimitives.WriteInt32BigEndian(length,payload.Length); return Encoding.ASCII.GetBytes(tag).Concat(length).Concat(payload).ToArray(); }
        byte[] Form(string tag,params byte[][] children) => Chunk("FORM",Encoding.ASCII.GetBytes(tag).Concat(children.SelectMany(c=>c)).ToArray());
        byte[] Group(string name,params ushort[] frames) => Chunk("MESG",BitConverter.GetBytes((ushort)frames.Length).Concat(Encoding.ASCII.GetBytes(name+"\0")).Concat(frames.SelectMany(BitConverter.GetBytes)).ToArray());
        byte[] info = BitConverter.GetBytes(30f).Concat(new short[] {(short)(replacement?8:10),0,0,0,0,0}.SelectMany(BitConverter.GetBytes)).ToArray();
        byte[] messages = empty ? Form("MSGS",Chunk("INFO",BitConverter.GetBytes((ushort)0))) : replacement ? Form("MSGS",Chunk("INFO",BitConverter.GetBytes((ushort)1)),Group("replacement",0,8)) : Form("MSGS",Chunk("INFO",BitConverter.GetBytes((ushort)2)),Group("same",0,5,5,10),Group("same",5));
        return Form("CKAT",Form("0001",Chunk("INFO",info),Form("XFRM"),messages));
    }
}
