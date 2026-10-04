using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;
public static class SceneWorldCurrentMovementAdmissionScenarios
{
    private static int checks;
    private static void Check(bool value,string why){checks++;if(!value)throw new InvalidDataException(why);}
    private static void Reject(Action action){bool rejected=false;try{action();}catch(Exception e)when(e is InvalidDataException or InvalidOperationException or ArgumentException or ObjectDisposedException){rejected=true;}Check(rejected,"expected rejection");}
    public static SceneWorldCreatureMovementCurrentInputs Reader(SceneWorldCoordinator w,SharedCreatureAuthoredMovementInputs p)=>new(w,42,p,p.Sources[p.RootPath].Sha256,SceneCreatureMovementConstructorScope.CallerVerifiedConcreteClientCreatureRawInputs,true);
    public static SceneWorldExplicitMovementAdmission Admission(SceneWorldCoordinator w,SceneWorldObjectMotionStore m,SharedCreatureAuthoredMovementInputs p,SceneWorldMovementSpeedSource source,SceneWorldCreatureMovementCurrentInputs reader)=>new(w,m,42,source,p,p.Sources[p.RootPath].Sha256,stepMicroseconds:10000,currentInputs:reader);
    public static void Run(string fixture,string output)
    {
        using var j=JsonDocument.Parse(File.ReadAllText(fixture,System.Text.Encoding.UTF8));var profiles=j.RootElement.GetProperty("Profiles").EnumerateArray().Select(SceneWorldExplicitStopScenarios.Profile).ToArray();byte[] baseline=Convert.FromHexString(j.RootElement.GetProperty("Baseline").GetString()!);var cases=new List<object>();
        foreach(var c in j.RootElement.GetProperty("Cases").EnumerateArray())
        {
            var p=profiles[c.GetProperty("Profile").GetInt32()];using var w=SceneWorldExplicitStopScenarios.World(p);long g=SceneWorldExplicitStopScenarios.Stage(w,baseline);using var m=new SceneWorldObjectMotionStore(w);using var reader=Reader(w,p);var source=(SceneWorldMovementSpeedSource)c.GetProperty("Source").GetInt32();using var a=Admission(w,m,p,source,reader);var d=c.GetProperty("Direction").EnumerateArray().Select(v=>v.GetSingle()).ToArray();var rows=new List<object>();
            foreach(var stage in j.RootElement.GetProperty("Stages").EnumerateArray())
            {
                a.Reset(0);if(stage.GetProperty("Wire").ValueKind!=JsonValueKind.Null)Check(w.Accept(g,Convert.FromHexString(stage.GetProperty("Wire").GetString()!)),"independent wire");var observation=reader.Read(g)!;var field=observation.Get(source==SceneWorldMovementSpeedSource.CurrentField7?(ushort)7:(ushort)11);var before=a.State;var pub=m.Find(SceneWorldExplicitStopScenarios.Target(w));var receipt=a.Submit(0,new(d[0],d[1]));
                if(receipt is null){Check(a.State==before&&ReferenceEquals(pub,m.Find(SceneWorldExplicitStopScenarios.Target(w))),"ineligible current source no prefix/state/publication mutation");Check(a.SubmitStop(0),"distinct stop on ineligible source");}
                else{Check(receipt.CurrentSource is not null&&reader.IsCurrent(receipt.CurrentSource)&&a.IsSourceCurrent(receipt),"current source captured and owned");Check(receipt.AuthoredSelection is null&&receipt.ReceivedSource is null&&receipt.CurrentSource!.AuthoredRoot==p,"full current source only");}
                int partition=c.GetProperty("Partition").GetInt32();for(long t=partition;t<30000;t+=partition)a.Advance(t);var state=a.Advance(30000);Check(state.Active&&state.FaultType is null,"actual captured native ticks");rows.Add(new{InputBits=field.RawBits,Origin=(int)field.Origin,Provenance=field.ReceivedInput?.Provenance,Eligible=receipt is not null,RawBits=receipt?.RawSpeedBits,CurrentSource=receipt?.CurrentSource is not null,Source=(int)a.SpeedSource,Position=new[]{state.Transform.Translation.X,state.Transform.Translation.Y,state.Transform.Translation.Z}});
            }
            cases.Add(rows);
        }
        Negatives(profiles[0],baseline,j.RootElement.GetProperty("Profiles")[0]);File.WriteAllText(output,JsonSerializer.Serialize(new{Cases=cases,Checks=checks}),new System.Text.UTF8Encoding(false));Console.WriteLine($"R50_CURRENT_ADMISSION_PASS sequences={cases.Count} stages={cases.Count*9} checks={checks}");
    }
    public static void Original(string archiveRoot,string configuration,string oraclePath,string fixture,string output)
    {
        if(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(configuration)))!="22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524")throw new InvalidDataException("Configuration pin");
        using var oracle=JsonDocument.Parse(File.ReadAllText(oraclePath,System.Text.Encoding.UTF8));var root=oracle.RootElement.GetProperty("Chain")[0];string path=root.GetProperty("Path").GetString()!,sha=root.GetProperty("Sha256").GetString()!;
        using var text=new StreamReader(configuration,System.Text.Encoding.UTF8,true);using var overlay=ConfiguredTreOverlay.Open(archiveRoot,text);var p=SharedCreatureAuthoredMovementInputs.FromProfile(SharedCreatureMovementInputProfile.Load(overlay.Lookup,path,sha));
        using var j=JsonDocument.Parse(File.ReadAllText(fixture,System.Text.Encoding.UTF8));byte[] baseline=Convert.FromHexString(j.RootElement.GetProperty("Baseline").GetString()!);var rows=new List<object>();
        foreach(var source in new[]{SceneWorldMovementSpeedSource.CurrentField7,SceneWorldMovementSpeedSource.CurrentField11})foreach(var direction in new[]{Vector2.UnitX,new Vector2(.6f,.8f),-Vector2.UnitY})
        {
            var idle=new LogicalAnimationClipSelection("appearance/lat/synthetic.lat","synthetic_idle",new string('0',64));var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{SceneObjectResourceRecipe.CreatureDetail0,new SceneCreatureResourceOptions(idle,new Dictionary<string,LogicalAnimationClipSelection>{{"walk",idle},{"run",idle}},false)}};
            using var w=new SceneWorldCoordinator(new ObjectTemplateCatalog(new[]{new ObjectTemplateCatalogEntry(LegacyAssetPath.Checksum(path),path,sha,new string('0',64))}),options,(_,_)=>throw new InvalidDataException("No original resource load"),SceneCreateZeroQuaternionPolicy.Reject,4,2,32,4194304,32,4194304,1048576,replicationPolicy:new(SceneWorldReplicationMode.CreatureView3MixedComponents,new(1024,64,64,16,512),16,1024,new(1024,32,24,64,64,16),CreatureWoundsApplicationPolicy.LegacyBounded,new(1024,32,64,64,16),new(16,16)),creatureView4PartialPolicy:new(new(MessageLimits:new(MaximumPayloadBytes:1048576)),new()));
            long g=w.BeginSelection(42).Generation;using var stream=new MemoryStream();using(var writer=new BinaryWriter(stream,System.Text.Encoding.UTF8,true)){writer.Write((ushort)9);writer.Write(ZoneSceneProtocolCodec.CmdStartSceneOpcode);writer.Write((byte)0);writer.Write(42UL);writer.Write((ushort)0);writer.Write(0f);writer.Write(0f);writer.Write(0f);writer.Write((ushort)0);writer.Write(0UL);}w.Accept(g,stream.ToArray());w.Accept(g,SceneObjectLifecycleCodec.EncodeCreate(new(42,0,0,0,1,0,0,0,LegacyAssetPath.Checksum(path),0)));w.Accept(g,baseline);w.Accept(g,SceneObjectLifecycleCodec.EncodeEndBaselines(new(42)));w.CompleteMinimumSceneBarrier(g,true);
            using var motions=new SceneWorldObjectMotionStore(w);using var reader=Reader(w,p);using var a=Admission(w,motions,p,source,reader);var stages=new List<object>();
            foreach(var stage in j.RootElement.GetProperty("Stages").EnumerateArray())
            {
                a.Reset(0);if(stage.GetProperty("Wire").ValueKind!=JsonValueKind.Null)w.Accept(g,Convert.FromHexString(stage.GetProperty("Wire").GetString()!));var observed=reader.Read(g)!.Get(source==SceneWorldMovementSpeedSource.CurrentField7?(ushort)7:(ushort)11);var receipt=a.Submit(0,direction);if(receipt is null)a.SubmitStop(0);var state=a.Advance(30000);stages.Add(new{InputBits=observed.RawBits,Origin=(int)observed.Origin,Eligible=receipt is not null,RawBits=receipt?.RawSpeedBits,Position=new[]{state.Transform.Translation.X,state.Transform.Translation.Y,state.Transform.Translation.Z}});
            }
            rows.Add(new{Source=(int)source,Direction=new[]{direction.X,direction.Y},Stages=stages});
        }
        File.WriteAllText(output,JsonSerializer.Serialize(new{Cases=rows,Sources=p.Sources,Mounts=overlay.MountCount}),new System.Text.UTF8Encoding(false));Console.WriteLine("R50_ORIGINAL_RAW_ADMISSION_PASS sequences=6 stages=54");
    }
    private static void Negatives(SharedCreatureAuthoredMovementInputs p,byte[] baseline,JsonElement profileFixture)
    {
        using var w=SceneWorldExplicitStopScenarios.World(p);long g=SceneWorldExplicitStopScenarios.Stage(w,baseline);using var m=new SceneWorldObjectMotionStore(w);using var reader=Reader(w,p);using var a=Admission(w,m,p,SceneWorldMovementSpeedSource.CurrentField7,reader);
        var bootstrap=a.Submit(20000,Vector2.UnitX)!;Check(bootstrap.CurrentSource!.Get(7).Origin==SceneCreatureMovementCurrentOrigin.ConstructorAuthoredRaw&&bootstrap.Speed==2,"absence bootstrap current choice");
        SceneWorldExplicitStopScenarios.Received(w,g,null,null);Check(!a.IsSourceCurrent(bootstrap),"late first listen publication invalidates old absence context");Check(Math.Abs(a.Advance(30000).Transform.Translation.X-.02f)<1e-7,"queued constructor speed captured after first partial publication");
        a.Reset(0);SceneWorldExplicitStopScenarios.Received(w,g,BitConverter.SingleToUInt32Bits(3),BitConverter.SingleToUInt32Bits(8));var captured=a.Submit(20000,Vector2.UnitX)!;
        SceneWorldExplicitStopScenarios.Received(w,g,BitConverter.SingleToUInt32Bits(9),BitConverter.SingleToUInt32Bits(4));Check(!a.IsSourceCurrent(captured)&&captured.RawSpeedBits==BitConverter.SingleToUInt32Bits(3),"later raw receipt cannot rewrite captured velocity");Check(Math.Abs(a.Advance(30000).Transform.Translation.X-.03f)<1e-7,"actual later-update captured tick");
        a.Reset(0);var fresh=a.Submit(0,Vector2.UnitX)!;Check(w.Accept(g,baseline)&&!a.IsSourceCurrent(fresh),"typed3 publication invalidates exact captured source");Check(a.Submit(0,Vector2.UnitY)!.CurrentSource!.HasCurrentTypedState,"fresh typed context automatic capture");
        var last=a.Submit(0,Vector2.UnitX)!;Check(w.Accept(g,SceneObjectLifecycleCodec.EncodeContainment(new(42,0,1)))&&!a.IsSourceCurrent(last),"registry containment invalidates source");Check(a.Submit(0,Vector2.UnitX)!.CurrentSource!.Target.Object.ContainerId==0,"fresh registry context capture");
        a.Reset(0);var before=a.State;var pub=m.Find(SceneWorldExplicitStopScenarios.Target(w));foreach(var d in new[]{Vector2.Zero,new Vector2(1,1),new Vector2(float.NaN,0)})Check(a.Submit(0,d)is null,"direction rejects");Reject(()=>a.Submit(-1,Vector2.UnitX));Reject(()=>a.Submit(60000001,Vector2.UnitX));Check(a.State==before&&ReferenceEquals(pub,m.Find(SceneWorldExplicitStopScenarios.Target(w))),"direction/time failures no mutation");a.Submit(20000,Vector2.UnitX);Reject(()=>a.Submit(19999,Vector2.UnitY));a.Reset(0);
        for(int i=0;i<1024;i++)Check(a.Submit(0,Vector2.UnitX)is not null,"current admission respects pending cap");Reject(()=>a.SubmitStop(0));Reject(()=>a.Submit(0,Vector2.UnitY));a.Reset(0);Check(a.SubmitStop(0),"stop after reset no sampling");
        Task.Run(()=>Reject(()=>a.Submit(0,Vector2.UnitX))).GetAwaiter().GetResult();reader.Dispose();before=a.State;Reject(()=>a.Submit(0,Vector2.UnitX));Check(a.State==before&&a.SubmitStop(0),"disposed borrowed current reader has no fallback; stop independent");
        using(var pending=SceneWorldExplicitStopScenarios.World(p))
        {
            long pg=SceneWorldExplicitStopScenarios.Stage(pending,baseline);pending.Accept(pg,SceneObjectLifecycleCodec.EncodeDestroy(new(42,0)));pending.Accept(pg,SceneWorldExplicitStopScenarios.Create());using var pm=new SceneWorldObjectMotionStore(pending);using var pr=Reader(pending,p);Reject(()=>Admission(pending,pm,p,SceneWorldMovementSpeedSource.CurrentField7,pr));pending.Accept(pg,SceneObjectLifecycleCodec.EncodeEndBaselines(new(42)));Reject(()=>Admission(pending,pm,p,SceneWorldMovementSpeedSource.CurrentField7,pr));Check(pm.Find(SceneWorldExplicitStopScenarios.Target(pending))is null,"pending/missing typed constructor no publication");
        }
        foreach(var change in new[]{"recreate","generation","close"})using(var world=SceneWorldExplicitStopScenarios.World(p))
        {
            long gen=SceneWorldExplicitStopScenarios.Stage(world,baseline);using var store=new SceneWorldObjectMotionStore(world);using var r=Reader(world,p);using var old=Admission(world,store,p,SceneWorldMovementSpeedSource.CurrentField11,r);var receipt=old.Submit(0,Vector2.UnitX)!;var saved=old.State;
            if(change=="recreate"){world.Accept(gen,SceneObjectLifecycleCodec.EncodeDestroy(new(42,0)));world.Accept(gen,SceneWorldExplicitStopScenarios.Create());world.Accept(gen,baseline);world.Accept(gen,SceneObjectLifecycleCodec.EncodeEndBaselines(new(42)));}else if(change=="generation")SceneWorldExplicitStopScenarios.Stage(world,baseline);else world.Close();Check(!old.IsSourceCurrent(receipt)&&old.Submit(0,Vector2.UnitX)is null&&!old.SubmitStop(0)&&old.State==saved,"stale exact-current incarnation "+change);
        }
        using(var world=SceneWorldExplicitStopScenarios.World(p))
        {
            long gen=SceneWorldExplicitStopScenarios.Stage(world,baseline);using var store=new SceneWorldObjectMotionStore(world);using var r=Reader(world,p);using var direct=new SceneWorldReceivedCreatureMovementInputs(world);string sha=p.Sources[p.RootPath].Sha256;
            Reject(()=>new SceneWorldExplicitMovementAdmission(world,store,42,SceneWorldMovementSpeedSource.CurrentField7,p,sha));Reject(()=>new SceneWorldExplicitMovementAdmission(world,store,42,SceneWorldMovementSpeedSource.CurrentField7,p,sha,direct,currentInputs:r));Reject(()=>new SceneWorldExplicitMovementAdmission(world,store,42,SceneWorldMovementSpeedSource.AuthoredSpeed0,p,sha,currentInputs:r));Reject(()=>new SceneWorldExplicitMovementAdmission(world,store,42,SceneWorldMovementSpeedSource.ReceivedField7,p,sha,direct,currentInputs:r));
            var clone=SceneWorldExplicitStopScenarios.Profile(profileFixture);using var cloneReader=Reader(world,clone);Reject(()=>Admission(world,store,p,SceneWorldMovementSpeedSource.CurrentField7,cloneReader));
            using(var foreign=SceneWorldExplicitStopScenarios.World(p)){SceneWorldExplicitStopScenarios.Stage(foreign,baseline);using var foreignReader=Reader(foreign,p);Reject(()=>Admission(world,store,p,SceneWorldMovementSpeedSource.CurrentField7,foreignReader));}
            Check(store.Find(SceneWorldExplicitStopScenarios.Target(world))is null,"all conflicting/root/world constructor failures before initial publication");
            using var valid=Admission(world,store,p,SceneWorldMovementSpeedSource.CurrentField11,r);SceneWorldExplicitStopScenarios.Received(world,gen,0,0);Reject(()=>valid.Submit(0,Vector2.UnitX,direct.Read(gen,42)));var zero=valid.Submit(0,Vector2.UnitX)!;valid.Dispose();Check(r.IsCurrent(zero.CurrentSource!)&&world.State!=ZoneSceneTransitionState.Closed,"dispose owns driver only");
        }
    }
}
