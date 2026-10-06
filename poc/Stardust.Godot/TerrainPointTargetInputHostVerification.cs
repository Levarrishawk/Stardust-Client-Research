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
public partial class TerrainPointTargetInputHostVerification : Node3D
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
    {try{await Run();GetTree().Quit();}catch(Exception e){GD.PushError("Point input host: "+e);GetTree().Quit(1);}}
    private async Task Run()
    {
        string[] args=OS.GetCmdlineUserArgs();string Arg(string name){int i=Array.IndexOf(args,name);Need(i>=0,"Explicit argument "+name);return args[i+1];}
        string output=Path.GetFullPath(Arg("--out")),repository=Path.GetFullPath(Arg("--repository"));Need(output.StartsWith(Path.Combine(repository,"local-private")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)&&!File.Exists(output),"Fresh own private receipt.");
        using var doc=JsonDocument.Parse(File.ReadAllText(Arg("--fixtures")));var f=doc.RootElement;var ray=Make(f.GetProperty("Flat"));var baseline=Convert.FromHexString(f.GetProperty("Baseline").GetString()!);string pin=f.GetProperty("RootSha256").GetString()!;
        using var world=World(pin);Stage(world,baseline,true,true);using var motions=new SceneWorldObjectMotionStore(world);var snapshot=world.Snapshots().Single();Need(motions.PublishLocal(snapshot,AppearanceTransform.Identity with{Translation=new(4,0,4)},0),"Actual typed starting pose.");
        using var controller=new SceneWorldTerrainPointTargetDriver(world,motions,42,ray,2,.01f,0,10000,maximumTargets:16);
        var terrain=new Node3D();AddChild(terrain);var markerParent=new Node3D();AddChild(markerParent);var visuals=ray.Source.Children.Select(c=>new TerrainPatchVisual(c.Geometry,terrain)).ToArray();
        var camera=new Camera3D{Current=true,Fov=50,Near=.05f,Far=200,Position=new(4,12,-4)};AddChild(camera);camera.LookAt(new(4,0,-4),Vector3.Back);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        Vector2 center=GetViewport().GetVisibleRect().Size/2;bool gpu=DisplayServer.GetName()!="headless";int checks=0,admissionChecks=0,pushed=0,totalHandled=0;var samples=new List<object>();var targets=new List<object>();
        using var picker=new TerrainCameraPicker(camera,markerParent,ray);using var adapter=new TerrainPointTargetInputAdapter(picker,controller);
        long clock=0;int readerInvocations=0;var propagation=new TerrainPointInputPropagationProbe();AddChild(propagation);var hostParent=new Node();AddChild(hostParent);var host=new TerrainPointTargetInputHost();hostParent.AddChild(host);
        void Check(bool value,string why){Need(value,why);checks++;}
        void Reject(Action action){try{action();}catch(Exception e) when(e is InvalidOperationException or ArgumentException or OperationCanceledException){checks++;admissionChecks++;return;}throw new InvalidOperationException("Expected rejection");}
        InputEventMouseButton Button(Vector2 pixel,MouseButton index=MouseButton.Left,bool pressed=true)=>new(){Position=pixel,ButtonIndex=index,Pressed=pressed};
        bool Push(InputEvent e){pushed++;int before=propagation.Count;GetViewport().PushInput(e,true);return propagation.Count==before;}
        host.Configure(adapter,()=>{readerInvocations++;return clock;});Check(host.IsConfigured&&!host.IsClosed&&!host.IsFaulted&&adapter.Attempts==0&&picker.Attempts==0&&controller.Target is null&&readerInvocations==0,"Configure performs no clock read/query/target admission.");
        Reject(()=>host.Configure(adapter,()=>0));
        using(var release=Button(center,pressed:false)){Check(!Push(release),"Viewport released left unhandled.");}
        using(var right=Button(center,MouseButton.Right)){Check(!Push(right),"Viewport right unhandled.");}
        using(var motion=new InputEventMouseMotion{Position=center}){Check(!Push(motion),"Viewport motion unhandled.");}
        Check(host.ClockReadCount==0&&readerInvocations==0&&host.HandledCount==0&&host.AcceptedCount==0&&adapter.Attempts==0&&picker.Attempts==0,"Ignored viewport events have no effects.");
        void Observe(string label)
        {
            var prior=host.LastResult;int before=picker.Attempts;
            using var e=Button(center);Check(Push(e),"Actual Viewport.PushInput marks admitted event handled.");totalHandled++;
            var result=host.LastResult!;var r=result.Pick!;
            Check(ReferenceEquals(result,adapter.LastResult)&&ReferenceEquals(r,picker.Current)&&ReferenceEquals(result.Owner,adapter)&&ReferenceEquals(r.Query,ray)&&ReferenceEquals(result.Target?.Hit??r.Hit,r.Hit)&&result.PickerAttemptsBefore==before&&result.PickerAttemptsAfter==picker.Attempts,"Raw child receipt/ref/counter identity.");
            Check(host.LastDiagnostic!.FromGodotEvent&&host.LastFailure is null&&host.HandledCount==totalHandled&&host.ClockReadCount==readerInvocations&&host.AcceptedCount==totalHandled&&adapter.IsCurrent(result),"Actual callback diagnostics and counts.");
            if(prior is not null)Check(!adapter.IsCurrent(prior),"Historical child receipt currency.");
            samples.Add(new{Label=label,Model=0,Pixel=new[]{center.X,center.Y},Viewport=new[]{r.ViewportSize.X,r.ViewportSize.Y},EngineOrigin=V(r.EngineOrigin),EngineDirection=V(r.EngineDirection),NativeOrigin=new[]{r.Ray.Origin.X,r.Ray.Origin.Y,r.Ray.Origin.Z},NativeDirection=new[]{r.Ray.Direction.X,r.Ray.Direction.Y,r.Ray.Direction.Z},MaximumT=r.Ray.MaximumT,Projection=camera.Projection.ToString(),camera.Fov,camera.Size,camera.Near,CameraPosition=V(camera.GlobalPosition),Basis=new[]{V(camera.GlobalBasis.X),V(camera.GlobalBasis.Y),V(camera.GlobalBasis.Z)},Hit=Hit(r.Hit),r.Revision,r.Attempt,Marker=picker.Marker is { } m?new{Visible=m.Visible,Position=V(m.GlobalPosition)}:null});
            if(r.Hit is not null){var t=result.Target!;Check(ReferenceEquals(t,controller.Target)&&ReferenceEquals(t.Hit,r.Hit)&&ReferenceEquals(t.Query,r.Query),"Exact actual target.");targets.Add(new{Sample=samples.Count-1,XBits=BitConverter.SingleToUInt32Bits(t.X),ZBits=BitConverter.SingleToUInt32Bits(t.Z),Clock=t.TimestampMicroseconds,ExactReferences=true});}
            else Check(result.Target is null&&ReferenceEquals(result.PriorTarget,controller.Target),"Miss retains actual controller target.");
        }
        Observe("fresh-hit");camera.Position=new(6,12,-6);camera.LookAt(new(6,0,-6),Vector3.Back);Observe("retarget");
        camera.Position=new(40,12,-40);camera.LookAt(new(40,0,-40),Vector3.Back);Observe("miss");
        controller.AdvanceNext(0,10000);clock=10000;camera.Position=new(4,10,6);camera.LookAt(new(4,0,-4));Observe("current-clock");
        camera.Position=new(4,12,-4);camera.LookAt(new(4,0,-4),Vector3.Back);Observe("final-visible");
        if(gpu){await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=GetViewport().GetTexture().GetImage();var pixel=image.GetPixel((int)center.X,(int)center.Y);Check(pixel.R>.8f&&pixel.B>.8f&&pixel.G<.2f,"GPU actual marker");Need(image.SavePng(Path.ChangeExtension(output,".png"))==Error.Ok,"Own capture");}
        var actualLast=adapter.LastResult;var actualTarget=controller.Target;var actualPick=picker.Current;host.Close();host.Close();Check(host.IsClosed&&host.LastResult is null&&!host.IsProcessingUnhandledInput(),"Terminal close drops result/ref/input processing.");
        bool borrowedSurvival=ReferenceEquals(adapter.LastResult,actualLast)&&ReferenceEquals(controller.Target,actualTarget)&&ReferenceEquals(picker.Current,actualPick)&&picker.OwnedResourceCount==3&&!adapter.IsDisposed;Check(borrowedSurvival,"Close preserves borrowed adapter/picker/controller.");
        using(var e=Button(center)){Check(!Push(e)&&host.ClockReadCount==5&&adapter.Attempts==5,"Closed node receives no events.");Reject(()=>host.Dispatch(e));}Reject(()=>host.Configure(adapter,()=>clock));host.Free();hostParent.Free();
        // Configuration negatives use no caller clock, query or target mutation.
        var detached=new TerrainPointTargetInputHost();Reject(()=>detached.Configure(adapter,()=>clock));detached.Free();
        var configParent=new Node();AddChild(configParent);var configHost=new TerrainPointTargetInputHost();configParent.AddChild(configHost);
        foreach(double invalid in new[]{double.NaN,double.PositiveInfinity,-1d,1e9+1})Reject(()=>configHost.Configure(adapter,()=>clock,invalid));
        Check(!configHost.IsConfigured&&adapter.Attempts==5&&picker.Attempts==5,"Failed configuration has no child effects.");configHost.Close();configParent.Free();
        bool clockReentry=true,eventFault=true,nativePrefix=true;
        // A clock callback catches nested host mutations; the outer guard rejects before child effects.
        foreach(string mutation in new[]{"Dispatch","Configure","Close"})
        {
            var p=new Node();AddChild(p);var h=new TerrainPointTargetInputHost();p.AddChild(h);int nested=0;using var e=Button(center);
            h.Configure(adapter,()=>{try{if(mutation=="Dispatch")h.Dispatch(e);else if(mutation=="Configure")h.Configure(adapter,()=>clock);else h.Close();}catch(InvalidOperationException){nested++;}return clock;});
            int attempts=adapter.Attempts,queries=picker.Attempts;var last=adapter.LastResult;Check(!Push(e),"Clock reentry event remains unhandled.");
            clockReentry&=nested==1&&h.IsFaulted&&h.ClockReadCount==1&&h.HandledCount==0&&adapter.Attempts==attempts&&picker.Attempts==queries&&ReferenceEquals(h.LastResult,last);
            Check(clockReentry&&!h.IsProcessingUnhandledInput()&&h.LastFailure==typeof(InvalidOperationException).FullName,"Caught clock mutation poisons outer before child.");
            Check(!Push(e)&&h.ClockReadCount==1,"Faulted host processing disabled.");h.Close();p.Free();
        }
        foreach(string failure in new[]{"InvalidClock","ReaderThrows","Token"})
        {
            var p=new Node();AddChild(p);var h=new TerrainPointTargetInputHost();p.AddChild(h);h.Configure(adapter,failure=="ReaderThrows"?()=>throw new InvalidDataException("Synthetic caller failure"):()=>clock+1);
            int attempts=adapter.Attempts,queries=picker.Attempts;using var e=Button(center);
            if(failure=="Token"){Reject(()=>h.Dispatch(e,new CancellationToken(true)));Check(h.ClockReadCount==0,"Precancelled direct dispatch does not read clock.");}
            else Check(!Push(e),"Actual event failure is not handled.");
            eventFault&=h.IsFaulted&&!h.IsProcessingUnhandledInput()&&h.HandledCount==0&&adapter.Attempts==attempts&&picker.Attempts==queries&&h.LastFailure is not null&&ReferenceEquals(h.LastResult,adapter.LastResult);
            Check(eventFault,"Clock/read/token failures bounded and child diagnostics exact.");h.Close();p.Free();
        }
        var cancelledParent=new Node();AddChild(cancelledParent);var cancelledHost=new TerrainPointTargetInputHost();cancelledParent.AddChild(cancelledHost);
        using(var token=new CancellationTokenSource())
        {
            int sideEffects=0;cancelledHost.Configure(adapter,()=>{sideEffects++;token.Cancel();return clock;});int attempts=adapter.Attempts;using var e=Button(center);
            Reject(()=>cancelledHost.Dispatch(e,token.Token));
            Check(sideEffects==1&&cancelledHost.ClockReadCount==1&&cancelledHost.IsFaulted&&adapter.Attempts==attempts&&picker.Attempts==attempts&&ReferenceEquals(cancelledHost.LastResult,adapter.LastResult),"Caller callback cancellation prefix remains, child is untouched.");
        }
        cancelledHost.Close();cancelledParent.Free();
        var queuedParent=new Node();AddChild(queuedParent);var queuedHost=new TerrainPointTargetInputHost();queuedParent.AddChild(queuedHost);queuedHost.Configure(adapter,()=>clock);queuedParent.QueueFree();
        int queuedAttempts=adapter.Attempts;using(var e=Button(center)){Check(!Push(e),"Queued parent event unhandled.");}
        Check(queuedHost.IsFaulted&&queuedHost.ClockReadCount==0&&adapter.Attempts==queuedAttempts&&picker.Attempts==queuedAttempts,"Queued parent rejected before clock or child.");
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);Check(queuedHost.IsClosed,"Queued parent actual teardown closes host.");
        // Actual marker callback reenters the host after child work starts: child commits can survive.
        using(var prefixPicker=new TerrainCameraPicker(camera,markerParent,ray))
        using(var prefixAdapter=new TerrainPointTargetInputAdapter(prefixPicker,controller))
        {
            var p=new Node();AddChild(p);var h=new TerrainPointTargetInputHost();p.AddChild(h);h.Configure(prefixAdapter,()=>clock);using var e=Button(center);int nested=0;
            Check(h.Dispatch(e)&&h.AcceptedCount==1&&h.HandledCount==0,"Direct setup preserves engine handled count distinction.");
            var marker=prefixPicker.Marker!;marker.Visible=false;var priorResult=prefixAdapter.LastResult;var priorTarget=controller.Target;
            void Reenter(){try{h.Close();}catch(InvalidOperationException){nested++;}}
            marker.VisibilityChanged+=Reenter;try{Check(!Push(e),"Post-child host reentry rejects event handling.");}finally{marker.VisibilityChanged-=Reenter;}
            nativePrefix=nested==1&&h.IsFaulted&&h.AcceptedCount==1&&h.HandledCount==0&&ReferenceEquals(h.LastResult,prefixAdapter.LastResult)&&h.LastResult?.Pick is not null&&h.LastResult.Target is not null&&ReferenceEquals(controller.Target,h.LastResult.Target)&&!ReferenceEquals(controller.Target,priorTarget)&&!ReferenceEquals(h.LastResult,priorResult)&&prefixAdapter.Attempts==2&&prefixPicker.Attempts==2;
            Check(nativePrefix,"Actual pick AND target prefix survives host failure; no rollback claim.");h.Close();p.Free();
        }
        // ExitTree closes before a callback can route further events; freeing the parent never closes borrowers.
        var exitParent=new Node();AddChild(exitParent);var exitHost=new TerrainPointTargetInputHost();exitParent.AddChild(exitHost);exitHost.Configure(adapter,()=>clock);exitParent.Free();
        bool exitClosed=exitHost.IsClosed&&!adapter.IsDisposed&&picker.OwnedResourceCount==3;Check(exitClosed,"Freed parent terminally closes host, borrowers survive.");
        var callbackParent=new Node();AddChild(callbackParent);var callbackHost=new TerrainPointTargetInputHost();callbackParent.AddChild(callbackHost);callbackHost.Configure(adapter,()=>{RemoveChild(callbackParent);return clock;});
        int beforeExit=adapter.Attempts;using(var e=Button(center)){Reject(()=>callbackHost.Dispatch(e));}
        Check(callbackHost.IsClosed&&callbackHost.LastResult is null&&adapter.Attempts==beforeExit&&callbackHost.ClockReadCount==1,"ExitTree inside clock closes before child and drops receipt refs.");callbackParent.Free();
        var threadParent=new Node();AddChild(threadParent);var threadHost=new TerrainPointTargetInputHost();threadParent.AddChild(threadHost);threadHost.Configure(adapter,()=>clock);
        using(var e=Button(center)){await Task.Run(()=>{try{threadHost.Dispatch(e);throw new Exception("Off-thread dispatch accepted");}catch(InvalidOperationException){}try{threadHost.Close();throw new Exception("Off-thread close accepted");}catch(InvalidOperationException){}});}Check(!threadHost.IsClosed&&threadHost.ClockReadCount==0,"Off-thread operations fail before effects.");threadHost.Close();threadParent.Free();
        var staleParent=new Node();AddChild(staleParent);var staleHost=new TerrainPointTargetInputHost();staleParent.AddChild(staleHost);staleHost.Configure(adapter,()=>clock);world.BeginSelection(43);int staleAttempts=adapter.Attempts;using(var e=Button(center)){Check(!Push(e),"Stale actual world event unhandled.");}
        Check(staleHost.IsFaulted&&staleHost.ClockReadCount==1&&adapter.Attempts==staleAttempts&&picker.Attempts==staleAttempts&&ReferenceEquals(staleHost.LastResult,adapter.LastResult),"Actual child validates stale world before picker effects.");staleHost.Close();staleParent.Free();
        Check(propagation.Count>0&&!propagation.SawHandled,"Ignored/failed events reach actual later unhandled-input observer with clear flag.");propagation.Free();
        adapter.Dispose();picker.Dispose();controller.Dispose();foreach(var visual in visuals)visual.Dispose();Check(picker.OwnedResourceCount==0&&markerParent.GetChildCount()==0&&terrain.GetChildCount()==0,"Borrower owners drain exact resources.");terrain.Free();markerParent.Free();camera.Free();
        File.WriteAllText(output,JsonSerializer.Serialize(new{Gpu=gpu,Samples=samples,Targets=targets,Checks=checks,AdmissionChecks=admissionChecks,ViewportEvents=pushed,HandledEvents=totalHandled,HostBorrowedSurvival=borrowedSurvival,ClockReentryBeforeChild=clockReentry,EventFaultDisabled=eventFault,ExitTreeClosed=exitClosed,NativePrefixPreserved=nativePrefix,OwnedResourcesAfter=0,BorrowedSurvival=true,AttemptsMaximum=256,OriginalPickingParity=false}),new System.Text.UTF8Encoding(false));GD.Print("PASS point input host");
    }
}

/// <summary>Actual later Godot event observer; never consumes input.</summary>
public partial class TerrainPointInputPropagationProbe : Node
{
    public int Count {get;private set;}
    public bool SawHandled {get;private set;}
    public override void _UnhandledInput(InputEvent input){Count++;SawHandled|=GetViewport().IsInputHandled();}
}
