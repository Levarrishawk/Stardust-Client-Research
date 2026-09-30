using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
internal static class PortalAssociationScenarios
{
    internal static void Run()
    {
        PortalLayoutDescription layout=PortalLayoutDecoder.Decode(PortalLayoutScenarios.Fixture());PathGraphDescription baseGraph=PathGraphDecoder.Decode(PathGraphScenarios.Fixture());
        PathGraphDescription graph=baseGraph with{Nodes=new[]{baseGraph.Nodes[0] with{GlobalKey=0,RawType=0},baseGraph.Nodes[1] with{GlobalKey=0,RawType=0}}};
        Dictionary<int,PathGraphDescription> cells=new(){[0]=graph,[1]=graph};PortalPathAssociations associations=PortalPathAssociationInspector.Inspect(layout,cells);
        if(associations.Bindings.Count!=4 || associations.ConnectionCandidates.Count!=1 || associations.ConnectionCandidates[0].LeftNodes.Count!=2 || associations.Diagnostics.Count!=5)throw new Exception("Multiple-node portal association mismatch.");
        PathGraphDescription global=baseGraph with{Nodes=new[]{baseGraph.Nodes[0] with{GlobalKey=0,RawType=5}}};associations=PortalPathAssociationInspector.Inspect(layout,cells,global);
        if(associations.Bindings.Any(binding=>binding.LayoutNodeIndex!=0) || associations.Diagnostics.Count!=1)throw new Exception("Typed portal layout-node key mapping mismatch.");
        cells[1]=graph with{Nodes=new[]{graph.Nodes[0] with{GlobalKey=9}}};associations=PortalPathAssociationInspector.Inspect(layout,cells,global);
        if(associations.ConnectionCandidates.Count!=0 || !associations.Diagnostics.Any(d=>d.StartsWith("GlobalKeyNotCellPortal:",StringComparison.Ordinal)) || !associations.Diagnostics.Any(d=>d.StartsWith("IncompletePortalCandidate:",StringComparison.Ordinal)))throw new Exception("Contradictory portal key was silently connected.");
        Console.WriteLine("PASS: paired portal geometry-key associations, typed layout-node lookup, multiple-node preservation and contradictory-key isolation");
    }
    internal static void ProbeOptional(string[] args)
    {
        if(args.Length!=8 || args[2]!="--configuration" || args[4]!="--members" || args[6]!="--expected-members-sha256")throw new ArgumentException("Association manifest/configuration required.");
        if(new FileInfo(args[5]).Length>1_048_576)throw new InvalidDataException("Association manifest budget.");byte[] manifest=File.ReadAllBytes(args[5]);if(Convert.ToHexString(SHA256.HashData(manifest))!=args[7])throw new InvalidDataException("Association manifest identity mismatch.");
        string[] members=File.ReadAllLines(args[5]);if(members.Length is 0 or >1000 || members.Distinct(StringComparer.Ordinal).Count()!=members.Length)throw new InvalidDataException("Association manifest count.");
        using StreamReader config=File.OpenText(args[3]);using ConfiguredTreOverlay overlay=ConfiguredTreOverlay.Open(args[1],config);
        foreach(string member in members)
        {
            TreLookupResult root=overlay.Lookup(member);if(root.Kind!=TreLookupKind.Present || root.Payload is null)throw new InvalidDataException("Association root missing.");
            PortalPathGraphBundle bundle=PortalPathGraphBundleReader.Load(overlay.Lookup,member,Convert.ToHexString(SHA256.HashData(root.Payload)));PortalPathAssociations value=PortalPathAssociationInspector.Inspect(bundle.Layout,bundle.CellPathGraphs,bundle.LayoutPathGraph);
            Console.WriteLine("associations="+JsonSerializer.Serialize(new{member,bindings=value.Bindings.Select(b=>new{cell=b.CellIndex,node=b.NodeIndex,geometry=b.GeometryIndex,layout_node=b.LayoutNodeIndex}),candidates=value.ConnectionCandidates.Select(c=>new{geometry=c.GeometryIndex,left_cell=c.LeftCell,right_cell=c.RightCell,left_nodes=c.LeftNodes,right_nodes=c.RightNodes}),diagnostics=value.Diagnostics,bundle_diagnostics=bundle.Diagnostics}));
        }
    }
}
