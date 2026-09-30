using System.Text.Json;
using System.Security.Cryptography;
using Stardust.Core.Assets;
using Stardust.Core.Proof;
internal static class FloorMeshScenarios
{
    internal static void Run()
    {
        foreach(string v in new[]{"0003","0004","0005","0006"})
        {
            byte[] source=Fixture(v);FloorMeshDescription f=FloorMeshDecoder.Decode(source);
            if(f.Vertices.Count!=3 || f.Triangles.Count!=1 || f.Triangles[0].Id!=17 || f.Triangles[0].Tag!=-8 || f.Triangles[0].PortalIds[2]!=42 || f.Triangles[0].NonSolidByte!=3)throw new Exception("Floor fields mismatch.");
            for(int n=0;n<source.Length;n++)HostAccountingScenarios.Reject<InvalidDataException>(()=>FloorMeshDecoder.Decode(source.AsSpan(0,n)));
            Array.Clear(source);if(f.Vertices[1].X!=3 || f.Triangles[0].EdgeFlags[2]!=2)throw new Exception("Floor borrowed input.");
        }
        foreach(byte[] b in new[]{Fixture("0002"),Fixture(badIndex:true),Fixture(badNeighbor:true),Fixture(nonfinite:true),Fixture(badCount:true),Fixture().Concat(new byte[]{0}).ToArray()})HostAccountingScenarios.Reject<InvalidDataException>(()=>FloorMeshDecoder.Decode(b));
        Console.WriteLine("PASS: owned FLOR 0003-0006 positions, triangles/adjacency, raw edge/portal fields, optional metadata and malformed inputs");
    }
    internal static byte[] Fixture(string version="0006",bool badIndex=false,bool badNeighbor=false,bool nonfinite=false,bool badCount=false,bool pathGraph=false)
    {
        byte[] Bytes(Action<BinaryWriter> action){using MemoryStream s=new();using BinaryWriter w=new(s);action(w);return s.ToArray();}
        byte[] verts=Bytes(w=>{if(version=="0006")w.Write(badCount?4u:3u);foreach(float x in new[]{0f,2,0,3,2,0,0,nonfinite?float.NaN:2,4})w.Write(x);});
        byte[] tris=Bytes(w=>{if(version=="0006")w.Write(1u);w.Write(0);w.Write(1);w.Write(badIndex?3:2);w.Write(17u);w.Write(badNeighbor?1:-1);w.Write(-1);w.Write(-1);w.Write(0f);w.Write(-1f);w.Write(0f);w.Write(new byte[]{0,1,2,3});w.Write(-8);w.Write(-1);w.Write(-1);w.Write(42);});
        List<byte[]> nodes=new(){MeshScenarios.Chunk("VERT",verts),MeshScenarios.Chunk("TRIS",tris)};
        if(version=="0004"){nodes.Add(MeshScenarios.Chunk("PNOD",Array.Empty<byte>()));nodes.Add(MeshScenarios.Chunk("PEDG",Array.Empty<byte>()));}
        if(version is "0005" or "0006")nodes.Add(MeshScenarios.Chunk("BEDG",Bytes(w=>{w.Write(1u);w.Write(0);w.Write(5);w.Write((byte)7);})));if(pathGraph)nodes.Add(PathGraphScenarios.Fixture());return MeshScenarios.Form("FLOR",MeshScenarios.Form(version,nodes.ToArray()));
    }
    internal static void ProbeOptional(string[] args)
    {
        if(args.Length!=8 || args[2]!="--configuration" || args[4]!="--members" || args[6]!="--expected-members-sha256")throw new ArgumentException("Floor list needs pinned manifest/configuration.");
        if(new FileInfo(args[5]).Length>4_194_304)throw new InvalidDataException("Floor manifest budget.");byte[] manifest=File.ReadAllBytes(args[5]);
        if(!Convert.ToHexString(SHA256.HashData(manifest)).Equals(args[7],StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Floor manifest identity mismatch.");
        string[] members=File.ReadAllLines(args[5]);if(members.Length is 0 or >10000 || members.Distinct(StringComparer.Ordinal).Count()!=members.Length)throw new InvalidDataException("Floor manifest count/duplicates.");
        using StreamReader config=File.OpenText(args[3]);using ConfiguredTreOverlay overlay=ConfiguredTreOverlay.Open(args[1],config);
        string? Hash(IffNode? node)=>node is null?null:Convert.ToHexString(SHA256.HashData(node.Payload.Span));
        foreach(string member in members)
        {
            TreLookupResult source=overlay.Lookup(member);if(source.Kind!=TreLookupKind.Present || source.Payload is null)throw new InvalidDataException("Floor source absent.");
            FloorMeshDescription floor=FloorMeshDecoder.Decode(source.Payload);
            Console.WriteLine("floor="+JsonSerializer.Serialize(new{member,priority=source.Priority,sha256=Convert.ToHexString(SHA256.HashData(source.Payload)),version=floor.Version,vertices=floor.Vertices.Count,triangles=floor.Triangles.Count,boundary_edges=floor.BoundaryEdges.Count,geometry_sha256=FloorMeshVerification.ComputeSha256(floor),tree_sha256=Hash(floor.BoundingTree),graph_sha256=Hash(floor.PathGraph),legacy_nodes_sha256=Hash(floor.LegacyPathNodes),legacy_edges_sha256=Hash(floor.LegacyPathEdges),diagnostics=floor.Diagnostics.Count}));
        }
    }
}
