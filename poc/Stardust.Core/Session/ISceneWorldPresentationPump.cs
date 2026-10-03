namespace Stardust.Core.Session;

/// <summary>Modern bounded scheduling policy, not recovered legacy timing.</summary>
public sealed record SceneWorldPresentationPumpPolicy(
    int MaximumQueuedMessages = 256,
    int MaximumQueuedBytes = 1048576,
    int MaximumMessageBytes = 65536,
    int MaximumMessagesPerStep = 16,
    int MaximumPendingPublications = 64,
    int MaximumPublicationsPerStep = 4,
    int MaximumPublicationAttempts = 4,
    int MaximumChangeAttemptsPerStep = 1,
    int MaximumActors = 64,
    long MaximumPendingResourceWeight = 67108864);

public sealed record SceneWorldPresentationPumpStep(
    int AcceptedMessages, int DroppedMessages, int PreparedResources,
    int PublishedResources, int RetiredResources, bool ChangeAcknowledged,
    int PendingMessages, int PendingPublications, bool Active);

/// <summary>Owner-thread pump. Consumer is borrowed; pump owns its close and world close.</summary>
public interface ISceneWorldPresentationPump : IDisposable
{
    SceneWorldObjectPresentationConsumer Consumer { get; }
    int QueuedMessageCount { get; }
    long QueuedMessageBytes { get; }
    int PendingPublicationCount { get; }
    long PendingResourceWeight { get; }
    Task DrainCompletion { get; }
    void QueueApplication(long generation, ReadOnlySpan<byte> message);
    SceneWorldPresentationPumpStep Step(double seconds);
    void Close();
}
