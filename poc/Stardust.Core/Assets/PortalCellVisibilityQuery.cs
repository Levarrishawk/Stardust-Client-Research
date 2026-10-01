using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record PortalPerspectiveView(Vector3 Eye,Vector3 Forward,Vector3 Up,float VerticalFovRadians,float Aspect,float FarDistance);
public sealed record PortalCellVisibilityResult(IReadOnlyList<int> Cells,int TraversalStates,int ClippedTriangles,bool BudgetFallback);

/// <summary>Modern conservative portal-triangle clipping in layout coordinates; no legacy DPVS claim.</summary>
public sealed class PortalCellVisibilityQuery
{
    private readonly int[] cells;
    private readonly Dictionary<int,(int Target,Vector3[][] Triangles)[]> adjacency=new();
    private readonly int maximumStates;
    private readonly int maximumPlanes;
    private readonly record struct Plane(Vector3 Normal,float Offset)
    {
        internal float Distance(Vector3 point)=>Vector3.Dot(Normal,point)+Offset;
    }
    public PortalCellVisibilityQuery(PortalLayoutDescription layout,IReadOnlyCollection<int> selectedCells,int maximumStates=4096,int maximumPlanes=256)
    {
        ArgumentNullException.ThrowIfNull(layout);ArgumentNullException.ThrowIfNull(selectedCells);
        cells=selectedCells.OrderBy(c=>c).ToArray();
        if(cells.Length is 0 or >64||cells.Distinct().Count()!=cells.Length||cells.Any(c=>c<0||c>=layout.Cells.Count)||maximumStates is <1 or >65536||maximumPlanes is <6 or >512)throw new ArgumentException("Visibility selection/budget invalid.");
        this.maximumStates=maximumStates;this.maximumPlanes=maximumPlanes;int totalTriangles=0;
        foreach(int cell in cells)
        {
            var edges=new List<(int Target,Vector3[][] Triangles)>();
            if(layout.Cells[cell].Portals.Count>128)throw new InvalidDataException("Visibility reference budget.");
            foreach(var reference in layout.Cells[cell].Portals)
            {
                if(!cells.Contains(reference.TargetCell))continue;
                if(reference.GeometryIndex<0||reference.GeometryIndex>=layout.Geometry.Count)throw new InvalidDataException("Visibility geometry index invalid.");
                var geometry=layout.Geometry[reference.GeometryIndex];
                if(geometry.Indices.Count%3!=0||geometry.Indices.Count/3>4096)throw new InvalidDataException("Visibility triangle budget.");
                var triangles=new List<Vector3[]>();
                for(int i=0;i<geometry.Indices.Count;i+=3)
                {
                    var triangle=new Vector3[3];
                    for(int corner=0;corner<3;corner++)
                    {
                        uint index=geometry.Indices[i+corner];if(index>=geometry.Vertices.Count)throw new InvalidDataException("Visibility vertex index invalid.");
                        triangle[corner]=geometry.Vertices[(int)index];if(!Finite(triangle[corner]))throw new InvalidDataException("Visibility vertex invalid.");
                    }
                    if(++totalTriangles>8192)throw new InvalidDataException("Visibility aggregate triangle budget.");
                    triangles.Add(triangle);
                }
                edges.Add((reference.TargetCell,triangles.ToArray()));
            }
            adjacency.Add(cell,edges.ToArray());
        }
    }
    public PortalCellVisibilityResult Query(int sourceCell,PortalPerspectiveView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        if(!adjacency.ContainsKey(sourceCell)||!Finite(view.Eye)||!Finite(view.Forward)||!Finite(view.Up)||view.Forward.LengthSquared()<1e-12f||view.Up.LengthSquared()<1e-12f||!float.IsFinite(view.VerticalFovRadians)||view.VerticalFovRadians is <=0 or >=3.13f||!float.IsFinite(view.Aspect)||view.Aspect is <=0 or >100||!float.IsFinite(view.FarDistance)||view.FarDistance is <=0 or >1e6f)throw new ArgumentException("Visibility view invalid.");
        Vector3 forward=Vector3.Normalize(view.Forward),right=Vector3.Cross(forward,Vector3.Normalize(view.Up));
        if(right.LengthSquared()<1e-12f)throw new ArgumentException("Visibility view axes parallel.");
        right=Vector3.Normalize(right);Vector3 up=Vector3.Normalize(Vector3.Cross(right,forward));
        float vertical=view.VerticalFovRadians*.5f,horizontal=MathF.Atan(MathF.Tan(vertical)*view.Aspect);
        Plane ThroughEye(Vector3 normal)=>new(normal,-Vector3.Dot(normal,view.Eye));
        var initial=new[]{ThroughEye(forward),new Plane(-forward,Vector3.Dot(forward,view.Eye)+view.FarDistance),ThroughEye(forward*MathF.Sin(horizontal)+right*MathF.Cos(horizontal)),ThroughEye(forward*MathF.Sin(horizontal)-right*MathF.Cos(horizontal)),ThroughEye(forward*MathF.Sin(vertical)+up*MathF.Cos(vertical)),ThroughEye(forward*MathF.Sin(vertical)-up*MathF.Cos(vertical))};
        var work=new Stack<(int Cell,Plane[] Planes,int[] Path)>();work.Push((sourceCell,initial,new[]{sourceCell}));
        var visible=new HashSet<int>{sourceCell};int states=0,clipped=0,clipOperations=0;
        PortalCellVisibilityResult Fallback()=>new(Array.AsReadOnly(cells.ToArray()),states,clipped,true);
        while(work.Count!=0)
        {
            if(++states>maximumStates)return Fallback();
            var state=work.Pop();
            foreach(var edge in adjacency[state.Cell])
            {
                if(state.Path.Contains(edge.Target))continue;
                foreach(var triangle in edge.Triangles)
                {
                    if(++clipped>65536)return Fallback();
                    Vector3 triangleNormal=Vector3.Cross(triangle[1]-triangle[0],triangle[2]-triangle[0]);
                    if(triangleNormal.LengthSquared()>1e-12f&&MathF.Abs(Vector3.Dot(triangleNormal,view.Eye-triangle[0]))<=triangleNormal.Length()*1e-5f)
                    {
                        // At a portal plane, aperture projection degenerates. Overdraw is safer than losing the adjacent room.
                        visible.Add(edge.Target);if(work.Count>=maximumStates)return Fallback();
                        work.Push((edge.Target,state.Planes,state.Path.Append(edge.Target).ToArray()));continue;
                    }
                    var polygon=triangle.ToList();
                    foreach(var plane in state.Planes)
                    {
                        if(polygon.Count<3)break;
                        var output=new List<Vector3>();var previous=polygon[^1];float previousDistance=plane.Distance(previous);
                        foreach(var current in polygon)
                        {
                            if(++clipOperations>1_000_000)return Fallback();
                            float currentDistance=plane.Distance(current);bool previousInside=previousDistance>=-1e-5f,currentInside=currentDistance>=-1e-5f;
                            if(previousInside!=currentInside){float fraction=previousDistance/(previousDistance-currentDistance);output.Add(previous+(current-previous)*Math.Clamp(fraction,0,1));}
                            if(currentInside)output.Add(current);previous=current;previousDistance=currentDistance;
                        }
                        polygon=output;
                    }
                    if(polygon.Count<3)continue;
                    visible.Add(edge.Target);var planes=state.Planes.ToList();Vector3 center=polygon.Aggregate(Vector3.Zero,(sum,p)=>sum+p)/polygon.Count;
                    for(int i=0;i<polygon.Count;i++)
                    {
                        Vector3 normal=Vector3.Cross(polygon[i]-view.Eye,polygon[(i+1)%polygon.Count]-view.Eye);
                        if(normal.LengthSquared()<1e-12f)continue;
                        normal=Vector3.Normalize(normal);if(Vector3.Dot(normal,center-view.Eye)<0)normal=-normal;
                        planes.Add(ThroughEye(normal));
                    }
                    if(planes.Count>maximumPlanes||work.Count>=maximumStates)return Fallback();
                    work.Push((edge.Target,planes.ToArray(),state.Path.Append(edge.Target).ToArray()));
                }
            }
        }
        return new(Array.AsReadOnly(visible.OrderBy(c=>c).ToArray()),states,clipped,false);
    }
    private static bool Finite(Vector3 value)=>float.IsFinite(value.X)&&float.IsFinite(value.Y)&&float.IsFinite(value.Z)&&MathF.Max(MathF.Abs(value.X),MathF.Max(MathF.Abs(value.Y),MathF.Abs(value.Z)))<=1e6f;
}
