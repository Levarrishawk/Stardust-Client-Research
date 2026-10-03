namespace Stardust.Core.Assets;

/// <summary>Immutable logical source, decoded input and generated face budget; excludes other storage.</summary>
public sealed class SceneObjectResourceAccountingReceipt
{
    internal SceneObjectResourceAccountingReceipt(int sources, long sourceBytes, int textures, int mips, long decodedBytes, int generatedFaces=0, long generatedFaceBytes=0, int switchFrames=0, int switchTextures=0, int switchMips=0, long switchBytes=0)
    { SwitchFrames=switchFrames; SwitchFrameTextures=switchTextures; SwitchFrameMips=switchMips; SwitchFrameDecodedBytes=switchBytes; Sources=sources; SourceBytes=sourceBytes; DecodedTextures=textures; Mips=mips; DecodedTextureBytes=decodedBytes; GeneratedFaces=generatedFaces; GeneratedFaceBytes=generatedFaceBytes; PendingWeight=checked(checked(sourceBytes+decodedBytes)+generatedFaceBytes); }
    public int SwitchFrames { get; }
    public int SwitchFrameTextures { get; }
    public int SwitchFrameMips { get; }
    public long SwitchFrameDecodedBytes { get; }
    public int Sources { get; }
    public long SourceBytes { get; }
    public int DecodedTextures { get; }
    public int Mips { get; }
    public long DecodedTextureBytes { get; }
    public int GeneratedFaces { get; }
    public long GeneratedFaceBytes { get; }
    public long PendingWeight { get; }
}

