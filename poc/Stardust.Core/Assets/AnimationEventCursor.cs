namespace Stardust.Core.Assets;

public readonly record struct AnimationEventOccurrence(long Cycle, int GroupIndex, int OccurrenceIndex, string Name, ushort Frame);

/// <summary>Modern, single-owner playback policy. Does not recover original scheduler semantics.</summary>
public sealed class AnimationEventCursor
{
    private readonly AnimationEventOccurrence[] occurrences;
    private readonly ushort endFrame;
    private readonly bool looping;
    private readonly int maximumCycles, maximumEvents;
    private bool initial = true;
    public double Phase { get; private set; }
    public double Timestamp { get; private set; }
    public bool IsPaused { get; private set; }
    public const double MaximumPhase = 1L << 40;

    public AnimationEventCursor(CompressedLocomotion source, bool looping = true,
        int maximumCyclesPerAdvance = 64, int maximumEventsPerAdvance = 4096)
    {
        ArgumentNullException.ThrowIfNull(source);
        Require(source.EndFrame > 0 && maximumCyclesPerAdvance is > 0 and <= 1024 &&
            maximumEventsPerAdvance is > 0 and <= 262144, "Invalid cursor limits.");
        endFrame = source.EndFrame; this.looping = looping;
        maximumCycles = maximumCyclesPerAdvance; maximumEvents = maximumEventsPerAdvance;
        occurrences = source.Messages.SelectMany((g, gi) => g.Frames.Select((f, oi) =>
            new AnimationEventOccurrence(0, gi, oi, g.Name, f))).OrderBy(e => e.Frame)
            .ThenBy(e => e.GroupIndex).ThenBy(e => e.OccurrenceIndex).ToArray();
    }

    internal AnimationEventCursor Fork() => (AnimationEventCursor)MemberwiseClone();
    internal void SeedPhase(double phase)
    {
        Require(double.IsFinite(phase) && phase >= 0 && phase <= MaximumPhase && (looping || phase <= endFrame), "Invalid event entry phase.");
        Phase = phase; Timestamp = 0; initial = false; IsPaused = false;
    }
    public void Pause() => IsPaused = true;
    public void Resume() => IsPaused = false;

    /// <summary>Starts a new playback epoch; prior returned batches remain owned and unchanged.</summary>
    public void Reset(double timestamp = 0)
    {
        Require(double.IsFinite(timestamp) && timestamp >= 0, "Invalid reset timestamp.");
        Timestamp = timestamp; Phase = 0; initial = true; IsPaused = false;
    }

    /// <summary>Absolute unwrapped frame phase and independent monotonic timestamp; no implicit FPS conversion.</summary>
    public IReadOnlyList<AnimationEventOccurrence> Advance(double timestamp, double phase)
    {
        Require(double.IsFinite(timestamp) && timestamp >= Timestamp && double.IsFinite(phase) &&
            phase >= Phase && phase <= MaximumPhase && (looping || phase <= endFrame), "Invalid time/phase advance.");
        Require(!IsPaused || phase == Phase, "Paused playback phase must remain unchanged.");
        Require(timestamp > Timestamp || phase == Phase, "Playback progress requires time progress.");
        if (phase == Phase) { Timestamp = timestamp; return Array.Empty<AnimationEventOccurrence>(); }
        long first = looping ? Math.Max(0, (long)Math.Floor(Phase / endFrame) - 1) : 0;
        long last = looping ? (long)Math.Floor(phase / endFrame) : 0;
        Require(!looping || Math.Floor(phase / endFrame) - Math.Floor(Phase / endFrame) <= maximumCycles,
            "Cycle advance budget exceeded.");
        int count = 0;
        // Binary bounds count the complete batch before any cursor mutation or output allocation.
        for (long cycle = first; cycle <= last; cycle++)
        {
            var (start, end) = Bounds(cycle, phase);
            count += end - start;
            Require(count <= maximumEvents, "Event advance budget exceeded.");
        }
        var result = new AnimationEventOccurrence[count]; int index = 0;
        for (long cycle = first; cycle <= last; cycle++)
        {
            var (start, end) = Bounds(cycle, phase);
            for (int i = start; i < end; i++) result[index++] = occurrences[i] with { Cycle = cycle };
        }
        Phase = phase; Timestamp = timestamp; initial = false;
        return Array.AsReadOnly(result);
    }

    private (int Start, int End) Bounds(long cycle, double target)
    {
        double offset = cycle * (double)endFrame;
        int Bound(double phase, bool inclusive)
        {
            int low = 0, high = occurrences.Length;
            while (low < high)
            {
                int middle = low + (high - low) / 2;
                double at = offset + occurrences[middle].Frame;
                if (at < phase || (!inclusive && at == phase)) low = middle + 1; else high = middle;
            }
            return low;
        }
        return (Bound(Phase, initial && cycle == 0), Bound(target, false));
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidDataException(message); }
}
