using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Stardust.Core.Assets;

internal static class DetailAppearanceScenarios
{
    internal static void Run()
    {
        foreach (string version in new[]{"0005","0006","0007"})
        {
            byte[] bytes = Fixture(version);
            DetailAppearanceDescription value = DetailAppearanceDecoder.Decode(bytes);
            if(value.Levels.Count != 2 || value.Levels[0].Id != 42 || value.Levels[1].ChildPath != "appearance/mesh/near.msh" ||
                value.Pivot != (version == "0005" ? null : (byte?)1) || value.Policies["WRIT"].Geometry?.Indices[2] != 2)
                throw new Exception("Authored detail structure mismatch.");
            for(int i=0;i<bytes.Length;i++) Reject(()=>DetailAppearanceDecoder.Decode(bytes.AsSpan(0,i)));
            Array.Clear(bytes);
            if(value.Metadata.FormType != "DTLA" || value.Policies["WRIT"].Geometry!.Vertices[1].X != 1)
                throw new Exception("Detail description borrowed source memory.");
        }
        DetailAppearanceDescription irregular = DetailAppearanceDecoder.Decode(Fixture("0007",duplicate:true,reversed:true));
        if(irregular.Diagnostics.Count != 2 || irregular.Levels[0].ChildPath != "appearance/mesh/replacement.msh")
            throw new Exception("Authored irregularity diagnostics mismatch.");
        Reject(()=>DetailAppearanceDecoder.Decode(Fixture("0004")));
        Reject(()=>DetailAppearanceDecoder.Decode(Fixture("0007",unknownId:true)));
        Reject(()=>DetailAppearanceDecoder.Decode(Fixture("0007",badIndex:true)));
        Reject(()=>DetailAppearanceDecoder.Decode(Fixture("0007",nonfinite:true)));
        DetailAppearanceSelector selector = new(DetailAppearanceDecoder.Decode(Fixture("0007")));
        bool[] available = {true,true};
        if(selector.Select(50,-1,available)!=1 || selector.Select(150,1,available)!=0 ||
            selector.Select(100,0,available)!=0 || selector.Select(100,1,available)!=1 ||
            selector.Select(0,-1,available)!=1 || selector.Select(1000,1,available)!=0 ||
            selector.Select(250,1,available,distanceScale:2)!=0 ||
            selector.Select(115,1,available,upperExtension:20)!=1 ||
            selector.Select(50,1,new[]{true,false})!=0 || selector.Select(50,-1,new[]{false,false})!=-1)
            throw new Exception("Detail selector thresholds, ties, scale, extension or availability mismatch.");
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(()=>selector.Select(float.NaN,-1,available));
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(()=>selector.Select(-1,-1,available));
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(()=>selector.Select(0,2,available));
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(()=>selector.Select(0,0,Array.Empty<bool>()));
        byte[] detailBytes=Fixture("0007");
        Dictionary<string,byte[]> resources=new(){["appearance/synthetic.lod"]=detailBytes,
            ["appearance/mesh/far.msh"]=MeshScenarios.Mesh("0001"),["appearance/mesh/near.msh"]=MeshScenarios.Mesh("0001",declaration:0x55405),
            ["shader/synthetic.sht"]=MaterialScenarios.Shader(),["effect/synthetic.eft"]=MaterialScenarios.Effect(),
            ["texture/synthetic.dds"]=DdsScenarios.Fixture("DXT1",4,4,1,new byte[8])};
        Dictionary<string,int> reads=new();
        TreLookupResult Resolve(string path)
        {reads[path]=reads.GetValueOrDefault(path)+1;return resources.TryGetValue(path,out byte[]? bytes)?new(TreLookupKind.Present,bytes,3):new(TreLookupKind.Missing);}
        string identity=Convert.ToHexString(SHA256.HashData(detailBytes));
        TexturedDetailAppearance bundle=TexturedDetailAppearanceReader.Load(Resolve,"appearance/synthetic.lod",identity);
        if(bundle.Children.Count!=2 || bundle.Sources.Count!=6 || reads.Values.Any(count=>count!=1))
            throw new Exception("Detail dependencies were not cached by logical identity.");
        Reject(()=>TexturedDetailAppearanceReader.Load(Resolve,"appearance/synthetic.lod",new string('0',64)));
        resources.Remove("appearance/mesh/near.msh");Reject(()=>TexturedDetailAppearanceReader.Load(Resolve,"appearance/synthetic.lod",identity));
        Reject(()=>TexturedDetailAppearanceReader.Load(path=>path=="appearance/mesh/far.msh"?new(TreLookupKind.Masked):Resolve(path),"appearance/synthetic.lod",identity));
        Console.WriteLine("PASS: owned detail bundle shared dependency cache, identity, missing and masked resource gates");
        Console.WriteLine("PASS: detail selector boundary ties, coarse/fine search, scale, extension and available-neighbor fallback");
        Console.WriteLine("PASS: owned DTLA 0005-0007 bands, ID child association, policy geometry, irregularities and malformed fixtures");
    }

