using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

/// <summary>Standalone supplied face inspection over baseline descriptor/builder APIs.</summary>
public partial class SceneCreatureFaceInspectionPreview : Node
{
    private SceneCreatureVisual? visual;
    private readonly CancellationTokenSource cancel=new();
    private readonly List<Task<SceneCreatureResources>> jobs=new();
    private readonly TaskCompletionSource<bool> entered=new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool closed;
    private int archivesClosed;
    private Godot.Environment? environment;
    public override async void _Ready()
    {
        try{await Run();GD.Print("PASS: face inspection complete");GetTree().Quit();}
        catch(Exception error){GD.PushError("Face inspection failed: "+error);try{await Shutdown();}catch{}GetTree().Quit(1);}
    }
    private async Task Run()
    {
        var args=OS.GetCmdlineUserArgs();string Arg(string key){int i=Array.IndexOf(args,key);if(i<0||i+1>=args.Length||args.Count(a=>a==key)!=1)throw new ArgumentException(key);return args[i+1];}
        string root=Arg("--asset-root"),cfgPath=Arg("--asset-config"),export=Arg("--face-export");bool canceled=args.Contains("--face-cancel"),gpu=DisplayServer.GetName()!="headless";
        string? capture=args.Contains("--face-capture")?Arg("--face-capture"):null;
        void PrivateFresh(string path,string extension)
        {
            string prefix=Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"),"../../local-private"))+Path.DirectorySeparatorChar;
            if(!Path.GetFullPath(path).StartsWith(prefix,StringComparison.OrdinalIgnoreCase)||Path.GetExtension(path)!=extension||File.Exists(path)||!Directory.Exists(Path.GetDirectoryName(Path.GetFullPath(path))))throw new ArgumentException("Fresh private destination required.");
        }
        PrivateFresh(export,".json");if(capture is not null){if(!gpu||canceled)throw new ArgumentException("Capture requires separate GPU mode.");foreach(string path in Captures(capture))PrivateFresh(path,".png");}
        byte[] cfg=File.ReadAllBytes(cfgPath);Check(Convert.ToHexString(SHA256.HashData(cfg))==RetainedSkeletalDetailBundleReader.ConfigurationPin,"configuration pin");
        byte[] profileBytes=File.ReadAllBytes(ProjectSettings.GlobalizePath("res://../preview-profiles/human-face-zero.json"));
        Check(Convert.ToHexString(SHA256.HashData(profileBytes))=="C4F7CF06AB8C4083A2510556550C047DC0ADD5A5A0040829A81CBB047CCCBD9B","supplied face profile pin");
        var values=BlueprintFaceCustomizationProfile.Parse(profileBytes);var policy=new SceneCreatureFaceInspectionPolicy(values);
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(ProjectSettings.GlobalizePath("res://../preview-profiles/scene-object-catalog.json")));var recipe=catalog.Require(2937962913);
        using var bankFile=File.OpenRead(ProjectSettings.GlobalizePath("res://../preview-profiles/human-locomotion.json"));var bank=SkeletalAnimationBankProfile.Load(bankFile);
        var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        var create=new SceneCreateObjectByCrcMessage(0xF122334455667788,0,0,0,1,0,0,0,2937962913,255);
        var raw=new Dictionary<string,byte[]>();
        Task<SceneCreatureResources> Load(bool face)
        {
            byte[] copy=cfg.ToArray();var task=Task.Run(()=>
            {
                try
                {
                    using var text=new StringReader(Encoding.UTF8.GetString(copy));using var overlay=ConfiguredTreOverlay.Open(root,text);var seen=new HashSet<string>();
                    TreLookupResult Resolve(string path)
                    {
                        cancel.Token.ThrowIfCancellationRequested();if(canceled){entered.TrySetResult(true);if(!cancel.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10)))throw new InvalidDataException("Cancel deadline");cancel.Token.ThrowIfCancellationRequested();}
                        Check(seen.Add(path),"one owned lookup");var result=overlay.Lookup(path);if(face&&result.Payload is not null)raw.Add(path,result.Payload.ToArray());return result;
                    }
                    var bundle=CreatureAppearanceDescriptorBundleReader.Load(Resolve,recipe.ObjectPath,recipe.ObjectSha256,recipe.AppearanceSha256,
                        limits:new(MaximumResources:128,MaximumSourceBytes:32*1024*1024),loadSkeletons:true,loadSkinSources:true,loadGeometry:true,loadLogicalTables:true,
                        animationSelection:idle,prepareSelectedAnimation:true,loadMaterialDefaults:true,loadPaletteDefaults:true,loadAlbedoFactors:true,
                        loadFaceBlueprints:face,loadFaceResources:face,loadFacePixelPrograms:face,additionalAnimationSelections:bank);
                    cancel.Token.ThrowIfCancellationRequested();
                    if(face)
                    {
                        void Reject(Action action){bool failed=false;try{action();}catch(InvalidDataException){failed=true;}Check(failed,"invalid face input rejected before GPU");}
                        var missing=policy.Values.ToDictionary(p=>p.Key,p=>p.Value);missing.Remove(missing.Keys.First());Reject(()=>BlueprintGeneratedFaceBuilder.Create(bundle.Geometries!,bundle.FaceBlueprints!,bundle.FaceResources!,missing));
                        var extra=policy.Values.ToDictionary(p=>p.Key,p=>p.Value);extra.Add(new("not_declared",false),0);Reject(()=>BlueprintGeneratedFaceBuilder.Create(bundle.Geometries!,bundle.FaceBlueprints!,bundle.FaceResources!,extra));
                        var geometry=bundle.Geometries!.ToDictionary(p=>p.Key,p=>p.Value);string path=geometry.First(p=>p.Value.TextureRenderers.Count>0).Key;var original=geometry[path];
                        geometry[path]=original with{TextureRenderers=original.TextureRenderers.Select(t=>t with{Bindings=t.Bindings.Select(b=>b with{TextureTag=0}).ToArray()}).ToArray()};
                        Reject(()=>BlueprintGeneratedFaceBuilder.Create(geometry,bundle.FaceBlueprints!,bundle.FaceResources!,policy.Values));
                        bundle=bundle with{GeneratedFaces=BlueprintGeneratedFaceBuilder.Create(bundle.Geometries!,bundle.FaceBlueprints!,bundle.FaceResources!,policy.Values,cancel.Token)};
                    }
                    long decoded=bundle.MaterialBundle!.Textures.Concat(bundle.FaceResources?.Textures??new Dictionary<string,DdsImage>()).GroupBy(p=>p.Key).Sum(g=>g.First().Value.Mips.Sum(m=>(long)m.Rgba.Length))+
                        (bundle.GeneratedFaces?.Outputs.Values.Sum(f=>f.Image.Mips.Sum(m=>(long)m.Rgba.Length))??0);
                    if(decoded>policy.MaximumDecodedBytes)throw new AppearanceGraphBudgetException("Inspection decoded budget.");
                    return new SceneCreatureResources(create,recipe,bundle,seen.Count,bundle.Sources.Values.Sum(s=>(long)s.Bytes));
                }finally{Array.Clear(copy);Interlocked.Increment(ref archivesClosed);}
            },cancel.Token);jobs.Add(task);return task;
        }
        if(canceled)
        {
            var job=Load(true);Check(await Task.WhenAny(entered.Task,job)==entered.Task,"archive entered");cancel.Cancel();try{await job;}catch(OperationCanceledException){}
            await Shutdown();Array.Clear(cfg);Check(archivesClosed==1&&SceneCreatureVisual.Allocations==0,"canceled publication zero");GD.Print("PASS: face canceled publications=0 archives_closed=1 drain_complete=true");return;
        }
        var standard=await Load(false);var generated=await Load(true);Array.Clear(cfg);
        var faces=generated.Creature.GeneratedFaces!;Check(faces.Outputs.Count==1&&faces.Bindings.Count==1,"one exact face destination");
        var binding=faces.Bindings.Single();Check(binding.Key.ShaderIndex==0&&binding.Key.TextureTag==0x4D41494E&&binding.Key.MeshPath.Contains("head"),"head slot0 MAIN");
        foreach(var factor in standard.Creature.MaterialBundle!.AlbedoFactors!.Materials)Check(generated.Creature.MaterialBundle!.AlbedoFactors!.Materials[factor.Key]==factor.Value,"retained body mouth head factors");
        var evidence=generated.Creature.FaceBlueprints!.Select(p=>
        {
            var prepared=BlueprintFacePreparer.Prepare(p.Value,generated.Creature.FaceResources!,policy.Values);var composed=faces.Outputs[p.Key];
            return new{blueprint=new{path=p.Key,p.Value.Width,p.Value.Height,p.Value.CameraScalar,p.Value.RenderSets},
                prepared=new{path=p.Key,requested=0,prepared.ShaderSlots},composed=new{path=p.Key,requested=0,policy=BlueprintComposedFace.Policy,composed.Image.Width,composed.Image.Height,composed.Layers,sha256=Convert.ToHexString(SHA256.HashData(composed.Image.Mips[0].Rgba.Span))}};
        }).ToArray();
        File.WriteAllText(export,JsonSerializer.Serialize(new{raw=raw.ToDictionary(p=>p.Key,p=>Convert.ToBase64String(p.Value)),evidence,defaultReads=standard.ExternalReads,generatedReads=generated.ExternalReads,defaultBytes=standard.SourceBytes,generatedBytes=generated.SourceBytes,binding=new{binding.Key.MeshPath,binding.Key.ShaderIndex,binding.Key.TextureTag,binding.Value.ShaderPath,binding.Value.RendererPath}}));raw.Clear();
        environment=new(){BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new Color("#14202e")};AddChild(new WorldEnvironment{Environment=environment});
        var camera=new Camera3D{Current=true,Fov=32,Position=new Vector3(0,1.05f,-4.2f)};AddChild(camera);camera.LookAt(new Vector3(0,1.05f,0));
        SceneCreatureVisual Publish(SceneCreatureResources source)=>SceneCreatureVisual.TryPublishCurrent(source,Transform3D.Identity,this,()=>!closed)??throw new InvalidDataException("Owned transfer");
        visual=Publish(standard);visual.SeekIdle(0);visual.SetPlaying(false);Check(visual.Verify(gpu),"default GPU boundary");if(capture is not null)await Capture(Captures(capture)[0]);visual.Dispose();
        visual=Publish(generated);visual.SeekIdle(0);visual.SetPlaying(false);Check(visual.Verify(gpu),"generated actual texture binding and bytes");
        string Identity()
        {
            var meshes=new List<object>();foreach(var node in visual.Root.GetChild(0).GetChildren().OfType<MeshInstance3D>())for(int surface=0;surface<node.Mesh.GetSurfaceCount();surface++)
            {var material=(ShaderMaterial)node.Mesh.SurfaceGetMaterial(surface);var texture=material.GetShaderParameter("albedo_texture").AsGodotObject();meshes.Add(new{mesh=node.Mesh.GetInstanceId(),surface,material=material.GetInstanceId(),shader=material.Shader.GetInstanceId(),texture=texture.GetInstanceId()});}
            Check(meshes.Count==5,"five inspected material surfaces");return JsonSerializer.Serialize(meshes);
        }
        string identity=Identity();GD.Print("face_destination="+JsonSerializer.Serialize(new{binding.Key.MeshPath,binding.Key.ShaderIndex,binding.Key.TextureTag,binding.Value.ShaderPath,binding.Value.RendererPath,identity}));
        if(capture is not null)await Capture(Captures(capture)[1]);
        foreach(string clip in new[]{"walk","run"})for(int frame=0;frame<4;frame++){Check(visual.SetMovement(clip,frame,.25)&&visual.SetEngineTransform(SceneCreatureStatePresentation.DeriveTransform(Transform3D.Identity,1.4f))&&visual.Verify(gpu)&&Identity()==identity,"animation Height retain generated binding identities");}
        visual.SeekIdle(0);visual.SetEngineTransform(Transform3D.Identity);Check(visual.Verify(gpu)&&Identity()==identity,"reset retained materials");
        GD.Print($"PASS: face generated destination=head_slot0_MAIN default_reads={standard.ExternalReads} generated_reads={generated.ExternalReads} default_bytes={standard.SourceBytes} generated_bytes={generated.SourceBytes} pre_gpu_negatives=3 animation_height_checks=8 retained_material_texture_ids=true");
        await Shutdown();Check(SceneCreatureVisual.Allocations==2&&SceneCreatureVisual.Disposals==2&&archivesClosed==2,"owned final drain");GD.Print("PASS: face teardown roots=2/2 archives_closed=2 drain_complete=true");
    }
    public static string[] Captures(string first)=>new[]{first,Path.Combine(Path.GetDirectoryName(first)!,Path.GetFileNameWithoutExtension(first)+"-generated.png")};
    private async Task Capture(string path){await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=GetViewport().GetTexture().GetImage();if(image.SavePng(path)!=Error.Ok)throw new IOException("Face capture");}
    private async Task Shutdown(){if(closed)return;closed=true;visual?.Dispose();cancel.Cancel();foreach(var job in jobs)try{await job;}catch(OperationCanceledException){}Check(jobs.All(j=>j.IsCompleted),"all jobs drained");environment?.Dispose();environment=null;}
    private static void Check(bool value,string message){if(!value)throw new InvalidDataException(message);}
    public override void _ExitTree(){closed=true;visual?.Dispose();cancel.Cancel();environment?.Dispose();}
}