/// <summary>Pure measurement of passive owned descriptors. Does not validate compatibility or freshness.</summary>
public static class SceneObjectResourceAccounting
{
    public static SceneObjectResourceAccountingReceipt Measure(SceneObjectResources result)
    {
        ArgumentNullException.ThrowIfNull(result);
        Need(result.Sources is not null && (result.Creature is null)!=(result.Static is null),"Incomplete resource descriptor.");
        int sources=0, textures=0, mips=0, generatedCount=0, switchFrames=0, switchTextures=0, switchMips=0; long switchBytes=0; long sourceBytes=0, decodedBytes=0, generatedBytes=0;
        foreach(var pair in result.Sources!)
        {
            Need(pair.Value is not null && pair.Key==pair.Value.Path && pair.Value.Bytes>0,"Invalid source accounting identity.");
            sources=checked(sources+1); sourceBytes=checked(sourceBytes+pair.Value!.Bytes);
        }
        Need(sources==result.ExternalReads && sourceBytes==result.SourceBytes,"Source accounting disagrees with completion.");
        var images=new Dictionary<string,object>(StringComparer.Ordinal);
        var inputReferences=new HashSet<object>(ReferenceEqualityComparer.Instance);
        bool Add(string path, object? image, bool equivalentFaceInput=false)
        {
            Need(path is not null && result.Sources.ContainsKey(path) && image is not null,"Decoded texture source/image absent.");
            inputReferences.Add(image!);
            if(images.TryGetValue(path!,out var prior)) { Need(ReferenceEquals(prior,image) || equivalentFaceInput && prior is DdsImage a && image is DdsImage b &&
                a.Format==b.Format && a.ColorSpace==b.ColorSpace && a.AlphaMode==b.AlphaMode && a.Mips is not null && b.Mips is not null && a.Mips.Count==b.Mips.Count &&
                a.Mips.Zip(b.Mips).All(p=>p.First is not null && p.Second is not null && p.First.Width==p.Second.Width && p.First.Height==p.Second.Height && p.First.Rgba.Span.SequenceEqual(p.Second.Rgba.Span)),"Contradictory shared decoded texture identity."); return false; }
            images.Add(path!,image!); textures=checked(textures+1); return true;
        }
        void Mip(int width,int height,int bytes,int channels)
        {
            Need(width>0 && height>0,"Invalid decoded mip dimensions.");
            long expected=checked(checked((long)width*height)*channels);
            Need(expected==bytes,"Decoded mip length disagrees with dimensions.");
            mips=checked(mips+1); decodedBytes=checked(decodedBytes+bytes);
        }
        void Rgba(string path,DdsImage? image,bool equivalentFaceInput=false)
        {
            if(!Add(path,image,equivalentFaceInput))return;
            Need(image!.Mips is {Count:>0},"Missing decoded mips.");
            foreach(var mip in image.Mips) { Need(mip is not null,"Null decoded mip."); Mip(mip!.Width,mip.Height,mip.Rgba.Length,4); }
        }
        void Hdr(string path,DdsHdrImage? image)
        {
            if(!Add(path,image))return;
            Need(image!.Mips is {Count:>0},"Missing decoded HDR mips.");
            foreach(var mip in image.Mips) { Need(mip is not null,"Null decoded HDR mip."); Mip(mip!.Width,mip.Height,mip.RgbHalf.Length,6); }
        }
        if(result.Creature is {} creature)
        {
            Need(creature.Creature?.MaterialBundle?.Textures is not null,"Missing creature texture map.");
            foreach(var pair in creature.Creature!.MaterialBundle!.Textures)Rgba(pair.Key,pair.Value);
            if(creature.Creature.FaceResources is {} faceResources)
            {
                Need(faceResources.Textures is not null,"Missing face texture map.");
                foreach(var pair in faceResources.Textures)Rgba(pair.Key,pair.Value,equivalentFaceInput:true);
            }
            if(creature.Creature.GeneratedFaces is {} generated)
            {
                Need(generated.Outputs is not null,"Missing generated output map.");
                var outputs=new Dictionary<DdsImage,BlueprintComposedFace>(ReferenceEqualityComparer.Instance);
                foreach(var face in generated.Outputs.Values)
                {
                    Need(face?.Image is not null && face.Layers is not null,"Missing generated output metadata.");
                    Need(!inputReferences.Contains(face.Image),"Generated output aliases an authored image.");
                    if(outputs.TryGetValue(face.Image,out var prior))
                    {
                        Need(prior.Layers.Count==face.Layers.Count && prior.Layers.Zip(face.Layers).All(pair=>
                            pair.First is not null && pair.Second is not null && pair.First.MipLevels is not null && pair.Second.MipLevels is not null &&
                            pair.First.ShaderIndex==pair.Second.ShaderIndex && pair.First.VertexArrayIndex==pair.Second.VertexArrayIndex &&
                            pair.First.TexturePath==pair.Second.TexturePath && pair.First.WrittenPixels==pair.Second.WrittenPixels &&
                            pair.First.RgbaSha256==pair.Second.RgbaSha256 && pair.First.MipLevels.SequenceEqual(pair.Second.MipLevels)),"Contradictory generated output aliases.");
                        continue;
                    }
                    outputs.Add(face.Image,face); generatedCount=checked(generatedCount+1);
                    Need(face.Image.Mips is {Count:>0},"Missing generated mips.");
                    foreach(var mip in face.Image.Mips)
                    {
                        Need(mip is not null && mip.Width>0 && mip.Height>0,"Invalid generated mip dimensions.");
                        Need(checked(checked((long)mip!.Width*mip.Height)*4)==mip.Rgba.Length,"Generated mip length disagrees with dimensions.");
                        generatedBytes=checked(generatedBytes+mip.Rgba.Length);
                    }
                }
            }
        }
        else
        {
            Need(result.Static!.Appearance?.Meshes is not null,"Missing static mesh map.");
            foreach(var mesh in result.Static.Appearance!.Meshes.Values)
            {
                Need(mesh?.Textures is not null && mesh.HdrTextures is not null,"Missing static texture maps.");
                foreach(var pair in mesh!.Textures)Rgba(pair.Key,pair.Value);
                foreach(var pair in mesh.HdrTextures)Hdr(pair.Key,pair.Value);
            }
        }
        if(result.Static?.SwitchFrames is {} bundle)
        {
            Need(bundle.Frames.Count is >0 and <=256 && bundle.Images.Count<=256 && bundle.MipCount<=4096,"Frame accounting work bound.");
            foreach(var pair in bundle.Sources)Need(result.Sources.TryGetValue(pair.Key,out var source) && source==pair.Value,"Frame source accounting mismatch.");
            foreach(var pair in bundle.Images)
            {
                Need(result.Sources.ContainsKey(pair.Key) && pair.Value.Mips.Count is >0 and <=32,"Frame image accounting identity.");
                switchTextures=checked(switchTextures+1);
                foreach(var mip in pair.Value.Mips) { Need(mip.Width>0 && mip.Height>0 && checked((long)mip.Width*mip.Height*4)==mip.DecodedBytes,"Frame mip accounting shape."); switchMips=checked(switchMips+1); switchBytes=checked(switchBytes+mip.DecodedBytes); }
                switchBytes=checked(switchBytes+pair.Value.RetainedTerminalBytes);
            }
            switchFrames=bundle.Frames.Count;
            Need(switchMips==bundle.MipCount && switchBytes==bundle.DecodedBytes,"Frame accounting counters mismatch.");
            textures=checked(textures+switchTextures);mips=checked(mips+switchMips);decodedBytes=checked(decodedBytes+switchBytes);
        }
        return new(sources,sourceBytes,textures,mips,decodedBytes,generatedCount,generatedBytes,switchFrames,switchTextures,switchMips,switchBytes);
    }
    private static void Need([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool condition,string reason) { if(!condition)throw new InvalidDataException(reason); }
}
