using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Stardust.Core.Assets;
using Stardust.Core.Proof;

internal static class ShaderOverlayScenarios
{
    internal static void Run()
    {
        foreach (string version in new[] { "0000", "0001", "0002" })
        {
            byte[] fixture = Shader(version);
            StaticShaderReferenceDescription shader = StaticShaderReferences.Decode(fixture);
            if (shader.EffectPath != "effect/synthetic.eft" || shader.Textures.Count != 1 ||
                shader.Textures[0].Tag != 0x4D41494E || shader.Textures[0].SourceFlag != 0 ||
                shader.Textures[0].AssetPath != "texture/synthetic.dds" || shader.Textures[0].Version != version)
                throw new Exception("Synthetic shader reference mismatch.");
            for (int length = 0; length < fixture.Length; length++)
                Reject(() => StaticShaderReferences.Decode(fixture.AsSpan(0, length)));
            Array.Clear(fixture);
            if (shader.Textures[0].BindingMetadata.Span.IndexOf((byte)0x4D) < 0)
                throw new Exception("Shader metadata borrowed input bytes.");
        }
        StaticShaderReferences.Decode(Shader("0001", trailing: 0));
        if (StaticShaderReferences.Decode(Shader("0002", named: false, flag: 1)).Textures[0].AssetPath is not null)
            throw new Exception("Unnamed resource was assigned an invented path.");
        Reject(() => StaticShaderReferences.Decode(Shader("0001", trailing: 1)));
        Reject(() => StaticShaderReferences.Decode(Shader("0000", trailing: 0)));
        Reject(() => StaticShaderReferences.Decode(Shader("0003")));
        Reject(() => StaticShaderReferences.Decode(Shader("0001", duplicate: true)));
        string directory = Path.Combine(Path.GetTempPath(), "stardust-overlay-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllBytes(Path.Combine(directory, "low.tre"), TreAssetScenarios.Build(false,
                new("test/value", new byte[] { 1 }), new("test/mask", new byte[] { 1 })));
            File.WriteAllBytes(Path.Combine(directory, "high.tre"), TreAssetScenarios.Build(true,
                new("test/value", new byte[] { 9 }), new("test/mask", Array.Empty<byte>())));
            const string config = "unrelated=value\nsearchTree_00_1=low.tre\nsearchTree_00_9=high.tre\n";
            ConfiguredTreOverlay overlay = ConfiguredTreOverlay.Open(directory, new StringReader(config));
            if (overlay.MountCount != 2 || overlay.Lookup("TEST/VALUE").Payload!.Single() != 9 ||
                overlay.Lookup("test/value").Priority != 9 || overlay.Lookup("test/mask").Kind != TreLookupKind.Masked ||
                overlay.Lookup("test/missing").Kind != TreLookupKind.Missing)
                throw new Exception("Configured overlay priority/mask mismatch.");
            overlay.Dispose(); overlay.Dispose();
            HostAccountingScenarios.Reject<ObjectDisposedException>(() => overlay.Lookup("test/value"));
            foreach (string invalid in new[] { "", config + "searchTree_00_1=high.tre\n",
                "searchTree_00_1=../low.tre", "searchTree_00_1=C:low.tre", "searchTree_00_1=low.txt",
                "searchTree_00_999999999999=low.tre", new string('x', 65_537) })
                Reject(() => { using var _ = ConfiguredTreOverlay.Open(directory, new StringReader(invalid)); });
            HostAccountingScenarios.Reject<FileNotFoundException>(() =>
                ConfiguredTreOverlay.Open(directory, new StringReader("searchTree_00_9=high.tre\nsearchTree_00_1=missing.tre")));
            // Reopen exclusively after a failed mount: previously opened archive handles must be released.
            using FileStream exclusive = new(Path.Combine(directory, "high.tre"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            File.Delete(Path.Combine(directory, "low.tre"));
            File.Delete(Path.Combine(directory, "high.tre"));
            Directory.Delete(directory);
        }
        Console.WriteLine("PASS: SSHT versioned external references, owned metadata, configured TRE priority/masks and failed-mount cleanup");
    }

