using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;

internal static class SceneCreatureFaceValidationScenarios
{
    private static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
    private static string Hash(ReadOnlySpan<byte> bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
    // Test-only wrapper around the frozen internal typed result constructor.
    private static SceneObjectResources Wrap(SceneCreatureResources payload)=>(SceneObjectResources)typeof(SceneObjectResources).GetConstructor(BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(SceneCreatureResources)},null)!.Invoke(new object?[]{payload});
    private static string Fingerprint(SceneObjectResources result)
    {
        var c=result.Creature!;var generated=c.Creature.GeneratedFaces!;
        return Hash(JsonSerializer.SerializeToUtf8Bytes(new {Common=c with {Creature=c.Creature with {GeneratedFaces=null}},generated.Outputs,Bindings=generated.Bindings.ToArray(),Values=generated.Values.ToArray()}));
    }
    internal static void Run(string[] args)
    {
        Check(args.Length==5,"root config catalog bank face-profile required");
        Check(Hash(File.ReadAllBytes(args[1]))==RetainedSkeletalDetailBundleReader.ConfigurationPin,"Config pin");
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(args[2])); using var bankStream=File.OpenRead(args[3]); var bank=SkeletalAnimationBankProfile.Load(bankStream);
        using var profileStream=File.OpenRead(args[4]);var values=BlueprintFaceCustomizationProfile.Load(profileStream);
        var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        var options=new SceneCreatureResourceOptions(idle,bank,true,128,33554432,new(values));
        var defaults=new SceneCreatureResourceOptions(idle,bank,true,128,33554432);
        var create=new SceneCreateObjectByCrcMessage(101,0,0,0,1,10,20,30,2937962913u,0);var recipe=catalog.Require(create.TemplateCrc);
        CreatureAppearanceDescriptorBundle bundle;SceneObjectResources ordinary;int reads=0,scopes=0;var borrowed=new List<byte[]>();
        using(var cfg=File.OpenText(args[1]))using(var overlay=ConfiguredTreOverlay.Open(args[0],cfg))
        {
            scopes++;TreLookupResult Read(string p){reads++;var r=overlay.Lookup(p);if(r.Payload is not null)borrowed.Add(r.Payload);return r;}
            ordinary=SceneObjectResourceRouter.Load(create,catalog,defaults,Read);
            bundle=CreatureAppearanceDescriptorBundleReader.Load(Read,recipe.ObjectPath,recipe.ObjectSha256,recipe.AppearanceSha256,
                limits:new(MaximumResources:128,MaximumSourceBytes:33554432),loadSkeletons:true,loadSkinSources:true,loadGeometry:true,
                loadLogicalTables:true,animationSelection:idle,prepareSelectedAnimation:true,loadMaterialDefaults:true,loadPaletteDefaults:true,
                loadFaceBlueprints:true,loadFaceResources:true,loadFacePixelPrograms:true,loadAlbedoFactors:true,additionalAnimationSelections:bank);
            foreach(var b in borrowed)Array.Clear(b); scopes--;
        }
        // The baseline readers own separate decodes; verify overlap but retain it to exercise face-only equivalence.
        var faceTextures=bundle.FaceResources!.Textures.ToDictionary(p=>p.Key,p=>p.Value);int canonicalAliases=0;
        foreach(var p in bundle.MaterialBundle!.Textures)if(faceTextures.TryGetValue(p.Key,out var image))
        {
            Check(JsonSerializer.Serialize(p.Value)==JsonSerializer.Serialize(image),"Repeated source must have identical decoded metadata/pixels");canonicalAliases++;
        }
        bundle=bundle with {FaceResources=bundle.FaceResources with {Textures=faceTextures}};
        var generated=BlueprintGeneratedFaceBuilder.Create(bundle.Geometries!,bundle.FaceBlueprints!,bundle.FaceResources!,values);
        bundle=bundle with {GeneratedFaces=generated};var payload=new SceneCreatureResources(create,recipe,bundle,bundle.Sources.Count,bundle.Sources.Values.Sum(s=>(long)s.Bytes));var result=Wrap(payload);
        int beforeReads=reads;string before=Fingerprint(result);
        SceneObjectResourceValidation.Validate(ordinary,create,catalog,defaults);Check(SceneObjectResourceAccounting.Measure(ordinary).GeneratedFaces==0,"Default receipt");
        SceneObjectResourceValidation.Validate(result,create,catalog,options);var receipt=SceneObjectResourceAccounting.Measure(result);
        Task.Factory.StartNew(()=>SceneObjectResourceValidation.Validate(result,create,catalog,options),CancellationToken.None,TaskCreationOptions.LongRunning,TaskScheduler.Default).GetAwaiter().GetResult();
        Check(reads==beforeReads&&scopes==0&&before==Fingerprint(result),"Closed no-I/O immutable validation");
        var negatives=new List<string>();
        void Reject(string name,Action action){try{action();}catch(Exception e)when(e is InvalidDataException or OverflowException or OperationCanceledException){negatives.Add(name);return;}throw new Exception("Accepted "+name);}
        void Bad(string name,CreatureAppearanceDescriptorBundle changed,SceneCreatureResourceOptions? selected=null)=>Reject(name,()=>SceneObjectResourceValidation.Validate(Wrap(payload with {Creature=changed,ExternalReads=changed.Sources.Count,SourceBytes=changed.Sources.Values.Sum(s=>(long)s.Bytes)}),create,catalog,selected??options));
        Bad("default-face-reject",bundle,defaults);Reject("opt-in-missing-face",()=>SceneObjectResourceValidation.Validate(ordinary,create,catalog,options));
        Bad("blueprints-null",bundle with {FaceBlueprints=null});Bad("resources-null",bundle with {FaceResources=null});Bad("outputs-null",bundle with {GeneratedFaces=null});
        Bad("global-values",bundle with {CustomizationValues=new Dictionary<ShaderCustomizationKey,int>()});Bad("blend-extension",bundle with {BlendShapes=new Dictionary<string,SkeletalBlendShapeDescription>()});
        Bad("values-missing",bundle with {GeneratedFaces=generated with {Values=new Dictionary<BlueprintCustomizationKey,int>()}});
        var wrongValues=values.ToDictionary(p=>p.Key,p=>p.Value);var firstKey=wrongValues.Keys.First();wrongValues[firstKey]=unchecked(wrongValues[firstKey]+1);
        Bad("values-differ",bundle with {GeneratedFaces=generated with {Values=wrongValues}});Bad("option-values-differ",bundle,new(idle,bank,true,128,33554432,new(wrongValues)));
        Bad("bindings-missing",bundle with {GeneratedFaces=generated with {Bindings=new Dictionary<BlueprintGeneratedTextureKey,BlueprintGeneratedTextureBinding>()}});
        var bindingMap=generated.Bindings.ToDictionary(p=>p.Key,p=>p.Value);var bindingKey=bindingMap.Keys.First();bindingMap[bindingKey]=bindingMap[bindingKey] with {ShaderPath="shader/wrong.sht"};
        Bad("binding-shader",bundle with {GeneratedFaces=generated with {Bindings=bindingMap}});
        var outputPair=generated.Outputs.First();var output=outputPair.Value;var mip=output.Image.Mips[0];
        CreatureAppearanceDescriptorBundle Output(BlueprintComposedFace changed){var map=generated.Outputs.ToDictionary(p=>p.Key,p=>p.Value);map[outputPair.Key]=changed;return bundle with {GeneratedFaces=generated with {Outputs=map}};}
        Bad("output-format",Output(output with {Image=output.Image with {Format=DdsFormat.Bc1}}));
        Bad("output-dimensions",Output(output with {Image=output.Image with {Mips=new[]{mip with {Width=mip.Width+1}}}}));
        Bad("output-length",Output(output with {Image=output.Image with {Mips=new[]{mip with {Rgba=new byte[4]}}}}));
        Bad("output-extra-mip",Output(output with {Image=output.Image with {Mips=new[]{mip,mip}}}));
        Bad("output-color",Output(output with {Image=output.Image with {ColorSpace=DdsColorSpace.Srgb}}));
        Bad("output-alpha",Output(output with {Image=output.Image with {AlphaMode=DdsAlphaMode.Premultiplied}}));
        Bad("layers-missing",Output(output with {Layers=Array.Empty<BlueprintFaceLayer>()}));
        void Layer(string name,BlueprintFaceLayer layer){var layers=output.Layers.ToArray();layers[0]=layer;Bad(name,Output(output with {Layers=layers}));}
        var firstLayer=output.Layers[0];Layer("layer-path",firstLayer with {TexturePath="texture/wrong.dds"});Layer("layer-index",firstLayer with {ShaderIndex=uint.MaxValue});
        Layer("layer-lod",firstLayer with {MipLevels=new[]{int.MaxValue}});Layer("layer-count",firstLayer with {WrittenPixels=-1});Layer("layer-hash",firstLayer with {RgbaSha256="bad"});
        var layersLast=output.Layers.ToArray();layersLast[^1]=layersLast[^1] with {RgbaSha256=new string('0',64)};Bad("final-hash",Output(output with {Layers=layersLast}));
        var corrupt=mip.Rgba.ToArray();corrupt[0]^=1;Bad("pixel-hash",Output(output with {Image=output.Image with {Mips=new[]{mip with {Rgba=corrupt}}}}));
        Bad("programs-missing",bundle with {FaceResources=bundle.FaceResources! with {PixelPrograms=new Dictionary<string,BlueprintFacePixelProgram>()}});
        var programs=bundle.FaceResources!.PixelPrograms!.ToDictionary(p=>p.Key,p=>p.Value);string programKey=programs.Keys.First();programs[programKey]=programs[programKey] with {FactorRegister=2};
        Bad("program-register",bundle with {FaceResources=bundle.FaceResources with {PixelPrograms=programs}});
        var tex=bundle.FaceResources.Textures.ToDictionary(p=>p.Key,p=>p.Value);tex.Remove(tex.Keys.First());Bad("texture-missing",bundle with {FaceResources=bundle.FaceResources with {Textures=tex}});
        var sources=bundle.Sources.ToDictionary(p=>p.Key,p=>p.Value);sources.Remove(bundle.FaceBlueprints!.Keys.First());Bad("renderer-source-missing",bundle with {Sources=sources});
        var blueprintMap=bundle.FaceBlueprints.ToDictionary(p=>p.Key,p=>p.Value);string rendererPath=blueprintMap.Keys.First();var blueprint=blueprintMap[rendererPath];
        void Blueprint(string name,BlueprintTextureRendererDescription changed){var map=blueprintMap.ToDictionary(p=>p.Key,p=>p.Value);map[rendererPath]=changed;Bad(name,bundle with {FaceBlueprints=map});}
        Blueprint("blueprint-dimension",blueprint with {Width=1025});Blueprint("blueprint-camera",blueprint with {CameraScalar=float.NaN});
        Blueprint("blueprint-prepare-budget",blueprint with {PrepareLists=new[]{(IReadOnlyList<BlueprintPrepareOperation>)Enumerable.Repeat(blueprint.PrepareLists.SelectMany(l=>l).First(),4097).ToArray()}});
        Blueprint("vertex-source",blueprint with {VertexArrays=Array.Empty<IffNode>()});
        var geometry=bundle.Geometries!.ToDictionary(p=>p.Key,p=>p.Value);string meshPath=geometry.First(p=>p.Value.TextureRenderers.Count>0).Key;var mesh=geometry[meshPath];var renderer=mesh.TextureRenderers[0];
        void Renderer(string name,SkeletalTextureRendererReference changed){var map=geometry.ToDictionary(p=>p.Key,p=>p.Value);map[meshPath]=mesh with {TextureRenderers=new[]{changed}};Bad(name,bundle with {Geometries=map});}
        Renderer("renderer-path",renderer with {Path="texture_renderer/missing.trt"});Renderer("renderer-slot",renderer with {Bindings=new[]{renderer.Bindings[0] with {ShaderIndex=uint.MaxValue}}});
        Renderer("renderer-tag",renderer with {Bindings=new[]{renderer.Bindings[0] with {TextureTag=0}}});Renderer("renderer-empty",renderer with {Bindings=Array.Empty<SkeletalTextureRendererBinding>()});
        var shaders=bundle.FaceResources.Shaders.ToDictionary(p=>p.Key,p=>p.Value);shaders.Remove(shaders.Keys.First());Bad("shader-missing",bundle with {FaceResources=bundle.FaceResources with {Shaders=shaders}});
        var palettes=bundle.FaceResources.Palettes.ToDictionary(p=>p.Key,p=>p.Value);palettes.Remove(palettes.Keys.First());Bad("palette-missing",bundle with {FaceResources=bundle.FaceResources with {Palettes=palettes}});
        var effects=bundle.FaceResources.EffectsByShader.ToDictionary(p=>p.Key,p=>p.Value);string effectKey=effects.Keys.First();effects[effectKey]=effects[effectKey] with {Implementations=Array.Empty<StaticEffectImplementation>()};Bad("effect-missing",bundle with {FaceResources=bundle.FaceResources with {EffectsByShader=effects}});
        var extraOutputs=generated.Outputs.ToDictionary(p=>p.Key,p=>p.Value);extraOutputs.Add("texture_renderer/extra.trt",output);Bad("output-extra",bundle with {GeneratedFaces=generated with {Outputs=extraOutputs}});
        var leafSources=bundle.Sources.ToDictionary(p=>p.Key,p=>p.Value);leafSources[rendererPath]=leafSources[rendererPath] with {Bytes=1};Bad("renderer-metadata-size",bundle with {Sources=leafSources});
        string overlap=bundle.MaterialBundle.Textures.Keys.Single(bundle.FaceResources.Textures.ContainsKey);var repeated=bundle.FaceResources.Textures[overlap];
        var conflicting=bundle.FaceResources.Textures.ToDictionary(p=>p.Key,p=>p.Value);var different=repeated.Mips[0].Rgba.ToArray();different[0]^=1;conflicting[overlap]=repeated with {Mips=new[]{repeated.Mips[0] with {Rgba=different}}.Concat(repeated.Mips.Skip(1)).ToArray()};
        var conflictingBundle=bundle with {FaceResources=bundle.FaceResources with {Textures=conflicting}};Bad("contradictory-cross-map-pixels",conflictingBundle);Reject("accounting-cross-map-pixels",()=>SceneObjectResourceAccounting.Measure(Wrap(payload with {Creature=conflictingBundle})));
        var formatConflict=bundle.FaceResources.Textures.ToDictionary(p=>p.Key,p=>p.Value);formatConflict[overlap]=repeated with {ColorSpace=DdsColorSpace.Srgb};
        Reject("accounting-cross-map-format",()=>SceneObjectResourceAccounting.Measure(Wrap(payload with {Creature=bundle with {FaceResources=bundle.FaceResources with {Textures=formatConflict}}})));
        Reject("generated-face-input-alias",()=>SceneObjectResourceAccounting.Measure(Wrap(payload with {Creature=Output(output with {Image=repeated})})));
        long decoded=receipt.DecodedTextureBytes+receipt.GeneratedFaceBytes;
        SceneObjectResourceValidation.Validate(result,create,catalog,new SceneCreatureResourceOptions(idle,bank,true,128,33554432,new(values,decoded)));
        Bad("combined-budget",bundle,new(idle,bank,true,128,33554432,new(values,decoded-1)));
        using(var cancelled=new CancellationTokenSource()){cancelled.Cancel();Reject("builder-cancel",()=>BlueprintGeneratedFaceBuilder.Create(bundle.Geometries!,bundle.FaceBlueprints!,bundle.FaceResources!,values,cancelled.Token));SceneObjectResourceValidation.Validate(result,create,catalog,options);}
        var aliases=generated.Outputs.ToDictionary(p=>p.Key,p=>p.Value);aliases.Add("synthetic/shared",output with {});
        var aliasResult=Wrap(payload with {Creature=bundle with {GeneratedFaces=generated with {Outputs=aliases}}});Check(SceneObjectResourceAccounting.Measure(aliasResult).GeneratedFaces==receipt.GeneratedFaces,"Shared generated reference dedup");
        aliases["synthetic/shared"]=output with {Layers=new[]{firstLayer}};Reject("contradictory-output-alias",()=>SceneObjectResourceAccounting.Measure(aliasResult));
        var inputAlias=output with {Image=bundle.MaterialBundle.Textures.First().Value};Reject("generated-input-alias",()=>SceneObjectResourceAccounting.Measure(Wrap(payload with {Creature=Output(inputAlias)})));
        var overflow=output with {Image=output.Image with {Mips=new[]{mip with {Width=int.MaxValue,Height=int.MaxValue}}}};Reject("generated-overflow",()=>SceneObjectResourceAccounting.Measure(Wrap(payload with {Creature=Output(overflow)})));
        Check(reads==beforeReads&&before==Fingerprint(result),"All negatives preserve result");
        var compositions=new List<object>();
        foreach(int requested in new[]{0,int.MinValue,int.MaxValue})
        {
            var profile=values.ToDictionary(p=>p.Key,p=>requested);var faces=BlueprintGeneratedFaceBuilder.Create(bundle.Geometries!,bundle.FaceBlueprints!,bundle.FaceResources!,profile);
            var variant=Wrap(payload with {Creature=bundle with {GeneratedFaces=faces}});SceneObjectResourceValidation.Validate(variant,create,catalog,new SceneCreatureResourceOptions(idle,bank,true,128,33554432,new(profile)));
            foreach(var pair in faces.Outputs)compositions.Add(new {path=pair.Key,requested,policy=BlueprintComposedFace.Policy,pair.Value.Image.Width,pair.Value.Image.Height,sha256=Hash(pair.Value.Image.Mips[0].Rgba.Span),pair.Value.Layers,Blueprint=bundle.FaceBlueprints[pair.Key],Prepared=BlueprintFacePreparer.Prepare(bundle.FaceBlueprints[pair.Key],bundle.FaceResources!,profile)});
        }
        Console.WriteLine("face_completion="+JsonSerializer.Serialize(new {Receipt=receipt,DefaultReceipt=SceneObjectResourceAccounting.Measure(ordinary),Sources=bundle.Sources.Values.OrderBy(s=>s.Path),Textures=bundle.MaterialBundle.Textures.Concat(bundle.FaceResources!.Textures).GroupBy(p=>p.Key).Select(g=>g.First()).Select(p=>new {Path=p.Key,Mips=p.Value.Mips.Select(m=>new {m.Width,m.Height,Bytes=m.Rgba.Length})}),Bindings=generated.Bindings.Select(p=>new {p.Key.MeshPath,p.Key.ShaderIndex,p.Key.TextureTag,p.Value.RendererPath,p.Value.ShaderPath,sha256=Hash(generated.Outputs[p.Value.RendererPath].Image.Mips[0].Rgba.Span)}),Compositions=compositions,Negatives=negatives,SeparateInputAliases=canonicalAliases,Reads=reads,ArchiveScopes=scopes,NoReads=true,Unchanged=true}));
        Console.WriteLine("PASS: opt-in face completion, default isolation, combined logical budget and generated accounting.");
    }
}
