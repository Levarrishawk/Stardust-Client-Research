using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Stardust.Core.Assets;
using Stardust.Core.Proof;

internal static class MeshScenarios
{
    internal static void Run()
    {
        StaticMeshDescription colored=StaticMeshDecoder.Decode(Mesh("0001",declaration:0x110D));
        if(colored.Primitives[0].Vertices[1].PackedColor0!=0x7F1234ABu || colored.Primitives[0].Vertices[1].TextureCoordinate!=Vector2.UnitX)throw new Exception("Packed color shifted vertex coordinates.");
        byte[] coloredBytes=Mesh("0001",declaration:0x110D);for(int n=0;n<coloredBytes.Length;n++)Reject(()=>StaticMeshDecoder.Decode(coloredBytes.AsSpan(0,n)));
        foreach (string primitiveVersion in new[] { "0000", "0001" })
        {
            byte[] fixture = Mesh(primitiveVersion);
            StaticMeshDescription decoded = StaticMeshDecoder.Decode(fixture);
            if (decoded.MeshVersion != "0004" || decoded.VertexCount != 3 || decoded.TriangleCount != 1 ||
                decoded.Primitives[0].ShaderPath != "shader/synthetic.sht" ||
                decoded.Primitives[0].Vertices[1] != new MeshVertex(new(1, 0, 0), new(0, 0, 1), new(1, 0)) ||
                !decoded.Primitives[0].Indices.SequenceEqual(new ushort[] { 0, 1, 2 }))
                throw new Exception("Synthetic static mesh values/widths mismatch.");
            for (int length = 0; length < fixture.Length; length++)
                Reject(() => StaticMeshDecoder.Decode(fixture.AsSpan(0, length)));
            Reject(() => StaticMeshDecoder.Decode(fixture.Concat(new byte[] { 0 }).ToArray()));
            Array.Clear(fixture);
            if (decoded.Primitives[0].Vertices[1].Position.X != 1 || decoded.AppearanceMetadata.Children[0].FormType != "0003")
                throw new Exception("Mesh borrowed caller input storage.");
        }
        foreach(uint declaration in new[]{0xD205u,0xD5405u,0x35305u})
        {
            byte[] packed=Mesh("0001",declaration:declaration);
            StaticMeshDescription decoded=StaticMeshDecoder.Decode(packed);
            var last=decoded.Primitives[0].Vertices[0].AdditionalTextureCoordinates![^1];
            int count=(int)(declaration>>8 & 15)-1;
            if(last.Dimension!=4 || last.Values.Z!=count*3 || last.Values.W!=-count*4)
                throw new Exception("Four-component coordinate payload was lost.");
            for(int n=0;n<packed.Length;n++)Reject(()=>StaticMeshDecoder.Decode(packed.AsSpan(0,n)));
            Reject(()=>StaticMeshDecoder.Decode(Mesh("0001",declaration:declaration,extraU:float.NaN)));
        }
        if(StaticMeshDecoder.Decode(Mesh("0001",declaration:0x5205)).Primitives[0].Vertices[0].AdditionalTextureCoordinates is not {Count:1} twoSets ||
            twoSets[0].Dimension!=2 || twoSets[0].Values!=new Vector4(12,-2,0,0))throw new Exception("Two-set coordinate layout mismatch.");
        foreach(string version in new[]{"0000","0001"})
        {
            byte[] bytes=Mesh(version,sorted:true,sortedPayload:SortedPayload(version));
            MeshPrimitive primitive=StaticMeshDecoder.Decode(bytes).Primitives[0];
            if(primitive.SortedIndices is not {Count:2} variants || variants[1].Direction!=-Vector3.UnitX ||
                !SortedMeshIndexSelector.Select(primitive,Vector3.UnitX).SequenceEqual(new ushort[]{2,1,0}) ||
                !ReferenceEquals(SortedMeshIndexSelector.Select(primitive,-Vector3.UnitX),variants[0].Indices) ||
                !ReferenceEquals(SortedMeshIndexSelector.Select(primitive,Vector3.Zero),variants[0].Indices))
                throw new Exception("Minimum-dot sorted-index selection mismatch.");
            for(int n=0;n<bytes.Length;n++)Reject(()=>StaticMeshDecoder.Decode(bytes.AsSpan(0,n)));
            Reject(()=>StaticMeshDecoder.Decode(Mesh(version,sorted:true,sortedPayload:SortedPayload(version,invalidIndex:true))));
            Reject(()=>StaticMeshDecoder.Decode(Mesh(version,sorted:true,sortedPayload:SortedPayload(version,nonfinite:true))));
            Reject(()=>StaticMeshDecoder.Decode(Mesh(version,sorted:true,sortedPayload:SortedPayload(version).Concat(new byte[]{0}).ToArray())));
            Array.Clear(bytes);
            if(variants[1].Indices[0]!=2)throw new Exception("Sorted indices borrowed source memory.");
        }
        MeshPrimitive ordinary=StaticMeshDecoder.Decode(Mesh("0001")).Primitives[0];
        if(!ReferenceEquals(ordinary.Indices,SortedMeshIndexSelector.Select(ordinary,Vector3.Zero)))throw new Exception("No sorted variants changed base indices.");
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(()=>SortedMeshIndexSelector.Select(ordinary,new(float.NaN,0,0)));
        Console.WriteLine("PASS: owned SIDX U32/U16 variants, minimum-dot ordering, stable ties, fallback and malformed cases");
        byte[] multiUv = Mesh("0001",declaration:0x55405);
        StaticMeshDescription multiDecoded = StaticMeshDecoder.Decode(multiUv);
        if(multiDecoded.Primitives[0].VertexDeclaration != 0x55405 ||
            multiDecoded.Primitives[0].Vertices[1].AdditionalTextureCoordinates is not {Count:3} extra ||
            extra[0] != new MeshTextureCoordinate(new(12,-2,0,0),2) || extra[2] != new MeshTextureCoordinate(new(14,-6,0,0),2))
            throw new Exception("Four-set vertex declaration lost auxiliary coordinates.");
        for(int n=0;n<multiUv.Length;n++) Reject(()=>StaticMeshDecoder.Decode(multiUv.AsSpan(0,n)));
        Reject(()=>StaticMeshDecoder.Decode(Mesh("0001",declaration:0x55405,extraU:float.NaN)));
        Array.Clear(multiUv);
        if(multiDecoded.Primitives[0].Vertices[1].AdditionalTextureCoordinates![0].Values.X != 12)
            throw new Exception("Auxiliary coordinates borrowed original bytes.");
        if (StaticMeshDecoder.Decode(Mesh("0000", indexOne: 0x10001)).Primitives[0].Indices[1] != 1)
            throw new Exception("Legacy U32 index did not preserve low-16 truncation.");
        Reject(() => StaticMeshDecoder.Decode(Mesh("0000", indexOne: 3)));
        Reject(() => StaticMeshDecoder.Decode(Mesh("0001", indexOne: 3)));
        Reject(() => StaticMeshDecoder.Decode(Mesh("0001", declaration: 0x1101)));
        Reject(() => StaticMeshDecoder.Decode(Mesh("0001", vertices: 4)));
        Reject(() => StaticMeshDecoder.Decode(Mesh("0001", topology: 10)));
        Reject(() => StaticMeshDecoder.Decode(Mesh("0001", sorted: true)));
        Reject(() => StaticMeshDecoder.Decode(Mesh("0001", firstX: float.NaN)));
        Reject(() => StaticMeshDecoder.Decode(Mesh("0001", firstX: float.PositiveInfinity)));
        Reject(() => StaticMeshDecoder.Decode(Mesh("0001", meshVersion: "0006")));
        byte[] invalidSize = Mesh("0000");
        BinaryPrimitives.WriteUInt32BigEndian(invalidSize.AsSpan(4), uint.MaxValue);
        Reject(() => IffDocument.Parse(invalidSize));
        byte[] odd = Form("TEST", Chunk("ODD ", new byte[] { 7 }), Chunk("NEXT", Array.Empty<byte>()));
        IffDocument oddTree = IffDocument.Parse(odd);
        if (oddTree.Root.Children.Count != 2 || oddTree.Root.Children[1].Offset != 21)
            throw new Exception("IFF inserted an unsupported even-byte pad.");
        Reject(() => IffDocument.Parse(odd, new(MaximumBytes: 4)));
        Reject(() => IffDocument.Parse(odd, new(MaximumNodes: 2)));
        byte[] nested = Form("LAST");
        for (int depth = 0; depth < 40; depth++) nested = Form("DEEP", nested);
        Reject(() => IffDocument.Parse(nested));
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(() => IffDocument.Parse(odd, new(MaximumDepth: 1000)));
        Console.WriteLine("PASS: owned unpadded bounded IFF tree, static mesh 1105/55405 geometry, versioned index widths and malformed inputs");
    }

