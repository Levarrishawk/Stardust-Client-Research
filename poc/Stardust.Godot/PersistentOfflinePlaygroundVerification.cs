using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

/// <summary>Explicit synthetic four-run verification of the original-resource playground.</summary>
public partial class PersistentOfflinePlaygroundVerification : Node
{
    private PersistentOfflinePlayground shell = null!;
    private readonly List<ScenePumpedWorldPreview> previews = new();
    private readonly List<ulong> readyIds = new();
    private string[][] arguments = null!;
    private string? capture;
    private bool rejectFactory, done;
    private int frames, completed, rejected, guiAccepted;
    private static void Check(bool value, string message) { if (!value) throw new InvalidDataException(message); }

    public override void _Ready()
    {
        try
        {
            var args = OS.GetCmdlineUserArgs();
            string Arg(string key) { int i = Array.IndexOf(args, key); if (i < 0 || i+1 >= args.Length) throw new ArgumentException(key); return args[i+1]; }
            arguments = JsonSerializer.Deserialize<string[][]>(File.ReadAllBytes(Arg("--round81-arguments")))!;
            Check(arguments.Length == 4 && arguments.All(a => a.Length <= 256), "Four bounded explicit argument sets");
            if (args.Contains("--round81-capture-prefix")) capture = Arg("--round81-capture-prefix");
            shell = new PersistentOfflinePlayground();
            shell.Configure(run => arguments[run-1], run => {
                if (rejectFactory) { rejectFactory = false; throw new IOException("Prescribed pre-admission factory rejection"); }
                GD.Print("round81_run_begin=" + run);
                var preview = new ScenePumpedWorldPreview(); previews.Add(preview); return preview;
            }, 4, preview => preview.ProbeOfflineRestartRetirement());
            shell.RunReady += run => {
                var preview = shell.CurrentPreview!;
                Check(shell.CurrentReady && GodotObject.IsInstanceValid(preview), "Fresh full preview readiness");
                readyIds.Add(preview.GetInstanceId());
                Check(shell.RestartHud is null, "Old outcome retired only when successor ready");
                GD.Print("round81_ready=" + JsonSerializer.Serialize(new { Run=run, PreviewId=preview.GetInstanceId(), FreshResources=true }));
                if (run == 1 && capture is not null) _ = CaptureActive();
            };
            shell.RunCompleted += (run, report) => {
                Check(run == ++completed && report.Restartable && report.CleanupSucceeded &&
                    report.Actors == 0 && report.QueuedPublications == 0 && report.PendingPublications == 0 &&
                    report.OutstandingArchiveJobs == 0 && report.ArchiveCalls == 3 && report.ArchiveClosed == 3,
                    "Actual full child drain before outcome and replacement");
                Check(!GodotObject.IsInstanceValid(previews[run-1]), "Completed child is freed before caller event");
                bool staleRejected = false;
                try { previews[run-1].ProbeOfflineRestartRetirement(); } catch (InvalidOperationException) { staleRejected = true; }
                Check(staleRejected, "Retired preview cannot issue new input");
                GD.Print("round81_run_end=" + JsonSerializer.Serialize(new { Run=run, Report=report, StalePreviewRejected=true }));
                _ = BetweenRuns(run, report);
            };
            AddChild(shell);
        }
        catch (Exception error) { Fail(error); }
    }

    private async Task CaptureActive()
    {
        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (!done && capture is not null && DisplayServer.GetName() != "headless")
            {
                using var image = GetViewport().GetTexture().GetImage();
                Check(image.SavePng(capture + ".active.png") == Error.Ok, "Full running preview GPU image");
            }
        }
        catch (Exception error) { Fail(error); }
    }

    private async Task BetweenRuns(int run, OfflinePreviewCompletionReport report)
    {
        try
        {
            var hud = shell.RestartHud!; string caption = shell.OutcomeCaption!;
            Check(caption.Contains(run == 3 ? "Stopped after an error" : "Finished with Escape"), "Actual persistent outcome caption");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(shell.CurrentPreview is null && ReferenceEquals(hud, shell.RestartHud) && shell.OutcomeCaption == caption,
                "Outcome persists with no active preview for two frames");
            if (run == 3 && capture is not null && DisplayServer.GetName() != "headless")
            {
                using var image = GetViewport().GetTexture().GetImage();
                Check(image.SavePng(capture + ".fault.png") == Error.Ok, "Persistent fault outcome GPU image");
            }
            if (run == 4)
            {
                Check(readyIds.Count == 4 && readyIds.Distinct().Count() == 4 && previews.All(p => !GodotObject.IsInstanceValid(p)),
                    "Four actual fresh preview instances retired");
                Check(!shell.RequestRestart(), "Successful run cap declines without a fifth factory");
                GD.Print("round81_restart_summary=" + JsonSerializer.Serialize(new { Passed=true, Runs=4,
                    RetiredPreviews=shell.RetiredPreviews, ActualGuiAccepted=guiAccepted, FactoryRejected=rejected,
                    PersistentOutcomes=4, FreshReadyInstances=readyIds.Count, AllChildrenFreed=true,
                    StalePreviewRejected=true, SocketCreated=false, OriginalParity=false }));
                done = true; shell.RequestClose(); return;
            }
            if (run == 1)
            {
                rejectFactory = true; int count = hud.RequestCount;
                PushRestartGesture();
                Check(hud.RequestCount == count+1 && hud.AcceptedRestartCount == 0 && shell.CurrentPreview is null &&
                    shell.StartedRuns == 1 && shell.OutcomeCaption == caption && shell.LastFactoryFailureType == typeof(IOException).FullName,
                    "Actual GUI rejected factory preserves previous outcome");
                rejected++;
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            int previous = hud.AcceptedRestartCount;
            PushRestartGesture();
            Check(hud.AcceptedRestartCount == previous+1 && shell.StartedRuns == run+1 && shell.CurrentPreview is not null,
                "Actual GUI admission starts a fresh full child");
            guiAccepted++;
            Check(shell.OutcomeCaption == caption && ReferenceEquals(shell.RestartHud, hud),
                "Old outcome remains until successor resource readiness");
            Check(!shell.RequestRestart(), "Duplicate request cannot replace an active successor");
        }
        catch (Exception error) { Fail(error); }
    }

    private void PushRestartGesture()
    {
        var viewport = GetViewport(); viewport.NotifyMouseEntered();
        Vector2 point = new(120, 504);
        using (var motion = new InputEventMouseMotion { Position=point, GlobalPosition=point, Relative=Vector2.One }) viewport.PushInput(motion, true);
        using (var press = new InputEventMouseButton { Position=point, GlobalPosition=point, ButtonIndex=MouseButton.Left,
            ButtonMask=MouseButtonMask.Left, Pressed=true }) viewport.PushInput(press, true);
        using (var release = new InputEventMouseButton { Position=point, GlobalPosition=point, ButtonIndex=MouseButton.Left, Pressed=false }) viewport.PushInput(release, true);
    }
    public override void _Process(double delta)
    {
        if (done) return;
        if (++frames > 15000 || shell is not null && shell.IsTerminal)
            Fail(new InvalidOperationException("Persistent preview terminated or timed out: " + shell?.LastFailureType));
    }
    private void Fail(Exception error)
    {
        if (done) return; done = true; GD.PushError("Round81 joined verification: " + error); GetTree().Quit(1);
    }
}
