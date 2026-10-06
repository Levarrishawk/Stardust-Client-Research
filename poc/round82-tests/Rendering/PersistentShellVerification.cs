using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

/// <summary>Admission/terminal-cleanup tests only; never claims actual resource/session run coverage.</summary>
public partial class PersistentShellVerification : Node
{
    private int checks;
    private void Check(bool good,string why){checks++;if(!good)throw new InvalidDataException(why);}
    public override async void _Ready(){try{await Run();GetTree().Quit();}catch(Exception error){GD.PushError(error.ToString());GetTree().Quit(1);}}
    private async Task Frames(){await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
    private async Task Run()
    {
        var args=OS.GetCmdlineUserArgs();string Arg(string name)=>args[Array.IndexOf(args,name)+1];string profile=Arg("--profile"),output=Arg("--out");
        string[] supplied={"--terrain-point-target-profile",profile};
        var rows=new List<object>();
        foreach(int budget in new[]{0,33}){var reject=new PersistentOfflinePlayground();bool threw=false;try{reject.Configure(_=>supplied,_=>new ScenePumpedWorldPreview(),budget);}catch(ArgumentOutOfRangeException){threw=true;}Check(threw,"bounded run admission");reject.Free();}
        foreach(string mode in new[]{"factory-throw","attached-factory","factory-reentry","published-reentry","invalid-startup","cleanup-throw"})
        {
            var shell=new PersistentOfflinePlayground();ScenePumpedWorldPreview? borrowed=null,candidate=null;int factories=0,events=0;
            if(mode=="attached-factory"){borrowed=new InertAdmissionPreview();AddChild(borrowed);}
            shell.Configure(_=>supplied,_=>{
                factories++;
                if(mode=="factory-throw")throw new IOException("candidate");
                if(mode=="attached-factory")return borrowed!;
                if(mode=="factory-reentry"){try{shell.RequestRestart();}catch(InvalidOperationException){}candidate=new InertAdmissionPreview();return candidate;}
                if(mode is "published-reentry" or "cleanup-throw"){var preview=new InertAdmissionPreview();preview.OnReady=()=>{if(mode=="cleanup-throw")preview.CorruptVerificationPeer();try{shell.RequestRestart();}catch(InvalidOperationException){}};candidate=preview;return preview;}
                candidate=new ScenePumpedWorldPreview();return candidate;
            },2);
            shell.RunCompleted+=(_,_)=>events++;AddChild(shell);await Frames();
            Check(shell.IsTerminal&&(mode=="cleanup-throw"?ReferenceEquals(shell.CurrentPreview,candidate):shell.CurrentPreview is null)&&factories==1&&!shell.RequestRestart(),"terminal admission/cleanup "+mode);
            bool borrowAlive=borrowed is not null&&GodotObject.IsInstanceValid(borrowed);
            bool candidateAlive=candidate is not null&&GodotObject.IsInstanceValid(candidate);
            Check(candidateAlive==(mode=="cleanup-throw")&&(! (mode=="attached-factory")||borrowAlive),"owned prefix freed and borrowed preserved "+mode);
            var report=shell.LastCompletedReport;
            Check(events==(mode is "published-reentry" or "invalid-startup" or "cleanup-throw"?1:0),"exactly one terminal cleanup report "+mode);
            if(report is not null)Check(!report.ValidOutcome&&!report.Restartable&&report.CleanupSucceeded==(mode!="cleanup-throw")&&report.Actors==0&&report.OutstandingArchiveJobs==0,"honest no-outcome terminal report");
            if(mode=="cleanup-throw")Check(report!.CleanupFailureTypes=="Peer cleanup:NullReferenceException"&&shell.RetiredPreviews==0,"cleanup catch terminal; child retained without false Free");
            rows.Add(new{Kind=mode,Started=shell.StartedRuns,Retired=shell.RetiredPreviews,Events=events,Factories=factories,Terminal=shell.IsTerminal,CandidateAlive=candidateAlive,BorrowAlive=borrowAlive,ValidOutcome=report?.ValidOutcome,Restartable=report?.Restartable,CleanupSucceeded=report?.CleanupSucceeded});
            shell.Free();if(borrowAlive)borrowed!.Free();
        }
        Check(GetChildCount()==0,"admission verification nodes cleared");
        File.WriteAllText(output,JsonSerializer.Serialize(new{Passed=true,Checks=checks,Rows=rows,Scope="Synthetic admission and terminal cleanup only; no terrain/assets/session run proof"}));GD.Print("PASS persistent shell admission");
    }
}
public partial class InertAdmissionPreview : ScenePumpedWorldPreview
{
    public Action? OnReady;
    public override void _Ready(){var callback=OnReady;OnReady=null;callback?.Invoke();}
}
public partial class ScenePumpedWorldPreview
{ public void CorruptVerificationPeer()=>peer=null!; }
