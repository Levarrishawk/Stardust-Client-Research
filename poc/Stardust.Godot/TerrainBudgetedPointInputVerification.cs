using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Geometry;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
using NVector3=System.Numerics.Vector3;

/// <summary>Own standalone replacement test node; synthetic sources, actual cameras and GPU marker.</summary>
public partial class TerrainBudgetedPointInputVerification : Node3D
{
    private static void Need(bool value,string why){if(!value)throw new InvalidOperationException(why);}
    private static double[] V(Vector3 v)=>new[]{(double)v.X,v.Y,v.Z};
    private static float F(uint b)=>BitConverter.UInt32BitsToSingle(b);
    private static TerrainModernPatchSetRayQuery Make(JsonElement chunks)
    {
        var receipts=new List<TerrainModernHeightPatch>();
        foreach(var c in chunks.EnumerateArray())
        {
            var grid=TerrainSampleCoordinateGrid.Build(c.GetProperty("Request").Deserialize<TerrainSampleCoordinateRequest>(),TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24);
            var heights=c.GetProperty("Heights").EnumerateArray().Select(v=>v.GetUInt32()).ToArray();
            var source=TerrainCoordinateHeightPatch.Build(grid,heights,Enumerable.Repeat(NVector3.UnitY,heights.Length).ToArray(),Enumerable.Repeat(0xff20a060u,heights.Length).ToArray());
            receipts.Add(TerrainModernHeightPatch.Build(source,TerrainModernNormalPolicy.ModernBinary64CentralDifference));
        }
        return TerrainModernPatchSetRayQuery.Build(TerrainModernPatchSetSurfaceQuery.Build(TerrainModernHeightPatchSet.Build(receipts.ToArray(),TerrainModernPatchEdgePolicy.IndependentSuppliedPatches,TerrainModernPatchNormalPolicy.IgnoreSharedNormals)));
    }
    private static object? Hit(TerrainModernPatchSetRayHit? h)=>h is null?null:new{Chunk=h.ChunkIndex,Patch=h.PatchIndex,h.TriangleIndex,h.VertexA,h.VertexB,h.VertexC,h.T,Position=new[]{h.Position.X,h.Position.Y,h.Position.Z},Weights=new[]{h.WeightA,h.WeightB,h.WeightC},Normal=new[]{h.Normal.X,h.Normal.Y,h.Normal.Z}};
    private static readonly string path = "object/root.iff", zero = new('0', 64);
    private static readonly uint crc = LegacyAssetPath.Checksum(path);

