using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

/// <summary>Authored offline proof through the actual scenario launcher; no physical OS events.</summary>
public partial class OfflineScenarioPlaygroundVerification : Node
{
    private PersistentOfflinePlaygroundLauncher launcher = null!;
    private PersistentOfflinePlayground shell = null!;
    private bool closing, done;
    private int frames, completed;
    private readonly System.Diagnostics.Stopwatch diagnosticClock = new();
    private double readyMilliseconds;
    private readonly bool closeSignal = OS.GetEnvironment("STARDUST_ROUND82_CLOSE_SIGNAL") == "1";
    private static void Check(bool value, string message) { if (!value) throw new InvalidDataException(message); }
    public override void _Ready()
    {
        try
        {
            GD.Print("round82_run_begin=1");
            diagnosticClock.Restart();
            launcher = new(); AddChild(launcher);
            shell = launcher.Playground ?? throw new InvalidDataException("Actual scenario launcher required");
            shell.RunReady += run => {
                readyMilliseconds=diagnosticClock.Elapsed.TotalMilliseconds;
                GD.Print("round82_ready="+JsonSerializer.Serialize(new {Run=run, FreshPreviewId=shell.CurrentPreview!.GetInstanceId(),
                    ReadyMilliseconds=readyMilliseconds, DiagnosticWallClock=true, SimulationClockChanged=false}));
            };
            shell.RunCompleted += OnCompleted;
        }
        catch(Exception error) { Fail(error); }
    }
    public override void _Process(double delta)
    {
        if(done || shell is null) return;
        if(++frames > 4000 || shell.IsTerminal) { Fail(new InvalidDataException("Scenario proof timed out or terminated")); return; }
        if(closeSignal && !closing && shell.CurrentReady && shell.CurrentPreview!.OfflineRestartProbeCompletedUpdates>=30)
        {
            closing=true;
            GD.Print("round82_close_signal="+JsonSerializer.Serialize(new {SyntheticGodotWindowSignal=true, PhysicalOsEvent=false}));
            GetWindow().EmitSignal(Window.SignalName.CloseRequested);
            Check(shell.IsClosing,"Owned Window close signal routed synchronously");
            var closingStatus=shell.InspectionStatus;
            Check(closingStatus.Closing && closingStatus.CloseRequests==1 && closingStatus.WindowCloseConnected,
                "Exactly one owned close transition");
            shell.RequestClose();
            Check(shell.InspectionStatus.CloseRequests==1,"Duplicate close is idempotent");
        }
    }
    private void OnCompleted(int run, OfflinePreviewCompletionReport report)
    {
        try
        {
            Check(run==++completed && report.CleanupSucceeded && report.Actors==0 && report.OutstandingArchiveJobs==0 &&
                report.ArchiveCalls==3 && report.ArchiveClosed==3 && report.QueuedPublications==0 && report.PendingPublications==0 &&
                report.PendingMessages==0 && report.RetainedPackets==0 && shell.CurrentPreview is null,"Actual full cleanup before callback");
            GD.Print("round82_run_end="+JsonSerializer.Serialize(new {Run=run, Report=report}));
            GD.Print("round82_reload_cost="+JsonSerializer.Serialize(new {Run=run,ReadyMilliseconds=readyMilliseconds,
                TotalMilliseconds=diagnosticClock.Elapsed.TotalMilliseconds,DiagnosticWallClock=true,IncludesSimulationAndDrain=true}));
            var status=shell.InspectionStatus;
            Check(status.CompletedUpdates==report.CompletedUpdates && status.AttemptedUpdates==report.AttemptedUpdates &&
                status.CleanupSucceeded==true && status.Actors==0 && status.OutstandingArchiveJobs==0 && !status.ChildPresent,
                "Inspection uses actual primitive completion");
            GD.Print("round82_inspection="+JsonSerializer.Serialize(status));
            if(closeSignal)
            {
                Check(closing && shell.IsClosing && report.WindowClosing && report.Reason=="Incomplete" &&
                    report.CompletedUpdates is >=30 and <=37 && report.AttemptedUpdates==report.CompletedUpdates && !shell.RequestRestart(),
                    "Close signal preserves incomplete prefix and forbids successor");
                done=true;
                GD.Print("round82_close_summary="+JsonSerializer.Serialize(new {Passed=true, Completed=completed, NativeLauncher=true,
                    SyntheticGodotWindowSignal=true, PhysicalOsEvent=false, report.CompletedUpdates, NoSuccessor=true}));
                return;
            }
            Check(report.Reason=="BudgetCompleted" && report.CompletedUpdates==3 && report.AttemptedUpdates==3 && report.RootFrames==0,
                "Bounded idle scenario expectation authored before native");
            _=BetweenRuns(run);
        }
        catch(Exception error) { Fail(error); }
    }
    private async Task BetweenRuns(int run)
    {
        try
        {
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Check(shell.CurrentPreview is null && shell.LastCompletedReport is not null && shell.OutcomeCaption is not null,
                "Primitive outcome remains inspectable across frames");
            if(run==1)
            {
                GD.Print("round82_run_begin=2");
                diagnosticClock.Restart();
                Check(shell.RequestRestart() && !shell.RequestRestart(),"One explicit successor, duplicate rejected");
                return;
            }
            Check(run==2 && !shell.RequestRestart() && shell.StartedRuns==2 && shell.RetiredPreviews==2,"Finite scenario cap");
            var cap=shell.InspectionStatus;
            Check(cap.State=="RunLimit" && cap.MaximumRuns==2 && cap.RemainingRuns==0 && !cap.RestartEnabled && cap.CloseEnabled &&
                shell.RestartHud?.IsRestartEnabled==false && shell.InspectionCaption.Contains("Run limit reached"),
                "Visible cap explains disabled restart and available close");
            var capture=OS.GetEnvironment("STARDUST_ROUND82_CAPTURE");
            if(capture.Length>0 && DisplayServer.GetName()!="headless")
            {
                using var image=GetViewport().GetTexture().GetImage();
                Check(image.SavePng(capture)==Error.Ok,"Inspection screenshot");
            }
            GD.Print("round82_scenario_summary="+JsonSerializer.Serialize(new {Passed=true,Runs=2,CompletedUpdates=6,
                ProtectedRoots=0,FreshPreviewsRetired=2,NativeLauncher=true,NoSocket=true,PhysicalOsEvent=false}));
            done=true; shell.RequestClose();
        }
        catch(Exception error) { Fail(error); }
    }
    private void Fail(Exception error) { if(done)return; done=true; GD.PushError("Round82 scenario verification: "+error); GetTree().Quit(1); }
}
