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
    private Action<double>? skeletalAnimationUpdate;
    private Action<bool>? skeletalAnimationFocus;
    private void DisposeSkeletalPreview(){skeletalAnimationUpdate=null;skeletalAnimationFocus=null;skeletalLoad?.Dispose();skeletalLoad=null;skeletalPreview?.Dispose();skeletalPreview=null;}
    private async Task BuildSkeletalPreview(string[] args)
    {
        string Read(string key){int i=Array.IndexOf(args,key);if(i<0||i+1>=args.Length||args.Count(v=>v==key)!=1)throw new ArgumentException("Missing/duplicate skeletal inspector option.");return args[i+1];}
        string root=Read("--asset-root"),configuration=Read("--asset-config"),member=Read("--asset-member"),pin=Read("--asset-sha256"),appearancePin=Read("--creature-appearance-sha256");
        int meshDetail=0;
        if(args.Contains("--creature-mesh-detail")&&(!int.TryParse(Read("--creature-mesh-detail"),System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out meshDetail)||meshDetail<0||meshDetail>3))throw new ArgumentException("Creature mesh detail must be an explicit integer 0..3.");
        bool vertexLitOnly=args.Contains("--creature-vertex-lit-only");
        bool materialDefaults=args.Contains("--creature-material-defaults");
        bool paletteDefaults=args.Contains("--creature-palette-defaults");
        bool albedoFactors=args.Contains("--creature-albedo-factors");
        bool normalDiffuse=args.Contains("--creature-normal-diffuse");if(normalDiffuse&&!albedoFactors)throw new ArgumentException("Normal diffuse requires owned albedo factors.");
        if(albedoFactors&&!paletteDefaults)throw new ArgumentException("Albedo factor preview requires owned palette defaults.");
        string? textureProfile=args.Contains("--creature-texture-profile")?Read("--creature-texture-profile"):null;
        if(textureProfile is not null&&!normalDiffuse)throw new ArgumentException("Texture profile requires normal diffuse preview.");
        bool specular=args.Contains("--creature-specular");if(specular&&!normalDiffuse)throw new ArgumentException("Specular preview requires normal diffuse resources.");
        bool customized=args.Contains("--creature-customization-profile");
        string? shapeProfile=args.Contains("--creature-shape-profile")?Read("--creature-shape-profile"):null;
        if(customized&&(!albedoFactors||args.Contains("--creature-face-profile")))throw new ArgumentException("Shared customization requires albedo factors and one profile.");
        bool generatedFace=args.Contains("--creature-face-profile")||customized;
        if(generatedFace&&!materialDefaults)throw new ArgumentException("Generated face requires owned material defaults.");
        string? faceProfile=generatedFace?Read(customized?"--creature-customization-profile":"--creature-face-profile"):null;
        if(paletteDefaults&&!materialDefaults)throw new ArgumentException("Palette inspection requires owned material defaults.");
        if(meshDetail>0&&(generatedFace||shapeProfile is not null))throw new ArgumentException("Lower mesh details do not support generated face/customization or shape profiles in this bounded preview.");
        if(meshDetail>=2&&(normalDiffuse||specular||textureProfile is not null))throw new ArgumentException("Mesh details 2/3 have no compressed-normal/tangent family; select CreatureVertexLitOnly without normal/specular/texture-choice options.");
        if(vertexLitOnly&&(meshDetail<2||specular||normalDiffuse||!albedoFactors||generatedFace||shapeProfile is not null||textureProfile is not null))throw new ArgumentException("Vertex-lit-only requires detail 2/3, owned material/palette/albedo and no normal/shape/face/texture-choice options.");
        bool originalAnimation=args.Contains("--creature-animation-frame");
        double selectedFrame=originalAnimation?double.Parse(Read("--creature-animation-frame"),System.Globalization.CultureInfo.InvariantCulture):0;
        if(!double.IsFinite(selectedFrame)||selectedFrame<0||selectedFrame>32767)throw new ArgumentException("Invalid selected animation frame.");
        var selection=originalAnimation?new LogicalAnimationClipSelection(Read("--creature-animation-table"),Read("--creature-animation-name"),Read("--creature-animation-sha256")):null;
        bool playAnimation=args.Contains("--creature-animation-play");if(playAnimation&&!originalAnimation)throw new ArgumentException("Animation playback requires explicit selected clip/frame.");
        bool cancel=args.Contains("--cancel-creature-load");if(cancel&&DisplayServer.GetName()!="headless")throw new ArgumentException("Cancellation fixture requires headless mode.");
        var layer=new CanvasLayer();AddChild(layer);var panel=new VBoxContainer{Position=new(24,24)};layer.AddChild(panel);panel.AddChild(new Label{Text="Loading character geometry…"});var button=new Button{Text="Cancel loading"};panel.AddChild(button);
        int mainThread=System.Environment.CurrentManagedThreadId,workerThread=mainThread,closed=0;
        var first=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sourceReads=new System.Collections.Generic.Dictionary<string,int>(StringComparer.Ordinal);
        var load=new OwnedBackgroundAssetLoad<CreatureAppearanceDescriptorBundle>(token=>
        {
            workerThread=System.Environment.CurrentManagedThreadId;
            try
            {
                token.ThrowIfCancellationRequested();using var config=File.OpenText(configuration);using var overlay=ConfiguredTreOverlay.Open(root,config);
                TreLookupResult Resolve(string path)
                {
                    sourceReads[path]=sourceReads.TryGetValue(path,out int readCount)?readCount+1:1;
                    token.ThrowIfCancellationRequested();
                    if(cancel&&!first.Task.IsCompleted){first.TrySetResult(true);token.WaitHandle.WaitOne(TimeSpan.FromSeconds(15));token.ThrowIfCancellationRequested();throw new InvalidOperationException("Character cancellation fixture timed out.");}
                    return overlay.Lookup(path);
                }
                var result=CreatureAppearanceDescriptorBundleReader.Load(Resolve,member,pin,appearancePin,meshDetail:meshDetail,limits:generatedFace?new(MaximumResources:128,MaximumSourceBytes:32*1024*1024):null,loadSkeletons:true,loadSkinSources:true,loadGeometry:true,loadLogicalTables:originalAnimation,animationSelection:selection,prepareSelectedAnimation:originalAnimation,loadMaterialDefaults:materialDefaults,loadPaletteDefaults:paletteDefaults,loadFaceBlueprints:generatedFace,loadFaceResources:generatedFace,loadFacePixelPrograms:generatedFace,loadAlbedoFactors:albedoFactors,loadBlendShapes:shapeProfile is not null,loadTextureChoices:normalDiffuse,loadCompressedNormals:normalDiffuse,loadVertexLitSpecular:specular||vertexLitOnly);
                if(shapeProfile is not null){using var shapeStream=File.OpenRead(shapeProfile);result=SkeletalAppearanceShapes.Apply(result,SkeletalAppearanceShapes.LoadProfile(shapeStream),token);}
                if(generatedFace)
                {
                    using var profile=File.OpenRead(faceProfile!);var values=BlueprintFaceCustomizationProfile.Load(profile);
                    result=customized?SkeletalAppearanceCustomization.Apply(result,values.ToDictionary(p=>new ShaderCustomizationKey(p.Key.VariableName,p.Key.FactoryFlag),p=>p.Value),token):
                        result with{GeneratedFaces=BlueprintGeneratedFaceBuilder.Create(result.Geometries!,result.FaceBlueprints!,result.FaceResources!,values,token)};
                }
                if(textureProfile is not null){using var textureStream=File.OpenRead(textureProfile);result=result with{MaterialBundle=SkeletalTextureChoices.Apply(result.MaterialBundle!,SkeletalAppearanceCustomization.LoadProfile(textureStream),token)};}
                if(originalAnimation&&selectedFrame>result.PreparedAnimation!.EndFrame)throw new InvalidDataException("Selected animation frame outside clip.");token.ThrowIfCancellationRequested();return result;
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
        if(customized)GD.Print($"PASS: explicit shared character customization keys={bundle.CustomizationValues!.Count} asset_bytes_unchanged=true");
        if(paletteDefaults)GD.Print($"PASS: owned character authored palette factors={bundle.MaterialBundle!.PaletteDefaults!.Values.Sum(p=>p.AuthoredFactors.Count)} missing={bundle.MaterialBundle.PaletteDefaults.Values.Sum(p=>p.MissingPalettePaths.Count)} albedo_translated={albedoFactors}");
        if(sourceReads.Values.Any(n=>n!=1))throw new AssetPreviewException("Selected character sources were read more than once.");
        skeletalPreview=new(bundle,specularPreview:specular,meshDetail:meshDetail,vertexLitOnly:vertexLitOnly);AddChild(skeletalPreview.Root);skeletalPreview.VerifyBoundary(DisplayServer.GetName()!="headless");
        GD.Print($"PASS: original character geometry texture_defaults={materialDefaults} sources={bundle.Sources.Count} source_bytes={bundle.Sources.Values.Sum(s=>(long)s.Bytes)} surfaces={skeletalPreview.Surfaces} vertices={skeletalPreview.Vertices} triangles={skeletalPreview.Triangles} bounds={skeletalPreview.Bounds} generated_face_texture={generatedFace} animation=false");
        if(generatedFace)foreach(var face in bundle.GeneratedFaces!.Outputs)
            GD.Print($"PASS: generated face MAIN renderer={face.Key} bindings={bundle.GeneratedFaces.Bindings.Count} policy={BlueprintComposedFace.Policy} rgba_sha256={Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(face.Value.Image.Mips[0].Rgba.Span))} gpu_readback={DisplayServer.GetName()!="headless"}");
        if(meshDetail>0)
        {
            foreach(var source in bundle.Sources.Values)GD.Print("skeletal_detail_source="+System.Text.Json.JsonSerializer.Serialize(source));
            foreach(var path in bundle.Geometries!.Keys)GD.Print($"PASS: selected mesh_detail={meshDetail} mesh={path} sha256={bundle.Sources[path].Sha256} skeleton_detail=0 vertex_lit_only={vertexLitOnly}");
        }
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
        if(originalAnimation)
        {
            var prepared=bundle.PreparedAnimation!;
            if(fixture)
            {
                foreach(double frame in new[]{0d,prepared.EndFrame*.25,prepared.EndFrame*.5,prepared.EndFrame*.75,(double)prepared.EndFrame,0d})
                {skeletalPreview.SetOriginalFrame(frame);skeletalPreview.VerifyBoundary(DisplayServer.GetName()!="headless");}
                skeletalPreview.SetDiagnosticPose(false);skeletalPreview.VerifyBoundary(DisplayServer.GetName()!="headless");
                if(skeletalPreview.MaximumDisplacement>2e-5)throw new AssetPreviewException("Original sampled pose bind reset mismatch.");
                var clock=new AnimationPreviewClock(prepared.FrameRate,prepared.EndFrame,prepared.ProxyTimeScale);clock.SetPlaying(true);
                var watch=System.Diagnostics.Stopwatch.StartNew();
                for(int tick=0;tick<480;tick++)
                {
                    clock.Advance(prepared.DurationSeconds/(prepared.ProxyTimeScale*240));skeletalPreview.SetOriginalFrame(clock.Frame);
                    if(tick%60==0)skeletalPreview.VerifyBoundary(DisplayServer.GetName()!="headless");
                }
                watch.Stop();if(clock.CompletedLoops!=2||clock.Frame!=0)throw new AssetPreviewException("Original preview timed loop mismatch.");
                double held=clock.Frame;clock.SetFocused(false);clock.Advance(10);if(clock.Frame!=held)throw new AssetPreviewException("Unfocused original preview advanced.");
                clock.SetFocused(true);clock.SetPlaying(false);clock.Advance(1);if(clock.Frame!=held)throw new AssetPreviewException("Paused original preview advanced.");
                foreach(double frame in new[]{(double)prepared.EndFrame,prepared.EndFrame*.125,0d}){clock.Seek(frame);skeletalPreview.SetOriginalFrame(clock.Frame);skeletalPreview.VerifyBoundary(DisplayServer.GetName()!="headless");}
                GD.Print($"PASS: original idle preview clock ticks=480 loops=2 focus_pause_seek=true full_pose_upload=true mean_ms={watch.Elapsed.TotalMilliseconds/480:0.###}");
            }
            skeletalPreview.SetOriginalFrame(selectedFrame);skeletalPreview.VerifyBoundary(DisplayServer.GetName()!="headless");
            GD.Print($"PASS: original sampled idle pose sources={bundle.Sources.Count} frame={selectedFrame} unused={string.Join(',',prepared.UnusedAnimationTransforms)} missing_bind={string.Join(',',prepared.MissingSkeletonTransforms)} repairs={prepared.RepairedPackedValues} timed_playback=false");
        }
        if(fixture&&DisplayServer.GetName()=="headless"){GetTree().Quit();return;}
        AddChild(new WorldEnvironment{Environment=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new Color("#14202e"),AmbientLightSource=Godot.Environment.AmbientSource.Color,AmbientLightColor=Colors.White,AmbientLightEnergy=.65f}});
        AddChild(new DirectionalLight3D{RotationDegrees=new(-35,25,0),LightEnergy=.8f});
        var camera=new PreviewOrbitCamera{Current=true,Fov=50};AddChild(camera);camera.Configure(skeletalPreview.Bounds.GetCenter(),Math.Max(skeletalPreview.Bounds.Size.Length()*(originalAnimation?.72f:.6f),1));
        var info=new CanvasLayer();AddChild(info);var header=new ColorRect{Color=new(.02f,.04f,.08f,.92f),OffsetRight=1280,OffsetBottom=110};info.AddChild(header);
        header.AddChild(new Label{Text=meshDetail==0?"STARDUST | ORIGINAL CHARACTER GEOMETRY":$"STARDUST | ORIGINAL CHARACTER | DETAIL {meshDetail} | {skeletalPreview.Surfaces} surfaces / {skeletalPreview.Vertices} vertices / {skeletalPreview.Triangles} triangles",Position=new(24,18)});
        var poseHint=new Label{Text=(originalAnimation?$"Original idle pose | frame {selectedFrame:0.##}":posed?"Neutral test arm pose":"Neutral bind pose")+" | drag to orbit | scroll to zoom | R to reset",Position=new(24,48)};header.AddChild(poseHint);
        header.AddChild(new Label{Text=vertexLitOnly?"Vertex-lit material | diagnostic lighting":specular?"Normal and specular preview | diagnostic lighting":normalDiffuse?"Normal mapped diffuse | diagnostic lighting":customized?"Selected skin and eye colors | generated face preview":albedoFactors?(generatedFace?"Authored skin and eye colors | generated face preview":"Authored skin and eye colors | face generation pending"):generatedFace?"Authored MAIN textures | generated face from explicit preview profile":materialDefaults?"Authored MAIN textures | palette shading and generated face pending":originalAnimation?"Neutral materials | textures and customization pending":"Character textures, customization and animation are pending",Position=new(24,76)});
        if(!originalAnimation)
        {
            var poseToggle=new CheckBox{Text="Test arm pose",Position=new(760,48),ButtonPressed=posed};header.AddChild(poseToggle);poseToggle.Toggled+=enabled=>{skeletalPreview.SetDiagnosticPose(enabled);poseHint.Text=(enabled?"Neutral test arm pose":"Neutral bind pose")+" | drag to orbit | scroll to zoom | R to reset";};
        }
        else
        {
            var prepared=bundle.PreparedAnimation!;var clock=new AnimationPreviewClock(prepared.FrameRate,prepared.EndFrame,prepared.ProxyTimeScale);clock.Seek(selectedFrame);clock.SetPlaying(playAnimation);clock.SetFocused(GetWindow().HasFocus());
            var play=new CheckBox{Text="Play idle",Position=new(760,45),ButtonPressed=playAnimation};header.AddChild(play);
            var scrub=new HSlider{Position=new(860,50),Size=new(360,24),MinValue=0,MaxValue=prepared.EndFrame,Step=.25,Value=selectedFrame};header.AddChild(scrub);
            play.Toggled+=clock.SetPlaying;
            scrub.ValueChanged+=frame=>{clock.Seek(frame);skeletalPreview.SetOriginalFrame(frame);poseHint.Text=$"Original idle | frame {frame:0.##} | drag to orbit | scroll to zoom | R to reset";};
            skeletalAnimationFocus=clock.SetFocused;
            skeletalAnimationUpdate=elapsed=>
            {
                if(!double.IsFinite(elapsed)||elapsed<0)throw new AssetPreviewException("Invalid preview frame elapsed time.");
                // Discard excess wall-clock stall time; inspection resumes smoothly without catch-up.
                if(!clock.Advance(Math.Min(elapsed,.25)))return;
                skeletalPreview.SetOriginalFrame(clock.Frame);scrub.SetValueNoSignal(clock.Frame);
                poseHint.Text=$"Original idle | frame {clock.Frame:0.##} | drag to orbit | scroll to zoom | R to reset";
            };
            if(fixture)
            {
                skeletalAnimationFocus(true);scrub.EmitSignal(Godot.Range.SignalName.ValueChanged,0d);play.EmitSignal(BaseButton.SignalName.Toggled,true);
                skeletalAnimationUpdate(1d/prepared.FrameRate);double firstFrame=clock.Frame;
                if(firstFrame<=0)throw new AssetPreviewException("Play control did not advance selected clip.");
                skeletalAnimationFocus(false);skeletalAnimationUpdate(.2);if(clock.Frame!=firstFrame)throw new AssetPreviewException("Focus pause control failed.");
                skeletalAnimationFocus(true);play.EmitSignal(BaseButton.SignalName.Toggled,false);skeletalAnimationUpdate(.2);if(clock.Frame!=firstFrame)throw new AssetPreviewException("Play pause control failed.");
                scrub.EmitSignal(Godot.Range.SignalName.ValueChanged,(double)prepared.EndFrame);if(clock.Frame!=prepared.EndFrame)throw new AssetPreviewException("Scrub endpoint control failed.");
                skeletalPreview.VerifyBoundary(true);
                scrub.EmitSignal(Godot.Range.SignalName.ValueChanged,selectedFrame);scrub.SetValueNoSignal(selectedFrame);play.ButtonPressed=playAnimation;
                skeletalAnimationUpdate=null;skeletalAnimationFocus=null;
                GD.Print("PASS: original idle play/scrub/focus control signals and GPU upload; restored selected frame");
            }
        }
        TabContainer? appearanceTabs=null;
        if(customized||shapeProfile is not null||textureProfile is not null){appearanceTabs=new TabContainer{Position=new(900,130),Size=new(350,510)};info.AddChild(appearanceTabs);}
        VBoxContainer EditPanel(string name)
        {
            var scroll=new ScrollContainer{Name=name};appearanceTabs!.AddChild(scroll);var content=new VBoxContainer{SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};scroll.AddChild(content);return content;
        }
        if(customized)
        {
            var customizePanel=EditPanel("Colors");
            var ui=InstallCustomizationControls(customizePanel,bundle,skeletalPreview,fixture);
            if(fixture)await VerifyCustomizationControls(skeletalPreview,bundle,ui);
        }
        if(shapeProfile is not null){var ui=InstallShapeControls(EditPanel("Shape"),bundle,skeletalPreview,fixture);if(fixture){VerifyShapeControls(skeletalPreview,bundle,ui);appearanceTabs!.CurrentTab=appearanceTabs.GetTabCount()-1;}}
        if(textureProfile is not null){var ui=InstallTextureChoiceControls(EditPanel("Details"),bundle,skeletalPreview,fixture);if(fixture){await VerifyTextureChoiceControls(skeletalPreview,bundle,ui);appearanceTabs!.CurrentTab=appearanceTabs.GetTabCount()-1;}}
        if(fixture)
        {
            await StaticMeshPreview.VerifyGpuUvBoundary(this);
            if(albedoFactors)await SkeletalAlbedoPreview.VerifyGpuBoundary(this);
            if(normalDiffuse)await SkeletalNormalDiffusePreview.VerifyGpuBoundary(this);
            if(specular){await SkeletalSpecularPreview.VerifyGpuBoundary(this,vertexVaryings:true);await SkeletalSpecularPreview.VerifyGpuBoundary(this,true);await StaticDoorVaryingGpuScenarios.Verify(this,true);if(args.Contains("--point-color-gpu-probe"))await StaticDoorVaryingGpuScenarios.Verify(this,true,true);}
            if(vertexLitOnly)await SkeletalMaterialPreview.VerifyVertexOnlyGpuBoundary(this);
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            using var image=GetViewport().GetTexture().GetImage();if(args.Contains("--capture")&&image.SavePng(Read("--capture"))!=Error.Ok)throw new IOException("Character capture failed.");
            GD.Print("PASS: original character GPU upload and capture boundary");GetTree().Quit();
        }
    }
}
