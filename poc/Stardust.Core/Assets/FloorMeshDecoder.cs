using System.Buffers.Binary;
using System.Numerics;
namespace Stardust.Core.Assets;

public sealed record FloorTriangle(IReadOnlyList<int> VertexIndices,uint Id,IReadOnlyList<int> Neighbors,Vector3 AuthoredNormal,
    IReadOnlyList<byte> EdgeFlags,byte NonSolidByte,int Tag,IReadOnlyList<int> PortalIds);
public readonly record struct FloorBoundaryEdge(int TriangleIndex,int EdgeIndex,byte RawFlag);
public sealed record FloorMeshDescription(string Version,IReadOnlyList<Vector3> Vertices,IReadOnlyList<FloorTriangle> Triangles,
    IReadOnlyList<FloorBoundaryEdge> BoundaryEdges,IffNode? BoundingTree,IffNode? PathGraph,IffNode? LegacyPathNodes,IffNode? LegacyPathEdges,IReadOnlyList<string> Diagnostics);

/// <summary>Owned deployed FLOR geometry and adjacency. Stored normal and edge bytes retain their authored representation.</summary>
public static class FloorMeshDecoder
{
    public static FloorMeshDescription Decode(ReadOnlySpan<byte> source)
    {
        IffNode root=IffDocument.Parse(source).Root;
        Require(root.IsForm("FLOR") && root.Children.Count==1,"Invalid FLOR envelope.");
        IffNode version=root.Children[0];Require(version.Tag=="FORM" && version.FormType is "0003" or "0004" or "0005" or "0006","Unsupported FLOR version.");
        Require(version.Children.Count>=2 && version.Children[0].Tag=="VERT" && version.Children[1].Tag=="TRIS","Invalid floor geometry order.");
        bool counted=version.FormType=="0006";
        ReadOnlySpan<byte> positions=Records(version.Children[0].Payload.Span,12,counted,1_000_000);
        ReadOnlySpan<byte> records=Records(version.Children[1].Payload.Span,60,counted,1_000_000);
        Vector3[] vertices=new Vector3[positions.Length/12];for(int i=0;i<vertices.Length;i++)vertices[i]=Vector(positions[(i*12)..]);
        FloorTriangle[] triangles=new FloorTriangle[records.Length/60];List<string> diagnostics=new();HashSet<uint> ids=new();
        for(int i=0;i<triangles.Length;i++)
        {
            ReadOnlySpan<byte> row=records.Slice(i*60,60);
            int[] indices={I32(row),I32(row[4..]),I32(row[8..])},neighbors={I32(row[16..]),I32(row[20..]),I32(row[24..])},portals={I32(row[48..]),I32(row[52..]),I32(row[56..])};
            Require(indices.All(index=>index>=0 && index<vertices.Length),"Floor vertex index out of range.");
            Require(neighbors.All(index=>index==-1 || index>=0 && index<triangles.Length),"Floor neighbor index out of range.");
            uint id=U32(row[12..]);if(!ids.Add(id))diagnostics.Add($"DuplicateTriangleId:{i}:{id}");
            byte[] flags={row[40],row[41],row[42]};
            triangles[i]=new(Array.AsReadOnly(indices),id,Array.AsReadOnly(neighbors),Vector(row[28..]),Array.AsReadOnly(flags),row[43],I32(row[44..]),Array.AsReadOnly(portals));
        }
        List<FloorBoundaryEdge> boundary=new();IffNode? tree=null,graph=null,pathNodes=null,pathEdges=null;int phase=0;
        foreach(IffNode node in version.Children.Skip(2))
        {
            if(version.FormType=="0004")
            {
                if(node.Tag=="PNOD" && phase==0){pathNodes=node;phase=1;}
                else if(node.Tag=="PEDG" && phase==1){pathEdges=node;phase=2;}
                else throw new InvalidDataException("Invalid legacy floor path metadata.");
            }
            else if(version.FormType is "0005" or "0006")
            {
                if(node.IsForm("BTRE") && phase==0){tree=node;phase=1;}
                else if(node.Tag=="BEDG" && phase<=1)
                {
                    ReadOnlySpan<byte> rows=Records(node.Payload.Span,9,true,3_000_000);
                    for(int i=0;i<rows.Length;i+=9)
                    {
                        int triangle=I32(rows[i..]),edge=I32(rows[(i+4)..]);
                        Require(triangle>=0 && triangle<triangles.Length && edge>=0,"Floor boundary reference out of range.");
                        // Original consumer uses edge modulo three; preserve the full authored integer.
                        boundary.Add(new(triangle,edge,rows[i+8]));
                    }
                    phase=2;
                }
                else if(node.IsForm("PGRF") && phase<=2){graph=node;phase=3;}
                else throw new InvalidDataException("Invalid optional floor metadata order.");
            }
            else throw new InvalidDataException("Unexpected floor metadata.");
        }
        return new(version.FormType!,Array.AsReadOnly(vertices),Array.AsReadOnly(triangles),boundary.AsReadOnly(),tree,graph,pathNodes,pathEdges,diagnostics.AsReadOnly());
    }
    private static ReadOnlySpan<byte> Records(ReadOnlySpan<byte> bytes,int stride,bool counted,int maximum)
    {
        if(counted){Require(bytes.Length>=4,"Truncated floor count.");uint count=U32(bytes);bytes=bytes[4..];Require(count<=maximum && (ulong)count*(uint)stride==(ulong)bytes.Length,"Floor count/stride mismatch.");}
        Require(bytes.Length%stride==0 && bytes.Length/stride<=maximum,"Floor record stride/budget.");return bytes;
    }
    private static Vector3 Vector(ReadOnlySpan<byte> bytes)=>new(Float(bytes),Float(bytes[4..]),Float(bytes[8..]));
    private static float Float(ReadOnlySpan<byte> bytes){float value=BitConverter.Int32BitsToSingle(I32(bytes));Require(float.IsFinite(value),"Nonfinite floor vector.");return value;}
    private static int I32(ReadOnlySpan<byte> bytes)=>BinaryPrimitives.ReadInt32LittleEndian(bytes);
    private static uint U32(ReadOnlySpan<byte> bytes)=>BinaryPrimitives.ReadUInt32LittleEndian(bytes);
    private static void Require(bool condition,string message){if(!condition)throw new InvalidDataException(message);}
}
