using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Session;

/// <summary>Independent deterministic host proof; no concrete Round16 pump or asset reads.</summary>
public partial class SceneWorldPresentationHostPreview : Node
{
    private readonly List<SceneWorldPresentationHost> hosts = new();
    private readonly List<FakePump> pumps = new();
    private readonly List<string> observations = new();
    private int checks;
    public override async void _Ready()
    {
        try { await Run(); GD.Print("PASS: host proof "+JsonSerializer.Serialize(new { checks, observations, pumps=pumps.Count, steps=pumps.Sum(p=>p.Steps), closes=pumps.Sum(p=>p.Closes), disposals=pumps.Sum(p=>p.Disposals), concretePump=false, originalReads=0 })); GetTree().Quit(); }
        catch(Exception error){GD.PushError("Host proof failed: "+error);foreach(var h in hosts){try{await h.CloseAsync();}catch{}}GetTree().Quit(1);}
    }
    private FakePump Pump(bool delayed=false) { var p=new FakePump(delayed);pumps.Add(p);return p; }
    private SceneWorldPresentationHost Host() {var h=new SceneWorldPresentationHost();hosts.Add(h);AddChild(h);return h;}
    private void Check(bool condition,string observation) {if(!condition)throw new InvalidDataException(observation);checks++;observations.Add(observation);}
    private void Reject<T>(Action action,string observation) where T:Exception
    {bool rejected=false;try{action();}catch(T){rejected=true;}Check(rejected,observation);}
    private async Task End(SceneWorldPresentationHost host,bool failure=false)
    {
        Check(await Task.WhenAny(host.Completion,Task.Delay(5000))==host.Completion,"bounded completion");
        bool failed=false;try{await host.Completion;}catch{failed=true;}
        Check(failed==failure && (host.Failure is not null)==failure,"expected completion result");
    }
    private async Task Run()
    {
        var rejected=Pump();
        using(var detached=new SceneWorldPresentationHost())
        {
            Reject<InvalidOperationException>(()=>detached.Configure(rejected),"detached configure rejects without ownership");
            Check(rejected.Closes==0 && rejected.Disposals==0,"rejected pump remains caller owned");
        }
        var continuous=Host();int ingress=0,frames=0;
        continuous.FrameCompleted+=step=>{frames++;Check(ingress==frames && step.AcceptedMessages==frames,"ingress before step and ordered event");if(frames==3)_=continuous.CloseAsync();};
        continuous.Configure(rejected,_=>ingress++);
        await End(continuous);
        Check(rejected.Steps==3 && continuous.CompletedFrames==3 && continuous.LastFrame?.Index==3 && rejected.Closes==1 && rejected.Disposals==1,"three real process frames close once");
        Check(ReferenceEquals(continuous.CloseAsync(),continuous.Completion),"idempotent completion identity");
        var unused=Pump();Reject<InvalidOperationException>(()=>continuous.Configure(unused),"second configure retains passed ownership");
        Check(unused.Closes==0 && unused.Disposals==0,"second candidate untouched");unused.Close();unused.Dispose();

        var configuration=Host();var badDrain=Pump();badDrain.NullDrain=true;
        Reject<ArgumentNullException>(()=>configuration.Configure(null!),"null pump rejected");
        Reject<ArgumentException>(()=>configuration.Configure(badDrain),"missing drain task rejects configuration");
        Check(badDrain.Closes==0 && badDrain.Disposals==0,"invalid configuration retains ownership");
        badDrain.NullDrain=false;badDrain.DrainQuery=()=>configuration.Configure(badDrain);
        Reject<InvalidOperationException>(()=>configuration.Configure(badDrain),"configure callback reentrancy rejected");
        badDrain.DrainQuery=null;configuration.Configure(badDrain);configuration.SetProcess(false);await configuration.CloseAsync();await End(configuration);

        var ingressClose=Host();var ingressClosePump=Pump();int skippedIngress=0;
        Action<double> ingressCallbacks=seconds=>{_=ingressClose.CloseAsync();};ingressCallbacks+=seconds=>skippedIngress++;
        ingressClose.Configure(ingressClosePump,ingressCallbacks);ingressClose.SetProcess(false);ingressClose._Process(.02);await End(ingressClose);
        Check(skippedIngress==0 && ingressClosePump.Steps==0,"ingress close suppresses later callbacks and step");

        var eventClose=Host();var eventClosePump=Pump();int skippedEvents=0;
        eventClose.FrameCompleted+=step=>{_=eventClose.CloseAsync();};eventClose.FrameCompleted+=step=>skippedEvents++;
        eventClose.Configure(eventClosePump);eventClose.SetProcess(false);eventClose._Process(.02);await End(eventClose);
        Check(skippedEvents==0 && eventClosePump.Steps==1,"observer close suppresses later callbacks");

        var threaded=Host();var threadPump=Pump();
        Check(await Task.Run(()=>{try{threaded.Configure(threadPump);return false;}catch(InvalidOperationException){return true;}}),"worker configure rejects");
        Check(threadPump.Closes==0 && threadPump.Disposals==0,"thread rejection retains pump");
        threaded.Configure(threadPump);threaded.SetProcess(false);
        Check(await Task.Run(()=>{try{threaded.CloseAsync();return false;}catch(InvalidOperationException){return true;}}),"worker close rejects");
        threaded._Process(0);Check(threadPump.Steps==1 && threaded.LastFrame?.Seconds==0,"zero delta accepted");await threaded.CloseAsync();await End(threaded);

        foreach(double delta in new[]{double.NaN,double.PositiveInfinity,-.01})
        {
            var h=Host();var p=Pump();h.Configure(p);h.SetProcess(false);h._Process(delta);await End(h,true);
            Check(p.Steps==0 && p.Closes==1 && p.Disposals==1,"invalid delta closes before step");
        }
        var before=Host();var beforePump=Pump();before.Configure(beforePump,_=>throw new IOException("ingress fault"));before.SetProcess(false);before._Process(.02);await End(before,true);
        Check(beforePump.Steps==0 && beforePump.Closes==1 && beforePump.Disposals==1,"ingress exception closes and disposes");

        var stepHost=Host();var stepPump=Pump();stepPump.StepFault=true;stepHost.Configure(stepPump);stepHost.SetProcess(false);stepHost._Process(.02);await End(stepHost,true);
        Check(stepPump.Steps==1 && stepHost.CompletedFrames==0 && stepPump.Closes==1 && stepPump.Disposals==1,"step fault emits no receipt");

        var observer=Host();var observerPump=Pump();int laterObservers=0;
        observer.FrameCompleted+=_=>throw new IOException("observer fault");observer.FrameCompleted+=_=>laterObservers++;
        observer.Configure(observerPump);observer.SetProcess(false);observer._Process(.02);await End(observer,true);
        Check(observer.CompletedFrames==1 && laterObservers==0 && observerPump.Disposals==1,"observer fault suppresses later callbacks");

        var reentrant=Host();var reentrantPump=Pump();reentrant.Configure(reentrantPump,_=>reentrant._Process(.02));reentrant.SetProcess(false);reentrant._Process(.02);await End(reentrant,true);
        Check(reentrantPump.Steps==0 && reentrantPump.Disposals==1,"frame reentrancy closes");

        var mutation=Host();var mutationPump=Pump();mutation.Configure(mutationPump,_=>mutation.BeforeStep=null);mutation.SetProcess(false);mutation._Process(.02);await End(mutation,true);
        Check(mutationPump.Steps==0 && mutationPump.Disposals==1,"callback mutation rejected");

        var callbackClose=Host();var closePump=Pump();callbackClose.Configure(closePump,seconds=>{_=callbackClose.CloseAsync();throw new IOException("fault after close");});callbackClose.SetProcess(false);callbackClose._Process(.02);await End(callbackClose,true);
        Check(closePump.Steps==0 && closePump.Closes==1 && closePump.Disposals==1,"close then callback fault still fails completion");

        var delayed=Host();var delayedPump=Pump(true);delayed.Configure(delayedPump);delayed.SetProcess(false);
        var close=delayed.CloseAsync();Check(!close.IsCompleted && delayedPump.Closes==1 && delayedPump.Disposals==0,"dispose waits for drain");
        delayedPump.Release();await End(delayed);Check(delayedPump.Disposals==1,"delayed drain completes before dispose");

        var exit=Host();var exitPump=Pump(true);exit.Configure(exitPump);exit.SetProcess(false);RemoveChild(exit);
        Check(exit.IsClosing && exitPump.Closes==1 && !exit.Completion.IsCompleted && exitPump.Disposals==0,"exit synchronously closes and hands off drain");
        exitPump.Release();await End(exit);Check(exitPump.Disposals==1,"detached host awaits drain on owner thread");

        var aggregate=Host();var aggregatePump=Pump(true);aggregatePump.CloseFault=true;aggregatePump.DisposeFault=true;aggregate.Configure(aggregatePump);aggregate.SetProcess(false);_=aggregate.CloseAsync();aggregatePump.Release(true);await End(aggregate,true);
        Check(aggregate.Failure is AggregateException && aggregatePump.Closes==1 && aggregatePump.Disposals==1,"close drain disposal faults retained");
        foreach(var h in hosts){await End(h,h.Failure is not null);if(h.GetParent()==this)RemoveChild(h);h.Free();}
        Check(pumps.All(p=>p.Closes==1 && p.Disposals==1),"all pump owners released once");
    }
    private sealed class FakePump : ISceneWorldPresentationPump
    {
        private readonly SceneWorldCoordinator world;
        private readonly TaskCompletionSource<bool>? drain;
        private readonly int owner=System.Environment.CurrentManagedThreadId;
        public SceneWorldObjectPresentationConsumer Consumer {get;}
        public int QueuedMessageCount=>0;
        public long QueuedMessageBytes=>0;
        public int PendingPublicationCount=>0;
        public long PendingResourceWeight=>0;
        public Task DrainCompletion {get{DrainQuery?.Invoke();return NullDrain?null!:drain?.Task??Task.CompletedTask;}}
        public int Steps,Closes,Disposals;
        public bool StepFault,CloseFault,DisposeFault,NullDrain;
        public Action? DrainQuery;
        public FakePump(bool delayed)
        {
            var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(ProjectSettings.GlobalizePath("res://../preview-profiles/scene-object-catalog.json")));
            catalog=new ObjectTemplateCatalog(catalog.Entries.Values.Where(e=>e.Recipe==SceneObjectResourceRecipe.CreatureDetail0));
            var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
            using var bankFile=File.OpenRead(ProjectSettings.GlobalizePath("res://../preview-profiles/human-locomotion.json"));
            var bank=SkeletalAnimationBankProfile.Load(bankFile);
            world=new(catalog,idle,bank,(_,_)=>throw new InvalidOperationException("Host proof has no asset loader"),SceneCreateZeroQuaternionPolicy.Reject);
            Consumer=new(world,(_,_)=>throw new InvalidOperationException("Host proof has no visual factory"));
            if(delayed)drain=new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        private void Owner(){if(System.Environment.CurrentManagedThreadId!=owner)throw new InvalidOperationException("Fake owner thread");}
        public void QueueApplication(long generation,ReadOnlySpan<byte> message)=>throw new NotSupportedException("No ingress protocol in host mechanics fixture");
        public SceneWorldPresentationPumpStep Step(double seconds)
        {Owner();Steps++;if(StepFault)throw new IOException("step fault");if(Consumer.Tick(seconds) || Consumer.Count!=0)throw new InvalidDataException("Cold baseline consumer changed");return new(Steps,0,0,0,0,false,0,0,false);}
        public void Close(){Owner();Closes++;world.Close();if(CloseFault)throw new IOException("close fault");}
        public void Dispose(){Owner();Disposals++;Consumer.Dispose();world.Dispose();if(DisposeFault)throw new IOException("dispose fault");}
        public void Release(bool fault=false){if(fault)drain!.TrySetException(new IOException("drain fault"));else drain!.TrySetResult(true);}
    }
}
