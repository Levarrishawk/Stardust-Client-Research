using System.Collections;
using System.Numerics;

namespace Stardust.Core.Assets;

public sealed class LocomotionEventPreviewBatch : IReadOnlyList<AnimationEventOccurrence>
{
    private readonly IReadOnlyList<AnimationEventOccurrence> occurrences;
    internal object Owner { get; }
    public long Generation { get; }
    public LocomotionPreviewState State { get; }
    public double UnwrappedPhase { get; }
    public bool Advanced { get; }
    internal LocomotionEventPreviewBatch(object owner, long generation, LocomotionPreviewState state,
        double phase, bool advanced, IReadOnlyList<AnimationEventOccurrence> occurrences)
    { Owner=owner; Generation=generation; State=state; UnwrappedPhase=phase; Advanced=advanced; this.occurrences=occurrences; }
    public int Count => occurrences.Count;
    public AnimationEventOccurrence this[int index] => occurrences[index];
    public IEnumerator<AnimationEventOccurrence> GetEnumerator() => occurrences.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>Modern accepted-distance events; preflight existing locomotion policy and cursor before committing either.</summary>
public sealed class LocomotionEventPreviewClock
{
    private readonly object owner = new();
    private readonly PreviewLocomotionClip[] clips;
    private readonly AnimationEventCursor[] templates;
    private LocomotionPreviewClock locomotion;
    private AnimationEventCursor cursor;
    private long cycles;
    public long Generation { get; private set; } = 1;
    public LocomotionPreviewState State => locomotion.State;
    public double UnwrappedPhase => cursor.Phase;
    public double GenerationSeconds => cursor.Timestamp;
    public long CompletedCycles => cycles;

    public LocomotionEventPreviewClock(PreviewLocomotionClip idle, PreviewLocomotionClip walk, PreviewLocomotionClip run,
        float runThreshold, CompressedLocomotion idleMessages, CompressedLocomotion walkMessages, CompressedLocomotion runMessages,
        int maximumCyclesPerTick = 64, int maximumEventsPerTick = 4096)
    {
        locomotion = new(idle, walk, run, runThreshold);
        clips = new[] { idle, walk, run };
        var sources = new[] { idleMessages, walkMessages, runMessages };
        templates = new AnimationEventCursor[3];
        for (int i = 0; i < 3; i++)
        {
            ArgumentNullException.ThrowIfNull(sources[i]);
            Require(sources[i].EndFrame == clips[i].EndFrame, "Locomotion event duration mismatch.");
            templates[i] = new(sources[i], true, maximumCyclesPerTick, maximumEventsPerTick);
        }
        cursor = templates[0].Fork();
    }

    public bool IsCurrent(LocomotionEventPreviewBatch batch)
    { ArgumentNullException.ThrowIfNull(batch); return ReferenceEquals(batch.Owner, owner) && batch.Generation == Generation; }

    public void Reset(Vector3 heading)
    {
        var candidate = locomotion.Fork(); candidate.Reset(heading);
        long generation = NextGeneration();
        var nextCursor = templates[0].Fork();
        locomotion = candidate; cursor = nextCursor; cycles = 0; Generation = generation;
    }

    public LocomotionEventPreviewBatch Tick(double seconds, Vector3 acceptedWorldDelta, bool active = true)
    {
        var candidate = locomotion.Fork();
        bool advanced = candidate.Advance(seconds, acceptedWorldDelta, active);
        if (!advanced) return new(owner, Generation, State, UnwrappedPhase, false, Array.Empty<AnimationEventOccurrence>());
        bool changed = candidate.State.Mode != State.Mode;
        long generation = changed ? NextGeneration() : Generation;
        int index = (int)candidate.State.Mode;
        var nextCursor = changed ? templates[index].Fork() : cursor.Fork();
        if (changed) nextCursor.SeedPhase(State.NormalizedPhase * clips[index].EndFrame);
        double crossed = Math.Floor(State.NormalizedPhase + candidate.LastPhaseIncrement);
        double nextCycles = (changed ? 0 : cycles) + crossed;
        double phase = nextCycles * clips[index].EndFrame + candidate.State.Frame;
        Require(double.IsFinite(phase) && phase <= AnimationEventCursor.MaximumPhase &&
            nextCycles >= 0 && nextCycles <= long.MaxValue && Math.Floor(phase / clips[index].EndFrame) == nextCycles,
            "Unrepresentable locomotion event phase/cycle.");
        double timestamp = nextCursor.Timestamp + seconds;
        Require(double.IsFinite(timestamp), "Invalid locomotion event time.");
        var events = nextCursor.Advance(timestamp, phase);
        var result = new LocomotionEventPreviewBatch(owner, generation, candidate.State, phase, true, events);
        locomotion = candidate; cursor = nextCursor; cycles = (long)nextCycles; Generation = generation;
        return result;
    }

    private long NextGeneration()
    { Require(Generation < long.MaxValue, "Locomotion generation exhausted."); return Generation + 1; }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidDataException(message); }
}
