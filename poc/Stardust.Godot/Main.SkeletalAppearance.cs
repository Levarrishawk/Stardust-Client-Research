using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;

public partial class Main
{
    private SkeletalMeshPreview? skeletalPreview;
    private OwnedBackgroundAssetLoad<CreatureAppearanceDescriptorBundle>? skeletalLoad;
    private void DisposeSkeletalPreview(){skeletalLoad?.Dispose();skeletalLoad=null;skeletalPreview?.Dispose();skeletalPreview=null;}
    private async Task BuildSkeletalPreview(string[] args)
    {
        string Read(string key){int i=Array.IndexOf(args,key);if(i<0||i+1>=args.Length||args.Count(v=>v==key)!=1)throw new ArgumentException("Missing/duplicate skeletal inspector option.");return args[i+1];}
        string root=Read("--asset-root"),configuration=Read("--asset-config"),member=Read("--asset-member"),pin=Read("--asset-sha256"),appearancePin=Read("--creature-appearance-sha256");
        bool cancel=args.Contains("--cancel-creature-load");if(cancel&&DisplayServer.GetName()!="headless")throw new ArgumentException("Cancellation fixture requires headless mode.");
        var layer=new CanvasLayer();AddChild(layer);var panel=new VBoxContainer{Position=new(24,24)};layer.AddChild(panel);panel.AddChild(new Label{Text="Loading character geometry…"});var button=new Button{Text="Cancel loading"};panel.AddChild(button);
        int mainThread=System.Environment.CurrentManagedThreadId,workerThread=mainThread,closed=0;
        var first=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var load=new OwnedBackgroundAssetLoad<CreatureAppearanceDescriptorBundle>(token=>
        {
            workerThread=System.Environment.CurrentManagedThreadId;
            try
            {
                token.ThrowIfCancellationRequested();using var config=File.OpenText(configuration);using var overlay=ConfiguredTreOverlay.Open(root,config);
                TreLookupResult Resolve(string path)
                {
                    token.ThrowIfCancellationRequested();
                    if(cancel&&!first.Task.IsCompleted){first.TrySetResult(true);token.WaitHandle.WaitOne(TimeSpan.FromSeconds(15));token.ThrowIfCancellationRequested();throw new InvalidOperationException("Character cancellation fixture timed out.");}
                    return overlay.Lookup(path);
                }
                var result=CreatureAppearanceDescriptorBundleReader.Load(Resolve,member,pin,appearancePin,loadSkeletons:true,loadSkinSources:true,loadGeometry:true);token.ThrowIfCancellationRequested();return result;
            }
            finally{Volatile.Write(ref closed,1);}
        });
        skeletalLoad=load;button.Pressed+=()=>{load.Cancel();button.Disabled=true;};CreatureAppearanceDescriptorBundle bundle;
        try
        {
            var transfer=load.TakeAsync();if(cancel&&await Task.WhenAny(first.Task,load.WorkerCompletion)==first.Task)button.EmitSignal(Button.SignalName.Pressed);
            bundle=await transfer;if(!GodotObject.IsInstanceValid(this)||!IsInsideTree())return;
            if(load.IsCancellationRequested)throw new OperationCanceledException();
            if(System.Environment.CurrentManagedThreadId!=mainThread||workerThread==mainThread||Volatile.Read(ref closed)!=1)throw new AssetPreviewException("Character worker ownership failed.");
            GD.Print("PASS: character CPU worker; main_thread_delivery=true archive_scope_closed=true");
        }
        catch(OperationCanceledException)
        {
            if(GodotObject.IsInstanceValid(this)&&IsInsideTree())
            {
                if(cancel&&(Volatile.Read(ref closed)!=1||skeletalPreview is not null))throw new AssetPreviewException("Canceled character published geometry.");
                GD.Print(cancel?"PASS: canceled character worker; pending first lookup, archive cleanup and zero GPU publication":"Character loading canceled.");GetTree().Quit();
            }
            return;
        }
        finally{if(GodotObject.IsInstanceValid(button))button.Disabled=true;load.Dispose();if(ReferenceEquals(skeletalLoad,load))skeletalLoad=null;if(GodotObject.IsInstanceValid(layer))layer.QueueFree();}
        skeletalPreview=new(bundle);AddChild(skeletalPreview.Root);skeletalPreview.VerifyBoundary(DisplayServer.GetName()!="headless");
        GD.Print($"PASS: original neutral character geometry sources={bundle.Sources.Count} source_bytes={bundle.Sources.Values.Sum(s=>(long)s.Bytes)} surfaces={skeletalPreview.Surfaces} vertices={skeletalPreview.Vertices} triangles={skeletalPreview.Triangles} bounds={skeletalPreview.Bounds} generated_face_texture=false animation=false");
        bool fixture=args.Contains("--asset-self-test")||args.Contains("--capture");bool posed=args.Contains("--creature-pose");
        if(fixture)
        {
            var bindBounds=skeletalPreview.Bounds;
            for(int repeat=0;repeat<4;repeat++)
            {
                skeletalPreview.SetDiagnosticPose(true);skeletalPreview.VerifyBoundary(DisplayServer.GetName()!="headless");
                if(skeletalPreview.MaximumDisplacement<.05f)throw new AssetPreviewException("Diagnostic arm pose did not move original vertices.");
                skeletalPreview.SetDiagnosticPose(false);skeletalPreview.VerifyBoundary(DisplayServer.GetName()!="headless");
                if(skeletalPreview.MaximumDisplacement>2e-5f)throw new AssetPreviewException("Diagnostic reset did not preserve original positions.");
            }
            if(skeletalPreview.Bounds.Position.DistanceTo(bindBounds.Position)>2e-5f||skeletalPreview.Bounds.Size.DistanceTo(bindBounds.Size)>2e-5f)throw new AssetPreviewException("Diagnostic bind reset bounds mismatch.");
            GD.Print("PASS: original weighted diagnostic pose and bind reset; switches=8 complete position/normal upload animation=false");
        }
        if(posed){skeletalPreview.SetDiagnosticPose(true);skeletalPreview.VerifyBoundary(DisplayServer.GetName()!="headless");}
        if(fixture&&DisplayServer.GetName()=="headless"){GetTree().Quit();return;}
        AddChild(new WorldEnvironment{Environment=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new Color("#14202e"),AmbientLightSource=Godot.Environment.AmbientSource.Color,AmbientLightColor=Colors.White,AmbientLightEnergy=.65f}});
        AddChild(new DirectionalLight3D{RotationDegrees=new(-35,25,0),LightEnergy=.8f});
        var camera=new PreviewOrbitCamera{Current=true,Fov=50};AddChild(camera);camera.Configure(skeletalPreview.Bounds.GetCenter(),Math.Max(skeletalPreview.Bounds.Size.Length()*.6f,1));
        var info=new CanvasLayer();AddChild(info);var header=new ColorRect{Color=new(.02f,.04f,.08f,.92f),OffsetRight=1280,OffsetBottom=110};info.AddChild(header);
        header.AddChild(new Label{Text="STARDUST — ORIGINAL CHARACTER GEOMETRY",Position=new(24,18)});
        var poseHint=new Label{Text=(posed?"Neutral test arm pose":"Neutral bind pose")+" · drag to orbit · scroll to zoom · R to reset",Position=new(24,48)};header.AddChild(poseHint);
        header.AddChild(new Label{Text="Character textures, customization and animation are pending",Position=new(24,76)});
        var poseToggle=new CheckBox{Text="Test arm pose",Position=new(760,48),ButtonPressed=posed};header.AddChild(poseToggle);poseToggle.Toggled+=enabled=>{skeletalPreview.SetDiagnosticPose(enabled);poseHint.Text=(enabled?"Neutral test arm pose":"Neutral bind pose")+" · drag to orbit · scroll to zoom · R to reset";};
        if(fixture)
        {
            await StaticMeshPreview.VerifyGpuUvBoundary(this);
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            using var image=GetViewport().GetTexture().GetImage();if(args.Contains("--capture")&&image.SavePng(Read("--capture"))!=Error.Ok)throw new IOException("Character capture failed.");
            GD.Print("PASS: original neutral character GPU upload and capture boundary");GetTree().Quit();
        }
    }
}
