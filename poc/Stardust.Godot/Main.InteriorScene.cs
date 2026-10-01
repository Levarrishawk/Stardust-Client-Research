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
    private SkeletalMeshPreview? interiorSceneCharacter;
    private OwnedBackgroundAssetLoad<InteriorSceneContentBundle>? interiorSceneLoad;
    private void DisposeInteriorScene()
    {
        interiorSceneLoad?.Dispose();interiorSceneLoad=null;
        if(actorMouseInput is not null){Input.MouseMode=Input.MouseModeEnum.Visible;actorMouseInput=null;}
        interiorSceneTimeline=null;interiorSceneHost=null;interiorScene?.Dispose();interiorScene=null;
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
                TreLookupResult Resolve(string path)
                {
                    token.ThrowIfCancellationRequested();
                    if(cancelTest&&!firstLookup.Task.IsCompleted){firstLookup.TrySetResult(true);token.WaitHandle.WaitOne(TimeSpan.FromSeconds(15));token.ThrowIfCancellationRequested();throw new InvalidOperationException("Cancellation fixture did not cancel pending lookup.");}
                    return overlay.Lookup(path);
                }
                var result=InteriorSceneContentBundleReader.Load(Resolve,specification,specPin,tablePin,member,layoutPin,loadRooms,loadMaterials,
                    actorTemplatePath:actorTemplate,actorTemplateSha256:actorTemplatePin,createTriangleGuard:triangleGuardRequested,
                    maximumCollisionTriangles:65536,characterAppearanceSha256:characterPin);token.ThrowIfCancellationRequested();return result;
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
        interiorSceneVisuals=new(content.Geometry,roomMaterials?.Meshes,allowMainTextureOnly:roomMaterials is not null);AddChild(interiorSceneVisuals.Root);
        var roomRoot=interiorSceneVisuals.Root.GetChild<Node3D>(0);var leafRoot=interiorSceneVisuals.Root.GetChild<Node3D>(1);
        for(int i=0;i<bundle.Doorways.Count;i++)interiorSceneLeaves.Add(leafRoot.GetChild<Node3D>(i));
        if(roomMaterials is not null)
        {
            foreach(string diagnostic in roomMaterials.Diagnostics.Concat(interiorSceneVisuals.MaterialDiagnostics))GD.Print("Scene material fallback: "+diagnostic);
            GD.Print($"PASS: scene MAIN texture inspection sources={roomMaterials.Sources.Count} textured_templates={interiorSceneVisuals.TexturedMeshTemplates} textured_surfaces={interiorSceneVisuals.TexturedSurfaces} neutral_surfaces={interiorSceneVisuals.NeutralSurfaces} omitted_surfaces={interiorSceneVisuals.OmittedSurfaces} gpu_textures={interiorSceneVisuals.UniqueGpuTextures} diagnostics={roomMaterials.Diagnostics.Count+interiorSceneVisuals.MaterialDiagnostics.Count}");
        }
        var roomNodes=rooms?.Geometry.Root.Children.Select((child,index)=>(child.AuthoredIndex,Node:roomRoot.GetChild<Node3D>(index))).ToDictionary(value=>value.AuthoredIndex,value=>value.Node);
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
            interiorSceneCharacter=new(content.Character,true);AddChild(interiorSceneCharacter.Root);interiorSceneCharacter.VerifyBoundary(!args.Contains("--headless"));
            GD.Print("PASS: owned interior neutral character; placement=world_feet animation=false generated_face=false");
        }
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
        if(characterRequested){panel.AddChild(new Label{Text="Neutral character shown in overview"});collisionMarker=new CheckBox{Text="Show collision sphere"};panel.AddChild(collisionMarker);}
        if(triangleGuardRequested)panel.AddChild(new Label{Text="Room geometry blocks movement"});
        var transformed=new CheckBox{Text="Rotate and translate building owner"};panel.AddChild(transformed);
        var follow=new CheckBox{Text="View inside the rooms",ButtonPressed=rooms is not null};if(rooms is not null)panel.AddChild(follow);
        var portalVisibility=new CheckBox{Text="Select rooms through portal openings",ButtonPressed=args.Contains("--portal-visibility")};if(rooms is not null)panel.AddChild(portalVisibility);
        var showFloors=new CheckBox{Text="Show floor inspection overlay",ButtonPressed=rooms is null};panel.AddChild(showFloors);
        showFloors.Toggled+=visible=>floorPreview.Root.Visible=visible;floorPreview.Root.Visible=showFloors.ButtonPressed;
        bool fixture=args.Contains("--asset-self-test")||args.Contains("--capture");
        var automatic=new CheckBox{Text="Continuous movement (W / A / S / D)",ButtonPressed=!fixture};panel.AddChild(automatic);
        var mouseLook=new CheckBox{Text="Mouse look (Tab toggles; Esc releases)"};if(rooms is not null)panel.AddChild(mouseLook);
        var paused=new CheckBox{Text="Pause simulation"};panel.AddChild(paused);
        var forward=new Button{Text="Step forward 0.08m"};var reverse=new Button{Text="Step backward 0.08m"};var wait=new Button{Text="Wait 0.1 seconds"};var reset=new Button{Text="Reset actor and both doors"};
        foreach(var button in new[]{forward,reverse,wait,reset})panel.AddChild(button);
        var status=new Label();panel.AddChild(status);
        AppearanceTransform owner=AppearanceTransform.Identity;bool w=false,s=false,a=false,d=false;var orientation=new ActorViewOrientation();
        PortalActorInput InputState()=>PlanarActorInputMapping.Create(owner.TransformDirection(orientation.PlanarForward(direction)),(w?1:0)-(s?1:0),(d?1:0)-(a?1:0),1.6f,permission.ButtonPressed);
        void Submit(long now)=>interiorSceneTimeline!.Submit(new(now,InputState()));
        void Overview()
        {
            var center=owner.TransformPoint((first.Start.LocalPosition+last.End)*.5f);var view=center+owner.TransformDirection(new(14,10,8));camera.Position=new(view.X,view.Y,-view.Z);camera.LookAt(new(center.X,center.Y+1,-center.Z));
        }
        void Refresh()
        {
            var state=interiorScene!.State;var p=state.WorldPosition;var bodyCenter=p+actorCenterOffset;actor.Position=new(bodyCenter.X,bodyCenter.Y,-bodyCenter.Z);
            actor.Visible=!follow.ButtonPressed&&(collisionMarker?.ButtonPressed??true);
            if(interiorSceneCharacter is not null){interiorSceneCharacter.Root.Position=new(p.X,p.Y,-p.Z);interiorSceneCharacter.Root.Visible=!follow.ButtonPressed;}
            if(follow.ButtonPressed){var view=p+NVector.UnitY*1.6f;var target=view+owner.TransformDirection(orientation.LookForward(direction))*4;camera.Position=new(view.X,view.Y,-view.Z);camera.LookAt(new(target.X,target.Y,-target.Z));}
            PortalCellVisibilityResult? visible=null;
            if(visibility is not null&&follow.ButtonPressed&&portalVisibility.ButtonPressed)
            {
                var size=GetViewport().GetVisibleRect().Size;visible=visibility.Query(state.Locator.CellIndex,new(state.Locator.LocalPosition+NVector.UnitY*1.6f,orientation.LookForward(direction),NVector.UnitY,Mathf.DegToRad(camera.Fov),size.X/size.Y,camera.Far));
            }
            if(roomNodes is not null)foreach(var pair in roomNodes)pair.Value.Visible=visible is null||visible.Cells.Contains(pair.Key);
            for(int i=0;i<interiorSceneLeaves.Count;i++)interiorSceneLeaves[i].Visible=visible is null||visible.Cells.Contains(bundle.Doorways[i].Cell)||visible.Cells.Contains(bundle.Doorways[i].TargetCell);
            for(int i=0;i<geometries.Length;i++)interiorSceneLeaves[i].Transform=GodotAppearanceTransform.ToGodot(state.Doors[geometries[i]].LeafPose);
            status.Text=$"Owner: {bundle.Layout.Cells[state.Locator.CellIndex].Name}\nTransfers: {state.TotalTransfers}; ticks: {state.Frame}\n"+
                string.Join("\n",geometries.Select((g,i)=>$"Door {i+1}: {state.Doors[g].Controller.Fraction:F3}; retained: {state.Doors[g].RetainedActors}{(state.Doors[g].ClosingHeld?"; waiting":"")}"))+
                $"\nMovement: {state.Movement?.Movement.Status.ToString()??"not moved"}\nContact: {state.Movement?.ContactKind?.ToString()??"none"}\nVisible rooms: {visible?.Cells.Count??rooms?.CellAppearances.Count??0}{(visible?.BudgetFallback==true?" (budget fallback)":"")}\nCatch-up time dropped: {interiorSceneHost!.State.DroppedMicroseconds/1000.0:F1} ms";
        }
        void Configure(bool rotated)
        {
            interiorScene?.Dispose();owner=rotated?new(new(0,0,1),NVector.UnitY,new(-1,0,0),new(20,4,-9)):AppearanceTransform.Identity;
            interiorScene=bundle.CreateActor(first.Start,owner,sphereVolume is null?new(UseLocalRayCoverage:true):null,holdClosing,sphereVolume,
                triangleGeometry,65536);
            interiorSceneHost=new(()=>interiorScene.State,(seconds,delta,entry)=>interiorScene.Tick(seconds,delta,l=>new(l.GeometryIndex==secondGeometry?entry:true,true)),interiorScene.Reset);
            mouseLook.ButtonPressed=false;orientation=new();w=s=a=d=false;long now=checked((long)Time.GetTicksUsec());interiorSceneTimeline=new(interiorSceneHost,now,InputState());
            interiorSceneTimeline.AdvanceTo(now,automatic.ButtonPressed&&!paused.ButtonPressed&&GetWindow().HasFocus());floorFrame.Transform=GodotAppearanceTransform.ToGodot(owner);floorPreview.SelectCell(-1);
            roomRoot.Transform=GodotAppearanceTransform.ToGodot(owner);
            Overview();Refresh();
        }
        void Manual(NVector localDelta,float seconds=.05f)
        {
            var from=interiorScene!.State.WorldPosition;var state=interiorScene.Tick(seconds,owner.TransformDirection(localDelta),l=>new(l.GeometryIndex==secondGeometry?permission.ButtonPressed:true,true));
            floorPreview.ShowActorWalk(from,state.Movement!);Refresh();
        }
        forward.Pressed+=()=>Manual(direction*.08f);reverse.Pressed+=()=>Manual(-direction*.08f);wait.Pressed+=()=>Manual(NVector.Zero,.1f);
        reset.Pressed+=()=>{mouseLook.ButtonPressed=false;orientation=new();w=s=a=d=false;long now=checked((long)Time.GetTicksUsec());interiorSceneTimeline!.Reset(now,InputState());floorPreview.SelectCell(-1);Refresh();};
        transformed.Toggled+=Configure;Configure(false);
        follow.Toggled+=_=>{if(!follow.ButtonPressed)mouseLook.ButtonPressed=false;mouseLook.Disabled=!follow.ButtonPressed||!interiorSceneHost!.State.Active;Overview();Refresh();};
        portalVisibility.Toggled+=_=>Refresh();
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
        actorUpdate=_=>
        {
            frame.Size=new(404,Math.Max(100,GetViewport().GetVisibleRect().Size.Y-36));
            bool active=automatic.ButtonPressed&&!paused.ButtonPressed&&GetWindow().HasFocus();bool nw=active&&Input.IsPhysicalKeyPressed(Key.W),ns=active&&Input.IsPhysicalKeyPressed(Key.S),na=active&&Input.IsPhysicalKeyPressed(Key.A),nd=active&&Input.IsPhysicalKeyPressed(Key.D);long now=checked((long)Time.GetTicksUsec());
            if(nw!=w||ns!=s||na!=a||nd!=d){w=nw;s=ns;a=na;d=nd;Submit(now);}var from=interiorScene!.State.WorldPosition;var clock=interiorSceneTimeline!.AdvanceTo(now,active);
            if(clock.StepsThisUpdate>0)floorPreview.ShowActorWalk(from,clock.Actor.Movement!);if(clock.FaultType is not null)automatic.ButtonPressed=false;Refresh();
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
        }
        if(DisplayServer.GetName()=="headless"){GetTree().Quit();return;}
        if(fixture)
        {
            if(sphereVolume is not null)
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
