using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Proof;
internal static class PathGraphScenarios
{
    internal static void Run()
    {
        byte[] input=Fixture();PathGraphDescription graph=PathGraphDecoder.Decode(input);
        if(graph.RawType!=3 || graph.Nodes[0].AuthoredId!=40 || graph.Nodes[1].AuthoredId!=-9 || graph.Nodes[0].SerializedPosition!=new System.Numerics.Vector3(1,2,3) || graph.Nodes[0].Radius!=0 ||
            graph.Edges[0]!=new PathGraphEdge(0,1,0.25f,0.75f) || graph.EdgeStarts[1]!=-1 || graph.OutgoingEdgeIndices[0].Single()!=0 || graph.OutgoingEdgeIndices[1].Count!=0)throw new Exception("Path graph fields/range policy mismatch.");
        for(int n=0;n<input.Length;n++)HostAccountingScenarios.Reject<InvalidDataException>(()=>PathGraphDecoder.Decode(input.AsSpan(0,n)));
        Array.Clear(input);if(graph.Nodes[0].SerializedPosition.Z!=3)throw new Exception("Path graph borrowed source.");
        foreach(byte[] bad in new[]{Fixture(badEndpoint:true),Fixture(badRange:true),Fixture(nonfinite:true),Fixture(negativeWidth:true),Fixture(badCount:true),Fixture().Concat(new byte[]{0}).ToArray()})HostAccountingScenarios.Reject<InvalidDataException>(()=>PathGraphDecoder.Decode(bad));
        Console.WriteLine("PASS: owned PGRF0001 raw node/edge fields, directed ordinal ranges, zero/-1 empty range and malformed inputs");
    }
    internal static byte[] Fixture(bool badEndpoint=false,bool badRange=false,bool nonfinite=false,bool negativeWidth=false,bool badCount=false)
    {
        byte[] Bytes(Action<BinaryWriter> write){using MemoryStream s=new();using BinaryWriter w=new(s);write(w);return s.ToArray();}
        return MeshScenarios.Form("PGRF",MeshScenarios.Form("0001",
            MeshScenarios.Chunk("META",Bytes(w=>w.Write(3))),
            MeshScenarios.Chunk("PNOD",Bytes(w=>{w.Write(badCount?3u:2u);for(int i=0;i<2;i++){w.Write(i==0?40:-9);w.Write(-1);w.Write(100+i);w.Write(i);w.Write(nonfinite?float.NaN:1f+i);w.Write(2f+i);w.Write(3f+i);w.Write(0f);}})),
            MeshScenarios.Chunk("PEDG",Bytes(w=>{w.Write(1u);w.Write(0);w.Write(badEndpoint?2:1);w.Write(negativeWidth?-1f:0.25f);w.Write(0.75f);})),
            MeshScenarios.Chunk("ECNT",Bytes(w=>{w.Write(2u);w.Write(1);w.Write(0);})),
            MeshScenarios.Chunk("ESTR",Bytes(w=>{w.Write(2u);w.Write(badRange?1:0);w.Write(-1);}))));
    }
    internal static void ProbeOptional(string[] args)
    {
        if(args.Length!=8 || args[2]!="--configuration" || args[4]!="--members" || args[6]!="--expected-members-sha256")throw new ArgumentException("Path graph manifest/configuration required.");
        if(new FileInfo(args[5]).Length>4_194_304)throw new InvalidDataException("Path graph manifest budget.");byte[] manifest=File.ReadAllBytes(args[5]);if(!Convert.ToHexString(SHA256.HashData(manifest)).Equals(args[7],StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Path graph manifest SHA mismatch.");
        string[] members=File.ReadAllLines(args[5]);if(members.Length is 0 or >10000 || members.Distinct(StringComparer.Ordinal).Count()!=members.Length)throw new InvalidDataException("Path graph manifest count.");
        using StreamReader config=File.OpenText(args[3]);using ConfiguredTreOverlay overlay=ConfiguredTreOverlay.Open(args[1],config);
        foreach(string member in members)
        {
            TreLookupResult source=overlay.Lookup(member);if(source.Kind!=TreLookupKind.Present || source.Payload is null)throw new InvalidDataException("Path graph source missing.");
            IffNode root=IffDocument.Parse(source.Payload).Root;IffNode node=root.Children.Single().Children.Single(n=>n.IsForm("PGRF"));PathGraphDescription graph=PathGraphDecoder.Decode(node);
            Console.WriteLine("path_graph="+JsonSerializer.Serialize(new{member,priority=source.Priority,sha256=Convert.ToHexString(SHA256.HashData(source.Payload)),type=graph.RawType,nodes=graph.Nodes.Count,edges=graph.Edges.Count,geometry_sha256=PathGraphVerification.ComputeSha256(graph)}));
        }
    }
}
