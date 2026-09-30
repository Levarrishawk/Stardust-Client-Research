using System.Collections.Generic;
using System.Linq;
using Godot;
using Stardust.Core.Proof;
using Stardust.Core.Assets;
using System.IO;
public partial class Main : Node
{
    private StaticMeshPreview? assetPreview;
    private DetailAppearancePreview? detailPreview;
    private GeometryAppearancePreview? geometryPreview;
    private FloorMeshPreview? floorPreview;
    private PathGraphPreview? pathPreview;
    private System.Action<Vector2>? floorPick;
    public override void _UnhandledInput(InputEvent input){if(input is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex==MouseButton.Right){try{floorPick?.Invoke(mouse.Position);}catch(System.Exception failure){GD.PushError("Floor inspection failed: "+failure.GetType().Name);GetTree().Quit(1);}}}
    private System.Action? cameraLodUpdate;
    public override void _Process(double delta)
    {
        try{cameraLodUpdate?.Invoke();}
        catch(System.Exception failure){cameraLodUpdate=null;GD.PushError("Camera LOD checkpoint failed: "+failure.GetType().Name);GetTree().Quit(1);}
    }
    public override async void _Ready()
    {
        try
        {
        if (!System.Environment.Is64BitProcess) { GD.PushError("Stardust client requires a 64-bit host process"); GetTree().Quit(1); return; }
        IReadOnlyList<string> events = await OfflineHostedLoginScenario.RunAsync();
        foreach (string entry in events) GD.Print($"[core] {entry}");
        string[] args=OS.GetCmdlineUserArgs();
        if(args.Contains("--hdr-fixtures"))
        {
            int option=System.Array.IndexOf(args,"--hdr-fixtures");
            if(option+1>=args.Length || args.Count(value=>value=="--hdr-fixtures")!=1)throw new System.ArgumentException("HDR fixture directory required once.");
            DdsHdrVerification.Verify(args[option+1],DisplayServer.GetName()!="headless",args.Contains("--hdr-materials"));GetTree().Quit();return;
        }
        if(args.Contains("--dds-self-test")){await VerifyDdsFile(args);return;}
        if(args.Contains("--door-style")){if(args.Contains("--door-portal"))await BuildInteriorDoorPreview(args);else await BuildDoorPreview(args);return;}
        if(args.Contains("--palette-inspector")){await BuildPalettePreview(args);return;}
        if(args.Contains("--asset-interior")){await BuildInteriorPreview(args);return;}
        if(args.Contains("--asset-component")){await BuildComponentPreview(args);return;}
        if(args.Contains("--asset-detail")){await BuildDetailPreview(args);return;}
        if(args.Contains("--asset-root")){await BuildAssetPreview(args);return;}
        if (OS.GetCmdlineUserArgs().Contains("--self-test")) { GD.Print("PASS: Godot hosted the async offline login I/O boundary without a render window"); GetTree().Quit(); return; }
        BuildWorld(); BuildInterface(events);
        }
        catch (System.Exception failure)
        {
            if(failure is AssetPreviewException) GD.Print("Asset preview boundary: " + failure.Message);
            GD.PushError("Offline client checkpoint failed: " + failure.GetType().Name);
            GetTree().Quit(1);
        }
    }
    public override void _ExitTree(){DisposeInteriorDoorResources();doorRuntime?.Dispose();doorRuntime=null;pathPreview?.Dispose();pathPreview=null;floorPick=null;floorPreview?.Dispose();floorPreview=null;cameraLodUpdate=null;assetPreview?.Dispose();assetPreview=null;detailPreview?.Dispose();detailPreview=null;geometryPreview?.Dispose();geometryPreview=null;}
    private async System.Threading.Tasks.Task BuildPalettePreview(string[] args)
    {
        string Read(string key)
        {int i=System.Array.IndexOf(args,key);if(i<0 || i+1>=args.Length || args.Count(value=>value==key)!=1)throw new System.ArgumentException("Missing/duplicate palette option.");return args[i+1];}
        using StreamReader configuration=File.OpenText(Read("--asset-config"));CustomizableShaderPaletteBundle bundle;
        using(ConfiguredTreOverlay overlay=ConfiguredTreOverlay.Open(Read("--asset-root"),configuration))
            bundle=CustomizableShaderPaletteBundleReader.Load(overlay.Lookup,Read("--asset-member"),Read("--asset-sha256"));
        foreach(string diagnostic in bundle.Diagnostics)GD.Print("Palette inspection: "+diagnostic);
        var preview=new PaletteFactorPreview(bundle,Read("--asset-member"));var layer=new CanvasLayer();AddChild(layer);layer.AddChild(preview.Root);
        if(args.Contains("--capture") || args.Contains("--asset-self-test")){preview.VerifyControls();PaletteFactorPreview.VerifySharedControls(bundle,layer);}
        if(DisplayServer.GetName()=="headless"){GD.Print("PASS: headless palette inspector controls; GPU pixels unavailable");GetTree().Quit();return;}
        if(args.Contains("--capture") || args.Contains("--asset-self-test"))
        {
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            using Image image=GetViewport().GetTexture().GetImage();
            GD.Print($"PASS: palette inspector GPU swatches={preview.VerifyPixels(image,GetViewport().GetVisibleRect())}");
            if(args.Contains("--capture") && image.SavePng(Read("--capture"))!=Error.Ok)throw new IOException("Palette capture failed.");
            GetTree().Quit();
        }
    }
    private async System.Threading.Tasks.Task BuildComponentPreview(string[] args)
    {
        string Read(string key)
        {int i=System.Array.IndexOf(args,key);if(i<0 || i+1>=args.Length || args.Count(value=>value==key)!=1)throw new System.ArgumentException("Missing/duplicate component option.");return args[i+1];}
        using StreamReader configuration=File.OpenText(Read("--asset-config"));
        AppearanceGeometryGraph asset;
        TexturedAppearanceGraph? textured=null;
        using(ConfiguredTreOverlay overlay=ConfiguredTreOverlay.Open(Read("--asset-root"),configuration))
        {
            if(args.Contains("--component-textures"))
            {textured=TexturedAppearanceGraphReader.Load(overlay.Lookup,Read("--asset-member"),Read("--asset-sha256"));asset=textured.Geometry;}
            else asset=AppearanceGeometryGraphReader.Load(overlay.Lookup,Read("--asset-member"),Read("--asset-sha256"));
        }
        StaticMeshPreview.VerifyCoordinateBoundary();geometryPreview=new(asset,textured?.Meshes,args.Contains("--main-texture-only"),args.Contains("--inspect-hardpoints"));geometryPreview.VerifySwitches();AddChild(geometryPreview.Root);geometryPreview.VerifyCameraSwitches();
        if(args.Contains("--inspect-hardpoints"))geometryPreview.VerifyHardpointPlacements();
        Aabb bounds=geometryPreview.Bounds;
        GD.Print($"PASS: Godot component geometry meshes={geometryPreview.MeshInstances} sources={asset.Sources.Count} diagnostics={geometryPreview.Diagnostics} camera_selectors={geometryPreview.CameraSelectors}");
        if(textured is not null)
        {
            foreach(string diagnostic in textured.Diagnostics.Concat(geometryPreview.MaterialDiagnostics))GD.Print("Component material fallback: "+diagnostic);
            GD.Print($"PASS: component texture inspection sources={textured.Sources.Count} textured_templates={geometryPreview.TexturedMeshTemplates} neutral_templates={geometryPreview.NeutralMeshTemplates} textured_surfaces={geometryPreview.TexturedSurfaces} neutral_surfaces={geometryPreview.NeutralSurfaces} unique_gpu_textures={geometryPreview.UniqueGpuTextures} material_diagnostics={textured.Diagnostics.Count+geometryPreview.MaterialDiagnostics.Count}");
        }
        if(args.Contains("--asset-self-test") && DisplayServer.GetName()=="headless"){GetTree().Quit();return;}
        AddChild(new WorldEnvironment{Environment=new Environment{BackgroundMode=Environment.BGMode.Color,BackgroundColor=new Color("#14202e")}});
        PreviewOrbitCamera camera=new(){Current=true};AddChild(camera);camera.Configure(bounds.GetCenter(),System.Math.Max(bounds.Size.Length()*0.6f,1));
        CanvasLayer layer=new();AddChild(layer);
        ColorRect header=new(){Color=new Color(0.02f,0.04f,0.08f,0.92f),OffsetRight=1280,OffsetBottom=165};layer.AddChild(header);
        Label title=new(){Text="STARDUST — COMPONENT GEOMETRY INSPECTOR",Position=new Vector2(28,18)};title.AddThemeFontSizeOverride("font_size",24);header.AddChild(title);
        header.AddChild(new Label{Text=$"{geometryPreview.MeshInstances} mesh instances • {asset.Sources.Count} sources • {geometryPreview.Diagnostics} omitted-part diagnostics",Position=new Vector2(28,54)});
        Label distanceLabel=new(){Text="Selection distance: 0 (offline control)",Position=new Vector2(28,80)};header.AddChild(distanceLabel);
        HSlider slider=new(){Position=new Vector2(28,112),Size=new Vector2(580,24),MinValue=0,MaxValue=2000,Step=1};header.AddChild(slider);
        slider.ValueChanged+=distance=>{geometryPreview.Select((float)distance);distanceLabel.Text=$"Selection distance: {distance:0} (offline control)";};
        bool cameraMode=args.Contains("--camera-lod");
        CheckButton cameraControl=new(){Text="Use camera distance",Position=new Vector2(690,104),ButtonPressed=cameraMode};header.AddChild(cameraControl);slider.Editable=!cameraMode;
        cameraControl.Toggled+=enabled=>{cameraMode=enabled;slider.Editable=!enabled;if(!enabled){geometryPreview.Select((float)slider.Value);distanceLabel.Text=$"Selection distance: {slider.Value:0} (offline control)";}};
        cameraLodUpdate=()=>{if(cameraMode){geometryPreview.SelectFromCamera(camera.GlobalPosition);distanceLabel.Text=$"Camera/PIVT distance selects {geometryPreview.CameraSelectors} detail instances";}};
        if(args.Contains("--inspect-hardpoints"))
        {var control=new CheckButton{Text=$"Hardpoints ({geometryPreview.HardpointMarkers})",Position=new Vector2(1020,104),ButtonPressed=true};header.AddChild(control);control.Toggled+=geometryPreview.SetHardpointsVisible;header.OffsetBottom=205;AddHardpointSelector(header,geometryPreview,new Vector2(28,158));}
        layer.AddChild(new Label{Text=textured is null?"Neutral colors show geometry only; original materials, particles and lighting remain untranslated.":$"Unlit MAIN surfaces: {geometryPreview.TexturedSurfaces} textured / {geometryPreview.NeutralSurfaces} neutral; original lighting/translucency remain untranslated.",Position=new Vector2(28,650)});
        layer.AddChild(new Label{Text="Drag to orbit • Scroll to zoom • R to reset",Position=new Vector2(28,680)});
        if(DisplayServer.GetName()!="headless" && (args.Contains("--capture") || args.Contains("--asset-self-test")))
        {await StaticMeshPreview.VerifyGpuUvBoundary(this).WaitAsync(System.TimeSpan.FromSeconds(15));GD.Print("PASS: GPU component host coordinate fixture");}
        if(args.Contains("--capture"))
        {
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            using Image image=GetViewport().GetTexture().GetImage();
            if(image.SavePng(Read("--capture"))!=Error.Ok)throw new IOException("Component capture failed.");
            GD.Print("PASS: component geometry inspector captured");GetTree().Quit();
        }
        else if(args.Contains("--asset-self-test"))GetTree().Quit();
    }
    private async System.Threading.Tasks.Task BuildInteriorPreview(string[] args)
    {
        string Read(string key)
        {int i=System.Array.IndexOf(args,key);if(i<0 || i+1>=args.Length || args.Count(value=>value==key)!=1)throw new System.ArgumentException("Missing/duplicate interior option.");return args[i+1];}
        using StreamReader configuration=File.OpenText(Read("--asset-config"));
        PortalSceneBundle scene;TexturedPortalScene? textured=null;
        using(ConfiguredTreOverlay overlay=ConfiguredTreOverlay.Open(Read("--asset-root"),configuration))
        {
            if(args.Contains("--interior-textures"))
            {textured=TexturedPortalSceneReader.Load(overlay.Lookup,Read("--asset-member"),Read("--asset-sha256"),loadFloors:args.Contains("--interior-floors"),loadPathGraphs:args.Contains("--interior-paths"),customizationMode:args.Contains("--authored-texture-defaults")?CustomizableShaderInspectionMode.AuthoredTextureDefaults:CustomizableShaderInspectionMode.Reject);scene=textured.Scene;}
            else scene=PortalSceneBundleReader.Load(overlay.Lookup,Read("--asset-member"),Read("--asset-sha256"),loadFloors:args.Contains("--interior-floors"),loadPathGraphs:args.Contains("--interior-paths"));
        }
        AppearanceGeometryGraph asset=scene.Geometry;
        if(args.Contains("--interior-paths")){pathPreview=new(scene);AddChild(pathPreview.Root);}
        if(args.Contains("--interior-floors")){floorPreview=new(scene.CellFloors,scene.CellPathGraphs,scene.Layout);AddChild(floorPreview.Root);floorPreview.VerifyQueries();}
        StaticMeshPreview.VerifyCoordinateBoundary();geometryPreview=new(asset,textured?.Materials.Meshes,args.Contains("--main-texture-only"),args.Contains("--inspect-hardpoints"));geometryPreview.VerifySwitches();AddChild(geometryPreview.Root);geometryPreview.VerifyCameraSwitches();
        if(args.Contains("--inspect-hardpoints"))geometryPreview.VerifyHardpointPlacements();
        if(textured is not null)
        {
            foreach(string diagnostic in textured.Diagnostics.Concat(geometryPreview.MaterialDiagnostics))GD.Print("Interior material fallback: "+diagnostic);
            GD.Print($"PASS: interior texture inspection sources={textured.Sources.Count} textured_templates={geometryPreview.TexturedMeshTemplates} neutral_templates={geometryPreview.NeutralMeshTemplates} textured_surfaces={geometryPreview.TexturedSurfaces} neutral_surfaces={geometryPreview.NeutralSurfaces} unique_gpu_textures={geometryPreview.UniqueGpuTextures} material_diagnostics={textured.Diagnostics.Count+geometryPreview.MaterialDiagnostics.Count}");
        }
        Aabb bounds=geometryPreview.Bounds;
        GD.Print($"PASS: Godot interior geometry meshes={geometryPreview.MeshInstances} sources={asset.Sources.Count} diagnostics={geometryPreview.Diagnostics} camera_selectors={geometryPreview.CameraSelectors}");
        AddChild(new WorldEnvironment{Environment=new Environment{BackgroundMode=Environment.BGMode.Color,BackgroundColor=new Color("#14202e")}});
        PreviewOrbitCamera camera=new(){Current=true};AddChild(camera);camera.Configure(bounds.GetCenter(),System.Math.Max(bounds.Size.Length()*0.6f,1));
        CanvasLayer layer=new();AddChild(layer);
        ColorRect header=new(){Color=new Color(0.02f,0.04f,0.08f,0.92f),OffsetRight=1280,OffsetBottom=235};layer.AddChild(header);
        Label title=new(){Text="STARDUST — INTERIOR CELL INSPECTOR",Position=new Vector2(28,18)};title.AddThemeFontSizeOverride("font_size",24);header.AddChild(title);
        header.AddChild(new Label{Text=$"{geometryPreview.MeshInstances} mesh instances • {textured?.Sources.Count??asset.Sources.Count} sources • {geometryPreview.Diagnostics} omitted-part diagnostics",Position=new Vector2(28,54)});
        Label distanceLabel=new(){Text="Selection distance: 0 (offline control)",Position=new Vector2(28,80)};header.AddChild(distanceLabel);
        HSlider slider=new(){Position=new Vector2(28,112),Size=new Vector2(580,24),MinValue=0,MaxValue=2000,Step=1};header.AddChild(slider);
        slider.ValueChanged+=distance=>{geometryPreview.Select((float)distance);distanceLabel.Text=$"Selection distance: {distance:0} (offline control)";};
        bool cameraMode=args.Contains("--camera-lod");
        CheckButton cameraControl=new(){Text="Use camera distance",Position=new Vector2(690,104),ButtonPressed=cameraMode};header.AddChild(cameraControl);slider.Editable=!cameraMode;
        cameraControl.Toggled+=enabled=>{cameraMode=enabled;slider.Editable=!enabled;if(!enabled){geometryPreview.Select((float)slider.Value);distanceLabel.Text=$"Selection distance: {slider.Value:0} (offline control)";}};
        cameraLodUpdate=()=>{if(cameraMode){geometryPreview.SelectFromCamera(camera.GlobalPosition);distanceLabel.Text=$"Camera/PIVT distance selects {geometryPreview.CameraSelectors} detail instances";}};
        if(args.Contains("--inspect-hardpoints"))
        {var control=new CheckButton{Text=$"Hardpoints ({geometryPreview.HardpointMarkers})",Position=new Vector2(1020,104),ButtonPressed=true};header.AddChild(control);control.Toggled+=geometryPreview.SetHardpointsVisible;AddHardpointSelector(header,geometryPreview,new Vector2(690,198));}
        OptionButton cells=new(){Position=new Vector2(28,155),Size=new Vector2(580,32)};header.AddChild(cells);cells.AddItem("All loaded cells",int.MaxValue);
        int[] cellIndices=scene.Geometry.Root.Children.Select(child=>child.AuthoredIndex).ToArray();
        for(int i=0;i<cellIndices.Length;i++)cells.AddItem($"{cellIndices[i]}: {scene.Layout.Cells[cellIndices[i]].Name}",cellIndices[i]);
        System.Action<int>? selectPath=null;
        System.Action<FloorPathHit>? selectSurfaceRoute=null;
        void SelectCell(int selected)
        {
            int cell=selected==0?-1:cells.GetItemId(selected);floorPreview?.SelectCell(cell);selectPath?.Invoke(cell);
            for(int i=0;i<cellIndices.Length;i++)((Node3D)geometryPreview.Root.GetChild(i)).Visible=cell<0 || cellIndices[i]==cell;
            int visible=geometryPreview.Root.GetChildren().Cast<Node3D>().Count(node=>node.Visible);
            if(visible!=(cell<0?cellIndices.Length:1))throw new AssetPreviewException("Cell selection visibility mismatch.");
            Aabb selectedBounds=cell<0?bounds:GeometryAppearancePreview.BoundsFor((Node3D)geometryPreview.Root.GetChild(System.Array.IndexOf(cellIndices,cell)));
            camera.Configure(selectedBounds.GetCenter(),System.Math.Max(selectedBounds.Size.Length()*0.6f,1));
        }
        for(int i=0;i<cells.ItemCount;i++)SelectCell(i);SelectCell(0);
        cells.ItemSelected+=selected=>SelectCell((int)selected);
        if(args.Contains("--cell"))
        {
            int cell=int.Parse(Read("--cell"),System.Globalization.CultureInfo.InvariantCulture);int selected=cell==-1?0:cells.GetItemIndex(cell);
            if(selected<0)throw new AssetPreviewException("Requested cell has no loaded appearance.");cells.Select(selected);SelectCell(selected);
        }
        if(floorPreview is not null)
        {
            bool floorOnly=args.Contains("--floor-only");floorPreview.Root.Visible=floorOnly;geometryPreview.Root.Visible=!floorOnly;
            CheckButton floorControl=new(){Text="Floor only",Position=new Vector2(690,155),ButtonPressed=floorOnly};header.AddChild(floorControl);
            floorControl.Toggled+=enabled=>{floorOnly=enabled;floorPreview.Root.Visible=enabled;geometryPreview.Root.Visible=!enabled;};
            Label queryLabel=new(){Text=pathPreview is null?"Right-click to inspect a solid floor surface":"Right-click a solid surface to set the same-cell route start",Position=new Vector2(28,620)};layer.AddChild(queryLabel);
            CheckButton movementControl=new(){Text="Walk between picks",Position=new Vector2(850,155)};header.AddChild(movementControl);FloorPathHit? movementStart=null;
            CheckButton transferControl=new(){Text="Portal transfer",Position=new Vector2(690,198)};header.AddChild(transferControl);
            CheckButton entryControl=new(){Text="Entry allowed",Position=new Vector2(865,198),ButtonPressed=true};header.AddChild(entryControl);
            CheckButton barrierControl=new(){Text="Barrier open",Position=new Vector2(1050,198),ButtonPressed=true};header.AddChild(barrierControl);
            CheckButton actorControl=new(){Text="Actor bounds",Position=new Vector2(1050,155)};header.AddChild(actorControl);
            actorControl.Toggled+=_=>movementStart=null;
            transferControl.Toggled+=enabled=>{movementStart=null;if(enabled){cells.Select(0);SelectCell(0);}queryLabel.Text=enabled?"Offline portal policy • show all cells • pick start then destination":"Single-cell movement • pick start then destination";};
            movementControl.Toggled+=enabled=>{movementStart=null;floorPreview.SelectCell(cells.Selected==0?-1:cells.GetItemId(cells.Selected));queryLabel.Text=enabled?"Right-click a start, then a destination in the same cell":"Right-click to inspect a solid floor surface";};
            cells.ItemSelected+=_=>movementStart=null;
            void ApplyFloorSelection(FloorPathHit hit)
            {
                if(movementControl.ButtonPressed)
                {
                    if(movementStart is null){movementStart=hit;queryLabel.Text=$"Movement start: cell {hit.CellIndex}, triangle {hit.Surface.TriangleIndex} • pick destination";}
                    else{queryLabel.Text=transferControl.ButtonPressed?floorPreview.WalkTransfer(movementStart,hit,new(entryControl.ButtonPressed,barrierControl.ButtonPressed),actorControl.ButtonPressed):floorPreview.Walk(movementStart,hit);movementStart=null;}
                }
                else selectSurfaceRoute?.Invoke(hit);
            }
            floorPick=position=>
            {
                Vector3 origin=camera.ProjectRayOrigin(position),end=origin+camera.ProjectRayNormal(position)*camera.Far;queryLabel.Text=floorPreview.Pick(origin,end);
                if(floorPreview.LastSelection is not FloorPathHit hit)return;
                ApplyFloorSelection(hit);
                if(!movementControl.ButtonPressed)queryLabel.Text=floorPreview.Pick(origin,end);
            };
            if(floorPreview.SampleSelections.Count>0)
            {
                FloorPathHit sample=floorPreview.SampleSelections[0];movementControl.ButtonPressed=true;ApplyFloorSelection(sample);ApplyFloorSelection(sample);
                if(movementStart is not null || floorPreview.LastWalk?.Status!=FloorWalkStatus.Accepted || !queryLabel.Text.StartsWith("Movement: Accepted",System.StringComparison.Ordinal))throw new AssetPreviewException("Two-pick floor movement control mismatch.");
                movementControl.ButtonPressed=false;GD.Print("PASS: two-pick floor movement controls and reset");
            }
            if(floorPreview.SampleTransfers.Count>0)
            {
                int restore=cells.Selected;var sample=floorPreview.SampleTransfers[0];transferControl.ButtonPressed=true;movementControl.ButtonPressed=true;
                ApplyFloorSelection(sample.From);entryControl.ButtonPressed=false;ApplyFloorSelection(sample.To);
                if(floorPreview.LastTransfer?.Status!=CellFloorWalkStatus.EntryDenied || movementStart is not null)throw new AssetPreviewException("Current portal entry control mismatch.");
                entryControl.ButtonPressed=true;ApplyFloorSelection(sample.From);ApplyFloorSelection(sample.To);
                if(floorPreview.LastTransfer?.Status!=CellFloorWalkStatus.Accepted || movementStart is not null)throw new AssetPreviewException("Portal transfer control failed to recover after permission change.");
                barrierControl.ButtonPressed=false;ApplyFloorSelection(sample.From);ApplyFloorSelection(sample.To);
                if(floorPreview.LastTransfer?.Status!=CellFloorWalkStatus.PortalBlocked)throw new AssetPreviewException("Current portal barrier control mismatch.");
                barrierControl.ButtonPressed=true;transferControl.ButtonPressed=false;movementControl.ButtonPressed=false;cells.Select(restore);SelectCell(restore);
                GD.Print("PASS: two-pick portal transfer controls, current entry/barrier changes and reset");
                if(floorPreview.SampleBarrierTransfers.Count>0)
                {
                var bodySample=floorPreview.SampleBarrierTransfers[0];
                transferControl.ButtonPressed=true;movementControl.ButtonPressed=true;actorControl.ButtonPressed=true;entryControl.ButtonPressed=false;
                ApplyFloorSelection(bodySample.From);ApplyFloorSelection(bodySample.To);
                if(floorPreview.LastBarrier?.Contact is null || floorPreview.LastTransfer?.Locator.CellIndex!=bodySample.From.CellIndex || movementStart is not null)throw new AssetPreviewException("Actor bounds controls failed to retain barrier contact owner.");
                entryControl.ButtonPressed=true;ApplyFloorSelection(bodySample.From);ApplyFloorSelection(bodySample.To);
                if(floorPreview.LastTransfer?.Status!=CellFloorWalkStatus.Accepted || floorPreview.LastBarrier?.Contact is not null)throw new AssetPreviewException("Actor bounds controls failed to recover after permission change.");
                actorControl.ButtonPressed=false;transferControl.ButtonPressed=false;movementControl.ButtonPressed=false;cells.Select(restore);SelectCell(restore);
                GD.Print("PASS: two-pick actor bounds controls, permission recovery and reset");
                }
            }
        }
        if(pathPreview is not null)
        {
            SpinBox startNode=new(){Position=new Vector2(28,198),Size=new Vector2(140,30),Step=1};SpinBox endNode=new(){Position=new Vector2(180,198),Size=new Vector2(140,30),Step=1};header.AddChild(startNode);header.AddChild(endNode);
            Label routeLabel=new(){Position=new Vector2(28,590)};layer.AddChild(routeLabel);bool updating=false;
            void Route(){if(!updating)routeLabel.Text=pathPreview.ShowRoute((int)startNode.Value,(int)endNode.Value);}
            startNode.ValueChanged+=_=>Route();endNode.ValueChanged+=_=>Route();
            selectPath=cell=>{updating=true;pathPreview.SelectCell(cell);startNode.MaxValue=endNode.MaxValue=System.Math.Max(pathPreview.CurrentNodes-1,0);startNode.Value=0;endNode.Value=endNode.MaxValue;updating=false;Route();};
            selectSurfaceRoute=hit=>{if(!hit.NearestNode.HasValue)return;int item=cells.GetItemIndex(hit.CellIndex);if(item<0)return;cells.Select(item);SelectCell(item);startNode.Value=hit.NearestNode.Value;Route();};
            selectPath(cells.Selected==0?-1:cells.GetItemId(cells.Selected));
            if(floorPreview is not null)
            {
                int restore=cells.Selected,verified=0;
                foreach(var hit in floorPreview.SampleSelections.Where(hit=>hit.NearestNode.HasValue))
                {selectSurfaceRoute(hit);if(cells.GetItemId(cells.Selected)!=hit.CellIndex || startNode.Value!=hit.NearestNode!.Value || pathPreview.CurrentNodes<=hit.NearestNode.Value)throw new AssetPreviewException("Surface-to-route control cell/reflection mismatch.");verified++;}
                cells.Select(restore);SelectCell(restore);GD.Print($"PASS: surface-to-route controls same_cell_probes={verified}");
            }
            if(args.Contains("--route-from"))startNode.Value=int.Parse(Read("--route-from"),System.Globalization.CultureInfo.InvariantCulture);
            if(args.Contains("--route-to"))endNode.Value=int.Parse(Read("--route-to"),System.Globalization.CultureInfo.InvariantCulture);
            header.AddChild(new Label{Text="Route node ordinals (from → to)",Position=new Vector2(350,203)});
        }
        GD.Print($"PASS: manual cell selection verified cells={cellIndices.Length} portal_connections={scene.Layout.Connections.Count}");
        layer.AddChild(new Label{Text=textured is null?"Neutral geometry/floor inspection • Manual cell visibility • Portal culling, navigation, doors and lighting remain unimplemented.":$"Unlit MAIN: {geometryPreview.TexturedSurfaces} textured / {geometryPreview.NeutralSurfaces} neutral • "+(args.Contains("--authored-texture-defaults")?"Authored texture defaults • Palette colors and original lighting omitted.":"Original lighting omitted • Manual cell visibility."),Position=new Vector2(28,650)});
        layer.AddChild(new Label{Text="Drag to orbit • Scroll to zoom • R to reset",Position=new Vector2(28,680)});
        if(args.Contains("--asset-self-test") && DisplayServer.GetName()=="headless"){GetTree().Quit();return;}
        if(DisplayServer.GetName()!="headless" && (args.Contains("--capture") || args.Contains("--asset-self-test")))
        {await StaticMeshPreview.VerifyGpuUvBoundary(this).WaitAsync(System.TimeSpan.FromSeconds(15));GD.Print("PASS: GPU interior host coordinate fixture");}
        if(args.Contains("--capture"))
        {
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            using Image image=GetViewport().GetTexture().GetImage();
            if(image.SavePng(Read("--capture"))!=Error.Ok)throw new IOException("Component capture failed.");
            GD.Print("PASS: interior cell inspector captured");GetTree().Quit();
        }
        else if(args.Contains("--asset-self-test"))GetTree().Quit();
    }
    private async System.Threading.Tasks.Task VerifyDdsFile(string[] args)
    {
        string Read(string key)
        {int i=System.Array.IndexOf(args,key);if(i<0 || i+1>=args.Length || args.Count(value=>value==key)!=1)throw new System.ArgumentException("Missing/duplicate DDS test option.");return args[i+1];}
        string path=Read("--dds-self-test");
        if(new FileInfo(path).Length>67_108_864)throw new IOException("DDS test source over budget.");
        byte[] bytes=File.ReadAllBytes(path);
        string identity=System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
        if(!identity.Equals(Read("--dds-sha256"),System.StringComparison.OrdinalIgnoreCase))throw new IOException("DDS test identity mismatch.");
        DdsImage image=DdsDecoder.Decode(bytes);DdsGpuVerification.Verify(image);
        GD.Print($"PASS: DDS owned mip upload format={image.Format} mips={image.Mips.Count} sha256={identity} renderer={DisplayServer.GetName()}");
        if(DisplayServer.GetName()!="headless")
        {await StaticMeshPreview.VerifyGpuUvBoundary(this).WaitAsync(System.TimeSpan.FromSeconds(15));GD.Print("PASS: DDS upload host GPU UV fixture");}
        GetTree().Quit();
    }
    private static void AddHardpointSelector(Control parent,GeometryAppearancePreview preview,Vector2 position)
    {
        var selector=new OptionButton{Position=position,Size=new Vector2(550,30),TooltipText="Inspect one authored hardpoint label. Ordinals identify appearance instance and record; duplicate names remain separate."};parent.AddChild(selector);
        selector.AddItem("Hardpoint axes only",int.MaxValue);var entries=preview.HardpointDescriptions;
        for(int i=0;i<System.Math.Min(entries.Count,512);i++)selector.AddItem(entries[i],i);
        if(entries.Count>0)selector.Select(1);
        selector.ItemSelected+=index=>preview.SelectHardpointAnnotation(index==0?-1:selector.GetItemId((int)index));
        selector.EmitSignal(OptionButton.SignalName.ItemSelected,0L);
        if(preview.VisibleHardpointLabels!=0)throw new AssetPreviewException("Hardpoint axes-only selector mismatch.");
        if(entries.Count>0){selector.EmitSignal(OptionButton.SignalName.ItemSelected,1L);if(preview.VisibleHardpointLabels!=1)throw new AssetPreviewException("Hardpoint label selector mismatch.");}
    }
    private async System.Threading.Tasks.Task BuildDetailPreview(string[] args)
    {
        string Read(string key)
        {int i=System.Array.IndexOf(args,key);if(i<0 || i+1>=args.Length || args.Count(value=>value==key)!=1)throw new System.ArgumentException("Missing/duplicate detail option.");return args[i+1];}
        using StreamReader configuration=File.OpenText(Read("--asset-config"));
        TexturedDetailAppearance asset;
        using(ConfiguredTreOverlay overlay=ConfiguredTreOverlay.Open(Read("--asset-root"),configuration))
            asset=TexturedDetailAppearanceReader.Load(overlay.Lookup,Read("--asset-member"),Read("--asset-sha256"));
        StaticMeshPreview.VerifyCoordinateBoundary();detailPreview=new(asset,args.Contains("--main-texture-only"));detailPreview.VerifySwitches();detailPreview.VerifyCameraSwitches(Transform3D.Identity);
        GD.Print($"PASS: Godot original detail uploads and camera/PIVT switches levels={asset.Description.Levels.Count} unique_sources={asset.Sources.Count}");
        if(args.Contains("--asset-self-test") && DisplayServer.GetName()=="headless"){GetTree().Quit();return;}
        AddChild(new WorldEnvironment{Environment=new Environment{BackgroundMode=Environment.BGMode.Color,BackgroundColor=new Color("#14202e")}});
        MeshInstance3D instance=new(){Mesh=detailPreview.Select(0)};AddChild(instance);
        Aabb bounds=detailPreview.Bounds;PreviewOrbitCamera camera=new(){Current=true};AddChild(camera);camera.Configure(bounds.GetCenter(),System.Math.Max(bounds.Size.Length()*0.5f,1));
        CanvasLayer layer=new();AddChild(layer);
        ColorRect header=new(){Color=new Color(0.02f,0.04f,0.08f,0.92f),OffsetRight=1280,OffsetBottom=180};layer.AddChild(header);
        Label title=new(){Text="STARDUST — ORIGINAL APPEARANCE LOD INSPECTOR",Position=new Vector2(28,18)};title.AddThemeFontSizeOverride("font_size",24);header.AddChild(title);
        Label selected=new(){Text=detailPreview.Describe(),Position=new Vector2(28,54)};header.AddChild(selected);
        header.AddChild(new Label{Text=args.Contains("--main-texture-only")?"Unlit MAIN texture inspection; original material programs remain untranslated.":"Unlit authored fallback preview; original lighting remains untranslated.",Position=new Vector2(28,150)});
        Label distanceLabel=new(){Text="Selection distance: 0 (offline control)",Position=new Vector2(28,80)};header.AddChild(distanceLabel);
        HSlider slider=new(){Position=new Vector2(28,112),Size=new Vector2(580,24),MinValue=0,MaxValue=2000,Step=1};header.AddChild(slider);
        slider.ValueChanged+=distance=>{instance.Mesh=detailPreview.Select((float)distance);selected.Text=detailPreview.Describe();distanceLabel.Text=$"Selection distance: {distance:0} (offline control)";};
        bool cameraMode=args.Contains("--camera-lod");
        CheckButton cameraControl=new(){Text="Use camera distance",Position=new Vector2(690,104),ButtonPressed=cameraMode};header.AddChild(cameraControl);slider.Editable=!cameraMode;
        cameraControl.Toggled+=enabled=>{cameraMode=enabled;slider.Editable=!enabled;if(!enabled){instance.Mesh=detailPreview.Select((float)slider.Value);selected.Text=detailPreview.Describe();distanceLabel.Text=$"Selection distance: {slider.Value:0} (offline control)";}};
        cameraLodUpdate=()=>{if(cameraMode){instance.Mesh=detailPreview.SelectFromCamera(camera.GlobalPosition,instance.GlobalTransform);selected.Text=detailPreview.Describe();distanceLabel.Text=$"Camera/PIVT distance: {detailPreview.CameraDistance(camera.GlobalPosition,instance.GlobalTransform):0.0}";}};
        layer.AddChild(new Label{Text="Drag to orbit • Scroll to zoom • R to reset • Slider chooses authored detail",Position=new Vector2(28,680)});
        if(DisplayServer.GetName()!="headless" && (args.Contains("--capture") || args.Contains("--asset-self-test")))
        {await StaticMeshPreview.VerifyGpuUvBoundary(this).WaitAsync(System.TimeSpan.FromSeconds(15));GD.Print("PASS: GPU detail coordinate fixture");}
        if(args.Contains("--capture"))
        {
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            using Image image=GetViewport().GetTexture().GetImage();
            if(image.SavePng(Read("--capture"))!=Error.Ok)throw new IOException("Detail capture failed.");
            GD.Print("PASS: original detail inspector captured");GetTree().Quit();
        }
        else if(args.Contains("--asset-self-test"))GetTree().Quit();
    }
    private async System.Threading.Tasks.Task BuildAssetPreview(string[] args)
    {
        string Read(string key)
        {int i=System.Array.IndexOf(args,key);if(i<0 || i+1>=args.Length || args.Count(value=>value==key)!=1)throw new System.ArgumentException("Missing/duplicate asset option.");return args[i+1];}
        using StreamReader configuration=File.OpenText(Read("--asset-config"));
        TexturedStaticMesh asset;
        using(ConfiguredTreOverlay overlay=ConfiguredTreOverlay.Open(Read("--asset-root"),configuration))
            asset=TexturedStaticMeshReader.Load(overlay.Lookup,Read("--asset-member"),Read("--asset-sha256"));
        StaticMeshPreview.VerifyCoordinateBoundary();
        assetPreview=new(asset,args.Contains("--main-texture-only"));
        assetPreview.VerifyUploadedAsset(asset);
        GD.Print($"PASS: Godot asset upload surfaces={asset.Mesh.Primitives.Count} vertices={asset.Mesh.VertexCount} triangles={asset.Mesh.TriangleCount} textures={asset.TextureCount}");
        if(args.Contains("--asset-self-test") && DisplayServer.GetName()=="headless"){GetTree().Quit();return;}
        WorldEnvironment world=new(){Environment=new Environment{BackgroundMode=Environment.BGMode.Color,BackgroundColor=new Color("#14202e")}};AddChild(world);
        MeshInstance3D node=new(){Mesh=assetPreview.Mesh};AddChild(node);
        Aabb bounds=assetPreview.Mesh.GetAabb();Vector3 center=bounds.GetCenter();float radius=System.Math.Max(bounds.Size.Length()*0.5f,1f);
        PreviewOrbitCamera camera=new(){Current=true};AddChild(camera);camera.Configure(center,radius);
        CanvasLayer layer=new();AddChild(layer);
        ColorRect header=new(){Color=new Color(0.02f,0.04f,0.08f,0.92f),OffsetRight=1280,OffsetBottom=108};layer.AddChild(header);
        Label title=new(){Text="STARDUST — ORIGINAL ASSET TEXTURE PREVIEW",Position=new Vector2(28,18)};title.AddThemeFontSizeOverride("font_size",24);header.AddChild(title);
        header.AddChild(new Label{Text=$"{asset.Mesh.VertexCount} vertices • {asset.Mesh.TriangleCount} triangles • {asset.Materials.Count} batches • {asset.TextureCount} decoded textures",Position=new Vector2(28,54)});
        header.AddChild(new Label{Text="Opaque, unlit preview. Legacy lighting programs remain untranslated.",Position=new Vector2(28,80)});
        layer.AddChild(new Label{Text="Drag to orbit • Scroll to zoom • R to reset",Position=new Vector2(28,680)});
        if(DisplayServer.GetName()!="headless" && (args.Contains("--capture") || args.Contains("--asset-self-test")))
        {await StaticMeshPreview.VerifyGpuUvBoundary(this).WaitAsync(System.TimeSpan.FromSeconds(15));GD.Print("PASS: GPU asymmetric UV corners and reflected front-face winding");}
        if(args.Contains("--capture"))
        {
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            using Image image=GetViewport().GetTexture().GetImage();
            Error result=image.SavePng(Read("--capture"));if(result!=Error.Ok)throw new System.IO.IOException("Preview capture failed.");
            GD.Print("PASS: original textured preview frame captured");GetTree().Quit();
        }
        else if(args.Contains("--asset-self-test")){GD.Print("PASS: original asset preview scene constructed");GetTree().Quit();}
    }
    private void BuildWorld()
    {
        WorldEnvironment world = new();
        world.Environment = new Environment { BackgroundMode = Environment.BGMode.Color, BackgroundColor = new Color("#122433"), AmbientLightSource = Environment.AmbientSource.Color, AmbientLightColor = new Color("#9ac4d8"), AmbientLightEnergy = 0.65f };
        AddChild(world);
        AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-55, -30, 0), LightEnergy = 1.25f, ShadowEnabled = true });
        PlaneMesh plane = new() { Size = new Vector2(80, 80), Material = new StandardMaterial3D { AlbedoColor = new Color("#426348"), Roughness = 0.92f } };
        AddChild(new MeshInstance3D { Mesh = plane });
        CapsuleMesh capsule = new() { Height = 3.0f, Radius = 0.55f, Material = new StandardMaterial3D { AlbedoColor = new Color("#54c7d9"), Metallic = 0.2f } };
        AddChild(new MeshInstance3D { Mesh = capsule, Position = new Vector3(0, 1.5f, 0) });
        Camera3D camera = new() { Position = new Vector3(8, 6, 10), Current = true }; camera.LookAtFromPosition(camera.Position, new Vector3(0, 1, 0)); AddChild(camera);
    }
    private void BuildInterface(IReadOnlyList<string> events)
    {
        CanvasLayer layer = new(); AddChild(layer);
        ColorRect header = new() { Color = new Color(0.02f, 0.08f, 0.12f, 0.93f), OffsetRight = 1280, OffsetBottom = 76 }; layer.AddChild(header);
        Label title = new() { Text = "STARDUST 64-BIT CLIENT — GODOT FEASIBILITY PROOF", Position = new Vector2(28, 18) }; title.AddThemeFontSizeOverride("font_size", 24); header.AddChild(title);
        PanelContainer panel = new() { Position = new Vector2(32, 120), Size = new Vector2(470, 345) }; layer.AddChild(panel);
        VBoxContainer stack = new(); stack.AddThemeConstantOverride("separation", 12); panel.AddChild(stack);
        Label heading = new() { Text = "Offline transport boundary validated" }; heading.AddThemeFontSizeOverride("font_size", 20); stack.AddChild(heading);
        stack.AddChild(new Label { Text = "Godot host → engine-neutral SOE core → fixture-driven login state", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        foreach (string entry in events) stack.AddChild(new Label { Text = $"✓  {entry}" });
        stack.AddChild(new HSeparator());
        stack.AddChild(new Label { Text = "This offline transcript uses synthetic credentials and no network access. Legacy asset decoding is outside this milestone.", AutowrapMode = TextServer.AutowrapMode.WordSmart });
    }
}
