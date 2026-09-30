using System;
using System.Linq;
using Godot;
using Stardust.Core.Assets;

public static class DdsGpuVerification
{
    public static void Verify(DdsImage source)
    {
        int required=1;for(int size=Math.Max(source.Width,source.Height);size>1;size>>=1)required++;
        if(source.Mips.Count!=1 && source.Mips.Count!=required)throw new AssetPreviewException("DDS upload fixture requires a complete chain or one mip.");
        byte[] pixels=source.Mips.SelectMany(mip=>mip.Rgba.ToArray()).ToArray();
        using Image image=Image.CreateFromData(source.Width,source.Height,source.Mips.Count>1,Image.Format.Rgba8,pixels);
        using ImageTexture texture=ImageTexture.CreateFromImage(image);
        using Image downloaded=texture.GetImage();
        if(downloaded.GetWidth()!=source.Width || downloaded.GetHeight()!=source.Height || downloaded.GetMipmapCount()!=source.Mips.Count-1 ||
            !downloaded.GetData().AsSpan().SequenceEqual(pixels))throw new InvalidOperationException("DDS RGBA mip GPU readback mismatch.");
    }
}
