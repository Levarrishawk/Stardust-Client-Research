using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using System.Security.Cryptography;
using Stardust.Core.Assets;

internal static class ComponentAppearanceScenarios
{
    internal static void Run()
    {
        // Independently chosen asymmetric transforms distinguish multiplication order and translation slots.
        AppearanceTransform parent=new(new(0,-1,0),new(1,0,0),new(0,0,1),new(10,20,30));
        AppearanceTransform local=new(new(2,0,0),new(0,3,0),new(0,0,4),new(1,2,3));
        Vector3 point=new(5,6,7);
        if(parent.Compose(local).TransformPoint(point)!=new Vector3(-10,31,61) ||
            parent.TransformPoint(local.TransformPoint(point))!=new Vector3(-10,31,61) ||
            AppearanceTransform.Identity.Compose(local)!=local || local.Compose(AppearanceTransform.Identity)!=local)
            throw new Exception("Component transform order mismatch.");
        foreach(string version in new[]{"0003","0004","0005"})
        {
            byte[] source=Fixture(version);
            ComponentAppearanceDescription value=ComponentAppearanceDecoder.Decode(source);
            if(value.Parts.Count!=1 || value.Parts[0].ChildPath!="appearance/mesh/synthetic.msh" ||
                value.Parts[0].LocalTransform.TransformPoint(new(1,2,3))!=new Vector3(18,46,74) ||
                value.RadarFlags!=(version=="0005"?(uint?)0x12345678:null))throw new Exception("Component structure mismatch.");
            for(int n=0;n<source.Length;n++)Reject(()=>ComponentAppearanceDecoder.Decode(source.AsSpan(0,n)));
            Array.Clear(source);
            if(value.Metadata.FormType!="CMPA" || value.Parts[0].LocalTransform.Translation.Z!=12)throw new Exception("Component data borrowed source.");
        }
        Reject(()=>ComponentAppearanceDecoder.Decode(Fixture("0002")));
        Reject(()=>ComponentAppearanceDecoder.Decode(Fixture("0005",badWidth:true)));
        Reject(()=>ComponentAppearanceDecoder.Decode(Fixture("0005",nonfinite:true)));
        GraphTests();
        Console.WriteLine("PASS: owned CMPA 0003-0005 parts, raw RADR, 3x4 transforms, parent/local order and malformed fixtures");
    }
    private static void GraphTests()
    {
        byte[] Part(string path,float translation)
        {
            byte[] transform=new byte[48];float[] m={1,0,0,translation,0,1,0,2,0,0,1,3};
            for(int i=0;i<12;i++)BinaryPrimitives.WriteInt32LittleEndian(transform.AsSpan(i*4),BitConverter.SingleToInt32Bits(m[i]));
            return MeshScenarios.Chunk("PART",Encoding.ASCII.GetBytes(path+"\0").Concat(transform).ToArray());
        }
        byte[] Component(params byte[][] parts)=>MeshScenarios.Form("CMPA",MeshScenarios.Form("0003",
            new[]{MeshScenarios.Form("APPR",MeshScenarios.Form("0003"))}.Concat(parts).ToArray()));
        Dictionary<string,byte[]> inputs=new()
        {
            ["appearance/root.cmp"]=Component(Part("child.cmp",10),Part("mesh/synthetic.msh",20),Part("missing.prt",30)),
            ["appearance/child.cmp"]=Component(Part("mesh/synthetic.msh",4)),
            ["appearance/mesh/synthetic.msh"]=MeshScenarios.Mesh("0001")
        };
        Dictionary<string,int> reads=new();
        TreLookupResult Resolve(string path)
        {reads[path]=reads.GetValueOrDefault(path)+1;return inputs.TryGetValue(path,out byte[]? bytes)?new(TreLookupKind.Present,bytes,1):new(TreLookupKind.Missing);}
        string identity=Convert.ToHexString(SHA256.HashData(inputs["appearance/root.cmp"]));
        AppearanceGeometryGraph graph=AppearanceGeometryGraphReader.Load(Resolve,"appearance/root.cmp",identity);
        if(graph.Root.Children.Count!=2 || graph.Root.Diagnostics.Count!=1 || graph.Sources.Count!=3 || reads.Values.Any(n=>n!=1) ||
            !ReferenceEquals(graph.Root.Children[0].Template.Children[0].Template,graph.Root.Children[1].Template) ||
            graph.Root.Children[0].LocalTransform.Compose(graph.Root.Children[0].Template.Children[0].LocalTransform).Translation!=new Vector3(14,4,6))
            throw new Exception("Shared component geometry or independent placement mismatch.");
        Array.Clear(inputs["appearance/mesh/synthetic.msh"]);
        if(graph.Root.Children[1].Template.Mesh?.VertexCount!=3)throw new Exception("Graph borrowed geometry.");
        inputs["appearance/mesh/synthetic.msh"]=MeshScenarios.Mesh("0001");
        inputs["appearance/child.cmp"]=Component(Part("root.cmp",4));
        AppearanceGeometryGraph cycle=AppearanceGeometryGraphReader.Load(Resolve,"appearance/root.cmp",identity);
        if(cycle.Root.Children[0].Template.Diagnostics.Count!=1)throw new Exception("Component cycle not diagnosed locally.");
        HostAccountingScenarios.Reject<AppearanceGraphBudgetException>(()=>AppearanceGeometryGraphReader.Load(Resolve,"appearance/root.cmp",identity,new(MaximumResources:1)));
        HostAccountingScenarios.Reject<AppearanceGraphBudgetException>(()=>AppearanceGeometryGraphReader.Load(Resolve,"appearance/root.cmp",identity,new(MaximumSourceBytes:1)));
        HostAccountingScenarios.Reject<AppearanceGraphBudgetException>(()=>AppearanceGeometryGraphReader.Load(Resolve,"appearance/root.cmp",identity,new(MaximumEdges:1)));
        HostAccountingScenarios.Reject<AppearanceGraphBudgetException>(()=>AppearanceGeometryGraphReader.Load(Resolve,"appearance/root.cmp",identity,new(MaximumDepth:1)));
        Reject(()=>AppearanceGeometryGraphReader.Load(Resolve,"appearance/root.cmp",new string('0',64)));
        inputs["appearance/textured.cmp"]=Component(Part("mesh/synthetic.msh",0),Part("mesh/second.msh",10));
        inputs["appearance/mesh/second.msh"]=MeshScenarios.Mesh("0001");
        inputs["shader/synthetic.sht"]=MaterialScenarios.Shader();inputs["effect/synthetic.eft"]=MaterialScenarios.Effect();
        inputs["texture/synthetic.dds"]=DdsScenarios.Fixture("DXT1",4,4,1,new byte[8]);
        string texturedIdentity=Convert.ToHexString(SHA256.HashData(inputs["appearance/textured.cmp"]));reads.Clear();
        TexturedAppearanceGraph textured=TexturedAppearanceGraphReader.Load(Resolve,"appearance/textured.cmp",texturedIdentity);
        TexturedStaticMesh first=textured.Meshes["appearance/mesh/synthetic.msh"],second=textured.Meshes["appearance/mesh/second.msh"];
        if(textured.Sources.Count!=6 || textured.Diagnostics.Count!=0 || reads.Values.Any(n=>n!=1) ||
            !ReferenceEquals(first.Materials["shader/synthetic.sht"],second.Materials["shader/synthetic.sht"]) ||
            !ReferenceEquals(first.Effects["effect/synthetic.eft"],second.Effects["effect/synthetic.eft"]) ||
            !ReferenceEquals(first.Textures["texture/synthetic.dds"],second.Textures["texture/synthetic.dds"]))
            throw new Exception("Component material resources were not shared by immutable identity.");
        HostAccountingScenarios.Reject<AppearanceGraphBudgetException>(()=>TexturedAppearanceGraphReader.Load(Resolve,"appearance/textured.cmp",texturedIdentity,maximumDecodedBytes:4));
        inputs["texture/synthetic.dds"]=DdsHdrScenarios.Fixture(96,4,4,1,new byte[16]);reads.Clear();
        textured=TexturedAppearanceGraphReader.Load(Resolve,"appearance/textured.cmp",texturedIdentity,maximumDecodedBytes:96);
        first=textured.Meshes["appearance/mesh/synthetic.msh"];second=textured.Meshes["appearance/mesh/second.msh"];
        if(first.Textures.Count!=0 || first.HdrTextures.Count!=1 || first.HdrTextures.Values.Single().Mips[0].RgbHalf.Length!=96 ||
            !ReferenceEquals(first.HdrTextures.Values.Single(),second.HdrTextures.Values.Single()) || reads.Values.Any(n=>n!=1))throw new Exception("Shared HDR material pixels/lookup/budget mismatch.");
        HostAccountingScenarios.Reject<AppearanceGraphBudgetException>(()=>TexturedAppearanceGraphReader.Load(Resolve,"appearance/textured.cmp",texturedIdentity,maximumDecodedBytes:95));
        byte[] Replace(byte[] bytes,string from,string to)
        {
            byte[] source=Encoding.ASCII.GetBytes(from),target=Encoding.ASCII.GetBytes(to),result=(byte[])bytes.Clone();
            if(source.Length!=target.Length)throw new Exception("Synthetic name replacement width mismatch.");
            int at=result.AsSpan().IndexOf(source);if(at<0)throw new Exception("Synthetic name missing.");target.CopyTo(result,at);return result;
        }
        inputs["appearance/mesh/second.msh"]=Replace(inputs["appearance/mesh/second.msh"],"shader/synthetic.sht","shader/secondary.sht");
        inputs["shader/secondary.sht"]=Replace(MaterialScenarios.Shader(),"texture/synthetic.dds","texture/secondary.dds");
        inputs["texture/secondary.dds"]=DdsScenarios.Fixture("DXT1",4,4,1,new byte[8]);
        textured=TexturedAppearanceGraphReader.Load(Resolve,"appearance/textured.cmp",texturedIdentity,maximumDecodedBytes:160);
        if(textured.Meshes["appearance/mesh/second.msh"].Textures.Count!=1 || textured.Meshes["appearance/mesh/synthetic.msh"].HdrTextures.Count!=1)throw new Exception("Mixed RGBA/HDR graph distinction mismatch.");
        HostAccountingScenarios.Reject<AppearanceGraphBudgetException>(()=>TexturedAppearanceGraphReader.Load(Resolve,"appearance/textured.cmp",texturedIdentity,maximumDecodedBytes:159));
        inputs.Remove("effect/synthetic.eft");
        TexturedAppearanceGraph partial=TexturedAppearanceGraphReader.Load(Resolve,"appearance/textured.cmp",texturedIdentity);
        if(partial.Meshes.Count!=0 || partial.Diagnostics.Count!=2 || partial.Geometry.Root.Children.Count!=2)
            throw new Exception("Failed material graph removed supported geometry.");
        Console.WriteLine("PASS: shared component material/effect/pixels, single lookup, explicit neutral fallback and fatal decoded budget");
        Console.WriteLine("PASS: bounded shared geometry templates, independent nested transforms, missing parts, cycle diagnosis and fatal aggregate budgets");
    }
    internal static byte[] Fixture(string version,bool badWidth=false,bool nonfinite=false)
    {
        byte[] matrix=new byte[badWidth?47:48];
        if(!badWidth)for(int i=0;i<12;i++)BinaryPrimitives.WriteInt32LittleEndian(matrix.AsSpan(i*4),BitConverter.SingleToInt32Bits(nonfinite&&i==7?float.NaN:i+1));
        List<byte[]> children=new(){MeshScenarios.Form("APPR",MeshScenarios.Form("0003"))};
        if(version=="0005")children.Add(MeshScenarios.Form("RADR",MeshScenarios.Chunk("INFO",new byte[]{0x78,0x56,0x34,0x12})));
        children.Add(MeshScenarios.Chunk("PART",Encoding.ASCII.GetBytes("mesh/synthetic.msh\0").Concat(matrix).ToArray()));
        return MeshScenarios.Form("CMPA",MeshScenarios.Form(version,children.ToArray()));
    }
    internal static void ProbeOptional(string[] args)
    {
        if(args.Length!=8 || args[2]!="--configuration" || args[4]!="--member" || args[6]!="--expected-sha256")
            throw new ArgumentException("Expected explicit component root, configuration, member and identity.");
        using StreamReader configuration=File.OpenText(args[3]);
        using ConfiguredTreOverlay overlay=ConfiguredTreOverlay.Open(args[1],configuration);
        if(args[0]=="--textured-graph")
        {
            TexturedAppearanceGraph graph=TexturedAppearanceGraphReader.Load(overlay.Lookup,args[5],args[7]);
            foreach((string path,AssetSourceIdentity source) in graph.Sources)
                Console.WriteLine($"material_source={path} bytes={source.Bytes} sha256={source.Sha256}");
            foreach(string diagnostic in graph.Diagnostics)Console.WriteLine($"material_diagnostic={diagnostic}");
            foreach((string path,TexturedStaticMesh mesh) in graph.Meshes)
                Console.WriteLine($"material_mesh={path} batches={mesh.Materials.Count} textures={mesh.TextureCount} effects={mesh.Effects.Count}");
            Dictionary<string,DdsImage> textures=new(StringComparer.Ordinal);
            foreach(TexturedStaticMesh mesh in graph.Meshes.Values)foreach((string path,DdsImage image) in mesh.Textures)textures.TryAdd(path,image);
            foreach((string path,DdsImage image) in textures)
                for(int i=0;i<image.Mips.Count;i++)Console.WriteLine($"graph_texture={path} mip={i} sha256={Convert.ToHexString(SHA256.HashData(image.Mips[i].Rgba.Span))}");
            Dictionary<string,DdsHdrImage> hdrTextures=new(StringComparer.Ordinal);
            foreach(var mesh in graph.Meshes.Values)foreach(var texture in mesh.HdrTextures)hdrTextures.TryAdd(texture.Key,texture.Value);
            foreach(var texture in hdrTextures)for(int level=0;level<texture.Value.Mips.Count;level++)Console.WriteLine($"graph_hdr_texture={texture.Key} mip={level} sha256={Convert.ToHexString(SHA256.HashData(texture.Value.Mips[level].RgbHalf.Span))}");
            Console.WriteLine($"PASS textured_graph sources={graph.Sources.Count} material_meshes={graph.Meshes.Count} textures={textures.Count+hdrTextures.Count} mips={textures.Values.Sum(image=>image.Mips.Count)+hdrTextures.Values.Sum(image=>image.Mips.Count)} diagnostics={graph.Diagnostics.Count}");return;
        }
        if(args[0]=="--geometry-graph")
        {
            AppearanceGeometryGraph graph=AppearanceGeometryGraphReader.Load(overlay.Lookup,args[5],args[7]);
            foreach((string path,AssetSourceIdentity source) in graph.Sources)
                Console.WriteLine($"graph_source={path} bytes={source.Bytes} sha256={source.Sha256}");
            int meshInstances=0,diagnostics=0;
            void Walk(AppearanceGeometryTemplate template,AppearanceTransform world,int depth)
            {
                diagnostics+=template.Diagnostics.Count;
                if(template.Mesh is not null)
                {
                    meshInstances++;
                    Console.WriteLine($"graph_mesh={template.Path} depth={depth} vertices={template.Mesh.VertexCount} triangles={template.Mesh.TriangleCount} geometry_sha256={Stardust.Core.Proof.StaticMeshVerification.ComputeGeometrySha256(template.Mesh)}");
                    Console.WriteLine($"graph_sorted={template.Path} variants={template.Mesh.Primitives.Sum(p=>p.SortedIndices?.Count??0)} sha256={Stardust.Core.Proof.StaticMeshVerification.ComputeSortedIndicesSha256(template.Mesh)}");
                }
                foreach(string diagnostic in template.Diagnostics)Console.WriteLine($"graph_diagnostic={template.Path}:{diagnostic}");
                foreach(AppearanceGeometryChild child in template.Children)Walk(child.Template,world.Compose(child.LocalTransform),depth+1);
            }
            Walk(graph.Root,AppearanceTransform.Identity,0);
            Console.WriteLine($"PASS geometry_graph sources={graph.Sources.Count} mesh_instances={meshInstances} diagnostics={diagnostics}");return;
        }
        TreLookupResult selected=overlay.Lookup(args[5]);
        if(selected.Kind!=TreLookupKind.Present || selected.Payload is null)throw new InvalidDataException("Missing component input.");
        string hash=Convert.ToHexString(SHA256.HashData(selected.Payload));
        if(!hash.Equals(args[7],StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Component input identity mismatch.");
        ComponentAppearanceDescription value=ComponentAppearanceDecoder.Decode(selected.Payload);
        Console.WriteLine($"component={args[5]} priority={selected.Priority} bytes={selected.Payload.Length} sha256={hash} version={value.Version} parts={value.Parts.Count} radar={value.RadarFlags}");
        for(int i=0;i<value.Parts.Count;i++)
        {
            ComponentAppearancePart part=value.Parts[i];AppearanceTransform t=part.LocalTransform;
            float[] m={t.RowX.X,t.RowX.Y,t.RowX.Z,t.Translation.X,t.RowY.X,t.RowY.Y,t.RowY.Z,t.Translation.Y,t.RowZ.X,t.RowZ.Y,t.RowZ.Z,t.Translation.Z};
            string bits=string.Concat(m.Select(f=>BitConverter.SingleToUInt32Bits(f).ToString("X8")));
            Console.WriteLine($"component_part={i} child={part.ChildPath} transform_bits={bits}");
        }
    }
    private static void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
}
