using System.Buffers.Binary;
using System.Numerics;
namespace Stardust.Core.Assets;
public sealed record PathGraphNode(int AuthoredId,int Field04,int GlobalKey,int RawType,Vector3 SerializedPosition,float Radius);
public readonly record struct PathGraphEdge(int From,int To,float LaneWidth0,float LaneWidth1);
public sealed record PathGraphDescription(int RawType,IReadOnlyList<PathGraphNode> Nodes,IReadOnlyList<PathGraphEdge> Edges,
    IReadOnlyList<int> EdgeCounts,IReadOnlyList<int> EdgeStarts,IReadOnlyList<IReadOnlyList<int>> OutgoingEdgeIndices);

/// <summary>Owned deployed PGRF0001 fields. Types/keys and serialized coordinate order are preserved.</summary>
public static class PathGraphDecoder
{
    public static PathGraphDescription Decode(ReadOnlySpan<byte> source)=>Decode(IffDocument.Parse(source).Root);
    public static PathGraphDescription Decode(IffNode root)
    {
        ArgumentNullException.ThrowIfNull(root);Require(root.IsForm("PGRF") && root.Children.Count==1 && root.Children[0].IsForm("0001"),"Unsupported PGRF envelope/version.");
        IReadOnlyList<IffNode> chunks=root.Children[0].Children;string[] tags={"META","PNOD","PEDG","ECNT","ESTR"};
        Require(chunks.Count==5 && chunks.Select(node=>node.Tag).SequenceEqual(tags) && chunks[0].Payload.Length==4,"Invalid path graph chunks.");
        ReadOnlySpan<byte> nodeBytes=Records(chunks[1].Payload.Span,32),edgeBytes=Records(chunks[2].Payload.Span,16),countsBytes=Records(chunks[3].Payload.Span,4),startsBytes=Records(chunks[4].Payload.Span,4);
        int count=nodeBytes.Length/32;Require(countsBytes.Length==count*4 && startsBytes.Length==count*4,"Path graph range table count mismatch.");
        PathGraphNode[] nodes=new PathGraphNode[count];PathGraphEdge[] edges=new PathGraphEdge[edgeBytes.Length/16];int[] counts=new int[count],starts=new int[count];
        for(int i=0;i<count;i++)
        {
            ReadOnlySpan<byte> n=nodeBytes.Slice(i*32,32);float radius=Float(n[28..]);Require(radius>=0,"Negative path node radius.");
            nodes[i]=new(Int(n),Int(n[4..]),Int(n[8..]),Int(n[12..]),new(Float(n[16..]),Float(n[20..]),Float(n[24..])),radius);
            counts[i]=Int(countsBytes[(i*4)..]);starts[i]=Int(startsBytes[(i*4)..]);
        }
        for(int i=0;i<edges.Length;i++)
        {
            ReadOnlySpan<byte> e=edgeBytes.Slice(i*16,16);int from=Int(e),to=Int(e[4..]);float w0=Float(e[8..]),w1=Float(e[12..]);
            Require(from>=0 && from<count && to>=0 && to<count,"Path edge endpoint out of range.");Require(w0>=0 && w1>=0,"Negative path edge width.");edges[i]=new(from,to,w0,w1);
        }
        bool[] covered=new bool[edges.Length];IReadOnlyList<int>[] outgoing=new IReadOnlyList<int>[count];
        for(int i=0;i<count;i++)
        {
            int c=counts[i],s=starts[i];Require(c>=0 && (c==0?s>=-1 && s<=edges.Length:s>=0 && (long)s+c<=edges.Length),"Path graph range out of bounds.");
            int[] indices=new int[c];for(int j=0;j<c;j++){int index=s+j;Require(!covered[index] && edges[index].From==i,"Path graph overlapping/wrong-source range.");covered[index]=true;indices[j]=index;}
            outgoing[i]=Array.AsReadOnly(indices);
        }
        Require(covered.All(value=>value),"Unreferenced path edge range.");
        return new(Int(chunks[0].Payload.Span),Array.AsReadOnly(nodes),Array.AsReadOnly(edges),Array.AsReadOnly(counts),Array.AsReadOnly(starts),Array.AsReadOnly(outgoing));
    }
    private static ReadOnlySpan<byte> Records(ReadOnlySpan<byte> bytes,int stride)
    {Require(bytes.Length>=4,"Truncated path graph count.");uint count=BinaryPrimitives.ReadUInt32LittleEndian(bytes);bytes=bytes[4..];Require(count<=1_000_000 && (ulong)count*(uint)stride==(ulong)bytes.Length,"Path graph count/stride budget mismatch.");return bytes;}
    private static int Int(ReadOnlySpan<byte> bytes)=>BinaryPrimitives.ReadInt32LittleEndian(bytes);
    private static float Float(ReadOnlySpan<byte> bytes){float value=BitConverter.Int32BitsToSingle(Int(bytes));Require(float.IsFinite(value),"Nonfinite path graph float.");return value;}
    private static void Require(bool condition,string message){if(!condition)throw new InvalidDataException(message);}
}
