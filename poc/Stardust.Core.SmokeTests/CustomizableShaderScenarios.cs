using System.Security.Cryptography;
using System.Text;
using Stardust.Core.Assets;
using Stardust.Core.Proof;

internal static class CustomizableShaderScenarios
{
    internal static void Run()
    {
        byte[] source=Fixture();var shader=CustomizableShaderDecoder.Decode(source);
        if(shader.TextureChoices.Count!=2 || shader.TextureSelections.Single()!=new ShaderTextureSelection(0x4D41494E,0,2,"ExactCaseVariable",2,1) || shader.PaletteFactors.Single()!=new ShaderPaletteFactor("ExactCaseColor",3,0x4D41494E,"palette/synthetic.pal",58))throw new Exception("Customization fields/order mismatch.");
        string hash=CustomizableShaderVerification.ComputeSha256(shader);Array.Clear(source);
        if(CustomizableShaderVerification.ComputeSha256(shader)!=hash || shader.InlineShader.EffectPath!="effect/synthetic.eft")throw new Exception("Customization description borrowed source.");
        source=Fixture();for(int length=0;length<source.Length;length++)Reject(()=>CustomizableShaderDecoder.Decode(source.AsSpan(0,length)));
        Reject(()=>CustomizableShaderDecoder.Decode(Fixture(first:-1)));Reject(()=>CustomizableShaderDecoder.Decode(Fixture(count:3)));
        Reject(()=>CustomizableShaderDecoder.Decode(Fixture(version:"0000")));Reject(()=>CustomizableShaderDecoder.Decode(Fixture(unknown:true)));
        var raw=CustomizableShaderDecoder.Decode(Fixture(authoredDefault:-7));if(raw.TextureSelections[0].AuthoredDefault!=-7)throw new Exception("Authored customization default was clamped during decoding.");
        if(CustomizableShaderTextureQuery.Select(raw).Single().AssetPath!="texture/choice0.dds")throw new Exception("Negative authored default did not clamp only at selection.");
        var values=new Dictionary<ShaderCustomizationKey,int>{{new("ExactCaseVariable",true),int.MaxValue},{new("ExactCaseVariable",false),0}};
        var selected=CustomizableShaderTextureQuery.Select(shader,values);values[new("ExactCaseVariable",true)]=0;
        if(selected.Single().RequestedValue!=int.MaxValue || selected.Single().ClampedValue!=1 || selected.Single().AssetPath!="texture/choice1.dds" || selected.Single().UsedAuthoredDefault)throw new Exception("Namespace/clamp/owned selection mismatch.");
        selected=CustomizableShaderTextureQuery.Select(shader,new Dictionary<ShaderCustomizationKey,int>{{new("exactcasevariable",true),0}});
        if(!selected.Single().UsedAuthoredDefault || selected.Single().ClampedValue!=1)throw new Exception("Customization variable matching lost exact case.");
        byte[] mesh=MeshScenarios.Mesh("0001");string meshPin=Convert.ToHexString(SHA256.HashData(mesh));
        var inputs=new Dictionary<string,byte[]>{{"appearance/test.msh",mesh},{"shader/synthetic.sht",Fixture()},{"effect/synthetic.eft",MaterialScenarios.Effect()},{"texture/choice1.dds",DdsScenarios.Fixture("DXT1",4,4,1,new byte[8])}};
        var reads=new List<string>();TreLookupResult Resolve(string path){reads.Add(path);return inputs.TryGetValue(path,out var bytes)?new(TreLookupKind.Present,bytes,1):new(TreLookupKind.Missing);}
        Reject(()=>TexturedStaticMeshReader.Load(Resolve,"appearance/test.msh",meshPin));reads.Clear();
        var bundle=TexturedStaticMeshReader.Load(Resolve,"appearance/test.msh",meshPin,CustomizableShaderInspectionMode.AuthoredTextureDefaults);
        if(bundle.Materials.Values.Single().Customization is null || bundle.Textures.Keys.Single()!="texture/choice1.dds" || reads.Contains("texture/choice0.dds") || bundle.Sources.Count!=4)throw new Exception("Authored selection dependency scope mismatch.");
        inputs.Remove("texture/choice1.dds");Reject(()=>TexturedStaticMeshReader.Load(Resolve,"appearance/test.msh",meshPin,CustomizableShaderInspectionMode.AuthoredTextureDefaults));
        Console.WriteLine("PASS: owned CSHD0001 inline SSHT, exact variable case, signed ranges/defaults, raw flags and malformed records");
    }
    internal static byte[] Fixture(short first=0,short count=2,short authoredDefault=1,string version="0001",bool unknown=false)
    {
        byte[] TextureRecord(){using MemoryStream s=new();using BinaryWriter w=new(s);w.Write(0x4D41494Eu);w.Write(first);w.Write(count);w.Write(Encoding.ASCII.GetBytes("ExactCaseVariable\0"));w.Write((byte)2);w.Write(authoredDefault);return s.ToArray();}
        byte[] PaletteRecord(){using MemoryStream s=new();using BinaryWriter w=new(s);w.Write(Encoding.ASCII.GetBytes("ExactCaseColor\0"));w.Write((byte)3);w.Write(0x4D41494Eu);w.Write(Encoding.ASCII.GetBytes("palette/synthetic.pal\0"));w.Write(58);return s.ToArray();}
        byte[] choices=new byte[]{2,0}.Concat(Encoding.ASCII.GetBytes("texture/choice0.dds\0texture/choice1.dds\0")).ToArray();
        return MeshScenarios.Form("CSHD",MeshScenarios.Form(version,MaterialScenarios.Shader(),
            MeshScenarios.Form("TXTR",MeshScenarios.Chunk("DATA",choices),MeshScenarios.Form("CUST",MeshScenarios.Chunk(unknown?"TX2D":"TX1D",TextureRecord()))),
            MeshScenarios.Form("TFAC",MeshScenarios.Chunk("PAL ",PaletteRecord()))));
    }
    internal static void ProbeOptional(string[] args)
    {
        if(args.Length!=8 || args[2]!="--configuration" || args[4]!="--members" || args[6]!="--expected-members-sha256")throw new ArgumentException("Customization inventory needs pinned member list.");
        if(new FileInfo(args[5]).Length>1_048_576)throw new InvalidDataException("Customization list budget.");
        byte[] bytes=File.ReadAllBytes(args[5]);if(!Convert.ToHexString(SHA256.HashData(bytes)).Equals(args[7],StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Customization list identity mismatch.");
        string[] members=File.ReadAllLines(args[5]);if(members.Length is 0 or >10_000 || members.Distinct(StringComparer.Ordinal).Count()!=members.Length)throw new InvalidDataException("Customization list cardinality.");
        using StreamReader config=File.OpenText(args[3]);using ConfiguredTreOverlay overlay=ConfiguredTreOverlay.Open(args[1],config);
        foreach(string member in members)
        {
            var selected=overlay.Lookup(member);if(selected.Kind!=TreLookupKind.Present || selected.Payload is null)throw new InvalidDataException("Customization member missing/masked.");
            var shader=CustomizableShaderDecoder.Decode(selected.Payload);
            Console.WriteLine($"cshd_member={member} priority={selected.Priority} sha256={Convert.ToHexString(SHA256.HashData(selected.Payload))} version={shader.Version} choices={shader.TextureChoices.Count} selections={shader.TextureSelections.Count} factors={shader.PaletteFactors.Count} records_sha256={CustomizableShaderVerification.ComputeSha256(shader)} inline_sha256={Convert.ToHexString(SHA256.HashData(shader.InlineShader.Metadata.Payload.Span))}");
            Console.WriteLine($"cshd_selection={member} defaults_sha256={CustomizableShaderVerification.ComputeSelectionsSha256(shader)} minimum_sha256={CustomizableShaderVerification.ComputeSelectionsSha256(shader,int.MinValue)} maximum_sha256={CustomizableShaderVerification.ComputeSelectionsSha256(shader,int.MaxValue)}");
            if(args[0]=="--cshd-palette-list")
            {
                var bundle=CustomizableShaderPaletteBundleReader.Load(overlay.Lookup,member,Convert.ToHexString(SHA256.HashData(selected.Payload)));
                Console.WriteLine($"cshd_palette={member} sources={bundle.Sources.Count} palettes={bundle.Palettes.Count} missing={bundle.MissingPalettePaths.Count} factors={bundle.AuthoredFactors.Count} defaults_sha256={PaletteVerification.ComputeFactorsSha256(bundle)} minimum_sha256={PaletteVerification.ComputeFactorsSha256(bundle,int.MinValue)} maximum_sha256={PaletteVerification.ComputeFactorsSha256(bundle,int.MaxValue)}");
            }
        }
        Console.WriteLine($"PASS cshd_inventory members={members.Length}");
    }
    private static void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
}
