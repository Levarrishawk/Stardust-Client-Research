namespace Stardust.Core.Assets;
public readonly record struct PortalPathBinding(int CellIndex,int NodeIndex,int GeometryIndex,int? LayoutNodeIndex);
public sealed record PortalPathConnectionCandidate(int GeometryIndex,int LeftCell,int RightCell,IReadOnlyList<int> LeftNodes,IReadOnlyList<int> RightNodes);
public sealed record PortalPathAssociations(IReadOnlyList<PortalPathBinding> Bindings,IReadOnlyList<PortalPathConnectionCandidate> ConnectionCandidates,IReadOnlyList<string> Diagnostics);

/// <summary>Evidence-constrained association inspection. Candidates do not assert movement/door/permission legality.</summary>
public static class PortalPathAssociationInspector
{
    public static PortalPathAssociations Inspect(PortalLayoutDescription layout,IReadOnlyDictionary<int,PathGraphDescription> cellGraphs,PathGraphDescription? layoutGraph=null)
    {
        ArgumentNullException.ThrowIfNull(layout);ArgumentNullException.ThrowIfNull(cellGraphs);
        if(cellGraphs.Values.Sum(graph=>(long)graph.Nodes.Count)>65536)throw new InvalidDataException("Portal path association node budget.");
        Dictionary<int,int[]> sides=layout.Connections.ToDictionary(c=>c.GeometryIndex,c=>c.Sides.Select(side=>side.CellIndex).Distinct().OrderBy(cell=>cell).ToArray());
        Dictionary<int,int[]> layoutNodes=layoutGraph is null?new():layoutGraph.Nodes.Select((node,index)=>(node,index)).Where(value=>value.node.RawType is 3 or 5).GroupBy(value=>value.node.GlobalKey).ToDictionary(group=>group.Key,group=>group.Select(value=>value.index).ToArray());
        List<PortalPathBinding> bindings=new();List<string> diagnostics=new();Dictionary<int,List<PortalPathBinding>> groups=new();
        foreach(var pair in cellGraphs.OrderBy(pair=>pair.Key))
        {
            if(pair.Key<0 || pair.Key>=layout.Cells.Count)throw new InvalidDataException("Path graph cell index outside layout.");
            for(int i=0;i<pair.Value.Nodes.Count;i++)
            {
                PathGraphNode node=pair.Value.Nodes[i];if(node.GlobalKey==-1)continue;
                if(node.RawType!=0){diagnostics.Add($"UnsupportedGlobalNodeType:{pair.Key}:{i}:{node.RawType}");continue;}
                if(!sides.TryGetValue(node.GlobalKey,out int[]? cells) || !cells.Contains(pair.Key)){diagnostics.Add($"GlobalKeyNotCellPortal:{pair.Key}:{i}:{node.GlobalKey}");continue;}
                int? ordinal=null;if(layoutNodes.TryGetValue(node.GlobalKey,out int[]? matching) && matching.Length==1)ordinal=matching[0];else diagnostics.Add($"LayoutPortalNodeUnavailable:{pair.Key}:{i}:{node.GlobalKey}");
                PortalPathBinding binding=new(pair.Key,i,node.GlobalKey,ordinal);bindings.Add(binding);if(!groups.TryGetValue(node.GlobalKey,out var group)){group=new();groups.Add(node.GlobalKey,group);}group.Add(binding);
            }
        }
        List<PortalPathConnectionCandidate> candidates=new();
        foreach(var pair in groups.OrderBy(pair=>pair.Key))
        {
            int[] cells=sides[pair.Key];var byCell=pair.Value.GroupBy(binding=>binding.CellIndex).ToDictionary(group=>group.Key,group=>group.Select(binding=>binding.NodeIndex).OrderBy(node=>node).ToArray());
            if(byCell.Values.Any(nodes=>nodes.Length>1))diagnostics.Add($"MultiplePortalNodes:{pair.Key}");
            if(cells.Length!=2 || !byCell.ContainsKey(cells[0]) || !byCell.ContainsKey(cells[1])){diagnostics.Add($"IncompletePortalCandidate:{pair.Key}");continue;}
            candidates.Add(new(pair.Key,cells[0],cells[1],Array.AsReadOnly(byCell[cells[0]]),Array.AsReadOnly(byCell[cells[1]])));
        }
        return new(bindings.AsReadOnly(),candidates.AsReadOnly(),diagnostics.AsReadOnly());
    }
}
