using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
namespace Stardust.Core.Assets;

public enum TerrainSuppliedStaticMaterialPolicy { TextureTimesVertexColor,TextureTimesVertexColorAndAuthoredMainDiffuse }
public sealed record TerrainSuppliedStaticMaterialLimits(int MaximumReads=2,int MaximumShaderBytes=1048576,int MaximumTextureBytes=67108864,
    long MaximumSourceBytes=68157440,int MaximumDimension=8192,int MaximumMips=16,int MaximumDecodedBytes=134217728);

/// <summary>Explicit supplied SSHT MAIN closure. Modern modulation only; no effect execution or family association.</summary>
public sealed class TerrainSuppliedStaticMaterial
{
    public const uint MainTag=0x4D41494E;
    private readonly DdsImage texture;
    private TerrainSuppliedStaticMaterial(StaticShaderReferenceDescription shader,StaticShaderParameters parameters,IReadOnlyDictionary<uint,uint> factors,
        ShaderTextureReference binding,DdsImage texture,AssetSourceIdentity shaderSource,AssetSourceIdentity textureSource,TerrainSuppliedStaticMaterialPolicy policy,
        Vector4? authoredDiffuse,long sourceBytes,long decodedBytes)
    {Shader=shader;Parameters=parameters;Factors=factors;Binding=binding;this.texture=texture;ShaderSource=shaderSource;TextureSource=textureSource;Policy=policy;AuthoredMainDiffuse=authoredDiffuse;SourceBytes=sourceBytes;DecodedBytes=decodedBytes;}
    public StaticShaderReferenceDescription Shader {get;}
    public StaticShaderParameters Parameters {get;}
    public IReadOnlyDictionary<uint,uint> Factors {get;}
    public ShaderTextureReference Binding {get;}
    public AssetSourceIdentity ShaderSource {get;}
    public AssetSourceIdentity TextureSource {get;}
    public TerrainSuppliedStaticMaterialPolicy Policy {get;}
    public Vector4? AuthoredMainDiffuse {get;}
    public Vector4 DiffuseMultiplier=>Policy==TerrainSuppliedStaticMaterialPolicy.TextureTimesVertexColorAndAuthoredMainDiffuse?AuthoredMainDiffuse??Vector4.One:Vector4.One;
    public DdsFormat Format=>texture.Format;
    public DdsColorSpace ColorSpace=>texture.ColorSpace;
    public DdsAlphaMode AlphaMode=>texture.AlphaMode;
    public uint? DxgiFormat=>texture.DxgiFormat;
    public int Width=>texture.Width;
    public int Height=>texture.Height;
    public int MipCount=>texture.Mips.Count;
    public bool IsCompleteMipChain {get{int count=1;for(int dimension=Math.Max(Width,Height);dimension>1;dimension>>=1)count++;return MipCount==count;}}
    public bool IsSingleMipOrCompleteChain=>MipCount==1||IsCompleteMipChain;
    public byte[] CopyMipRgba(int level)=>CopyRgba(level);
    public long SourceBytes {get;}
    public long DecodedBytes {get;}
    public int ExternalReads=>2;
    public bool OriginalTerrainAssociation=>false;
    public bool OriginalShaderExecutionParity=>false;
    public byte[] CopyRgba(int level)=>texture.Mips[level].Rgba.ToArray();
    public DdsImage CopyDecodedTexture()=>texture with{Mips=Array.AsReadOnly(texture.Mips.Select(m=>m with{Rgba=m.Rgba.ToArray()}).ToArray()),RetainedTerminalBlock=texture.RetainedTerminalBlock.ToArray()};
    public Vector4 Modulate(Vector4 mainTexel,Vector4 vertexColor)
    {Color(mainTexel);Color(vertexColor);return mainTexel*vertexColor*DiffuseMultiplier;}
    private static void Color(Vector4 v)
    {if(!float.IsFinite(v.X)||!float.IsFinite(v.Y)||!float.IsFinite(v.Z)||!float.IsFinite(v.W)||v.X<0||v.X>1||v.Y<0||v.Y>1||v.Z<0||v.Z>1||v.W<0||v.W>1)throw new ArgumentOutOfRangeException(nameof(v));}
    private static void Need(bool value,string why){if(!value)throw new InvalidDataException(why);}
    private static void Options(TerrainSuppliedStaticMaterialLimits l)
    {
        if(l.MaximumReads is <0 or >2||l.MaximumShaderBytes is <1 or >16777216||l.MaximumTextureBytes is <128 or >67108864
            ||l.MaximumSourceBytes is <0 or >134217728||l.MaximumDimension is <1 or >32768||l.MaximumMips is <1 or >16||l.MaximumDecodedBytes is <4 or >134217728)
            throw new ArgumentOutOfRangeException(nameof(l));
    }
    private static string Path(string path,string prefix,string suffix)
    {
        ArgumentNullException.ThrowIfNull(path);
        if(path!=LegacyAssetPath.Normalize(path)||!path.StartsWith(prefix,StringComparison.Ordinal)||!path.EndsWith(suffix,StringComparison.Ordinal)
            ||path.Contains(':')||path.Split('/').Any(v=>v.Length==0||v is "." or ".."))throw new ArgumentException("Canonical bounded logical resource path required.",nameof(path));
        return path;
    }
    public static TerrainSuppliedStaticMaterial Load(Func<string,TreLookupResult> resolve,string shaderPath,string shaderSha256,
        TerrainSuppliedStaticMaterialPolicy policy=TerrainSuppliedStaticMaterialPolicy.TextureTimesVertexColor,TerrainSuppliedStaticMaterialLimits? limits=null,CancellationToken cancellation=default)
    {
        ArgumentNullException.ThrowIfNull(resolve);var l=limits??new();Options(l);
        if(!Enum.IsDefined(policy))throw new ArgumentOutOfRangeException(nameof(policy));
        shaderPath=Path(shaderPath,"shader/",".sht");
        if(shaderSha256 is null||shaderSha256.Length!=64||!shaderSha256.All(Uri.IsHexDigit))throw new ArgumentException("Exact SHA-256 required.",nameof(shaderSha256));
        cancellation.ThrowIfCancellationRequested();Need(l.MaximumReads>=2,"Two source reads required before resolution.");
        long sourceBytes=0;int reads=0;
        (byte[] Bytes,AssetSourceIdentity Identity) Read(string path,int maximumBytes)
        {
            cancellation.ThrowIfCancellationRequested();Need(reads<l.MaximumReads,"Read count bound.");reads++;
            var selected=resolve(path);cancellation.ThrowIfCancellationRequested();
            Need(selected.Kind==TreLookupKind.Present&&selected.Payload is not null,"Source missing or tombstoned; no fallback.");
            int length=selected.Payload!.Length;Need(length<=maximumBytes,"Individual source-byte cap before copy.");
            long total=checked(sourceBytes+length);Need(total<=l.MaximumSourceBytes,"Combined source-byte cap before copy.");
            var owned=selected.Payload.ToArray();cancellation.ThrowIfCancellationRequested();sourceBytes=total;
            string digest=Convert.ToHexString(SHA256.HashData(owned));cancellation.ThrowIfCancellationRequested();
            return (owned,new(path,selected.Priority,length,digest));
        }
        var shaderInput=Read(shaderPath,l.MaximumShaderBytes);Need(shaderInput.Identity.Sha256.Equals(shaderSha256,StringComparison.OrdinalIgnoreCase),"Pinned shader identity mismatch.");
        // Frozen parser owns the IFF description. Unsupported wrappers/versions reject without dependency reads.
        var shader=StaticShaderReferences.DecodeIncludingInlineEffect(shaderInput.Bytes);cancellation.ThrowIfCancellationRequested();
        var parameters=StaticShaderParameterDecoder.Decode(shader);var factors=StaticShaderFactors.Decode(shader);
        foreach(var material in parameters.Materials.Values)for(int i=0;i<16;i++)Need(material[i]>=0&&material[i]<=1,"HDR/negative material color unsupported by modern color policy.");
        var candidates=shader.Textures.Where(v=>v.Tag==MainTag).ToArray();Need(candidates.Length==1,"Exactly one authored MAIN binding required.");
        var binding=candidates[0];Need(binding.SourceFlag==0&&binding.AssetPath is not null,"Named static MAIN required; dynamic/absent rejected.");
        string texturePath=Path(binding.AssetPath!,"texture/",".dds");
        Vector4? diffuse=parameters.Materials.TryGetValue(MainTag,out var values)?StaticShaderMaterialDecoder.Decode(values).DiffuseRgba:null;
        var textureInput=Read(texturePath,l.MaximumTextureBytes);Need(textureInput.Bytes.Length>=128,"Truncated DDS header.");
        uint mipCount=Math.Max(1u,BinaryPrimitives.ReadUInt32LittleEndian(textureInput.Bytes.AsSpan(28,4)));Need(mipCount<=l.MaximumMips,"DDS mip cap before decode.");
        cancellation.ThrowIfCancellationRequested();var decoded=DdsDecoder.Decode(textureInput.Bytes,new(l.MaximumDimension,l.MaximumTextureBytes,l.MaximumDecodedBytes));
        cancellation.ThrowIfCancellationRequested();long decodedBytes=0;
        foreach(var mip in decoded.Mips){decodedBytes=checked(decodedBytes+mip.Rgba.Length);Need(mip.Rgba.Length==checked(mip.Width*mip.Height*4),"Actual decoded mip extent.");}
        Need(decodedBytes<=l.MaximumDecodedBytes&&decoded.Mips.Count<=l.MaximumMips,"Decoded totals before publication.");
        cancellation.ThrowIfCancellationRequested();return new(shader,parameters,factors,binding,decoded,shaderInput.Identity,textureInput.Identity,policy,diffuse,sourceBytes,decodedBytes);
    }
}
