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
    internal static void ProbeOptional(string[] args,bool loadSkeletons=false,bool bindPose=false,bool loadSkinSources=false,bool loadGeometry=false,bool jointBinding=false,bool shaderDeformation=false,bool logicalTables=false,bool selectedAnimation=false,bool compressedClip=false)
    {
        if(args.Length!=10||args[2]!="--configuration"||args[4]!="--member"||args[6]!="--expected-sha256"||args[8]!="--appearance-sha256")throw new ArgumentException("Pinned object-root and SAT descriptor inputs required.");
        using var config=File.OpenText(args[3]);using var overlay=ConfiguredTreOverlay.Open(args[1],config);
        Dictionary<string,int> counts=new(StringComparer.Ordinal);
        TreLookupResult Read(string p){counts[p]=counts.GetValueOrDefault(p)+1;return overlay.Lookup(p);}
        var bundle=CreatureAppearanceDescriptorBundleReader.Load(Read,args[5],args[7],args[9],loadSkeletons:loadSkeletons,loadSkinSources:loadSkinSources,loadGeometry:loadGeometry,loadLogicalTables:logicalTables,animationSelection:selectedAnimation?LogicalAnimationProxyScenarios.IdleSelection:null);
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
            HostAccountingScenarios.Reject<InvalidDataException>(()=>CreatureAppearanceDescriptorBundleReader.Load(overlay.Lookup,args[5],args[7],args[9],loadSkeletons:true,loadLogicalTables:true,animationSelection:LogicalAnimationProxyScenarios.IdleSelection with{ClipSha256=new string('0',64)}));
            string clip=bundle.SelectedAnimation!.Proxy.ClipPath;
            HostAccountingScenarios.Reject<InvalidDataException>(()=>CreatureAppearanceDescriptorBundleReader.Load(p=>p==clip?new(TreLookupKind.Masked,Priority:54):overlay.Lookup(p),args[5],args[7],args[9],loadSkeletons:true,loadLogicalTables:true,animationSelection:LogicalAnimationProxyScenarios.IdleSelection));
        }
        HostAccountingScenarios.Reject<InvalidDataException>(()=>CreatureAppearanceDescriptorBundleReader.Load(overlay.Lookup,args[5],args[7],new string('0',64)));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>CreatureAppearanceDescriptorBundleReader.Load(p=>p==bundle.Appearance.MeshGenerators[0]?new(TreLookupKind.Masked,Priority:54):overlay.Lookup(p),args[5],args[7],args[9]));
        HostAccountingScenarios.Reject<AppearanceGraphBudgetException>(()=>CreatureAppearanceDescriptorBundleReader.Load(overlay.Lookup,args[5],args[7],args[9],new(MaximumResources:7)));
        Console.WriteLine($"PASS original object-root skeletal descriptors sources={bundle.Sources.Count}");
    }
}
