using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Configuration;
using Stardust.Core.Geometry;
using Stardust.Core.Session;

/// <summary>Self-contained offline original-resource terrain entry; no transport or synthetic replication fixtures.</summary>
public partial class OfflineTerrainClient : Node
{
    private sealed record Resources(OriginalTerrainRegion Region,CreatureAppearanceDescriptorBundle Creature,TerrainSuppliedStaticMaterial Material,MosTaikeSceneResources? Town=null,MosTaikeObstacleBoxes? Obstacles=null);
    private readonly bool town=OS.HasFeature("stardust_offline_mos_taike");
    private readonly List<GeometryAppearancePreview> scenery=new();
    private readonly CancellationTokenSource cancellation=new();
    private Task<Resources>? loading;
    private bool closing,focused,paused,proof;
    private readonly HashSet<Key> keys=new();
    private readonly List<TerrainPatchVisual> terrain=new();
    private SkeletalMeshPreview? actor;
    private ImageTexture? texture;
    private Resources? resources;
    private OfflineTerrainRoaming? motion;
    private TerrainRoamingCamera? camera;
    private Label? caption;
    private double animationSeconds,elapsed;
    private string clip="idle";
    private string? evidence;
    private int proofPhase;
    private double walkDistance,runDistance,walkSeconds,runSeconds,maximumGroundError;
    private readonly List<object> observations=new();
    public override async void _Ready()
    {
        GetTree().AutoAcceptQuit=false;
        GetWindow().Title=town?"Stardust | DX12 Mos Taike | Offline":"Stardust | DX12 Terrain | Offline";
        try
        {
            string[] args=OS.GetCmdlineUserArgs();
            bool test=args.SequenceEqual(new[]{"--terrain-self-test"});
            bool cancel=args.SequenceEqual(new[]{"--terrain-cancel"});
            proof=args.Length==2&&args[0]=="--offline-terrain-proof";
            if(args.Length!=0&&!test&&!cancel&&!proof)throw new ArgumentException("Unsupported terrain launch option.");
            RendererAdmission.Verify(DisplayServer.GetName()!="headless" || args.Length==0 || proof);
            if(proof)
            {
                evidence=args[1];
                if(!Path.IsPathFullyQualified(evidence)||!Directory.Exists(evidence)||Directory.EnumerateFileSystemEntries(evidence).Any())
                    throw new ArgumentException("Fresh empty absolute terrain proof directory required.");
            }
            string profilePath=Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath())!,"character-launch.json");
            if(new FileInfo(profilePath).Length is <1 or >65536)throw new InvalidDataException("Bounded adjacent installation profile required.");
            var profile=ReplacementLaunchProfileCodec.Decode(File.ReadAllBytes(profilePath));
            var layer=new CanvasLayer();AddChild(layer);
            var header=new ColorRect{Color=new(.025f,.04f,.06f,.90f),AnchorRight=1,OffsetBottom=118,MouseFilter=Control.MouseFilterEnum.Ignore};layer.AddChild(header);
            caption=new Label{Text=town?"Loading original Mos Taike terrain, buildings and human…":"Loading original Tatooine base terrain and human…",Position=new(24,12)};header.AddChild(caption);
            loading=Task.Run(()=>Load(profile,cancellation.Token,town));
            if(cancel)cancellation.Cancel();
            resources=await loading;
            if(closing||!IsInsideTree())return;
            motion=new(resources.Region.Surface,resources.Region.Start,resources.Obstacles is {} obstacles?obstacles.Blocks:null);
            GD.Print("offline_terrain_sources="+JsonSerializer.Serialize(new{TerrainPath=OriginalTerrainRegion.TerrainPath,resources.Region.TerrainSha256,
                GeneratorSha256=resources.Region.Generator.SourceSha256,resources.Region.Height.RuleSha256,resources.Region.Height.FamilyId,
                Chunks=resources.Region.Patches.Count,Patches=resources.Region.Patches.TotalPatches,Triangles=resources.Region.Surface.TotalTriangles,
                Seams=resources.Region.Patches.SharedSampleComparisons,resources.Material.ShaderSource,resources.Material.TextureSource,
                CreatureSources=resources.Creature.Sources.Values,Clips=ClipIdentities(resources.Creature),resources.Region.AppliedHeightLayers,
                Town=TownReceipt(resources.Town),OriginalFullTerrainParity=false,SocketCreated=false}));
            VerifyController(resources.Region);
            if(resources.Obstacles is {} originalObstacles)
            {
                var garage=resources.Town!.Placements.Single(p=>p.ObjectPath.Contains("parking_garage"));
                var box=originalObstacles.Boxes.Single(b=>b.ObjectId==garage.Node.Id);var isolated=new MosTaikeObstacleBoxes(new[]{box});
                float z=(box.Minimum.Z+box.Maximum.Z)/2;
                if(!isolated.Blocks(new(box.Minimum.X-1,box.Minimum.Y,z),new(box.Maximum.X+1,box.Minimum.Y,z)))throw new InvalidDataException("Original garage sweep admitted crossing.");
                if(isolated.Blocks(new(box.Minimum.X-1,box.Minimum.Y,box.Maximum.Z+.36f),new(box.Maximum.X+1,box.Minimum.Y,box.Maximum.Z+.36f)))throw new InvalidDataException("Original garage tangent rejected.");
                if(originalObstacles.Blocks(motion.Position,motion.Position))throw new InvalidDataException("Town starting placement obstructed.");
                float cameraDistance=isolated.CameraDistance(new(box.Minimum.X-2,box.Minimum.Y+1,z),new(box.Maximum.X+2,box.Minimum.Y+1,z));
                if(cameraDistance is <=0 or >=2)throw new InvalidDataException("Original garage camera crossing failed to shorten.");
                GD.Print("offline_town_obstacle_checks="+JsonSerializer.Serialize(new{Boxes=originalObstacles.Boxes.Count,GarageId=garage.Node.Id,SweptCrossingRejected=true,TangentPassed=true,StartClear=true,CameraCrossingDistance=cameraDistance}));
            }
            if(proof)File.WriteAllText(Path.Combine(evidence!,"terrain-geometry.json"),JsonSerializer.Serialize(resources.Region.Surface.Children.Select(c=>new{
                c.ChunkIndex,c.PatchIndex,Positions=c.Geometry.CopyPositions().Select(p=>new[]{p.X,p.Y,p.Z}),Indices=c.Geometry.CopyIndices()})));
            if(test){GD.Print("PASS: terrain CPU seams, grounding, walk/run, focus and edge hold");GetTree().Quit();return;}
            byte[] rgba=Enumerable.Range(0,resources.Material.MipCount).SelectMany(resources.Material.CopyRgba).ToArray();
            using(var image=Image.CreateFromData(resources.Material.Width,resources.Material.Height,resources.Material.MipCount>1,Image.Format.Rgba8,rgba))texture=ImageTexture.CreateFromImage(image);
            using(var readback=texture.GetImage())if(!readback.GetData().AsSpan().SequenceEqual(rgba))throw new InvalidDataException("Terrain texture upload differs.");
            foreach(var child in resources.Region.Surface.Children)
            {
                var visual=new TerrainPatchVisual(child.Geometry,this,TerrainPatchRenderPolicy.ModernSuppliedMainTexture,resources.Material,texture);
                terrain.Add(visual);visual.VerifyReadback();
            }
            actor=new(resources.Creature,specularPreview:true);AddChild(actor.Root);actor.SetClipFrame("idle",0);actor.VerifyBoundary(true);
            if(resources.Town is {} district)
            {
                foreach(var placement in district.Placements)
                {
                    var visual=new GeometryAppearancePreview(placement.Appearance.Geometry,placement.Appearance.Meshes,allowMainTextureOnly:true,modernTownAlpha:true);
                    scenery.Add(visual);visual.Root.Transform=GodotAppearanceTransform.ToGodot(placement.Transform);AddChild(visual.Root);
                    if(visual.NeutralSurfaces!=0||visual.OmittedSurfaces!=0)throw new InvalidDataException("Town material presentation omitted original geometry: "+placement.ObjectPath+" "+string.Join(";",visual.MaterialDiagnostics));
                    visual.SelectFromCamera(ToGodot(motion.Position));
                }
            }
            AddChild(new WorldEnvironment{Environment=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new Color("#a8c0d5"),
                AmbientLightSource=Godot.Environment.AmbientSource.Color,AmbientLightColor=new Color("#d9d9df"),AmbientLightEnergy=.25f,
                TonemapMode=Godot.Environment.ToneMapper.Filmic}});
            AddChild(new DirectionalLight3D{RotationDegrees=new(-48,-30,0),LightEnergy=.55f,ShadowEnabled=true});
            camera=new TerrainRoamingCamera{Current=true,Fov=58,Near=.05f,Far=600,Obstacles=resources.Obstacles,Surface=town?resources.Region.Surface:null,Town=town};AddChild(camera);camera.Reset();camera.Follow(ToGodot(motion.Position));
            focused=proof||GetWindow().HasFocus();
            PublishPose();
            GD.Print("offline_terrain_ready=true partial_generator="+(!town).ToString().ToLowerInvariant()+" live_transport=false");
        }
        catch(OperationCanceledException){GD.Print("PASS: terrain cancellation drained worker before GPU publication");GetTree().Quit();}
        catch(Exception error)
        {
            GD.PushError("Offline terrain failed: "+error.Message);
            if(proof||DisplayServer.GetName()=="headless"||OS.GetCmdlineUserArgs().Contains("--terrain-self-test")||OS.GetCmdlineUserArgs().Contains("--terrain-cancel"))
            {GetTree().Quit(1);return;}
            if(caption is null){var failureLayer=new CanvasLayer();AddChild(failureLayer);caption=new Label{Position=new(24,24)};failureLayer.AddChild(caption);}
            caption.Text="Terrain client could not start: "+error.Message+"\nCheck character-launch.json and the original installation. Escape closes.";
        }
        finally{if(closing&&IsInsideTree())GetTree().Quit();}
    }
    private static Resources Load(ReplacementLaunchProfile profile,CancellationToken token,bool town)
    {
        token.ThrowIfCancellationRequested();
        using var preparation=ReplacementLaunchPreparation.Open(profile);
        token.ThrowIfCancellationRequested();
        var overlay=preparation.Inputs.BorrowedOverlay;
        var recipe=preparation.Creature.Catalog.Entries.Values.Single();
        var creature=CreatureAppearanceDescriptorBundleReader.Load(overlay.Lookup,profile.ObjectPath,recipe.ObjectSha256,recipe.AppearanceSha256,
            limits:new(MaximumResources:256,MaximumSourceBytes:64*1024*1024),loadSkeletons:true,loadSkinSources:true,loadGeometry:true,
            loadLogicalTables:true,animationSelection:preparation.Creature.Options.Idle,prepareSelectedAnimation:true,loadMaterialDefaults:true,
            loadPaletteDefaults:true,loadAlbedoFactors:true,loadTextureChoices:true,loadCompressedNormals:true,loadVertexLitSpecular:true,
            additionalAnimationSelections:preparation.Creature.Options.Movement);
        token.ThrowIfCancellationRequested();
        var selected=overlay.Lookup(OriginalTerrainRegion.TerrainPath);
        if(selected.Kind!=TreLookupKind.Present||selected.Payload is null)throw new InvalidDataException("Original terrain missing; no replacement plane.");
        var region=town?OriginalTerrainRegion.BuildMosTaike(selected.Payload,token):OriginalTerrainRegion.Build(selected.Payload,token);
        var district=town?MosTaikeSceneResources.Load(overlay.Lookup,token):null;
        // Read actual root ASCN's family; deterministic first family entry is a modern preview selection.
        var ascn=region.Generator.Layers[0].Children.Single(c=>c.FormType=="ASCN");
        var node=IffDocument.Parse(ascn.CopyEncodedBytes()).Root.Children.Single();
        var data=node.Children.Single(c=>c.Tag=="DATA").Payload.Span;
        if(node.FormType!="0001"||data.Length!=12)throw new InvalidDataException("Unsupported base ASCN.");
        uint id=System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(data);
        var family=TerrainGeneratorShaderFamilies.Bind(region.Generator).Families.Single(f=>f.FamilyKeyBits==id);
        string name=Encoding.ASCII.GetString(family.Entries[0].CopyStringBytes());
        string shaderPath="shader/"+name+".sht";
        var shader=overlay.Lookup(shaderPath);
        if(shader.Kind!=TreLookupKind.Present||shader.Payload is null)throw new InvalidDataException("Base terrain shader missing.");
        var material=TerrainSuppliedStaticMaterial.Load(overlay.Lookup,shaderPath,Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(shader.Payload)),
            TerrainSuppliedStaticMaterialPolicy.TextureTimesVertexColor,cancellation:token);
        if(!material.IsSingleMipOrCompleteChain)throw new InvalidDataException("Unsupported terrain mip chain.");
        token.ThrowIfCancellationRequested();return new(region,creature,material,district,district is null?null:new MosTaikeObstacleBoxes(district));
    }
    private static object ClipIdentities(CreatureAppearanceDescriptorBundle c)=>new{Idle=c.PreparedAnimation!.ClipSha256,
        Walk=c.AdditionalAnimations!["walk"].ClipSha256,Run=c.AdditionalAnimations["run"].ClipSha256};
    private static object? TownReceipt(MosTaikeSceneResources? scene)=>scene is null?null:new{scene.Snapshot.Sha256,
        Rendered=scene.Placements.Count,Buildings=scene.Placements.Count(p=>p.ObjectPath.StartsWith("object/building/")),
        Placements=scene.Placements.Select(p=>new{p.Node.Id,p.ObjectPath,p.ObjectSha256,p.Profile.AppearancePath,
            Position=new[]{p.Node.Position.X,p.Node.Position.Y,p.Node.Position.Z},QuaternionWXYZ=new[]{p.Node.Rotation.W,p.Node.Rotation.X,p.Node.Rotation.Y,p.Node.Rotation.Z},
            p.Node.PortalLayoutCrc,Sources=p.Appearance.Sources.Values}),scene.Omissions};
    private static void VerifyController(OriginalTerrainRegion region)
    {
        var m=new OfflineTerrainRoaming(region.Surface,region.Start);var forward=new System.Numerics.Vector3(0,0,1);
        float start=m.Position.Z;
        for(int i=0;i<100;i++)m.Advance(new(0,-1),forward,false,true,.01);
        float walk=m.Position.Z-start;
        for(int i=0;i<100;i++)m.Advance(new(0,-1),forward,true,true,.01);
        float run=m.Position.Z-start-walk;
        if(Math.Abs(walk-3)>.001||Math.Abs(run-6)>.001)throw new InvalidDataException("Walk/run displacement mismatch.");
        var before=m.Position;m.Advance(new(0,-1),forward,true,false,.1);
        if(m.Position!=before||m.Speed!=0)throw new InvalidDataException("Unfocused movement advanced.");
        for(int i=0;i<500;i++)m.Advance(new(0,-1),forward,true,true,.1);
        if(m.EdgeHolds==0||m.Position.Y!=(float)region.Surface.Query(m.Position.X,m.Position.Z)!.Hit.Height)
            throw new InvalidDataException("Rendered surface grounding/edge hold mismatch.");
        m.Reset();if(m.Position.X!=region.Start.X||m.Position.Z!=region.Start.Y||m.Speed!=0)throw new InvalidDataException("Reset failed.");
        try{OriginalTerrainRegion.Build(new byte[12]);throw new InvalidDataException("Truncated terrain accepted.");}catch(InvalidDataException e)when(e.Message!="Truncated terrain accepted."){}
        foreach(var bad in new[]{double.NaN,-1,.2}){try{m.Advance(default,forward,false,true,bad);throw new InvalidDataException("Bad elapsed accepted.");}catch(ArgumentOutOfRangeException){}}
        GD.Print("offline_terrain_controller_checks="+JsonSerializer.Serialize(new{WalkDistance=walk,RunDistance=run,m.EdgeHolds,Grounded=true,FocusHeld=true,InvalidElapsedRejected=3}));
    }
    public override void _Input(InputEvent input)
    {
        if(input is not InputEventKey key)return;
        if(key.Keycode==Key.Escape&&key.Pressed){Close();GetViewport().SetInputAsHandled();return;}
        if(key.Echo)return;
        if(key.Keycode==Key.P&&key.Pressed){paused=!paused;keys.Clear();}
        if(key.Keycode==Key.R&&key.Pressed){motion?.Reset();camera?.Reset();keys.Clear();}
        Key code=key.PhysicalKeycode!=Key.None?key.PhysicalKeycode:key.Keycode;
        if(code is Key.W or Key.A or Key.S or Key.D or Key.Shift){if(key.Pressed)keys.Add(code);else keys.Remove(code);}
    }
    public override void _Notification(int what)
    {
        if(what==NotificationWMCloseRequest){Close();return;}
        if(what==NotificationWMWindowFocusOut){focused=proof;keys.Clear();camera?.StopDrag();}
        if(what==NotificationWMWindowFocusIn){focused=true;keys.Clear();}
    }
    public override void _Process(double delta)
    {
        if(closing||actor is null||motion is null||camera is null||resources is null)return;
        try
        {
            elapsed+=delta;
            if(proof){keys.Clear();if(elapsed<2)keys.Add(Key.W);else if(elapsed<4){keys.Add(Key.W);keys.Add(Key.Shift);}else if(elapsed<4.6)keys.Add(Key.D);}
            var raw=new System.Numerics.Vector2((keys.Contains(Key.D)?1:0)-(keys.Contains(Key.A)?1:0),(keys.Contains(Key.S)?1:0)-(keys.Contains(Key.W)?1:0));
            Vector3 f=-camera.GlobalBasis.Z;
            var previous=motion.Position;double step=Math.Clamp(delta,0,.1);
            motion.Advance(raw,new(f.X,f.Y,-f.Z),keys.Contains(Key.Shift),focused&&!paused,step);
            double moved=motion.LastHorizontalDistance;
            if(motion.Speed==3){walkDistance+=moved;walkSeconds+=step;}else if(motion.Speed==6){runDistance+=moved;runSeconds+=step;}
            maximumGroundError=Math.Max(maximumGroundError,Math.Abs(motion.Position.Y-resources.Region.Surface.Query(motion.Position.X,motion.Position.Z)!.Hit.Height));
            string next=motion.Speed==0?"idle":keys.Contains(Key.Shift)?"run":"walk";
            if(next!=clip){clip=next;animationSeconds=0;}else if(focused&&!paused)animationSeconds+=Math.Clamp(delta,0,.1);
            var prepared=clip=="idle"?resources.Creature.PreparedAnimation!:resources.Creature.AdditionalAnimations![clip];
            double frame=(animationSeconds*prepared.ProxyTimeScale*prepared.FrameRate)%Math.Max(1,(int)prepared.EndFrame);
            actor.SetMovementFrame(clip,frame,focused&&!paused?step:0);PublishPose();
            if(proof&&elapsed>new[]{.6,2.6,4.8}[Math.Min(proofPhase,2)]&&proofPhase<3)
            {int phase=proofPhase++;observations.Add(new{Phase=phase,Elapsed=elapsed,motion.Speed,Clip=clip,Frame=frame,Position=new[]{motion.Position.X,motion.Position.Y,motion.Position.Z}});CaptureProof(phase);}
        }
        catch(Exception error){GD.PushError("Terrain frame failed: "+error.Message);GetTree().Quit(1);}
    }
    private void PublishPose()
    {
        actor!.Root.Position=ToGodot(motion!.Position);actor.Root.Rotation=new(0,-motion.Heading,0);
        if(motion.Speed>0)
        {
            var expected=new Vector3(Mathf.Sin(motion.Heading),0,-Mathf.Cos(motion.Heading));
            if((actor.Root.Basis*Vector3.Forward).DistanceTo(expected)>1e-5f)throw new InvalidDataException("Actor facing/reflection mismatch.");
        }
        camera!.Follow(actor.Root.Position);
        foreach(var visual in scenery)visual.SelectFromCamera(camera.GlobalPosition);
        if(town)
        {
            caption!.Text="STARDUST | DIRECTX 12 | Mos Taike — bounded west / central district\n"+
                "WASD move | Shift run | right-drag orbit | wheel zoom | P pause | R reset | Esc close\n"+
                $"{clip} | x {motion.Position.X:0.0} z {motion.Position.Z:0.0} height {motion.Position.Y:0.00} | 160 × 160m | "+
                (motion.Speed==0&&motion.ObstacleHolds>0?"Obstacle: movement held":motion.Speed==0&&motion.EdgeHolds>0?"District edge: movement held":paused?"Paused":"Offline")+"\n"+
                $"Original height rules, 5 buildings, {resources!.Town!.Placements.Count} placed objects | modern materials / obstacle boxes | interiors pending";
            return;
        }
        caption!.Text="STARDUST | DIRECTX 12 | Tatooine base terrain — partial generator\n"+
            "WASD move | Shift run | right-drag orbit | wheel zoom | P pause | R reset | Esc close\n"+
            $"{clip} | x {motion.Position.X:0.0} z {motion.Position.Z:0.0} height {motion.Position.Y:0.00} | 256 × 256m | "+
            (motion.EdgeHolds>0&&motion.Speed==0?"Region edge: movement held":paused?"Paused":"Offline")+"\nLater geographic layers, blending, flora and water pending; original full elevations differ.";
    }
    private async void CaptureProof(int phase)
    {
        await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        using var image=GetViewport().GetTexture().GetImage();
        if(image.SavePng(Path.Combine(evidence!,"terrain-"+phase+".png"))!=Error.Ok)throw new IOException("Proof PNG failed.");
        if(phase!=2)return;
        actor!.VerifyBoundary(true);
        File.WriteAllText(Path.Combine(evidence!,"terrain-proof.json"),JsonSerializer.Serialize(new{Version=1,ActualProcessUpdates=motion!.Updates,
            FocusOverrideSynthetic=true,Motion=observations,Backend=RenderingServer.GetCurrentRenderingDriverName(),
            WalkDistance=walkDistance,RunDistance=runDistance,WalkSeconds=walkSeconds,RunSeconds=runSeconds,MaximumGroundError=maximumGroundError,
            Clips=ClipIdentities(resources!.Creature),resources.Region.TerrainSha256,resources.Region.Height.RuleSha256,resources.Region.AppliedHeightLayers,
            Town=TownReceipt(resources.Town),motion.ObstacleHolds,OriginalFullTerrainParity=false,SocketCreated=false},new JsonSerializerOptions{WriteIndented=true}));
        GD.Print("PASS: exported DX12 terrain walk/run/idle GPU proof");Close();
    }
    private static Vector3 ToGodot(System.Numerics.Vector3 p)=>new(p.X,p.Y,-p.Z);
    private void Close(){if(closing)return;closing=true;keys.Clear();cancellation.Cancel();if(loading?.IsCompleted!=false)GetTree().Quit();}
    public override void _ExitTree()
    {
        actor?.Dispose();actor=null;
        foreach(var visual in scenery){visual.Root.QueueFree();visual.Dispose();}scenery.Clear();
        foreach(var visual in terrain)visual.Dispose();
        GD.Print("offline_terrain_cleanup="+JsonSerializer.Serialize(new{Patches=terrain.Count,Disposed=terrain.All(v=>v.IsDisposed),WorkerDrained=loading?.IsCompleted!=false}));
        terrain.Clear();texture?.Dispose();texture=null;cancellation.Dispose();
    }
}

