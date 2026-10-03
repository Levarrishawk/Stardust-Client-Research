using Stardust.Core.Protocol;
using System.Security.Cryptography;
using System.Text;

namespace Stardust.Core.Assets;

public enum SceneStaticMaterialPolicy { RequireComplete, AllowNeutral }
public sealed record SceneStaticResources(SceneCreateObjectByCrcMessage Create, ObjectTemplateCatalogEntry Recipe,
    SharedObjectAppearanceProfile Profile, TexturedAppearanceGraph Appearance, IReadOnlyDictionary<string, AssetSourceIdentity> Sources,
    int ExternalReads, long SourceBytes, SceneStaticMaterialPolicy MaterialPolicy)
{
    public SwitchTextureFrameBundle? SwitchFrames { get; init; }
    internal SceneStaticSwitchFrameProof? SwitchFrameProof { get; init; }
}

public sealed record SceneStaticSwitchFramePolicy(SwitchTextureFrameBundleLimits? Limits = null,
    int MaximumRetainedMips = 4096, long MaximumPendingWeight = 268435456)
{
    internal void Validate(SceneStaticMaterialPolicy material, SwitchTextureFrameSelection? selection)
    {
        if(material != SceneStaticMaterialPolicy.RequireComplete || selection is null)throw new ArgumentException("All frames require complete materials and explicit pinned selection.");
        var l=Limits??new(); var d=l.DdsLimits??new();
        if(MaximumRetainedMips is <1 or >8192 || MaximumPendingWeight is <1 or >536870912 ||
            l.MaximumFrames is <1 or >256 || l.MaximumSources is <1 or >258 || l.MaximumSourceBytes is <1 or >268435456 ||
            l.MaximumDecodedBytes is <0 or >536870912 || l.MaximumMips is <0 or >4096 || l.MaximumShaderBytes is <1 or >1048576 ||
            d.MaximumDimension is <1 or >32768 || d.MaximumInputBytes is <128 or >67108864 || d.MaximumDecodedBytes is <4 or >268435456)
            throw new ArgumentOutOfRangeException(nameof(Limits));
    }
}

internal sealed class SceneStaticSwitchFrameProof
{
    internal readonly SwitchTextureFrameBundle Bundle;
    private readonly string[] hashes;
    private readonly string tailHash, shaderHash, baseHash, version, effect;
    private readonly string[] bindings;
    private static string Binding(ShaderTextureReference t)=>$"{t.Tag:X8}:{t.SourceFlag}:{t.Version}:{t.AssetPath}:{Convert.ToHexString(SHA256.HashData(t.BindingMetadata.Span))}";
    internal readonly int Selected;
    internal readonly SceneStaticSwitchFramePolicy Policy;
    internal SceneStaticSwitchFrameProof(SwitchTextureFrameBundle bundle, int selected, SceneStaticSwitchFramePolicy policy)
    { Bundle=bundle; Selected=selected; Policy=policy; var desc=bundle.CopySwitchDescription();var selectedRefs=SwitchTextureShaderDecoder.Select(desc,bundle.CopyBaseDescription(),selected);
      shaderHash=Convert.ToHexString(SHA256.HashData(desc.Metadata.Payload.Span));baseHash=Convert.ToHexString(SHA256.HashData(selectedRefs.Metadata.Payload.Span));version=selectedRefs.Version;effect=selectedRefs.EffectPath;bindings=selectedRefs.Textures.Select(Binding).ToArray(); tailHash=Convert.ToHexString(SHA256.HashData(bundle.Frames[selected].Image.CopyRetainedTerminalBlock())); hashes=bundle.Frames[selected].Image.Mips.Select(m=>Convert.ToHexString(SHA256.HashData(m.CopyRgba()))).ToArray(); }
    internal bool MatchesMaterial(SwitchTextureShaderDescription desc, StaticShaderReferenceDescription references)
    { return references.InlineEffect is null && references.Version==version && references.EffectPath==effect && references.Textures.Count==bindings.Length &&
        references.Textures.Select(Binding).SequenceEqual(bindings) && Convert.ToHexString(SHA256.HashData(desc.Metadata.Payload.Span))==shaderHash && Convert.ToHexString(SHA256.HashData(references.Metadata.Payload.Span))==baseHash; }
    internal bool Matches(DdsImage image, int selected)
    {
        if(selected!=Selected)return false; var own=Bundle.Frames[selected].Image;
        return image.Format==own.Format && image.ColorSpace==own.ColorSpace && image.AlphaMode==own.AlphaMode && image.DxgiFormat==own.DxgiFormat &&
            image.RetainedTerminalBlock.Length==own.RetainedTerminalBytes && Convert.ToHexString(SHA256.HashData(image.RetainedTerminalBlock.Span))==tailHash && image.Mips.Count==hashes.Length &&
            image.Mips.Select((m,i)=>m.Width==own.Mips[i].Width && m.Height==own.Mips[i].Height && m.Rgba.Length==own.Mips[i].DecodedBytes &&
                Convert.ToHexString(SHA256.HashData(m.Rgba.Span))==hashes[i]).All(b=>b);
    }
}