    internal static void ProbeOptional(string[] args)
    {
        if (args.Length != 8 || args[0] != "--mesh-overlay" || args[2] != "--configuration" ||
            args[4] != "--member" || args[6] != "--expected-sha256") throw new ArgumentException("Invalid overlay probe arguments.");
        using StreamReader config = File.OpenText(args[3]);
        using ConfiguredTreOverlay overlay = ConfiguredTreOverlay.Open(args[1], config);
        TreLookupResult selected = overlay.Lookup(args[5]);
        byte[] bytes = RequirePayload(selected);
        string hash = Convert.ToHexString(SHA256.HashData(bytes));
        if (!hash.Equals(args[7], StringComparison.OrdinalIgnoreCase)) throw new Exception("Original mesh identity mismatch.");
        StaticMeshDescription mesh = StaticMeshDecoder.Decode(bytes);
        HashSet<string> textures = new(), effects = new();
        int bindings = 0, unnamed = 0;
        foreach (string path in mesh.Primitives.Select(primitive => primitive.ShaderPath).Distinct())
        {
            TreLookupResult lookup = overlay.Lookup(path);
            StaticShaderReferenceDescription shader = StaticShaderReferences.Decode(RequirePayload(lookup));
            effects.Add(shader.EffectPath);
            IffNode effect = IffDocument.Parse(RequirePayload(overlay.Lookup(shader.EffectPath))).Root;
            if (!effect.IsForm("EFCT")) throw new Exception("Shader effect did not resolve to EFCT.");
            foreach (ShaderTextureReference texture in shader.Textures)
            {
                bindings++;
                if (texture.AssetPath is null) { unnamed++; continue; }
                byte[] dds = RequirePayload(overlay.Lookup(texture.AssetPath));
                if (dds.Length < 128 || !dds.AsSpan(0, 4).SequenceEqual("DDS "u8)) throw new Exception("Texture did not resolve to a DDS envelope.");
                textures.Add(texture.AssetPath);
            }
            Console.WriteLine($"shader={path} priority={lookup.Priority} version={shader.Version} bindings={shader.Textures.Count}");
        }
        Console.WriteLine($"PASS: configured mounts={overlay.MountCount} mesh_priority={selected.Priority} mesh_sha256={hash} shaders={mesh.Primitives.Select(p => p.ShaderPath).Distinct().Count()} bindings={bindings} textures={textures.Count} effects={effects.Count} unnamed={unnamed}");
        Console.WriteLine("geometry_sha256=" + StaticMeshVerification.ComputeGeometrySha256(mesh));
        TexturedStaticMesh asset = TexturedStaticMeshReader.Load(overlay.Lookup, args[5], args[7]);
        foreach ((string path, DdsImage image) in asset.Textures)
        {
            Console.WriteLine($"texture={path} format={image.Format} width={image.Width} mips={image.Mips.Count} rgba_sha256={Convert.ToHexString(SHA256.HashData(image.Mips[0].Rgba.Span))}");
            for(int level=0;level<image.Mips.Count;level++)
                Console.WriteLine($"texture_mip={path} level={level} rgba_sha256={Convert.ToHexString(SHA256.HashData(image.Mips[level].Rgba.Span))}");
        }
        foreach ((string path, StaticEffectDescription effect) in asset.Effects)
        {
            Console.WriteLine($"effect={path} implementations={effect.Implementations.Count} precalculated={effect.ContainsPrecalculatedVertexLighting}");
            foreach (StaticEffectImplementation implementation in effect.Implementations)
                foreach (StaticEffectPass pass in implementation.Passes)
                {
                    Console.WriteLine($"pass=0009 depth_test={pass.Fields[0x19]} depth_write={pass.Fields[0x1A]} alpha_test={pass.Fields[0x30]} material_tag={pass.Fields[0x7C]:X8} programmable={pass.PixelProgram is not null} fixed_stages={pass.FixedStages.Count}");
                    foreach(FixedTextureStage stage in pass.FixedStages) Console.WriteLine($"fixed_texture_tag={stage.TextureTag:X8} coordinate_tag={stage.CoordinateTag:X8}");
                }
        }
        foreach((string path,StaticMeshMaterial material) in asset.Materials)
            Console.WriteLine($"binding={path} source_flag={material.References.Textures[0].SourceFlag} uv={material.Parameters.CoordinateSets[0x4D41494E]}");
        foreach(var texture in asset.HdrTextures)
            for(int level=0;level<texture.Value.Mips.Count;level++)Console.WriteLine($"texture_hdr_mip={texture.Key} level={level} rgb_half_sha256={Convert.ToHexString(SHA256.HashData(texture.Value.Mips[level].RgbHalf.Span))}");
        Console.WriteLine($"PASS: owned textured CPU asset materials={asset.Materials.Count} textures={asset.TextureCount} effects={asset.Effects.Count} sources={asset.Sources.Count}");
    }

    private static byte[] RequirePayload(TreLookupResult result) => result.Kind == TreLookupKind.Present ? result.Payload! :
        throw new InvalidDataException("Required logical asset is missing or masked.");
    private static byte[] Shader(string version, byte? trailing = null, bool named = true, byte flag = 0, bool duplicate = false)
    {
        int length = version == "0000" ? 5 : version == "0002" ? 12 : 11;
        byte[] data = new byte[length + (trailing.HasValue ? 1 : 0)];
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(version == "0000" ? 1 : 0), 0x4D41494E);
        data[version == "0000" ? 0 : 4] = flag;
        if (trailing.HasValue) data[^1] = trailing.Value;
        byte[] binding = MeshScenarios.Form("TXM ", MeshScenarios.Form(version,
            named ? new[] { MeshScenarios.Chunk("DATA", data), MeshScenarios.Chunk("NAME", Encoding.ASCII.GetBytes("texture/synthetic.dds\0")) } :
                new[] { MeshScenarios.Chunk("DATA", data) }));
        return MeshScenarios.Form("SSHT", MeshScenarios.Form("0000",
            MeshScenarios.Form("TXMS", duplicate ? new[] { binding, binding } : new[] { binding }),
            MeshScenarios.Chunk("NAME", Encoding.ASCII.GetBytes("synthetic.eft\0"))));
    }
    private static void Reject(Action action) => HostAccountingScenarios.Reject<InvalidDataException>(action);
}
