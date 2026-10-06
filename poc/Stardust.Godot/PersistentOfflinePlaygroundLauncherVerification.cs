using System;
using System.IO;
using System.Text.Json;
using Godot;

/// <summary>Explicit synthetic controlled-close proof through the actual interactive launcher.</summary>
public partial class PersistentOfflinePlaygroundLauncherVerification : Node
{
    private PersistentOfflinePlaygroundLauncher launcher = null!;
    private bool closing;
    private int frames;
    public override void _Ready()
    {
        launcher = new PersistentOfflinePlaygroundLauncher(); AddChild(launcher);
        var shell = launcher.Playground ?? throw new InvalidDataException("Launcher created actual shell");
        shell.RunCompleted += (run, report) => {
            if (run != 1 || !closing || !shell.IsClosing || report.Reason != "Incomplete" || !report.WindowClosing ||
                !report.CleanupSucceeded || report.CompletedUpdates is <30 or >37 || report.AttemptedUpdates != report.CompletedUpdates ||
                report.Actors != 0 || report.OutstandingArchiveJobs != 0 || report.ArchiveCalls != 3 || report.ArchiveClosed != 3 ||
                report.QueuedPublications != 0 || report.PendingPublications != 0 || report.PendingMessages != 0 || report.RetainedPackets != 0 ||
                shell.CurrentPreview is not null || shell.RequestRestart())
                throw new InvalidDataException("Active close preserves incomplete prefix and drains without a successor");
            GD.Print("round81_active_close_summary=" + JsonSerializer.Serialize(new { Passed=true, Report=report,
                ActualInteractiveLauncher=true, SuccessNotInvented=true, NoSuccessor=true, AllActorsAndJobsDrained=true }));
        };
    }
    public override void _Process(double delta)
    {
        var shell = launcher?.Playground;
        if (shell is null) return;
        if (++frames > 3000 || shell.IsTerminal) { GD.PushError("Active close verification timed out or terminated"); GetTree().Quit(1); return; }
        if (!closing && shell.CurrentReady && shell.CurrentPreview!.OfflineRestartProbeCompletedUpdates >=30)
        {
            closing = true; shell.RequestClose();
        }
    }
}
