using Stardust.Core.Assets;

internal static class SkeletalCustomizationScenarios
{
    internal static CreatureAppearanceDescriptorBundle Fixture()
    {
        const uint main=0x4D41494E;const string renderer="texturerenderer/test.trt",palettePath="palette/test.pal",shaderPath="shader/test.sht";
        var fixture=BlueprintFaceCompositionScenarios.Fixture(new(DdsFormat.Rgba32,new[]{new DdsMip(1,1,new byte[]{255,255,255,255})}));
        var palette=PaletteDecoder.Decode(PaletteScenarios.Fixture(new byte[]{20,40,60,0,80,100,120,0}));
        var blueprint=fixture.Blueprint with{PrepareLists=new IReadOnlyList<BlueprintPrepareOperation>[]{new BlueprintPrepareOperation[]{new("SSTC",0,main,0,null,null,null,null),new("STFP",0,main,null,null,"skin",palettePath,0)}}};
        var resources=fixture.Resources with{Palettes=new Dictionary<string,PaletteDescription>{{palettePath,palette}}};
        var refs=fixture.Resources.Shaders.Values.Single();
        var description=new CustomizableShaderDescription("0001",refs,Array.Empty<string>(),Array.Empty<ShaderTextureSelection>(),new[]{new ShaderPaletteFactor("skin",0,main,palettePath,0)},refs.Metadata);
        var selected=CustomizableShaderPaletteQuery.Select(description,_=>palette);
        var paletteBundle=new CustomizableShaderPaletteBundle(description,new Dictionary<string,PaletteDescription>{{palettePath,palette}},Array.Empty<string>(),selected,new Dictionary<string,AssetSourceIdentity>(),Array.Empty<string>());
        var materials=new SkeletalMaterialBundle(new Dictionary<string,StaticMeshMaterial>{{shaderPath,new(refs,StaticShaderParameterDecoder.Decode(refs),description)}},new Dictionary<string,DdsImage>(),new Dictionary<string,StaticEffectDescription>(),
            new Dictionary<string,CustomizableShaderPaletteBundle>{{shaderPath,paletteBundle}},new(new Dictionary<string,SkeletalAlbedoProgram>(),new Dictionary<string,SkeletalAlbedoFactors>{{shaderPath,new("synthetic",false,main,0,0xFF14283C,0xFF14283C)}}));
        var bundle=new CreatureAppearanceDescriptorBundle(null!,null!,new Dictionary<string,LodMeshGeneratorDescription>(),new Dictionary<string,AssetSourceIdentity>(),Geometries:new Dictionary<string,SkeletalMeshGeometryDescription>{{"mesh",SkeletalMeshGeometryDecoder.Decode(SkeletalGeometryScenarios.Fixture())}},
            MaterialBundle:materials,FaceBlueprints:new Dictionary<string,BlueprintTextureRendererDescription>{{renderer,blueprint}},FaceResources:resources);
        return bundle;
    }
    internal static void Run()
    {
        const string renderer="texturerenderer/test.trt",shaderPath="shader/test.sht";
        var bundle=Fixture();
        var controls=SkeletalAppearanceCustomization.DescribeControls(bundle);
        if(controls.Count!=1||controls[0].Key!=new ShaderCustomizationKey("skin",false)||controls[0].MaximumValue!=1)throw new Exception("Shared owned control cardinality differs.");
        var values=new Dictionary<ShaderCustomizationKey,int>{{new("skin",false),1}};
        var result=SkeletalAppearanceCustomization.Apply(bundle,values);values.Clear();
        if(result.MaterialBundle!.AlbedoFactors!.Materials[shaderPath].FirstArgb!=0xFF506478||result.GeneratedFaces!.Outputs[renderer].Image.Mips[0].Rgba.Span[0]!=80||result.CustomizationValues![new("skin",false)]!=1||result.MaterialBundle.PaletteDefaults![shaderPath].AuthoredFactors.Single().UsedAuthoredDefault)
            throw new Exception("Shared skin selection/explicit values/ownership differs.");
        if(bundle.MaterialBundle!.AlbedoFactors!.Materials[shaderPath].FirstArgb!=0xFF14283C||bundle.GeneratedFaces is not null)throw new Exception("Customization mutated input bundle.");
        values[new("skin",false)]=-1;var fallback=SkeletalAppearanceCustomization.Apply(bundle,values);
        if(fallback.MaterialBundle!.AlbedoFactors!.Materials[shaderPath].FirstArgb!=0xFF14283C||fallback.GeneratedFaces!.Outputs[renderer].Image.Mips[0].Rgba.Span[0]!=80)throw new Exception("CSHD first versus BTRT last palette fallback was flattened.");
        void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
        values.Clear();Reject(()=>SkeletalAppearanceCustomization.Apply(bundle,values));values[new("skin",true)]=0;Reject(()=>SkeletalAppearanceCustomization.Apply(bundle,values));
        values[new("skin",false)]=0;Reject(()=>SkeletalAppearanceCustomization.Apply(bundle,values));values.Remove(new("skin",true));
        Reject(()=>SkeletalAppearanceCustomization.Apply(bundle with{FaceResources=null},values));
        HostAccountingScenarios.Reject<OperationCanceledException>(()=>SkeletalAppearanceCustomization.Apply(bundle,values,new CancellationToken(true)));
        Console.WriteLine("PASS: shared explicit face/material namespace selection, owned input/output separation, distinct invalid palette fallbacks, missing/extra/wrong-domain and cancellation gates");
    }
}
