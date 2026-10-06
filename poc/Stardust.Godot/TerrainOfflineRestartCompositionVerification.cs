using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Godot;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Geometry;
using Stardust.Core.Session;
public partial class TerrainOfflineRestartCompositionVerification : Node3D
{
    private static int checks, resourceLoads;
    private static readonly string path = "object/root.iff", zero = new('0', 64);
    private static readonly uint crc = LegacyAssetPath.Checksum(path);
    private static void Check(bool value, string why) { checks++; if (!value) throw new InvalidDataException(why); }
    private static void Reject(Action action)
    { try { action(); } catch (Exception e) when (e is InvalidDataException or InvalidOperationException or ArgumentException or NotSupportedException or ObjectDisposedException or OperationCanceledException) { checks++; return; } throw new Exception("Expected rejection"); }
    private static SceneWorldCoordinator World(string pin, uint? rootCrc = null) => new(
        new ObjectTemplateCatalog([new(rootCrc ?? crc, rootCrc is null ? path : "object/wrong.iff", pin, zero, SceneObjectResourceRecipe.CreatureDetail0)]),
        new Dictionary<SceneObjectResourceRecipe, SceneObjectResourceOptions> { [SceneObjectResourceRecipe.CreatureDetail0] = new SceneCreatureResourceOptions(
            new("appearance/lat/synthetic.lat", "idle", zero), new Dictionary<string, LogicalAnimationClipSelection> {
                ["walk"] = new("appearance/lat/synthetic.lat", "idle", zero), ["run"] = new("appearance/lat/synthetic.lat", "idle", zero) }, false) },
        (_, _) => {resourceLoads++;throw new InvalidDataException("No resource loads");}, SceneCreateZeroQuaternionPolicy.Reject, 4, 2, 32, 4194304, 32, 4194304, 1048576,
        replicationPolicy: new(SceneWorldReplicationMode.CreatureView3MixedComponents, new(1024, 64, 64, 16, 512), 16, 1024,
            new(1024, 32, 24, 64, 64, 16), CreatureWoundsApplicationPolicy.LegacyBounded, new(1024, 32, 64, 64, 16), new(16, 16)),
        creatureView4PartialPolicy: new(new(MessageLimits: new(MaximumPayloadBytes: 1048576)), new()));
    private static byte[] Start()
    { using var s = new MemoryStream(); using var w = new BinaryWriter(s); w.Write((ushort)9); w.Write(ZoneSceneProtocolCodec.CmdStartSceneOpcode);
        w.Write((byte)0); w.Write(42UL); w.Write((ushort)0); w.Write(0f); w.Write(0f); w.Write(0f); w.Write((ushort)0); w.Write(0UL); return s.ToArray(); }
    private static byte[] Create(uint? rootCrc = null) => SceneObjectLifecycleCodec.EncodeCreate(new(42, 0, 0, 0, 1, 0, 0, 0, rootCrc ?? crc, 0));
    private static long Stage(SceneWorldCoordinator w, byte[] baseline, bool typed, bool final, uint? rootCrc = null)
    { long g = w.BeginSelection(42).Generation; Check(w.Accept(g, Start()), "start"); Check(w.Accept(g, Create(rootCrc)), "create");
        if (typed) Check(w.Accept(g, baseline), "typed3"); if (final) Check(w.Accept(g, SceneObjectLifecycleCodec.EncodeEndBaselines(new(42))), "end");
        Check(w.CompleteMinimumSceneBarrier(g, true) is not null, "barrier"); return g; }
    private static SharedCreatureAuthoredMovementInputs Profile(JsonElement p)
    { var resources = p.GetProperty("Resources").EnumerateArray().ToDictionary(r => r.GetProperty("Path").GetString()!, r => new TreLookupResult(TreLookupKind.Present, Convert.FromHexString(r.GetProperty("Hex").GetString()!), 77));
        return SharedCreatureAuthoredMovementInputs.FromProfile(SharedCreatureMovementInputProfile.Load(q => resources.GetValueOrDefault(q, new(TreLookupKind.Missing)), path, p.GetProperty("RootSha256").GetString()!)); }
    private static TerrainModernHeightPatch Make(JsonElement c)
    {
        var p=c.GetProperty("Profiles").EnumerateArray().Select(v=>(TerrainHeightArithmeticPrecision)v.GetInt32()).ToArray();
        var grid=TerrainSampleCoordinateGrid.Build(c.GetProperty("Request").Deserialize<TerrainSampleCoordinateRequest>(),p[0],p[1],p[2],p[3]);
        var heights=c.GetProperty("Heights").EnumerateArray().Select(v=>v.GetUInt32()).ToArray();
        var data=TerrainCoordinateHeightPatch.Build(grid,heights,Enumerable.Repeat(Vector3.UnitY,heights.Length).ToArray(),c.GetProperty("Colors").EnumerateArray().Select(v=>v.GetUInt32()).ToArray(),c.GetProperty("Mask").GetInt32(),BitConverter.UInt32BitsToSingle(c.GetProperty("ScaleBits").GetUInt32()));
        return TerrainModernHeightPatch.Build(data,TerrainModernNormalPolicy.ModernBinary64CentralDifference);
    }
    private static uint[] V(Vector3 v)=>[BitConverter.SingleToUInt32Bits(v.X),BitConverter.SingleToUInt32Bits(v.Y),BitConverter.SingleToUInt32Bits(v.Z)];
    private static TerrainModernPatchSetRayHit Pick(TerrainModernPatchSetRayQuery query,double x,double z)=>query.Query(new(new(x,100,z),new(0,-1,0),0,200))??throw new InvalidDataException("Synthetic target ray missed.");
    public override async void _Ready()
    {
        try { await RunJoined(); GetTree().Quit(); }
        catch(Exception e) { GD.PushError("Restart composition: "+e);GetTree().Quit(1); }
    }
    private async System.Threading.Tasks.Task RunJoined()
    {
        var args=OS.GetCmdlineUserArgs();string Arg(string k){int i=Array.IndexOf(args,k);if(i<0)throw new ArgumentException(k);return args[i+1];}
        string output=Arg("--out");using var doc=JsonDocument.Parse(File.ReadAllText(Arg("--fixtures")));var f=doc.RootElement;
        var baseline=Convert.FromHexString(f.GetProperty("Baseline").GetString()!);var authored=Profile(f.GetProperty("Profiles")[0]);
        var chunks=f.GetProperty("Terrain").EnumerateArray().Select(Make).ToArray();
        var surface=TerrainModernPatchSetSurfaceQuery.Build(TerrainModernHeightPatchSet.Build(chunks,TerrainModernPatchEdgePolicy.RequireBitIdenticalActiveSharedEdges,TerrainModernPatchNormalPolicy.RequireBitIdenticalSharedNormals));
        var query=TerrainModernPatchSetRayQuery.Build(surface);using var world=World(authored.Sources[path].Sha256);Stage(world,baseline,true,true);
        using var motions=new SceneWorldObjectMotionStore(world);var snapshot=world.Snapshots().Single();
        using var queue=new SceneWorldClientRootMotionQueue(world,motions,new(100,10));
        using var runtime=new TerrainOfflineSessionRestartOwner(4);using var network=new OfflineSoeRootMotionRestartOwner(4);
        var cases=f.GetProperty("Cases").EnumerateArray().ToArray();int current=0;bool injectFactoryFailure=true;int rejected=0,accepted=0,retirements=0;
        TerrainConfiguredOfflineSession MakeRuntime()
        {
            var c=cases[current];var start=c.GetProperty("Start");Check(motions.PublishLocal(snapshot,AppearanceTransform.Identity with{Translation=new(start[0].GetSingle(),0,start[1].GetSingle())},0),"fresh explicit caller placement");
            var settings=TerrainOfflinePlaygroundSettings.Create(maximumUpdates:c.GetProperty("Maximum").GetInt32(),maximumClickAttempts:1,holdMissingDestination:!c.GetProperty("Fault").GetBoolean());
            return new(world,motions,42,query,settings,0,.25f);
        }
        OfflineSoeRootMotionSessionOwner MakeNetwork()
        {
            var c=cases[current];return new(world,motions,queue,c.GetProperty("ConnectionId").GetUInt32(),c.GetProperty("Seed").GetUInt32(),flags:5);
        }
        runtime.Start(MakeRuntime);network.Start(MakeNetwork);
        var owner=new Node3D();AddChild(owner);var rows=new List<object>();var reports=new List<TerrainOfflineSessionCompletion>();var rootReports=new List<OfflineSoeRootMotionCompletion>();
        for(int run=0;run<4;run++)
        {
            var c=cases[run];bool fault=c.GetProperty("Fault").GetBoolean();var actual=runtime.Active!;var root=network.Active!;
            Check(actual.Controller.State.CompletedUpdates==0&&root.Session.Samples==0&&root.Transport.RetainedOutboundPackets==0,"fresh generation zero history");
            string request=root.RequestHex,response=root.ResponseHex;actual.PointDriver.SetManualInput(fault?Vector2.UnitX:Vector2.UnitY,0);
            SceneWorldOfflineRootMotionDrainReceipt? drain=null;
            for(int i=1;i<=(fault?2:3);i++)
            {
                if(fault&&i==2){Reject(()=>actual.Controller.AdvanceNext(10000,20000));break;}
                actual.Controller.AdvanceNext((i-1)*10000,i*10000);
                byte[] ack=Convert.FromHexString(c.GetProperty("AckHex").GetString()!);
                try{var receipt=root.Session.Observe(snapshot,motions.Find(snapshot)!,i*10000,i*10,ack);if(receipt.Drain is not null)drain=receipt.Drain;}
                finally{Array.Clear(ack);}
            }
            var position=V(motions.Find(snapshot)!.Transform.Translation);
            var oldController=actual.Controller;runtime.Complete();network.Complete(queue);
            var completion=runtime.LastCompletion!;var rootCompletion=network.LastCompletion!;reports.Add(completion);rootReports.Add(rootCompletion);
            Check(runtime.Active is null&&network.Active is null&&actual.IsDisposed&&root.IsDisposed&&actual.PointDriver.Driver is null&&root.Transport.State==SoeClientTransportState.Closed,"owners retired before next factory");
            Check(root.Transport.PendingMessageCount==0&&root.Transport.RetainedOutboundPackets==0&&queue.Count==0,"all buffers drained");
            Check(completion.CompletedUpdates==(fault?1:3)&&completion.AttemptedUpdates==(fault?2:3)&&completion.Faulted==fault,"actual completed/fault prefix");
            Check(ReferenceEquals(completion,runtime.LastCompletion)&&ReferenceEquals(rootCompletion,network.LastCompletion),"primitive history retained");
            var hud=new TerrainOfflineRestartHud();string serialized=JsonSerializer.Serialize(completion);int callbacks=0;
            bool Restart()
            {
                callbacks++;
                if(injectFactoryFailure){injectFactoryFailure=false;try{runtime.Restart(()=>throw new IOException("deliberate factory failure"));}catch(IOException){rejected++;Check(runtime.Active is null&&network.Active is null&&runtime.Generation==1&&network.Generation==1,"failed factory keeps retired prefix");return false;}}
                current=run+1;runtime.Restart(MakeRuntime);
                try{network.Restart(MakeNetwork);}catch{runtime.Dispose();network.Dispose();throw;}
                accepted++;return true;
            }
            hud.Configure(owner,new(completion.Reason.ToString(),completion.CompletedUpdates,completion.MaximumUpdates,rootCompletion.Drained,rootCompletion.QueueCount,rootCompletion.PendingMessages,rootCompletion.RetainedPackets),Restart);
            string caption=hud.Caption;await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Check(!hud.IsRetired&&hud.Caption==caption&&owner.GetChildCount()>0&&runtime.Active is null&&network.Active is null,"outcome persists while no active children");
            if(!DisplayServer.GetName().Equals("headless",StringComparison.OrdinalIgnoreCase)&&run is 1 or 2)
            {await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);GetViewport().GetTexture().GetImage().SavePng(output+"."+run+".png");}
            rows.Add(new{Name=c.GetProperty("Name").GetString(),Generation=run+1,Completion=completion,RootCompletion=rootCompletion,Position=position,Drain=drain,RequestHex=request,ResponseHex=response});
            if(run<3)
            {
                if(run==0){Check(!hud.RequestRestart()&&hud.RequestCount==1&&hud.AcceptedRestartCount==0&&hud.Caption==caption&&!hud.IsRetired,"failed factory request keeps caption and retry");}
                if(run==0)
                {
                    GetViewport().NotifyMouseEntered();
                    using(var motion=new InputEventMouseMotion{Position=new Godot.Vector2(40,500),GlobalPosition=new Godot.Vector2(40,500),Relative=new Godot.Vector2(1,1)})GetViewport().PushInput(motion,true);
                    using(var press=new InputEventMouseButton{Pressed=true,ButtonIndex=MouseButton.Left,ButtonMask=MouseButtonMask.Left,Position=new Godot.Vector2(40,500),GlobalPosition=new Godot.Vector2(40,500)})GetViewport().PushInput(press,true);
                    using(var release=new InputEventMouseButton{Pressed=false,ButtonIndex=MouseButton.Left,Position=new Godot.Vector2(40,500),GlobalPosition=new Godot.Vector2(40,500)})GetViewport().PushInput(release,true);
                    await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                    Check(hud.AcceptedRestartCount==1&&hud.RequestCount==2,"actual GUI button starts fresh movement and root owners");
                }
                else Check(hud.RequestRestart()&&hud.AcceptedRestartCount==1,"explicit accepted restart callback");
                int count=callbacks;Check(!hud.RequestRestart()&&callbacks==count,"accepted button cannot dispatch duplicate");
                Check(JsonSerializer.Serialize(completion)==serialized&&ReferenceEquals(reports[run],completion)&&runtime.Generation==run+2&&network.Generation==run+2,"history and matching successful generation preserved");
                Reject(()=>oldController.AdvanceNext(0,10000));
                Check(motions.Find(snapshot) is not null&&queue.Count==0,"late old controller cannot damage current generation");
            }
            hud.Dispose();hud.Dispose();retirements+=hud.RetirementCount;Check(hud.IsRetired&&hud.RetirementCount==1&&owner.GetChildCount()==0,"old owned display retires once after fresh run accepted");
        }
        runtime.Dispose();network.Dispose();Check(runtime.Active is null&&network.Active is null&&runtime.IsDisposed&&network.IsDisposed,"final cycles close");
        bool partialAdmissionClosed;
        using(var partialRuntime=new TerrainOfflineSessionRestartOwner(1))using(var partialNetwork=new OfflineSoeRootMotionRestartOwner(1))
        {
            var child=partialRuntime.Start(MakeRuntime);
            try{partialNetwork.Start(()=>throw new IOException("joined second-factory failure"));throw new InvalidDataException("Expected second factory rejection");}
            catch(IOException){partialRuntime.Dispose();partialNetwork.Dispose();}
            partialAdmissionClosed=child.IsDisposed&&partialRuntime.Active is null&&partialNetwork.Active is null&&partialRuntime.LastCompletion is null&&partialNetwork.LastCompletion is null&&partialNetwork.Generation==0&&queue.Count==0;
            Check(partialAdmissionClosed,"failed second factory closes newly admitted first owner without invented completion");
        }
        bool worldSurvived=world.Snapshots().Single().Object.Handle.ObjectId==42&&motions.Find(snapshot) is not null&&query.Source.Query(7.8,7.8) is not null;
        Check(worldSurvived&&reports.Count==4&&rootReports.Count==4,"borrowed scene/surface survives all generations");
        bool noActors=resourceLoads==0&&owner.GetChildCount()==0;Check(noActors,"no resource actors loaded or orphan display nodes");owner.Free();
        File.WriteAllText(output,JsonSerializer.Serialize(new{Runs=rows,RejectedFactoryRequests=rejected,AcceptedRestarts=accepted,HudRetirements=retirements,NativeChecks=checks,WorldSurvived=worldSurvived,QueuesEmpty=queue.Count==0,NoBorrowedActorsCreated=noActors,PartialAdmissionClosed=partialAdmissionClosed}));
    }
}
