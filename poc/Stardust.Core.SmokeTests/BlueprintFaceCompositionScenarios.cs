using Stardust.Core.Assets;

internal static class BlueprintFaceCompositionScenarios
{
    private const uint Main=0x4D41494E;
    internal static IffNode Quad(float left=0,float right=1,float top=0,float bottom=1,bool constantUv=false,bool reversed=false,bool nonplanar=false)
    {
        float[] data={left,top,3,0,0,right,top,3,constantUv?0:1,0,right,bottom,nonplanar?4:3,constantUv?0:1,constantUv?0:1,left,bottom,3,0,constantUv?0:1};
        if(reversed)for(int i=0;i<5;i++)(data[5+i],data[15+i])=(data[15+i],data[5+i]);
        return IffDocument.Parse(MeshScenarios.Form("VTXA",MeshScenarios.Form("0003",MeshScenarios.Chunk("INFO",BitConverter.GetBytes(0x1101u).Concat(BitConverter.GetBytes(4u)).ToArray()),MeshScenarios.Chunk("DATA",data.SelectMany(BitConverter.GetBytes).ToArray())))).Root;
    }
    private static DdsImage Solid(params byte[] rgba)=>new(DdsFormat.Rgba32,new[]{new DdsMip(1,1,rgba)});
    internal static (BlueprintTextureRendererDescription Blueprint,BlueprintFaceResources Resources,BlueprintFacePreparation Preparation) Fixture(DdsImage image,bool blend=false,bool clamp=false,uint factor=0xFFFFFFFF,IffNode? vertices=null,uint size=2)
    {
        const string shaderPath="shader/synthetic-face.sht",pixelPath="pixel_program/synthetic.psh",texturePath="texture/synthetic.dds";
        var shader=StaticShaderReferences.Decode(MaterialScenarios.Shader());
        var binding=shader.Textures[0] with {Version="0002",BindingMetadata=BitConverter.GetBytes(Main).Concat(new byte[]{1,clamp?(byte)2:(byte)0,clamp?(byte)2:(byte)0,0,1,2,2,0}).ToArray()};
        shader=shader with{Textures=new[]{binding}};
        var effect=StaticEffectDecoder.Decode(MaterialScenarios.Effect());var pass=effect.Implementations[0].Passes[0];
        var fields=pass.Fields.ToDictionary(p=>p.Key,p=>p.Value);foreach(int offset in new[]{0x19,0x1A,0x30,0x4C,0x24,0x44})fields[offset]=0;
        fields[0x20]=blend?1u:0;fields[0x3C]=15;fields[0x28]=4;fields[0x2C]=5;fields[0x40]=Main;
        byte[] metadata=new byte[]{0}.Concat(BitConverter.GetBytes(Main)).Concat(BitConverter.GetBytes(Main)).Concat(new byte[]{5,5,5,6,6,6,0}).ToArray();
        pass=pass with{Version="0007",Fields=fields,VertexProgram="vertex_program/texren_copy_c1a1.vsh",PixelTextures=new[]{new PixelTextureBinding(0,Main,"0000",metadata)}};
        effect=effect with{Implementations=new[]{effect.Implementations[0] with{Passes=new[]{pass}}}};
        var blueprint=new BlueprintTextureRendererDescription(1,size,size,new uint[]{4},new[]{shaderPath},new[]{texturePath},new[]{vertices??Quad()},1,new[]{new BlueprintRenderSet(0,new uint[]{0})},Array.Empty<IReadOnlyList<BlueprintPrepareOperation>>());
        var resources=new BlueprintFaceResources(new Dictionary<string,StaticShaderReferenceDescription>{{shaderPath,shader}},new Dictionary<string,StaticEffectDescription>{{shaderPath,effect}},new Dictionary<string,DdsImage>{{texturePath,image}},new Dictionary<string,PaletteDescription>(),new Dictionary<string,BlueprintFacePixelProgram>{{pixelPath,BlueprintFacePixelProgramDecoder.Decode(BlueprintFacePixelProgramScenarios.Fixture())}});
        var preparation=new BlueprintFacePreparation(new[]{new BlueprintPreparedShader(0,shaderPath,new Dictionary<uint,string>{{Main,texturePath}},new Dictionary<uint,uint>{{Main,factor}})},Array.Empty<BlueprintSelectedOperation>());
        return(blueprint,resources,preparation);
    }
    internal static void Run()
    {
        byte[] basePixels={100,80,60,200};var baseFace=Fixture(Solid(basePixels));var result=BlueprintFaceCompositor.Compose(baseFace.Blueprint,baseFace.Resources,baseFace.Preparation);
        if(result.Layers[0].WrittenPixels!=4||!result.Image.Mips[0].Rgba.Span.SequenceEqual(new byte[]{100,80,60,200,100,80,60,200,100,80,60,200,100,80,60,200}))throw new Exception("Copy raster/shared fan edge coverage differs.");
        var overlay=Fixture(Solid(200,40,100,128),blend:true,clamp:true,factor:0x0080FFFF);
        const string overlayPath="shader/synthetic-overlay.sht",texture="texture/overlay.dds";
        var resources=baseFace.Resources with{Shaders=baseFace.Resources.Shaders.Append(new KeyValuePair<string,StaticShaderReferenceDescription>(overlayPath,overlay.Resources.Shaders.Values.Single())).ToDictionary(p=>p.Key,p=>p.Value),EffectsByShader=baseFace.Resources.EffectsByShader.Append(new KeyValuePair<string,StaticEffectDescription>(overlayPath,overlay.Resources.EffectsByShader.Values.Single())).ToDictionary(p=>p.Key,p=>p.Value),Textures=baseFace.Resources.Textures.Append(new KeyValuePair<string,DdsImage>(texture,overlay.Resources.Textures.Values.Single())).ToDictionary(p=>p.Key,p=>p.Value)};
        var blueprint=baseFace.Blueprint with{ShaderPaths=new[]{baseFace.Blueprint.ShaderPaths[0],overlayPath},RenderSets=new[]{baseFace.Blueprint.RenderSets[0],new BlueprintRenderSet(1,new uint[]{0})}};
        var prepared=baseFace.Preparation with{ShaderSlots=new[]{baseFace.Preparation.ShaderSlots[0],overlay.Preparation.ShaderSlots[0] with{Slot=1,ShaderPath=overlayPath,TextureBindings=new Dictionary<uint,string>{{Main,texture}}}}};
        var composed=BlueprintFaceCompositor.Compose(blueprint,resources,prepared);
        if(composed.Layers.Any(l=>l.WrittenPixels!=4)||!composed.Image.Mips[0].Rgba.Span.SequenceEqual(Enumerable.Repeat(new byte[]{100,60,80,164},4).SelectMany(v=>v).ToArray()))throw new Exception("Hand-calculated RGB factor/copied alpha/straight blend/shared-edge result differs.");
        var gradient=new DdsImage(DdsFormat.Rgba32,new[]{new DdsMip(2,1,new byte[]{0,0,0,255,200,0,0,255})});
        foreach(bool clamp in new[]{false,true})
        {
            var fixture=Fixture(gradient,clamp:clamp,vertices:Quad(constantUv:true));var sampled=BlueprintFaceCompositor.Compose(fixture.Blueprint,fixture.Resources,fixture.Preparation);
            if(sampled.Image.Mips[0].Rgba.Span[0]!=(clamp?0:100))throw new Exception("Hand-calculated clamp/wrap linear edge differs.");
        }
        var mips=new DdsImage(DdsFormat.Rgba32,new[]{new DdsMip(4,4,Enumerable.Repeat(new byte[]{10,0,0,255},16).SelectMany(v=>v).ToArray()),new DdsMip(2,2,Enumerable.Repeat(new byte[]{20,0,0,255},4).SelectMany(v=>v).ToArray()),new DdsMip(1,1,new byte[]{30,0,0,255})});
        var lod=Fixture(mips,size:1);var reduced=BlueprintFaceCompositor.Compose(lod.Blueprint,lod.Resources,lod.Preparation);
        if(reduced.Image.Mips[0].Rgba.Span[0]!=30||reduced.Layers[0].MipLevels.Any(m=>m!=2)||reduced.Layers[0].WrittenPixels!=1)throw new Exception("Point LOD/shared diagonal selection differs.");
        var clipped=Fixture(Solid(10,20,30,255),vertices:Quad(left:-.5f,right:.5f));var clippedResult=BlueprintFaceCompositor.Compose(clipped.Blueprint,clipped.Resources,clipped.Preparation);
        if(clippedResult.Layers[0].WrittenPixels!=2||clippedResult.Image.Mips[0].Rgba.Span[4]!=0||clippedResult.Image.Mips[0].Rgba.Span[12]!=0)throw new Exception("Clipped face rectangle coverage differs.");
        var degenerate=Fixture(Solid(10,20,30,255),vertices:Quad(left:0,right:0));if(BlueprintFaceCompositor.Compose(degenerate.Blueprint,degenerate.Resources,degenerate.Preparation).Layers[0].WrittenPixels!=0)throw new Exception("Degenerate face fan wrote pixels.");
        void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
        Reject(()=>BlueprintFaceCompositor.Compose(baseFace.Blueprint,baseFace.Resources with{PixelPrograms=null},baseFace.Preparation));
        Reject(()=>BlueprintFaceCompositor.Compose(baseFace.Blueprint,baseFace.Resources,baseFace.Preparation with{ShaderSlots=Array.Empty<BlueprintPreparedShader>()}));
        Reject(()=>BlueprintFaceCompositor.Compose(baseFace.Blueprint,baseFace.Resources,baseFace.Preparation,new(MaximumDimension:1)));
        Reject(()=>BlueprintFaceCompositor.Compose(baseFace.Blueprint,baseFace.Resources,baseFace.Preparation,new(MaximumTriangles:1)));
        Reject(()=>BlueprintFaceCompositor.Compose(baseFace.Blueprint,baseFace.Resources,baseFace.Preparation,new(MaximumPixelTests:1)));
        var wrongSampler=baseFace.Resources.Shaders.Values.Single();byte[] sampler=wrongSampler.Textures[0].BindingMetadata.ToArray();sampler[9]=1;
        Reject(()=>BlueprintFaceCompositor.Compose(baseFace.Blueprint,baseFace.Resources with{Shaders=new Dictionary<string,StaticShaderReferenceDescription>{{baseFace.Blueprint.ShaderPaths[0],wrongSampler with{Textures=new[]{wrongSampler.Textures[0] with{BindingMetadata=sampler}}}}}},baseFace.Preparation));
        Reject(()=>BlueprintFaceCompositor.Compose(baseFace.Blueprint,baseFace.Resources,baseFace.Preparation with{ShaderSlots=new[]{baseFace.Preparation.ShaderSlots[0] with{TextureBindings=new Dictionary<uint,string>()}}}));
        var invalidMip=Fixture(new(DdsFormat.Rgba32,new[]{new DdsMip(1,1,new byte[3])}));
        Reject(()=>BlueprintFaceCompositor.Compose(invalidMip.Blueprint,invalidMip.Resources,invalidMip.Preparation));
        foreach(var invalid in new[]{Quad(reversed:true),Quad(nonplanar:true),Quad(left:float.NaN)})Reject(()=>BlueprintFaceCompositor.Compose(baseFace.Blueprint with{VertexArrays=new[]{invalid}},baseFace.Resources,baseFace.Preparation));
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();HostAccountingScenarios.Reject<OperationCanceledException>(()=>BlueprintFaceCompositor.Compose(baseFace.Blueprint,baseFace.Resources,baseFace.Preparation,cancellation:cancellation.Token));
        Array.Clear(basePixels);
        if(composed.Image.Mips[0].Rgba.Span[0]!=100)throw new Exception("Composed output ownership differs.");
        Console.WriteLine("PASS: CPU face copy and hand-calculated RGB/alpha blend, shared fan edge, clipping/degeneracy, clamp/wrap samples, point LOD and resource/budget/planarity/winding/cancellation gates");
    }
}
