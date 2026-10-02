using System.Text;
using Stardust.Core.Assets;

internal static class BlueprintGeneratedFaceScenarios
{
    internal static void Run()
    {
        const string valid="""{"version":1,"values":[{"variableName":"color","factoryFlag":false,"value":0},{"variableName":"color","factoryFlag":true,"value":1}]}""";
        byte[] bytes=Encoding.UTF8.GetBytes(valid);var values=BlueprintFaceCustomizationProfile.Parse(bytes);Array.Clear(bytes);
        if(values[new("color",false)]!=0||values[new("color",true)]!=1)throw new Exception("Profile exact factory identity/ownership differs.");
        void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
        foreach(string input in new[]{"{}",valid.Replace("\"version\":1","\"version\":2"),valid.Replace("\"version\":1","\"version\":1,\"version\":1"),valid.Replace("\"value\":0","\"value\":0,\"unexpected\":0"),valid.Replace("\"value\":0","\"value\":2147483648"),valid.Replace("\"factoryFlag\":false","\"factoryFlag\":0"),valid.Replace("\"factoryFlag\":true","\"factoryFlag\":false"),valid.Replace("\"color\"","\"has space\""),valid[..^1]})Reject(()=>BlueprintFaceCustomizationProfile.Parse(Encoding.UTF8.GetBytes(input)));
        Reject(()=>BlueprintFaceCustomizationProfile.Parse(new byte[65537]));
        using var stream=new MemoryStream(Encoding.UTF8.GetBytes(valid));if(BlueprintFaceCustomizationProfile.Load(stream).Count!=2)throw new Exception("Profile stream differs.");
        var fixture=BlueprintFaceCompositionScenarios.Fixture(new(DdsFormat.Rgba32,new[]{new DdsMip(1,1,new byte[]{80,90,100,255})}));
        string renderer="texturerenderer/test.trt";uint tag=0x4D41494E;
        var blueprint=fixture.Blueprint with{PrepareLists=new IReadOnlyList<BlueprintPrepareOperation>[]{new BlueprintPrepareOperation[]{new("SSTC",0,tag,0,null,null,null,null),new("STFP",0,tag,null,null,"color","palette/test.pal",0)}}};
        var resources=fixture.Resources with{Palettes=new Dictionary<string,PaletteDescription>{{"palette/test.pal",PaletteDecoder.Decode(PaletteScenarios.Fixture(new byte[]{255,255,255,0}))}}};
        var geometry=SkeletalMeshGeometryDecoder.Decode(SkeletalGeometryScenarios.Fixture());
        var meshes=new Dictionary<string,SkeletalMeshGeometryDescription>{{"appearance/mesh/a.mgn",geometry},{"appearance/mesh/b.mgn",geometry}};
        var blueprints=new Dictionary<string,BlueprintTextureRendererDescription>{{renderer,blueprint}};var supplied=new Dictionary<BlueprintCustomizationKey,int>{{new("color",false),0}};
        var generated=BlueprintGeneratedFaceBuilder.Create(meshes,blueprints,resources,supplied);supplied.Clear();
        if(generated.Outputs.Count!=1||generated.Bindings.Count!=2||generated.Values[new("color",false)]!=0||generated.Outputs[renderer].Image.Mips[0].Rgba.Span[0]!=80)throw new Exception("Generated closure/binding/value ownership differs.");
        Reject(()=>BlueprintGeneratedFaceBuilder.Create(meshes,blueprints,resources,supplied));supplied[new("color",false)]=0;supplied[new("extra",true)]=0;
        Reject(()=>BlueprintGeneratedFaceBuilder.Create(meshes,blueprints,resources,supplied));supplied.Remove(new("extra",true));
        Reject(()=>BlueprintGeneratedFaceBuilder.Create(meshes,new Dictionary<string,BlueprintTextureRendererDescription>(),resources,supplied));
        Reject(()=>BlueprintGeneratedFaceBuilder.Create(new Dictionary<string,SkeletalMeshGeometryDescription>{{"mesh",geometry with{TextureRenderers=Array.Empty<SkeletalTextureRendererReference>()}}},blueprints,resources,supplied));
        Reject(()=>BlueprintGeneratedFaceBuilder.Create(new Dictionary<string,SkeletalMeshGeometryDescription>{{"mesh",geometry with{TextureRenderers=new[]{new SkeletalTextureRendererReference(renderer,new[]{new SkeletalTextureRendererBinding(0,0)})}}}},blueprints,resources,supplied));
        var conflict=geometry with{TextureRenderers=new[]{geometry.TextureRenderers[0],new SkeletalTextureRendererReference("texturerenderer/other.trt",geometry.TextureRenderers[0].Bindings)}};
        Reject(()=>BlueprintGeneratedFaceBuilder.Create(new Dictionary<string,SkeletalMeshGeometryDescription>{{"mesh",conflict}},new Dictionary<string,BlueprintTextureRendererDescription>{{renderer,blueprint},{"texturerenderer/other.trt",blueprint}},resources,supplied));
        using var canceled=new CancellationTokenSource();canceled.Cancel();HostAccountingScenarios.Reject<OperationCanceledException>(()=>BlueprintGeneratedFaceBuilder.Create(meshes,blueprints,resources,supplied,canceled.Token));
        Console.WriteLine("PASS: strict explicit face profile/duplicate fields/types/counts/ownership and generated per-mesh TRTS closure/conflict/unbound/extra-value/cancellation gates");
    }
}
