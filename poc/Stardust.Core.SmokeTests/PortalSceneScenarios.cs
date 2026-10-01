using System.Security.Cryptography;
using System.Text;
using Stardust.Core.Assets;
internal static class PortalSceneScenarios
{
    internal static void Run()
    {
        Dictionary<string,byte[]> inputs=new()
        {
            ["appearance/test.pob"]=PortalLayoutScenarios.Fixture(),
            ["appearance/test.apt"]=MeshScenarios.Form("APT ",MeshScenarios.Form("0000",MeshScenarios.Chunk("NAME",Encoding.ASCII.GetBytes("appearance/mesh/test.msh\0")))),
            ["appearance/mesh/test.msh"]=MeshScenarios.Mesh("0001")
        };
        Dictionary<string,int> reads=new();
        TreLookupResult Resolve(string path){reads[path]=reads.GetValueOrDefault(path)+1;return inputs.TryGetValue(path,out byte[]? data)?new(TreLookupKind.Present,data,1):new(TreLookupKind.Missing);}
        string pin=Convert.ToHexString(SHA256.HashData(inputs["appearance/test.pob"]));
        PortalSceneBundle scene=PortalSceneBundleReader.Load(Resolve,"appearance/test.pob",pin);
        if(scene.CellAppearances.Count!=2 || scene.Sources.Count!=3 || scene.Diagnostics.Count!=0 || reads.Values.Any(value=>value!=1) ||
            !ReferenceEquals(scene.CellAppearances[0],scene.CellAppearances[1]) || scene.Geometry.Root.Children[1].AuthoredIndex!=1)throw new Exception("Cell scene sharing/identity mismatch.");
        Array.Clear(inputs["appearance/mesh/test.msh"]);
        if(scene.CellAppearances[1].Mesh?.VertexCount!=3)throw new Exception("Cell scene borrowed input.");
        inputs["appearance/mesh/test.msh"]=MeshScenarios.Mesh("0001");
        var selected=new[]{1};
        TreLookupResult MutateSelection(string path){selected[0]=0;return Resolve(path);}
        var narrow=PortalSceneBundleReader.Load(MutateSelection,"appearance/test.pob",pin,selectedAppearanceCells:selected);
        if(narrow.CellAppearances.Count!=1||!narrow.CellAppearances.ContainsKey(1)||narrow.Geometry.Root.Children.Single().AuthoredIndex!=1)throw new Exception("Selected appearance set borrowed or renumbered.");
        narrow=PortalSceneBundleReader.Load(Resolve,"appearance/test.pob",pin,selectedAppearanceCells:Array.Empty<int>());
        if(narrow.CellAppearances.Count!=0||narrow.Sources.Count!=1)throw new Exception("Empty appearance selection loaded unrelated resources.");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>PortalSceneBundleReader.Load(Resolve,"appearance/test.pob",pin,selectedAppearanceCells:new[]{1,1}));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>PortalSceneBundleReader.Load(Resolve,"appearance/test.pob",pin,selectedAppearanceCells:new[]{2}));
        HostAccountingScenarios.Reject<AppearanceGraphBudgetException>(()=>PortalSceneBundleReader.Load(Resolve,"appearance/test.pob",pin,new(MaximumResources:2)));
        HostAccountingScenarios.Reject<AppearanceGraphBudgetException>(()=>PortalSceneBundleReader.Load(Resolve,"appearance/test.pob",pin,new(MaximumSourceBytes:inputs["appearance/test.pob"].Length)));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>PortalSceneBundleReader.Load(Resolve,"appearance/test.pob",new string('0',64)));
        string meshPin=Convert.ToHexString(SHA256.HashData(inputs["appearance/mesh/test.msh"]));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>AppearanceGeometryGraphReader.LoadMany(Resolve,new AppearanceGeometryRequest[]{new("appearance/mesh/test.msh",meshPin),new("appearance/mesh/test.msh",new string('0',64))}));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>AppearanceGeometryGraphReader.LoadMany(Resolve,new AppearanceGeometryRequest[]{new("appearance/mesh/test.msh",meshPin),new("appearance/mesh/test.msh",new string('0',64))},allowUnavailableRoots:true));
        AppearanceGeometryCollection roots=AppearanceGeometryGraphReader.LoadMany(Resolve,new AppearanceGeometryRequest[]{new("appearance/mesh/test.msh",meshPin),new("appearance/missing.apt")},allowUnavailableRoots:true);
        if(roots.Roots[0].Template is null || roots.Roots[1].Template is not null || roots.Diagnostics.Count!=1)throw new Exception("Unavailable root isolation mismatch.");
        inputs["appearance/collision/test.flr"]=FloorMeshScenarios.Fixture();
        scene=PortalSceneBundleReader.Load(Resolve,"appearance/test.pob",pin,loadFloors:true);
        if(scene.CellFloors.Count!=1 || scene.CellFloors[1].Triangles.Count!=1 || scene.Sources.Count!=4)throw new Exception("Cell floor load/source accounting mismatch.");
        HostAccountingScenarios.Reject<AppearanceGraphBudgetException>(()=>PortalSceneBundleReader.Load(Resolve,"appearance/test.pob",pin,new(MaximumResources:3),true));
        inputs["appearance/test.pob"]=PortalLayoutScenarios.Fixture(actualGraph:true);pin=Convert.ToHexString(SHA256.HashData(inputs["appearance/test.pob"]));
        inputs["appearance/collision/test.flr"]=FloorMeshScenarios.Fixture(pathGraph:true);
        scene=PortalSceneBundleReader.Load(Resolve,"appearance/test.pob",pin,loadPathGraphs:true);
        if(scene.LayoutPathGraph?.Nodes.Count!=2 || scene.CellPathGraphs[1].Edges.Count!=1 || scene.CellFloors.Count!=1 || scene.Sources.Count!=4)throw new Exception("Cell/layout path graph bundle mismatch.");
        inputs.Remove("appearance/collision/test.flr");scene=PortalSceneBundleReader.Load(Resolve,"appearance/test.pob",pin,loadFloors:true);
        if(scene.CellFloors.Count!=0 || scene.Diagnostics.Count!=1)throw new Exception("Missing cell floor not diagnosed.");
        inputs["shader/synthetic.sht"]=MaterialScenarios.Shader();inputs["effect/synthetic.eft"]=MaterialScenarios.Effect();inputs["texture/synthetic.dds"]=DdsScenarios.Fixture("DXT1",4,4,1,new byte[8]);reads.Clear();
        TexturedPortalScene textured=TexturedPortalSceneReader.Load(Resolve,"appearance/test.pob",pin);
        if(textured.Sources.Count!=6 || textured.Materials.Meshes.Count!=1 || textured.Diagnostics.Count!=0 || reads.Values.Any(count=>count!=1))throw new Exception("Shared textured cell scene mismatch.");
        reads.Clear();textured=TexturedPortalSceneReader.Load(Resolve,"appearance/test.pob",pin,selectedAppearanceCells:new[]{1});
        if(textured.Scene.CellAppearances.Count!=1||!textured.Scene.CellAppearances.ContainsKey(1)||textured.Materials.Meshes.Count!=1||reads.Values.Any(count=>count!=1))throw new Exception("Selected textured cell sharing mismatch.");
        textured=TexturedPortalSceneReader.Load(Resolve,"appearance/test.pob",pin,selectedAppearanceCells:Array.Empty<int>());
        if(textured.Sources.Count!=1||textured.Materials.Meshes.Count!=0)throw new Exception("Empty selected texture scene loaded materials.");
        HostAccountingScenarios.Reject<AppearanceGraphBudgetException>(()=>TexturedPortalSceneReader.Load(Resolve,"appearance/test.pob",pin,maximumDecodedBytes:4));
        HostAccountingScenarios.Reject<AppearanceGraphBudgetException>(()=>TexturedPortalSceneReader.Load(Resolve,"appearance/test.pob",pin,new(MaximumResources:5)));
        inputs.Remove("texture/synthetic.dds");textured=TexturedPortalSceneReader.Load(Resolve,"appearance/test.pob",pin);
        if(textured.Scene.CellAppearances.Count!=2 || textured.Materials.Meshes.Count!=0 || textured.Diagnostics.Count!=1)throw new Exception("Missing material removed cell geometry.");
        inputs.Remove("appearance/test.apt");
        scene=PortalSceneBundleReader.Load(Resolve,"appearance/test.pob",pin);
        if(scene.CellAppearances.Count!=0 || scene.Diagnostics.Count!=4)throw new Exception("Missing cell appearances were not explicit.");
        int failedReads=0;TreLookupResult BrokenFloor(string path){if(path=="appearance/collision/test.flr"){failedReads++;throw new InvalidDataException("Synthetic extraction failure.");}return Resolve(path);}
        PortalPathGraphBundle badFloor=PortalPathGraphBundleReader.Load(BrokenFloor,"appearance/test.pob",pin);
        if(failedReads!=1 || badFloor.Diagnostics.Count!=1)throw new Exception("Failed source attempt not accounted/diagnosed.");
        HostAccountingScenarios.Reject<AppearanceGraphBudgetException>(()=>PortalPathGraphBundleReader.Load(BrokenFloor,"appearance/test.pob",pin,new(MaximumResources:1)));
        Console.WriteLine("PASS: shared portal cell appearance roots, owned geometry, pinned identities, aggregate budgets and local omissions");
    }
    internal static void ProbeOptional(string[] args)
    {
        if(args.Length is not (8 or 10) || args[2]!="--configuration" || args[4]!="--member" || args[6]!="--expected-sha256" || args.Length==10&&args[8]!="--appearance-cells")throw new ArgumentException("Portal scene probe needs pinned root/configuration.");
        int[]? selected=args.Length==10?args[9].Split(',').Select(int.Parse).ToArray():null;
        using StreamReader config=File.OpenText(args[3]);using ConfiguredTreOverlay overlay=ConfiguredTreOverlay.Open(args[1],config);
        TexturedPortalScene? textured=args[0] is "--portal-textures" or "--portal-textures-defaults" or "--portal-materials" or "--portal-materials-defaults"?TexturedPortalSceneReader.Load(overlay.Lookup,args[5],args[7],loadFloors:args[0] is "--portal-textures" or "--portal-textures-defaults",loadPathGraphs:args[0] is "--portal-textures" or "--portal-textures-defaults",customizationMode:args[0] is "--portal-textures-defaults" or "--portal-materials-defaults"?CustomizableShaderInspectionMode.AuthoredTextureDefaults:CustomizableShaderInspectionMode.Reject,selectedAppearanceCells:selected):null;
        PortalSceneBundle scene=textured?.Scene??PortalSceneBundleReader.Load(overlay.Lookup,args[5],args[7],loadFloors:args[0] is "--portal-floors" or "--portal-paths" or "--portal-textures" or "--portal-textures-defaults" or "--portal-surface-paths",loadPathGraphs:args[0] is "--portal-paths" or "--portal-surface-paths",selectedAppearanceCells:selected);
        if(args[0]=="--portal-surface-paths")
        {
            var picker=new PortalSurfacePathQuery(scene.CellFloors.ToDictionary(pair=>pair.Key,pair=>new FloorSurfaceQuery(pair.Value)),scene.CellPathGraphs.ToDictionary(pair=>pair.Key,pair=>new DirectedPathGraphQuery(pair.Value)));int total=0;
            foreach(var pair in scene.CellFloors)
            {
                var floor=pair.Value;float lo=floor.Vertices.Count==0?0:floor.Vertices.Min(v=>v.Y)-1,hi=floor.Vertices.Count==0?0:floor.Vertices.Max(v=>v.Y)+1;
                for(int ti=0;ti<floor.Triangles.Count;ti++)
                {
                    var triangle=floor.Triangles[ti];var a=floor.Vertices[triangle.VertexIndices[0]];var b=floor.Vertices[triangle.VertexIndices[1]];var c=floor.Vertices[triangle.VertexIndices[2]];
                    if(triangle.NonSolidByte!=0 || System.Numerics.Vector3.Cross(b-a,c-a).Y==0)continue;
                    if(++total>10000)throw new InvalidDataException("Surface/path probe budget.");float x=(float)(((double)a.X+b.X+c.X)/3),z=(float)(((double)a.Z+b.Z+c.Z)/3);
                    var hit=picker.Pick(new(x,hi,z),new(x,lo,z),pair.Key);
                    Console.WriteLine("surface_path="+System.Text.Json.JsonSerializer.Serialize(new{cell=pair.Key,probe=ti,hit=hit is null?null:new{cell=hit.CellIndex,triangle=hit.Surface.TriangleIndex,node=hit.NearestNode,status=hit.Status.ToString(),position=new[]{hit.Surface.Position.X,hit.Surface.Position.Y,hit.Surface.Position.Z}}}));
                }
            }
            Console.WriteLine($"PASS surface_path probes={total}");
        }
        Console.WriteLine($"portal_scene cells={scene.Layout.Cells.Count} loaded={scene.CellAppearances.Count} sources={scene.Sources.Count} diagnostics={scene.Diagnostics.Count}");
        if(args[0] is "--portal-floors" or "--portal-paths" or "--portal-textures" or "--portal-textures-defaults" or "--portal-surface-paths"){Console.WriteLine($"portal_floors count={scene.CellFloors.Count}");foreach(var floor in scene.CellFloors)Console.WriteLine($"portal_floor={floor.Key} vertices={floor.Value.Vertices.Count} triangles={floor.Value.Triangles.Count} sha256={Stardust.Core.Proof.FloorMeshVerification.ComputeSha256(floor.Value)}");}
        if(args[0] is "--portal-paths" or "--portal-textures" or "--portal-textures-defaults" or "--portal-surface-paths")
        {
            if(scene.LayoutPathGraph is not null)Console.WriteLine($"portal_graph=layout nodes={scene.LayoutPathGraph.Nodes.Count} edges={scene.LayoutPathGraph.Edges.Count} sha256={Stardust.Core.Proof.PathGraphVerification.ComputeSha256(scene.LayoutPathGraph)}");
            foreach(var graph in scene.CellPathGraphs)Console.WriteLine($"portal_graph={graph.Key} nodes={graph.Value.Nodes.Count} edges={graph.Value.Edges.Count} sha256={Stardust.Core.Proof.PathGraphVerification.ComputeSha256(graph.Value)}");
        }
        foreach(var cell in scene.CellAppearances)Console.WriteLine($"cell={cell.Key} name={scene.Layout.Cells[cell.Key].Name} appearance={cell.Value.Path}");
        HashSet<string> meshes=new();
        void Visit(AppearanceGeometryTemplate template){if(template.Mesh is not null && meshes.Add(template.Path))Console.WriteLine($"portal_mesh={template.Path} vertices={template.Mesh.VertexCount} triangles={template.Mesh.TriangleCount} geometry_sha256={Stardust.Core.Proof.StaticMeshVerification.ComputeGeometrySha256(template.Mesh)} sorted_sha256={Stardust.Core.Proof.StaticMeshVerification.ComputeSortedIndicesSha256(template.Mesh)}");foreach(var child in template.Children)Visit(child.Template);}
        Visit(scene.Geometry.Root);
        if(textured is not null)
        {
            foreach(string meshPath in textured.Materials.Meshes.Keys)Console.WriteLine("portal_material_mesh="+meshPath);
            Dictionary<string,StaticEffectDescription> effectGraphs=new();
            foreach(var mesh in textured.Materials.Meshes.Values)foreach(var effect in mesh.Effects)effectGraphs.TryAdd(effect.Key,effect.Value);
            foreach(var effect in effectGraphs)
            for(int implementation=0;implementation<effect.Value.Implementations.Count;implementation++)
            {
                var candidate=effect.Value.Implementations[implementation];
                Console.WriteLine($"portal_effect={effect.Key} implementation={implementation} version={candidate.Version} phase={candidate.PhaseTag:X8} flag5={candidate.Flag5?.ToString()??"absent"} flag6={candidate.Flag6?.ToString()??"absent"}");
                for(int pass=0;pass<candidate.Passes.Count;pass++)
                {
                    var state=candidate.Passes[pass];
                    string fields=string.Join(",",state.Fields.OrderBy(field=>field.Key).Select(field=>$"{field.Key:X2}:{field.Value:X8}"));
                    Console.WriteLine($"portal_effect_pass={effect.Key} implementation={implementation} pass={pass} version={state.Version} stages={state.FixedStageCount} fields={fields}");
                }
            }
            Dictionary<string,DdsImage> textures=new();foreach(var mesh in textured.Materials.Meshes.Values)foreach(var texture in mesh.Textures)textures.TryAdd(texture.Key,texture.Value);
            foreach(var texture in textures)for(int i=0;i<texture.Value.Mips.Count;i++)Console.WriteLine($"portal_texture={texture.Key} mip={i} sha256={Convert.ToHexString(SHA256.HashData(texture.Value.Mips[i].Rgba.Span))}");
            Dictionary<string,DdsHdrImage> hdrTextures=new();foreach(var mesh in textured.Materials.Meshes.Values)foreach(var texture in mesh.HdrTextures)hdrTextures.TryAdd(texture.Key,texture.Value);
            foreach(var texture in hdrTextures)for(int level=0;level<texture.Value.Mips.Count;level++)Console.WriteLine($"portal_hdr_texture={texture.Key} mip={level} sha256={Convert.ToHexString(SHA256.HashData(texture.Value.Mips[level].RgbHalf.Span))}");
            Console.WriteLine($"portal_materials sources={textured.Sources.Count} meshes={textured.Materials.Meshes.Count} textures={textures.Count+hdrTextures.Count} mips={textures.Values.Sum(t=>t.Mips.Count)+hdrTextures.Values.Sum(t=>t.Mips.Count)} diagnostics={textured.Diagnostics.Count}");
            foreach(string diagnostic in textured.Diagnostics)Console.WriteLine("material_diagnostic="+diagnostic);
        }
        foreach(var source in (textured?.Sources??scene.Sources).Values)Console.WriteLine($"source={source.Path} priority={source.Priority} bytes={source.Bytes} sha256={source.Sha256}");
        foreach(string diagnostic in scene.Diagnostics)Console.WriteLine("diagnostic="+diagnostic);
    }
}
