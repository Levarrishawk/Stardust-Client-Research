using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Geometry;
using NVector3=System.Numerics.Vector3;

/// <summary>Own standalone replacement test node; synthetic sources, actual cameras and GPU marker.</summary>
public partial class TerrainCameraPickerVerification : Node3D
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
    public override async void _Ready()
    {try{await Run();GetTree().Quit();}catch(Exception e){GD.PushError("Terrain camera picker: "+e);GetTree().Quit(1);}}
    private async Task Run()
    {
        string[] args=OS.GetCmdlineUserArgs();string Arg(string name){int i=Array.IndexOf(args,name);Need(i>=0,"Explicit own argument "+name);return args[i+1];}
        string output=Path.GetFullPath(Arg("--out")),repository=Path.GetFullPath(Arg("--repository"));Need(output.StartsWith(Path.Combine(repository,"local-private")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)&&!File.Exists(output),"Fresh private receipt.");
        using var doc=JsonDocument.Parse(File.ReadAllText(Arg("--fixtures")));var flat=Make(doc.RootElement.GetProperty("Flat"));var slope=Make(doc.RootElement.GetProperty("Slope"));
        var terrain=new Node3D();AddChild(terrain);var markerParent=new Node3D{Position=new(2,1,3)};AddChild(markerParent);
        var visuals=flat.Source.Children.Select(c=>new TerrainPatchVisual(c.Geometry,terrain)).ToArray();
        var camera=new Camera3D{Current=true,Fov=50,Near=.05f,Far=200,Position=new(4,12,-4)};AddChild(camera);camera.LookAt(new(4,0,-4),Vector3.Back);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        var samples=new List<object>();int checks=0;bool gpu=DisplayServer.GetName()!="headless";int hits=0,misses=0;
        using var picker=new TerrainCameraPicker(camera,markerParent,flat);TerrainCameraPickReceipt? historical=null;
        void Observe(TerrainCameraPicker p,Vector2 pixel,string label,int model)
        {
            var r=p.Pick(pixel,1000);Need(ReferenceEquals(r.Query,p.Query)&&ReferenceEquals(r.Camera,camera)&&r.CameraInstanceId==camera.GetInstanceId()&&p.IsCurrent(r),"Actual receipt identity.");checks++;
            if(r.Hit is { } h){Need(ReferenceEquals(h.Source,p.Query.Source)&&ReferenceEquals(h.Geometry,h.Patch.Geometry)&&ReferenceEquals(h.Child,h.Membership.Query),"Actual hit identity chain.");hits++;}else misses++;
            if(historical is not null&&ReferenceEquals(p,picker))Need(!p.IsCurrent(historical),"Historical receipt stale.");
            samples.Add(new{Label=label,Model=model,Pixel=new[]{pixel.X,pixel.Y},Viewport=new[]{r.ViewportSize.X,r.ViewportSize.Y},EngineOrigin=V(r.EngineOrigin),EngineDirection=V(r.EngineDirection),NativeOrigin=new[]{r.Ray.Origin.X,r.Ray.Origin.Y,r.Ray.Origin.Z},NativeDirection=new[]{r.Ray.Direction.X,r.Ray.Direction.Y,r.Ray.Direction.Z},MaximumT=r.Ray.MaximumT,Projection=camera.Projection.ToString(),camera.Fov,camera.Size,camera.Near,CameraPosition=V(camera.GlobalPosition),Basis=new[]{V(camera.GlobalBasis.X),V(camera.GlobalBasis.Y),V(camera.GlobalBasis.Z)},Hit=Hit(r.Hit),r.Revision,r.Attempt,Marker=p.Marker is { } m?new{Visible=m.Visible,Position=V(m.GlobalPosition),Id=m.GetInstanceId()}:null});
            if(ReferenceEquals(p,picker))historical=r;
        }
        Vector2 viewportSize=GetViewport().GetVisibleRect().Size;Vector2 center=viewportSize/2;GD.Print("picker_viewport="+viewportSize);
        foreach(var mode in new[]{Camera3D.ProjectionType.Perspective,Camera3D.ProjectionType.Orthogonal})
        {
            camera.Projection=mode;camera.Size=8;
            foreach(var point in new[]{center,new Vector2(0,0),new Vector2(viewportSize.X-1,0),new Vector2(0,viewportSize.Y-1),viewportSize-Vector2.One,new Vector2(viewportSize.X*.625f,viewportSize.Y*.4166667f)})Observe(picker,point,mode.ToString(),0);
        }
        camera.Projection=Camera3D.ProjectionType.Frustum;var priorProjection=picker.Current;bool badProjection=false;try{_=picker.Pick(center,1000);}catch(InvalidOperationException){badProjection=true;}Need(badProjection&&!picker.IsCurrent(priorProjection)&&ReferenceEquals(picker.Current,priorProjection),"Unsupported projection rejects/stales safely.");checks++;camera.Projection=Camera3D.ProjectionType.Orthogonal;
        var firstMarker=picker.Marker!.GetInstanceId();Observe(picker,center,"repeat",0);Need(picker.Marker!.GetInstanceId()==firstMarker&&picker.MarkerBuilds==1,"Lazy marker reused.");checks++;
        // Center lies exactly on a shared chunk edge at native X8.
        camera.Position=new(8,12,-4);camera.LookAt(new(8,0,-4),Vector3.Back);camera.Basis=new Basis(Vector3.Left,Vector3.Back,Vector3.Up);Observe(picker,center,"shared-edge",0);Need(picker.SelectedHit?.ChunkIndex==0,"Exact shared-edge retains earlier actual chunk.");checks++;
        camera.Position=new(4,10,6);camera.LookAt(new(4,0,-4));camera.Projection=Camera3D.ProjectionType.Perspective;Observe(picker,center,"oblique",0);
        using(var sloped=new TerrainCameraPicker(camera,markerParent,slope)){Observe(sloped,center,"sloped-oblique",1);Need(sloped.OwnedResourceCount==3,"Slope hit owns marker resources.");checks++;sloped.Dispose();Need(sloped.OwnedResourceCount==0,"Slope cleanup.");checks++;}
        var prior=picker.Current;var oldPosition=picker.Marker!.GlobalPosition;bool oldVisible=picker.Marker.Visible;int oldAttempts=picker.Attempts;
        foreach(var bad in new[]{new Vector2(float.NaN,0),new Vector2(-1,0),new Vector2(viewportSize.X,0),new Vector2(0,viewportSize.Y)})
        {bool rejected=false;try{_=picker.Pick(bad,1000);}catch(ArgumentException){rejected=true;}Need(rejected&&ReferenceEquals(picker.Current,prior)&&picker.Marker.GlobalPosition==oldPosition&&picker.Marker.Visible==oldVisible&&picker.Attempts==oldAttempts,"Invalid pixel preserves selection/marker.");checks++;}
        foreach(double bad in new[]{double.NaN,double.PositiveInfinity,-1,1e9+1})
        {bool rejected=false;try{_=picker.Pick(center,bad);}catch(ArgumentException){rejected=true;}Need(rejected&&ReferenceEquals(picker.Current,prior),"Invalid maximum preserves prior.");checks++;}
        using(var cancelled=new CancellationTokenSource()){cancelled.Cancel();bool rejected=false;try{_=picker.Pick(center,1000,cancelled.Token);}catch(OperationCanceledException){rejected=true;}Need(rejected&&ReferenceEquals(picker.Current,prior)&&picker.Marker.GlobalPosition==oldPosition,"Precancel preserves prior.");checks++;}
        bool offThread=await Task.Run(()=>{try{_=picker.Pick(center,1000);return false;}catch(InvalidOperationException){return true;}});Need(offThread&&ReferenceEquals(picker.Current,prior),"Main-thread gate.");checks++;
        using(var input=new InputEventMouseButton{ButtonIndex=MouseButton.Right,Pressed=true,Position=center})Need(!picker.HandleInput(input,1000,out _)&&picker.Attempts==oldAttempts,"Right ignored.");checks++;
        using(var input=new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=false,Position=center})Need(!picker.HandleInput(input,1000,out _)&&picker.Attempts==oldAttempts,"Released ignored.");checks++;
        using(var input=new InputEventMouseMotion())Need(!picker.HandleInput(input,1000,out _)&&picker.Attempts==oldAttempts,"Motion ignored.");checks++;
        using(var input=new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=true,Position=center})Need(picker.HandleInput(input,1000,out var r)&&ReferenceEquals(r,picker.Current),"Actual left event picks.");checks++;
        camera.Position=new(100,12,-4);camera.LookAt(new(100,0,-4),Vector3.Back);Observe(picker,center,"explicit-miss",0);Need(picker.SelectedHit is null&&!picker.Marker.Visible,"Miss clears/hides.");checks++;
        camera.Position=new(4,12,-4);camera.LookAt(new(4,0,-4),Vector3.Back);camera.Projection=Camera3D.ProjectionType.Orthogonal;
        using(var capped=new TerrainCameraPicker(camera,markerParent,flat,1)){_=capped.Pick(center,1000);var r=capped.Current;bool rejected=false;try{_=capped.Pick(center,1000);}catch(InvalidOperationException){rejected=true;}Need(rejected&&capped.Attempts==1&&ReferenceEquals(capped.Current,r),"Exact/one-less attempts.");checks++;}
        using(var overflow=new TerrainCameraPicker(camera,markerParent,flat,256,ulong.MaxValue)){bool rejected=false;try{_=overflow.Pick(center,1000);}catch(OverflowException){rejected=true;}Need(rejected&&overflow.Attempts==0&&overflow.OwnedResourceCount==0,"Revision overflow before query/allocation.");checks++;}
        using(var maximum=new TerrainCameraPicker(camera,markerParent,flat)){for(int i=0;i<256;i++)_=maximum.Pick(center,1000);bool rejected=false;try{_=maximum.Pick(center,1000);}catch(InvalidOperationException){rejected=true;}Need(rejected&&maximum.Attempts==256&&maximum.MarkerBuilds==1,"256 attempts exact and exhaustion.");checks++;}
        var deadParent=new Node3D();AddChild(deadParent);using(var dead=new TerrainCameraPicker(camera,deadParent,flat)){_=dead.Pick(center,1000);deadParent.Free();bool rejected=false;try{_=dead.Pick(center,1000);}catch(InvalidOperationException){rejected=true;}Need(rejected,"Freed parent rejects.");checks++;dead.Dispose();Need(dead.OwnedResourceCount==0,"Freed parent drains own resources.");checks++;}
        var other=new Camera3D{Current=true,Position=camera.Position};AddChild(other);other.MakeCurrent();Need(!picker.IsCurrent(picker.Current),"Different active camera stales currency.");checks++;camera.MakeCurrent();Need(picker.IsCurrent(picker.Current),"Restoring borrowed active camera restores retained receipt currency.");checks++;other.Free();
        var freedCamera=new Camera3D{Current=true,Position=camera.Position};AddChild(freedCamera);freedCamera.MakeCurrent();freedCamera.Basis=camera.Basis;using(var freed=new TerrainCameraPicker(freedCamera,markerParent,flat)){_=freed.Pick(center,1000);var former=freed.Current;freedCamera.Free();bool rejected=false;try{_=freed.Pick(center,1000);}catch(InvalidOperationException){rejected=true;}Need(rejected&&!freed.IsCurrent(former)&&ReferenceEquals(freed.Current,former),"Freed camera rejects safely and preserves historical receipt.");checks++;}camera.MakeCurrent();
        bool lateCancellation=false;int lateTrials=0;var dense=Make(doc.RootElement.GetProperty("Dense"));Need(dense.ChunkCount==16&&dense.PatchCount==256&&dense.TotalTriangles==2048,"Actual dense cancellation geometry counts.");checks++;
        foreach(int spins in new[]{1000,10000,100000,1000000})
        {
            if(lateCancellation)break;lateTrials++;using var candidate=new TerrainCameraPicker(camera,markerParent,dense);_=candidate.Pick(center,1000);var candidatePrior=candidate.Current;var candidatePosition=candidate.Marker!.GlobalPosition;using var cancellation=new CancellationTokenSource();using var ready=new ManualResetEventSlim();using var go=new ManualResetEventSlim();
            var stop=Task.Run(()=>{ready.Set();go.Wait();Thread.SpinWait(spins);cancellation.Cancel();});ready.Wait();go.Set();bool rejected=false;
            try{_=candidate.Pick(center,1000,cancellation.Token);}catch(OperationCanceledException){rejected=true;}await stop;
            if(rejected&&candidate.Attempts==2){Need(ReferenceEquals(candidate.Current,candidatePrior)&&candidate.Marker!.GlobalPosition==candidatePosition&&candidate.Marker.Visible&&candidate.OwnedResourceCount==3,"Late charged cancellation preserves prior marker/selection.");lateCancellation=true;checks++;}
        }
        Need(lateCancellation,"At least one cancellation observed after actual query-attempt charge.");
        using(var reentrant=new TerrainCameraPicker(camera,markerParent,flat))
        {
            int nestedRejected=0;int beforeChildren=markerParent.GetChildCount();
            void Reenter(Node entered)
            {
                if(entered is not MeshInstance3D)return;
                try{_=reentrant.Pick(center,1000);}catch(InvalidOperationException){nestedRejected++;}
                try{reentrant.Dispose();}catch(InvalidOperationException){nestedRejected++;}
            }
            markerParent.ChildEnteredTree+=Reenter;bool rejected=false;
            try{_=reentrant.Pick(center,1000);}catch(InvalidOperationException){rejected=true;}finally{markerParent.ChildEnteredTree-=Reenter;}
            Need(rejected&&nestedRejected==2&&reentrant.Current is null&&reentrant.MarkerBuilds==0&&reentrant.OwnedResourceCount==0&&markerParent.GetChildCount()==beforeChildren,"Real AddChild callbacks reject nested mutations, outer publication and drain staged resources.");checks++;
        }
        Observe(picker,center,"final-visible",0);
        if(gpu){await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=GetViewport().GetTexture().GetImage();var pixel=image.GetPixel((int)center.X,(int)center.Y);Need(pixel.R>.8f&&pixel.B>.8f&&pixel.G<.2f,"GPU magenta marker at actual selected center.");checks++;Need(image.SavePng(Path.ChangeExtension(output,".png"))==Error.Ok,"Own GPU capture.");}
        var receipt=picker.Current;picker.Dispose();picker.Dispose();Need(picker.OwnedResourceCount==0&&picker.Current is null&&!picker.IsCurrent(receipt!)&&GodotObject.IsInstanceValid(camera)&&GodotObject.IsInstanceValid(markerParent)&&camera.IsInsideTree()&&markerParent.IsInsideTree(),"Exact owned cleanup and borrowed survival.");checks++;
        foreach(var visual in visuals)visual.Dispose();Need(terrain.GetChildCount()==0&&markerParent.GetChildCount()==0,"All owned children detached.");checks++;terrain.Free();markerParent.Free();camera.Free();
        File.WriteAllText(output,JsonSerializer.Serialize(new{Gpu=gpu,Samples=samples,Checks=checks,Hits=hits,Misses=misses,AttemptsMaximum=256,OwnedResourcesAfter=0,BorrowedSurvival=true,OriginalPickingParity=false,LateCancellation=lateCancellation,LateCancellationTrials=lateTrials}),new System.Text.UTF8Encoding(false));
        GD.Print("PASS terrain camera picker owned verification");
    }
}
