using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;
using NVector = System.Numerics.Vector3;

public partial class Main
{
    private InteriorDoorActorScene? interiorScene;
    private FixedStepActorHost<InteriorDoorActorSceneState>? interiorSceneHost;
    private PortalActorInputTimeline<InteriorDoorActorSceneState>? interiorSceneTimeline;
    private readonly List<Node3D> interiorSceneLeaves = new();
    private GeometryAppearancePreview? interiorSceneVisuals;
    private StaticRoomLightingPreview? interiorRoomLighting;
    private StaticDoorLightingPreview? interiorDoorLighting;
    private SkeletalMeshPreview? interiorSceneCharacter;
    private OwnedBackgroundAssetLoad<InteriorSceneContentBundle>? interiorSceneLoad;
    private void DisposeInteriorScene()
    {
        interiorSceneLoad?.Dispose();interiorSceneLoad=null;
        if(actorMouseInput is not null){Input.MouseMode=Input.MouseModeEnum.Visible;actorMouseInput=null;}
        interiorSceneTimeline=null;interiorSceneHost=null;interiorScene?.Dispose();interiorScene=null;
        interiorDoorLighting?.Dispose();interiorDoorLighting=null;
        interiorRoomLighting?.Dispose();interiorRoomLighting=null;
        interiorSceneLeaves.Clear();interiorSceneVisuals?.Dispose();interiorSceneVisuals=null;
        interiorSceneCharacter?.Dispose();interiorSceneCharacter=null;
    }
    private async System.Threading.Tasks.Task BuildInteriorScenePreview(string[] args)
    {
        string Read(string key)
        {
            int i=Array.IndexOf(args,key);if(i<0||i+1>=args.Length||args.Count(v=>v==key)!=1)throw new ArgumentException("Missing/duplicate scene option.");return args[i+1];
        }
        string specPath=Read("--interior-scene-spec");if(new FileInfo(specPath).Length>16384)throw new AssetPreviewException("Scene specification byte budget.");
        InteriorDoorSceneBundle bundle;
        PortalSceneBundle? rooms=null;
        TexturedAppearanceGraph? roomMaterials=null;
        InteriorSceneContentBundle content;
        string rootPath=Read("--asset-root"),configurationPath=Read("--asset-config"),member=Read("--asset-member"),layoutPin=Read("--asset-sha256"),specPin=Read("--interior-scene-sha256"),tablePin=Read("--door-table-sha256");
        byte[] specification=File.ReadAllBytes(specPath);bool loadRooms=args.Contains("--scene-geometry"),loadMaterials=args.Contains("--scene-textures"),cancelTest=args.Contains("--cancel-scene-load");
        bool profileRequested=args.Contains("--actor-template");
        if(profileRequested!=args.Contains("--actor-template-sha256"))throw new ArgumentException("Actor template and pin are required together.");
        string? actorTemplate=profileRequested?Read("--actor-template"):null,actorTemplatePin=profileRequested?Read("--actor-template-sha256"):null;
        bool triangleGuardRequested=args.Contains("--scene-triangle-guard");
        bool characterRequested=args.Contains("--scene-character");
        if(characterRequested!=args.Contains("--scene-character-appearance-sha256")||characterRequested&&(!profileRequested||!loadRooms))throw new ArgumentException("Scene character requires actor profile, rooms and appearance pin.");
        string? characterPin=characterRequested?Read("--scene-character-appearance-sha256"):null;
        string? characterShapeProfile=args.Contains("--scene-character-shape-profile")?Read("--scene-character-shape-profile"):null;
        if(characterShapeProfile is not null&&!characterRequested)throw new ArgumentException("Scene shapes require owned character geometry.");
        bool characterPaletteDefaults=args.Contains("--scene-character-palette-defaults");
        bool characterMaterialDefaults=args.Contains("--scene-character-material-defaults");
        bool characterCustomized=args.Contains("--scene-character-customization-profile");
        bool characterAlbedoFactors=args.Contains("--scene-character-albedo-factors"),characterFaceRequested=args.Contains("--scene-character-face-profile")||characterCustomized;
        bool doorLightingRequested=args.Contains("--scene-door-lighting");
        bool roomLightingRequested=args.Contains("--scene-room-lighting");
        bool cellLightingRequested=args.Contains("--scene-cell-lighting");
        bool useRecoveredLightSelection=cellLightingRequested;
        bool characterSpecular=args.Contains("--scene-character-specular");
        bool characterNormalDiffuse=args.Contains("--scene-character-normal-diffuse");
        string? characterTextureProfile=args.Contains("--scene-character-texture-profile")?Read("--scene-character-texture-profile"):null;
        if(args.Contains("--scene-legacy-light-selection")&&!cellLightingRequested)throw new ArgumentException("Legacy light selection requires owned cell lights.");
        if(doorLightingRequested&&(!cellLightingRequested||!loadMaterials))throw new ArgumentException("Door lighting requires owned cell lights and materials.");
        if(roomLightingRequested&&!cellLightingRequested)throw new ArgumentException("Room lighting requires owned cell-lighting inputs.");
        if(cellLightingRequested&&!characterSpecular)throw new ArgumentException("Cell lighting requires specular character resources.");
        if(characterSpecular&&!characterNormalDiffuse)throw new ArgumentException("Scene specular requires normal diffuse resources.");
        if(characterNormalDiffuse&&!characterAlbedoFactors||characterTextureProfile is not null&&!characterNormalDiffuse)throw new ArgumentException("Scene normal/details require owned albedo and normal resources.");
        if(characterCustomized&&(!characterAlbedoFactors||args.Contains("--scene-character-face-profile")))throw new ArgumentException("Shared scene customization requires albedo factors and one profile.");
        string? characterFaceProfile=characterFaceRequested?Read(characterCustomized?"--scene-character-customization-profile":"--scene-character-face-profile"):null;
        if(characterAlbedoFactors&&!characterPaletteDefaults)throw new ArgumentException("Character albedo factors require palettes.");
        if(characterFaceRequested&&!characterMaterialDefaults)throw new ArgumentException("Generated character face requires materials.");
        if(characterPaletteDefaults&&!characterMaterialDefaults)throw new ArgumentException("Character palette defaults require materials.");
        if(characterMaterialDefaults&&!characterRequested)throw new ArgumentException("Character textures require owned scene character.");
        bool characterAnimationRequested=args.Contains("--scene-character-animation-table");
        if(characterAnimationRequested&&!characterRequested)throw new ArgumentException("Scene animation requires an owned character.");
        string? cameraProfilePin=args.Contains("--scene-camera-profile-sha256")?Read("--scene-camera-profile-sha256"):null;
        if(cameraProfilePin is not null&&(!characterRequested||!triangleGuardRequested))throw new ArgumentException("Camera profile requires owned character/room triangle guard.");
        string? locomotionProfile=args.Contains("--scene-character-locomotion-profile")?Read("--scene-character-locomotion-profile"):null;
        if(locomotionProfile is not null&&!characterAnimationRequested)throw new ArgumentException("Movement bank requires pinned idle selection.");
        var characterSelection=characterAnimationRequested?new LogicalAnimationClipSelection(Read("--scene-character-animation-table"),Read("--scene-character-animation-name"),Read("--scene-character-animation-sha256")):null;
        if(triangleGuardRequested&&(!profileRequested||!loadMaterials||!loadRooms))throw new ArgumentException("Scene triangle guard requires original profile and selected room/material content.");
        var loadingLayer=new CanvasLayer();AddChild(loadingLayer);var loadingPanel=new VBoxContainer{Position=new(24,24)};loadingLayer.AddChild(loadingPanel);
        loadingPanel.AddChild(new Label{Text="Loading selected interior assets…"});var cancelLoad=new Button{Text="Cancel loading"};loadingPanel.AddChild(cancelLoad);
        int mainThread=System.Environment.CurrentManagedThreadId,workerThread=mainThread,workerClosed=0,loadingFrames=0;
        var firstLookup=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);actorUpdate=_=>loadingFrames++;
        var load=new OwnedBackgroundAssetLoad<InteriorSceneContentBundle>(token=>
        {
            workerThread=System.Environment.CurrentManagedThreadId;
            try
            {
                token.ThrowIfCancellationRequested();using var config=File.OpenText(configurationPath);using var overlay=ConfiguredTreOverlay.Open(rootPath,config);
                IReadOnlyDictionary<BlueprintCustomizationKey,int>? faceValues=null;
                if(characterFaceProfile is not null){using var profile=File.OpenRead(characterFaceProfile);faceValues=BlueprintFaceCustomizationProfile.Load(profile);}
                IReadOnlyDictionary<string,int>? shapeValues=null;if(characterShapeProfile is not null){using var shapeStream=File.OpenRead(characterShapeProfile);shapeValues=SkeletalAppearanceShapes.LoadProfile(shapeStream);}
                IReadOnlyDictionary<ShaderCustomizationKey,int>? textureValues=null;if(characterTextureProfile is not null){using var textureStream=File.OpenRead(characterTextureProfile);textureValues=SkeletalAppearanceCustomization.LoadProfile(textureStream);}
                TreLookupResult Resolve(string path)
                {
                    token.ThrowIfCancellationRequested();
                    if(cancelTest&&!firstLookup.Task.IsCompleted){firstLookup.TrySetResult(true);token.WaitHandle.WaitOne(TimeSpan.FromSeconds(15));token.ThrowIfCancellationRequested();throw new InvalidOperationException("Cancellation fixture did not cancel pending lookup.");}
                    return overlay.Lookup(path);
                }
                IReadOnlyDictionary<string,LogicalAnimationClipSelection>? animationBank=null;
                if(locomotionProfile is not null){using var bankStream=File.OpenRead(locomotionProfile);animationBank=SkeletalAnimationBankProfile.Load(bankStream);}
                var result=InteriorSceneContentBundleReader.Load(Resolve,specification,specPin,tablePin,member,layoutPin,loadRooms,loadMaterials,
                    actorTemplatePath:actorTemplate,actorTemplateSha256:actorTemplatePin,createTriangleGuard:triangleGuardRequested,
                    maximumCollisionTriangles:65536,characterAppearanceSha256:characterPin,characterAnimationSelection:characterSelection,characterMaterialDefaults:characterMaterialDefaults,characterPaletteDefaults:characterPaletteDefaults,
                    characterAlbedoFactors:characterAlbedoFactors,characterFaceValues:characterCustomized?null:faceValues,cancellation:token,
                    characterCustomizationValues:characterCustomized?faceValues!.ToDictionary(p=>new ShaderCustomizationKey(p.Key.VariableName,p.Key.FactoryFlag),p=>p.Value):null,characterShapeValues:shapeValues,characterNormalDiffuse:characterNormalDiffuse,characterTextureValues:textureValues,characterVertexLitSpecular:characterSpecular,characterAdditionalAnimations:animationBank,cameraProfileSha256:cameraProfilePin,loadCellLights:cellLightingRequested,loadRoomLighting:roomLightingRequested,loadStaticNormals:doorLightingRequested);token.ThrowIfCancellationRequested();return result;
            }
            finally{Volatile.Write(ref workerClosed,1);}
        });
        interiorSceneLoad=load;cancelLoad.Pressed+=()=>{load.Cancel();cancelLoad.Disabled=true;};
        try
        {
            var transfer=load.TakeAsync();
            if(cancelTest&&await Task.WhenAny(firstLookup.Task,load.WorkerCompletion)==firstLookup.Task)cancelLoad.EmitSignal(Button.SignalName.Pressed);
            content=await transfer;
            if(!GodotObject.IsInstanceValid(this)||!IsInsideTree())return;
            if(load.IsCancellationRequested)throw new OperationCanceledException();
            if(System.Environment.CurrentManagedThreadId!=mainThread||workerThread==mainThread||Volatile.Read(ref workerClosed)!=1)throw new AssetPreviewException("Interior worker/main-thread ownership failed.");
            bundle=content.Scene;rooms=content.Rooms;roomMaterials=content.Materials;
            GD.Print($"PASS: interior CPU worker; main_thread_delivery=true archive_scope_closed=true loading_frames={loadingFrames}");
        }
        catch(OperationCanceledException)
        {
            if(GodotObject.IsInstanceValid(this)&&IsInsideTree())
            {
                if(cancelTest&&(Volatile.Read(ref workerClosed)!=1||interiorScene is not null||interiorSceneVisuals is not null||interiorSceneCharacter is not null))throw new AssetPreviewException("Canceled load leaked scene publication.");
                GD.Print(cancelTest?"PASS: canceled interior worker; pending first lookup, archive cleanup and zero scene/GPU publication":"Interior loading canceled.");GetTree().Quit();
            }
            return;
        }
        finally
        {
            actorUpdate=null;if(GodotObject.IsInstanceValid(cancelLoad))cancelLoad.Disabled=true;if(GodotObject.IsInstanceValid(loadingLayer))loadingLayer.QueueFree();
            load.Dispose();if(ReferenceEquals(interiorSceneLoad,load))interiorSceneLoad=null;
        }
        if(characterPaletteDefaults)GD.Print($"PASS: owned interior character palette factors={content.Character!.MaterialBundle!.PaletteDefaults!.Values.Sum(p=>p.AuthoredFactors.Count)} translated={characterAlbedoFactors}");
        if(characterFaceRequested)GD.Print($"PASS: owned interior generated face outputs={content.Character!.GeneratedFaces!.Outputs.Count} bindings={content.Character.GeneratedFaces.Bindings.Count}");
        GD.Print($"PASS: owned interior content sources={content.Sources.Count} source_bytes={content.Sources.Values.Sum(source=>(long)source.Bytes)}");
        if(!bundle.Sources.TryGetValue(LegacyAssetPath.Normalize(Read("--asset-member")),out var layoutIdentity)||!layoutIdentity.Sha256.Equals(Read("--asset-sha256"),StringComparison.OrdinalIgnoreCase))
            throw new AssetPreviewException("Scene layout differs from pinned inspector member.");
        if(bundle.Doorways.Count!=2||bundle.Doorways[0].TargetCell!=bundle.Doorways[1].Cell||bundle.Doorways[0].Cell==bundle.Doorways[1].TargetCell)
            throw new AssetPreviewException("Scene preview requires a selected three-cell/two-door route.");
        var first=bundle.Doorways[0].ConnectedApproach(stepPolicy:new(UseLocalRayCoverage:true));
        var last=bundle.Doorways[1].ConnectedApproach(stepPolicy:new(UseLocalRayCoverage:true));
        var direction=last.End-first.Start.LocalPosition;direction.Y=0;direction=NVector.Normalize(direction);
        bool holdClosing=args.Contains("--hold-closing-doors");
        var recoveredSphere=content.ActorProfile?.Collision.CreateFallbackSphere();
        ActorSphereVolume? sphereVolume=recoveredSphere is null?null:new(recoveredSphere.LocalCenter,recoveredSphere.Radius);
        if(profileRequested!=(sphereVolume is not null))throw new AssetPreviewException("Requested actor profile was not delivered.");
        if(sphereVolume is not null)GD.Print($"PASS: original actor profile sources={content.ActorProfile!.Sources.Count} radius={sphereVolume.Radius} center_offset={sphereVolume.WorldCenterOffset}");
        var actorCenterOffset=sphereVolume?.WorldCenterOffset??new NVector(0,.9f,0);
        var triangleGeometry=content.TriangleGuard?.Geometry.ToDictionary(p=>p.Key,p=>p.Value.Triangles);
        if(triangleGuardRequested!=(triangleGeometry is not null))throw new AssetPreviewException("Requested triangle guard was not delivered.");
        if(content.TriangleGuard is not null)GD.Print($"PASS: explicit modern room triangle guard selector={content.TriangleGuard.Selector} source={content.TriangleGuard.SourceTriangles} eligible={content.TriangleGuard.EligibleTriangles} retained={content.TriangleGuard.Triangles} degenerate={content.TriangleGuard.DegenerateTriangles}");
        float occupancyAdvance=sphereVolume is null?.48f:.8f-sphereVolume.Radius-PortalBarrierCollider.Padding-.03f;
        if(holdClosing&&occupancyAdvance<=0)throw new AssetPreviewException("Selected sphere lacks the occupied-threshold fixture clearance.");
        int secondGeometry=bundle.Layout.Cells[bundle.Doorways[1].Cell].Portals[bundle.Doorways[1].Portal].GeometryIndex;
        var geometries=bundle.Doorways.Select(d=>bundle.Layout.Cells[d.Cell].Portals[d.Portal].GeometryIndex).ToArray();
        interiorSceneVisuals=new(content.Geometry,roomMaterials?.Meshes,allowMainTextureOnly:roomMaterials is not null,normalMaterials:content.StaticNormals);AddChild(interiorSceneVisuals.Root);
        var roomRoot=interiorSceneVisuals.Root.GetChild<Node3D>(0);var leafRoot=interiorSceneVisuals.Root.GetChild<Node3D>(1);
        for(int i=0;i<bundle.Doorways.Count;i++)interiorSceneLeaves.Add(leafRoot.GetChild<Node3D>(i));
        if(roomMaterials is not null)
        {
            foreach(string diagnostic in roomMaterials.Diagnostics.Concat(interiorSceneVisuals.MaterialDiagnostics))GD.Print("Scene material fallback: "+diagnostic);
            GD.Print($"PASS: scene MAIN texture inspection sources={roomMaterials.Sources.Count} textured_templates={interiorSceneVisuals.TexturedMeshTemplates} textured_surfaces={interiorSceneVisuals.TexturedSurfaces} neutral_surfaces={interiorSceneVisuals.NeutralSurfaces} omitted_surfaces={interiorSceneVisuals.OmittedSurfaces} gpu_textures={interiorSceneVisuals.UniqueGpuTextures} diagnostics={roomMaterials.Diagnostics.Count+interiorSceneVisuals.MaterialDiagnostics.Count}");
        }
        var roomNodes=rooms?.Geometry.Root.Children.Select((child,index)=>(child.AuthoredIndex,Node:roomRoot.GetChild<Node3D>(index))).ToDictionary(value=>value.AuthoredIndex,value=>value.Node);
        if(roomLightingRequested)
        {
            int textureCount=interiorSceneVisuals.UniqueGpuTextures;interiorRoomLighting=new(content,roomNodes!,interiorSceneVisuals);
            if(textureCount!=interiorSceneVisuals.UniqueGpuTextures)throw new AssetPreviewException("Room lighting duplicated shared GPU textures.");
            GD.Print($"PASS: retained original room material overrides surfaces={interiorRoomLighting.SurfaceCount} materials={interiorRoomLighting.MaterialCount} shared_textures=true");
        }
        if(doorLightingRequested)
        {
            int textureCount=interiorSceneVisuals.UniqueGpuTextures;interiorDoorLighting=new(content,interiorSceneLeaves,interiorSceneVisuals,useRecoveredLightSelection);
            if(textureCount!=interiorSceneVisuals.UniqueGpuTextures)throw new AssetPreviewException("Door lighting duplicated shared GPU textures.");
            GD.Print($"PASS: retained original door normal overrides surfaces={interiorDoorLighting.SurfaceCount} materials={interiorDoorLighting.MaterialCount} fallback_vertices={interiorDoorLighting.FallbackVertices} shared_textures=true");
        }
        var visibility=rooms is null?null:new PortalCellVisibilityQuery(bundle.Layout,rooms.CellAppearances.Keys.ToArray());
        floorPreview=new(bundle.Floors);var floorFrame=new Node3D();AddChild(floorFrame);floorFrame.AddChild(floorPreview.Root);
        var material=new StandardMaterial3D{ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,AlbedoColor=new Color(.95f,.65f,.18f,.65f),Transparency=BaseMaterial3D.TransparencyEnum.Alpha};
        Mesh mesh=sphereVolume is null?new BoxMesh{Size=new(.5f,1.8f,.5f),Material=material}:
            new SphereMesh{Radius=sphereVolume.Radius,Height=sphereVolume.Radius*2,RadialSegments=24,Rings=12,Material=material};
        interiorDoorResources.Add(material);interiorDoorResources.Add(mesh);
        var actor=new MeshInstance3D{Mesh=mesh};AddChild(actor);var camera=new Camera3D{Current=true,Near=.02f,Far=200,Fov=70,KeepAspect=Camera3D.KeepAspectEnum.Height};AddChild(camera);
        if(characterRequested!=(content.Character is not null))throw new AssetPreviewException("Requested scene character was not delivered.");
        if(content.Character is not null)
        {
            interiorSceneCharacter=new(content.Character,true,characterSpecular);AddChild(interiorSceneCharacter.Root);interiorSceneCharacter.VerifyBoundary(!args.Contains("--headless"));
            GD.Print($"PASS: owned interior character; texture_defaults={characterMaterialDefaults} placement=world_feet animation={characterAnimationRequested} generated_face={characterFaceRequested}");
        }
        var characterAnimation=content.Character?.PreparedAnimation;
        if(characterAnimationRequested!=(characterAnimation is not null))throw new AssetPreviewException("Selected interior animation was not prepared.");
        var characterClock=characterAnimation is null?null:new AnimationPreviewClock(characterAnimation.FrameRate,characterAnimation.EndFrame,characterAnimation.ProxyTimeScale);
        if(characterClock is not null){characterClock.SetPlaying(true);interiorSceneCharacter!.SetOriginalFrame(0);}
        LocomotionPreviewClock? locomotion=null;LocomotionPreviewState? presentedLocomotion=null;var gaitTicks=new Dictionary<PreviewLocomotionMode,int>();double pendingPresentationSeconds=0;
        if(locomotionProfile is not null)
        {
            var bank=content.Character!.AdditionalAnimations??throw new AssetPreviewException("Owned movement bank missing.");
            if(bank.Count!=2||!bank.ContainsKey("walk")||!bank.ContainsKey("run"))throw new AssetPreviewException("Interior movement preview requires explicit walk/run keys.");
            locomotion=new(LocomotionPreviewClock.Describe(characterAnimation!),LocomotionPreviewClock.Describe(bank["walk"]),LocomotionPreviewClock.Describe(bank["run"]),3);
            GD.Print("PASS: owned original movement bank; explicit run threshold=3 accepted planar distance controls phase root motion is inert");
        }
        var cameraProfile=content.CameraProfile;var chaseRig=cameraProfile is null?null:ChaseCameraObstruction.FromInterior(content.TriangleGuard!);
        var cameraRecovery=new ChaseCameraDistanceClock(.2);
        ChaseCameraResult? cameraBoom=null;CameraCellResult? cameraCell=null;PortalCellVisibilityResult? presentedVisibility=null;
        var cameraCells=rooms is null?null:new PortalCameraCellQuery(bundle.Layout,rooms.CellAppearances.Keys.ToArray());
        cameraLodUpdate=()=>interiorSceneVisuals.SelectFromCamera(camera.GlobalPosition);
        AddChild(new WorldEnvironment{Environment=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new Color(.08f,.10f,.14f)}});
        var layer=new CanvasLayer();AddChild(layer);var frame=new PanelContainer{Position=new(18,18),Size=new(404,GetViewport().GetVisibleRect().Size.Y-36)};layer.AddChild(frame);
        var style=new StyleBoxFlat{BgColor=new Color(.02f,.03f,.06f,.94f),ContentMarginLeft=12,ContentMarginRight=12,ContentMarginTop=12,ContentMarginBottom=12};interiorDoorResources.Add(style);frame.AddThemeStyleboxOverride("panel",style);
        var scroll=new ScrollContainer{HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled,VerticalScrollMode=ScrollContainer.ScrollMode.Auto};frame.AddChild(scroll);
        var panel=new VBoxContainer{SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};scroll.AddChild(panel);
        panel.AddThemeFontSizeOverride("font_size",14);panel.AddThemeConstantOverride("separation",2);
        panel.AddChild(new Label{Text=$"Interior route: {bundle.Layout.Cells[bundle.Doorways[0].Cell].Name} → {bundle.Layout.Cells[bundle.Doorways[1].Cell].Name} → {bundle.Layout.Cells[bundle.Doorways[1].TargetCell].Name}",AutowrapMode=TextServer.AutowrapMode.WordSmart,CustomMinimumSize=new(380,0)});
        var permission=new CheckBox{Text="Second doorway: entry allowed",ButtonPressed=true};panel.AddChild(permission);
        if(holdClosing)panel.AddChild(new Label{Text="Closing doors wait for actor clearance"});
        if(sphereVolume is not null)panel.AddChild(new Label{Text=$"Actor collision sphere: radius {sphereVolume.Radius:F2} m"});
        CheckBox? collisionMarker=null;
        if(characterRequested){panel.AddChild(new Label{Text=characterSpecular?"Character normal/specular preview | "+(cellLightingRequested?"original room lights":"diagnostic lighting") :characterNormalDiffuse?"Character normal mapped diffuse | diagnostic lighting":characterFaceRequested&&characterAlbedoFactors?"Character skin and eye colors · generated face":characterMaterialDefaults?"Character MAIN texture preview":"Neutral character shown in overview"});collisionMarker=new CheckBox{Text="Show collision sphere"};panel.AddChild(collisionMarker);}
        CheckBox? characterPlay=null;
        if(characterClock is not null){characterPlay=new CheckBox{Text=locomotion is null?"Play original idle":"Animate accepted movement",ButtonPressed=true};panel.AddChild(characterPlay);characterPlay.Toggled+=characterClock.SetPlaying;}
        if(triangleGuardRequested)panel.AddChild(new Label{Text="Room geometry blocks movement"});
        var transformed=new CheckBox{Text="Rotate and translate building owner"};panel.AddChild(transformed);
        var follow=new CheckBox{Text="View inside the rooms",ButtonPressed=rooms is not null};if(rooms is not null)panel.AddChild(follow);
        var thirdPerson=new CheckBox{Text="Follow character from behind",ButtonPressed=chaseRig is not null&&!args.Contains("--asset-self-test")&&!args.Contains("--capture")};
        var cameraZoom=new OptionButton();
        if(cameraProfile is not null)
        {
            panel.AddChild(thirdPerson);foreach(float value in cameraProfile.ZoomSettings)cameraZoom.AddItem($"Camera distance: {value:0.##} m");cameraZoom.Select(cameraProfile.SelectSetting(-1));panel.AddChild(cameraZoom);
        }
        var portalVisibility=new CheckBox{Text="Select rooms through portal openings",ButtonPressed=args.Contains("--portal-visibility")};if(rooms is not null)panel.AddChild(portalVisibility);
        var showFloors=new CheckBox{Text="Show floor inspection overlay",ButtonPressed=rooms is null};panel.AddChild(showFloors);
        showFloors.Toggled+=visible=>floorPreview.Root.Visible=visible;floorPreview.Root.Visible=showFloors.ButtonPressed;
        var runSpeed=new CheckBox{Text="Run speed (5.2 m/s)"};if(locomotion is not null)panel.AddChild(runSpeed);
        bool fixture=args.Contains("--asset-self-test")||args.Contains("--capture");
        var automatic=new CheckBox{Text="Continuous movement (W / A / S / D)",ButtonPressed=!fixture};panel.AddChild(automatic);
        var mouseLook=new CheckBox{Text="Mouse look (Tab toggles; Esc releases)"};if(rooms is not null)panel.AddChild(mouseLook);
        var paused=new CheckBox{Text="Pause simulation"};panel.AddChild(paused);
        var forward=new Button{Text="Step forward 0.08m"};var reverse=new Button{Text="Step backward 0.08m"};var wait=new Button{Text="Wait 0.1 seconds"};var reset=new Button{Text="Reset actor and both doors"};
        foreach(var button in new[]{forward,reverse,wait,reset})panel.AddChild(button);
        var status=new Label();panel.AddChild(status);
        AppearanceTransform owner=AppearanceTransform.Identity;bool w=false,s=false,a=false,d=false;var orientation=new ActorViewOrientation();
        PortalActorInput InputState()=>PlanarActorInputMapping.Create(owner.TransformDirection(orientation.PlanarForward(direction)),(w?1:0)-(s?1:0),(d?1:0)-(a?1:0),runSpeed.ButtonPressed?5.2f:1.6f,permission.ButtonPressed);
        runSpeed.Toggled+=_=>Submit(checked((long)Time.GetTicksUsec()));
        void Submit(long now)=>interiorSceneTimeline!.Submit(new(now,InputState()));
        void Overview()
        {
            var center=owner.TransformPoint((first.Start.LocalPosition+last.End)*.5f);var view=center+owner.TransformDirection(new(14,10,8));camera.Position=new(view.X,view.Y,-view.Z);camera.LookAt(new(center.X,center.Y+1,-center.Z));
        }
        int? lightingCell=null;AppearanceTransform? lightingOwner=null;int lightingPublications=0;var lightingVisited=new HashSet<int>();
        void Refresh(double cameraSeconds=0,bool cameraActive=true)
        {
            interiorRoomLighting?.SetOwner(owner);interiorDoorLighting?.SetOwner(owner);
            var state=interiorScene!.State;var p=state.WorldPosition;var bodyCenter=p+actorCenterOffset;actor.Position=new(bodyCenter.X,bodyCenter.Y,-bodyCenter.Z);
            if(cellLightingRequested&&(lightingCell!=state.Locator.CellIndex||lightingOwner!=owner))
            {
                var before=interiorSceneCharacter!.GeometryFingerprint();
                var rig=useRecoveredLightSelection?CellLightingPreview.FromLegacyCell(content.CellLights![state.Locator.CellIndex],owner,new(.1f)):new CellLightingPreview(content.CellLights![state.Locator.CellIndex],owner,new(.1f));interiorSceneCharacter.ApplyCellLighting(rig);
                if(before!=interiorSceneCharacter.GeometryFingerprint())throw new AssetPreviewException("Cell lighting replaced retained geometry/material/pose.");
                lightingCell=state.Locator.CellIndex;lightingOwner=owner;lightingPublications++;lightingVisited.Add(lightingCell.Value);
            }
            bool renderedThirdPerson=false;
            if(interiorSceneCharacter is not null)interiorSceneCharacter.Root.Position=new(p.X,p.Y,-p.Z);
            if(locomotion is not null&&!ReferenceEquals(presentedLocomotion,locomotion.State))
            {
                var presentation=locomotion.State;interiorSceneCharacter!.SetMovementFrame(presentation.Mode.ToString().ToLowerInvariant(),presentation.Frame,Math.Min(.25,pendingPresentationSeconds));pendingPresentationSeconds=0;
                interiorSceneCharacter.Root.Rotation=new(0,MathF.Atan2(-presentation.Heading.X,presentation.Heading.Z),0);
                if((interiorSceneCharacter.Root.Basis*Vector3.Forward).DistanceTo(new(presentation.Heading.X,0,-presentation.Heading.Z))>1e-5f)throw new AssetPreviewException("Locomotion heading reflection failed.");presentedLocomotion=presentation;
            }
            if(follow.ButtonPressed)
            {
                var look=owner.TransformDirection(orientation.LookForward(direction));var view=p+NVector.UnitY*(cameraProfile?.TargetHeight(0,new HashSet<int>(),1)??1.6f);
                if(chaseRig is not null&&thirdPerson.ButtonPressed)
                {
                    var doorBoxes=bundle.Doorways.Select((door,index)=>new PortalBarrierCollider(door.Leaf.Extent.Minimum!.Value,door.Leaf.Extent.Maximum!.Value,state.Doors[geometries[index]].LeafPose)).ToArray();
                    cameraBoom=chaseRig.Query(view,look,cameraProfile!.ZoomDistance(cameraZoom.Selected,1),owner,doorBoxes);
                    float displayed=(float)cameraRecovery.Advance(cameraBoom.Distance,cameraSeconds,cameraActive).Distance;
                    cameraBoom=cameraBoom with{Distance=displayed,Position=cameraBoom.Target-NVector.Normalize(look)*displayed};view=cameraBoom.Position;
                    renderedThirdPerson=cameraBoom.Distance>cameraProfile.FirstPersonThreshold;
                    camera.Position=new(view.X,view.Y,-view.Z);var target=renderedThirdPerson?cameraBoom.Target:view+look*4;camera.LookAt(new(target.X,target.Y,-target.Z));
                }
                else {cameraBoom=null;camera.Position=new(view.X,view.Y,-view.Z);var target=view+look*4;camera.LookAt(new(target.X,target.Y,-target.Z));}
            }
            else cameraBoom=null;
            actor.Visible=(!follow.ButtonPressed||renderedThirdPerson)&&(collisionMarker?.ButtonPressed??true);
            if(interiorSceneCharacter is not null)interiorSceneCharacter.Root.Visible=!follow.ButtonPressed||renderedThirdPerson;
            cameraCell=cameraBoom is null?null:cameraCells!.LocateWorld(state.Locator.CellIndex,cameraBoom.Target,cameraBoom.Position,owner);
            portalVisibility.Disabled=false;portalVisibility.TooltipText=cameraCell is not null&&cameraCell.Status!=CameraCellStatus.Located?"Camera room is unresolved; all loaded rooms remain visible":"";
            PortalCellVisibilityResult? visible=null;
            if(visibility is not null&&follow.ButtonPressed&&portalVisibility.ButtonPressed&&(cameraCell is null||cameraCell.Status==CameraCellStatus.Located))
            {
                var size=GetViewport().GetVisibleRect().Size;var eye=GodotAppearanceTransform.ToGodot(owner).AffineInverse()*camera.Position;
                visible=visibility.Query(cameraCell?.CellIndex??state.Locator.CellIndex,new(new(eye.X,eye.Y,-eye.Z),orientation.LookForward(direction),NVector.UnitY,Mathf.DegToRad(camera.Fov),size.X/size.Y,camera.Far));
            }
            presentedVisibility=visible;
            if(roomNodes is not null)foreach(var pair in roomNodes)pair.Value.Visible=visible is null||visible.Cells.Contains(pair.Key);
            for(int i=0;i<interiorSceneLeaves.Count;i++)interiorSceneLeaves[i].Visible=visible is null||visible.Cells.Contains(bundle.Doorways[i].Cell)||visible.Cells.Contains(bundle.Doorways[i].TargetCell);
            for(int i=0;i<geometries.Length;i++)interiorSceneLeaves[i].Transform=GodotAppearanceTransform.ToGodot(state.Doors[geometries[i]].LeafPose);
            status.Text=$"Owner: {bundle.Layout.Cells[state.Locator.CellIndex].Name}\nTransfers: {state.TotalTransfers}; ticks: {state.Frame}\n"+
                string.Join("\n",geometries.Select((g,i)=>$"Door {i+1}: {state.Doors[g].Controller.Fraction:F3}; retained: {state.Doors[g].RetainedActors}{(state.Doors[g].ClosingHeld?"; waiting":"")}"))+
                $"\nMovement: {state.Movement?.Movement.Status.ToString()??"not moved"}\nContact: {state.Movement?.ContactKind?.ToString()??"none"}\nCamera distance: {cameraBoom?.Distance.ToString("F2")??"first-person/overview"}\nCamera room: {cameraCell?.CellIndex?.ToString()??(cameraCell is null?state.Locator.CellIndex.ToString():cameraCell.Status.ToString())}\nVisible rooms: {visible?.Cells.Count??rooms?.CellAppearances.Count??0}{(visible?.BudgetFallback==true?" (budget fallback)":"")}\nCatch-up time dropped: {interiorSceneHost!.State.DroppedMicroseconds/1000.0:F1} ms";
        }
        InteriorDoorActorSceneState TickScene(float seconds,NVector delta,bool? entry)
        {
            var before=interiorScene!.State.WorldPosition;var next=interiorScene.Tick(seconds,delta,l=>new(l.GeometryIndex==secondGeometry?entry:true,true));
            if(locomotion is not null)
            {
                if(locomotion.Advance(seconds,next.WorldPosition-before,characterClock!.Playing))pendingPresentationSeconds+=seconds;
                gaitTicks[locomotion.State.Mode]=gaitTicks.GetValueOrDefault(locomotion.State.Mode)+1;
            }
            return next;
        }
        void Configure(bool rotated)
        {
            cameraRecovery.Reset();interiorScene?.Dispose();owner=rotated?new(new(0,0,1),NVector.UnitY,new(-1,0,0),new(20,4,-9)):AppearanceTransform.Identity;
            interiorScene=bundle.CreateActor(first.Start,owner,sphereVolume is null?new(UseLocalRayCoverage:true):null,holdClosing,sphereVolume,
                triangleGeometry,65536);
            interiorSceneHost=new(()=>interiorScene.State,(seconds,delta,entry)=>TickScene(seconds,delta,entry),interiorScene.Reset);
            if(locomotion is not null){locomotion.Reset(owner.TransformDirection(direction));presentedLocomotion=null;pendingPresentationSeconds=0;}
            mouseLook.ButtonPressed=false;orientation=new();w=s=a=d=false;long now=checked((long)Time.GetTicksUsec());interiorSceneTimeline=new(interiorSceneHost,now,InputState());
            interiorSceneTimeline.AdvanceTo(now,automatic.ButtonPressed&&!paused.ButtonPressed&&GetWindow().HasFocus());floorFrame.Transform=GodotAppearanceTransform.ToGodot(owner);floorPreview.SelectCell(-1);
            if(characterClock is not null){characterClock.Seek(0);interiorSceneCharacter!.SetOriginalFrame(0);}
            roomRoot.Transform=GodotAppearanceTransform.ToGodot(owner);
            Overview();Refresh();
        }
        void Manual(NVector localDelta,float seconds=.05f)
        {
            var from=interiorScene!.State.WorldPosition;var state=TickScene(seconds,owner.TransformDirection(localDelta),permission.ButtonPressed);
            floorPreview.ShowActorWalk(from,state.Movement!);Refresh(Math.Min(seconds,.25f));
        }
        forward.Pressed+=()=>Manual(direction*.08f);reverse.Pressed+=()=>Manual(-direction*.08f);wait.Pressed+=()=>Manual(NVector.Zero,.1f);
        reset.Pressed+=()=>{cameraRecovery.Reset();mouseLook.ButtonPressed=false;orientation=new();w=s=a=d=false;long now=checked((long)Time.GetTicksUsec());interiorSceneTimeline!.Reset(now,InputState());if(locomotion is not null){locomotion.Reset(owner.TransformDirection(direction));presentedLocomotion=null;pendingPresentationSeconds=0;}if(characterClock is not null){characterClock.Seek(0);interiorSceneCharacter!.SetOriginalFrame(0);}floorPreview.SelectCell(-1);Refresh();};
        transformed.Toggled+=Configure;Configure(false);
        follow.Toggled+=_=>{cameraRecovery.Reset();if(!follow.ButtonPressed)mouseLook.ButtonPressed=false;mouseLook.Disabled=!follow.ButtonPressed||!interiorSceneHost!.State.Active;Overview();Refresh();};
        portalVisibility.Toggled+=_=>Refresh();thirdPerson.Toggled+=_=>{cameraRecovery.Reset();Refresh();};cameraZoom.ItemSelected+=_=>Refresh();
        if(collisionMarker is not null)collisionMarker.Toggled+=_=>Refresh();
        void Boundary(bool focused)
        {
            bool active=automatic.ButtonPressed&&!paused.ButtonPressed&&focused;w=active&&Input.IsPhysicalKeyPressed(Key.W);s=active&&Input.IsPhysicalKeyPressed(Key.S);a=active&&Input.IsPhysicalKeyPressed(Key.A);d=active&&Input.IsPhysicalKeyPressed(Key.D);
            mouseLook.Disabled=!active||!follow.ButtonPressed;if(!active)mouseLook.ButtonPressed=false;
            long now=checked((long)Time.GetTicksUsec());Submit(now);interiorSceneTimeline!.AdvanceTo(now,active);forward.Disabled=reverse.Disabled=wait.Disabled=automatic.ButtonPressed;Refresh();
        }
        automatic.Toggled+=_=>Boundary(GetWindow().HasFocus());paused.Toggled+=_=>Boundary(GetWindow().HasFocus());permission.Toggled+=_=>Submit(checked((long)Time.GetTicksUsec()));actorFocus=Boundary;Boundary(GetWindow().HasFocus());
        mouseLook.Toggled+=captured=>{if(captured&&(!automatic.ButtonPressed||paused.ButtonPressed||!follow.ButtonPressed||!interiorSceneHost!.State.Active)){mouseLook.ButtonPressed=false;return;}if(!fixture)Input.MouseMode=captured?Input.MouseModeEnum.Captured:Input.MouseModeEnum.Visible;};
        actorMouseInput=e=>
        {
            if(!mouseLook.ButtonPressed||!automatic.ButtonPressed||paused.ButtonPressed||!follow.ButtonPressed)return;
            if(!float.IsFinite(e.Relative.X)||!float.IsFinite(e.Relative.Y)||Math.Abs(e.Relative.X)>4096||Math.Abs(e.Relative.Y)>4096)throw new ArgumentException("Bounded mouse motion required.");
            orientation=orientation.Rotate(e.Relative.X*.0025f,-e.Relative.Y*.0025f);Submit(checked((long)Time.GetTicksUsec()));
        };
        actorKeyInput=e=>{if(e.Echo)return;if(e.Pressed&&e.PhysicalKeycode==Key.Tab){mouseLook.ButtonPressed=!mouseLook.ButtonPressed;return;}if(e.Pressed&&e.PhysicalKeycode==Key.Escape){mouseLook.ButtonPressed=false;return;}switch(e.PhysicalKeycode){case Key.W:w=e.Pressed;break;case Key.S:s=e.Pressed;break;case Key.A:a=e.Pressed;break;case Key.D:d=e.Pressed;break;default:return;}Submit(checked((long)Time.GetTicksUsec()));};
        actorUpdate=elapsed=>
        {
            frame.Size=new(404,Math.Max(100,GetViewport().GetVisibleRect().Size.Y-36));
            bool active=automatic.ButtonPressed&&!paused.ButtonPressed&&GetWindow().HasFocus();bool nw=active&&Input.IsPhysicalKeyPressed(Key.W),ns=active&&Input.IsPhysicalKeyPressed(Key.S),na=active&&Input.IsPhysicalKeyPressed(Key.A),nd=active&&Input.IsPhysicalKeyPressed(Key.D);long now=checked((long)Time.GetTicksUsec());
            if(nw!=w||ns!=s||na!=a||nd!=d){w=nw;s=ns;a=na;d=nd;Submit(now);}var from=interiorScene!.State.WorldPosition;var clock=interiorSceneTimeline!.AdvanceTo(now,active);
            if(clock.StepsThisUpdate>0)floorPreview.ShowActorWalk(from,clock.Actor.Movement!);if(clock.FaultType is not null)automatic.ButtonPressed=false;Refresh(Math.Min(elapsed,.25),GetWindow().HasFocus()&&!paused.ButtonPressed);
            if(characterClock is not null&&locomotion is null)
            {
                if(!double.IsFinite(elapsed)||elapsed<0)throw new AssetPreviewException("Invalid scene animation elapsed time.");
                characterClock.SetFocused(GetWindow().HasFocus()&&!paused.ButtonPressed);
                if(characterClock.Advance(Math.Min(elapsed,.25)))interiorSceneCharacter!.SetOriginalFrame(characterClock.Frame);
            }
        };
        if(fixture)
        {
            IEnumerable<MeshInstance3D> Meshes(Node node)
            {
                foreach(Node child in node.GetChildren()){if(child is MeshInstance3D meshNode)yield return meshNode;foreach(var nested in Meshes(child))yield return nested;}
            }
            var firstLeafMeshes=Meshes(interiorSceneLeaves[0]).ToArray();var secondLeafMeshes=Meshes(interiorSceneLeaves[1]).ToArray();
            if(firstLeafMeshes.Length!=2||secondLeafMeshes.Length!=2||firstLeafMeshes.Where((meshNode,index)=>meshNode.Mesh!=secondLeafMeshes[index].Mesh||meshNode==secondLeafMeshes[index]).Any())throw new AssetPreviewException("Independent leaf instances failed immutable mesh sharing.");
            if(interiorSceneVisuals.TexturedMeshTemplates+interiorSceneVisuals.NeutralMeshTemplates!=(rooms is null?2:5))throw new AssetPreviewException("Scene mesh template sharing count failed.");
            GD.Print("PASS: shared scene visuals; two independently transformed leaf instances share both LOD meshes");
            if(rooms is not null)
            {
                var beforeView=interiorScene!.State;follow.ButtonPressed=false;follow.ButtonPressed=true;
                bool selectedVisibility=portalVisibility.ButtonPressed;portalVisibility.ButtonPressed=!selectedVisibility;portalVisibility.ButtonPressed=selectedVisibility;
                if(interiorScene.State!=beforeView||actor.Visible||rooms.CellAppearances.Count!=bundle.Floors.Count)throw new AssetPreviewException("Selected-room view ownership failed.");
                GD.Print($"PASS: selected room geometry; cells={rooms.CellAppearances.Count} sources={rooms.Sources.Count}; view toggles preserve actor state");
                if(roomMaterials is not null){StaticMeshPreview.VerifyZeroPassPolicy(roomMaterials.Meshes.Values.First());GD.Print("PASS: zero-pass synthetic all-surface omission and retained original primitive upload mapping");}
            }
            void Go(NVector target,int cell)
            {
                for(int i=0;i<240;i++){var delta=target-interiorScene!.State.Locator.LocalPosition;delta.Y=0;if(delta.Length()<1e-5f)break;if(delta.Length()>.08f)delta=NVector.Normalize(delta)*.08f;Manual(delta);}
                if(interiorScene!.State.Locator.CellIndex!=cell||NVector.Distance(interiorScene.State.Locator.LocalPosition,target)>1e-4f)throw new AssetPreviewException("Multi-door manual route failed.");
            }
            int checkedTransfers=0;
            foreach(bool rotated in new[]{false,true})
            {
                Configure(rotated);permission.ButtonPressed=true;
                if(portalVisibility.ButtonPressed&&roomNodes!.Values.Count(node=>node.Visible)!=3)throw new AssetPreviewException("Initial portal room visibility failed.");
                for(int trip=0;trip<2;trip++){Go(last.End,bundle.Doorways[1].TargetCell);Go(first.Start.LocalPosition,first.Start.CellIndex);checkedTransfers+=4;}
                if(interiorScene!.State.TotalTransfers!=8)throw new AssetPreviewException("Multi-door shared controller accounting failed.");
                reset.EmitSignal(Button.SignalName.Pressed);permission.ButtonPressed=false;
                for(int i=0;i<200;i++)forward.EmitSignal(Button.SignalName.Pressed);
                if(interiorScene.State.Locator.CellIndex!=bundle.Doorways[1].Cell||interiorScene.State.TotalTransfers!=1)throw new AssetPreviewException("Multi-door second permission failed.");
                var held=interiorScene.State.WorldPosition;reverse.EmitSignal(Button.SignalName.Pressed);if(NVector.Distance(held,interiorScene.State.WorldPosition)<.07f)throw new AssetPreviewException("Multi-door denied retreat failed.");
                permission.ButtonPressed=true;reset.EmitSignal(Button.SignalName.Pressed);Go(last.End,bundle.Doorways[1].TargetCell);
                if(portalVisibility.ButtonPressed&&(roomNodes!.Values.Count(node=>node.Visible)!=1||!roomNodes[bundle.Doorways[1].TargetCell].Visible))throw new AssetPreviewException("Target-cell portal room visibility failed.");
                var state=interiorScene.State;var foot=state.WorldPosition;
                var bodyCenter=foot+actorCenterOffset;
                if(actor.Position.DistanceTo(new(bodyCenter.X,bodyCenter.Y,-bodyCenter.Z))>1e-5f||floorFrame.Transform!=GodotAppearanceTransform.ToGodot(owner)||geometries.Select((g,i)=>interiorSceneLeaves[i].Transform!=GodotAppearanceTransform.ToGodot(state.Doors[g].LeafPose)).Any(v=>v))throw new AssetPreviewException("Multi-door reflected placement failed.");
                if(interiorSceneCharacter is not null&&interiorSceneCharacter.Root.GlobalPosition.DistanceTo(new(state.WorldPosition.X,state.WorldPosition.Y,-state.WorldPosition.Z))>1e-5f)throw new AssetPreviewException("Character world-feet placement failed.");
                if(characterClock is not null)
                {
                    var beforeAnimation=interiorScene.State;var visualRoot=interiorSceneCharacter!.Root.Transform;characterClock.SetFocused(true);characterClock.SetPlaying(true);
                    for(int tick=0;tick<240;tick++)
                    {
                        characterClock.Advance(characterAnimation!.DurationSeconds/(characterAnimation.ProxyTimeScale*240));interiorSceneCharacter.SetOriginalFrame(characterClock.Frame);
                        if(tick%60==0)interiorSceneCharacter.VerifyBoundary(DisplayServer.GetName()!="headless");
                        if(!ReferenceEquals(interiorScene.State,beforeAnimation)||interiorSceneCharacter.Root.Transform!=visualRoot)throw new AssetPreviewException("Visual animation mutated authoritative actor feet/door state.");
                    }
                    characterPlay!.EmitSignal(BaseButton.SignalName.Toggled,false);double animationHeld=characterClock.Frame;characterClock.Advance(.1);if(characterClock.Frame!=animationHeld)throw new AssetPreviewException("Interior animation pause control failed.");
                    characterPlay.EmitSignal(BaseButton.SignalName.Toggled,true);characterClock.SetFocused(false);characterClock.Advance(1);if(characterClock.Frame!=animationHeld)throw new AssetPreviewException("Interior animation focus pause failed.");
                }
                if(roomRoot.Transform!=GodotAppearanceTransform.ToGodot(owner))throw new AssetPreviewException("Selected room reflected placement failed.");
            }
            if(holdClosing)
            {
                foreach(bool rotated in new[]{false,true})
                {
                    Configure(rotated);permission.ButtonPressed=true;Go(last.End,bundle.Doorways[1].TargetCell);Go(last.Start.LocalPosition,last.Start.CellIndex);
                    Go(last.Start.LocalPosition+direction*occupancyAdvance,last.Start.CellIndex);permission.ButtonPressed=false;
                    for(int i=0;i<80;i++)Manual(NVector.Zero);
                    if(!interiorScene!.State.Doors[secondGeometry].ClosingHeld)throw new AssetPreviewException("Original occupied close did not hold.");
                    long total=interiorScene.State.TotalTransfers;forward.EmitSignal(Button.SignalName.Pressed);
                    if(interiorScene.State.Movement!.Movement.Status!=CellFloorWalkStatus.EntryDenied||interiorScene.State.TotalTransfers!=total)throw new AssetPreviewException("Held original close granted entry.");
                    var held=interiorScene.State.WorldPosition;reverse.EmitSignal(Button.SignalName.Pressed);if(NVector.Distance(held,interiorScene.State.WorldPosition)<.07f)throw new AssetPreviewException("Held original close blocked retreat.");
                    for(int i=0;i<19;i++)reverse.EmitSignal(Button.SignalName.Pressed);for(int i=0;i<20;i++)Manual(NVector.Zero);
                    if(interiorScene.State.Doors[secondGeometry].ClosingHeld||interiorScene.State.Doors[secondGeometry].Controller.Fraction!=0)throw new AssetPreviewException("Retreat failed to release original closing hold.");
                }
                permission.ButtonPressed=true;
                GD.Print("PASS: original guarded closing host; two poses, occupied hold, denied entry, retreat and completed release verified");
            }
            automatic.ButtonPressed=true;_Notification((int)NotificationWMWindowFocusOut);
            if(interiorSceneHost!.State.Active||interiorSceneHost.State.PendingMicroseconds!=0)throw new AssetPreviewException("Multi-door focus loss failed.");
            _Notification((int)NotificationWMWindowFocusIn);if(!interiorSceneHost.State.Active)throw new AssetPreviewException("Multi-door focus resume failed.");
            paused.ButtonPressed=true;if(interiorSceneHost.State.Active||!forward.Disabled)throw new AssetPreviewException("Multi-door pause control failed.");
            automatic.ButtonPressed=false;paused.ButtonPressed=false;if(forward.Disabled||reverse.Disabled||wait.Disabled)throw new AssetPreviewException("Multi-door manual control ownership failed.");
            void KeyEvent(Key key,bool pressed){using var e=new InputEventKey{Keycode=key,PhysicalKeycode=key,Pressed=pressed};Input.ParseInputEvent(e);Input.FlushBufferedEvents();}
            if(rooms is not null)
            {
                void Motion(float x,float y){using var e=new InputEventMouseMotion{Relative=new(x,y)};Input.ParseInputEvent(e);Input.FlushBufferedEvents();}
                foreach(bool rotated in new[]{false,true})
                {
                    Configure(rotated);automatic.ButtonPressed=true;_Notification((int)NotificationWMWindowFocusIn);
                    try
                    {
                        KeyEvent(Key.W,true);KeyEvent(Key.Tab,true);if(!mouseLook.ButtonPressed)throw new AssetPreviewException("Mouse look activation failed.");
                        int queued=interiorSceneTimeline!.PendingTransitions;var actorBefore=interiorScene!.State;
                        Motion(MathF.PI/.0025f,-60);Refresh();
                        if(interiorSceneTimeline.PendingTransitions!=queued+1||interiorScene.State!=actorBefore||NVector.Distance(InputState().WorldVelocity,owner.TransformDirection(-direction*1.6f))>1e-5f||Math.Abs(orientation.PitchRadians-.15f)>1e-6f)throw new AssetPreviewException("Timestamped mouse heading/planar movement failed.");
                        var looking=owner.TransformDirection(orientation.LookForward(direction));if((-camera.GlobalBasis.Z).DistanceTo(new(looking.X,looking.Y,-looking.Z))>1e-5f)throw new AssetPreviewException("Reflected pitched camera heading failed.");
                        if(portalVisibility.ButtonPressed&&roomNodes!.Values.Count(node=>node.Visible)!=1)throw new AssetPreviewException("Reverse mouse view failed portal visibility.");
                        Motion(0,-4000);if(orientation.PitchRadians!=ActorViewOrientation.MaximumPitchRadians||InputState().WorldVelocity.Y!=0)throw new AssetPreviewException("Mouse pitch clamp changed movement height.");
                        KeyEvent(Key.Escape,true);var held=orientation;Motion(100,100);if(mouseLook.ButtonPressed||orientation!=held)throw new AssetPreviewException("Mouse release failed.");
                        KeyEvent(Key.Tab,true);_Notification((int)NotificationWMWindowFocusOut);Motion(100,100);if(mouseLook.ButtonPressed||orientation!=held||interiorSceneHost!.State.Active)throw new AssetPreviewException("Mouse focus cleanup failed.");
                        _Notification((int)NotificationWMWindowFocusIn);KeyEvent(Key.Tab,true);paused.ButtonPressed=true;Motion(100,100);if(mouseLook.ButtonPressed||orientation!=held||interiorSceneHost.State.Active)throw new AssetPreviewException("Mouse pause cleanup failed.");
                    }
                    finally{KeyEvent(Key.W,false);KeyEvent(Key.Tab,false);KeyEvent(Key.Escape,false);mouseLook.ButtonPressed=false;paused.ButtonPressed=false;automatic.ButtonPressed=false;}
                }
                Configure(true);GD.Print("PASS: mouse event route; two owner poses, timestamped heading/planar velocity, camera/portal view, pitch clamp, release/focus/pause cleanup");
            }
            try
            {
                int queued=interiorSceneTimeline!.PendingTransitions;
                KeyEvent(Key.W,true);if(!w||interiorSceneTimeline.PendingTransitions!=queued+1)throw new AssetPreviewException("Multi-door timestamped physical forward event failed.");
                KeyEvent(Key.S,true);if(InputState().WorldVelocity!=NVector.Zero)throw new AssetPreviewException("Multi-door opposing keys failed.");
                KeyEvent(Key.W,false);KeyEvent(Key.S,false);KeyEvent(Key.D,true);KeyEvent(Key.W,true);
                if(!d||!w||Math.Abs(InputState().WorldVelocity.Length()-1.6f)>1e-6)throw new AssetPreviewException("Multi-door diagonal key normalization failed.");
                KeyEvent(Key.D,false);KeyEvent(Key.W,false);if(InputState().WorldVelocity!=NVector.Zero||interiorSceneTimeline.PendingTransitions!=queued+8)throw new AssetPreviewException("Multi-door physical release route failed.");
                interiorSceneTimeline.AdvanceTo(checked((long)Time.GetTicksUsec()),false);if(interiorSceneTimeline.PendingTransitions!=0||interiorSceneTimeline.CurrentInput.WorldVelocity!=NVector.Zero)throw new AssetPreviewException("Multi-door suspended key history failed.");
            }
            finally{KeyEvent(Key.W,false);KeyEvent(Key.S,false);KeyEvent(Key.A,false);KeyEvent(Key.D,false);}
            foreach(bool rotated in new[]{false,true})
            {
                Configure(rotated);interiorSceneTimeline!.Reset(0,new(NVector.Zero,true));long squareTime=0;
                for(int i=0;i<30;i++)interiorSceneTimeline.AdvanceTo(squareTime+=20_000);
                foreach(var axes in new[]{(F:1f,R:1f),(F:1f,R:-1f),(F:-1f,R:-1f),(F:-1f,R:1f)})
                {
                    interiorSceneTimeline.Submit(new(squareTime,PlanarActorInputMapping.Create(owner.TransformDirection(direction),axes.F,axes.R,1.6f,true)));
                    for(int i=0;i<20;i++)interiorSceneTimeline.AdvanceTo(squareTime+=20_000);
                }
                if(interiorScene!.State.TotalTransfers!=2||interiorScene.State.Locator.CellIndex!=first.Start.CellIndex||NVector.Distance(interiorScene.State.WorldPosition,owner.TransformPoint(first.Start.LocalPosition))>1e-4f)throw new AssetPreviewException("Multi-door diagonal owner round trip failed.");
            }
            Configure(true);long now=0;interiorSceneTimeline!.Reset(now,new(NVector.Zero,true));interiorSceneTimeline.Submit(new(35_000,new(owner.TransformDirection(direction*1.6f),true)));
            for(int i=0;i<367;i++)interiorSceneTimeline.AdvanceTo(now+=20_000);
            if(interiorScene!.State.Locator.CellIndex!=bundle.Doorways[1].TargetCell||interiorScene.State.TotalTransfers!=2)throw new AssetPreviewException("Timestamped multi-door forward route failed.");
            interiorSceneTimeline.Submit(new(now,new(owner.TransformDirection(-direction*1.6f),true)));for(int i=0;i<365;i++)interiorSceneTimeline.AdvanceTo(now+=20_000);
            if(interiorScene.State.Locator.CellIndex!=first.Start.CellIndex||interiorScene.State.TotalTransfers!=4)throw new AssetPreviewException("Timestamped multi-door reverse route failed.");
            interiorSceneTimeline.AdvanceTo(now+=2_000_007);if(interiorSceneHost!.State.StepsThisUpdate!=8||interiorSceneHost.State.DroppedMicroseconds!=1_840_000)throw new AssetPreviewException("Multi-door bounded stall failed.");
            long before=interiorScene.State.Frame;interiorSceneTimeline.AdvanceTo(now+=100_000,false);interiorSceneTimeline.AdvanceTo(now+=100_000,true);if(interiorScene.State.Frame!=before||interiorSceneHost.State.PendingMicroseconds!=0)throw new AssetPreviewException("Multi-door pause/resume failed.");
            transformed.ButtonPressed=true;Configure(true);Go(last.End,bundle.Doorways[1].TargetCell);
            if(holdClosing){Go(last.Start.LocalPosition,last.Start.CellIndex);Go(last.Start.LocalPosition+direction*occupancyAdvance,last.Start.CellIndex);permission.ButtonPressed=false;for(int i=0;i<80;i++)Manual(NVector.Zero);forward.EmitSignal(Button.SignalName.Pressed);}
            GD.Print($"PASS: original multi-door host; manual_transfers={checkedTransfers} owner_poses=2 second-denial/retreat/reset, physical key route/diagonal round trip, typed timestamped four-transfer route, stall/pause/focus and all-leaf reflected placement verified");
            if(characterClock is not null)
            {
                interiorSceneCharacter!.SetOriginalFrame(characterAnimation!.EndFrame*.25);interiorSceneCharacter.VerifyBoundary(DisplayServer.GetName()!="headless");
                GD.Print($"PASS: animated original interior character sources={content.Sources.Count} selected_clip_cycles=2 actor_feet_authoritative=true door_state_unchanged=true pause_focus=true unused={string.Join(',',characterAnimation.UnusedAnimationTransforms)} missing_bind={string.Join(',',characterAnimation.MissingSkeletonTransforms)}");
            }
        }
        if(fixture&&locomotion is not null)
        {
            var actorBefore=interiorScene!.State;var rootBefore=interiorSceneCharacter!.Root.Transform;string topology=interiorSceneCharacter.GeometryFingerprint(false);
            int textureCount=interiorSceneCharacter.TextureCount;long textureBytes=interiorSceneCharacter.TextureBytes;
            foreach(string key in new[]{"walk","run","idle","run","walk","idle"})
            {
                var clip=key=="idle"?characterAnimation!:content.Character!.AdditionalAnimations![key];
                for(int tick=0;tick<80;tick++)
                {
                    interiorSceneCharacter.SetClipFrame(key,clip.EndFrame*(tick%40)/40.0);
                    if(tick%20==0)interiorSceneCharacter.VerifyBoundary(DisplayServer.GetName()!="headless");
                    if(!ReferenceEquals(actorBefore,interiorScene.State)||rootBefore!=interiorSceneCharacter.Root.Transform||topology!=interiorSceneCharacter.GeometryFingerprint(false)||textureCount!=interiorSceneCharacter.TextureCount||textureBytes!=interiorSceneCharacter.TextureBytes)throw new AssetPreviewException("Retained clip switch changed actor/door/root/resource state.");
                }
            }
            foreach(string key in new[]{"walk","run","idle","walk","idle"})
            {
                var clip=key=="idle"?characterAnimation!:content.Character!.AdditionalAnimations![key];
                for(int tick=0;tick<20;tick++)
                {
                    interiorSceneCharacter.SetMovementFrame(key,clip.EndFrame*(tick%20)/20.0,.01);
                    if(tick%5==0)interiorSceneCharacter.VerifyBoundary(DisplayServer.GetName()!="headless");
                    if(!ReferenceEquals(actorBefore,interiorScene.State)||rootBefore!=interiorSceneCharacter.Root.Transform||topology!=interiorSceneCharacter.GeometryFingerprint(false)||textureCount!=interiorSceneCharacter.TextureCount||textureBytes!=interiorSceneCharacter.TextureBytes)throw new AssetPreviewException("Pose transition changed retained actor/resource state.");
                }
                if(interiorSceneCharacter.MovementTransitioning)throw new AssetPreviewException("Pose transition did not finish.");
            }
            interiorSceneCharacter.SetMovementFrame("walk",content.Character!.AdditionalAnimations!["walk"].EndFrame*.25,.05);string interruptedPose=interiorSceneCharacter.GeometryFingerprint();
            interiorSceneCharacter.SetMovementFrame("run",content.Character.AdditionalAnimations["run"].EndFrame*.25,0);
            if(interruptedPose!=interiorSceneCharacter.GeometryFingerprint())throw new AssetPreviewException("Interrupted GPU pose transition was discontinuous.");
            GD.Print("PASS: original movement pose transitions;100 retained GPU blend updates interrupted_continuity=true no actor/door/root/resource mutation duration=.15 explicit modern policy");
            string pose=interiorSceneCharacter.GeometryFingerprint();
            try{interiorSceneCharacter.SetClipFrame("absent",0);throw new AssetPreviewException("Missing movement clip accepted.");}catch(AssetPreviewException exception)when(exception.Message=="Prepared movement clip required."){}
            try{interiorSceneCharacter.SetClipFrame("walk",double.NaN);throw new AssetPreviewException("Invalid movement frame accepted.");}catch(System.IO.InvalidDataException){}
            if(pose!=interiorSceneCharacter.GeometryFingerprint())throw new AssetPreviewException("Invalid movement clip/frame changed published pose.");
            Configure(true);permission.ButtonPressed=true;
            var speedActor=interiorScene!.State;runSpeed.ButtonPressed=true;if(Math.Abs(InputState().WorldVelocity.Length())>1e-6||!ReferenceEquals(speedActor,interiorScene.State))throw new AssetPreviewException("Run control moved an idle actor.");
            w=true;if(Math.Abs(InputState().WorldVelocity.Length()-5.2f)>1e-6)throw new AssetPreviewException("Run control speed selection failed.");runSpeed.ButtonPressed=false;if(Math.Abs(InputState().WorldVelocity.Length()-1.6f)>1e-6)throw new AssetPreviewException("Walk speed restoration failed.");w=false;
            for(int i=0;i<12;i++)Manual(NVector.Zero,.1f);for(int i=0;i<6;i++)Manual(direction*.26f);
            if(!gaitTicks.TryGetValue(PreviewLocomotionMode.Run,out int runs)||runs==0||!gaitTicks.ContainsKey(PreviewLocomotionMode.Walk)||!gaitTicks.ContainsKey(PreviewLocomotionMode.Idle))throw new AssetPreviewException("Accepted route did not exercise all locomotion modes.");
            var held=locomotion.State;characterPlay!.EmitSignal(BaseButton.SignalName.Toggled,false);Manual(NVector.Zero);if(!ReferenceEquals(held,locomotion.State))throw new AssetPreviewException("Movement animation pause changed phase.");characterPlay.EmitSignal(BaseButton.SignalName.Toggled,true);
            Manual(NVector.Zero);if(locomotion.State.Mode!=PreviewLocomotionMode.Idle)throw new AssetPreviewException("Stationary accepted tick did not select idle.");
            GD.Print($"PASS: original movement playback;480 retained clip updates gait_ticks={string.Join(',',gaitTicks.Select(p=>$"{p.Key}:{p.Value}"))} actor_feet_authoritative=true root_translation_inert=true heading_reflected=true pause_reset=true cache={textureCount}/{textureBytes}");
        }
        if(fixture&&chaseRig is not null)
        {
            int views=0,obstructed=0;float farthest=0;
            foreach(bool rotated in new[]{false,true})
            {
                Configure(rotated);follow.ButtonPressed=true;thirdPerson.ButtonPressed=true;
                var actorBefore=interiorScene!.State;string topology=interiorSceneCharacter!.GeometryFingerprint(false);var rootBefore=interiorSceneCharacter.Root.Transform;
                foreach(float yaw in new[]{0f,MathF.PI*.5f,MathF.PI,-MathF.PI*.5f})foreach(float pitch in new[]{-.35f,.35f})foreach(int setting in new[]{0,2,7,14})
                {
                    cameraRecovery.Reset();orientation=new(yaw,pitch);cameraZoom.Select(setting);Refresh();views++;
                    if(cameraBoom is null||camera.GlobalPosition.DistanceTo(new(cameraBoom.Position.X,cameraBoom.Position.Y,-cameraBoom.Position.Z))>1e-5f||!ReferenceEquals(actorBefore,interiorScene.State)||topology!=interiorSceneCharacter.GeometryFingerprint(false)||rootBefore!=interiorSceneCharacter.Root.Transform)throw new AssetPreviewException("Original camera query/owner/reflection/state boundary failed.");
                    if(cameraBoom.Distance>cameraBoom.RequestedDistance||cameraBoom.Distance<0)throw new AssetPreviewException("Camera clearance escaped selected boom.");
                    if(cameraBoom.Obstacle!=ChaseCameraObstacle.None)obstructed++;farthest=Math.Max(farthest,cameraBoom.Distance);
                    if(interiorSceneCharacter.Root.Visible!=(cameraBoom.Distance>cameraProfile!.FirstPersonThreshold))throw new AssetPreviewException("Camera near-mode character visibility failed.");
                    if(cameraCell!.Status==CameraCellStatus.Located)
                    {
                        if(presentedVisibility is null||!presentedVisibility.Cells.Contains(cameraCell.CellIndex!.Value)||roomNodes!.Any(pair=>pair.Value.Visible!=presentedVisibility.Cells.Contains(pair.Key)))throw new AssetPreviewException("Camera-cell portal visibility publication failed.");
                    }
                    else if(roomNodes!.Values.Any(node=>!node.Visible))throw new AssetPreviewException("Unresolved camera-cell fallback hid an owned room.");
                }
                thirdPerson.ButtonPressed=false;Refresh();if(cameraBoom is not null||interiorSceneCharacter.Root.Visible)throw new AssetPreviewException("First-person restoration failed.");
                follow.ButtonPressed=false;Refresh();if(!interiorSceneCharacter.Root.Visible)throw new AssetPreviewException("Overview character restoration failed.");
            }
            if(obstructed==0)throw new AssetPreviewException("Original chase fixtures did not encounter room geometry.");
            Configure(false);orientation=new();thirdPerson.ButtonPressed=true;follow.ButtonPressed=true;cameraZoom.Select(0);Refresh();cameraZoom.Select(7);Refresh();
            var recoveryActor=interiorScene!.State;string recoveryGeometry=interiorSceneCharacter!.GeometryFingerprint(false);var recoveryRoot=interiorSceneCharacter.Root.Transform;
            float previousDistance=cameraBoom!.Distance;
            for(int step=0;step<20;step++)
            {
                Refresh(.05);
                if(cameraBoom!.Distance<previousDistance||cameraBoom.Distance>cameraRecovery.State!.SafeDistance||!ReferenceEquals(recoveryActor,interiorScene.State)||recoveryGeometry!=interiorSceneCharacter.GeometryFingerprint(false)||recoveryRoot!=interiorSceneCharacter.Root.Transform)throw new AssetPreviewException("Camera release changed collision/actor/resource boundaries.");
                previousDistance=cameraBoom.Distance;
            }
            if(previousDistance<4.9f)throw new AssetPreviewException("Camera release did not approach selected clear distance.");
            orientation=new(MathF.PI,0);Refresh(0,false);if(cameraBoom!.Obstacle==ChaseCameraObstacle.None||cameraBoom.Distance!=cameraRecovery.State!.SafeDistance||cameraBoom.Distance>=previousDistance)throw new AssetPreviewException("Paused original door obstruction did not retract immediately.");
            float heldDistance=cameraBoom.Distance;orientation=new();Refresh(.1,false);if(cameraBoom!.Distance!=heldDistance)throw new AssetPreviewException("Paused camera expanded after release.");Refresh(.1,true);if(cameraBoom!.Distance<=heldDistance)throw new AssetPreviewException("Active camera did not recover after obstruction release.");
            GD.Print("PASS: original camera distance recovery;20 release steps monotonic/bounded=true paused_collision_retracts=true paused_release_holds=true actor_resources_unchanged=true time_constant=0.2");
            int differentCameraCells=0;
            foreach(bool rotated in new[]{false,true})
            {
                Configure(rotated);orientation=new();cameraZoom.Select(7);thirdPerson.ButtonPressed=true;follow.ButtonPressed=true;
                for(int step=0;step<120&&interiorScene!.State.Locator.CellIndex==first.Start.CellIndex;step++)Manual(direction*.08f);
                Refresh();
                if(interiorScene!.State.Locator.CellIndex==first.Start.CellIndex||cameraCell?.Status!=CameraCellStatus.Located||cameraCell.CellIndex==interiorScene.State.Locator.CellIndex||cameraCell.CellIndex!=first.Start.CellIndex||presentedVisibility is null||!presentedVisibility.Cells.Contains(cameraCell.CellIndex.Value))throw new AssetPreviewException("Camera remained attached to actor cell after original doorway crossing.");
                differentCameraCells++;
                int doorLightCountBefore=interiorDoorLighting?.OwnerPublications??0;int lightCountBefore=lightingPublications;int roomLightCountBefore=interiorRoomLighting?.OwnerPublications??0;
                var stateBefore=interiorScene.State;portalVisibility.ButtonPressed=false;Refresh();if(roomNodes!.Values.Any(n=>!n.Visible)||!ReferenceEquals(stateBefore,interiorScene.State))throw new AssetPreviewException("Camera visibility toggle changed actor/fallback.");portalVisibility.ButtonPressed=true;Refresh();
                if(doorLightingRequested&&interiorDoorLighting!.OwnerPublications!=doorLightCountBefore)throw new AssetPreviewException("Camera visibility republished door lighting.");
                if(roomLightingRequested&&interiorRoomLighting!.OwnerPublications!=roomLightCountBefore)throw new AssetPreviewException("Camera visibility republished room lighting.");
                if(cellLightingRequested&&(lightingCell!=interiorScene.State.Locator.CellIndex||lightingCell==cameraCell!.CellIndex||lightingPublications!=lightCountBefore))throw new AssetPreviewException("Camera visibility changed actor-owned light selection.");
            }
            GD.Print($"PASS: camera/actor cells differ after original doorway crossing;owner_poses={differentCameraCells} portal_visibility_toggle_state_inert=true");
            Configure(true);orientation=new();cameraZoom.Select(cameraProfile!.SelectSetting(-1));thirdPerson.ButtonPressed=true;follow.ButtonPressed=true;Refresh();
            GD.Print($"PASS: original-profile chase camera;views={views} obstructed={obstructed} maximum_clear_distance={farthest} room_triangles={chaseRig.RoomTriangles} live_door_boxes=2 reflected_owner=true actor_resources_unchanged=true first/third/overview=true camera_cell_independent=true portal_selection_from_camera=true");
        }
        if(DisplayServer.GetName()=="headless"){GetTree().Quit();return;}
        TabContainer? characterEditTabs=null;
        if(characterCustomized||characterShapeProfile is not null||characterTextureProfile is not null){characterEditTabs=new TabContainer{CustomMinimumSize=new(370,480)};panel.AddChild(characterEditTabs);}
        VBoxContainer CharacterEditPanel(string name)
        {
            var inner=new ScrollContainer{Name=name};characterEditTabs!.AddChild(inner);var contentPanel=new VBoxContainer{SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};inner.AddChild(contentPanel);return contentPanel;
        }
        if(characterCustomized)
        {
            var ui=InstallCustomizationControls(CharacterEditPanel("Colors"),content.Character!,interiorSceneCharacter!,fixture,()=>interiorScene!.State);
            if(fixture)await VerifyCustomizationControls(interiorSceneCharacter!,content.Character!,ui);
        }
        if(characterShapeProfile is not null)
        {
            var ui=InstallShapeControls(CharacterEditPanel("Shape"),content.Character!,interiorSceneCharacter!,fixture,()=>interiorScene!.State);
            if(fixture){VerifyShapeControls(interiorSceneCharacter!,content.Character!,ui);characterEditTabs!.CurrentTab=characterEditTabs.GetTabCount()-1;}
        }
        if(!fixture&&characterTextureProfile is not null)InstallTextureChoiceControls(CharacterEditPanel("Details"),content.Character!,interiorSceneCharacter!,false,()=>interiorScene!.State);
        if(fixture)
        {
            if(characterTextureProfile is not null){var ui=InstallTextureChoiceControls(CharacterEditPanel("Details"),content.Character!,interiorSceneCharacter!,fixture,()=>interiorScene!.State);await VerifyTextureChoiceControls(interiorSceneCharacter!,content.Character!,ui);characterEditTabs!.CurrentTab=characterEditTabs.GetTabCount()-1;}
            if(characterNormalDiffuse)await SkeletalNormalDiffusePreview.VerifyGpuBoundary(this);
            if(characterSpecular){await SkeletalSpecularPreview.VerifyGpuBoundary(this,vertexVaryings:true);await SkeletalSpecularPreview.VerifyGpuBoundary(this,true);await StaticDoorVaryingGpuScenarios.Verify(this,true);if(args.Contains("--point-color-gpu-probe"))await StaticDoorVaryingGpuScenarios.Verify(this,true,true);}
            if(doorLightingRequested)
            {
                await StaticDoorLightingPreview.VerifyGpuBoundary(this);interiorDoorLighting!.Verify();
                GD.Print($"PASS: original retained door normal lighting surfaces={interiorDoorLighting.SurfaceCount} materials={interiorDoorLighting.MaterialCount} fallback_vertices={interiorDoorLighting.FallbackVertices} owner_publications={interiorDoorLighting.OwnerPublications} shared_meshes_unchanged=true camera_visibility_independent=true");
            }
            if(roomLightingRequested)
            {
                await StaticRoomLightingPreview.VerifyGpuBoundary(this);interiorRoomLighting!.Verify();
                GD.Print($"PASS: original retained room lighting cells=3 surfaces={interiorRoomLighting.SurfaceCount} materials={interiorRoomLighting.MaterialCount} owner_publications={interiorRoomLighting.OwnerPublications} shared_meshes_unchanged=true camera_visibility_independent=true");
            }
            if(cellLightingRequested)
            {
                await SkeletalSpecularPreview.VerifyGpuBoundary(this,false,false,true,vertexVaryings:true);
                await SkeletalSpecularPreview.VerifyGpuBoundary(this,true,false,true);
                await StaticVertexColorPreview.VerifyGpuBoundary(this);
                await CellLightingGpuPreview.VerifyGpuBoundary(this);
                if(useRecoveredLightSelection)await CellLightingGpuPreview.VerifyGpuBoundary(this,true);
                await SkeletalSpecularPreview.VerifyGpuBoundary(this,false,true,vertexVaryings:true);
                if(lightingVisited.Count!=3||lightingPublications<3||lightingCell!=interiorScene!.State.Locator.CellIndex)throw new AssetPreviewException("Original owner-cell light transfer coverage failed.");
                GD.Print($"PASS: original owner-cell lighting cells={lightingVisited.Count} publications={lightingPublications} retained_geometry_material_pose=true camera_cell_independent=true");
            }
            if(characterAlbedoFactors)await SkeletalAlbedoPreview.VerifyGpuBoundary(this);
            if(chaseRig is not null){thirdPerson.ButtonPressed=true;follow.ButtonPressed=true;Refresh();}
            else if(sphereVolume is not null)
            {
                // Bounded capture shows the selected collision volume using the existing overview visibility policy.
                follow.ButtonPressed=false;Refresh();
                var center=interiorScene!.State.WorldPosition+actorCenterOffset;
                var view=center+owner.TransformDirection(new(.6f,.8f,-2.5f));
                camera.Position=new(view.X,view.Y,-view.Z);camera.LookAt(new(center.X,center.Y,-center.Z));
            }
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            using var image=GetViewport().GetTexture().GetImage();if(args.Contains("--capture")&&image.SavePng(Read("--capture"))!=Error.Ok)throw new IOException("Multi-door capture failed.");
            GD.Print("PASS: original multi-door GPU rendering.");GetTree().Quit();
        }
    }
}
