using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;

internal static class SceneCreatureFaceResourceScenarios
{
    private static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
    private static string Hash(ReadOnlySpan<byte> bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
    private static string Fingerprint(SceneObjectResources resources)
    {
        var c=resources.Creature!;
        return Hash(JsonSerializer.SerializeToUtf8Bytes(new {Payload=c with {Creature=c.Creature with {GeneratedFaces=null}},
            Outputs=c.Creature.GeneratedFaces?.Outputs.Select(p=>new {p.Key,p.Value}),
            Bindings=c.Creature.GeneratedFaces?.Bindings.Select(p=>new {p.Key,p.Value}),
            Values=c.Creature.GeneratedFaces?.Values.Select(p=>new {p.Key,p.Value})}));
    }
    private static void Emit(string tag,object value)=>Console.WriteLine(tag+"="+JsonSerializer.Serialize(value));
    internal static void Run(string[] args)
    {
        Check(args.Length==5,"root/config/catalog/bank/explicit-face-profile required");
        Check(Hash(File.ReadAllBytes(args[1]))==RetainedSkeletalDetailBundleReader.ConfigurationPin,"Config pin");
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(args[2]));using var bankStream=File.OpenRead(args[3]);var bank=SkeletalAnimationBankProfile.Load(bankStream);
        using var profileStream=File.OpenRead(args[4]);var originalValues=BlueprintFaceCustomizationProfile.Load(profileStream);
        var mutable=originalValues.ToDictionary(p=>p.Key,p=>p.Value);var policy=new SceneCreatureFaceInspectionPolicy(mutable);mutable.Clear();Check(policy.Values.Count==originalValues.Count,"Frozen policy snapshots mutable caller values");
        var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        var create=new SceneCreateObjectByCrcMessage(101,-0f,0,0,1,10,20,30,2937962913,0);
        var defaultOptions=new SceneCreatureResourceOptions(idle,bank,true);
        SceneCreatureResourceOptions Options(SceneCreatureFaceInspectionPolicy p,int count=128,long bytes=33554432)=>new(idle,bank,true,count,bytes,p);
        int scopes=0,totalReads=0;List<string> lastOrder=new();var failures=new List<object>();
        SceneObjectResources Load(SceneCreatureResourceOptions options,Action<string,CancellationTokenSource>? trigger=null,CancellationTokenSource? cts=null)
        {
            var borrowed=new List<byte[]>();lastOrder=new();SceneObjectResources loaded;scopes++;
            try{using var cfg=File.OpenText(args[1]);using var overlay=ConfiguredTreOverlay.Open(args[0],cfg);
                loaded=SceneObjectResourceRouter.Load(create,catalog,options,path=>{totalReads++;lastOrder.Add(path);var r=overlay.Lookup(path);if(r.Payload is not null)borrowed.Add(r.Payload);if(cts is not null)trigger?.Invoke(path,cts);return r;},cts?.Token??default);
            }finally{foreach(var b in borrowed)Array.Clear(b);scopes--;}
            Check(scopes==0&&lastOrder.Count==loaded.ExternalReads&&lastOrder.Distinct().Count()==lastOrder.Count,"Unique closure and archives closed");return loaded;
        }
        long Decoded(SceneObjectResources r)=>r.Creature!.Creature.MaterialBundle!.Textures.Concat(r.Creature.Creature.FaceResources?.Textures??new Dictionary<string,DdsImage>()).GroupBy(p=>p.Key,StringComparer.Ordinal).Sum(g=>g.First().Value.Mips.Sum(m=>(long)m.Rgba.Length))+(r.Creature.Creature.GeneratedFaces?.Outputs.Values.Sum(f=>f.Image.Mips.Sum(m=>(long)m.Rgba.Length))??0);
        var plain=Load(defaultOptions);string plainHash=Fingerprint(plain);
        Check(plain.ExternalReads==39&&plain.SourceBytes==4686371&&plain.Creature!.Creature.GeneratedFaces is null,"Default39-source mode unchanged");
        var full=Load(Options(policy));string fullHash=Fingerprint(full);var c=full.Creature!.Creature;var generated=c.GeneratedFaces!;long decoded=Decoded(full);string finalPath=lastOrder[^1];
        Check(c.FaceBlueprints is {Count:1}&&c.FaceResources?.PixelPrograms is {Count:>0}&&generated.Outputs.Count==1&&generated.Bindings.Count==1,"Owned face-only extension");
        var binding=generated.Bindings.Single();Check(binding.Key==new BlueprintGeneratedTextureKey("appearance/mesh/hum_m_head_l0.mgn",0,0x4D41494E)&&binding.Value.RendererPath=="texturerenderer/hum_m_face.trt"&&binding.Value.ShaderPath=="shader/hum_m_face.sht","Exact head MAIN destination");
        Check(generated.Values.OrderBy(p=>p.Key.VariableName).SequenceEqual(policy.Values.OrderBy(p=>p.Key.VariableName))&&c.CustomizationValues is null,"Exact explicit face values; no global customization");
        Check(Hash(JsonSerializer.SerializeToUtf8Bytes(c.MaterialBundle))==Hash(JsonSerializer.SerializeToUtf8Bytes(plain.Creature!.Creature.MaterialBundle))&&Hash(JsonSerializer.SerializeToUtf8Bytes(c.PreparedAnimation))==Hash(JsonSerializer.SerializeToUtf8Bytes(plain.Creature.Creature.PreparedAnimation)),"Other material/prepared animation fields preserved");
        var comparable=c with {Sources=plain.Creature.Creature.Sources,FaceBlueprints=null,FaceResources=null,GeneratedFaces=null};
        Check(Hash(JsonSerializer.SerializeToUtf8Bytes(comparable))==Hash(JsonSerializer.SerializeToUtf8Bytes(plain.Creature.Creature)),"All non-face descriptor fields preserved");
        Check(Hash(generated.Outputs.Single().Value.Image.Mips[0].Rgba.Span)=="A41EE8925C695A8BC2DA0CAC1FA17864D471270507DF3900472F0655A5A42808","Established supplied-zero composed face hash");
        var aliases=c.MaterialBundle!.Textures.Keys.Intersect(c.FaceResources!.Textures.Keys,StringComparer.Ordinal).OrderBy(p=>p).Select(path=>
        {
            var a=c.MaterialBundle!.Textures[path];var b=c.FaceResources!.Textures[path];
            bool equivalent=a.Format==b.Format&&a.ColorSpace==b.ColorSpace&&a.AlphaMode==b.AlphaMode&&a.DxgiFormat==b.DxgiFormat&&a.Mips.Count==b.Mips.Count&&
                a.Mips.Zip(b.Mips).All(p=>p.First.Width==p.Second.Width&&p.First.Height==p.Second.Height&&p.First.Rgba.Span.SequenceEqual(p.Second.Rgba.Span));
            Check(equivalent,"Cross-map original texture aliases are byte/metadata equivalent");
            return new {Path=path,SameImageReference=ReferenceEquals(a,b),Equivalent=equivalent,Mips=a.Mips.Count};
        }).ToArray();
        Emit("face_resource",new {full.ExternalReads,full.SourceBytes,DecodedBytes=decoded,Aliases=aliases,Sources=c.Sources.Values.OrderBy(s=>s.Path),
            Bindings=generated.Bindings.Select(p=>new {p.Key.MeshPath,p.Key.ShaderIndex,p.Key.TextureTag,p.Value.RendererPath,p.Value.ShaderPath}),
            Outputs=generated.Outputs.Select(p=>new {RendererPath=p.Key,p.Value.Image.Width,p.Value.Image.Height,Bytes=p.Value.Image.Mips.Sum(m=>(long)m.Rgba.Length),Mip0Sha256=Hash(p.Value.Image.Mips[0].Rgba.Span)}),profileSHA256=Hash(File.ReadAllBytes(args[4]))});
        var exact=Load(Options(new(policy.Values,decoded),full.ExternalReads,full.SourceBytes));Check(Decoded(exact)==decoded,"Exact source/decoded limits succeed");
        void Bad<T>(string name,Func<SceneObjectResources> action)where T:Exception
        {SceneObjectResources? prefix=null;try{prefix=action();}catch(T){Check(prefix is null&&scopes==0,"No result prefix and closed failure archives");failures.Add(new{name,rejected=true});return;}throw new Exception("Accepted invalid "+name);}
        var missing=policy.Values.ToDictionary(p=>p.Key,p=>p.Value);missing.Remove(missing.Keys.First());Bad<InvalidDataException>("missing-key",()=>Load(Options(new(missing))));
        var extra=policy.Values.ToDictionary(p=>p.Key,p=>p.Value);extra.Add(new("extra",true),0);Bad<InvalidDataException>("extra-key",()=>Load(Options(new(extra))));
        var wrong=policy.Values.ToDictionary(p=>p.Key,p=>p.Value);var first=wrong.First(p=>!wrong.ContainsKey(new(p.Key.VariableName,!p.Key.FactoryFlag)));wrong.Remove(first.Key);wrong.Add(new(first.Key.VariableName,!first.Key.FactoryFlag),first.Value);Bad<InvalidDataException>("wrong-domain",()=>Load(Options(new(wrong))));
        Bad<ArgumentException>("invalid-key",()=>Load(Options(new(new Dictionary<BlueprintCustomizationKey,int>{{new("invalid key",false),0}}))));
        Bad<AppearanceGraphBudgetException>("source-count",()=>Load(Options(policy,full.ExternalReads-1)));
        Bad<AppearanceGraphBudgetException>("source-bytes",()=>Load(Options(policy,128,full.SourceBytes-1)));
        Bad<AppearanceGraphBudgetException>("decoded-one-below",()=>Load(Options(new(policy.Values,decoded-1))));
        Bad<AppearanceGraphBudgetException>("decoded-before-compose",()=>Load(Options(new(policy.Values,4))));
        Bad<ArgumentException>("direct-face-without-albedo",()=>{var r=SceneCreatureResourceReader.Load(create,catalog,idle,bank,_=>throw new Exception("No read expected"),loadAlbedoFactors:false,faceInspection:policy);throw new Exception("Unexpected direct reader result "+r.ExternalReads);});
        using(var cancel=new CancellationTokenSource()){cancel.Cancel();int before=totalReads;Bad<OperationCanceledException>("pre-cancel",()=>Load(Options(policy),cts:cancel));Check(totalReads==before,"Precancel zero resolver calls");}
        using(var cancel=new CancellationTokenSource()){Bad<OperationCanceledException>("first-read-cancel",()=>Load(Options(policy),(p,t)=>t.Cancel(),cancel));Check(lastOrder.Count==1,"First read cancellation exact");}
        bool compositionCancelled=false;
        using(var cancel=new CancellationTokenSource())
        {
            SceneObjectResources? prefix=null;
            try{prefix=Load(Options(policy),(p,t)=>{if(p==finalPath)t.CancelAfter(10);},cancel);}
            catch(OperationCanceledException e){Check(prefix is null&&scopes==0,"Composition cancellation has no result prefix");compositionCancelled=e.StackTrace?.Contains("BlueprintFaceCompositor")==true;failures.Add(new{name="composition-cancel",rejected=true,compositionCancelled});}
        }
        Check(compositionCancelled,"Cancellation observed inside existing compositor");
        foreach(int requested in new[]{int.MinValue,int.MaxValue})
        {
            var supplied=policy.Values.ToDictionary(p=>p.Key,_=>requested);var fallback=Load(Options(new(supplied)));Check(fallback.Creature!.Creature.GeneratedFaces!.Values.Values.All(v=>v==requested),"Established fallback keeps supplied integers");
            Emit("face_fallback",new{requested,sha256=Hash(fallback.Creature.Creature.GeneratedFaces.Outputs.Single().Value.Image.Mips[0].Rgba.Span)});
        }
        Check(plainHash==Fingerprint(plain)&&fullHash==Fingerprint(full),"Successful resources preserved across failures");
        Emit("face_route",new{full.ExternalReads,full.SourceBytes,DecodedBytes=decoded,DefaultReads=plain.ExternalReads,DefaultBytes=plain.SourceBytes,ProfileSha256=Hash(File.ReadAllBytes(args[4])),Sources=c.Sources.Values.OrderBy(s=>s.Path),MaterialTextures=c.MaterialBundle!.Textures.Select(p=>new{path=p.Key,mips=p.Value.Mips.Select(m=>new{m.Width,m.Height,Bytes=m.Rgba.Length,sha256=Hash(m.Rgba.Span)})}),FaceTextures=c.FaceResources!.Textures.Select(p=>new{path=p.Key,mips=p.Value.Mips.Select(m=>new{m.Width,m.Height,Bytes=m.Rgba.Length,sha256=Hash(m.Rgba.Span)})}),Values=policy.Values.Select(p=>new{p.Key.VariableName,p.Key.FactoryFlag,Value=p.Value}),Unchanged=true,ArchiveClosed=scopes==0,SnapshotOwned=true});
        foreach(var pair in c.FaceBlueprints!)
        {
            var prepared=BlueprintFacePreparer.Prepare(pair.Value,c.FaceResources!,policy.Values);var composed=generated.Outputs[pair.Key];
            Emit("face_blueprint",new{path=pair.Key,pair.Value.Width,pair.Value.Height,pair.Value.CameraScalar,pair.Value.RenderSets});
            Emit("face_prepared",new{path=pair.Key,prepared.ShaderSlots});
            Emit("face_composition",new{path=pair.Key,requested=0,policy=BlueprintComposedFace.Policy,composed.Image.Width,composed.Image.Height,composed.Layers,sha256=Hash(composed.Image.Mips[0].Rgba.Span)});
        }
        Emit("generated_face_binding",new{binding.Key.MeshPath,binding.Key.ShaderIndex,binding.Key.TextureTag,binding.Value.RendererPath,binding.Value.ShaderPath,sha256=Hash(generated.Outputs[binding.Value.RendererPath].Image.Mips[0].Rgba.Span)});
        Emit("face_cases",failures);Emit("face_summary",new{Negatives=failures.Count,compositionCancelled,ArchiveScopes=scopes,OtherFieldsPreserved=true,NoGlobalCustomization=true,ExactLimits=true});
        Console.WriteLine("PASS: default and explicit original face route, independent-ready composition, budgets/value closure and cancellation cleanup.");
    }
}
