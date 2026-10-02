using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Stardust.Core.Assets;

internal static class MaterialScenarios
{
    internal static void Run()
    {
        byte[] effectBytes = Effect();
        StaticEffectDescription effect = StaticEffectDecoder.Decode(effectBytes);
        var emptyImplementation=Form("IMPL",Form("0005",Chunk("SCAP",Word(0x300)),Chunk("DATA",new byte[]{0,0x4E,0x49,0x41,0x4D,0,0})));
        var noPass=StaticEffectDecoder.Decode(Form("EFCT",Form("0001",Chunk("DATA",new byte[]{1,0}),emptyImplementation)));
        if(noPass.HasRenderPasses||!effect.HasRenderPasses||(effect with{Implementations=Array.Empty<StaticEffectImplementation>()}).HasRenderPasses)throw new Exception("Zero-pass effect classification failed.");
        if(!(effect with{Implementations=new[]{noPass.Implementations[0],effect.Implementations[0]}}).HasRenderPasses)throw new Exception("Mixed effect candidates classified no-draw.");
        if (effect.Implementations.Count != 2 || effect.ContainsPrecalculatedVertexLighting ||
            effect.Implementations[0].Capabilities.Single() != 0x300 || effect.Implementations[0].Options.Single() != 0x444F5433 ||
            effect.Implementations[0].Passes[0].PixelProgram != "pixel_program/synthetic.psh" ||
            effect.Implementations[0].Passes[0].VertexProgram != "vertex_program/synthetic.vsh" ||
            (effect.Implementations[0].Passes[0].PixelTextures.Single().Stage != 0 || effect.Implementations[0].Passes[0].PixelTextures.Single().TextureTag != 0x4D41494E) ||
            effect.Implementations[1].Passes[0].FixedStages.Single().CoordinateTag != 0x4D41494E ||
            effect.Implementations[1].Passes[0].FixedVertexLighting.Length != 7 ||
            effect.Implementations[0].Passes[0].Fields[0x19] != 1 || effect.Implementations[0].Passes[0].Fields[0x7C] != 0x4D41494E)
            throw new Exception("Effect graph/state/bindings mismatch.");
        for (int length = 0; length < effectBytes.Length; length++) Reject(() => StaticEffectDecoder.Decode(effectBytes.AsSpan(0,length)));
        Array.Clear(effectBytes);
        if (effect.Implementations[0].Passes[0].Metadata.FormType != "PASS") throw new Exception("Effect borrowed caller memory.");
        byte[] shaderBytes = Shader();
        StaticShaderParameters parameters = StaticShaderParameterDecoder.Decode(StaticShaderReferences.Decode(shaderBytes));
        if (parameters.Materials[0x4D41494E][16] != 16 || parameters.CoordinateSets[0x4D41494E] != 0)
            throw new Exception("Material scalars/coordinate map mismatch.");
        Reject(() => StaticShaderParameterDecoder.Decode(StaticShaderReferences.Decode(Shader(nan:true))));
        Reject(() => StaticShaderParameterDecoder.Decode(StaticShaderReferences.Decode(Shader(coordinate:16))));
        Reject(() => StaticShaderParameterDecoder.Decode(StaticShaderReferences.Decode(Shader(duplicate:true))));
        Reject(() => StaticEffectDecoder.Decode(Effect(passVersion:"0010")));
        Reject(() => StaticEffectDecoder.Decode(Effect(effectCount:3)));
        Reject(() => StaticEffectDecoder.Decode(Effect(stageIndex:8)));
        StaticEffectDescription older = StaticEffectDecoder.Decode(Effect(passVersion:"0008",implementationVersion:"0004"));
        if(older.Implementations[0].Flag5 is not null || older.Implementations[0].Version!="0004" || older.Implementations[0].Passes[0].Version!="0008" || older.Implementations[0].Passes[0].Fields.ContainsKey(0x4D) || older.Implementations[0].Passes[0].Fields.ContainsKey(0x64) || older.Implementations[0].Passes[0].Fields[0x7C]!=0x4D41494E)throw new Exception("Older effect field absence/order mismatch.");
        foreach(string version in new[]{"0008","0009"})
        {
            var raw=StaticEffectDecoder.Decode(Effect(passVersion:version,implementationVersion:"0004",asymmetric:true)).Implementations[0].Passes[0].Fields;
            if(raw[0x4C]!=30 || raw[0x50]!=0x23222120 || raw[0x60]!=39 || raw[0x74]!=0x2F2E2D2C || raw[0x78]!=0x33323130 || (version=="0009" && (raw[0x4D]!=31 || raw[0x64]!=40 || raw[0x70]!=43)))throw new Exception("Asymmetric older/current pass field offsets differ.");
        }
        byte[] legacyBytes=Effect(passVersion:"0007",implementationVersion:"0003",asymmetric:true);
        var legacy=StaticEffectDecoder.Decode(legacyBytes).Implementations[0];
        if(legacy.Capabilities.Count!=0 || legacy.Options.Single()!=0x444F5433 || legacy.Flag5 is not null || legacy.Passes[0].Fields.ContainsKey(0x48) || legacy.Passes[0].Fields[0x40]!=0x15141312 || legacy.Passes[0].Fields[0x50]!=0x23222120)throw new Exception("Legacy pass/implementation layout mismatch.");
        for(int length=0;length<legacyBytes.Length;length++)Reject(()=>StaticEffectDecoder.Decode(legacyBytes.AsSpan(0,length)));
        var oldTexture=StaticEffectDecoder.Decode(Effect(passVersion:"0007",implementationVersion:"0003",legacyTexture:true)).Implementations[0].Passes[0].PixelTextures.Single();
        if(oldTexture.Version!="0000" || oldTexture.Metadata.Length!=16 || oldTexture.Stage!=0 || oldTexture.TextureTag!=0x4D41494E)throw new Exception("Legacy texture map metadata lost.");
        Reject(()=>StaticEffectDecoder.Decode(Effect(passVersion:"0007",implementationVersion:"0003",legacyTexture:true,legacyTextureLength:15)));
        Reject(()=>StaticEffectDecoder.Decode(Effect(passVersion:"0007",implementationVersion:"0003",legacyTexture:true,bindingCount:2,duplicateStage:true)));
        byte[] inlineShader=Form("SSHT",Form("0000",legacyBytes));
        Reject(()=>StaticShaderReferences.Decode(inlineShader));
        var inline=StaticShaderReferences.DecodeIncludingInlineEffect(inlineShader);
        Array.Clear(inlineShader);
        if(inline.EffectPath.Length!=0 || inline.InlineEffect is null || inline.InlineEffect.FormType!="EFCT")throw new Exception("Inline effect ownership/discriminator mismatch.");
        Reject(()=>StaticShaderReferences.DecodeIncludingInlineEffect(Form("SSHT",Form("0000",legacyBytes,Chunk("NAME",Name("synthetic.eft"))))));
        byte[] olderBytes=Effect(passVersion:"0008",implementationVersion:"0004");
        foreach(string implementationVersion in new[]{"0006","0007"})
        {
            byte[] versioned=Effect(implementationVersion:implementationVersion,asymmetric:true);
            var candidate=StaticEffectDecoder.Decode(versioned).Implementations[0];
            if(candidate.Version!=implementationVersion||candidate.Flag5!=1||candidate.Flag6!=1||candidate.Passes[0].Fields[0x74]!=0x2F2E2D2C)throw new Exception("Later inline implementation metadata/field preservation failed.");
            for(int length=0;length<versioned.Length;length++)Reject(()=>StaticEffectDecoder.Decode(versioned.AsSpan(0,length)));
        }
        Reject(()=>StaticEffectDecoder.Decode(Effect(implementationVersion:"0008")));
        Reject(()=>StaticEffectDecoder.Decode(Effect(implementationVersion:"0009")));
        for(int length=0;length<olderBytes.Length;length++)Reject(()=>StaticEffectDecoder.Decode(olderBytes.AsSpan(0,length)));
        StaticEffectDescription multi = StaticEffectDecoder.Decode(Effect(bindingCount:4));
        if(multi.Implementations[0].Passes[0].PixelTextures.Count != 4 || multi.Implementations[0].Passes[0].PixelTextures[3].Stage != 3)
            throw new Exception("Repeated PTXM records were lost.");
        Reject(()=>StaticEffectDecoder.Decode(Effect(bindingCount:2,duplicateStage:true)));
        Reject(()=>StaticEffectDecoder.Decode(Effect(bindingCount:9)));
        byte[] mesh = MeshScenarios.Mesh("0000");
        string meshHash = Convert.ToHexString(SHA256.HashData(mesh));
        Dictionary<string, byte[]> input = new() { ["appearance/synthetic.msh"]=mesh,
            ["shader/synthetic.sht"]=shaderBytes,["effect/synthetic.eft"]=Effect(),
            ["texture/synthetic.dds"]=DdsScenarios.Fixture("DXT1",4,4,1,new byte[8]) };
        TreLookupResult Resolve(string path) => input.TryGetValue(path,out byte[]? bytes) ? new(TreLookupKind.Present,bytes,7) : new(TreLookupKind.Missing);
        TexturedStaticMesh asset = TexturedStaticMeshReader.Load(Resolve,"appearance/synthetic.msh",meshHash);
        if (asset.Sources.Count != 4 || asset.Materials.Count != 1 || asset.Textures.Count != 1 || asset.Effects.Count != 1)
            throw new Exception("Owned asset dependency graph mismatch.");
        Reject(() => TexturedStaticMeshReader.Load(Resolve,"appearance/synthetic.msh",new string('0',64)));
        input.Remove("texture/synthetic.dds"); Reject(() => TexturedStaticMeshReader.Load(Resolve,"appearance/synthetic.msh",meshHash));
        Reject(() => TexturedStaticMeshReader.Load(path => path=="texture/synthetic.dds" ? new(TreLookupKind.Masked) : Resolve(path),"appearance/synthetic.msh",meshHash));
        Console.WriteLine("PASS: ordered effect candidates, PASS fields/program/fixed bindings, material scalars, coordinates and owned asset graph negatives");
    }
    internal static void ProbeInline(string[] args)
    {
        if(args.Length!=3)throw new ArgumentException("Expected --inline-effect <shader-path> <sha256>.");
        byte[] bytes=File.ReadAllBytes(args[1]);
        if(bytes.Length>1024*1024 || Convert.ToHexString(SHA256.HashData(bytes))!=args[2].ToUpperInvariant())throw new InvalidDataException("Inline shader input identity/budget mismatch.");
        var shader=StaticShaderReferences.DecodeIncludingInlineEffect(bytes);
        var effect=StaticEffectDecoder.DecodeInlineEffect(shader);
        if(effect.Implementations.Count!=2 || effect.Implementations.Any(i=>i.Version!="0003" || i.Passes.Count!=1 || i.Passes[0].Version!="0007"))throw new Exception("Original face effect candidates differ.");
        for(int n=0;n<bytes.Length;n++)Reject(()=>StaticShaderReferences.DecodeIncludingInlineEffect(bytes.AsSpan(0,n)));
        Console.WriteLine("INLINE_EFFECT_JSON:"+System.Text.Json.JsonSerializer.Serialize(new{shader.Version,shader.EffectPath,Textures=shader.Textures.Select(t=>new{t.Tag,t.SourceFlag,t.AssetPath,Metadata=Convert.ToHexString(t.BindingMetadata.Span)}),Implementations=effect.Implementations.Select(i=>new{i.Version,i.Capabilities,i.Options,i.PhaseTag,i.Flag5,i.Flag6,Passes=i.Passes.Select(p=>new{p.Version,p.Fields,p.VertexProgram,p.PixelProgram,PixelTextures=p.PixelTextures.Select(t=>new{t.Stage,t.TextureTag,t.Version,Metadata=Convert.ToHexString(t.Metadata.Span)}),FixedStages=p.FixedStages.Select(t=>new{t.TextureTag,t.CoordinateTag,Metadata=Convert.ToHexString(t.Metadata.Span)})})})}));
        Array.Clear(bytes);
        if(shader.Textures.Single().BindingMetadata.Span[0]!=0x4e)throw new Exception("Original inline shader borrowed caller memory.");
        Console.WriteLine("PASS: pinned original inline face shader, both candidates, every truncated prefix and source ownership");
    }
    internal static byte[] Effect(string passVersion="0009", byte effectCount=2, byte stageIndex=0, byte bindingCount=1, bool duplicateStage=false, string implementationVersion="0005", bool asymmetric=false, bool legacyTexture=false, int legacyTextureLength=16)
    {
        byte[] tag=Word(0x4D41494E), implData=new byte[7]; implData[0]=1; tag.CopyTo(implData,1); implData[5]=implData[6]=1;
        if(implementationVersion is "0003" or "0004")implData=implData[..5];
        byte[] Pass(bool fixedPath)
        {
            byte[] data=asymmetric?Enumerable.Range(0,56).Select(i=>(byte)i).ToArray():new byte[56]; data[0]=fixedPath?(byte)1:(byte)0; data[4]=data[5]=1; tag.CopyTo(data,52);
            if(passVersion is "0007" or "0008")data=data.Where((_,i)=>i!=31 && i is not (40 or 41 or 42 or 43) && (passVersion!="0007" || i is not (26 or 27 or 28 or 29))).ToArray();
            byte[] vertex=fixedPath ? Form("PFFP",Chunk("0001",new byte[]{1,0,0,0,0,0,0})) :
                Form("PVSH",Chunk("0000",Name("vertex_program/synthetic.vsh")));
            byte[] stage=new byte[33]; tag.CopyTo(stage,18); tag.CopyTo(stage,22);
            byte[] pixel=fixedPath ? Form("STAG",Chunk("0000",stage)) :
                Form("PPSH",Form("0001",new[]{Chunk("DATA",new byte[]{bindingCount}.Concat(Name("pixel_program/synthetic.psh")).ToArray())}.Concat(Enumerable.Range(0,bindingCount).Select(i=>Form("PTXM",Chunk(legacyTexture?"0000":"0002",new byte[]{duplicateStage?stageIndex:(byte)(stageIndex+i)}.Concat(tag).Concat(legacyTexture?new byte[11]:Array.Empty<byte>()).Take(legacyTexture?legacyTextureLength:5).ToArray())))).ToArray()));
            return Form("PASS",Form(passVersion,Chunk("DATA",data),vertex,pixel));
        }
        return Form("EFCT",Form("0001",Chunk("DATA",new byte[]{effectCount,0}),
            Form("IMPL",Form(implementationVersion,implementationVersion=="0003" ? new[]{Chunk("DATA",implData),Chunk("OPTN",Word(0x444F5433)),Pass(false)} : new[]{Chunk("SCAP",Word(0x300)),Chunk("OPTN",Word(0x444F5433)),Chunk("DATA",implData),Pass(false)})),
            Form("IMPL",Form(implementationVersion,implementationVersion=="0003" ? new[]{Chunk("DATA",implData),Pass(true)} : new[]{Chunk("SCAP",Word(0x100)),Chunk("DATA",implData),Pass(true)}))));
    }
    internal static byte[] Shader(bool nan=false, byte coordinate=0, bool duplicate=false)
    {
        byte[] scalars=new byte[68];
        for(int i=0;i<17;i++) BinaryPrimitives.WriteInt32LittleEndian(scalars.AsSpan(i*4),BitConverter.SingleToInt32Bits(nan && i==0 ? float.NaN : i));
        byte[] tag=Word(0x4D41494E), binding=new byte[11]; tag.CopyTo(binding,0);
        byte[] coordinateRecord=tag.Concat(new byte[]{coordinate}).ToArray();
        return Form("SSHT",Form("0000",Form("MATS",Form("0000",Chunk("TAG ",tag),Chunk("MATL",scalars))),
            Form("TCSS",Chunk("0000",duplicate ? coordinateRecord.Concat(coordinateRecord).ToArray() : coordinateRecord)),
            Form("TXMS",Form("TXM ",Form("0001",Chunk("DATA",binding),Chunk("NAME",Name("texture/synthetic.dds"))))),
            Chunk("NAME",Name("effect/synthetic.eft"))));
    }
    private static byte[] Word(uint value) { byte[] bytes=new byte[4]; BinaryPrimitives.WriteUInt32LittleEndian(bytes,value); return bytes; }
    private static byte[] Name(string value)=>Encoding.ASCII.GetBytes(value+"\0");
    private static byte[] Chunk(string tag,byte[] data)=>MeshScenarios.Chunk(tag,data);
    private static byte[] Form(string type,params byte[][] children)=>MeshScenarios.Form(type,children);
    private static void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
}
