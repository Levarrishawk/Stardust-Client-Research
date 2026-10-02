using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stardust.Core.Assets;

/// <summary>One component-preview owner; core content-identity cache supplies shared immutable DdsImage references.</summary>
internal sealed class PreviewTextureCache : IDisposable
{
    private readonly Dictionary<DdsImage,ImageTexture> textures=new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<DdsHdrImage,ImageTexture> hdrTextures=new(ReferenceEqualityComparer.Instance);
    private long uploadedBytes;
    internal int Count=>textures.Count+hdrTextures.Count;
    internal long UploadedBytes=>uploadedBytes;
    internal void Release(DdsImage source)
    {
        if(!textures.Remove(source,out var texture))return;
        texture.Dispose();uploadedBytes-=source.Mips.Sum(mip=>(long)mip.Rgba.Length);
    }
    internal ImageTexture Get(DdsImage source)
    {
        if(textures.TryGetValue(source,out ImageTexture? existing))return existing;
        long bytes=source.Mips.Sum(mip=>(long)mip.Rgba.Length);
        if(bytes>134_217_728-uploadedBytes)throw new InvalidOperationException("Component GPU texture upload budget exceeded.");
        byte[] pixels=source.Mips.SelectMany(mip=>mip.Rgba.ToArray()).ToArray();
        using Image image=Image.CreateFromData(source.Width,source.Height,source.Mips.Count>1,Image.Format.Rgba8,pixels);
        ImageTexture texture=ImageTexture.CreateFromImage(image);textures.Add(source,texture);uploadedBytes+=bytes;return texture;
    }
    internal ImageTexture Get(DdsHdrImage source)
    {
        if(hdrTextures.TryGetValue(source,out ImageTexture? existing))return existing;
        long bytes=source.Mips.Sum(mip=>(long)mip.RgbHalf.Length);
        if(bytes>134_217_728-uploadedBytes)throw new InvalidOperationException("Component GPU texture upload budget exceeded.");
        byte[] pixels=source.Mips.SelectMany(mip=>mip.RgbHalf.ToArray()).ToArray();
        using Image image=Image.CreateFromData(source.Width,source.Height,source.Mips.Count>1,Image.Format.Rgbh,pixels);
        ImageTexture texture=ImageTexture.CreateFromImage(image);hdrTextures.Add(source,texture);uploadedBytes+=bytes;return texture;
    }
    public void Dispose(){foreach(ImageTexture texture in textures.Values.Concat(hdrTextures.Values))texture.Dispose();textures.Clear();hdrTextures.Clear();uploadedBytes=0;}
}