public static class SceneStaticResourceReader
{
    private sealed class Invocation { internal bool Poisoned; }
    [ThreadStatic] private static Invocation? active;
    private static void Guard(CancellationToken token) { if(active?.Poisoned==true)throw new InvalidOperationException("Recursive static resource load."); token.ThrowIfCancellationRequested(); }
    public static SceneStaticResources Load(SceneCreateObjectByCrcMessage create, ObjectTemplateCatalog catalog,
        Func<string,TreLookupResult> resolve, SceneStaticMaterialPolicy materialPolicy, CancellationToken token=default,
        AppearanceGraphLimits? limits=null, long maximumDecodedBytes=134217728, SwitchTextureFrameSelection? switchTextureFrame=null,
        SceneStaticSwitchFramePolicy? switchFrames=null)
    {
        if(active is not null) { active.Poisoned=true; throw new InvalidOperationException("Recursive static resource load."); }
        active=new(); try { return LoadCore(create,catalog,resolve,materialPolicy,token,limits,maximumDecodedBytes,switchTextureFrame,switchFrames); } finally { active=null; }
    }
    private static SceneStaticResources LoadCore(SceneCreateObjectByCrcMessage create, ObjectTemplateCatalog catalog,
        Func<string, TreLookupResult> resolve, SceneStaticMaterialPolicy materialPolicy, CancellationToken token = default,
        AppearanceGraphLimits? limits = null, long maximumDecodedBytes = 134217728, SwitchTextureFrameSelection? switchTextureFrame = null, SceneStaticSwitchFramePolicy? switchFrames = null)
    {
        ArgumentNullException.ThrowIfNull(create); ArgumentNullException.ThrowIfNull(catalog); ArgumentNullException.ThrowIfNull(resolve);
        switchTextureFrame?.Validate(); switchFrames?.Validate(materialPolicy,switchTextureFrame);
        if (!Enum.IsDefined(materialPolicy)) throw new ArgumentOutOfRangeException(nameof(materialPolicy));
        limits ??= new(MaximumResources: 64, MaximumSourceBytes: 16777216);
        if (limits.MaximumResources is < 1 or > 4096 || limits.MaximumSourceBytes is < 1 or > 134217728 ||
            limits.MaximumDepth is < 1 or > 32 || limits.MaximumEdges is < 1 or > 4096 || maximumDecodedBytes is < 4 or > 134217728)
            throw new ArgumentOutOfRangeException(nameof(limits));
        if (new[] { create.QuaternionX, create.QuaternionY, create.QuaternionZ, create.QuaternionW,
                    create.PositionX, create.PositionZ, create.PositionY }.Any(f => !float.IsFinite(f)))
            throw new InvalidDataException("Finite raw create required.");
        var recipe = catalog.Require(create.TemplateCrc);
        if (recipe.Recipe != SceneObjectResourceRecipe.StaticAppearance) throw new InvalidDataException("Static appearance recipe required.");
        Guard(token); int reads = 0; bool framePhase=false; long encodedSeen=0, frameEncoded=0, appearanceDecoded=0;
        var cache = new OwnedAssetSourceCache(path =>
        {
            Guard(token); var result = resolve(path); reads++; Guard(token);
            if(framePhase && result.Kind==TreLookupKind.Present && result.Payload is {} payload)
            { var fl=switchFrames!.Limits??new(); long cap=path.StartsWith("shader/",StringComparison.Ordinal)?fl.MaximumShaderBytes:(fl.DdsLimits??new()).MaximumInputBytes; if(payload.Length>cap)throw new InvalidDataException("All-frame source input cap.");
                if(checked(frameEncoded+payload.Length)>fl.MaximumSourceBytes || checked(encodedSeen+payload.Length+appearanceDecoded)>switchFrames.MaximumPendingWeight)throw new InvalidDataException("All-frame intermediate encoded growth cap.");
                frameEncoded=checked(frameEncoded+payload.Length);
            }
            if(result.Kind==TreLookupKind.Present && result.Payload is {} sourcePayload)encodedSeen=checked(encodedSeen+sourcePayload.Length);
            return result;
        }, limits);
        TreLookupResult Read(string path) { Guard(token); return cache.Read(path); }
        var profile = SharedObjectAppearanceProfileReader.Load(Read, recipe.ObjectPath, recipe.ObjectSha256,
            Math.Min(16, limits.MaximumDepth), Math.Min(1048576, limits.MaximumSourceBytes));
        var appearance = TexturedAppearanceGraphReader.Load(Read, profile.AppearancePath, recipe.AppearanceSha256, limits, maximumDecodedBytes, switchTextureFrame: switchTextureFrame);
        Guard(token); var visited = new HashSet<string>(StringComparer.Ordinal); int meshCount = 0;
        void Check(AppearanceGeometryTemplate template)
        {
            if (!visited.Add(template.Path)) return;
            if (template.Diagnostics.Count != 0) throw new InvalidDataException("Required static geometry part unavailable.");
            if (template.Mesh is not null) meshCount++;
            foreach (var child in template.Children) Check(child.Template);
        }
        Check(appearance.Geometry.Root);
        if (meshCount == 0) throw new InvalidDataException("Static recipe has no supported geometry.");
        if (materialPolicy == SceneStaticMaterialPolicy.RequireComplete && (appearance.Diagnostics.Count != 0 || appearance.Meshes.Count != meshCount))
            throw new InvalidDataException("Complete static materials required by caller policy.");
        Guard(token);
        var initial=new SceneStaticResources(create with { }, recipe, profile, appearance, cache.Sources, reads, cache.Sources.Values.Sum(s => (long)s.Bytes), materialPolicy);
        if(switchFrames is null)return initial;
        var selected=switchTextureFrame!; var inspections=appearance.Meshes.Values.SelectMany(m=>m.Materials).Where(p=>p.Key==selected.ShaderPath).Select(p=>p.Value.SwitchTexture).ToArray();
        if(inspections.Length==0 || inspections.Any(i=>i is null || i.Selection!=selected))throw new InvalidDataException("Requested SWTS material absent.");
        var desc=inspections[0]!.Description; var fl=switchFrames.Limits??new();
        if(desc.RawDtstWord0 is 0 or >int.MaxValue || desc.RawDtstWord0!=desc.Frames.Count || desc.Frames.Count>fl.MaximumFrames || !float.IsFinite(BitConverter.UInt32BitsToSingle(desc.RawDtstWord1)) || BitConverter.UInt32BitsToSingle(desc.RawDtstWord1)<=0)throw new InvalidDataException("Modern frame eligibility.");
        // Preflight every literal before reading any additional path; accepted legacy normalization is insufficient.
        static void Literal(ReadOnlySpan<byte> raw,string expected,string prefix,string suffix)
        {
            if(raw.Length<2 || raw.Length>1025 || raw[^1]!=0 || raw[..^1].IndexOf((byte)0)>=0 || Encoding.ASCII.GetString(raw[..^1])!=expected || expected.Length>1024 || !expected.StartsWith(prefix,StringComparison.Ordinal) || !expected.EndsWith(suffix,StringComparison.Ordinal))throw new InvalidDataException("Canonical all-frame literal required.");
            bool component=false; foreach(char c in expected[..^suffix.Length]) { if(c=='/') { if(!component)throw new InvalidDataException("Empty path component.");component=false; } else { if(c is not (>= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '-'))throw new InvalidDataException("Unsafe path.");component=true; } } if(!component)throw new InvalidDataException("Empty path.");
            foreach(byte b in raw[..^1])if(b>127)throw new InvalidDataException("ASCII path required.");
        }
        var fields=desc.Metadata.Children[0].Children; Literal(fields[0].Payload.Span,desc.BaseShaderPath,"shader/",".sht");
        for(int i=0;i<desc.Frames.Count;i++)Literal(fields[i+2].Payload.Span[4..],desc.Frames[i].TexturePath,"texture/",".dds");
        var needed=desc.Frames.Select(f=>f.TexturePath).Append(selected.ShaderPath).Append(desc.BaseShaderPath).ToHashSet(StringComparer.Ordinal);
        if(needed.Count>fl.MaximumSources || cache.Sources.Keys.Concat(needed).Distinct(StringComparer.Ordinal).Count()>limits.MaximumResources)throw new InvalidDataException("All-frame source union cap.");
        var baseBytes=Read(desc.BaseShaderPath).Payload??throw new InvalidDataException("Base shader absent.");
        var basis=StaticShaderReferences.Decode(baseBytes);
        foreach(var map in basis.Metadata.Children[0].Children.Where(n=>n.IsForm("TXMS")))foreach(var binding in map.Children)foreach(var name in binding.Children[0].Children.Where(n=>n.Tag=="NAME"))
        { var text=name.Payload.Span; if(text.Length<1)throw new InvalidDataException("Empty base literal.");Literal(text,Encoding.ASCII.GetString(text[..^1]),"texture/",".dds"); }
        for(int i=0;i<desc.Frames.Count;i++)_=SwitchTextureShaderDecoder.Select(desc,basis,i);
        var before=SceneObjectResourceAccounting.Measure(new SceneObjectResources(initial));
        appearanceDecoded=before.DecodedTextureBytes; frameEncoded=needed.Where(cache.Sources.ContainsKey).Sum(p=>(long)cache.Sources[p].Bytes);
        if(before.Mips>switchFrames.MaximumRetainedMips || checked(encodedSeen+appearanceDecoded)>switchFrames.MaximumPendingWeight || frameEncoded>fl.MaximumSourceBytes)throw new InvalidDataException("All-frame pre-existing retained budget.");
        foreach(string path in needed.Where(cache.Sources.ContainsKey))if(cache.Sources[path].Bytes>(path.StartsWith("shader/",StringComparison.Ordinal)?fl.MaximumShaderBytes:(fl.DdsLimits??new()).MaximumInputBytes))throw new InvalidDataException("Cached all-frame source input cap.");
        framePhase=true;
        // Reserve the complete encoded union before decoding so pending weight cannot grow past its cap mid-decode.
        foreach(string path in needed) { Guard(token); var source=Read(path); if(source.Kind!=TreLookupKind.Present || source.Payload is null)throw new InvalidDataException("All-frame source absent."); if(needed.Where(cache.Sources.ContainsKey).Sum(p=>(long)cache.Sources[p].Bytes)>fl.MaximumSourceBytes)throw new InvalidDataException("All-frame encoded bundle growth cap."); if(checked(cache.Sources.Values.Sum(s=>(long)s.Bytes)+before.DecodedTextureBytes)>switchFrames.MaximumPendingWeight)throw new InvalidDataException("All-frame pending source growth cap."); }
        long sourceBytes=cache.Sources.Values.Sum(s=>(long)s.Bytes);
        long remaining=Math.Min(maximumDecodedBytes-before.DecodedTextureBytes,switchFrames.MaximumPendingWeight-sourceBytes-before.DecodedTextureBytes);
        int remainingMips=switchFrames.MaximumRetainedMips-before.Mips;
        if(remaining<0 || remainingMips<0)throw new InvalidDataException("All-frame retained budget.");
        var bundle=SwitchTextureFrameBundle.Load(Read,selected.ShaderPath,selected.ShaderSha256,fl with {MaximumDecodedBytes=Math.Min(fl.MaximumDecodedBytes,remaining),MaximumMips=Math.Min(fl.MaximumMips,remainingMips)},token);
        var complete=initial with {Sources=cache.Sources,ExternalReads=reads,SourceBytes=sourceBytes,SwitchFrames=bundle,SwitchFrameProof=new(bundle,selected.FrameIndex,switchFrames)};
        var options=new SceneStaticResourceOptions(materialPolicy,selected,limits,maximumDecodedBytes,switchFrames);
        Guard(token); SceneObjectResourceValidation.Validate(new SceneObjectResources(complete),create,catalog,options); Guard(token); return complete;
    }
}
