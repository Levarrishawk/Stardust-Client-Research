using Stardust.Core.Assets;

internal static class SkeletalTextureChoiceScenarios
{
    internal static void Run()
    {
        var custom=CustomizableShaderDecoder.Decode(CustomizableShaderScenarios.Fixture());var image=DdsDecoder.Decode(DdsScenarios.Fixture("DXT1",4,4,1,new byte[8]));
        var material=new StaticMeshMaterial(custom.InlineShader,StaticShaderParameterDecoder.Decode(custom.InlineShader),custom);
        var source=new SkeletalMaterialBundle(new Dictionary<string,StaticMeshMaterial>{{"shader",material}},new Dictionary<string,DdsImage>{{"texture/choice0.dds",image},{"texture/choice1.dds",image}},new Dictionary<string,StaticEffectDescription>());
        var key=new ShaderCustomizationKey("ExactCaseVariable",true);var values=new Dictionary<ShaderCustomizationKey,int>{{key,int.MaxValue}};
        var selected=SkeletalTextureChoices.Apply(source,values);values.Clear();
        if(selected.TextureSelection!.Values[key]!=int.MaxValue||selected.TextureSelection.Selections["shader"][0].ClampedValue!=1||selected.TextureSelection.References["shader"].Textures[0].AssetPath!="texture/choice1.dds"||!ReferenceEquals(selected.Textures,source.Textures)||!ReferenceEquals(selected.Materials,source.Materials))throw new Exception("Owned explicit texture selection differs.");
        var minimum=SkeletalTextureChoices.Apply(selected,new Dictionary<ShaderCustomizationKey,int>{{key,int.MinValue}});
        if(minimum.TextureSelection!.References["shader"].Textures[0].AssetPath!="texture/choice0.dds")throw new Exception("Texture selection accumulated previous state or did not clamp.");
        void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
        Reject(()=>SkeletalTextureChoices.Apply(source,new Dictionary<ShaderCustomizationKey,int>()));
        Reject(()=>SkeletalTextureChoices.Apply(source,new Dictionary<ShaderCustomizationKey,int>{{key with{Private=false},0}}));
        Reject(()=>SkeletalTextureChoices.Apply(source with{Textures=new Dictionary<string,DdsImage>()},new Dictionary<ShaderCustomizationKey,int>{{key,0}}));
        var absent=custom with{TextureSelections=new[]{custom.TextureSelections[0] with{TextureTag=0}}};
        var untouched=SkeletalTextureChoices.Apply(source with{Materials=new Dictionary<string,StaticMeshMaterial>{{"shader",material with{Customization=absent}}}},new Dictionary<ShaderCustomizationKey,int>{{key,0}});
        if(!untouched.TextureSelection!.References["shader"].Textures.SequenceEqual(custom.InlineShader.Textures))throw new Exception("Absent texture tag created a binding.");
        HostAccountingScenarios.Reject<OperationCanceledException>(()=>SkeletalTextureChoices.Apply(source,new Dictionary<ShaderCustomizationKey,int>{{key,0}},new CancellationToken(true)));
        int reads=0;HostAccountingScenarios.Reject<ArgumentException>(()=>CreatureAppearanceDescriptorBundleReader.Load(_=>{reads++;return new(TreLookupKind.Missing);},"object/x.iff",new string('0',64),new string('0',64),loadTextureChoices:true));if(reads!=0)throw new Exception("Invalid choice option reached lookup.");
        byte[] specification=Array.Empty<byte>();
        HostAccountingScenarios.Reject<ArgumentException>(()=>InteriorSceneContentBundleReader.Load(_=>{reads++;return new(TreLookupKind.Missing);},specification,new string('0',64),new string('0',64),"appearance/x.pob",new string('0',64),characterNormalDiffuse:true));
        HostAccountingScenarios.Reject<ArgumentException>(()=>InteriorSceneContentBundleReader.Load(_=>{reads++;return new(TreLookupKind.Missing);},specification,new string('0',64),new string('0',64),"appearance/x.pob",new string('0',64),characterTextureValues:new Dictionary<ShaderCustomizationKey,int>()));
        if(reads!=0)throw new Exception("Invalid interior normal/detail option reached lookup.");
        Console.WriteLine("PASS: explicit skeletal texture choice ownership/clamping/namespace/nonaccumulation/absent-target and missing-resource/cancellation gates");
    }
}
