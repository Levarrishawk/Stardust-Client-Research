using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
internal static class PathRouteScenarios
{
    internal static void Run()
    {
        PathGraphDescription baseline=PathGraphDecoder.Decode(PathGraphScenarios.Fixture());
        List<PathGraphNode> nodes=new(){baseline.Nodes[0] with{SerializedPosition=Vector3.Zero},baseline.Nodes[0] with{SerializedPosition=Vector3.UnitX},baseline.Nodes[0] with{SerializedPosition=Vector3.UnitY},baseline.Nodes[0] with{SerializedPosition=new(1,1,0)}};
        List<PathGraphEdge> edges=new(){new(0,1,99,99),new(0,2,0,0),new(1,3,0,0),new(2,3,0,0),new(1,1,0,0)};
        DirectedPathGraphQuery query=new(baseline with{Nodes=nodes,Edges=edges});DirectedPathRoute? route=query.FindRoute(0,3);
        if(route is null || route.Distance!=2 || !route.NodeIndices.SequenceEqual(new[]{0,1,3}) || !route.EdgeIndices.SequenceEqual(new[]{0,2}) || query.FindRoute(3,0) is not null || query.FindRoute(1,1)?.Distance!=0)throw new Exception("Directed route/tie/reverse policy mismatch.");
        if(!query.FindRoute(0,3,new[]{true,false,true,true})!.NodeIndices.SequenceEqual(new[]{0,2,3}) || query.FindRoute(0,3,new[]{true,false,false,true}) is not null || query.FindNearestNode(new(0.5f,0,0))!=0 || query.FindNearestNode(Vector3.Zero,new[]{false,false,false,false}) is not null)throw new Exception("Route availability/nearest policy mismatch.");
        nodes.Clear();edges.Clear();if(query.FindRoute(0,3)?.Distance!=2)throw new Exception("Route borrowed input.");
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(()=>query.FindRoute(-1,0));HostAccountingScenarios.Reject<ArgumentException>(()=>query.FindRoute(0,1,new[]{true}));HostAccountingScenarios.Reject<InvalidDataException>(()=>query.FindNearestNode(new(float.PositiveInfinity,0,0)));
        PathGraphNode zero=baseline.Nodes[0] with{SerializedPosition=Vector3.Zero};query=new(baseline with{Nodes=new[]{zero,zero,zero},Edges=new PathGraphEdge[]{new(0,1,0,0),new(1,0,0,0),new(1,2,0,0)}});
        if(query.FindRoute(0,2)?.NodeIndices.Count!=3 || query.FindRoute(0,2)?.Distance!=0)throw new Exception("Zero-weight cycle route mismatch.");
        var floor=FloorMeshDecoder.Decode(FloorMeshScenarios.Fixture());floor=floor with {Triangles=new[]{floor.Triangles[0] with {NonSolidByte=0}}};var floorQuery=new FloorSurfaceQuery(floor);
        var cellFloors=new Dictionary<int,FloorSurfaceQuery>{{4,floorQuery},{2,floorQuery}};
        var cellGraphs=new Dictionary<int,DirectedPathGraphQuery>{{4,new(baseline with {Nodes=Array.Empty<PathGraphNode>(),Edges=Array.Empty<PathGraphEdge>()})},{2,query}};
        var picker=new PortalSurfacePathQuery(cellFloors,cellGraphs);cellFloors.Clear();cellGraphs.Clear();
        var hit=picker.Pick(new(0.2f,3,0.2f),new(0.2f,1,0.2f));
        if(hit?.CellIndex!=2 || hit.NearestNode!=0 || hit.Status!=FloorPathSelectionStatus.Selected || picker.Pick(new(0.2f,3,0.2f),new(0.2f,1,0.2f),4)?.Status!=FloorPathSelectionStatus.EmptyCellGraph || picker.Pick(Vector3.One,Vector3.One) is not null || picker.Pick(new(0.2f,3,0.2f),new(0.2f,1,0.2f),0) is not null)throw new Exception("Surface/path cell ownership, ties, empty/missing hit or owned map mismatch.");
        picker=new(new Dictionary<int,FloorSurfaceQuery>{{0,floorQuery}},new Dictionary<int,DirectedPathGraphQuery>{{1,query}});
        if(picker.Pick(new(0.2f,3,0.2f),new(0.2f,1,0.2f))?.Status!=FloorPathSelectionStatus.MissingCellGraph)throw new Exception("Surface pick borrowed another cell graph.");
        HostAccountingScenarios.Reject<ArgumentException>(()=>new PortalSurfacePathQuery(new Dictionary<int,FloorSurfaceQuery>(),new Dictionary<int,DirectedPathGraphQuery>{{-1,query}}));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>picker.Pick(new(float.NaN,0,0),Vector3.Zero));
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(()=>picker.Pick(Vector3.Zero,Vector3.One,-2));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new PortalSurfacePathQuery(Enumerable.Range(0,4097).ToDictionary(index=>index,_=>floorQuery),new Dictionary<int,DirectedPathGraphQuery>()));
        Console.WriteLine("PASS: surface-to-path cell ownership, deterministic floor/node ties, missing/empty graph status, owned maps and invalid boundaries");
        Console.WriteLine("PASS: modern directed Euclidean routes, deterministic ties/zero cycles, unavailable nodes, nearest selection and owned inputs");
    }
    internal static void ProbeOptional(string[] args)
    {
        if(args.Length!=8 || args[2]!="--configuration" || args[4]!="--fixtures" || args[6]!="--expected-sha256")throw new ArgumentException("Route fixture/configuration required.");
        if(new FileInfo(args[5]).Length>8_388_608)throw new InvalidDataException("Route fixture budget.");byte[] input=File.ReadAllBytes(args[5]);if(!Convert.ToHexString(SHA256.HashData(input)).Equals(args[7],StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Route fixture SHA mismatch.");
        using JsonDocument document=JsonDocument.Parse(input);if(document.RootElement.GetArrayLength() is 0 or >10000)throw new InvalidDataException("Route fixture count.");
        using StreamReader config=File.OpenText(args[3]);using ConfiguredTreOverlay overlay=ConfiguredTreOverlay.Open(args[1],config);int total=0;
        foreach(JsonElement fixture in document.RootElement.EnumerateArray())
        {
            string member=fixture.GetProperty("member").GetString()!;TreLookupResult source=overlay.Lookup(member);if(source.Kind!=TreLookupKind.Present || source.Payload is null || Convert.ToHexString(SHA256.HashData(source.Payload))!=fixture.GetProperty("sha256").GetString())throw new InvalidDataException("Route source SHA mismatch.");
            IffNode root=IffDocument.Parse(source.Payload).Root;PathGraphDescription graph=PathGraphDecoder.Decode(root.Children.Single().Children.Single(n=>n.IsForm("PGRF")));DirectedPathGraphQuery query=new(graph);int index=0;
            foreach(JsonElement probe in fixture.GetProperty("queries").EnumerateArray())
            {
                if(++total>50000)throw new InvalidDataException("Route probe aggregate budget.");bool[]? allowed=probe.TryGetProperty("available",out var mask)?mask.EnumerateArray().Select(x=>x.GetBoolean()).ToArray():null;
                DirectedPathRoute? route=query.FindRoute(probe.GetProperty("from").GetInt32(),probe.GetProperty("to").GetInt32(),allowed);
                Console.WriteLine("route="+JsonSerializer.Serialize(new{member,index=index++,route=route is null?null:new{nodes=route.NodeIndices,edges=route.EdgeIndices,distance=route.Distance}}));
            }
        }
    }
}