    internal static void ProbeOptional(string[] args)
    {
        if(args.Length != 8 || args[2] != "--configuration" || args[4] != "--member" || args[6] != "--expected-sha256")
            throw new ArgumentException("Invalid detail probe arguments.");
        using StreamReader config = File.OpenText(args[3]);
        using ConfiguredTreOverlay overlay = ConfiguredTreOverlay.Open(args[1],config);
        if(args[0]=="--detail-bundle")
        {
            TexturedDetailAppearance asset=TexturedDetailAppearanceReader.Load(overlay.Lookup,args[5],args[7]);
            foreach((string path,AssetSourceIdentity source) in asset.Sources)
                Console.WriteLine($"appearance_source={path} priority={source.Priority} bytes={source.Bytes} sha256={source.Sha256}");
            foreach((string path,TexturedStaticMesh child) in asset.Children)
                Console.WriteLine($"child_bundle={path} vertices={child.Mesh.VertexCount} triangles={child.Mesh.TriangleCount} textures={child.TextureCount}");
            Console.WriteLine($"PASS: owned appearance bundle levels={asset.Description.Levels.Count} sources={asset.Sources.Count}");return;
        }
        TreLookupResult selected = overlay.Lookup(args[5]);
        if(selected.Kind != TreLookupKind.Present || selected.Payload is null) throw new InvalidDataException("Missing detail asset.");
        string hash = Convert.ToHexString(SHA256.HashData(selected.Payload));
        if(!hash.Equals(args[7],StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Detail identity mismatch.");
        DetailAppearanceDescription detail = DetailAppearanceDecoder.Decode(selected.Payload);
        Console.WriteLine($"detail={args[5]} priority={selected.Priority} bytes={selected.Payload.Length} sha256={hash} version={detail.Version} pivot={detail.Pivot}");
        foreach(DetailAppearanceLevel level in detail.Levels)
        {
            Console.WriteLine(FormattableString.Invariant($"level_id={level.Id} lower={level.LowerDistance:R} upper={level.UpperDistance:R} child={level.ChildPath}"));
            if(level.ChildPath is null) continue;
            TreLookupResult child = overlay.Lookup(level.ChildPath);
            if(child.Kind != TreLookupKind.Present || child.Payload is null) {Console.WriteLine($"child_kind={child.Kind}");continue;}
            string childHash = Convert.ToHexString(SHA256.HashData(child.Payload));
            StaticMeshDescription mesh = StaticMeshDecoder.Decode(child.Payload);
            Console.WriteLine($"child_mesh={level.ChildPath} priority={child.Priority} sha256={childHash} vertices={mesh.VertexCount} triangles={mesh.TriangleCount} batches={mesh.Primitives.Count}");
            Console.WriteLine($"child_geometry={level.ChildPath} sha256={Stardust.Core.Proof.StaticMeshVerification.ComputeGeometrySha256(mesh)}");
        }
        foreach((string tag,DetailAppearancePolicy policy) in detail.Policies)
            Console.WriteLine($"policy={tag} flags={policy.Flags} vertices={policy.Geometry?.Vertices.Count ?? 0} indices={policy.Geometry?.Indices.Count ?? 0}");
        Console.WriteLine($"PASS: original owned detail levels={detail.Levels.Count} diagnostics={detail.Diagnostics.Count}");
    }

    internal static byte[] Fixture(string version, bool duplicate=false,bool reversed=false,bool unknownId=false,bool badIndex=false,bool nonfinite=false,byte[]? appearanceBase=null)
    {
        byte[] bands=new byte[24];
        BinaryPrimitives.WriteUInt32LittleEndian(bands,42); Float(bands,4,reversed?201:100);Float(bands,8,nonfinite?float.NaN:200);
        BinaryPrimitives.WriteUInt32LittleEndian(bands.AsSpan(12),7);Float(bands,16,0);Float(bands,20,100);
        byte[] Child(uint id,string path) => Chunk("CHLD",Word(id).Concat(Encoding.ASCII.GetBytes(path+"\0")).ToArray());
        List<byte[]> children=new(){appearanceBase??Form("APPR",Form("0003",Chunk("ODD ",new byte[]{1})))};
        if(version!="0005") children.Add(Chunk("PIVT",new byte[]{1}));
        children.Add(Chunk("INFO",bands));
        List<byte[]> refs=new(){Child(42,"mesh/far.msh"),Child(unknownId?9u:7u,"mesh/near.msh")};
        if(duplicate) refs.Add(Child(42,"mesh/replacement.msh"));
        children.Add(Form("DATA",refs.ToArray()));
        byte[] vertices=new byte[36];Float(vertices,12,1);Float(vertices,28,1);
        byte[] indices=Word(0).Concat(Word(1)).Concat(Word(badIndex?3u:2u)).ToArray();
        if(version=="0007") children.Add(Form("RADR",Chunk("INFO",Word(0))));
        children.Add(Form("TEST",Chunk("INFO",Word(0))));
        children.Add(Form("WRIT",Chunk("INFO",Word(1)),Form("IDTL",Form("0000",Chunk("VERT",vertices),Chunk("INDX",indices)))));
        return Form("DTLA",Form(version,children.ToArray()));
    }
    private static void Float(byte[] bytes,int offset,float value)=>BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset),BitConverter.SingleToInt32Bits(value));
    private static byte[] Word(uint value){byte[] bytes=new byte[4];BinaryPrimitives.WriteUInt32LittleEndian(bytes,value);return bytes;}
    private static byte[] Chunk(string tag,byte[] bytes)=>MeshScenarios.Chunk(tag,bytes);
    private static byte[] Form(string tag,params byte[][] children)=>MeshScenarios.Form(tag,children);
    private static void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
}
