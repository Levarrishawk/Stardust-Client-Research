using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record StaticMeshCollisionGeometry(int SourceTriangleCount,int EligibleTriangleCount,int DegenerateTriangleCount,
    IReadOnlyList<CollisionTriangle> Triangles);

/// <summary>Owned indexed collision geometry with stable source triangle IDs and explicit exact degeneracy accounting.</summary>
public static class StaticMeshCollisionGeometryReader
{
    public static StaticMeshCollisionGeometry Create(StaticMeshDescription mesh,
        IReadOnlyDictionary<string,MeshPrimitiveCollisionFlags> shaderFlags,uint selector,int maximumTriangles=2048)
    {
        ArgumentNullException.ThrowIfNull(mesh);ArgumentNullException.ThrowIfNull(shaderFlags);
        if(selector>1)throw new ArgumentOutOfRangeException(nameof(selector));
        if(maximumTriangles is <0 or >StaticTriangleBoxCollider.MaximumSupportedTriangles)throw new ArgumentOutOfRangeException(nameof(maximumTriangles));
        if(mesh.Primitives.Count>4096)throw new InvalidDataException("Collision primitive budget.");
        var triangles=new List<CollisionTriangle>();int source=0,eligible=0,degenerate=0;
        foreach(var primitive in mesh.Primitives)
        {
            if(!shaderFlags.TryGetValue(primitive.ShaderPath,out var flags))throw new InvalidDataException("Missing shader collision predicates.");
            if(primitive.Vertices.Count>1_000_000||primitive.Indices.Count%3!=0||primitive.Indices.Count/3>65536-source)
                throw new InvalidDataException("Collision indexed geometry budget/extent.");
            var vertices=primitive.Vertices.Select(v=>v.Position).ToArray();var indices=primitive.Indices.ToArray();
            if(vertices.Any(v=>!Finite(v))||indices.Any(i=>i>=vertices.Length))throw new InvalidDataException("Invalid collision vertex/index.");
            for(int i=0;i<indices.Length;i+=3,source++)
            {
                if(!flags.AcceptsSelector(selector))continue;
                eligible++;var a=vertices[indices[i]];var b=vertices[indices[i+1]];var c=vertices[indices[i+2]];
                double ux=(double)b.X-a.X,uy=(double)b.Y-a.Y,uz=(double)b.Z-a.Z,vx=(double)c.X-a.X,vy=(double)c.Y-a.Y,vz=(double)c.Z-a.Z;
                if(uy*vz-uz*vy==0&&uz*vx-ux*vz==0&&ux*vy-uy*vx==0){degenerate++;continue;}
                if(triangles.Count==maximumTriangles)throw new InvalidDataException("Collision selected triangle budget.");
                triangles.Add(new(source,a,b,c));
            }
        }
        return new(source,eligible,degenerate,triangles.AsReadOnly());
    }
    private static bool Finite(Vector3 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z)&&Math.Abs(v.X)<=1e6f&&Math.Abs(v.Y)<=1e6f&&Math.Abs(v.Z)<=1e6f;
}
