using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Session;

public partial class SceneWorldFixedStepHostVerification : Node
{
    private int checks;
    private void Check(bool value,string why){checks++;if(!value)throw new InvalidDataException(why);}
    private void Reject(Action action){bool rejected=false;try{action();}catch(Exception e) when(e is InvalidOperationException or ArgumentException){rejected=true;}Check(rejected,"Expected configuration rejection");}
    public override async void _Ready(){try{await Run();GetTree().Quit();}catch(Exception error){GD.PushError("Fixed host: "+error);GetTree().Quit(1);}}
    private SceneWorldPresentationHost Host(Node? parent=null){var host=new SceneWorldPresentationHost();(parent??this).AddChild(host);return host;}
    private async Task End(SceneWorldPresentationHost host,CountingPump pump,bool failed)
    {
        Check(await Task.WhenAny(host.Completion,Task.Delay(5000))==host.Completion,"bounded completion");bool failure=false;try{await host.Completion;}catch{failure=true;}
        Check(failure==failed&&(host.Failure is not null)==failed&&pump.Closes==1&&pump.Disposals==1,"expected completion and one owned close/disposal");
        if(GodotObject.IsInstanceValid(host))host.Free();
    }
    private async Task Run()
    {
        var args=OS.GetCmdlineUserArgs();string Arg(string key){int i=Array.IndexOf(args,key);if(i<0)throw new ArgumentException(key);return args[i+1];}
        string output=Arg("--out");using var doc=JsonDocument.Parse(File.ReadAllText(Arg("--fixtures")));
        var trace=new List<string>();var pump=new CountingPump(trace);var host=Host();int countIndex=0,completions=0;
        var counts=doc.RootElement.GetProperty("Counts").EnumerateArray().Select(x=>x.GetInt32()).ToArray();
        var deltas=doc.RootElement.GetProperty("Deltas").EnumerateArray().Select(x=>x.GetDouble()).ToArray();
        host.FrameCompleted+=_=>trace.Add("after:"+(++completions));
        host.Configure(pump,s=>trace.Add("before:"+s.ToString("F6",System.Globalization.CultureInfo.InvariantCulture)),
            d=>{trace.Add("schedule:"+countIndex);return counts[countIndex++];},.01,()=>{trace.Add("continue");return true;});host.SetProcess(false);
        var rows=new List<object>();
        for(int i=0;i<counts.Length;i++){host._Process(deltas[i]);rows.Add(new{Index=i,Completed=host.CompletedFrames,StepCalls=pump.Steps,ActualPumpCalls=pump.ActualCalls,LastSeconds=host.LastFrame?.Seconds});}
        Check(!host.IsClosing&&host.CompletedFrames==11&&pump.ActualCalls==11&&host.LastFrame!.Index==11,"separate eleven actual pump steps");
        _=host.CloseAsync();await End(host,pump,false);
        var stopTrace=new List<string>();pump=new(stopTrace);host=Host();int gates=0,stops=0;
        host.FrameCompleted+=_=>stops++;host.Configure(pump,_=>{},_=>8,.01,()=>++gates<=2);host.SetProcess(false);host._Process(.2);
        Check(stops==2&&gates==3&&pump.ActualCalls==2&&host.CompletedFrames==2&&!host.IsClosing,"endpoint stops reserved slots without closing/refund");_=host.CloseAsync();await End(host,pump,false);
        foreach(double seconds in new[]{.001,.1}){pump=new(new());host=Host();host.Configure(pump,null,fixedStepSchedule:_=>1,fixedStepSeconds:seconds);host.SetProcess(false);host._Process(.5);Check(host.LastFrame?.Seconds==seconds&&pump.ActualCalls==1,"fixed interval endpoints");_=host.CloseAsync();await End(host,pump,false);}
        pump=new(new());host=Host();Action<ISceneWorldPresentationPump,Action<double>?> legacyConfigure=host.Configure;legacyConfigure(pump,null);host.SetProcess(false);host._Process(.037);
        Check(host.LastFrame?.Seconds==.037&&host.CompletedFrames==1&&pump.ActualCalls==1,"exact legacy Configure delegate and original variable delta");_=host.CloseAsync();await End(host,pump,false);
        var failures=new List<object>();
        foreach(string kind in new[]{"bad-count-negative","bad-count-large","bad-delta","schedule-throw","schedule-reentry","continue-reentry","before-reentry","observer-reentry","before-mutation","schedule-close","continue-close","before-close","pump-close","observer-close","before-throw","pump-throw","observer-throw","parent-change","node-exit","off-thread","second-before-throw","second-observer-cancel","null-configure-reentry","node-free","pump-reentry"})
        {
            var parent=new Node();AddChild(parent);var other=new Node();AddChild(other);host=Host(parent);pump=new(new());int scheduleCalls=0,before=0,after=0;
            void Nested(){try{host._Process(.01);}catch(InvalidOperationException){}}
            int Schedule(double delta){scheduleCalls++;if(kind=="schedule-throw")throw new IOException("schedule");if(kind=="schedule-reentry")Nested();if(kind=="null-configure-reentry"){try{host.Configure(null!);}catch(ArgumentNullException){}}if(kind=="schedule-close")_=host.CloseAsync();if(kind=="parent-change")host.Reparent(other);if(kind=="node-exit")parent.RemoveChild(host);if(kind=="node-free")host.Free();return kind=="bad-count-negative"?-1:kind=="bad-count-large"?9:8;}
            bool Continue(){if(kind=="continue-reentry")Nested();if(kind=="continue-close")_=host.CloseAsync();return true;}
            void Before(double s){before++;if(kind=="before-reentry")Nested();if(kind=="before-mutation"){try{host.BeforeStep=null;}catch(InvalidOperationException){}}if(kind=="before-close")_=host.CloseAsync();if(kind=="before-throw"||kind=="second-before-throw"&&before==2)throw new IOException("before");}
            host.FrameCompleted+=step=>{after++;if(kind=="observer-reentry")Nested();if(kind=="observer-close")_=host.CloseAsync();if(kind=="observer-throw")throw new IOException("observer");if(kind=="second-observer-cancel"&&after==2)throw new OperationCanceledException("observer cancellation");};
            host.Configure(pump,Before,Schedule,.01,Continue);host.SetProcess(false);
            if(kind=="pump-close")pump.Hook=()=>{_=host.CloseAsync();};
            if(kind=="pump-throw")pump.Hook=()=>throw new IOException("pump wrapper prefix");
            if(kind=="pump-reentry")pump.Hook=Nested;
            if(kind=="off-thread")
            {
                Check(await Task.Run(()=>{try{host._Process(.1);return false;}catch(InvalidOperationException){return true;}}),"off-thread no source/pump");
                Check(scheduleCalls==0&&pump.Steps==0&&!host.IsClosing,"off-thread preserves host");_=host.CloseAsync();
            }
            else host._Process(kind=="bad-delta"?double.NaN:.1);
            bool failed=kind is not ("schedule-close" or "continue-close" or "before-close" or "pump-close" or "observer-close" or "parent-change" or "node-exit" or "node-free" or "off-thread");
            failures.Add(new{Kind=kind,ScheduleCalls=scheduleCalls,Before=before,After=after,StepCalls=pump.Steps,ActualPumpCalls=pump.ActualCalls,Completed=host.CompletedFrames,Failed=failed});
            await End(host,pump,failed);parent.Free();other.Free();
        }
        foreach(string kind in new[]{"no-schedule-seconds","no-schedule-continue","seconds-nan","seconds-low","seconds-high","getter-reentry"})
        {
            host=Host();pump=new(new());double s=kind=="seconds-nan"?double.NaN:kind=="seconds-low"?.0009:kind=="seconds-high"?.1001:.02;
            if(kind=="getter-reentry")pump.DrainHook=()=>{try{host.Configure(pump,null,fixedStepSchedule:_=>1);}catch(InvalidOperationException){}};
            Reject(()=>host.Configure(pump,null,fixedStepSchedule:kind.StartsWith("no-schedule")?null:_=>1,fixedStepSeconds:kind=="no-schedule-continue"||kind=="getter-reentry"?.01:s,continueFixedSteps:kind=="no-schedule-continue"?()=>true:null));
            Check(pump.Closes==0&&pump.Disposals==0&&!host.IsClosing,"configure rejection preserves pump ownership");pump.DrainHook=null;pump.Close();pump.Dispose();_=host.CloseAsync();await host.Completion;host.Free();
        }
        bool gpu=DisplayServer.GetName()!="headless";
        if(gpu){var label=new Label{Text="Fixed-step preview\n11 separately completed steps\n0 / 1 / 2 / 8 scheduled slots",Position=new(20,20)};AddChild(label);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=GetViewport().GetTexture().GetImage();Check(image.SavePng(Path.ChangeExtension(output,"png"))==Error.Ok,"private replacement capture");label.Free();}
        Check(GetChildCount()==0,"all verification nodes released");File.WriteAllText(output,JsonSerializer.Serialize(new{Passed=true,Checks=checks,Gpu=gpu,Rows=rows,Trace=trace,EndpointSteps=stops,EndpointGateCalls=gates,Failures=failures,ConfigureRejections=6,ActualPump=true}));GD.Print("PASS fixed-step presentation host");
    }
    private sealed class CountingPump : ISceneWorldPresentationPump
    {
        private readonly SceneWorldPresentationPump inner;
        private readonly List<string> trace;
        public int Steps,ActualCalls,Closes,Disposals;
        public Action? Hook,DrainHook;
        public CountingPump(List<string> trace)
        {
            this.trace=trace;var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(ProjectSettings.GlobalizePath("res://../preview-profiles/scene-object-catalog.json")));
            catalog=new(catalog.Entries.Values.Where(e=>e.Recipe==SceneObjectResourceRecipe.CreatureDetail0));
            using var file=File.OpenRead(ProjectSettings.GlobalizePath("res://../preview-profiles/human-locomotion.json"));var bank=SkeletalAnimationBankProfile.Load(file);
            var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
            var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{[SceneObjectResourceRecipe.CreatureDetail0]=new SceneCreatureResourceOptions(idle,bank,false)};
            var world=new SceneWorldCoordinator(catalog,options,(_,_)=>throw new InvalidOperationException("No asset loader"),SceneCreateZeroQuaternionPolicy.Reject);
            inner=new(world,(_,_)=>throw new InvalidOperationException("No visual factory"));
        }
        public SceneWorldObjectPresentationConsumer Consumer=>inner.Consumer;
        public int QueuedMessageCount=>inner.QueuedMessageCount;
        public long QueuedMessageBytes=>inner.QueuedMessageBytes;
        public int PendingPublicationCount=>inner.PendingPublicationCount;
        public long PendingResourceWeight=>inner.PendingResourceWeight;
        public Task DrainCompletion {get{DrainHook?.Invoke();return inner.DrainCompletion;}}
        public void QueueApplication(long generation,ReadOnlySpan<byte> message)=>inner.QueueApplication(generation,message);
        public SceneWorldPresentationPumpStep Step(double seconds){Steps++;trace.Add("pump:"+seconds.ToString("F6",System.Globalization.CultureInfo.InvariantCulture));Hook?.Invoke();ActualCalls++;return inner.Step(seconds);}
        public void Close(){Closes++;inner.Close();}
        public void Dispose(){Disposals++;inner.Dispose();}
    }
}
