using System.Buffers.Binary;
using System.Security.Cryptography;
using Stardust.Core.Assets;
using Stardust.Core.Proof;

internal static class PaletteScenarios
{
    internal static void Run()
    {
        byte[] source=Fixture(new byte[]{0x12,0x34,0x56,0x80,0xA1,0xB2,0xC3,0x7F},trailing:17);var palette=PaletteDecoder.Decode(source);
        if(palette.Entries[0].OpaqueArgb!=0xFF123456 || palette.Entries[1].RawFlags!=0x7F || palette.IgnoredTrailingBytes.Length!=17)throw new Exception("Palette channel/flag/trailing mismatch.");
        string hash=PaletteVerification.ComputeSha256(palette);Array.Clear(source);if(PaletteVerification.ComputeSha256(palette)!=hash)throw new Exception("Palette borrowed source.");
        foreach(int index in new[]{int.MinValue,-1,2,int.MaxValue})if(PaletteDecoder.Select(palette,index)?.SelectedIndex!=0)throw new Exception("Palette invalid index did not fall back to zero.");
        if(PaletteDecoder.Select(palette,1)?.OpaqueArgb!=0xFFA1B2C3 || PaletteDecoder.Select(PaletteDecoder.CreateMissingFallback(),58)?.OpaqueArgb!=0xFF00FFFF)throw new Exception("Palette valid/fallback color mismatch.");
        if(PaletteDecoder.Select(PaletteDecoder.Decode(Fixture(Array.Empty<byte>())),0) is not null)throw new Exception("Empty palette invented a color.");
        if(PaletteDecoder.Decode(Fixture(new byte[4],version:0x0307)).Diagnostics.Count!=1)throw new Exception("Palette low-version warning was lost.");
        if(PaletteDecoder.Decode(Fixture(new byte[4096])).Entries.Count!=1024)throw new Exception("Maximum palette count rejected.");
        source=Fixture(new byte[8]);for(int length=0;length<source.Length;length++)Reject(()=>PaletteDecoder.Decode(source.AsSpan(0,length)));
        Reject(()=>PaletteDecoder.Decode(Fixture(new byte[4100])));Reject(()=>PaletteDecoder.Decode(Fixture(new byte[4],version:0x0200)));
        foreach(int offset in new[]{0,8,12}){byte[] bad=Fixture(new byte[4]);bad[offset]=0;Reject(()=>PaletteDecoder.Decode(bad));}
        foreach(int offset in new[]{4,16,22}){byte[] bad=Fixture(new byte[4]);bad[offset]^=1;Reject(()=>PaletteDecoder.Decode(bad));}
        FactorQueries();
        Console.WriteLine("PASS: owned RIFF PAL records, opaque ARGB/raw flags, tolerated trailing/low-version warning, zero-index policy and cyan fallback");
    }
    private static void FactorQueries()
    {
        byte[] shaderBytes=CustomizableShaderScenarios.Fixture();var shader=CustomizableShaderDecoder.Decode(shaderBytes);
        var palette=PaletteDecoder.Decode(Fixture(new byte[]{0x12,0x34,0x56,0x80,0xA1,0xB2,0xC3,0x7F}));
        var values=new Dictionary<ShaderCustomizationKey,int>{{new("ExactCaseColor",true),1}};
        var selected=CustomizableShaderPaletteQuery.Select(shader,_=>palette,values).Single();values[new("ExactCaseColor",true)]=0;
        if(selected.Color?.OpaqueArgb!=0xFFA1B2C3 || selected.RequestedIndex!=1 || selected.UsedAuthoredDefault || selected.MissingPalette)throw new Exception("Palette factor override/ownership mismatch.");
        foreach(var key in new[]{new ShaderCustomizationKey("exactcasecolor",true),new ShaderCustomizationKey("ExactCaseColor",false)})
        {
            selected=CustomizableShaderPaletteQuery.Select(shader,_=>palette,new Dictionary<ShaderCustomizationKey,int>{{key,1}}).Single();
            if(selected.RequestedIndex!=58 || selected.Color?.SelectedIndex!=0 || !selected.UsedAuthoredDefault)throw new Exception("Palette factor namespace/default policy mismatch.");
        }
        int reads=0;var duplicate=shader with {PaletteFactors=Array.AsReadOnly(new[]{shader.PaletteFactors[0],shader.PaletteFactors[0]})};
        if(CustomizableShaderPaletteQuery.Select(duplicate,_=>{reads++;return null;}).Any(f=>!f.MissingPalette || f.Color?.OpaqueArgb!=0xFF00FFFF) || reads!=1)throw new Exception("Palette factor missing deduplication mismatch.");
        string pin=Convert.ToHexString(SHA256.HashData(shaderBytes));byte[] paletteBytes=Fixture(new byte[]{0x12,0x34,0x56,0x80});
        var inputs=new Dictionary<string,byte[]>{{"shader/test.sht",shaderBytes},{"palette/synthetic.pal",paletteBytes}};
        var paths=new List<string>();TreLookupKind absent=TreLookupKind.Missing;
        TreLookupResult Resolve(string path){paths.Add(path);return inputs.TryGetValue(path,out var bytes)?new(TreLookupKind.Present,bytes,1):new(absent);}
        var bundle=CustomizableShaderPaletteBundleReader.Load(Resolve,"shader/test.sht",pin);
        if(bundle.Sources.Count!=2 || bundle.Palettes.Count!=1 || bundle.AuthoredFactors.Single().Color?.OpaqueArgb!=0xFF123456 || paths.Count!=2 || bundle.Diagnostics.Count!=1)throw new Exception("Palette factor dependency scope mismatch.");
        Reject(()=>CustomizableShaderPaletteBundleReader.Load(Resolve,"shader/test.sht",new string('0',64)));
        HostAccountingScenarios.Reject<IOException>(()=>CustomizableShaderPaletteBundleReader.Load(Resolve,"shader/test.sht",pin,new AppearanceGraphLimits(MaximumResources:1)));
        inputs.Remove("palette/synthetic.pal");
        foreach(var kind in new[]{TreLookupKind.Missing,TreLookupKind.Masked})
        {
            absent=kind;bundle=CustomizableShaderPaletteBundleReader.Load(Resolve,"shader/test.sht",pin);
            if(bundle.Sources.Count!=1 || bundle.MissingPalettePaths.Count!=1 || !bundle.AuthoredFactors.Single().MissingPalette || bundle.AuthoredFactors.Single().Color?.OpaqueArgb!=0xFF00FFFF)throw new Exception("Missing/masked palette policy mismatch.");
        }
        inputs["palette/synthetic.pal"]=new byte[24];Reject(()=>CustomizableShaderPaletteBundleReader.Load(Resolve,"shader/test.sht",pin));
        inputs["palette/synthetic.pal"]=Fixture(Array.Empty<byte>());bundle=CustomizableShaderPaletteBundleReader.Load(Resolve,"shader/test.sht",pin);
        if(bundle.AuthoredFactors.Single().Color is not null || bundle.AuthoredFactors.Single().MissingPalette || !bundle.Diagnostics.Any(d=>d.StartsWith("EmptyPalette:")))throw new Exception("Empty palette invented a fallback.");
        inputs.Remove("shader/test.sht");Reject(()=>CustomizableShaderPaletteBundleReader.Load(Resolve,"shader/test.sht",pin));
        Console.WriteLine("PASS: palette factor exact namespaces, supplied/authored values, zero fallback, shared dependencies, missing/masked/empty/malformed distinctions and budgets");
    }
    internal static byte[] Fixture(byte[] entries,ushort version=0x0300,int trailing=0)
    {
        byte[] bytes=new byte[24+entries.Length+trailing];"RIFF"u8.CopyTo(bytes);"PAL data"u8.CopyTo(bytes.AsSpan(8));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4),(uint)(16+entries.Length));BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16),(uint)(4+entries.Length));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(20),version);BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(22),(ushort)(entries.Length/4));entries.CopyTo(bytes,24);
        bytes.AsSpan(24+entries.Length).Fill(0xAB);return bytes;
    }
    internal static void ProbeOptional(string[] args)
    {
        if(args.Length!=8 || args[2]!="--configuration" || args[4]!="--members" || args[6]!="--expected-members-sha256")throw new ArgumentException("Palette probe needs pinned member list.");
        if(new FileInfo(args[5]).Length>1_048_576)throw new InvalidDataException("Palette member-list budget.");
        byte[] list=File.ReadAllBytes(args[5]);if(!Convert.ToHexString(SHA256.HashData(list)).Equals(args[7],StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Palette member-list pin mismatch.");
        string[] members=File.ReadAllLines(args[5]);if(members.Length is 0 or >10_000 || members.Distinct(StringComparer.Ordinal).Count()!=members.Length)throw new InvalidDataException("Palette member-list cardinality.");
        using StreamReader config=File.OpenText(args[3]);using ConfiguredTreOverlay overlay=ConfiguredTreOverlay.Open(args[1],config);
        foreach(string member in members)
        {
            var selected=overlay.Lookup(member);if(selected.Kind!=TreLookupKind.Present || selected.Payload is null)throw new InvalidDataException("Palette member missing/masked.");
            var palette=PaletteDecoder.Decode(selected.Payload);
            Console.WriteLine($"palette_member={member} priority={selected.Priority} sha256={Convert.ToHexString(SHA256.HashData(selected.Payload))} version={palette.Version:X4} entries={palette.Entries.Count} trailing={palette.IgnoredTrailingBytes.Length} diagnostics={palette.Diagnostics.Count} records_sha256={PaletteVerification.ComputeSha256(palette)} selections_sha256={PaletteVerification.ComputeSelectionsSha256(palette)}");
        }
        Console.WriteLine($"PASS palette_inventory members={members.Length}");
    }
    private static void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
}
