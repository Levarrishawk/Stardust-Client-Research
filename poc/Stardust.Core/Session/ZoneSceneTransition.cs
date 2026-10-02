using Stardust.Core.Protocol;
namespace Stardust.Core.Session;

public enum ZoneSceneTransitionState { Idle, Selecting, Initializing, AwaitingServerReady, Ready, Failed, Closed }

/// <summary>Owned one-shot publication. Every access returns defensive byte copies.</summary>
public sealed class ZoneScenePublication
{
    private readonly byte[] ready;
    private readonly byte[][] payloads;
    internal ZoneScenePublication(long generation, byte[] ready, byte[][] payloads)
    {
        Generation = generation;
        this.ready = ready;
        this.payloads = payloads;
    }
    public long Generation { get; }
    public byte[] CopyReadyMessage() => (byte[])ready.Clone();
    public IReadOnlyList<byte[]> CopyStagedPayloads() => Array.AsReadOnly(payloads.Select(p => (byte[])p.Clone()).ToArray());
}

/// <summary>Single-owner offline coordinator. Call from one serialized owner; never invokes payload consumers.</summary>
public sealed class ZoneSceneTransition
{
    private readonly int maximumCount;
    private readonly int maximumBytes;
    private readonly List<byte[]> staged = new();
    public ZoneSceneTransition(int maximumCount, int maximumBytes)
    {
        if (maximumCount < 0) throw new ArgumentOutOfRangeException(nameof(maximumCount));
        if (maximumBytes < 0) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        this.maximumCount = maximumCount;
        this.maximumBytes = maximumBytes;
    }
    public ZoneSceneTransitionState State { get; private set; }
    public long Generation { get; private set; }
    public ulong? SelectedObjectId { get; private set; }
    public CmdStartSceneMessage? Scene { get; private set; }
    public int StagedCount => staged.Count;
    public int StagedBytes { get; private set; }

    /// <summary>Explicit replacement cancels prior coordinator state, but cannot recall already transferred publications.</summary>
    public long BeginSelection(ulong objectId)
    {
        EnsureOpen();
        AdvanceGeneration();
        Clear();
        SelectedObjectId = objectId;
        State = ZoneSceneTransitionState.Selecting;
        return Generation;
    }
    public bool AcceptStart(long generation, CmdStartSceneMessage scene)
    {
        if (generation != Generation) return false;
        RequireState(ZoneSceneTransitionState.Selecting);
        ArgumentNullException.ThrowIfNull(scene);
        if (scene.ObjectId != SelectedObjectId) throw new InvalidDataException("Scene object differs from selected character.");
        // DTOs are public: preserve codec invariants even when manually constructed.
        if (!ValidAscii16(scene.Terrain) || !ValidAscii16(scene.Template) || !float.IsFinite(scene.X) || !float.IsFinite(scene.Z) || !float.IsFinite(scene.Y))
            throw new InvalidDataException("Invalid scene DTO.");
        Scene = scene;
        State = ZoneSceneTransitionState.Initializing;
        return true;
    }
    public bool Stage(long generation, ReadOnlySpan<byte> applicationPayload)
    {
        if (generation != Generation) return false;
        RequireState(ZoneSceneTransitionState.Initializing);
        if (staged.Count >= maximumCount || applicationPayload.Length > maximumBytes - StagedBytes)
            throw new InvalidDataException("Scene staging budget exceeded.");
        byte[] owned = applicationPayload.ToArray();
        staged.Add(owned);
        StagedBytes += owned.Length;
        return true;
    }
    public ZoneScenePublication? CompleteMinimumResources(long generation)
    {
        if (generation != Generation || State != ZoneSceneTransitionState.Initializing) return null;
        // Allocate the complete result before mutating state, so failed allocation cannot publish partially.
        ZoneScenePublication publication = new(Generation, SceneReadyProtocolCodec.Encode(), staged.ToArray());
        staged.Clear();
        StagedBytes = 0;
        State = ZoneSceneTransitionState.AwaitingServerReady;
        return publication;
    }
    public bool AcceptServerReady(long generation, ReadOnlySpan<byte> message)
    {
        if (generation != Generation) return false;
        SceneReadyProtocolCodec.Decode(message);
        if (State == ZoneSceneTransitionState.Ready) return false;
        RequireState(ZoneSceneTransitionState.AwaitingServerReady);
        State = ZoneSceneTransitionState.Ready;
        return true;
    }
    public bool Fail(long generation)
    {
        if (generation != Generation || State is ZoneSceneTransitionState.Idle or ZoneSceneTransitionState.Failed or ZoneSceneTransitionState.Closed)
            return false;
        Clear();
        State = ZoneSceneTransitionState.Failed;
        return true;
    }
    public void Reset()
    {
        EnsureOpen();
        AdvanceGeneration();
        Clear();
        State = ZoneSceneTransitionState.Idle;
    }
    public void Close()
    {
        if (State == ZoneSceneTransitionState.Closed) return;
        AdvanceGeneration();
        Clear();
        State = ZoneSceneTransitionState.Closed;
    }
    private static bool ValidAscii16(string? value) => value is not null && value.Length <= ushort.MaxValue && !value.Any(character => character > 0x7F);
    private void AdvanceGeneration() => Generation = checked(Generation + 1);
    private void Clear() { staged.Clear(); StagedBytes = 0; Scene = null; SelectedObjectId = null; }
    private void EnsureOpen()
    {
        if (State == ZoneSceneTransitionState.Closed) throw new InvalidOperationException("Scene coordinator is closed.");
    }
    private void RequireState(ZoneSceneTransitionState expected)
    {
        if (State != expected) throw new InvalidOperationException($"Expected {expected}, found {State}.");
    }
}

