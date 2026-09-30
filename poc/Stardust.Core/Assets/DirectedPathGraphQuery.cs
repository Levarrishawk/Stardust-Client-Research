using System.Numerics;
namespace Stardust.Core.Assets;
public sealed record DirectedPathRoute(IReadOnlyList<int> NodeIndices,IReadOnlyList<int> EdgeIndices,double Distance);

/// <summary>Modern deterministic shortest-path inspection using Euclidean lengths, not recovered client route costs.</summary>
public sealed class DirectedPathGraphQuery
{
    private readonly Vector3[] positions;
    private readonly PathGraphEdge[] edges;
    private readonly int[][] outgoing;
    private readonly double[] weights;
    public int NodeCount=>positions.Length;
    public DirectedPathGraphQuery(PathGraphDescription graph)
    {
        ArgumentNullException.ThrowIfNull(graph);if(graph.Nodes.Count>1_000_000 || graph.Edges.Count>1_000_000)throw new InvalidDataException("Directed graph query budget.");
        positions=graph.Nodes.Select(n=>n.SerializedPosition).ToArray();foreach(var p in positions)Finite(p);
        edges=graph.Edges.ToArray();weights=new double[edges.Length];List<int>[] lists=Enumerable.Range(0,positions.Length).Select(_=>new List<int>()).ToArray();
        for(int i=0;i<edges.Length;i++)
        {
            PathGraphEdge e=edges[i];if(e.From<0 || e.From>=positions.Length || e.To<0 || e.To>=positions.Length)throw new InvalidDataException("Directed graph endpoint out of range.");
            lists[e.From].Add(i);weights[i]=Math.Sqrt(Squared(positions[e.From],positions[e.To]));
        }
        outgoing=lists.Select(list=>list.ToArray()).ToArray();
    }
    public int? FindNearestNode(Vector3 position,IReadOnlyList<bool>? available=null)
    {
        Finite(position);bool[] allowed=Allowed(available);int selected=-1;double nearest=double.PositiveInfinity;
        for(int i=0;i<positions.Length;i++)if(allowed[i]){double distance=Squared(position,positions[i]);if(distance<nearest){nearest=distance;selected=i;}}
        return selected<0?null:selected;
    }
    public DirectedPathRoute? FindRoute(int from,int to,IReadOnlyList<bool>? available=null)
    {
        if(from<0 || from>=positions.Length || to<0 || to>=positions.Length)throw new ArgumentOutOfRangeException(nameof(from));
        bool[] allowed=Allowed(available);if(!allowed[from] || !allowed[to])return null;
        double[] distance=Enumerable.Repeat(double.PositiveInfinity,positions.Length).ToArray();int[] previous=Enumerable.Repeat(-1,positions.Length).ToArray();bool[] settled=new bool[positions.Length];
        PriorityQueue<int,(double Distance,int Node)> pending=new();distance[from]=0;pending.Enqueue(from,(0,from));
        while(pending.TryDequeue(out int current,out var priority))
        {
            if(settled[current] || priority.Distance!=distance[current])continue;settled[current]=true;if(current==to)break;
            foreach(int edge in outgoing[current])
            {
                int target=edges[edge].To;if(!allowed[target] || settled[target])continue;double next=distance[current]+weights[edge];
                if(next>=distance[target])continue;distance[target]=next;previous[target]=edge;pending.Enqueue(target,(next,target));
            }
        }
        if(!settled[to])return null;
        List<int> nodes=new(){to},routeEdges=new();int node=to;
        while(node!=from){int edge=previous[node];if(edge<0 || routeEdges.Count>=positions.Length)throw new InvalidDataException("Invalid directed route predecessor chain.");routeEdges.Add(edge);node=edges[edge].From;nodes.Add(node);}
        nodes.Reverse();routeEdges.Reverse();return new(nodes.AsReadOnly(),routeEdges.AsReadOnly(),distance[to]);
    }
    private bool[] Allowed(IReadOnlyList<bool>? available)
    {if(available is not null && available.Count!=positions.Length)throw new ArgumentException("Availability count must equal node count.",nameof(available));return available?.ToArray()??Enumerable.Repeat(true,positions.Length).ToArray();}
    private static double Squared(Vector3 a,Vector3 b){double x=(double)a.X-b.X,y=(double)a.Y-b.Y,z=(double)a.Z-b.Z;return x*x+y*y+z*z;}
    private static void Finite(Vector3 p){if(!float.IsFinite(p.X) || !float.IsFinite(p.Y) || !float.IsFinite(p.Z))throw new InvalidDataException("Nonfinite graph query coordinate.");}
}
