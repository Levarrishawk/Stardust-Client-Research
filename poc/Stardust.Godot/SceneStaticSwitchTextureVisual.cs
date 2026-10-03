using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Runtime;

/// <summary>Explicit modern matching-second playback. Owns two cursors/cache, borrows exact terminal visual/bundle.</summary>
public sealed class SceneStaticSwitchTextureVisual : IDisposable
{
    private readonly int owner=System.Environment.CurrentManagedThreadId;
    private readonly object bindingOwner=new();
    private SceneStaticVisual? inner;
    private SwitchTextureFrameBundle? bundle;
    private readonly Dictionary<string,ImageTexture> cache=new(StringComparer.Ordinal);
    private readonly Dictionary<string,DdsImage> cpu=new(StringComparer.Ordinal);
    private ImageTexture[] frames=Array.Empty<ImageTexture>();
    private DeltaTimeTextureCursor active=null!, staged=null!;
    private bool retired,bindingAcquired;
    public bool IsRetired=>retired;
    public int UniqueUploads{get;private set;}
    public int ImageCopies{get;private set;}
    public long UploadedBytes{get;private set;}
    public DeltaTimeTextureCursorSnapshot Snapshot{get{Owner();return active.Snapshot;}}
    public ulong SelectedTextureId{get{Owner();return retired?0:frames[active.Snapshot.Index].GetInstanceId();}}
    public bool CursorsSynchronized{get{Owner();var a=active.Snapshot;var b=staged.Snapshot;return a.Index==b.Index && BitConverter.SingleToInt32Bits(a.Residual)==BitConverter.SingleToInt32Bits(b.Residual) && a.Revision==b.Revision && a.CumulativeAdvances==b.CumulativeAdvances;}}
    public static void ValidateInputs(SceneStaticResources source,SwitchTextureFrameBundle bundle)
    {
        SceneStaticVisual.ValidateDescriptor(source,true);ArgumentNullException.ThrowIfNull(bundle);
        if(bundle.ShaderPath!=StaticSwitchTexturePreview.ShaderPath || bundle.Sources[bundle.ShaderPath].Sha256!=StaticSwitchTexturePreview.ShaderPin || bundle.FrameCount!=8 || bundle.Frames.Count!=8 || bundle.RawDtstWord1!=0x3DCCCCCD || bundle.BaseShaderPath!="shader/defaultemis.sht" || bundle.Images.Count>8 || bundle.DecodedBytes>349536 || bundle.MipCount>64)throw new InvalidDataException("Exact bounded terminal frame bundle required.");
        var description=bundle.CopySwitchDescription();var baseShader=bundle.CopyBaseDescription();
        foreach(var mesh in source.Appearance.Meshes.Values)foreach(var material in mesh.Materials.Values.Where(m=>m.SwitchTexture is not null))
        {
            var sw=material.SwitchTexture!;
            if(sw.BaseShaderSource.Sha256!=bundle.Sources[bundle.BaseShaderPath].Sha256 || sw.Description.BaseShaderPath!=bundle.BaseShaderPath || sw.Description.RawDtstWord0!=bundle.RawDtstWord0 || sw.Description.RawDtstWord1!=bundle.RawDtstWord1)throw new InvalidDataException("Static/bundle base or timing correspondence differs.");
            for(int i=0;i<8;i++)if(sw.Description.Frames[i].TexturePath!=bundle.Frames[i].TexturePath || sw.Description.Frames[i].TextureTag!=bundle.Frames[i].TextureTag)throw new InvalidDataException("Static/bundle ordered frames differ.");
        }
        for(int i=0;i<8;i++)
        {
            var frame=bundle.Frames[i];var selected=SwitchTextureShaderDecoder.Select(description,baseShader,i);
            if(frame.Index!=i || frame.TextureTag!=0x4D41494E || frame.TexturePath!=description.Frames[i].TexturePath || !ReferenceEquals(bundle.Images[frame.TexturePath],frame.Image) || selected.Textures.Single(t=>t.Tag==0x4D41494E).AssetPath!=frame.TexturePath)throw new InvalidDataException("Ordered MAIN correspondence differs.");
            var image=frame.Image;if(image.Mips.Count!=8 || image.Mips[0].Width!=128 || image.Mips[0].Height!=64 || image.RetainedTerminalBytes!=0)throw new InvalidDataException("Pinned terminal image layout differs.");
        }
    }
    public SceneStaticSwitchTextureVisual(SceneStaticVisual inner,SceneStaticResources source,SwitchTextureFrameBundle bundle,int maximumCatchUpSteps=4096)
    {
        Owner();ValidateInputs(source,bundle);if(!inner.CanBindTerminal)throw new InvalidOperationException("Live unclaimed terminal visual required.");
        active=new(8,bundle.Interval,maximumCatchUpSteps);staged=new(8,bundle.Interval,maximumCatchUpSteps);
        this.inner=inner;this.bundle=bundle;
        try
        {
            // Copy each path once before the first GPU allocation. Equal bytes at distinct paths stay distinct.
            foreach(var pair in bundle.Images){var image=pair.Value.CopyImage();cpu.Add(pair.Key,image);ImageCopies++;UploadedBytes=checked(UploadedBytes+image.Mips.Sum(m=>(long)m.Rgba.Length));}
            if(UploadedBytes>349536)throw new InvalidDataException("Terminal upload budget.");
            var frame0=bundle.Frames[0].TexturePath;
            foreach(var mesh in source.Appearance.Meshes.Values.Where(m=>m.Textures.ContainsKey(frame0)))
                for(int i=0;i<8;i++)if(!mesh.Textures[frame0].Mips[i].Rgba.Span.SequenceEqual(cpu[frame0].Mips[i].Rgba.Span))throw new InvalidDataException("Static/bundle frame0 bytes differ.");
            if(!inner.AcquireTerminalBinding(bindingOwner))throw new InvalidOperationException("Terminal lifetime changed before upload.");
            bindingAcquired=true;
            frames=UploadFrames(bundle.Frames.Select(f=>f.TexturePath).ToArray(),cpu,cache);UniqueUploads=cache.Count;
            if(!inner.TryBindTerminalTexture(bindingOwner,frames[0]))throw new InvalidOperationException("Terminal retired before binding.");
        }
        catch{Dispose();throw;}
    }
    // Shared production/test cache path. Ordered aliases share by path, equal-byte distinct paths do not.
    internal static ImageTexture[] UploadFrames(IReadOnlyList<string> paths,IReadOnlyDictionary<string,DdsImage> images,Dictionary<string,ImageTexture> cache)
    {
        if(OS.GetThreadCallerId()!=OS.GetMainThreadId() || cache.Count!=0 || paths.Count is <1 or >256 || images.Count is <1 or >8 || paths.Any(p=>!images.ContainsKey(p)) || images.Keys.Any(p=>!paths.Contains(p)))throw new InvalidDataException("Prevalidated bounded upload mapping required.");
        long bytes=images.Values.Sum(i=>i.Mips.Sum(m=>(long)m.Rgba.Length));if(bytes>349536)throw new InvalidDataException("Upload mapping byte budget.");
        foreach(var pair in images)
        {
            var image=pair.Value;var packed=image.Mips.SelectMany(m=>m.Rgba.ToArray()).ToArray();
            using var upload=Image.CreateFromData(image.Width,image.Height,image.Mips.Count>1,Image.Format.Rgba8,packed);cache.Add(pair.Key,ImageTexture.CreateFromImage(upload));
        }
        return paths.Select(p=>cache[p]).ToArray();
    }
    private void Owner(){if(System.Environment.CurrentManagedThreadId!=owner || OS.GetThreadCallerId()!=OS.GetMainThreadId())throw new InvalidOperationException("Switch visual owner/main thread required.");}
    private bool Current(){Owner();if(retired)return false;if(inner!.Tick())return true;Dispose();return false;}
    public bool Tick()=>Current();
    public bool Advance(float suppliedSeconds)=>Apply(suppliedSeconds,false);
    public bool Reset()=>Apply(0,true);
    private bool Apply(float delta,bool reset)
    {
        if(!Current())return false;
        // Staging errors are before binding and leave the active cursor and engine unchanged.
        DeltaTimeTextureCursorSnapshot candidate=reset?staged.Reset():staged.Advance(delta);
        try
        {
            if(!inner!.TryBindTerminalTexture(bindingOwner,frames[candidate.Index])){Dispose();return false;}
            // Commit and synchronize; an unexpected engine or receipt-allocation failure retires, never claims rollback.
            var previous=active;active=staged;staged=previous;
            if(reset)staged.Reset();else staged.Advance(delta);
            if(!CursorsSynchronized)throw new InvalidOperationException("Cursor staging invariant violated.");return true;
        }
        catch{inner?.Dispose();Dispose();throw;}
    }
    public bool VerifySelected(bool gpu)
    {
        if(!Current())return false;int index=active.Snapshot.Index;var texture=frames[index];var original=cpu[bundle!.Frames[index].TexturePath];
        if(!inner!.HasTerminalTexture(texture) || !inner.VerifyGeometry() || !CursorsSynchronized)throw new InvalidDataException("Selected binding/geometry/state differs.");
        if(gpu){using var readback=texture.GetImage();if(readback.GetWidth()!=original.Width || readback.GetHeight()!=original.Height || readback.GetFormat()!=Image.Format.Rgba8 || readback.GetMipmapCount()!=7 || !readback.GetData().AsSpan().SequenceEqual(original.Mips.SelectMany(m=>m.Rgba.ToArray()).ToArray()))throw new InvalidDataException("Selected original mip readback differs.");}
        return true;
    }
    public void Dispose()
    {
        Owner();if(retired)return;retired=true;
        // Detach our textures first; the caller owns inner retirement and any worker drain.
        try{if(bindingAcquired && inner is not null && !inner.IsRetired)inner.ReleaseTerminalBinding(bindingOwner);}
        catch{inner?.Dispose();throw;}
        finally{foreach(var texture in cache.Values){ulong id=texture.GetInstanceId();texture.Dispose();if(GodotObject.InstanceFromId(id)is not null)throw new InvalidOperationException("Switch cache texture survived disposal.");}cache.Clear();cpu.Clear();frames=Array.Empty<ImageTexture>();active?.Dispose();staged?.Dispose();inner=null;bundle=null;}
    }
}
