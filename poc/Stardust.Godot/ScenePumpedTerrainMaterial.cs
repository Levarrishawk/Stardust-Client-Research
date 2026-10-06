using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;

// Explicit selected original resource; association with original terrain remains unproved.
public partial class ScenePumpedWorldPreview
{
    private TerrainSuppliedStaticMaterial? suppliedTerrainMaterial;
    private ImageTexture? suppliedTerrainTexture;
    private int terrainMaterialReads,terrainTextureAllocations,terrainTextureDisposals;
    private void LoadTerrainMaterialArguments(string[] args,string root)
    {
        int option=Array.IndexOf(args,"--terrain-material-profile");if(option<0)return;
        Check(option+1<args.Length&&args.Count(a=>a=="--terrain-material-profile")==1&&args.Contains("--terrain-peer"),"Explicit terrain material mode");
        using var stream=File.OpenRead(args[option+1]);Check(stream.Length is >0 and <=4096,"Bounded material profile");
        using var doc=JsonDocument.Parse(stream,new JsonDocumentOptions{MaxDepth=4});var p=doc.RootElement;
        Check(p.GetProperty("Version").GetInt32()==1&&p.GetProperty("Policy").GetString()=="ExplicitSuppliedSshtMainVertexColor"
            &&!p.GetProperty("OriginalTerrainAssociation").GetBoolean(),"Supplied material association policy");
        using var text=new StringReader(Encoding.UTF8.GetString(cfg));
        using(var overlay=ConfiguredTreOverlay.Open(root,text))
        {
            suppliedTerrainMaterial=TerrainSuppliedStaticMaterial.Load(path=>{terrainMaterialReads++;return overlay.Lookup(path);},
                p.GetProperty("ShaderPath").GetString()!,p.GetProperty("ShaderSha256").GetString()!,TerrainSuppliedStaticMaterialPolicy.TextureTimesVertexColor);
        }
        Check(suppliedTerrainMaterial.ExternalReads==terrainMaterialReads&&terrainMaterialReads==2,"Exactly shader and MAIN texture read");
        int required=1;for(int size=Math.Max(suppliedTerrainMaterial.Width,suppliedTerrainMaterial.Height);size>1;size>>=1)required++;
        Check(suppliedTerrainMaterial.MipCount==1||suppliedTerrainMaterial.MipCount==required,"Godot upload requires one mip or complete chain before allocation");
        GD.Print("terrain_supplied_material_cpu="+JsonSerializer.Serialize(new{suppliedTerrainMaterial.ShaderSource,suppliedTerrainMaterial.TextureSource,
            suppliedTerrainMaterial.Width,suppliedTerrainMaterial.Height,suppliedTerrainMaterial.MipCount,suppliedTerrainMaterial.SourceBytes,suppliedTerrainMaterial.DecodedBytes,
            ColorSpace=suppliedTerrainMaterial.ColorSpace.ToString(),Policy=suppliedTerrainMaterial.Policy.ToString(),Reads=terrainMaterialReads,
            OwnedOverlayClosed=true,OriginalTerrainAssociation=false,OriginalShaderExecutionParity=false}));
    }
    private TerrainPatchVisual CreateSparseTerrainVisual(TerrainPatchGeometry geometry)
    {
        if(suppliedTerrainMaterial is null)return new(geometry,this,TerrainPatchRenderPolicy.ModernLitVertexColor);
        if(suppliedTerrainTexture is null)
        {
            var source=suppliedTerrainMaterial;
            byte[] pixels=Enumerable.Range(0,source.MipCount).SelectMany(source.CopyRgba).ToArray();
            using var image=Image.CreateFromData(source.Width,source.Height,source.MipCount>1,Image.Format.Rgba8,pixels);
            suppliedTerrainTexture=ImageTexture.CreateFromImage(image);terrainTextureAllocations++;
            using var readback=suppliedTerrainTexture.GetImage();
            Check(readback.GetWidth()==source.Width&&readback.GetHeight()==source.Height&&readback.GetMipmapCount()==source.MipCount-1
                &&readback.GetData().AsSpan().SequenceEqual(pixels),"Owned MAIN upload byte readback");
            GD.Print("terrain_supplied_material_gpu="+JsonSerializer.Serialize(new{Textures=1,SharedByPatches=16,ExactMipBytes=true,
                Sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(pixels)),BorrowedByPatches=true,OriginalShaderExecutionParity=false}));
            GD.Print("terrain_supplied_material_mips="+JsonSerializer.Serialize(Enumerable.Range(0,source.MipCount).Select(i=>new{
                Width=Math.Max(1,source.Width>>i),Height=Math.Max(1,source.Height>>i),Bytes=source.CopyRgba(i).Length,
                Sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(source.CopyRgba(i)))})));
        }
        return new(geometry,this,TerrainPatchRenderPolicy.ModernSuppliedMainTexture,suppliedTerrainMaterial,suppliedTerrainTexture);
    }
    private void CloseTerrainMaterial()
    {
        if(suppliedTerrainTexture is not null){suppliedTerrainTexture.Dispose();suppliedTerrainTexture=null;terrainTextureDisposals++;}
        if(suppliedTerrainMaterial is not null){GD.Print("terrain_supplied_material_cleanup="+JsonSerializer.Serialize(new{Allocated=terrainTextureAllocations,
            Disposed=terrainTextureDisposals,Drained=terrainTextureAllocations==terrainTextureDisposals,Reads=terrainMaterialReads}));suppliedTerrainMaterial=null;}
    }
}
