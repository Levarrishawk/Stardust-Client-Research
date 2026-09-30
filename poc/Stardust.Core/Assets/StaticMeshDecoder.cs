using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace Stardust.Core.Assets;

public readonly record struct MeshTextureCoordinate(Vector4 Values, byte Dimension);
public readonly record struct MeshVertex(Vector3 Position, Vector3 Normal, Vector2 TextureCoordinate,
    IReadOnlyList<MeshTextureCoordinate>? AdditionalTextureCoordinates = null, uint? PackedColor0 = null);
public sealed record MeshSortedIndexVariant(Vector3 Direction,IReadOnlyList<ushort> Indices);
public sealed record MeshPrimitive(string ShaderPath, string PrimitiveVersion,
    IReadOnlyList<MeshVertex> Vertices, IReadOnlyList<ushort> Indices, uint VertexDeclaration = 0x1105,
    IReadOnlyList<MeshSortedIndexVariant>? SortedIndices=null);
public sealed record StaticMeshDescription(string MeshVersion, IffNode AppearanceMetadata,
    IReadOnlyList<MeshPrimitive> Primitives,AppearanceHardpointTable? Hardpoints=null)
{
    public int VertexCount => Primitives.Sum(primitive => primitive.Vertices.Count);
    public int TriangleCount => Primitives.Sum(primitive => primitive.Indices.Count / 3);
}

/// <summary>Bounded MESH 0004/0005 indexed triangle geometry, selected VTXA 0003 declarations and owned SIDX variants.</summary>
public static class StaticMeshDecoder
{
    public static StaticMeshDescription Decode(ReadOnlySpan<byte> source)
    {
        IffNode mesh = IffDocument.Parse(source).Root;
        Require(mesh.IsForm("MESH") && mesh.Children.Count == 1, "Invalid MESH envelope.");
        IffNode version = mesh.Children[0];
        Require(version.Tag == "FORM" && version.FormType is "0004" or "0005", "Unsupported MESH version.");
        Require(version.Children.Count == 2 && version.Children[0].IsForm("APPR") && version.Children[1].IsForm("SPS "),
            "Unsupported MESH child layout.");
        IffNode appearance = version.Children[0];
        IffNode sps = version.Children[1];
        Require(sps.Children.Count == 1 && sps.Children[0].IsForm("0001"), "Unsupported SPS version.");
        IReadOnlyList<IffNode> groups = sps.Children[0].Children;
        Require(groups.Count >= 1 && groups[0].Tag == "CNT " && groups[0].Payload.Length == 4, "Invalid SPS count.");
        uint groupCount = U32(groups[0].Payload.Span);
        Require(groupCount <= 1024 && groupCount == groups.Count - 1, "SPS group count mismatch/limit.");
        List<MeshPrimitive> primitives = new();
        foreach (IffNode group in groups.Skip(1))
        {
            Require(group.Tag == "FORM" && group.Children.Count >= 2, "Invalid shader group.");
            IffNode name = group.Children[0], info = group.Children[1];
            Require(name.Tag == "NAME" && info.Tag == "INFO" && info.Payload.Length == 4, "Inline shader or invalid group metadata.");
            string shader = ReadName(name.Payload.Span);
            uint primitiveCount = U32(info.Payload.Span);
            Require(primitiveCount <= 4096 && primitiveCount == group.Children.Count - 2 &&
                primitives.Count + primitiveCount <= 4096, "Primitive count mismatch/limit.");
            foreach (IffNode primitive in group.Children.Skip(2)) primitives.Add(ReadPrimitive(shader, primitive));
        }
        return new(version.FormType!, appearance, primitives.AsReadOnly(),AppearanceHardpointDecoder.DecodeBase(appearance));
    }