    private static SceneWorldCoordinator World(string pin, uint? rootCrc = null) => new(
        new ObjectTemplateCatalog([new(rootCrc ?? crc, rootCrc is null ? path : "object/wrong.iff", pin, zero, SceneObjectResourceRecipe.CreatureDetail0)]),
        new Dictionary<SceneObjectResourceRecipe, SceneObjectResourceOptions> { [SceneObjectResourceRecipe.CreatureDetail0] = new SceneCreatureResourceOptions(
            new("appearance/lat/synthetic.lat", "idle", zero), new Dictionary<string, LogicalAnimationClipSelection> {
                ["walk"] = new("appearance/lat/synthetic.lat", "idle", zero), ["run"] = new("appearance/lat/synthetic.lat", "idle", zero) }, false) },
        (_, _) => throw new InvalidDataException("No resource loads"), SceneCreateZeroQuaternionPolicy.Reject, 4, 2, 32, 4194304, 32, 4194304, 1048576,
        replicationPolicy: new(SceneWorldReplicationMode.CreatureView3MixedComponents, new(1024, 64, 64, 16, 512), 16, 1024,
            new(1024, 32, 24, 64, 64, 16), CreatureWoundsApplicationPolicy.LegacyBounded, new(1024, 32, 64, 64, 16), new(16, 16)),
        creatureView4PartialPolicy: new(new(MessageLimits: new(MaximumPayloadBytes: 1048576)), new()));
    private static byte[] Start()
    { using var s = new MemoryStream(); using var w = new BinaryWriter(s); w.Write((ushort)9); w.Write(ZoneSceneProtocolCodec.CmdStartSceneOpcode);
        w.Write((byte)0); w.Write(42UL); w.Write((ushort)0); w.Write(0f); w.Write(0f); w.Write(0f); w.Write((ushort)0); w.Write(0UL); return s.ToArray(); }
    private static byte[] Create(uint? rootCrc = null) => SceneObjectLifecycleCodec.EncodeCreate(new(42, 0, 0, 0, 1, 0, 0, 0, rootCrc ?? crc, 0));
    private static long Stage(SceneWorldCoordinator w, byte[] baseline, bool typed, bool final, uint? rootCrc = null)
    { long g = w.BeginSelection(42).Generation; Need(w.Accept(g, Start()), "start"); Need(w.Accept(g, Create(rootCrc)), "create");
        if (typed) Need(w.Accept(g, baseline), "typed3"); if (final) Need(w.Accept(g, SceneObjectLifecycleCodec.EncodeEndBaselines(new(42))), "end");
        Need(w.CompleteMinimumSceneBarrier(g, true) is not null, "barrier"); return g; }
    public override async void _Ready(){try{await Run();GetTree().Quit();}catch(Exception e){GD.PushError("Budgeted point: "+e);GetTree().Quit(1);}}
    private async Task Run()
    {
        var args=OS.GetCmdlineUserArgs();string Arg(string key){int i=Array.IndexOf(args,key);Need(i>=0,key);return args[i+1];}
        string output=Arg("--out");using var doc=JsonDocument.Parse(File.ReadAllText(Arg("--fixtures")));var f=doc.RootElement;
        var ray=Make(f.GetProperty("Flat"));var baseline=Convert.FromHexString(f.GetProperty("Baseline").GetString()!);string pin=f.GetProperty("RootSha256").GetString()!;
        using var world=World(pin);Stage(world,baseline,true,true);using var motions=new SceneWorldObjectMotionStore(world);Need(motions.PublishLocal(world.Snapshots().Single(),AppearanceTransform.Identity with{Translation=new(4,0,4)},0),"starting pose");
        var budget=new TerrainOfflineSessionBudget(128,32);using var driver=new SceneWorldTerrainPointTargetDriver(world,motions,42,ray,2,.01f,0,10000,maximumTargets:32,budget:budget);
        var parent=new Node3D();AddChild(parent);var camera=new Camera3D{Current=true,Fov=50,Near=.05f,Far=200,Position=new(4,12,-4)};AddChild(camera);camera.Basis=new Basis(Vector3.Right,Vector3.Forward,Vector3.Up);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);var center=GetViewport().GetVisibleRect().Size/2;
        int checks=0,rejections=0,clockCalls=0;void Check(bool value,string why){checks++;Need(value,why);}void Reject(Action call){try{call();}catch(Exception e) when(e is InvalidOperationException or ArgumentException or OperationCanceledException){checks++;rejections++;return;}throw new InvalidOperationException("Expected rejection");}
        InputEventMouseButton Click()=>new(){Pressed=true,ButtonIndex=MouseButton.Left,Position=center,GlobalPosition=center};
        using var picker=new TerrainCameraPicker(camera,parent,ray,maximumAttempts:32);using var adapter=new TerrainPointTargetInputAdapter(picker,driver,32,budget,true);var host=new TerrainPointTargetInputHost();parent.AddChild(host);host.Configure(adapter,()=>{clockCalls++;return driver.State.DriverResult.TimestampMicroseconds;});
        var rows=new List<object>();var samples=new List<object>();
        for(int i=0;i<32;i++)
        {
            bool miss=i==7;float p=miss?40:i%2==0?4:6;camera.Position=new(p,12,-p);camera.Basis=new Basis(Vector3.Right,Vector3.Forward,Vector3.Up);
            using var e=Click();GetViewport().PushInput(e,true);var result=adapter.LastResult!;var pick=result.Pick!;
            Check(!host.IsFaulted&&host.AcceptedCount==i+1&&host.HandledCount==i+1&&clockCalls==i+1&&adapter.Attempts==i+1&&picker.Attempts==i+1,"actual admitted viewport click");
            rows.Add(new{Index=i,Attempts=adapter.Attempts,Remaining=adapter.RemainingAttempts,Exhausted=adapter.IsExhausted,PickerAttempts=picker.Attempts,HostAccepted=host.AcceptedCount,HostHandled=host.HandledCount,ClockCalls=clockCalls,Hit=pick.Hit is not null,TargetRevision=driver.Target?.Revision,SuccessfulTargets=driver.State.SuccessfulTargets,TargetXBits=result.Target is null?(uint?)null:BitConverter.SingleToUInt32Bits(result.Target.X),TargetZBits=result.Target is null?(uint?)null:BitConverter.SingleToUInt32Bits(result.Target.Z)});
            samples.Add(new{Label="click-"+i,Model=0,Pixel=new[]{center.X,center.Y},Viewport=new[]{pick.ViewportSize.X,pick.ViewportSize.Y},EngineOrigin=V(pick.EngineOrigin),EngineDirection=V(pick.EngineDirection),NativeOrigin=new[]{pick.Ray.Origin.X,pick.Ray.Origin.Y,pick.Ray.Origin.Z},NativeDirection=new[]{pick.Ray.Direction.X,pick.Ray.Direction.Y,pick.Ray.Direction.Z},MaximumT=pick.Ray.MaximumT,Projection=camera.Projection.ToString(),camera.Fov,camera.Size,camera.Near,CameraPosition=V(camera.GlobalPosition),Basis=new[]{V(camera.GlobalBasis.X),V(camera.GlobalBasis.Y),V(camera.GlobalBasis.Z)},Hit=Hit(pick.Hit),Marker=picker.Marker is { } marker?new{Visible=marker.Visible,Position=V(marker.GlobalPosition)}:null});
        }
        var last=adapter.LastResult;var oldPick=picker.Current;var oldTarget=driver.Target;var oldDiagnostic=host.LastDiagnostic;bool current=adapter.IsCurrent(last);
        using(var e=Click())GetViewport().PushInput(e,true);
        Check(adapter.Attempts==32&&picker.Attempts==32&&clockCalls==32&&host.ClockReadCount==32&&host.AcceptedCount==32&&host.HandledCount==32&&!host.IsFaulted,"33rd actual viewport declined before clock/query/handled");
        Check(ReferenceEquals(last,adapter.LastResult)&&ReferenceEquals(oldPick,picker.Current)&&ReferenceEquals(oldTarget,driver.Target)&&ReferenceEquals(oldDiagnostic,host.LastDiagnostic)&&current&&adapter.IsCurrent(last),"exhaustion preserves exact issued histories");
        bool directDeclined;using(var e=Click()){directDeclined=!adapter.HandleInput(e,long.MinValue,double.NaN,out var result)&&result is null;}Check(directDeclined,"direct exhausted left returns false/null without clock/range inspection");
        using(var e=Click())Reject(()=>adapter.HandleInput(e,0,1000,out _,new CancellationToken(true)));
        Check(adapter.MaximumAttempts==32&&adapter.RemainingAttempts==0&&adapter.IsExhausted&&adapter.DeclineWhenExhausted,"owner getters");
        await Task.Run(()=>{foreach(Action read in new Action[]{()=>{_ = adapter.MaximumAttempts;},()=>{_ = adapter.RemainingAttempts;},()=>{_ = adapter.IsExhausted;},()=>{_ = adapter.DeclineWhenExhausted;}}){try{read();throw new Exception("off-thread getter");}catch(InvalidOperationException){}}});checks++;
        // Budget admission changes no actual borrower cap or identity.
        Reject(()=>new TerrainPointTargetInputAdapter(picker,driver,0,budget));Reject(()=>new TerrainPointTargetInputAdapter(picker,driver,33,budget));Reject(()=>new TerrainPointTargetInputAdapter(picker,driver,17));Reject(()=>new TerrainPointTargetInputAdapter(picker,driver,16,new TerrainOfflineSessionBudget(128,8)));
        driver.SetManualInput(new(1,0),0);driver.AdvanceNext(0,10000);driver.SetManualInput(System.Numerics.Vector2.Zero,10000);driver.AdvanceNext(10000,20000);
        var motion=driver.State.DriverResult;Check(driver.State.CompletedUpdates==2&&driver.State.Mode==SceneWorldTerrainPointTargetMode.Idle,"manual move and stop after click exhaustion");
        var manual=new{Updates=driver.State.CompletedUpdates,Clock=motion.TimestampMicroseconds,Mode=driver.State.Mode.ToString(),XBits=BitConverter.SingleToUInt32Bits(motion.Transform.Translation.X),ZBits=BitConverter.SingleToUInt32Bits(motion.Transform.Translation.Z),SpeedBits=BitConverter.SingleToUInt32Bits(motion.Speed)};
        // Fresh strict adapter/picker and controller: exact legacy16 cap remains throwing.
        using(var strictDriver=new SceneWorldTerrainPointTargetDriver(world,motions,42,ray,2,.01f,20000,10000,maximumTargets:16))
        using(var strictPicker=new TerrainCameraPicker(camera,parent,ray,maximumAttempts:32))
        using(var strict=new TerrainPointTargetInputAdapter(strictPicker,strictDriver))
        {
            for(int i=0;i<16;i++){using var e=Click();strict.HandleInput(e,20000,1000,out _);}var prior=strict.LastResult;using var exhausted=Click();Reject(()=>strict.HandleInput(exhausted,20000,1000,out _));
            Check(strict.Attempts==16&&strictPicker.Attempts==16&&strict.MaximumAttempts==16&&strict.IsExhausted&&!strict.DeclineWhenExhausted&&ReferenceEquals(prior,strict.LastResult),"legacy16 strict throwing cap before extra picker query");
        }
        foreach(bool pickerLimited in new[]{false,true})
        {
            using var limitedDriver=new SceneWorldTerrainPointTargetDriver(world,motions,42,ray,2,.01f,20000,10000,maximumTargets:pickerLimited?32:1,budget:budget);
            using var limitedPicker=new TerrainCameraPicker(camera,parent,ray,maximumAttempts:pickerLimited?1:32);
            using var limited=new TerrainPointTargetInputAdapter(limitedPicker,limitedDriver,32,budget,true);
            using var e=Click();limited.HandleInput(e,20000,1000,out var retained);Reject(()=>limited.HandleInput(e,20000,1000,out _));
            Check(limited.Attempts==1&&limited.RemainingAttempts==31&&!limited.IsExhausted&&limitedPicker.Attempts==1&&ReferenceEquals(retained,limited.LastResult),"explicit adapter budget never raises hidden borrower cap");
        }
        using(var probePicker=new TerrainCameraPicker(camera,parent,ray,maximumAttempts:32))
        using(var probeDriver=new SceneWorldTerrainPointTargetDriver(world,motions,42,ray,2,.01f,20000,10000,maximumTargets:32,budget:budget))
        using(var probe=new TerrainPointTargetInputAdapter(probePicker,probeDriver,32,budget,true))
        using(var e=Click())
        {
            Reject(()=>probe.HandleInput(e,20001,1000,out _));Reject(()=>probe.HandleInput(e,20000,1000,out _,new CancellationToken(true)));
            camera.Current=false;Reject(()=>probe.HandleInput(e,20000,1000,out _));camera.Current=true;
            Check(probe.Attempts==0&&probePicker.Attempts==0&&probe.LastResult is null,"clock/cancel/lifecycle rejects before query");
            await Task.Run(()=>{try{probe.HandleInput(e,20000,1000,out _);throw new Exception("off-thread accepted");}catch(InvalidOperationException){}});checks++;
        }
        bool gpu=DisplayServer.GetName()!="headless";if(gpu){await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);Check(true,"GPU frame");}
        adapter.Dispose();using(var retiredClick=Click())Reject(()=>host.Dispatch(retiredClick));Check(host.IsFaulted&&clockCalls==32&&host.ClockReadCount==32&&host.LastDiagnostic is {Clock:null,FailureType:"System.ObjectDisposedException"},"disposed exhausted adapter rejects before clock callback");host.Close();host.Free();Check(!picker.IsDisposed&&picker.Current is not null&&driver.Query==ray&&GodotObject.IsInstanceValid(parent),"adapter disposes no borrower");picker.Dispose();driver.Dispose();Check(picker.OwnedResourceCount==0&&parent.GetChildCount()==0,"owners clean resources");parent.Free();camera.Free();Check(GetChildCount()==0,"all native nodes released");
        File.WriteAllText(output,JsonSerializer.Serialize(new{Passed=true,Gpu=gpu,Checks=checks,Rejections=rejections,Rows=rows,Samples=samples,Manual=manual,Declined33rd=true,DirectDeclined=directDeclined,ExactHistoryPreserved=true,BorrowedSurvival=true,OwnedResourcesAfter=0,AttemptsMaximum=256,OriginalPickingParity=false}));GD.Print("PASS budgeted point input");
    }
}
