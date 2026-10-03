using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;

try
{
    if(args.Length!=4)throw new ArgumentException("Explicit root/config/catalog/bank required");
    string Hash(ReadOnlySpan<byte> b)=>Convert.ToHexString(SHA256.HashData(b));
    void Check(bool v,string why){if(!v)throw new Exception(why);}
    Check(Hash(File.ReadAllBytes(args[1]))==RetainedSkeletalDetailBundleReader.ConfigurationPin,"Configuration pin");
    var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(args[2]));using var bankFile=File.OpenRead(args[3]);var bank=SkeletalAnimationBankProfile.Load(bankFile);
    var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
    var options=new SceneCreatureResourceOptions(idle,bank,true);var create=new SceneCreateObjectByCrcMessage(101,0,0,0,1,0,0,0,2937962913,0);
    int reads=0,scopes=0;var borrowed=new List<byte[]>();SceneObjectResources result;scopes++;
    try {using var cfg=File.OpenText(args[1]);using var overlay=ConfiguredTreOverlay.Open(args[0],cfg);
        result=SceneObjectResourceRouter.Load(create,catalog,options,p=>{reads++;var r=overlay.Lookup(p);if(r.Payload is not null)borrowed.Add(r.Payload);return r;});foreach(var b in borrowed)Array.Clear(b);
    }finally{scopes--;}
    Check(scopes==0&&reads==39&&result.SourceBytes==4686371,"Exact closed39-source closure");SceneObjectResourceValidation.Validate(result,create,catalog,options);
    string before=Hash(JsonSerializer.SerializeToUtf8Bytes(result));int beforeReads=reads;var c=result.Creature!.Creature;var bundle=c.MaterialBundle!;
    var rows=new List<object>();
    foreach(var pair in c.Geometries!.OrderBy(p=>p.Key))for(int slot=0;slot<pair.Value.Shaders.Count;slot++)
    {
        var g=pair.Value.Shaders[slot];var material=bundle.Materials[g.ShaderPath];var main=material.References.Textures.Single(t=>t.Tag==0x4D41494E);
        var image=bundle.Textures[main.AssetPath!];var mip=image.Mips[0];var pixels=mip.Rgba.Span;
        var factors=bundle.AlbedoFactors!.Materials[g.ShaderPath];int white=0;var minima=new[]{255,255,255,255};var maxima=new[]{0,0,0,0};
        for(int n=0;n<pixels.Length;n+=4){if(pixels[n]==255&&pixels[n+1]==255&&pixels[n+2]==255)white++;for(int j=0;j<4;j++){minima[j]=Math.Min(minima[j],pixels[n+j]);maxima[j]=Math.Max(maxima[j],pixels[n+j]);}}
        int middle=((mip.Height/2)*mip.Width+mip.Width/2)*4;var rgba=pixels.Slice(middle,4).ToArray();
        var evaluated=SkeletalAlbedoEvaluator.Evaluate(new Vector4(rgba[0]/255f,rgba[1]/255f,rgba[2]/255f,rgba[3]/255f),factors);
        var rendererBindings=pair.Value.TextureRenderers.SelectMany(r=>r.Bindings.Where(b=>b.ShaderIndex==(uint)slot).Select(b=>new {RendererPath=r.Path,b.ShaderIndex,b.TextureTag,SourceLoaded=result.Sources.ContainsKey(r.Path)})).ToArray();
        bundle.PaletteDefaults!.TryGetValue(g.ShaderPath,out var palettes);
        var passes=bundle.Effects[material.References.EffectPath].Implementations.SelectMany(i=>i.Passes).Where(p=>p.PixelProgram==factors.PixelProgram).Select(p=>new {p.PixelProgram,FirstTag=p.Fields.GetValueOrDefault(0x40),SecondTag=p.Fields.GetValueOrDefault(0x44),p.PixelTextures}).ToArray();
        rows.Add(new {Mesh=pair.Key,MeshSource=result.Sources[pair.Key],Slot=slot,Shader=g.ShaderPath,ShaderSource=result.Sources[g.ShaderPath],g.Triangles.Count,
            material.References.EffectPath,EffectSource=result.Sources[material.References.EffectPath],Main=main,MainSource=result.Sources[main.AssetPath!],
            MainCoordinates=material.Parameters.CoordinateSets[0x4D41494E],RendererBindings=rendererBindings,Customization=material.Customization is not null,CshdVersion=material.Customization?.Version,
            AuthoredTextureSelections=material.Customization?.TextureSelections,PaletteFactors=palettes?.AuthoredFactors,StaticFactors=StaticShaderFactors.Decode(material.References),Albedo=factors,ProgramSource=result.Sources[factors.PixelProgram],Passes=passes,
            Texture=new {mip.Width,mip.Height,Mips=image.Mips.Count,Format=image.Format.ToString(),Pixels=pixels.Length/4,WhiteRgbPixels=white,Min=minima,Max=maxima,MidRgba=rgba,MidAlbedo=new[]{evaluated.X,evaluated.Y,evaluated.Z},Mip0Sha256=Hash(pixels)}});
    }
    Check(reads==beforeReads&&before==Hash(JsonSerializer.SerializeToUtf8Bytes(result)),"Diagnostic performs no further I/O or resource mutation");
    int cancelledReads=0;using var cancelled=new CancellationTokenSource();cancelled.Cancel();
    try{SceneObjectResourceRouter.Load(create,catalog,options,p=>{cancelledReads++;throw new Exception("Resolver must not run");},cancelled.Token);throw new Exception("Canceled diagnostic input accepted");}catch(OperationCanceledException){}
    Check(cancelledReads==0,"Precancel has no reads");
    Console.WriteLine("head_gap_receipt="+JsonSerializer.Serialize(new {Reads=reads,result.SourceBytes,ArchiveClosed=scopes==0,NoFurtherReads=true,Unchanged=true,PrecancelReads=cancelledReads,
        FaceBlueprints=c.FaceBlueprints is not null,FaceResources=c.FaceResources is not null,GeneratedFaces=c.GeneratedFaces is not null,CustomizationValues=c.CustomizationValues is not null,
        Rows=rows,Sources=result.Sources.Values.OrderBy(s=>s.Path)}));
    Console.WriteLine("PASS: bounded original head/body material chain exported; closed archives, no mutation/additional reads, precancel cleanup.");
}
catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}
