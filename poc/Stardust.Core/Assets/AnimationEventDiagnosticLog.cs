namespace Stardust.Core.Assets;

public readonly record struct AnimationEventDiagnosticRecord(long Sequence, long Generation, AnimationEventOccurrence Occurrence);

/// <summary>Modern ordered diagnostic sink. A fixed ring and one frontier replace an unbounded delivered-ID set.</summary>
public sealed class AnimationEventDiagnosticLog
{
    public const int MaximumCapacity = 4096;
    private readonly AnimationEventPlayback playback;
    private readonly AnimationEventDiagnosticRecord[] ring;
    private AnimationEventOccurrence? frontier;
    private int next, count;
    public int Capacity => ring.Length;
    public int Count => count;
    public long Generation { get; private set; }
    public long DeliveredTotal { get; private set; }
    public long GenerationDeliveredTotal { get; private set; }
    public AnimationEventDiagnosticLog(AnimationEventPlayback playback, int capacity = 128)
    {
        ArgumentNullException.ThrowIfNull(playback);
        Require(capacity is > 0 and <= MaximumCapacity, "Invalid diagnostic capacity.");
        this.playback = playback; ring = new AnimationEventDiagnosticRecord[capacity]; Generation = playback.Generation;
    }

    /// <summary>Snapshot oldest to newest. Clearing display history never rearms delivered occurrences.</summary>
    public IReadOnlyList<AnimationEventDiagnosticRecord> Recent
    {
        get
        {
            var result = new AnimationEventDiagnosticRecord[count];
            for (int i = 0; i < count; i++) result[i] = ring[(next - count + ring.Length + i) % ring.Length];
            return Array.AsReadOnly(result);
        }
    }
    public void ClearRecent() { Array.Clear(ring); next = 0; count = 0; }

    /// <summary>Explicit restart/replace sync. Same-generation sync retains the frontier and both totals.</summary>
    public void SynchronizeGeneration()
    {
        if (Generation == playback.Generation) return;
        ClearRecent(); frontier = null; GenerationDeliveredTotal = 0; Generation = playback.Generation;
    }

    /// <summary>Rejects empty/stale/foreign/repeated/out-of-order batches completely before publication.</summary>
    public void Deliver(AnimationEventPlaybackBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        Require(playback.IsCurrent(batch) && batch.Count > 0, "Foreign/stale/empty diagnostic batch.");
        bool changed = Generation != batch.Generation;
        AnimationEventOccurrence? prior = changed ? null : frontier;
        foreach (var occurrence in batch)
        {
            Require(prior is null || Compare(prior.Value, occurrence) < 0, "Already-delivered/duplicate/out-of-order occurrence.");
            prior = occurrence;
        }
        Require(DeliveredTotal <= long.MaxValue - batch.Count && (changed || GenerationDeliveredTotal <= long.MaxValue - batch.Count),
            "Diagnostic total exhausted.");
        if (changed) SynchronizeGeneration();
        foreach (var occurrence in batch)
        {
            DeliveredTotal++; GenerationDeliveredTotal++;
            ring[next] = new(DeliveredTotal, Generation, occurrence); next = (next + 1) % ring.Length;
            count = Math.Min(count + 1, ring.Length);
        }
        frontier = prior;
    }
    private static int Compare(AnimationEventOccurrence a, AnimationEventOccurrence b)
    {
        int compare = a.Cycle.CompareTo(b.Cycle); if (compare != 0) return compare;
        compare = a.Frame.CompareTo(b.Frame); if (compare != 0) return compare;
        compare = a.GroupIndex.CompareTo(b.GroupIndex); return compare != 0 ? compare : a.OccurrenceIndex.CompareTo(b.OccurrenceIndex);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}