    private static MeshPrimitive ReadPrimitive(string shader, IffNode primitive)
    {
        Require(primitive.Tag == "FORM" && primitive.FormType is "0000" or "0001", "Unsupported primitive version.");
        Require(primitive.Children.Count is 3 or 4, "Unsupported primitive child layout.");
        IffNode info = primitive.Children[0], vtxa = primitive.Children[1], index = primitive.Children[2];
        Require(info.Tag == "INFO" && info.Payload.Length == 6, "Invalid primitive INFO.");
        ReadOnlySpan<byte> flags = info.Payload.Span;
        Require(U32(flags) == 9 && flags[4] == 1 && flags[5] <= 1 && primitive.Children.Count==3+flags[5],
            "Only indexed triangle lists with consistent sorted-index presence are supported.");
        Require(vtxa.IsForm("VTXA") && vtxa.Children.Count == 1 && vtxa.Children[0].IsForm("0003"), "Unsupported VTXA version.");
        IReadOnlyList<IffNode> vertexChunks = vtxa.Children[0].Children;
        Require(vertexChunks.Count == 2 && vertexChunks[0].Tag == "INFO" && vertexChunks[0].Payload.Length == 8 &&
            vertexChunks[1].Tag == "DATA", "Invalid VTXA chunks.");
        ReadOnlySpan<byte> vertexInfo = vertexChunks[0].Payload.Span;
        uint declaration = U32(vertexInfo);
        Require(declaration is 0x1105 or 0x55405 or 0xD205 or 0xD5405 or 0x5205 or 0x35305 or 0x110D, "Unsupported vertex declaration.");
        int coordinateCount = (int)(declaration >> 8 & 15);
        int[] dimensions = Enumerable.Range(0,coordinateCount).Select(set=>(int)(declaration >> (12+set*2) & 3)+1).ToArray();
        bool hasColor = declaration == 0x110D;
        int uvOffset = hasColor ? 28 : 24;
        int stride = uvOffset + dimensions.Sum()*4;
        uint vertexCount = U32(vertexInfo[4..]);
        ReadOnlySpan<byte> data = vertexChunks[1].Payload.Span;
        Require(vertexCount <= 1_000_000 && (ulong)vertexCount * (uint)stride == (ulong)data.Length, "Vertex count/stride mismatch.");
        MeshVertex[] vertices = new MeshVertex[(int)vertexCount];
        for (int i = 0; i < vertices.Length; i++)
        {
            ReadOnlySpan<byte> vertex = data.Slice(i * stride, stride);
            MeshTextureCoordinate[] additional = coordinateCount == 1 ? Array.Empty<MeshTextureCoordinate>() : new MeshTextureCoordinate[coordinateCount - 1];
            int coordinateOffset=uvOffset+8;
            for (int set = 1; set < coordinateCount; set++)
            {
                float x=F32(vertex[coordinateOffset..]),y=dimensions[set]>=2?F32(vertex[(coordinateOffset+4)..]):0;
                float z=dimensions[set]>=3?F32(vertex[(coordinateOffset+8)..]):0,w=dimensions[set]==4?F32(vertex[(coordinateOffset+12)..]):0;
                additional[set-1]=new(new(x,y,z,w),(byte)dimensions[set]);coordinateOffset+=dimensions[set]*4;
            }
            vertices[i] = new(new(F32(vertex), F32(vertex[4..]), F32(vertex[8..])),
                new(F32(vertex[12..]), F32(vertex[16..]), F32(vertex[20..])),
                new(F32(vertex[uvOffset..]), F32(vertex[(uvOffset+4)..])), additional.Length == 0 ? null : Array.AsReadOnly(additional),hasColor ? U32(vertex[24..]) : null);
        }
        Require(index.Tag == "INDX" && index.Payload.Length >= 4, "Invalid index chunk.");
        ReadOnlySpan<byte> indicesData = index.Payload.Span;
        uint indexCount = U32(indicesData);
        int width = primitive.FormType == "0000" ? 4 : 2;
        Require(indexCount <= 3_000_000 && indexCount % 3 == 0 &&
            4 + (ulong)indexCount * (uint)width == (ulong)indicesData.Length, "Index count/width mismatch.");
        ushort[] indices = new ushort[(int)indexCount];
        for (int i = 0; i < indices.Length; i++)
        {
            ReadOnlySpan<byte> entry = indicesData[(4 + i * width)..];
            indices[i] = width == 4 ? unchecked((ushort)U32(entry)) : BinaryPrimitives.ReadUInt16LittleEndian(entry);
            Require(indices[i] < vertexCount, "Index refers beyond the vertex array.");
        }
        IReadOnlyList<MeshSortedIndexVariant>? sorted=null;
        if(flags[5]!=0)
        {
            IffNode chunk=primitive.Children[3];Require(chunk.Tag=="SIDX" && chunk.Payload.Length>=4,"Invalid sorted-index chunk.");
            ReadOnlySpan<byte> payload=chunk.Payload.Span;uint count=U32(payload);
            Require(count<=4096,"Sorted-index variant budget exceeded.");
            int cursor=4;long aggregate=indexCount;List<MeshSortedIndexVariant> variants=new();
            for(uint variant=0;variant<count;variant++)
            {
                Require(payload.Length-cursor>=16,"Truncated sorted-index variant.");
                Vector3 direction=new(F32(payload[cursor..]),F32(payload[(cursor+4)..]),F32(payload[(cursor+8)..]));cursor+=12;
                uint entries=U32(payload[cursor..]);cursor+=4;aggregate+=entries;
                Require(aggregate<=3_000_000 && entries%3==0 && (ulong)entries*(uint)width<=(ulong)(payload.Length-cursor),"Invalid sorted-index count/width.");
                ushort[] values=new ushort[(int)entries];
                for(int i=0;i<values.Length;i++,cursor+=width)
                {
                    values[i]=width==4?unchecked((ushort)U32(payload[cursor..])):BinaryPrimitives.ReadUInt16LittleEndian(payload[cursor..]);
                    Require(values[i]<vertexCount,"Sorted index refers beyond the vertex array.");
                }
                variants.Add(new(direction,Array.AsReadOnly(values)));
            }
            Require(cursor==payload.Length,"Trailing sorted-index data.");sorted=variants.AsReadOnly();
        }
        return new(shader, primitive.FormType!, Array.AsReadOnly(vertices), Array.AsReadOnly(indices),declaration,sorted);
    }

    private static string ReadName(ReadOnlySpan<byte> bytes)
    {
        Require(bytes.Length is >= 2 and <= 1025 && bytes[^1] == 0, "Invalid shader name.");
        foreach (byte value in bytes[..^1]) Require(value is >= 0x20 and <= 0x7E, "Invalid shader name encoding.");
        try { return LegacyAssetPath.Normalize(Encoding.ASCII.GetString(bytes[..^1])); }
        catch (ArgumentException) { throw new InvalidDataException("Invalid shader logical name."); }
    }
    private static uint U32(ReadOnlySpan<byte> bytes) => BinaryPrimitives.ReadUInt32LittleEndian(bytes);
    private static float F32(ReadOnlySpan<byte> bytes)
    {
        float value = BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes));
        Require(float.IsFinite(value), "Non-finite vertex component.");
        return value;
    }
    private static void Require(bool valid, string message)
    {
        if (!valid) throw new InvalidDataException(message);
    }
}
