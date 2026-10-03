using Stardust.Core.Assets;
using Stardust.Core.Session;
using Stardust.Core.SmokeTests;
try {
    if(args.Length!=6)throw new ArgumentException("root cfg catalog bank peer export");
    bool affinityChecked=false;
    ISceneWorldPresentationPump Create(SceneWorldCoordinator world,
        Func<ScenePreparedObjectPublication,SceneWorldObjectSnapshot,ISceneWorldObjectPresentation?> factory,
        SceneWorldPresentationPumpPolicy policy)
    {
        if(!affinityChecked) {
            bool rejected=Task.Factory.StartNew(() => {
                try { _=new SceneWorldPresentationPump(world,factory,policy);return false; }
                catch(InvalidOperationException) { return true; }
            },CancellationToken.None,TaskCreationOptions.LongRunning,TaskScheduler.Default).GetAwaiter().GetResult();
            if(!rejected || world.State!=ZoneSceneTransitionState.Ready)throw new InvalidDataException("Constructor owner mismatch must reject before ownership");
            affinityChecked=true;
        }
        return new SceneWorldPresentationPump(world,factory,policy,r=>SceneObjectResourceAccounting.Measure(r).PendingWeight);
    }
    SceneWorldPumpPeerScenarios.Run(args[..4],args[4],args[5],Create);
    if(!affinityChecked)throw new InvalidDataException("Owner mismatch check was not executed");
    Console.WriteLine("PASS: joined concrete pump, logical resource accounting and constructor owner rejection");
} catch(Exception error) {Console.Error.WriteLine(error);Environment.ExitCode=1;}
