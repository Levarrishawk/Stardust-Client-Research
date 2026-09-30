using System.Collections.ObjectModel;
namespace Stardust.Core.Assets;
internal sealed record PortalFloorResources(IReadOnlyDictionary<int,FloorMeshDescription> Floors,IReadOnlyDictionary<int,PathGraphDescription> Graphs,PathGraphDescription? LayoutGraph,IReadOnlyList<string> Diagnostics)
{
    internal static PortalFloorResources Load(PortalLayoutDescription layout,Func<string,TreLookupResult> Read,bool loadFloors,bool loadPathGraphs)
    {
        List<string> diagnostics=new();
        Dictionary<int,FloorMeshDescription> cellFloors=new();Dictionary<string,FloorMeshDescription> floorTemplates=new(StringComparer.Ordinal);
        if(loadFloors || loadPathGraphs)
        {
            for(int cell=0;cell<layout.Cells.Count;cell++)
            {
                string? floorPath=layout.Cells[cell].FloorPath;if(string.IsNullOrEmpty(floorPath))continue;
                try
                {
                    floorPath=LegacyAssetPath.Normalize(floorPath);
                    if(!floorTemplates.TryGetValue(floorPath,out FloorMeshDescription? floor))
                    {
                        TreLookupResult selected=Read(floorPath);
                        if(selected.Kind!=TreLookupKind.Present || selected.Payload is null)throw new InvalidDataException("Missing/masked floor.");
                        floor=FloorMeshDecoder.Decode(selected.Payload);floorTemplates.Add(floorPath,floor);
                    }
                    cellFloors.Add(cell,floor);
                }
                catch(InvalidDataException failure){diagnostics.Add($"UnavailableCellFloor:{cell}:{floorPath}:{failure.Message}");}
            }
        }
        Dictionary<int,PathGraphDescription> cellGraphs=new();PathGraphDescription? layoutGraph=null;
        if(loadPathGraphs)
        {
            Dictionary<FloorMeshDescription,PathGraphDescription> shared=new(ReferenceEqualityComparer.Instance);
            foreach(var pair in cellFloors)
            {
                if(pair.Value.PathGraph is null)continue;
                try
                {
                    if(!shared.TryGetValue(pair.Value,out PathGraphDescription? graph)){graph=PathGraphDecoder.Decode(pair.Value.PathGraph);shared.Add(pair.Value,graph);}cellGraphs.Add(pair.Key,graph);
                }
                catch(InvalidDataException failure){diagnostics.Add($"UnavailableCellPathGraph:{pair.Key}:{failure.Message}");}
            }
            if(layout.PathGraph is not null)
                try{layoutGraph=PathGraphDecoder.Decode(layout.PathGraph);}catch(InvalidDataException failure){diagnostics.Add($"UnavailableLayoutPathGraph:{failure.Message}");}
        }
        return new(new ReadOnlyDictionary<int,FloorMeshDescription>(cellFloors),new ReadOnlyDictionary<int,PathGraphDescription>(cellGraphs),layoutGraph,diagnostics.AsReadOnly());
    }
}