    internal static void ProbeOptional(string[] args)
    {
        if (args.Length != 6 || args[0] != "--mesh-tre" || args[2] != "--member" || args[4] != "--expected-sha256")
            throw new ArgumentException("Mesh probe requires --mesh-tre PATH --member LOGICAL --expected-sha256 HASH.");
        using TreArchive archive = new(File.OpenRead(args[1]));
        byte[] source = archive.Read(args[3]);
        string hash = Convert.ToHexString(SHA256.HashData(source));
        if (!hash.Equals(args[5], StringComparison.OrdinalIgnoreCase)) throw new Exception("Original mesh identity mismatch.");
        StaticMeshDescription mesh = StaticMeshDecoder.Decode(source);
        Console.WriteLine($"PASS: original mesh SHA256={hash} bytes={source.Length} version={mesh.MeshVersion} batches={mesh.Primitives.Count} vertices={mesh.VertexCount} triangles={mesh.TriangleCount}");
        Console.WriteLine("geometry_sha256=" + StaticMeshVerification.ComputeGeometrySha256(mesh));
    }

    internal static byte[] Mesh(string primitiveVersion, uint indexOne = 1, uint declaration = 0x1105,
        uint vertices = 3, uint topology = 9, bool sorted = false, float firstX = 0, string meshVersion = "0004", float extraU = 11,byte[]? sortedPayload=null)
    {
        using MemoryStream vertexBytes = new();
        using (BinaryWriter writer = new(vertexBytes, Encoding.ASCII, leaveOpen: true))
            foreach ((Vector3 position, Vector2 uv) in new[]
                { (new Vector3(firstX, 0, 0), Vector2.Zero), (Vector3.UnitX, Vector2.UnitX), (Vector3.UnitY, Vector2.UnitY) })
            {
                writer.Write(position.X); writer.Write(position.Y); writer.Write(position.Z);
                writer.Write(0f); writer.Write(0f); writer.Write(1f); if(declaration==0x110D)writer.Write(0x7F1234ABu);writer.Write(uv.X); writer.Write(uv.Y);
                if(declaration is 0x55405 or 0xD205 or 0xD5405 or 0x5205 or 0x35305)
                    for(int set=1;set<(declaration>>8 & 15);set++)
                    {
                        int dimension=(int)(declaration>>(12+set*2)&3)+1;
                        writer.Write(extraU+set);writer.Write(-set*2f);
                        if(dimension>=3)writer.Write(set*3f);if(dimension==4)writer.Write(-set*4f);
                    }
            }
        using MemoryStream indexBytes = new();
        using (BinaryWriter writer = new(indexBytes, Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(3u);
            foreach (uint value in new[] { 0u, indexOne, 2u })
                if (primitiveVersion == "0000") writer.Write(value); else writer.Write((ushort)value);
        }
        byte[] primitiveInfo = new byte[6]; BinaryPrimitives.WriteUInt32LittleEndian(primitiveInfo, topology);
        primitiveInfo[4] = 1; primitiveInfo[5] = sorted ? (byte)1 : (byte)0;
        byte[] vertexInfo = new byte[8]; BinaryPrimitives.WriteUInt32LittleEndian(vertexInfo, declaration);
        BinaryPrimitives.WriteUInt32LittleEndian(vertexInfo.AsSpan(4), vertices);
        List<byte[]> primitiveChildren=new(){Chunk("INFO", primitiveInfo),
            Form("VTXA", Form("0003", Chunk("INFO", vertexInfo), Chunk("DATA", vertexBytes.ToArray()))),
            Chunk("INDX", indexBytes.ToArray())};
        if(sortedPayload is not null)primitiveChildren.Add(Chunk("SIDX",sortedPayload));
        byte[] primitive=Form(primitiveVersion,primitiveChildren.ToArray());
        byte[] one = new byte[] { 1, 0, 0, 0 };
        return Form("MESH", Form(meshVersion, Form("APPR", Form("0003", Chunk("ODD ", new byte[] { 1 }))),
            Form("SPS ", Form("0001", Chunk("CNT ", one), Form("0001",
                Chunk("NAME", Encoding.ASCII.GetBytes("shader/synthetic.sht\0")), Chunk("INFO", one), primitive)))));
    }
    private static byte[] SortedPayload(string version,bool invalidIndex=false,bool nonfinite=false)
    {
        using MemoryStream bytes=new();using BinaryWriter writer=new(bytes);
        writer.Write(2u);
        for(int i=0;i<2;i++)
        {
            writer.Write(nonfinite?float.NaN:i==0?1f:-1f);writer.Write(0f);writer.Write(0f);writer.Write(3u);
            foreach(uint index in i==0?new[]{0u,1u,2u}:new[]{invalidIndex?3u:2u,1u,0u})
                if(version=="0000")writer.Write(index);else writer.Write((ushort)index);
        }
        return bytes.ToArray();
    }

    internal static byte[] Chunk(string tag, byte[] bytes)
    {
        byte[] block = new byte[8 + bytes.Length]; Encoding.ASCII.GetBytes(tag).CopyTo(block, 0);
        BinaryPrimitives.WriteUInt32BigEndian(block.AsSpan(4), (uint)bytes.Length); bytes.CopyTo(block, 8); return block;
    }
    internal static byte[] Form(string type, params byte[][] children) => Chunk("FORM",
        Encoding.ASCII.GetBytes(type).Concat(children.SelectMany(child => child)).ToArray());
    private static void Reject(Action action) => HostAccountingScenarios.Reject<InvalidDataException>(action);
}
