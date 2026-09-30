using System.Collections.ObjectModel;
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Stardust.Core.Assets;

public sealed record AssetSourceIdentity(string Path, int? Priority, int Bytes, string Sha256);
public sealed record StaticMeshMaterial(StaticShaderReferenceDescription References, StaticShaderParameters Parameters,
    CustomizableShaderDescription? Customization = null);
public sealed record TexturedStaticMesh(StaticMeshDescription Mesh,
    IReadOnlyDictionary<string, StaticMeshMaterial> Materials, IReadOnlyDictionary<string, DdsImage> Textures,
    IReadOnlyDictionary<string, StaticEffectDescription> Effects, IReadOnlyDictionary<string, AssetSourceIdentity> Sources)
{
    public IReadOnlyDictionary<string,DdsHdrImage> HdrTextures {get;init;} =
        new ReadOnlyDictionary<string,DdsHdrImage>(new Dictionary<string,DdsHdrImage>(StringComparer.Ordinal));
    public int TextureCount=>Textures.Count+HdrTextures.Count;
}

/// <summary>Explicit resolver, owned CPU descriptions, bounded dependency graph; no engine or filesystem discovery.</summary>
public static class TexturedStaticMeshReader
{
    public static TexturedStaticMesh Load(Func<string, TreLookupResult> resolve, string meshPath, string expectedMeshSha256,
        CustomizableShaderInspectionMode customizationMode=CustomizableShaderInspectionMode.Reject)
        =>LoadImplementation(resolve,meshPath,expectedMeshSha256,null,customizationMode);
    internal static TexturedStaticMesh LoadShared(Func<string,TreLookupResult> resolve,string meshPath,string expectedMeshSha256,StaticMeshDecodeCache cache)
        =>LoadImplementation(resolve,meshPath,expectedMeshSha256,cache);
    private static TexturedStaticMesh LoadImplementation(Func<string,TreLookupResult> resolve,string meshPath,string expectedMeshSha256,StaticMeshDecodeCache? cache,
        CustomizableShaderInspectionMode customizationMode=CustomizableShaderInspectionMode.Reject)
    {
        ArgumentNullException.ThrowIfNull(resolve);
        Dictionary<string, AssetSourceIdentity> sources = new(StringComparer.Ordinal);
        Dictionary<string, StaticMeshMaterial> materials = new(StringComparer.Ordinal);
        Dictionary<string, DdsImage> textures = new(StringComparer.Ordinal);
        Dictionary<string,DdsHdrImage> hdrTextures=new(StringComparer.Ordinal);
        Dictionary<string, StaticEffectDescription> effects = new(StringComparer.Ordinal);
        long sourceBytes = 0;
        byte[] Read(string path)
        {
            StaticEffectDecoder.Require(sources.Count < 192, "Static mesh dependency budget exceeded.");
            TreLookupResult selected = resolve(path);
            StaticEffectDecoder.Require(selected.Kind == TreLookupKind.Present && selected.Payload is not null, "Static mesh dependency is missing or masked.");
            byte[] payload = selected.Payload!;
            sourceBytes += payload.Length;
            StaticEffectDecoder.Require(sourceBytes <= 134_217_728, "Static mesh source-byte budget exceeded.");
            sources.Add(path, new(path, selected.Priority, payload.Length, Convert.ToHexString(SHA256.HashData(payload))));
            return payload;
        }
        meshPath = LegacyAssetPath.Normalize(meshPath);
        byte[] meshBytes = Read(meshPath);
        StaticEffectDecoder.Require(sources[meshPath].Sha256.Equals(expectedMeshSha256, StringComparison.OrdinalIgnoreCase), "Static mesh input identity mismatch.");
        StaticMeshDescription mesh = StaticMeshDecoder.Decode(meshBytes);
        long decodedBytes = 0;
        foreach (string shaderPath in mesh.Primitives.Select(primitive => primitive.ShaderPath).Distinct())
        {
            StaticEffectDecoder.Require(materials.Count < 64, "Static mesh material budget exceeded.");
            byte[] shaderBytes=Read(shaderPath);
            StaticMeshMaterial material=cache?.Material(shaderBytes)??DecodeMaterial(shaderBytes,customizationMode);
            StaticShaderReferenceDescription shader=material.References;
            materials.Add(shaderPath,material);
            if (!effects.ContainsKey(shader.EffectPath))
            {
                StaticEffectDecoder.Require(effects.Count < 64, "Static mesh effect budget exceeded.");
                byte[] effectBytes=Read(shader.EffectPath);
                effects.Add(shader.EffectPath,cache?.Effect(effectBytes)??StaticEffectDecoder.Decode(effectBytes));
            }
            foreach (ShaderTextureReference binding in shader.Textures)
            {
                StaticEffectDecoder.Require(binding.AssetPath is not null, "Dynamic or unnamed texture requires another renderer path.");
                if (textures.ContainsKey(binding.AssetPath!) || hdrTextures.ContainsKey(binding.AssetPath!)) continue;
                StaticEffectDecoder.Require(textures.Count+hdrTextures.Count < 64 && decodedBytes <= 134_217_724, "Static mesh texture/decode budget exceeded.");
                byte[] textureBytes=Read(binding.AssetPath!);
                int remaining=(int)(134_217_728-decodedBytes);
                bool hdr=textureBytes.Length>=148 && textureBytes.AsSpan(84,4).SequenceEqual("DX10"u8) &&
                    BinaryPrimitives.ReadUInt32LittleEndian(textureBytes.AsSpan(128)) is 95 or 96;
                if(hdr)
                {
                    StaticEffectDecoder.Require(remaining>=6,"Static mesh HDR decoded budget exceeded.");
                    DdsHdrImage texture=cache?.HdrTexture(textureBytes,remaining)??DdsHdrDecoder.Decode(textureBytes,new(MaximumDecodedBytes:remaining));
                    decodedBytes+=texture.Mips.Sum(mip=>(long)mip.RgbHalf.Length);hdrTextures.Add(binding.AssetPath!,texture);
                }
                else
                {
                    DdsImage texture=cache?.Texture(textureBytes,remaining)??DdsDecoder.Decode(textureBytes,new(MaximumDecodedBytes:remaining));
                    decodedBytes += texture.Mips.Sum(mip => (long)mip.Rgba.Length);textures.Add(binding.AssetPath!, texture);
                }
            }
        }
        return new(mesh, new ReadOnlyDictionary<string,StaticMeshMaterial>(materials),
            new ReadOnlyDictionary<string,DdsImage>(textures), new ReadOnlyDictionary<string,StaticEffectDescription>(effects),
            new ReadOnlyDictionary<string,AssetSourceIdentity>(sources)){HdrTextures=new ReadOnlyDictionary<string,DdsHdrImage>(hdrTextures)};
    }
    internal static StaticMeshMaterial DecodeMaterial(byte[] bytes,CustomizableShaderInspectionMode mode=CustomizableShaderInspectionMode.Reject)
    {
        if(!Enum.IsDefined(mode))throw new ArgumentOutOfRangeException(nameof(mode));
        CustomizableShaderDescription? customization=null;StaticShaderReferenceDescription shader;
        if(mode==CustomizableShaderInspectionMode.AuthoredTextureDefaults && bytes.Length>=12 && bytes.AsSpan(8,4).SequenceEqual("CSHD"u8))
        {customization=CustomizableShaderDecoder.Decode(bytes);shader=CustomizableShaderTextureQuery.ApplyAuthoredDefaults(customization);}
        else shader=StaticShaderReferences.Decode(bytes);
        return new(shader,StaticShaderParameterDecoder.Decode(shader),customization);
    }
}

