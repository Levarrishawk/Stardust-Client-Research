using System;
using System.Text.Json;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

public partial class ScenePumpedWorldPreview
{
    private SceneWorldCombinedMotionApplicationRouter? pointCombinedRouter;
    private SceneWorldFullTransformApplicationResult? pointQueuedApplied, pointQueuedDuplicate;
    private bool pointAdvanced, pointCorrectionQueued, pointCorrectionPending;
    private string? pointQueuedHex;
    private void PrepareCombinedMotionIngress()
    {
        if (!pointAdvanced) return;
        pointCombinedRouter = new(world!, motions!, (message, snapshot) => {
            Check(message.ParentId is null && message.DirectionRaw == 0 && message.SpeedRaw >= 0, "Explicit diagnostic world projection");
            return (AppearanceTransform.Identity with { Translation = message.NativePosition }, (float)message.SpeedRaw);
        }, SceneCreateZeroQuaternionPolicy.Identity, ObjectControllerTransformDirection.ServerToClient);
    }
    private bool AcceptHostMotion(long currentGeneration, ReadOnlyMemory<byte> application)
    {
        if (pointCombinedRouter is null) return router!.Accept(currentGeneration, application.Span);
        var result = pointCombinedRouter.Receive(currentGeneration, application.Span);
        if (pointCorrectionPending && result.FullResult is { } full) {
            if (full.Applied) { Check(pointQueuedApplied is null, "One queued correction applies"); pointQueuedApplied = full; }
            else { Check(pointQueuedDuplicate is null, "One queued duplicate declines"); pointQueuedDuplicate = full; }
        }
        return result.Accepted;
    }
    private void QueuePointCorrection()
    {
        if(pointCameraRelativeHosted){QueueSoePointCorrection();return;}
        var root = pointProfile!.RootElement;
        float x = PointFloat(root, "CorrectionXBits"), z = PointFloat(root, "CorrectionZBits");
        var ground = suppliedTerrainSetQuery!.Query(x, z) ?? throw new InvalidOperationException("Correction outside accepted terrain");
        var raw = ObjectControllerTransformMessage.ServerToClient(ObjectControllerTransformKind.Root, 0x1B, humanId, 0,
            root.GetProperty("CorrectionCounter").GetUInt32(), null, new System.Numerics.Vector4(0, 0, 0, 1), new(x, (float)ground.Hit.Height, z), 0);
        byte[] frame = ObjectControllerTransformCodec.Encode(raw);
        if (root.GetProperty("MalformedCorrection").GetBoolean()) Array.Resize(ref frame, frame.Length - 1);
        pointQueuedHex = Convert.ToHexString(frame); pointCorrectionPending = true;
        try {
            pump!.QueueApplication(generation, frame);
            if (!root.GetProperty("MalformedCorrection").GetBoolean()) pump.QueueApplication(generation, frame);
        } finally { Array.Clear(frame); }
        pointCorrectionQueued = true;
        GD.Print("point_correction_queued=" + JsonSerializer.Serialize(new { AtStep = pointIndex, Hex = pointQueuedHex,
            Messages = pump!.QueuedMessageCount, Bytes = pump.QueuedMessageBytes, CallerBytesCleared = true, Sent = false, OriginalMovementParity = false }));
    }
    private bool FrameQueuedPointCorrection(SceneWorldPresentationPumpStep receipt)
    {
        if (!pointCorrectionPending) return false;
        var applied = pointQueuedApplied ?? throw new InvalidOperationException("Queued correction not applied");
        var duplicate = pointQueuedDuplicate ?? throw new InvalidOperationException("Queued duplicate not declined");
        Check(applied.Applied && duplicate.Status == SceneWorldFullTransformApplicationStatus.CounterRejected
            && ReferenceEquals(applied.Motion, duplicate.Motion) && ReferenceEquals(applied.Motion, motions!.Find(applied.Snapshot!))
            && receipt.AcceptedMessages == 1 && receipt.DroppedMessages == 1 && pump!.QueuedMessageCount == 0 && pump.QueuedMessageBytes == 0,
            "Actual pump routes owned correction and duplicate before steering");
        var p = applied.Motion!.Transform.Translation;
        var engine = visuals[humanId].CreatureVisual!.Root.Position;
        Check(engine.DistanceTo(new Vector3(p.X, p.Y, -p.Z)) < 2e-5f, "Queued corrected presentation reflects once");
        GD.Print("point_queued_correction_ready=" + JsonSerializer.Serialize(new { AtStep = pointIndex, Hex = pointQueuedHex,
            Native = new[] { p.X, p.Y, p.Z }, Counter = applied.Raw.MovementCounter,
            Applied = true, DuplicateRejected = true, ActualMotion = true, receipt.AcceptedMessages, receipt.DroppedMessages,
            QueuedMessages = pump!.QueuedMessageCount, QueuedBytes = pump.QueuedMessageBytes, TimestampMicroseconds = localClock,
            DriverUpdates = pointDriver!.State.CompletedUpdates, Sent = false, OriginalMovementParity = false }));
        RebaseAcceptedLocomotion(applied.Motion!);RebaseCameraFollow(applied.Motion!);
        pointCorrectionPending = false;
        return true;
    }
    private void CloseCombinedMotionIngress() { pointCombinedRouter?.Dispose(); pointCombinedRouter = null; pointQueuedApplied = pointQueuedDuplicate = null; }
}
