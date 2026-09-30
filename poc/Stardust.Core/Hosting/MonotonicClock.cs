using System.Diagnostics;

namespace Stardust.Core.Hosting;

public interface IMonotonicClock
{
    long ReadMilliseconds();
}

/// <summary>Elapsed process-local time; wall-clock adjustments do not change deadlines.</summary>
public sealed class StopwatchMonotonicClock : IMonotonicClock
{
    private readonly long origin = Stopwatch.GetTimestamp();
    public long ReadMilliseconds() => checked((long)Stopwatch.GetElapsedTime(origin).TotalMilliseconds);
}

/// <summary>Reject invalid injected-clock samples before changing host/protocol state.</summary>
public sealed class MonotonicClockGuard(IMonotonicClock clock)
{
    private readonly IMonotonicClock clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private long previous;

    public long ReadMilliseconds()
    {
        long sample = clock.ReadMilliseconds();
        if (sample < 0 || sample < previous)
            throw new InvalidOperationException("Monotonic clock moved backwards or returned a negative sample.");
        previous = sample;
        return sample;
    }
}