/// <summary>Per-load immutable decode sharing by content identity, never a global or cross-corpus mutable cache.</summary>
internal sealed class StaticMeshDecodeCache(long maximumDecodedBytes,CustomizableShaderInspectionMode customizationMode=CustomizableShaderInspectionMode.Reject)
{
    private readonly Dictionary<string,StaticMeshMaterial> materials=new(StringComparer.Ordinal);
    private readonly Dictionary<string,StaticEffectDescription> effects=new(StringComparer.Ordinal);
    private readonly Dictionary<string,DdsImage> textures=new(StringComparer.Ordinal);
    private readonly Dictionary<string,DdsHdrImage> hdrTextures=new(StringComparer.Ordinal);
    private long decodedBytes;
    private static string Key(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
    internal StaticMeshMaterial Material(byte[] bytes)
    {string key=Key(bytes);if(materials.TryGetValue(key,out var value))return value;value=TexturedStaticMeshReader.DecodeMaterial(bytes,customizationMode);materials.Add(key,value);return value;}
    internal StaticEffectDescription Effect(byte[] bytes)
    {string key=Key(bytes);if(effects.TryGetValue(key,out var value))return value;value=StaticEffectDecoder.Decode(bytes);effects.Add(key,value);return value;}
    internal DdsImage Texture(byte[] bytes,int localRemaining)
    {
        string key=Key(bytes);
        if(textures.TryGetValue(key,out DdsImage? value))
        {
            if(value.Mips.Sum(mip=>(long)mip.Rgba.Length)>localRemaining)throw new InvalidDataException("Static mesh decoded pixel budget exceeded.");
            return value;
        }
        value=DdsDecoder.Decode(bytes,new(MaximumDecodedBytes:localRemaining));
        long size=value.Mips.Sum(mip=>(long)mip.Rgba.Length);
        if(size>maximumDecodedBytes-decodedBytes)throw new AppearanceGraphBudgetException("Shared appearance decoded pixel budget exceeded.");
        decodedBytes+=size;textures.Add(key,value);return value;
    }
    internal DdsHdrImage HdrTexture(byte[] bytes,int localRemaining)
    {
        string key=Key(bytes);
        if(hdrTextures.TryGetValue(key,out DdsHdrImage? value))
        {
            if(value.Mips.Sum(mip=>(long)mip.RgbHalf.Length)>localRemaining)throw new InvalidDataException("Static mesh HDR decoded pixel budget exceeded.");
            return value;
        }
        value=DdsHdrDecoder.Decode(bytes,new(MaximumDecodedBytes:localRemaining));
        long size=value.Mips.Sum(mip=>(long)mip.RgbHalf.Length);
        if(size>maximumDecodedBytes-decodedBytes)throw new AppearanceGraphBudgetException("Shared appearance decoded pixel budget exceeded.");
        decodedBytes+=size;hdrTextures.Add(key,value);return value;
    }
}
