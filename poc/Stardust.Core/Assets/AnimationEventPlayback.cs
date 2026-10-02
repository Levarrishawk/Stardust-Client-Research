using System.Collections;

namespace Stardust.Core.Assets;

/// <summary>Owned immutable occurrences, tagged with one playback owner and generation.</summary>
public sealed class AnimationEventPlaybackBatch : IReadOnlyList<AnimationEventOccurrence>
{
    private readonly IReadOnlyList<AnimationEventOccurrence> events;
    internal object Owner { get; }
    public long Generation { get; }
    internal AnimationEventPlaybackBatch(object owner, long generation, IReadOnlyList<AnimationEventOccurrence> events)
    { Owner = owner; Generation = generation; this.events = events; }
    public int Count => events.Count;
    public AnimationEventOccurrence this[int index] => events[index];
    public IEnumerator<AnimationEventOccurrence> GetEnumerator() => events.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>Modern elapsed-time bridge; no wall clock, original scheduler, or effect dispatch.</summary>
public sealed class AnimationEventPlayback
{
    private readonly object owner = new();
    private readonly int maximumCycles, maximumEvents;
    private AnimationEventCursor cursor;
    private double timeCompensation;
    public long Generation { get; private set; } = 1;
    public float FrameRate { get; private set; }
    public ushort EndFrame { get; private set; }
    public bool Looping { get; private set; }
    public bool Playing { get; private set; } = true;
    public bool Focused { get; private set; } = true;
    public double ActiveSeconds { get; private set; }
    public double UnwrappedPhase => cursor.Phase;
    public double CurrentFrame => Looping ? UnwrappedPhase % EndFrame : UnwrappedPhase;
    public long CompletedLoops => Looping ? (long)Math.Floor(UnwrappedPhase / EndFrame) : 0;

    public AnimationEventPlayback(CompressedLocomotion source, float frameRate, bool looping = true,
        int maximumCyclesPerAdvance = 64, int maximumEventsPerAdvance = 4096)
    {
        ValidateRate(frameRate);
        cursor = new(source, looping, maximumCyclesPerAdvance, maximumEventsPerAdvance);
        FrameRate = frameRate; EndFrame = source.EndFrame; Looping = looping;
        maximumCycles = maximumCyclesPerAdvance; maximumEvents = maximumEventsPerAdvance;
    }

    public void Pause() => Playing = false;
    public void Resume() => Playing = Looping || UnwrappedPhase < EndFrame;
    public void SetFocused(bool focused) => Focused = focused;
    public bool IsCurrent(AnimationEventPlaybackBatch batch)
    { ArgumentNullException.ThrowIfNull(batch); return ReferenceEquals(batch.Owner, owner) && batch.Generation == Generation; }

    public void Restart() => Reset(playing: true);
    public void Reset(bool playing = false)
    {
        long generation = NextGeneration();
        cursor.Reset(); ActiveSeconds = 0; timeCompensation = 0; Generation = generation; Playing = playing;
    }

    /// <summary>Starts the replacement clip at zero, playing, retaining the focus gate and configured budgets.</summary>
    public void ReplaceClip(CompressedLocomotion source, float frameRate, bool looping = true)
    {
        ValidateRate(frameRate); long generation = NextGeneration();
        var replacement = new AnimationEventCursor(source, looping, maximumCycles, maximumEvents);
        cursor = replacement; FrameRate = frameRate; EndFrame = source.EndFrame; Looping = looping;
        ActiveSeconds = 0; timeCompensation = 0; Generation = generation; Playing = true;
    }

    public AnimationEventPlaybackBatch Advance(double elapsedSeconds)
    {
        Require(double.IsFinite(elapsedSeconds) && elapsedSeconds >= 0 && elapsedSeconds <= 60,
            "Elapsed seconds outside bounded playback range.");
        if (!Playing || !Focused || elapsedSeconds == 0)
            return new(owner, Generation, Array.Empty<AnimationEventOccurrence>());
        // Compensate active seconds, then multiply once by FPS rather than accumulating rounded frame deltas.
        double delta = elapsedSeconds - timeCompensation;
        double time = ActiveSeconds + delta;
        double compensation = (time - ActiveSeconds) - delta;
        Require(double.IsFinite(time) && time > ActiveSeconds, "Elapsed time cannot make representable progress.");
        double phase = time * FrameRate;
        Require(double.IsFinite(phase) && phase <= AnimationEventCursor.MaximumPhase, "Playback phase ceiling exceeded.");
        // Explicit modern snap at authored integer frame positions; keep raw time/compensation for future ticks.
        double boundary = Math.Round(phase);
        double tolerance = 8 * 2.2204460492503131e-16 * Math.Max(EndFrame, Math.Abs(phase));
        if (boundary > 0 && Math.Abs(phase - boundary) <= tolerance) phase = boundary;
        phase = Math.Max(cursor.Phase, phase);
        if (!Looping) phase = Math.Min(EndFrame, phase);
        // Cursor validates and counts its entire batch transactionally. Commit time only after it accepts.
        var occurrences = cursor.Advance(time, phase);
        ActiveSeconds = time; timeCompensation = compensation;
        if (!Looping && phase == EndFrame) Playing = false;
        return new(owner, Generation, occurrences);
    }

    private long NextGeneration()
    { Require(Generation < long.MaxValue, "Playback generation exhausted."); return Generation + 1; }
    private static void ValidateRate(float fps)
    { Require(float.IsFinite(fps) && fps > 0 && fps <= 1000, "Invalid playback FPS."); }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidDataException(message); }
}
