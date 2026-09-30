using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class AppearanceHardpointScenarios
{
    internal static void Run()
    {
        byte[] Record(string name,float x=2)
        {
            using MemoryStream payload=new();using BinaryWriter writer=new(payload);
            foreach(float value in new[]{0f,-1,0,x,1,0,0,3,0,0,1,4})writer.Write(value);
            writer.Write(Encoding.ASCII.GetBytes(name));writer.Write((byte)0);
            return MeshScenarios.Chunk("HPNT",payload.ToArray());
        }
        byte[] Base(params byte[][] records)=>MeshScenarios.Form("APPR",MeshScenarios.Form("0003",MeshScenarios.Form("NULL"),MeshScenarios.Form("HPTS",records)));
        byte[] bytes=Base(Record("hp_A"),Record("hp_a"),Record("same"),Record("same",5),Record(""));
        AppearanceHardpointTable table=AppearanceHardpointDecoder.DecodeBase(IffDocument.Parse(bytes).Root);
        if(table.Authored.Count!=5 || table.DuplicateNames.Single()!="same" || table.FindAll("same").Count!=2 || table.TryFindUnique("same",out _) || table.TryFindUnique("missing",out _) ||
            !table.TryFindUnique("hp_A",out AppearanceHardpoint? first) || first!.LocalTransform.TransformPoint(new(1,2,3))!=new Vector3(0,4,7))throw new Exception("Owned hardpoint query/transform fixture mismatch.");
        if(table.FindAll("hp_a").Single().AuthoredIndex!=1 || table.FindAll("").Single().NameChecksum!=0)throw new Exception("Hardpoint name case/empty mismatch.");
        Array.Clear(bytes);if(first.Name!="hp_A" || first.LocalTransform.Translation.X!=2)throw new Exception("Hardpoint borrowed source.");
        bytes=Base(Record("test"));for(int length=0;length<bytes.Length;length++)Reject(()=>AppearanceHardpointDecoder.DecodeBase(IffDocument.Parse(bytes.AsSpan(0,length)).Root));
        Reject(()=>AppearanceHardpointDecoder.DecodeBase(IffDocument.Parse(Base(Record("test",float.NaN))).Root));
        Reject(()=>AppearanceHardpointDecoder.DecodeBase(IffDocument.Parse(Base(Record(new string('a',256)))).Root));
        Reject(()=>AppearanceHardpointDecoder.DecodeBase(IffDocument.Parse(Base(MeshScenarios.Chunk("CNT ",new byte[4]))).Root));
        Reject(()=>AppearanceHardpointDecoder.DecodeBase(IffDocument.Parse(Base(Record("embedded\0tail"))).Root));
        HostAccountingScenarios.Reject<ArgumentException>(()=>table.FindAll("line\nbreak"));
        if(AppearanceHardpointDecoder.DecodeBase(IffDocument.Parse(Base()).Root).Authored.Count!=0)throw new Exception("Empty HPTS fabricated entries.");
        var mesh=StaticMeshDecoder.Decode(MeshScenarios.Mesh("0001")) with {Hardpoints=table};
        var leaf=new AppearanceGeometryTemplate("appearance/test.msh",mesh,null,null,Array.Empty<AppearanceGeometryChild>(),Array.Empty<string>());
        AppearanceTransform parent=new(new(0,-1,0),new(1,0,0),Vector3.UnitZ,new(10,20,30));
        AppearanceTransform child=new(new(2,0,0),new(0,3,0),new(0,0,4),new(1,2,3));
        var children=new[]{new AppearanceGeometryChild(7,child,leaf),new AppearanceGeometryChild(9,AppearanceTransform.Identity,leaf)};
        var owner=new AppearanceGeometryTemplate("appearance/test.cmp",null,null,null,children,Array.Empty<string>());
        var graph=new AppearanceGeometryGraph(owner,new Dictionary<string,AssetSourceIdentity>());
        var scene=AppearanceHardpointSceneQuery.Collect(graph,parent);
        if(scene.InstanceCount!=3 || scene.Placements.Count!=10 || scene.Placements[0].ChildOrdinals.Single()!=7 || scene.Placements[0].PlacementTransform.TransformPoint(new(1,2,3))!=new Vector3(-4,21,61) || !AppearanceHardpointSceneQuery.TryFindUnique(scene,1,"hp_A",out _) || AppearanceHardpointSceneQuery.TryFindUnique(scene,1,"same",out _) || AppearanceHardpointSceneQuery.FindAll(scene,1,"hp_a").Single().Hardpoint.AuthoredIndex!=1)throw new Exception("Hardpoint scene composition/occurrence/ambiguity mismatch.");
        children[0]=new(100,AppearanceTransform.Identity,leaf);if(scene.Placements[0].ChildOrdinals.Single()!=7)throw new Exception("Hardpoint placement borrowed child path.");
        HostAccountingScenarios.Reject<AppearanceGraphBudgetException>(()=>AppearanceHardpointSceneQuery.Collect(graph,maximumInstances:2));
        HostAccountingScenarios.Reject<AppearanceGraphBudgetException>(()=>AppearanceHardpointSceneQuery.Collect(graph,maximumPlacements:9));
        HostAccountingScenarios.Reject<AppearanceGraphBudgetException>(()=>AppearanceHardpointSceneQuery.Collect(graph,maximumDepth:1));
        Reject(()=>AppearanceHardpointSceneQuery.Collect(graph,new(Vector3.UnitX,Vector3.UnitY,Vector3.UnitZ,new(float.NaN,0,0))));
        Reject(()=>AppearanceHardpointSceneQuery.Collect(graph,new(new(float.MaxValue,0,0),Vector3.UnitY,Vector3.UnitZ,Vector3.Zero)));
        var cyclic=new AppearanceGeometryChild[1];var cycle=owner with {Children=cyclic};cyclic[0]=new(0,AppearanceTransform.Identity,cycle);
        Reject(()=>AppearanceHardpointSceneQuery.Collect(graph with {Root=cycle}));
        Console.WriteLine("PASS: composed hardpoint instance paths, asymmetric nested transforms, shared templates, owned paths, ambiguity, cycles and aggregate limits");
        Console.WriteLine("PASS: owned APPR hardpoint names/transforms, CRC/string order, case sensitivity, duplicate ambiguity and malformed fixtures");
    }
    internal static void ProbeOptional(string[] args)
    {
        if(args.Length!=8 || args[2]!="--configuration" || args[4]!="--members" || args[6]!="--expected-members-sha256")throw new ArgumentException("Hardpoint inventory requires pinned member list/configuration.");
        if(new FileInfo(args[5]).Length>4_194_304)throw new InvalidDataException("Hardpoint list budget.");
        byte[] manifest=File.ReadAllBytes(args[5]);
        if(!Convert.ToHexString(SHA256.HashData(manifest)).Equals(args[7],StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Hardpoint member-list identity mismatch.");
        string[] members=File.ReadAllLines(args[5]);
        if(members.Length is 0 or >50_000 || members.Distinct(StringComparer.Ordinal).Count()!=members.Length)throw new InvalidDataException("Hardpoint list count/duplicates.");
        using StreamReader config=File.OpenText(args[3]);using ConfiguredTreOverlay overlay=ConfiguredTreOverlay.Open(args[1],config);
        foreach(string member in members)
        {
            TreLookupResult selected=overlay.Lookup(member);if(selected.Kind!=TreLookupKind.Present || selected.Payload is null)throw new InvalidDataException("Hardpoint inventory source missing/masked.");
            IffNode root=IffDocument.Parse(selected.Payload).Root;
            if(root.Children.Count!=1)throw new InvalidDataException("Unsupported inventory outer layout.");
            IffNode metadata=root.Children[0].Children.Single(node=>node.IsForm("APPR"));AppearanceHardpointTable table=AppearanceHardpointDecoder.DecodeBase(metadata);
            string TransformHex(AppearanceTransform value)
            {
                using MemoryStream stream=new();using BinaryWriter writer=new(stream);
                foreach(float field in new[]{value.RowX.X,value.RowX.Y,value.RowX.Z,value.Translation.X,value.RowY.X,value.RowY.Y,value.RowY.Z,value.Translation.Y,value.RowZ.X,value.RowZ.Y,value.RowZ.Z,value.Translation.Z})writer.Write(field);
                return Convert.ToHexString(stream.ToArray());
            }
            foreach(var group in table.Authored.GroupBy(value=>value.Name,StringComparer.Ordinal))
            {
                if(!table.FindAll(group.Key).Select(value=>value.AuthoredIndex).SequenceEqual(group.Select(value=>value.AuthoredIndex)) || table.TryFindUnique(group.Key,out _)!=(group.Count()==1))
                    throw new InvalidDataException("Original hardpoint lookup disagreement.");
            }
            Console.WriteLine("hardpoint="+JsonSerializer.Serialize(new{member,priority=selected.Priority,sha256=Convert.ToHexString(SHA256.HashData(selected.Payload)),
                hardpoints=table.Authored.Select(value=>new{name=value.Name,name_checksum=value.NameChecksum,transform_hex=TransformHex(value.LocalTransform)}),
                sorted_indices=table.Sorted.Select(value=>value.AuthoredIndex),duplicate_names=table.DuplicateNames}));
        }
        Console.WriteLine($"PASS hardpoint_inventory members={members.Length}");
    }
    internal static void ProbeSceneOptional(string[] args)
    {
        if(args.Length!=8 || args[2]!="--configuration" || args[4]!="--member" || args[6]!="--expected-sha256")throw new ArgumentException("Hardpoint scene needs pinned root.");
        using StreamReader config=File.OpenText(args[3]);using ConfiguredTreOverlay overlay=ConfiguredTreOverlay.Open(args[1],config);
        var graph=args[5].EndsWith(".pob",StringComparison.Ordinal)?PortalSceneBundleReader.Load(overlay.Lookup,args[5],args[7]).Geometry:AppearanceGeometryGraphReader.Load(overlay.Lookup,args[5],args[7]);
        var scene=AppearanceHardpointSceneQuery.Collect(graph);
        foreach(var placement in scene.Placements)
        {
            var t=placement.PlacementTransform;
            Console.WriteLine("hardpoint_placement="+JsonSerializer.Serialize(new{instance=placement.InstanceOrdinal,owner=placement.OwnerPath,children=placement.ChildOrdinals,authored=placement.Hardpoint.AuthoredIndex,name=placement.Hardpoint.Name,checksum=placement.Hardpoint.NameChecksum,matrix=new[]{t.RowX.X,t.RowX.Y,t.RowX.Z,t.Translation.X,t.RowY.X,t.RowY.Y,t.RowY.Z,t.Translation.Y,t.RowZ.X,t.RowZ.Y,t.RowZ.Z,t.Translation.Z}}));
        }
        foreach(var source in graph.Sources.Values)Console.WriteLine($"source={source.Path} priority={source.Priority} bytes={source.Bytes} sha256={source.Sha256}");
        Console.WriteLine($"PASS hardpoint_scene instances={scene.InstanceCount} placements={scene.Placements.Count}");
    }
    private static void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
}
