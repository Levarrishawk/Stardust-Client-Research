using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class SkeletalDescriptorScenarios
{
    internal static void Run()
    {
        byte[] fixture=Sat();var sat=SkeletalAppearanceDecoder.Decode(fixture);Array.Clear(fixture);
        if(!sat.InfoFlag||sat.MeshGenerators[0]!="appearance/mesh/body.lmg"||sat.Skeletons[0].AttachmentTransform!=""||sat.Skeletons[1].AttachmentTransform!="head"||sat.AnimationTables.Count!=1||sat.DistanceIntervals[0].Lower!=-1||sat.Sfsk!=true||sat.Apag!=false)throw new Exception("Owned SAT descriptor mismatch.");
        var lod=SkeletalAppearanceDecoder.DecodeLodMesh(Lod(8));
        if(lod.SerializedPaths.Count!=8||lod.ExposedPaths().Count!=4||lod.ExposedPaths(true)[0]!="appearance/mesh/l1.mgn")throw new Exception("MLOD exposed cap/skip mismatch.");
        if(SkeletalAppearanceDecoder.DecodeLodMesh(Lod(-1)).SerializedPaths.Count!=0||SkeletalAppearanceDecoder.DecodeLodMesh(Lod(1)).ExposedPaths(true).Count!=1)throw new Exception("MLOD negative/singleton policy mismatch.");
        void Reject(Action a)=>HostAccountingScenarios.Reject<InvalidDataException>(a);
        byte[] valid=Sat();for(int i=0;i<valid.Length;i++)Reject(()=>SkeletalAppearanceDecoder.Decode(valid.AsSpan(0,i)));
        byte[] mlod=Lod(4);for(int i=0;i<mlod.Length;i++)Reject(()=>SkeletalAppearanceDecoder.DecodeLodMesh(mlod.AsSpan(0,i)));
        Reject(()=>SkeletalAppearanceDecoder.Decode(Sat(flag:2)));
        Reject(()=>SkeletalAppearanceDecoder.Decode(Sat(meshCount:65)));
        Reject(()=>SkeletalAppearanceDecoder.Decode(Sat(tail:true)));
        Reject(()=>SkeletalAppearanceDecoder.Decode(Sat(distance:float.NaN)));
        Reject(()=>SkeletalAppearanceDecoder.DecodeLodMesh(Lod(65)));
        byte[] wrongVersion=Sat();wrongVersion[23]=(byte)'4';Reject(()=>SkeletalAppearanceDecoder.Decode(wrongVersion));
        byte[] wrongLod=Lod(1);wrongLod[23]=(byte)'1';Reject(()=>SkeletalAppearanceDecoder.DecodeLodMesh(wrongLod));
        byte[] badName=Lod(1);badName[^1]=1;Reject(()=>SkeletalAppearanceDecoder.DecodeLodMesh(badName));
        byte[] extraName=Lod(1).Concat(new byte[]{0}).ToArray();Reject(()=>SkeletalAppearanceDecoder.DecodeLodMesh(extraName));
        int reads=0;HostAccountingScenarios.Reject<ArgumentException>(()=>CreatureAppearanceDescriptorBundleReader.Load(p=>{reads++;return new(TreLookupKind.Missing);},"object/x.iff","bad",new string('0',64)));
        HostAccountingScenarios.Reject<ArgumentException>(()=>CreatureAppearanceDescriptorBundleReader.Load(p=>{reads++;return new(TreLookupKind.Missing);},"object/x.iff",new string('0',64),new string('0',64),loadMaterialDefaults:true));
        HostAccountingScenarios.Reject<ArgumentException>(()=>CreatureAppearanceDescriptorBundleReader.Load(p=>{reads++;return new(TreLookupKind.Missing);},"object/x.iff",new string('0',64),new string('0',64),loadPaletteDefaults:true));
        HostAccountingScenarios.Reject<ArgumentException>(()=>CreatureAppearanceDescriptorBundleReader.Load(p=>{reads++;return new(TreLookupKind.Missing);},"object/x.iff",new string('0',64),new string('0',64),loadFaceResources:true));
        if(reads!=0)throw new Exception("Descriptor invalid option reached lookup.");
        Console.WriteLine("PASS: owned SMAT 0003/MLOD 0000 descriptors, raw flags/attachments/hysteresis intervals, four-level cap/skip-L0 and malformed width/count/string/version gates");
    }
    private static byte[] Text(params string[] s)=>Encoding.ASCII.GetBytes(string.Join('\0',s)+"\0");
    private static byte[] Sat(byte flag=1,uint meshCount=1,bool tail=false,float distance=10)
    {
        List<byte[]> parts=new(){MeshScenarios.Chunk("INFO",BitConverter.GetBytes(meshCount).Concat(BitConverter.GetBytes(2u)).Append(flag).ToArray()),
            MeshScenarios.Chunk("MSGN",Text("appearance/mesh/body.lmg")),MeshScenarios.Chunk("SKTI",Text("appearance/skeleton/body.skt","","appearance/skeleton/face.skt","head")),
            MeshScenarios.Chunk("LATX",BitConverter.GetBytes((ushort)1).Concat(Text("appearance/skeleton/body.skt","appearance/lat/body.lat")).ToArray()),
            MeshScenarios.Form("LDTB",MeshScenarios.Form("0000",MeshScenarios.Chunk("INFO",BitConverter.GetBytes((ushort)1).Concat(BitConverter.GetBytes(-1f)).Concat(BitConverter.GetBytes(distance)).ToArray()))),
            MeshScenarios.Chunk("SFSK",new byte[]{1}),MeshScenarios.Chunk("APAG",new byte[]{0})};
        if(tail)parts.Add(MeshScenarios.Chunk("WHAT",Array.Empty<byte>()));return MeshScenarios.Form("SMAT",MeshScenarios.Form("0003",parts.ToArray()));
    }
    private static byte[] Lod(short count)=>MeshScenarios.Form("MLOD",MeshScenarios.Form("0000",new[]{MeshScenarios.Chunk("INFO",BitConverter.GetBytes(count))}.Concat(Enumerable.Range(0,Math.Max(0,(int)count)).Select(i=>MeshScenarios.Chunk("NAME",Text($"appearance/mesh/l{i}.mgn")))).ToArray()));
    internal static void ProbeOptional(string[] args,bool loadSkeletons=false,bool bindPose=false,bool loadSkinSources=false,bool loadGeometry=false,bool jointBinding=false,bool shaderDeformation=false,bool logicalTables=false,bool selectedAnimation=false,bool compressedClip=false,bool sampleClip=false,bool animationPose=false,bool materials=false,bool paletteDefaults=false,bool faceBlueprints=false,bool faceResources=false,bool facePixelPrograms=false,bool faceCompose=false,bool generatedFaces=false,bool albedoFactors=false,bool customized=false,bool blendShapes=false,bool textureChoices=false,bool compressedNormals=false,bool specular=false,bool vertexLitSpecular=false,bool locomotionBank=false,bool poseTransitions=false)
    {
        if(args.Length!=(customized?12:10)||customized&&args[10]!="--customization-profile"||args[2]!="--configuration"||args[4]!="--member"||args[6]!="--expected-sha256"||args[8]!="--appearance-sha256")throw new ArgumentException("Pinned object-root and SAT descriptor inputs required.");
        using var config=File.OpenText(args[3]);using var overlay=ConfiguredTreOverlay.Open(args[1],config);
        Dictionary<string,int> counts=new(StringComparer.Ordinal);
        TreLookupResult Read(string p){counts[p]=counts.GetValueOrDefault(p)+1;return overlay.Lookup(p);}
        var bundle=CreatureAppearanceDescriptorBundleReader.Load(Read,args[5],args[7],args[9],limits:generatedFaces?new(MaximumResources:128,MaximumSourceBytes:32*1024*1024):null,loadSkeletons:loadSkeletons,loadSkinSources:loadSkinSources,loadGeometry:loadGeometry,loadLogicalTables:logicalTables,animationSelection:selectedAnimation?LogicalAnimationProxyScenarios.IdleSelection:null,prepareSelectedAnimation:animationPose,loadMaterialDefaults:materials,loadPaletteDefaults:paletteDefaults,loadFaceBlueprints:faceBlueprints,loadFaceResources:faceResources,loadFacePixelPrograms:facePixelPrograms,loadAlbedoFactors:albedoFactors,loadBlendShapes:blendShapes,loadTextureChoices:textureChoices,loadCompressedNormals:compressedNormals,loadVertexLitSpecular:vertexLitSpecular,additionalAnimationSelections:locomotionBank?SkeletalAnimationBankScenarios.Profile():null);
        if(locomotionBank)SkeletalAnimationBankScenarios.Probe(bundle,overlay.Lookup,args[5],args[7],args[9]);
        if(poseTransitions)SkeletalPoseTransitionScenarios.Probe(bundle);
        if(compressedNormals)SkeletalCompressedNormalScenarios.Probe(bundle,overlay.Lookup);
        if(textureChoices)
        {
            var content=bundle.MaterialBundle!;var keys=content.Materials.Values.SelectMany(m=>m.Customization!.TextureSelections).Select(s=>new ShaderCustomizationKey(s.VariableName,s.RawPrivateFlag!=0)).Distinct().ToArray();
            foreach(int requested in new[]{int.MinValue,0,1,2,3,int.MaxValue})
            {
                var selected=SkeletalTextureChoices.Apply(content,keys.ToDictionary(k=>k,_=>requested));
                foreach(var pair in selected.TextureSelection!.Selections)Console.WriteLine("skeletal_texture_choice="+JsonSerializer.Serialize(new{path=pair.Key,requested,selections=pair.Value,textures=selected.TextureSelection.References[pair.Key].Textures}));
            }
            foreach(string choice in content.Materials.Values.SelectMany(m=>m.Customization!.TextureChoices).Distinct())foreach(var kind in new[]{TreLookupKind.Missing,TreLookupKind.Masked})
                HostAccountingScenarios.Reject<InvalidDataException>(()=>CreatureAppearanceDescriptorBundleReader.Load(p=>p==choice?new(kind):overlay.Lookup(p),args[5],args[7],args[9],loadSkeletons:true,loadSkinSources:true,loadGeometry:true,loadMaterialDefaults:true,loadTextureChoices:true));
        }
        if(blendShapes)DumpBlendShapes(bundle);
        if(customized)
        {
            using var profile=File.OpenRead(args[11]);bundle=SkeletalAppearanceCustomization.Apply(bundle,SkeletalAppearanceCustomization.LoadProfile(profile));
            Console.WriteLine("skeletal_customization_values="+JsonSerializer.Serialize(bundle.CustomizationValues!.Select(p=>new{p.Key.VariableName,p.Key.Private,p.Value})));
        }
        if(specular)SkeletalSpecularScenarios.Probe(bundle);
        if(vertexLitSpecular)SkeletalVertexLitSpecularScenarios.Probe(bundle,overlay.Lookup);
        foreach(var source in bundle.Sources.Values)Console.WriteLine("skeletal_descriptor_source="+JsonSerializer.Serialize(source));
        Console.WriteLine("skeletal_descriptor="+JsonSerializer.Serialize(new{bundle.Profile.RootPath,bundle.Profile.AppearancePath,bundle.Appearance,bundle.LodMeshes,counts}));
        if(loadSkeletons)
        {
            foreach(var source in bundle.Skeletons!)for(int i=0;i<source.Value.SerializedLevels.Count;i++)
            {
                var level=source.Value.SerializedLevels[i];Console.WriteLine("skeletal_skeleton="+JsonSerializer.Serialize(new{path=source.Key,level=i,joints=level.Joints.Count,sha256=SkeletonTemplateScenarios.CanonicalHash(level)}));
            }
            var body=bundle.Skeletons[bundle.Appearance.Skeletons[0].Path];string attachment=bundle.Appearance.Skeletons[1].AttachmentTransform;
            if(attachment.Length==0||body.ExposedLevels().Any(level=>level.FindTransform(attachment)<0))throw new Exception("Original face attachment absent/ambiguous at an exposed body detail.");
            if(bindPose)
            {
                var face=bundle.Skeletons[bundle.Appearance.Skeletons[1].Path].SerializedLevels[0];
                for(int detail=0;detail<body.ExposedLevels().Count;detail++)
                {
                    var level=body.ExposedLevels()[detail];var pose=SkeletonBindPoseBuilder.Create(level);int head=level.FindTransform(attachment);
                    var facial=SkeletonBindPoseBuilder.Create(face,pose.InverseBind[head]);
                    void Dump(string kind,Stardust.Core.Assets.SkeletonBindPose p){for(int joint=0;joint<p.InverseBind.Count;joint++)Console.WriteLine("skeletal_bind_pose="+JsonSerializer.Serialize(new{detail,kind,joint,matrix=SkeletonBindPoseScenarios.LegacyValues(p.InverseBind[joint])}));}
                    Dump("body",pose);Dump("face",facial);
                }
            }
            HostAccountingScenarios.Reject<InvalidDataException>(()=>CreatureAppearanceDescriptorBundleReader.Load(p=>p==bundle.Appearance.Skeletons[0].Path?new(TreLookupKind.Masked,Priority:54):overlay.Lookup(p),args[5],args[7],args[9],loadSkeletons:true));
        }
        if(counts.Values.Any(n=>n!=1))throw new Exception("Descriptor source repeated externally.");
        if(loadSkinSources)
        {
            foreach(var source in bundle.SkinSources!)
            {
                var s=source.Value;Console.WriteLine("skeletal_skin_source="+JsonSerializer.Serialize(new{path=source.Key,positions=s.Positions.Count,weights=s.Weights.Count,normals=s.Normals.Count,transforms=s.TransformNames.Count,suffix=s.UninterpretedSuffix.Select(n=>n.FormType??n.Tag).ToArray(),sha256=SkeletalSkinSourceScenarios.CanonicalHash(s)}));
            }
            string first=bundle.SkinSources.Keys.First();HostAccountingScenarios.Reject<InvalidDataException>(()=>CreatureAppearanceDescriptorBundleReader.Load(p=>p==first?new(TreLookupKind.Masked,Priority:54):overlay.Lookup(p),args[5],args[7],args[9],loadSkeletons:true,loadSkinSources:true));
        }
        if(loadGeometry)foreach(var source in bundle.Geometries!)
        {
            var g=source.Value;Console.WriteLine("skeletal_geometry="+JsonSerializer.Serialize(new{path=source.Key,source=g.SourceTriangles,degenerate=g.DegenerateTriangles,triangles=g.Triangles,vertices=g.Shaders.Sum(s=>s.PositionIndices.Count),shaders=g.Shaders.Count,tangents=g.Tangents.Count,renderers=g.TextureRenderers.Count,sha256=SkeletalGeometryScenarios.CanonicalHash(g)}));
        }
        if(materials)
        {
            var content=bundle.MaterialBundle!;
            DumpMaterials(content);
            if(paletteDefaults)
            {
                string missing=content.PaletteDefaults!.Values.SelectMany(p=>p.Palettes.Keys).First();
                var fallback=CreatureAppearanceDescriptorBundleReader.Load(p=>p==missing?new(TreLookupKind.Masked,Priority:54):overlay.Lookup(p),args[5],args[7],args[9],loadSkeletons:true,loadSkinSources:true,loadGeometry:true,loadMaterialDefaults:true,loadPaletteDefaults:true);
                var factors=fallback.MaterialBundle!.PaletteDefaults!.Values.SelectMany(p=>p.AuthoredFactors).Where(f=>f.PalettePath==missing).ToArray();
                if(factors.Length==0||factors.Any(f=>!f.MissingPalette||f.Color?.OpaqueArgb!=0xFF00FFFF))throw new Exception("Character palette missing fallback mismatch.");
                HostAccountingScenarios.Reject<InvalidDataException>(()=>CreatureAppearanceDescriptorBundleReader.Load(p=>p==missing?new(TreLookupKind.Present,Payload:new byte[24],Priority:53):overlay.Lookup(p),args[5],args[7],args[9],loadSkeletons:true,loadSkinSources:true,loadGeometry:true,loadMaterialDefaults:true,loadPaletteDefaults:true));
            }
            foreach(string missing in new[]{content.Materials.Keys.First(),content.Effects.Keys.First(),content.Textures.Keys.First()})
                HostAccountingScenarios.Reject<InvalidDataException>(()=>CreatureAppearanceDescriptorBundleReader.Load(p=>p==missing?new(TreLookupKind.Masked,Priority:54):overlay.Lookup(p),args[5],args[7],args[9],loadSkeletons:true,loadSkinSources:true,loadGeometry:true,loadMaterialDefaults:true));
            HostAccountingScenarios.Reject<AppearanceGraphBudgetException>(()=>CreatureAppearanceDescriptorBundleReader.Load(overlay.Lookup,args[5],args[7],args[9],new(MaximumResources:13),loadSkeletons:true,loadSkinSources:true,loadGeometry:true,loadMaterialDefaults:true));
            Console.WriteLine($"PASS original skeletal material defaults materials={content.Materials.Count} textures={content.Textures.Count} effects={content.Effects.Count} palettes_applied=false generated_face=false");
        }
        if(albedoFactors)
        {
            var albedo=bundle.MaterialBundle!.AlbedoFactors!;
            foreach(var pair in albedo.Materials)Console.WriteLine("skeletal_albedo="+JsonSerializer.Serialize(new{path=pair.Key,factors=pair.Value}));
            foreach(var pair in albedo.Programs)
            {
                Console.WriteLine("skeletal_albedo_program="+JsonSerializer.Serialize(new{path=pair.Key,pair.Value.UsesAlphaHueMask,bytecodeSha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(pair.Value.Metadata.Children[0].Children[1].Payload.Span))}));
                foreach(var kind in new[]{TreLookupKind.Missing,TreLookupKind.Masked})HostAccountingScenarios.Reject<InvalidDataException>(()=>CreatureAppearanceDescriptorBundleReader.Load(p=>p==pair.Key?new(kind):overlay.Lookup(p),args[5],args[7],args[9],loadSkeletons:true,loadSkinSources:true,loadGeometry:true,loadMaterialDefaults:true,loadPaletteDefaults:true,loadAlbedoFactors:true));
            }
            if(albedo.Materials.Count!=4||albedo.Programs.Count!=2)throw new Exception("Original albedo factor closure differs.");
        }
        if(faceBlueprints)foreach(var pair in bundle.FaceBlueprints!)
        {
            var d=pair.Value;
            Console.WriteLine("face_blueprint="+JsonSerializer.Serialize(new{path=pair.Key,d.TemplateValue,d.Width,d.Height,d.DestinationFormatIds,d.ShaderPaths,d.TexturePaths,d.CameraScalar,d.RenderSets,d.PrepareLists,vertices=d.VertexArrays.Select(v=>new{declaration=System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(v.Children[0].Children[0].Payload.Span),count=System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(v.Children[0].Children[0].Payload.Span[4..]),sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(v.Children[0].Children[1].Payload.Span))})}));
            byte[] raw=overlay.Lookup(pair.Key).Payload!;
            for(int length=0;length<raw.Length;length++)HostAccountingScenarios.Reject<InvalidDataException>(()=>BlueprintTextureRendererDecoder.Decode(raw.AsSpan(0,length)));
            byte[] wrong=raw.ToArray();wrong[23]=(byte)'2';HostAccountingScenarios.Reject<InvalidDataException>(()=>BlueprintTextureRendererDecoder.Decode(wrong));
            var tree=IffDocument.Parse(raw).Root.Children[0];wrong=raw.ToArray();wrong[tree.Children[1].Offset+16]=5;HostAccountingScenarios.Reject<InvalidDataException>(()=>BlueprintTextureRendererDecoder.Decode(wrong));
            HostAccountingScenarios.Reject<InvalidDataException>(()=>CreatureAppearanceDescriptorBundleReader.Load(p=>p==pair.Key?new(TreLookupKind.Masked,Priority:56):overlay.Lookup(p),args[5],args[7],args[9],loadSkeletons:true,loadSkinSources:true,loadGeometry:true,loadFaceBlueprints:true));
            Console.WriteLine("PASS: owned original face blueprint, every truncated prefix, unsupported version/count and missing/masked dependency gates; generated_face=false");
        }
        if(faceResources)
        {
            var resources=bundle.FaceResources!;
            Console.WriteLine("face_resources="+JsonSerializer.Serialize(new{shaders=resources.Shaders.Keys,effects=resources.EffectsByShader.Select(p=>new{path=p.Key,candidates=p.Value.Implementations.Count}),textures=resources.Textures.Select(p=>new{path=p.Key,format=p.Value.Format.ToString(),terminalBlock=Convert.ToHexString(p.Value.RetainedTerminalBlock.Span),mips=p.Value.Mips.Select(m=>new{m.Width,m.Height,sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(m.Rgba.Span))})}),palettes=resources.Palettes.Select(p=>new{path=p.Key,sha256=Stardust.Core.Proof.PaletteVerification.ComputeSha256(p.Value),entries=p.Value.Entries.Count})}));
            if(resources.Shaders.Count!=2 || resources.Textures.Count!=32 || resources.Palettes.Count!=3 || counts.Values.Any(n=>n!=1))throw new Exception("Original face dependency closure/deduplication differs.");
            foreach(string missing in new[]{resources.Shaders.Keys.First(),resources.Textures.Keys.First(),resources.Palettes.Keys.First()})
                foreach(var kind in new[]{TreLookupKind.Missing,TreLookupKind.Masked})HostAccountingScenarios.Reject<InvalidDataException>(()=>CreatureAppearanceDescriptorBundleReader.Load(p=>p==missing?new(kind):overlay.Lookup(p),args[5],args[7],args[9],loadSkeletons:true,loadSkinSources:true,loadGeometry:true,loadFaceBlueprints:true,loadFaceResources:true));
            if(facePixelPrograms)
            {
                if(resources.PixelPrograms!.Count!=1)throw new Exception("Original pixel-program deduplication differs.");
                foreach(var program in resources.PixelPrograms)
                {
                    Console.WriteLine("face_pixel_program="+JsonSerializer.Serialize(new{path=program.Key,program.Value.TextureStage,program.Value.FactorRegister,bytecode=Convert.ToHexString(program.Value.Metadata.Children[0].Children[1].Payload.Span)}));
                    foreach(var kind in new[]{TreLookupKind.Missing,TreLookupKind.Masked})HostAccountingScenarios.Reject<InvalidDataException>(()=>CreatureAppearanceDescriptorBundleReader.Load(p=>p==program.Key?new(kind):overlay.Lookup(p),args[5],args[7],args[9],loadSkeletons:true,loadSkinSources:true,loadGeometry:true,loadFaceBlueprints:true,loadFaceResources:true,loadFacePixelPrograms:true));
                }
            }
            foreach(var pair in bundle.FaceBlueprints!)foreach(int requested in customized?new[]{0}:new[]{0,int.MinValue,int.MaxValue})
            {
                var values=customized?bundle.GeneratedFaces!.Values:pair.Value.PrepareLists.SelectMany(list=>list).Where(op=>op.VariableName is not null).Select(op=>new BlueprintCustomizationKey(op.VariableName!,op.Kind=="SST1" || op.RawFactoryFlag==1)).Distinct().ToDictionary(key=>key,_=>requested);
                var preparation=BlueprintFacePreparer.Prepare(pair.Value,resources,values);
                Console.WriteLine("face_preparation="+JsonSerializer.Serialize(new{path=pair.Key,requested,values=values.Select(p=>new{p.Key.VariableName,p.Key.FactoryFlag,p.Value}),preparation.ShaderSlots,preparation.Operations}));
                if(faceCompose)
                {
                    var composed=BlueprintFaceCompositor.Compose(pair.Value,resources,preparation);
                    Console.WriteLine("face_composition="+JsonSerializer.Serialize(new{path=pair.Key,requested,policy=BlueprintComposedFace.Policy,composed.Image.Width,composed.Image.Height,composed.Layers,sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(composed.Image.Mips[0].Rgba.Span))}));
                    if(composed.Layers.Count!=5||composed.Layers[0].WrittenPixels!=65536)throw new Exception("Original face base coverage differs.");
                }
                if(generatedFaces&&requested==0)
                {
                    var generated=BlueprintGeneratedFaceBuilder.Create(bundle.Geometries!,bundle.FaceBlueprints!,resources,values);
                    foreach(var binding in generated.Bindings)
                        Console.WriteLine("generated_face_binding="+JsonSerializer.Serialize(new{binding.Key.MeshPath,binding.Key.ShaderIndex,binding.Key.TextureTag,binding.Value.RendererPath,binding.Value.ShaderPath,sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(generated.Outputs[binding.Value.RendererPath].Image.Mips[0].Rgba.Span))}));
                    if(generated.Outputs.Count!=1||generated.Bindings.Count!=1)throw new Exception("Original generated face binding count differs.");
                }
                if(preparation.ShaderSlots.Count!=5 || preparation.Operations.Count!=9)throw new Exception("Original preparation slot/operation count differs.");
                foreach(var key in values.Keys.ToArray()) {var omitted=values.Where(p=>p.Key!=key).ToDictionary(p=>p.Key,p=>p.Value);HostAccountingScenarios.Reject<InvalidDataException>(()=>BlueprintFacePreparer.Prepare(pair.Value,resources,omitted));}
            }
            Console.WriteLine($"PASS: original object-root face shaders/textures/palettes, single-read closure and missing/masked families; generated_face={faceCompose} gpu=false");
        }
        if(jointBinding)SkeletalJointBindingScenarios.Probe(bundle);
        if(shaderDeformation)SkeletalShaderDeformationScenarios.Probe(bundle);
        if(logicalTables)
        {
            LogicalAnimationTableScenarios.Probe(bundle);string first=bundle.LogicalTables!.Keys.First();
            HostAccountingScenarios.Reject<InvalidDataException>(()=>CreatureAppearanceDescriptorBundleReader.Load(p=>p==first?new(TreLookupKind.Masked,Priority:54):overlay.Lookup(p),args[5],args[7],args[9],loadSkeletons:true,loadLogicalTables:true));
        }
        if(selectedAnimation)
        {
            LogicalAnimationProxyScenarios.Probe(bundle);
            if(compressedClip)CompressedKeyframeScenarios.Probe(bundle);
            if(sampleClip)CompressedSamplingScenarios.Probe(bundle);
            if(animationPose)SkeletonAnimationPoseScenarios.Probe(bundle);
            HostAccountingScenarios.Reject<InvalidDataException>(()=>CreatureAppearanceDescriptorBundleReader.Load(overlay.Lookup,args[5],args[7],args[9],loadSkeletons:true,loadLogicalTables:true,animationSelection:LogicalAnimationProxyScenarios.IdleSelection with{ClipSha256=new string('0',64)}));
            string clip=bundle.SelectedAnimation!.Proxy.ClipPath;
            HostAccountingScenarios.Reject<InvalidDataException>(()=>CreatureAppearanceDescriptorBundleReader.Load(p=>p==clip?new(TreLookupKind.Masked,Priority:54):overlay.Lookup(p),args[5],args[7],args[9],loadSkeletons:true,loadLogicalTables:true,animationSelection:LogicalAnimationProxyScenarios.IdleSelection));
        }
        HostAccountingScenarios.Reject<InvalidDataException>(()=>CreatureAppearanceDescriptorBundleReader.Load(overlay.Lookup,args[5],args[7],new string('0',64)));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>CreatureAppearanceDescriptorBundleReader.Load(p=>p==bundle.Appearance.MeshGenerators[0]?new(TreLookupKind.Masked,Priority:54):overlay.Lookup(p),args[5],args[7],args[9]));
        HostAccountingScenarios.Reject<AppearanceGraphBudgetException>(()=>CreatureAppearanceDescriptorBundleReader.Load(overlay.Lookup,args[5],args[7],args[9],new(MaximumResources:7)));
        Console.WriteLine($"PASS original object-root skeletal descriptors sources={bundle.Sources.Count}");
    }
    internal static void DumpBlendShapes(CreatureAppearanceDescriptorBundle bundle)
    {
        if(bundle.ShapeSelection is not null)foreach(var mesh in bundle.ShapeSelection.Geometries)
            Console.WriteLine("skeletal_blend_application="+JsonSerializer.Serialize(new{path=mesh.Key,selected=-1,values=bundle.ShapeSelection.Values,positions=mesh.Value.Skin.Positions.Select(v=>new[]{v.X,v.Y,v.Z}),normals=mesh.Value.Skin.Normals.Select(v=>new[]{v.X,v.Y,v.Z}),tangents=mesh.Value.Tangents.Select(v=>new[]{v.X,v.Y,v.Z,v.W})}));
        foreach(var mesh in bundle.BlendShapes!)for(int index=0;index<mesh.Value.Targets.Count;index++)
        {
            var target=mesh.Value.Targets[index];Console.WriteLine("skeletal_blend_target="+JsonSerializer.Serialize(new{path=mesh.Key,index,target.VariableName,@private=false,positions=target.Positions.Count,normals=target.Normals.Count,tangents=target.Tangents.Count,hardpoints=target.HardpointMetadata is not null,sha256=SkeletalBlendShapeScenarios.CanonicalHash(target)}));
        }
        foreach(var mesh in bundle.BlendShapes!)foreach(int selected in new[]{0,2,3,127,255})
        {
            var original=bundle.Geometries![mesh.Key];var values=mesh.Value.Targets.Select(t=>t.VariableName).Distinct(StringComparer.Ordinal).ToDictionary(k=>k,_=>selected,StringComparer.Ordinal);
            var shaped=SkeletalBlendShapeEvaluator.Apply(original,mesh.Value,values);
            Console.WriteLine("skeletal_blend_application="+JsonSerializer.Serialize(new{path=mesh.Key,selected,positions=shaped.Skin.Positions.Select(v=>new[]{v.X,v.Y,v.Z}),normals=shaped.Skin.Normals.Select(v=>new[]{v.X,v.Y,v.Z}),tangents=shaped.Tangents.Select(v=>new[]{v.X,v.Y,v.Z,v.W})}));
            if(!ReferenceEquals(shaped.Shaders,original.Shaders)||!ReferenceEquals(shaped.Skin.Weights,original.Skin.Weights))throw new Exception("Shape changed topology/weights.");
        }
    }
    internal static void DumpGeneratedAppearance(CreatureAppearanceDescriptorBundle bundle)
    {
        var albedo=bundle.MaterialBundle!.AlbedoFactors!;
        foreach(var pair in albedo.Materials)Console.WriteLine("skeletal_albedo="+JsonSerializer.Serialize(new{path=pair.Key,factors=pair.Value}));
        foreach(var pair in albedo.Programs){Console.WriteLine("skeletal_albedo_program="+JsonSerializer.Serialize(new{path=pair.Key,pair.Value.UsesAlphaHueMask,bytecodeSha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(pair.Value.Metadata.Children[0].Children[1].Payload.Span))}));}
        var resources=bundle.FaceResources!;
        Console.WriteLine("face_resources="+JsonSerializer.Serialize(new{shaders=resources.Shaders.Keys,effects=resources.EffectsByShader.Select(p=>new{path=p.Key,candidates=p.Value.Implementations.Count}),textures=resources.Textures.Select(p=>new{path=p.Key,format=p.Value.Format.ToString(),terminalBlock=Convert.ToHexString(p.Value.RetainedTerminalBlock.Span),mips=p.Value.Mips.Select(m=>new{m.Width,m.Height,sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(m.Rgba.Span))})}),palettes=resources.Palettes.Select(p=>new{path=p.Key,sha256=Stardust.Core.Proof.PaletteVerification.ComputeSha256(p.Value),entries=p.Value.Entries.Count})}));
        foreach(var program in resources.PixelPrograms!){Console.WriteLine("face_pixel_program="+JsonSerializer.Serialize(new{path=program.Key,program.Value.TextureStage,program.Value.FactorRegister,bytecode=Convert.ToHexString(program.Value.Metadata.Children[0].Children[1].Payload.Span)}));}
        var generated=bundle.GeneratedFaces!;
        foreach(var pair in bundle.FaceBlueprints!)
        {
            var d=pair.Value;Console.WriteLine("face_blueprint="+JsonSerializer.Serialize(new{path=pair.Key,d.TemplateValue,d.Width,d.Height,d.DestinationFormatIds,d.ShaderPaths,d.TexturePaths,d.CameraScalar,d.RenderSets,d.PrepareLists,vertices=d.VertexArrays.Select(v=>new{declaration=System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(v.Children[0].Children[0].Payload.Span),count=System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(v.Children[0].Children[0].Payload.Span[4..]),sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(v.Children[0].Children[1].Payload.Span))})}));
            var values=generated.Values;int requested=0;
            var preparation=BlueprintFacePreparer.Prepare(d,resources,values);Console.WriteLine("face_preparation="+JsonSerializer.Serialize(new{path=pair.Key,requested,values=values.Select(p=>new{p.Key.VariableName,p.Key.FactoryFlag,p.Value}),preparation.ShaderSlots,preparation.Operations}));
            var composed=generated.Outputs[pair.Key];Console.WriteLine("face_composition="+JsonSerializer.Serialize(new{path=pair.Key,requested,policy=BlueprintComposedFace.Policy,composed.Image.Width,composed.Image.Height,composed.Layers,sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(composed.Image.Mips[0].Rgba.Span))}));
        }
        foreach(var binding in generated.Bindings){Console.WriteLine("generated_face_binding="+JsonSerializer.Serialize(new{binding.Key.MeshPath,binding.Key.ShaderIndex,binding.Key.TextureTag,binding.Value.RendererPath,binding.Value.ShaderPath,sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(generated.Outputs[binding.Value.RendererPath].Image.Mips[0].Rgba.Span))}));}
    }
    internal static void DumpMaterials(SkeletalMaterialBundle content)
    {
        if(content.PaletteDefaults is not null)foreach(var pair in content.PaletteDefaults)Console.WriteLine("skeletal_palette_defaults="+JsonSerializer.Serialize(new{path=pair.Key,factors=pair.Value.AuthoredFactors,missing=pair.Value.MissingPalettePaths,diagnostics=pair.Value.Diagnostics}));
            foreach(var pair in content.Materials)Console.WriteLine("skeletal_material="+JsonSerializer.Serialize(new{path=pair.Key,effect=pair.Value.References.EffectPath,textures=pair.Value.References.Textures.Select(t=>new{t.Tag,t.SourceFlag,t.AssetPath}),coordinates=pair.Value.Parameters.CoordinateSets,customization=pair.Value.Customization is null?null:new{pair.Value.Customization.TextureChoices,pair.Value.Customization.TextureSelections,pair.Value.Customization.PaletteFactors}}));
            foreach(var pair in content.Textures)Console.WriteLine("skeletal_texture="+JsonSerializer.Serialize(new{path=pair.Key,pair.Value.Width,pair.Value.Height,format=pair.Value.Format.ToString(),mips=pair.Value.Mips.Select(m=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(m.Rgba.Span))).ToArray()}));
    }

}
