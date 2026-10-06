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
public partial class TerrainPointTargetInputVerification : Node3D
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
    public override async void _Ready()
    {try{await Run();GetTree().Quit();}catch(Exception e){GD.PushError("Point input: "+e);GetTree().Quit(1);}}
    private async Task Run()
    {
        string[] args=OS.GetCmdlineUserArgs();string Arg(string name){int i=Array.IndexOf(args,name);Need(i>=0,"Explicit argument "+name);return args[i+1];}
        string output=Path.GetFullPath(Arg("--out")),repository=Path.GetFullPath(Arg("--repository"));Need(output.StartsWith(Path.Combine(repository,"local-private")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)&&!File.Exists(output),"Fresh own private receipt.");
        using var doc=JsonDocument.Parse(File.ReadAllText(Arg("--fixtures")));var f=doc.RootElement;var ray=Make(f.GetProperty("Flat"));var foreignRay=Make(f.GetProperty("Flat"));
        var baseline=Convert.FromHexString(f.GetProperty("Baseline").GetString()!);string pin=f.GetProperty("RootSha256").GetString()!;
        using var world=World(pin);Stage(world,baseline,true,true);using var motions=new SceneWorldObjectMotionStore(world);
        var snapshot=world.Snapshots().Single();Need(motions.PublishLocal(snapshot,AppearanceTransform.Identity with{Translation=new(4,0,4)},0),"Actual stored typed starting pose.");
        using var controller=new SceneWorldTerrainPointTargetDriver(world,motions,42,ray,2,.01f,0,10000,maximumTargets:16);
        var terrain=new Node3D();AddChild(terrain);var parent=new Node3D();AddChild(parent);var visuals=ray.Source.Children.Select(c=>new TerrainPatchVisual(c.Geometry,terrain)).ToArray();
        var camera=new Camera3D{Current=true,Fov=50,Near=.05f,Far=200,Position=new(4,12,-4)};AddChild(camera);camera.LookAt(new(4,0,-4),Vector3.Back);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);Vector2 center=GetViewport().GetVisibleRect().Size/2;
        bool gpu=DisplayServer.GetName()!="headless";int checks=0,admissionChecks=0;var samples=new List<object>();var targets=new List<object>();
        using var picker=new TerrainCameraPicker(camera,parent,ray);using var adapter=new TerrainPointTargetInputAdapter(picker,controller);
        void Check(bool value,string why){Need(value,why);checks++;}
        void Reject(Action action){try{action();}catch(Exception e) when(e is InvalidOperationException or ArgumentException or OperationCanceledException or InvalidDataException or OverflowException){checks++;admissionChecks++;return;}throw new InvalidOperationException("Expected rejection");}
        InputEventMouseButton Button(Vector2 pixel,MouseButton index=MouseButton.Left,bool pressed=true)=>new(){Position=pixel,ButtonIndex=index,Pressed=pressed};
        void Observe(Vector2 pixel,string label,long clock=0)
        {
            using var e=Button(pixel);Check(adapter.HandleInput(e,clock,1000,out var result),"Actual pressed left dispatched.");var r=result!.Pick!;
            Check(result.Attempt==adapter.Attempts&&r.Attempt==result.PickerAttemptsAfter&&result.PickerAttemptsAfter==result.PickerAttemptsBefore+1&&ReferenceEquals(result.Owner,adapter),"Actual issued counters.");
            Check(ReferenceEquals(result,adapter.LastResult)&&ReferenceEquals(r,picker.Current)&&ReferenceEquals(r.Query,controller.Query)&&ReferenceEquals(r.Query,ray)&&adapter.IsCurrent(result),"Actual references and currency.");
            samples.Add(new{Label=label,Model=0,Pixel=new[]{pixel.X,pixel.Y},Viewport=new[]{r.ViewportSize.X,r.ViewportSize.Y},EngineOrigin=V(r.EngineOrigin),EngineDirection=V(r.EngineDirection),NativeOrigin=new[]{r.Ray.Origin.X,r.Ray.Origin.Y,r.Ray.Origin.Z},NativeDirection=new[]{r.Ray.Direction.X,r.Ray.Direction.Y,r.Ray.Direction.Z},MaximumT=r.Ray.MaximumT,Projection=camera.Projection.ToString(),camera.Fov,camera.Size,camera.Near,CameraPosition=V(camera.GlobalPosition),Basis=new[]{V(camera.GlobalBasis.X),V(camera.GlobalBasis.Y),V(camera.GlobalBasis.Z)},Hit=Hit(r.Hit),r.Revision,r.Attempt,Marker=picker.Marker is { } marker?new{Visible=marker.Visible,Position=V(marker.GlobalPosition)}:null});
            if(r.Hit is not null)
            {
                var t=result.Target!;Check(ReferenceEquals(t,controller.Target)&&ReferenceEquals(t.Hit,r.Hit)&&ReferenceEquals(t.Query,r.Query),"Exact returned hit submitted without reconstruction.");
                targets.Add(new{Sample=samples.Count-1,XBits=BitConverter.SingleToUInt32Bits(t.X),ZBits=BitConverter.SingleToUInt32Bits(t.Z),Clock=t.TimestampMicroseconds,ExpectedClock=clock,ExactReferences=true});
            }else Check(result.Target is null&&ReferenceEquals(controller.Target,result.PriorTarget),"Miss retains prior controller target.");
        }
        using(var released=Button(center,pressed:false)){Check(!adapter.HandleInput(released,long.MinValue,double.NaN,out var ignored,new CancellationToken(true))&&ignored is null,"Release ignored without clock/token/range work.");}
        using(var right=Button(center,MouseButton.Right)){Check(!adapter.HandleInput(right,0,1000,out _),"Right ignored.");}
        using(var motion=new InputEventMouseMotion()){Check(!adapter.HandleInput(motion,0,1000,out _),"Motion ignored.");}
        Check(adapter.Attempts==0&&picker.Attempts==0&&controller.Target is null,"Ignored events charge nothing.");
        picker.ValidateAdmission(center,1000);Check(picker.Attempts==0&&picker.Current is null&&picker.OwnedResourceCount==0,"Fresh picker admission has no effects.");
        Reject(()=>new TerrainPointTargetInputAdapter(picker,controller,0));Reject(()=>new TerrainPointTargetInputAdapter(picker,controller,17));
        using(var foreign=new SceneWorldTerrainPointTargetDriver(world,motions,42,foreignRay,2,.01f,0,10000)){Reject(()=>new TerrainPointTargetInputAdapter(picker,foreign));}
        foreach(var pixel in new[]{new Vector2(-1,0),new Vector2(float.NaN,0),new Vector2(GetViewport().GetVisibleRect().Size.X,0)})
        {using var e=Button(pixel);Reject(()=>adapter.HandleInput(e,0,1000,out _));}
        using(var e=Button(center))
        {Reject(()=>adapter.HandleInput(e,1,1000,out _));Reject(()=>adapter.HandleInput(e,0,double.NaN,out _));Reject(()=>adapter.HandleInput(e,0,-1,out _));Reject(()=>adapter.HandleInput(e,0,1000,out _,new CancellationToken(true)));}
        camera.Current=false;using(var e=Button(center)){Reject(()=>adapter.HandleInput(e,0,1000,out _));}camera.Current=true;
        Check(adapter.Attempts==0&&picker.Attempts==0&&controller.Target is null,"Admission failures before all effects.");
        using(var offThreadEvent=Button(center)){await Task.Run(()=>{try{adapter.HandleInput(offThreadEvent,0,1000,out _);throw new Exception("Off-thread accepted");}catch(InvalidOperationException){}});}checks++;
        using(var freshWorld=World(pin))
        {
            Stage(freshWorld,baseline,true,true);using var freshStore=new SceneWorldObjectMotionStore(freshWorld);
            Need(freshStore.PublishLocal(freshWorld.Snapshots().Single(),AppearanceTransform.Identity with{Translation=new(4,0,4)},0),"Fresh miss pose.");
            using var freshController=new SceneWorldTerrainPointTargetDriver(freshWorld,freshStore,42,ray,2,.01f,0,10000,maximumTargets:1);
            using var freshPicker=new TerrainCameraPicker(camera,parent,ray);using var fresh=new TerrainPointTargetInputAdapter(freshPicker,freshController);
            camera.Position=new(40,12,-40);camera.LookAt(new(40,0,-40),Vector3.Back);
            using var e=Button(center);fresh.HandleInput(e,0,1000,out var miss);
            Check(miss!.Pick!.Hit is null&&freshController.Target is null&&fresh.IsCurrent(miss)&&freshPicker.OwnedResourceCount==0,"Fresh miss currency without prior target or marker.");
            freshController.Dispose();Check(!fresh.IsCurrent(miss),"Disposed controller stales miss.");
            camera.Position=new(4,12,-4);camera.LookAt(new(4,0,-4),Vector3.Back);
        }
        using(var overflowPicker=new TerrainCameraPicker(camera,parent,ray,revisionSeed:ulong.MaxValue))
        {Reject(()=>overflowPicker.ValidateAdmission(center,1000));Check(overflowPicker.Attempts==0&&overflowPicker.Current is null&&overflowPicker.OwnedResourceCount==0,"Admission revision overflow has no effects.");}
        using(var closedPicker=new TerrainCameraPicker(camera,parent,ray))
        {closedPicker.Dispose();Reject(()=>closedPicker.ValidateAdmission(center,1000));Reject(()=>new TerrainPointTargetInputAdapter(closedPicker,controller));}
        camera.Projection=Camera3D.ProjectionType.Frustum;using(var e=Button(center)){Reject(()=>adapter.HandleInput(e,0,1000,out _));}camera.Projection=Camera3D.ProjectionType.Perspective;
        Check(adapter.Attempts==0&&picker.Attempts==0,"Additional admission checks still charge nothing.");
        Observe(center,"fresh-hit");var historical=adapter.LastResult;
        camera.Position=new(6,12,-6);camera.LookAt(new(6,0,-6),Vector3.Back);Observe(center,"retarget");Check(!adapter.IsCurrent(historical),"Retarget historical currency.");
        camera.Position=new(40,12,-40);camera.LookAt(new(40,0,-40),Vector3.Back);Observe(center,"miss");
        camera.Position=new(4,10,6);camera.LookAt(new(4,0,-4));Observe(center,"oblique");
        controller.AdvanceNext(0,10000);using(var e=Button(center)){Reject(()=>adapter.HandleInput(e,0,1000,out _));}Observe(center,"current-clock",10000);
        // A last admissible target stays current; a second press rejects before picker charge.
        using(var limitedController=new SceneWorldTerrainPointTargetDriver(world,motions,42,ray,2,.01f,0,10000,maximumTargets:1))
        using(var limitedPicker=new TerrainCameraPicker(camera,parent,ray))
        using(var limited=new TerrainPointTargetInputAdapter(limitedPicker,limitedController))
        using(var e=Button(center))
        {
            limited.HandleInput(e,0,1000,out var last);Check(limited.IsCurrent(last),"Last admissible successful target remains current.");
            var oldPick=limitedPicker.Current;Reject(()=>limited.HandleInput(e,0,1000,out _));Check(limited.Attempts==1&&limitedPicker.Attempts==1&&ReferenceEquals(oldPick,limitedPicker.Current)&&limited.IsCurrent(last),"Target cap rejected before pick.");
        }
        // Real property callback cancels after picker commits, before SubmitTarget.
        bool prefixRejected=false;
        using(var prefixPicker=new TerrainCameraPicker(camera,parent,ray))
        using(var prefix=new TerrainPointTargetInputAdapter(prefixPicker,controller))
        using(var cancel=new CancellationTokenSource())
        using(var e=Button(center))
        {
            prefix.HandleInput(e,10000,1000,out var first);var priorTarget=controller.Target;var marker=prefixPicker.Marker!;
            marker.Visible=false;
            void Cancel()=>cancel.Cancel();marker.VisibilityChanged+=Cancel;
            try{Reject(()=>prefix.HandleInput(e,10000,1000,out _,cancel.Token));}finally{marker.VisibilityChanged-=Cancel;}
            var failed=prefix.LastResult!;prefixRejected=failed.Pick is not null&&ReferenceEquals(failed.Pick,prefixPicker.Current)&&failed.FailureType is not null&&failed.Target is null&&ReferenceEquals(controller.Target,priorTarget)&&prefix.Attempts==2&&prefixPicker.Attempts==2&&failed.PickerAttemptsBefore==1&&failed.PickerAttemptsAfter==2&&failed.Attempt==2;
            Check(prefixRejected&&!prefix.IsCurrent(failed),"Exact committed pick prefix and prior target preserved.");
        }
        // A caught adapter reentry from actual AddChild must invalidate the outer target suffix.
        using(var reentryPicker=new TerrainCameraPicker(camera,parent,ray))
        using(var reentry=new TerrainPointTargetInputAdapter(reentryPicker,controller))
        using(var e=Button(center))
        {
            int nested=0;var priorTarget=controller.Target;
            void Reenter(Node node){if(node is not MeshInstance3D)return;try{reentry.HandleInput(e,10000,1000,out _);}catch(InvalidOperationException){nested++;}try{reentry.Dispose();}catch(InvalidOperationException){nested++;}}
            parent.ChildEnteredTree+=Reenter;try{Reject(()=>reentry.HandleInput(e,10000,1000,out _));}finally{parent.ChildEnteredTree-=Reenter;}
            Check(nested==2&&reentry.LastResult!.Pick is not null&&reentryPicker.Current is not null&&ReferenceEquals(controller.Target,priorTarget)&&!reentry.IsDisposed,"Caught callback reentry records committed picker prefix without target.");
        }
        using(var guardPicker=new TerrainCameraPicker(camera,parent,ray))
        {
            int nested=0;void Reenter(Node node){if(node is not MeshInstance3D)return;try{guardPicker.ValidateAdmission(center,1000);}catch(InvalidOperationException){nested++;}}
            parent.ChildEnteredTree+=Reenter;try{Reject(()=>guardPicker.Pick(center,1000));}finally{parent.ChildEnteredTree-=Reenter;}
            Check(nested==1&&guardPicker.Current is null&&guardPicker.OwnedResourceCount==0,"Admission preserves picker caught-reentry poison.");
        }
        using(var cappedPicker=new TerrainCameraPicker(camera,parent,ray,maximumAttempts:1))
        using(var capped=new TerrainPointTargetInputAdapter(cappedPicker,controller,maximumAttempts:1))
        using(var e=Button(center))
        {capped.HandleInput(e,10000,1000,out _);Reject(()=>capped.HandleInput(e,10000,1000,out _));Reject(()=>cappedPicker.ValidateAdmission(center,1000));Check(capped.Attempts==1&&cappedPicker.Attempts==1,"Adapter and picker caps distinct.");}
        camera.Position=new(4,12,-4);camera.LookAt(new(4,0,-4),Vector3.Back);Observe(center,"final-visible",10000);
        if(gpu){await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=GetViewport().GetTexture().GetImage();var pixel=image.GetPixel((int)center.X,(int)center.Y);Check(pixel.R>.8f&&pixel.B>.8f&&pixel.G<.2f,"GPU actual marker");Need(image.SavePng(Path.ChangeExtension(output,".png"))==Error.Ok,"Own GPU capture");}
        var finalPick=picker.Current;var finalTarget=controller.Target;var finalMarker=picker.Marker;adapter.Dispose();adapter.Dispose();
        bool survives=ReferenceEquals(picker.Current,finalPick)&&ReferenceEquals(controller.Target,finalTarget)&&GodotObject.IsInstanceValid(finalMarker)&&picker.OwnedResourceCount==3&&!adapter.IsCurrent(historical)&&adapter.LastResult is null;
        Check(survives,"Adapter disposal drains only its references and diagnostics.");
        using(var e=Button(center)){Reject(()=>adapter.HandleInput(e,10000,1000,out _));}
        // Retirement must fail admission before a fresh picker can charge or allocate a marker.
        using(var stalePicker=new TerrainCameraPicker(camera,parent,ray))
        using(var stale=new TerrainPointTargetInputAdapter(stalePicker,controller))
        using(var e=Button(center))
        {world.BeginSelection(43);Reject(()=>stale.HandleInput(e,10000,1000,out _));Check(stale.Attempts==0&&stalePicker.Attempts==0&&stalePicker.OwnedResourceCount==0,"Retired world rejected pre-pick.");}
        picker.Dispose();controller.Dispose();foreach(var visual in visuals)visual.Dispose();Check(picker.OwnedResourceCount==0&&parent.GetChildCount()==0&&terrain.GetChildCount()==0,"Owners drain exact resources.");terrain.Free();parent.Free();camera.Free();
        File.WriteAllText(output,JsonSerializer.Serialize(new{Gpu=gpu,Samples=samples,Targets=targets,Checks=checks,AdmissionChecks=admissionChecks,OwnedResourcesAfter=0,BorrowedSurvival=true,AdapterBorrowedSurvival=survives,PrefixRejected=prefixRejected,CapBeforePick=true,AttemptsMaximum=256,OriginalPickingParity=false}),new System.Text.UTF8Encoding(false));GD.Print("PASS point input adapter");
    }
}
