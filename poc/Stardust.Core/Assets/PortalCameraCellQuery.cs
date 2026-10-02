using System.Numerics;

namespace Stardust.Core.Assets;

public enum CameraCellStatus { Located, AmbiguousBoundary, UnloadedCell, UnpairedPortal, CrossingBudget }
public sealed record CameraPortalCrossing(int GeometryIndex,int SourceCell,int TargetCell,double Parameter);
public sealed record CameraCellResult(CameraCellStatus Status,int? CellIndex,IReadOnlyList<CameraPortalCrossing> Crossings);

/// <summary>Explicit modern cell tracking along a collision-resolved camera boom from a trusted actor-cell anchor.</summary>
public sealed class PortalCameraCellQuery
{
    private sealed record Triangle(Vector3 A,Vector3 B,Vector3 C);
    private sealed record Edge(int Geometry,int Target,bool Paired,Triangle[] Triangles);
    private readonly Dictionary<int,Edge[]> edges=new();
    private readonly int maximumCrossings;
    private const double BoundaryTolerance=1e-5;
    public PortalCameraCellQuery(PortalLayoutDescription layout,IReadOnlyCollection<int> selectedCells,int maximumCrossings=32)
    {
        ArgumentNullException.ThrowIfNull(layout);ArgumentNullException.ThrowIfNull(selectedCells);
        var selected=selectedCells.ToArray();
        if(layout.Cells.Count>4096||layout.Geometry.Count>4096||layout.Cells.Sum(c=>(long)c.Portals.Count)>32768||selected.Length is 0 or >64||selected.Distinct().Count()!=selected.Length||selected.Any(c=>c<0||c>=layout.Cells.Count)||maximumCrossings is <0 or >128)
            throw new ArgumentException("Camera cell selection/budget invalid.");
        this.maximumCrossings=maximumCrossings;int count=0;
        var sides=layout.Cells.SelectMany((c,i)=>c.Portals.Select(p=>(Cell:i,Reference:p))).GroupBy(p=>p.Reference.GeometryIndex).ToDictionary(p=>p.Key,p=>p.ToArray());
        foreach(int cell in selected)
        {
            if(layout.Cells[cell].Portals.Count>128)throw new InvalidDataException("Camera cell portal budget.");
            var owned=new List<Edge>();
            foreach(var reference in layout.Cells[cell].Portals)
            {
                if(reference.GeometryIndex<0||reference.GeometryIndex>=layout.Geometry.Count||reference.TargetCell<0||reference.TargetCell>=layout.Cells.Count)
                    throw new InvalidDataException("Camera portal reference invalid.");
                var geometry=layout.Geometry[reference.GeometryIndex];var triangles=new List<Triangle>();
                if(geometry.Indices.Count%3!=0)throw new InvalidDataException("Camera portal index width.");
                for(int i=0;i<geometry.Indices.Count;i+=3)
                {
                    Vector3 Vertex(int corner){uint index=geometry.Indices[i+corner];if(index>=geometry.Vertices.Count)throw new InvalidDataException("Camera portal vertex index.");var value=geometry.Vertices[(int)index];Validate(value);return value;}
                    var triangle=new Triangle(Vertex(0),Vertex(1),Vertex(2));
                    if(Vector3.Cross(triangle.B-triangle.A,triangle.C-triangle.A).LengthSquared()<1e-12f)continue;
                    if(++count>8192)throw new InvalidDataException("Camera portal triangle budget.");triangles.Add(triangle);
                }
                var pair=sides[reference.GeometryIndex];bool paired=pair.Length==2&&pair[0].Cell!=pair[1].Cell&&pair.All(p=>p.Reference.TargetCell==pair.Single(q=>q.Cell!=p.Cell).Cell);
                owned.Add(new(reference.GeometryIndex,reference.TargetCell,paired,triangles.ToArray()));
            }
            edges.Add(cell,owned.ToArray());
        }
    }
    public CameraCellResult Locate(int anchorCell,Vector3 localAnchor,Vector3 localCamera)
    {
        Validate(localAnchor);Validate(localCamera);if(!edges.ContainsKey(anchorCell))throw new ArgumentException("Camera anchor cell is not loaded.");
        int cell=anchorCell;double previous=-1;var crossed=new List<CameraPortalCrossing>();
        CameraCellResult Stop(CameraCellStatus status)=>new(status,status==CameraCellStatus.Located?cell:null,Array.AsReadOnly(crossed.ToArray()));
        while(true)
        {
            Edge? nearest=null;double parameter=double.PositiveInfinity;
            foreach(var edge in edges[cell])
            {
                foreach(var triangle in edge.Triangles)
                {
                    double Distance(Vector3 p)
                    {
                        var n=Vector3.Normalize(Vector3.Cross(triangle.B-triangle.A,triangle.C-triangle.A));
                        return (double)n.X*(p.X-triangle.A.X)+(double)n.Y*(p.Y-triangle.A.Y)+(double)n.Z*(p.Z-triangle.A.Z);
                    }
                    double start=Distance(localAnchor),end=Distance(localCamera);
                    if(Math.Abs(end)<=BoundaryTolerance&&Contains(triangle,localCamera))return Stop(CameraCellStatus.AmbiguousBoundary);
                    if(Math.Abs(start)<=BoundaryTolerance&&Contains(triangle,localAnchor))return Stop(CameraCellStatus.AmbiguousBoundary);
                    if(start*end>=0)continue;double t=start/(start-end);
                    if(t<=previous+1e-8)continue;
                    var point=Vector3.Lerp(localAnchor,localCamera,(float)t);if(!Contains(triangle,point))continue;
                    if(Math.Abs(t-parameter)<=1e-8&&nearest is not null&&nearest.Geometry!=edge.Geometry)return Stop(CameraCellStatus.AmbiguousBoundary);
                    if(t<parameter){parameter=t;nearest=edge;}
                }
            }
            if(nearest is null)return Stop(CameraCellStatus.Located);
            if(crossed.Count>=maximumCrossings)return Stop(CameraCellStatus.CrossingBudget);
            if(!nearest.Paired)return Stop(CameraCellStatus.UnpairedPortal);
            crossed.Add(new(nearest.Geometry,cell,nearest.Target,parameter));
            if(!edges.ContainsKey(nearest.Target))return Stop(CameraCellStatus.UnloadedCell);
            cell=nearest.Target;previous=parameter;
        }
    }
    public CameraCellResult LocateWorld(int anchorCell,Vector3 worldAnchor,Vector3 worldCamera,AppearanceTransform owner)
    {
        PortalBarrierCollider.ValidatePose(owner);Validate(worldAnchor);Validate(worldCamera);
        Vector3 Local(Vector3 point)
        {
            var p=point-owner.Translation;
            return new(owner.RowX.X*p.X+owner.RowY.X*p.Y+owner.RowZ.X*p.Z,owner.RowX.Y*p.X+owner.RowY.Y*p.Y+owner.RowZ.Y*p.Z,owner.RowX.Z*p.X+owner.RowY.Z*p.Y+owner.RowZ.Z*p.Z);
        }
        return Locate(anchorCell,Local(worldAnchor),Local(worldCamera));
    }
    private static bool Contains(Triangle triangle,Vector3 point)
    {
        var e=triangle.B-triangle.A;var f=triangle.C-triangle.A;var p=point-triangle.A;
        double Dot(Vector3 a,Vector3 b)=>(double)a.X*b.X+(double)a.Y*b.Y+(double)a.Z*b.Z;
        double ee=Dot(e,e),ef=Dot(e,f),ff=Dot(f,f),pe=Dot(p,e),pf=Dot(p,f),det=ee*ff-ef*ef;
        if(det<=0)return false;double u=(ff*pe-ef*pf)/det,v=(ee*pf-ef*pe)/det;
        return u>=-1e-6&&v>=-1e-6&&u+v<=1+1e-6;
    }
    private static void Validate(Vector3 value)
    {
        if(!float.IsFinite(value.X)||!float.IsFinite(value.Y)||!float.IsFinite(value.Z)||Math.Abs(value.X)>1e6||Math.Abs(value.Y)>1e6||Math.Abs(value.Z)>1e6)
            throw new InvalidDataException("Bounded finite camera cell vector required.");
    }
}
