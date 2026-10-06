using System;
using System.Text.Json;
using Godot;

// Explicit synthetic retirement stress, used only by the Round81 joined verifier.
public partial class ScenePumpedWorldPreview
{
    public int OfflineRestartProbeCompletedUpdates => pointDriver?.State.CompletedUpdates ?? 0;
    public void ProbeOfflineRestartRetirement()
    {
        if (!pointConfigured || !pointStarted || !offlineOutcomeRecorded || pointClosed ||
            pointDriver is null || offlineSessionController is null || manualEventHost is null ||
            runEventHost is null || cameraOrbitHost is null || offlineFrameClock is null)
            throw new InvalidOperationException("Retirement probe requires the completed, still owned configured preview");
        var before = pointDriver.State;
        int manual = manualEventHost.HandledCount, run = runEventHost.HandledCount;
        bool healthy = !offlineSessionController.State.IsFaulted;
        if (healthy)
        {
            PushManualKey(Key.W, true);
            PushRunKey(false);
            PushRunKey(true);
            Check(manualEventHost.HandledCount == manual + 1 && runEventHost.HandledCount == run + 2,
                "Retirement probe uses actual viewport movement keys");
        }
        var orbit = cameraOrbitHost;
        double yaw = orbit.Yaw, pitch = orbit.Pitch, distance = orbit.Distance;
        int handled = orbit.HandledCount;
        using (var motion = new InputEventMouseMotion { Relative = new Vector2(18, 7),
            ButtonMask = MouseButtonMask.Right, Position = new Vector2(800, 300) })
            GetViewport().PushInput(motion, true);
        using (var wheel = new InputEventMouseButton { ButtonIndex = MouseButton.WheelUp,
            Pressed = true, Position = new Vector2(800, 300) })
            GetViewport().PushInput(wheel, true);
        Check(orbit.HandledCount == handled + 2 && orbit.LastDiagnostic?.FromGodotEvent == true &&
            orbit.Yaw != yaw && orbit.Pitch != pitch && orbit.Distance != distance,
            "Retirement probe changes actual orbit through two viewport events");
        Check(pointDriver.State.CompletedUpdates == before.CompletedUpdates &&
            pointDriver.State.AttemptedUpdates == before.AttemptedUpdates &&
            ReferenceEquals(pointDriver.State.DriverResult, before.DriverResult),
            "Retirement probe never advances a completed or faulted simulation");
        GD.Print("offline_restart_retirement_probe=" + JsonSerializer.Serialize(new {
            ActualViewportEvents = true, HealthyMovementKeys = healthy,
            ManualHeld = manualEventHost.CurrentInput != System.Numerics.Vector2.Zero,
            RunHeld = runEventHost.IsRunning, orbit.Yaw, orbit.Pitch, orbit.Distance,
            CameraChanged = true, CompletedUpdates = before.CompletedUpdates,
            AttemptedUpdates = before.AttemptedUpdates, SimulationAdvanced = false,
            CompletionAlreadyFrozen = true, Synthetic = true
        }));
    }
}