public partial class TerrainRoamingCamera : Camera3D
{
    public MosTaikeObstacleBoxes? Obstacles {get;set;}
    public TerrainModernPatchSetSurfaceQuery? Surface {get;set;}
    public bool Town {get;set;}
    private Vector3 target;
    private float yaw=.6f,pitch=.12f,distance=7;
    private bool drag;
    public void StopDrag()=>drag=false;
    public void Reset(){yaw=Town?1.35f:.6f;pitch=Town?.20f:.12f;distance=7;UpdatePose();}
    public void Follow(Vector3 position){target=position+new Vector3(0,1.2f,0);UpdatePose();}
    private void UpdatePose()
    {
        var offset=new Vector3(Mathf.Sin(yaw)*Mathf.Cos(pitch),Mathf.Sin(pitch),Mathf.Cos(yaw)*Mathf.Cos(pitch));
        var candidate=target+offset*distance;
        // Find a segment that is still clear after terrain raises its endpoint; recheck rather than publishing the raised collision.
        for(int probe=0;probe<=64;probe++)
        {
            float trial=Math.Max(.25f,distance*(64-probe)/64);candidate=target+offset*trial;
            if(Surface?.Query(candidate.X,-candidate.Z) is {} hit)candidate.Y=Math.Max(candidate.Y,(float)hit.Hit.Height+.35f);
            float actual=target.DistanceTo(candidate);
            if(Obstacles is null||Obstacles.CameraDistance(new(target.X,target.Y,-target.Z),new(candidate.X,candidate.Y,-candidate.Z))>=actual-.001f)break;
            if(probe==64)candidate=target+new Vector3(0,.05f,0);
        }
        Position=candidate;if(Position.DistanceTo(target)>.01f)LookAt(target);
    }
    public override void _UnhandledInput(InputEvent input)
    {
        if(input is InputEventMouseButton b){if(b.ButtonIndex==MouseButton.Right)drag=b.Pressed;
            if(b.Pressed&&b.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown){distance=Mathf.Clamp(distance*(b.ButtonIndex==MouseButton.WheelUp?.9f:1.1f),2,30);UpdatePose();GetViewport().SetInputAsHandled();}}
        else if(input is InputEventMouseMotion m&&drag){yaw-=m.Relative.X*.007f;pitch=Mathf.Clamp(pitch+m.Relative.Y*.007f,.08f,1.25f);UpdatePose();GetViewport().SetInputAsHandled();}
    }
}

